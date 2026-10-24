using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Ini_Edit___Translate
{
    internal interface IChatGptTokens
    {
        Task<string> GetAccessTokenAsync(string clientId, CancellationToken cancel);
    }

    internal sealed class ChatGptModel
    {
        internal string Slug, DisplayName;
        public override string ToString() => DisplayName;
    }

    internal sealed class ChatGptClient : IChatGptTokens, IDisposable
    {
        internal const string Issuer = "https://auth.openai.com";
        internal const string Resource = "https://api.openai.com/v1";
        internal const string TokenEndpoint = Issuer + "/api/accounts/oauth/token";
        internal const string UsageUrl = "https://chatgpt.com/settings/usage";
        internal const string Scope = "openid profile email offline_access resource.invoke chatgpt.tokens.use.direct";
        private readonly HttpClient http;
        internal readonly ChatGptVault Vault;

        internal ChatGptClient(ChatGptVault vault = null, HttpMessageHandler handler = null)
        {
            Vault = vault ?? new ChatGptVault();
            http = handler == null ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) : new HttpClient(handler);
            http.Timeout = TimeSpan.FromSeconds(45);
        }

        internal static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        internal static byte[] Decode(string value)
        {
            string text = value.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(text.PadRight((text.Length + 3) / 4 * 4, '='));
        }
        internal static string RandomValue()
        {
            var bytes = new byte[32]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return Base64Url(bytes);
        }
        internal static bool Equal(string a, string b)
        {
            if (a == null || b == null) return false;
            using (var sha = SHA256.Create())
            {
                var left = sha.ComputeHash(Encoding.UTF8.GetBytes(a)); var right = sha.ComputeHash(Encoding.UTF8.GetBytes(b));
                int difference = 0; for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i]; return difference == 0;
            }
        }

        internal sealed class Attempt
        {
            internal string State = RandomValue(), Nonce = RandomValue(), Verifier = RandomValue();
            internal string Redirect, HostId, ClientId, Email, IdToken;
            internal bool RequestConsent;
            internal bool IsNew => string.IsNullOrEmpty(ClientId);
            internal string AuthorizeUrl()
            {
                string challenge; using (var sha = SHA256.Create()) challenge = Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(Verifier)));
                var values = new Dictionary<string, string> { ["client_id"] = IsNew ? "dynamic_agent_client" : ClientId,
                    ["ext_agent_host_id"] = HostId, ["response_type"] = "code", ["redirect_uri"] = Redirect,
                    ["scope"] = Scope, ["resource"] = Resource, ["state"] = State, ["nonce"] = Nonce,
                    ["code_challenge_method"] = "S256", ["code_challenge"] = challenge };
                if (IsNew) values["agent_name_hint"] = "INI Edit Translate 2026";
                else
                {
                    if (RequestConsent) values["prompt"] = "consent";
                    if (!string.IsNullOrEmpty(Email)) values["login_hint"] = Email;
                    if (!string.IsNullOrEmpty(IdToken)) values["id_token_hint"] = IdToken;
                }
                return Issuer + "/api/accounts/authorize?" + string.Join("&", values.Select(v => Uri.EscapeDataString(v.Key) + "=" + Uri.EscapeDataString(v.Value)));
            }
        }

        internal static (string Code, string ClientId) ValidateCallback(Uri uri, Attempt attempt)
        {
            var redirect = new Uri(attempt.Redirect);
            if (uri.Scheme != redirect.Scheme || uri.Host != redirect.Host || uri.Port != redirect.Port || uri.AbsolutePath != redirect.AbsolutePath)
                throw new InvalidOperationException(Ui.Text("cgInvalidLogin"));
            var query = HttpUtility.ParseQueryString(uri.Query);
            foreach (string field in new[] { "state", "code", "client_id", "error", "iss" })
                if ((query.GetValues(field)?.Length ?? 0) > 1) throw new InvalidOperationException(Ui.Text("cgInvalidLogin"));
            if (!Equal(query["state"], attempt.State)) throw new InvalidOperationException(Ui.Text("cgInvalidLogin"));
            if (!string.IsNullOrEmpty(query["iss"]) && query["iss"] != Issuer) throw new InvalidOperationException(Ui.Text("cgInvalidLogin"));
            if (!string.IsNullOrEmpty(query["error"])) throw new InvalidOperationException(Ui.Text("cgDeclined"));
            string client = query["client_id"] ?? attempt.ClientId;
            if (string.IsNullOrWhiteSpace(query["code"]) || string.IsNullOrWhiteSpace(client) || client == "dynamic_agent_client" ||
                client.Length > 512 || client.Any(char.IsControl) || (!attempt.IsNew && !Equal(client, attempt.ClientId)))
                throw new InvalidOperationException(Ui.Text("cgInvalidLogin"));
            return (query["code"], client);
        }

        internal async Task<ChatGptAccount> SignInAsync(string existingClientId, CancellationToken cancel, Action<string> browser = null)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
                timeout.CancelAfter(TimeSpan.FromMinutes(5));
                cancel = timeout.Token;
                ChatGptAccount previous;
                string host;
                using (var lease = await Vault.LockAsync(cancel).ConfigureAwait(false))
                {
                    host = lease.Data.HostId;
                    previous = lease.Data.Accounts.SingleOrDefault(a => a.ClientId == existingClientId);
                    if (!string.IsNullOrEmpty(existingClientId) && previous == null) throw new InvalidOperationException(Ui.Text("cgLoginRequired"));
                    lease.Save(); // Persist the stable host before the browser is opened.
                }
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Server.ExclusiveAddressUse = true;
                listener.Start();
                try
                {
                    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                    var attempt = new Attempt { HostId = host, Redirect = "http://127.0.0.1:" + port + "/auth/callback",
                        ClientId = previous?.ClientId, Email = previous?.Email, IdToken = previous?.IdToken,
                        RequestConsent = previous != null && !previous.PlanEnabled };
                    using (cancel.Register(() => listener.Stop()))
                    {
                        (browser ?? (url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })))(attempt.AuthorizeUrl());
                        (string Code, string ClientId) callback;
                        while (true)
                        {
                            TcpClient socket;
                            try { socket = await listener.AcceptTcpClientAsync().ConfigureAwait(false); }
                            catch (Exception) when (cancel.IsCancellationRequested) { throw new OperationCanceledException(cancel); }
                            using (socket)
                            using (var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancel))
                            {
                                requestTimeout.CancelAfter(TimeSpan.FromSeconds(15));
                                using (requestTimeout.Token.Register(() => socket.Close()))
                                {
                                    string header;
                                    try { header = await ReadHeadersAsync(socket.GetStream(), requestTimeout.Token).ConfigureAwait(false); }
                                    catch (Exception) when (!cancel.IsCancellationRequested) { continue; }
                                    var first = header.Split(new[] { "\r\n" }, StringSplitOptions.None)[0].Split(' ');
                                    if (first.Length != 3 || first[0] != "GET" || !first[1].StartsWith("/auth/callback?", StringComparison.Ordinal))
                                    { await ReplyAsync(socket, false).ConfigureAwait(false); continue; }
                                    var uri = new Uri("http://127.0.0.1:" + port + first[1]);
                                    var query = HttpUtility.ParseQueryString(uri.Query);
                                    if (!Equal(query["state"], attempt.State)) { await ReplyAsync(socket, false).ConfigureAwait(false); continue; }
                                    try { callback = ValidateCallback(uri, attempt); }
                                    catch { await ReplyAsync(socket, false).ConfigureAwait(false); throw; }
                                    await ReplyAsync(socket, true).ConfigureAwait(false);
                                    break;
                                }
                            }
                        }
                        var token = await TokenAsync(new Dictionary<string, string> { ["grant_type"] = "authorization_code",
                            ["client_id"] = callback.ClientId, ["code"] = callback.Code, ["code_verifier"] = attempt.Verifier,
                            ["redirect_uri"] = attempt.Redirect, ["resource"] = Resource }, cancel).ConfigureAwait(false);
                        var claims = await VerifyIdAsync((string)token["id_token"], callback.ClientId, attempt.Nonce, cancel).ConfigureAwait(false);
                        if (previous != null && !Equal((string)claims["sub"], previous.Subject)) throw new InvalidOperationException(Ui.Text("cgWrongAccount"));
                        var account = MakeAccount(token, callback.ClientId, claims, null);
                        using (var lease = await Vault.LockAsync(cancel).ConfigureAwait(false))
                        {
                            var saved = lease.Data.Accounts.SingleOrDefault(a => a.ClientId == account.ClientId);
                            if (saved != null && !Equal(saved.Subject, account.Subject)) throw new InvalidOperationException(Ui.Text("cgWrongAccount"));
                            if (saved != null) lease.Data.Accounts.Remove(saved);
                            lease.Data.Accounts.Add(account); lease.Save();
                        }
                        return account;
                    }
                }
                catch (Exception) when (cancel.IsCancellationRequested) { throw new OperationCanceledException(cancel); }
                finally { listener.Stop(); }
            }
        }

        private static async Task<string> ReadHeadersAsync(NetworkStream stream, CancellationToken cancel)
        {
            var text = new StringBuilder(); var one = new byte[1];
            while (text.Length < 8192)
            {
                if (await stream.ReadAsync(one, 0, 1, cancel).ConfigureAwait(false) == 0) throw new IOException();
                text.Append((char)one[0]);
                if (text.Length >= 4 && text.ToString(text.Length - 4, 4) == "\r\n\r\n") return text.ToString();
            }
            throw new IOException();
        }
        private static async Task ReplyAsync(TcpClient socket, bool valid)
        {
            // Never echo codes, tokens, error descriptions or callback parameters into HTML.
            var body = Encoding.UTF8.GetBytes("<!doctype html><meta charset=utf-8><title>INI Edit Translate 2026</title><p>" +
                (valid ? Ui.Text("cgBrowserDone") : Ui.Text("cgInvalidLogin")) + "</p>");
            var header = Encoding.ASCII.GetBytes("HTTP/1.1 " + (valid ? "200 OK" : "400 Bad Request") + "\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\nContent-Security-Policy: default-src 'none'\r\nReferrer-Policy: no-referrer\r\nConnection: close\r\nContent-Length: " + body.Length + "\r\n\r\n");
            try { var stream = socket.GetStream(); await stream.WriteAsync(header, 0, header.Length).ConfigureAwait(false); await stream.WriteAsync(body, 0, body.Length).ConfigureAwait(false); }
            catch (IOException) { }
        }

        private async Task<JObject> TokenAsync(Dictionary<string, string> fields, CancellationToken cancel)
        {
            using (var body = new FormUrlEncodedContent(fields))
            using (var response = await http.PostAsync(TokenEndpoint, body, cancel).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode) throw await OpenAiErrors.FromHttpAsync(response, true).ConfigureAwait(false);
                return JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
        }

        private async Task<JObject> VerifyIdAsync(string jwt, string clientId, string nonce, CancellationToken cancel)
        {
            using (var response = await http.GetAsync(Issuer + "/.well-known/jwks.json", cancel).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException(Ui.Text("cgInvalidLogin"));
                return ValidateId(jwt, clientId, nonce, JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false)), DateTimeOffset.UtcNow);
            }
        }

        internal static JObject ValidateId(string jwt, string clientId, string nonce, JObject jwks, DateTimeOffset now)
        {
            try
            {
                if (string.IsNullOrEmpty(jwt) || jwt.Length > 65536) throw new InvalidDataException();
                var parts = jwt.Split('.'); if (parts.Length != 3) throw new InvalidDataException();
                var header = JObject.Parse(Encoding.UTF8.GetString(Decode(parts[0])));
                if ((string)header["alg"] != "RS256" || string.IsNullOrEmpty((string)header["kid"])) throw new InvalidDataException();
                var key = ((JArray)jwks["keys"]).OfType<JObject>().Single(k => (string)k["kid"] == (string)header["kid"] &&
                    (string)k["kty"] == "RSA" && (k["use"] == null || (string)k["use"] == "sig") && (k["alg"] == null || (string)k["alg"] == "RS256"));
                var modulus = Decode((string)key["n"]); if (modulus.Length < 256) throw new InvalidDataException();
                using (var rsa = new RSACryptoServiceProvider { PersistKeyInCsp = false })
                {
                    rsa.ImportParameters(new RSAParameters { Modulus = modulus, Exponent = Decode((string)key["e"]) });
                    if (!rsa.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), "SHA256", Decode(parts[2]))) throw new InvalidDataException();
                }
                var claims = JObject.Parse(Encoding.UTF8.GetString(Decode(parts[1])));
                var audiences = claims["aud"] is JArray array ? array.Values<string>().ToArray() : new[] { (string)claims["aud"] };
                if ((string)claims["iss"] != Issuer || !audiences.Contains(clientId) ||
                    (claims["azp"] != null && (string)claims["azp"] != clientId) || (audiences.Length > 1 && (string)claims["azp"] != clientId) ||
                    string.IsNullOrWhiteSpace((string)claims["sub"]) || (long?)claims["exp"] <= now.ToUnixTimeSeconds() || claims["exp"] == null ||
                    (long?)claims["nbf"] > now.ToUnixTimeSeconds() + 60 || (long?)claims["iat"] > now.ToUnixTimeSeconds() + 60 ||
                    (nonce != null && !Equal((string)claims["nonce"], nonce))) throw new InvalidDataException();
                return claims;
            }
            catch { throw new InvalidOperationException(Ui.Text("cgInvalidLogin")); }
        }

        private static ChatGptAccount MakeAccount(JObject token, string clientId, JObject claims, ChatGptAccount previous)
        {
            string access = (string)token["access_token"];
            long seconds = (long?)token["expires_in"] ?? 0;
            if (string.IsNullOrWhiteSpace(access) || seconds <= 0 || seconds > 86400 ||
                !string.Equals((string)token["token_type"], "Bearer", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(Ui.Text("cgInvalidLogin"));
            var scopes = token["scope"]?.Type == JTokenType.String ? ((string)token["scope"]).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) :
                token["scopes"] is JArray array ? array.Values<string>().ToArray() : previous?.Scopes ?? new string[0];
            return new ChatGptAccount { ClientId = clientId, Subject = (string)claims?["sub"] ?? previous?.Subject,
                Email = (string)claims?["email"] ?? previous?.Email, IdToken = (string)token["id_token"] ?? previous?.IdToken,
                AccessToken = access, RefreshToken = (string)token["refresh_token"] ?? previous?.RefreshToken,
                ExpiresUtc = DateTime.UtcNow.AddSeconds(seconds), Scopes = scopes };
        }

        public async Task<string> GetAccessTokenAsync(string clientId, CancellationToken cancel)
        {
            using (var lease = await Vault.LockAsync(cancel).ConfigureAwait(false))
            {
                var account = lease.Data.Accounts.SingleOrDefault(a => a.ClientId == clientId);
                if (account == null || !account.SignedIn) throw new InvalidOperationException(Ui.Text("cgLoginRequired"));
                if (!account.PlanEnabled) throw new InvalidOperationException(Ui.Text("cgNoConsent"));
                if (!string.IsNullOrEmpty(account.AccessToken) && account.ExpiresUtc > DateTime.UtcNow.AddMinutes(2)) return account.AccessToken;
                if (string.IsNullOrEmpty(account.RefreshToken)) throw new InvalidOperationException(Ui.Text("cgLoginRequired"));
                var token = await TokenAsync(new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["client_id"] = account.ClientId,
                    ["refresh_token"] = account.RefreshToken, ["resource"] = Resource }, cancel).ConfigureAwait(false);
                JObject claims = null;
                if (token["id_token"] != null)
                {
                    claims = await VerifyIdAsync((string)token["id_token"], account.ClientId, null, cancel).ConfigureAwait(false);
                    if (!Equal((string)claims["sub"], account.Subject)) throw new InvalidOperationException(Ui.Text("cgWrongAccount"));
                }
                var updated = MakeAccount(token, account.ClientId, claims, account);
                lease.Data.Accounts.Remove(account); lease.Data.Accounts.Add(updated); lease.Save();
                if (!updated.PlanEnabled) throw new InvalidOperationException(Ui.Text("cgNoConsent"));
                return updated.AccessToken;
            }
        }

        internal async Task<List<ChatGptModel>> ModelsAsync(string clientId, CancellationToken cancel)
        {
            var token = await GetAccessTokenAsync(clientId, cancel).ConfigureAwait(false);
            using (var request = new HttpRequestMessage(HttpMethod.Get, Resource + "/models"))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using (var response = await http.SendAsync(request, cancel).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode) throw await OpenAiErrors.FromHttpAsync(response, true).ConfigureAwait(false);
                JObject result;
                try { result = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false)); }
                catch (Newtonsoft.Json.JsonException) { throw new InvalidOperationException(Ui.Text("cgCatalogFormat")); }
                if (!(result["models"] is JArray)) throw new InvalidOperationException(Ui.Text("cgCatalogFormat"));
                    var models = (result["models"] as JArray ?? new JArray()).OfType<JObject>()
                        .Where(m => (string)m["visibility"] == "list" && !string.IsNullOrWhiteSpace((string)m["slug"]))
                        .Select(m => new ChatGptModel { Slug = (string)m["slug"], DisplayName = (string)m["display_name"] ?? (string)m["slug"] }).ToList();
                    if (models.Count == 0) throw new InvalidOperationException(Ui.Text("cgNoModels"));
                    return models;
                }
            }
        }

        internal async Task<bool> SignOutAsync(string clientId, CancellationToken cancel)
        {
            using (var lease = await Vault.LockAsync(cancel).ConfigureAwait(false))
            {
                var account = lease.Data.Accounts.SingleOrDefault(a => a.ClientId == clientId);
                if (account == null) return true;
                bool revoked = string.IsNullOrEmpty(account.RefreshToken);
                try
                {
                    if (!revoked)
                    {
                        JObject config;
                        using (var discovery = await http.GetAsync(Issuer + "/.well-known/openid-configuration", cancel).ConfigureAwait(false))
                        {
                            if (!discovery.IsSuccessStatusCode) throw new InvalidDataException();
                            config = JObject.Parse(await discovery.Content.ReadAsStringAsync().ConfigureAwait(false));
                        }
                        string endpoint = (string)config["revocation_endpoint"];
                        if ((string)config["issuer"] != Issuer || endpoint != Issuer + "/api/accounts/oauth/revoke") throw new InvalidDataException();
                        for (int retry = 0; retry < 2 && !revoked; retry++)
                        {
                            using (var body = new FormUrlEncodedContent(new Dictionary<string, string> {
                                ["token"] = account.RefreshToken, ["token_type_hint"] = "refresh_token", ["client_id"] = account.ClientId }))
                            using (var response = await http.PostAsync(endpoint, body, cancel).ConfigureAwait(false))
                            { revoked = response.IsSuccessStatusCode; }
                            if (!revoked && retry == 0) await Task.Delay(500, cancel).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception) { revoked = false; }
                account.AccessToken = null; account.RefreshToken = null; account.IdToken = null; account.Scopes = new string[0];
                lease.Save(); return revoked;
            }
        }
        public void Dispose() { http.Dispose(); }
    }

    internal static class OpenAiErrors
    {
        internal static string SafeCode(string code)
        {
            return code != null && System.Text.RegularExpressions.Regex.IsMatch(code,
                @"\A(?:subscription_sharing_|chatpass_v2_)[a-z_]{1,70}\z|\A(?:invalid_grant|invalid_client|invalid_refresh_token|token_expired|refresh_token_expired|refresh_token_invalidated|refresh_token_reused|insufficient_quota|credit_balance_exhausted|rate_limit_exceeded|rate_limit_error|slow_down|server_error|model_not_found|invalid_request_error|unsupported_value|permission_denied|authentication_error)\z") ? code : "-";
        }

        internal static Exception FromStream(string code)
        {
            return new InvalidOperationException(FromCode(code, true).Message + Environment.NewLine + "SSE; code=" + SafeCode(code));
        }
        internal static Exception FromCode(string code, bool chatGpt)
        {
            switch (code)
            {
                case "subscription_sharing_usage_limit_exceeded":
                    return new InvalidOperationException(Ui.Text("cgLimit"));
                case "subscription_sharing_usage_unavailable": case "subscription_sharing_user_unavailable":
                    return new InvalidOperationException(Ui.Text("cgUnavailable"));
                case "subscription_sharing_user_not_eligible":
                    return new InvalidOperationException(Ui.Text("cgNotEligible"));
                case "subscription_sharing_route_not_supported": case "chatpass_v2_scope_not_authorized":
                case "chatpass_v2_invalid_authorization_context":
                    return new InvalidOperationException(Ui.Text("cgPermission"));
                case "subscription_sharing_unsupported_capability":
                    return new InvalidOperationException(Ui.Text("cgUnsupported"));
                case "insufficient_quota": case "credit_balance_exhausted": case "organization_spend_limit_exceeded":
                case "project_spend_limit_exceeded": case "organization_usage_limit_exceeded":
                    return new InvalidOperationException(Ui.Text(chatGpt ? "cgLimit" : "apiQuota"));
                case "rate_limit_exceeded": case "rate_limit_error": case "slow_down": return new InvalidOperationException(Ui.Text("apiRateLimit"));
                default: return new InvalidOperationException(Ui.Text(chatGpt ? "cgRequestFailed" : "translationFailed"));
            }
        }
        internal static async Task<Exception> FromHttpAsync(HttpResponseMessage response, bool chatGpt)
        {
            string code = null, param = null, shape = "non-json";
            try
            {
                var body = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                var error = body["error"] as JObject;
                shape = error != null ? "error-object" : body["detail"] != null ? "detail" : "json-object";
                code = error?["code"]?.Type == JTokenType.String ? (string)error["code"] : null;
                if (string.IsNullOrEmpty(code) && error?["type"]?.Type == JTokenType.String) code = (string)error["type"];
                param = error?["param"]?.Type == JTokenType.String ? (string)error["param"] : null;
            }
            catch { }
            string reason;
            int status = (int)response.StatusCode;
            if (chatGpt && status == 401) reason = Ui.Text("cgLoginRequired");
            else if (!string.IsNullOrEmpty(code)) reason = FromCode(code, chatGpt).Message;
            else if (status == 429) reason = Ui.Text(chatGpt ? "cgLimit" : "api429Unknown");
            else if (chatGpt && status == 403) reason = Ui.Text("cgPermission");
            else if (chatGpt && status == 503) reason = Ui.Text("cgUnavailable");
            else reason = Ui.Format("apiHttpFailure", status);
            // Only protocol identifiers are copied, never server messages or arbitrary body values.
            string safeCode = SafeCode(code);
            string safeParam = new[] { "model", "input", "stream", "store", "text.format", "service_tier", "tools", "scope", "resource", "client_id", "grant_type" }.Contains(param) ? param : "-";
            System.Collections.Generic.IEnumerable<string> ids;
            string requestId = response.Headers.TryGetValues("x-request-id", out ids) ? ids.FirstOrDefault() : null;
            if (requestId == null || !System.Text.RegularExpressions.Regex.IsMatch(requestId, @"\Areq_[a-zA-Z0-9]{1,80}\z")) requestId = "-";
            return new InvalidOperationException(reason + Environment.NewLine +
                "HTTP " + status + "; code=" + safeCode + "; param=" + safeParam + "; shape=" + shape + "; request-id=" + requestId);
        }

        internal static string SafeMessage(Exception error)
        {
            if (error is InvalidOperationException) return error.Message;
            if (error is HttpRequestException || error is System.Net.WebException || error is System.Net.Sockets.SocketException)
                return Ui.Text("cgNetworkError");
            return Ui.Text("cgRequestFailed") + " (" + error.GetType().Name + ")";
        }
    }
}
