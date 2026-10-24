using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ini_Edit___Translate;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class ChatGptTests
{
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Fails(Action work) { bool failed = false; try { work(); } catch { failed = true; } Assert(failed, "Expected rejection"); }
    private static Task<T> Async<T>(T value) => Task.FromResult(value);
    private sealed class Handler : HttpMessageHandler
    {
        internal int Calls;
        internal Func<HttpRequestMessage, Task<HttpResponseMessage>> Reply;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
        { cancel.ThrowIfCancellationRequested(); Interlocked.Increment(ref Calls); return Reply(request); }
    }
    private sealed class Tokens : IChatGptTokens
    {
        internal bool Fail;
        public Task<string> GetAccessTokenAsync(string clientId, CancellationToken cancel)
        { if (Fail) throw new InvalidOperationException("No plan permission"); return Async("oauth-access-fixture"); }
    }
    private sealed class BlockedStream : Stream
    {
        private readonly TaskCompletionSource<int> waiting = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token) => waiting.Task;
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { waiting.TrySetException(new IOException()); base.Dispose(disposing); }
    }
    private static HttpResponseMessage Json(JObject json, HttpStatusCode code = HttpStatusCode.OK) =>
        new HttpResponseMessage(code) { Content = new StringContent(json.ToString(), Encoding.UTF8, "application/json") };
    private static ChatGptAccount Account(string client = "oaiapp_fixture") => new ChatGptAccount
    {
        ClientId = client, Subject = "subject-fixture", Email = "test@example.invalid", AccessToken = "private-access-fixture",
        IdToken = "private-id-fixture", RefreshToken = "private-refresh-fixture", ExpiresUtc = DateTime.UtcNow.AddHours(1),
        Scopes = ChatGptClient.Scope.Split(' ')
    };
    private static ChatGptVault NewVault(string root) => new ChatGptVault(Path.Combine(root, Guid.NewGuid().ToString("N"), "session.dat"));
    private static void Put(ChatGptVault vault, params ChatGptAccount[] accounts)
    { using (var lease = vault.LockAsync(CancellationToken.None).GetAwaiter().GetResult()) { lease.Data.Accounts.AddRange(accounts); lease.Save(); } }
    private static JObject Claims(string client, string nonce) => new JObject { ["iss"] = ChatGptClient.Issuer, ["aud"] = client,
        ["sub"] = "subject-fixture", ["email"] = "test@example.invalid", ["exp"] = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(), ["nonce"] = nonce };
    private static string Jwt(RSACryptoServiceProvider rsa, JObject claims, string alg = "RS256")
    {
        string prefix = ChatGptClient.Base64Url(Encoding.UTF8.GetBytes(new JObject { ["alg"] = alg, ["kid"] = "fixture-key" }.ToString(Formatting.None))) + "." +
            ChatGptClient.Base64Url(Encoding.UTF8.GetBytes(claims.ToString(Formatting.None)));
        return prefix + "." + ChatGptClient.Base64Url(rsa.SignData(Encoding.ASCII.GetBytes(prefix), "SHA256"));
    }
    private static JObject Jwks(RSACryptoServiceProvider rsa)
    {
        var key = rsa.ExportParameters(false);
        return new JObject { ["keys"] = new JArray(new JObject { ["kid"] = "fixture-key", ["kty"] = "RSA", ["alg"] = "RS256", ["use"] = "sig",
            ["n"] = ChatGptClient.Base64Url(key.Modulus), ["e"] = ChatGptClient.Base64Url(key.Exponent) }) };
    }
    private static JObject Token(string id) => new JObject { ["id_token"] = id, ["access_token"] = "new-private-access",
        ["refresh_token"] = "new-private-refresh", ["token_type"] = "Bearer", ["expires_in"] = 3600, ["scope"] = ChatGptClient.Scope };
    private static JObject Completed() => new JObject { ["status"] = "completed", ["output"] = new JArray(new JObject {
        ["type"] = "message", ["content"] = new JArray(new JObject { ["type"] = "output_text", ["text"] =
            new JObject { ["de"] = "Sonne {0}", ["fr"] = "Soleil {0}", ["pl"] = "Słońce {0}" }.ToString(Formatting.None) }) }) };
    private static string Event(JObject value) => "data: " + value.ToString(Formatting.None) + "\n\n";
    private static string CompleteStream() => Event(new JObject { ["type"] = "response.output_text.delta", ["delta"] = "ignored partial" }) +
        Event(new JObject { ["type"] = "response.completed", ["response"] = Completed() });
    private static OpenAiSettings PlanSettings() => new OpenAiSettings { UseChatGpt = true, ChatGptClientId = "oaiapp_fixture", ChatGptModel = "account-model-fixture",
        ProtectedKey = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes("api-must-not-be-used"), null, DataProtectionScope.CurrentUser)) };

    internal static void Run(Action<string, Action> test)
    {
        string root = Path.Combine(Path.GetTempPath(), "iet-chatgpt-tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            test("ChatGPT PKCE, unique attempts and returning registration", () =>
            {
                var first = new ChatGptClient.Attempt { HostId = "urn:uuid:fixture", Redirect = "http://127.0.0.1:12345/auth/callback",
                    Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" };
                var query = HttpUtility.ParseQueryString(new Uri(first.AuthorizeUrl()).Query);
                Assert(query["client_id"] == "dynamic_agent_client" && query["agent_name_hint"] == "INI Edit Translate 2026", "dynamic registration");
                Assert(query["code_challenge"] == "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", "RFC 7636 challenge");
                Assert(query["resource"] == ChatGptClient.Resource && query["scope"].Contains("chatgpt.tokens.use.direct"), "plan grant");
                var second = new ChatGptClient.Attempt(); Assert(first.State != second.State && first.Nonce != second.Nonce, "fresh attempt randomness");
                first.ClientId = "issued-client"; first.Email = "test@example.invalid";
                query = HttpUtility.ParseQueryString(new Uri(first.AuthorizeUrl()).Query);
                Assert(query["client_id"] == "issued-client" && query["agent_name_hint"] == null && query["ext_agent_host_id"] == first.HostId, "issued registration reused");
                first.RequestConsent = true;
                Assert(HttpUtility.ParseQueryString(new Uri(first.AuthorizeUrl()).Query)["prompt"] == "consent", "explicit reconsent after missing permission");
            });
            test("ChatGPT callback state, origin, duplicate and client binding", () =>
            {
                var attempt = new ChatGptClient.Attempt { Redirect = "http://127.0.0.1:12345/auth/callback" };
                string good = attempt.Redirect + "?state=" + attempt.State + "&code=code-fixture&client_id=oaiapp_fixture";
                Assert(ChatGptClient.ValidateCallback(new Uri(good), attempt).ClientId == "oaiapp_fixture", "issued ID accepted");
                Fails(() => ChatGptClient.ValidateCallback(new Uri(good.Replace(attempt.State, "wrong")), attempt));
                Fails(() => ChatGptClient.ValidateCallback(new Uri(good.Replace("127.0.0.1", "localhost")), attempt));
                Fails(() => ChatGptClient.ValidateCallback(new Uri(good + "&state=" + attempt.State), attempt));
                Fails(() => ChatGptClient.ValidateCallback(new Uri(good.Replace("/auth/callback", "/callback")), attempt));
                Fails(() => ChatGptClient.ValidateCallback(new Uri(good.Replace("oaiapp_fixture", "dynamic_agent_client")), attempt));
                attempt.ClientId = "different-saved-client"; Fails(() => ChatGptClient.ValidateCallback(new Uri(good), attempt));
            });
            test("ChatGPT signed ID token verifies signature issuer audience expiry nonce", () =>
            {
                using (var rsa = new RSACryptoServiceProvider(2048) { PersistKeyInCsp = false })
                using (var other = new RSACryptoServiceProvider(2048) { PersistKeyInCsp = false })
                {
                    var claims = Claims("client", "nonce"); var keys = Jwks(rsa);
                    Assert((string)ChatGptClient.ValidateId(Jwt(rsa, claims), "client", "nonce", keys, DateTimeOffset.UtcNow)["sub"] == "subject-fixture", "signed identity");
                    Fails(() => ChatGptClient.ValidateId(Jwt(other, claims), "client", "nonce", keys, DateTimeOffset.UtcNow));
                    foreach (var change in new[] { "iss", "aud", "nonce", "exp", "sub" })
                    {
                        var bad = (JObject)claims.DeepClone(); bad[change] = change == "exp" ? (JToken)1 : change == "sub" ? "" : "wrong";
                        Fails(() => ChatGptClient.ValidateId(Jwt(rsa, bad), "client", "nonce", keys, DateTimeOffset.UtcNow));
                    }
                    Fails(() => ChatGptClient.ValidateId(Jwt(rsa, claims, "none"), "client", "nonce", keys, DateTimeOffset.UtcNow));
                }
            });
            test("ChatGPT vault is encrypted atomic and preserves host and separate accounts", () =>
            {
                var vault = NewVault(root); Put(vault, Account(), Account("other-client"));
                string host = vault.ReadAsync(CancellationToken.None).GetAwaiter().GetResult().HostId;
                Assert(!Encoding.UTF8.GetString(File.ReadAllBytes(vault.FilePath)).Contains("private-access-fixture"), "no plaintext credentials");
                using (var lease = vault.LockAsync(CancellationToken.None).GetAwaiter().GetResult()) { lease.Data.Accounts[0].AccessToken = "changed-token"; lease.Save(); }
                var restored = new ChatGptVault(vault.FilePath).ReadAsync(CancellationToken.None).GetAwaiter().GetResult();
                Assert(restored.HostId == host && restored.Accounts.Count == 2 && restored.Accounts[1].AccessToken == "private-access-fixture", "stable host and separation");
                Assert(!File.Exists(vault.FilePath + ".bak"), "no stale token backup");
            });
            test("ChatGPT loopback login exchanges issued ID and saves only verified identity", () =>
                Task.Run(() => LoopbackLoginAsync(root)).GetAwaiter().GetResult());
            test("ChatGPT returning login reuses registration and rejects another identity", () =>
                Task.Run(() => LoopbackLoginAsync(root, true, true)).GetAwaiter().GetResult());
            test("ChatGPT login can be cancelled without browser or account replacement", () =>
            {
                var vault = NewVault(root); Put(vault, Account());
                using (var cancel = new CancellationTokenSource())
                using (var client = new ChatGptClient(vault, new Handler { Reply = request => throw new Exception("Unexpected network") }))
                {
                    bool cancelled = false;
                    try { client.SignInAsync(null, cancel.Token, url => cancel.Cancel()).GetAwaiter().GetResult(); }
                    catch (OperationCanceledException) { cancelled = true; }
                    Assert(cancelled && vault.ReadAsync(CancellationToken.None).GetAwaiter().GetResult().Accounts.Single().ClientId == "oaiapp_fixture", "existing account retained");
                }
            });
            test("ChatGPT rotating refresh is serialized across instances", () =>
            {
                var vault = NewVault(root); var expired = Account(); expired.ExpiresUtc = DateTime.UtcNow.AddMinutes(-1); Put(vault, expired, Account("other-client"));
                int refreshes = 0;
                Func<HttpRequestMessage, Task<HttpResponseMessage>> respond = async request =>
                {
                    Assert(request.RequestUri.AbsoluteUri == ChatGptClient.TokenEndpoint, "public token endpoint");
                    var form = HttpUtility.ParseQueryString(await request.Content.ReadAsStringAsync());
                    Assert(form["grant_type"] == "refresh_token" && form["client_id"] == expired.ClientId && form["resource"] == ChatGptClient.Resource && form["scope"] == null, "refresh contract");
                    Assert(form["refresh_token"] == expired.RefreshToken, "latest refresh token");
                    Interlocked.Increment(ref refreshes); await Task.Delay(30);
                    return Json(new JObject { ["access_token"] = "rotated-access", ["refresh_token"] = "rotated-refresh", ["expires_in"] = 3600, ["token_type"] = "Bearer" });
                };
                using (var one = new ChatGptClient(vault, new Handler { Reply = respond }))
                using (var two = new ChatGptClient(new ChatGptVault(vault.FilePath), new Handler { Reply = respond }))
                {
                    var results = Task.WhenAll(one.GetAccessTokenAsync(expired.ClientId, CancellationToken.None), two.GetAccessTokenAsync(expired.ClientId, CancellationToken.None)).GetAwaiter().GetResult();
                    Assert(results.All(s => s == "rotated-access") && refreshes == 1, "one serialized refresh");
                    var data = vault.ReadAsync(CancellationToken.None).GetAwaiter().GetResult();
                    Assert(data.Accounts.Single(a => a.ClientId == expired.ClientId).RefreshToken == "rotated-refresh", "rotation committed");
                    Assert(data.Accounts.Single(a => a.ClientId == "other-client").RefreshToken == "private-refresh-fixture", "other account retained");
                }
            });
            test("ChatGPT denied plan grant cannot reach inference or API fallback", () =>
            {
                var vault = NewVault(root); var account = Account(); account.Scopes = new[] { "openid", "email" }; Put(vault, account);
                var handler = new Handler { Reply = request => throw new Exception("Unexpected inference") };
                using (var auth = new ChatGptClient(vault, handler)) Fails(() => auth.GetAccessTokenAsync(account.ClientId, CancellationToken.None).GetAwaiter().GetResult());
                Assert(handler.Calls == 0, "no call without grant");
                var requests = new Handler { Reply = request => throw new Exception("Unexpected inference") };
                using (var translation = new OpenAiTranslation(PlanSettings(), requests, new Tokens { Fail = true }))
                    Fails(() => translation.TranslateAsync("Sun {0}", "en", "de", CancellationToken.None).GetAwaiter().GetResult());
                Assert(requests.Calls == 0, "API key is not used as fallback");
            });
            test("ChatGPT model catalog uses OAuth and filters server ordering", () =>
            {
                var vault = NewVault(root); Put(vault, Account());
                using (var client = new ChatGptClient(vault, new Handler { Reply = request =>
                {
                    Assert(request.RequestUri.AbsoluteUri == ChatGptClient.Resource + "/models" && request.Headers.Authorization.Parameter == "private-access-fixture", "OAuth model request");
                    return Async(Json(new JObject { ["models"] = new JArray(
                        new JObject { ["slug"] = "first", ["display_name"] = "First", ["visibility"] = "list" },
                        new JObject { ["slug"] = "hidden", ["visibility"] = "hide" },
                        new JObject { ["slug"] = "second", ["visibility"] = "list" }) }));
                } }))
                { Assert(string.Join(",", client.ModelsAsync("oaiapp_fixture", CancellationToken.None).GetAwaiter().GetResult().Select(m => m.Slug)) == "first,second", "catalog only, no hard-coded models"); }
            });
            test("ChatGPT Responses contract and terminal-only translation", () =>
            {
                var handler = new Handler { Reply = async request =>
                {
                    Assert(request.RequestUri.AbsoluteUri == "https://api.openai.com/v1/responses" && request.Headers.Authorization.Parameter == "oauth-access-fixture", "public OAuth request");
                    var payload = JObject.Parse(await request.Content.ReadAsStringAsync());
                    Assert((bool)payload["store"] == false && (bool)payload["stream"] && payload["input"] is JArray && payload["max_output_tokens"] == null, "plan request contract");
                    Assert((string)payload["model"] == "account-model-fixture", "account-specific model");
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(CompleteStream(), Encoding.UTF8, "text/event-stream") };
                } };
                using (var translation = new OpenAiTranslation(PlanSettings(), handler, new Tokens()))
                {
                    Assert(translation.TranslateAsync("Sun {0}", "en", "de", CancellationToken.None).GetAwaiter().GetResult() == "Sonne {0}", "completed translation");
                    Assert(translation.TranslateAsync("Sun {0}", "en", "fr", CancellationToken.None).GetAwaiter().GetResult() == "Soleil {0}", "same operation cache");
                    Assert(handler.Calls == 1, "single three-language response");
                }
            });
            test("ChatGPT rejects truncated incomplete failed and quota streams", () =>
            {
                foreach (string stream in new[] { "data: {\"type\":\"response.output_text.delta\",\"delta\":\"partial\"}\n\n",
                    Event(new JObject { ["type"] = "response.incomplete" }),
                    Event(new JObject { ["type"] = "response.failed", ["response"] = new JObject { ["error"] = new JObject { ["code"] = "subscription_sharing_usage_limit_exceeded" } } }) + CompleteStream(),
                    "data: [DONE]\n\n", "data: invalid\n\n" })
                { using (var input = new MemoryStream(Encoding.UTF8.GetBytes(stream))) Fails(() => ChatGptStream.ReadAsync(input, CancellationToken.None).GetAwaiter().GetResult()); }
            });
            test("ChatGPT SSE multiline and cancellation", () =>
            {
                string stream = string.Join("\n", new JObject { ["type"] = "response.completed", ["response"] = Completed() }.ToString().Split('\n').Select(s => "data: " + s)) + "\n\n";
                using (var input = new MemoryStream(Encoding.UTF8.GetBytes(stream))) Assert((string)ChatGptStream.ReadAsync(input, CancellationToken.None).GetAwaiter().GetResult()["status"] == "completed", "multiline SSE");
                using (var cancel = new CancellationTokenSource())
                using (var input = new MemoryStream(Encoding.UTF8.GetBytes(CompleteStream())))
                { cancel.Cancel(); bool stopped = false; try { ChatGptStream.ReadAsync(input, cancel.Token).GetAwaiter().GetResult(); } catch (OperationCanceledException) { stopped = true; } Assert(stopped, "SSE cancellation"); }
            });
            test("ChatGPT SSE cancellation interrupts an active blocked read", () =>
            {
                using (var input = new BlockedStream())
                using (var cancel = new CancellationTokenSource())
                {
                    var operation = ChatGptStream.ReadAsync(input, cancel.Token);
                    cancel.Cancel(); bool cancelled = false;
                    try { operation.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                    Assert(cancelled, "active read cancelled");
                }
            });
            test("ChatGPT sign-out revokes chosen session and retains other accounts", () =>
            {
                var vault = NewVault(root); Put(vault, Account(), Account("other-client"));
                var handler = new Handler { Reply = async request =>
                {
                    if (request.RequestUri.AbsolutePath.Contains("openid-configuration")) return Json(new JObject { ["issuer"] = ChatGptClient.Issuer, ["revocation_endpoint"] = ChatGptClient.Issuer + "/api/accounts/oauth/revoke" });
                    var form = HttpUtility.ParseQueryString(await request.Content.ReadAsStringAsync());
                    Assert(form["token_type_hint"] == "refresh_token" && form["client_id"] == "oaiapp_fixture", "revoke exact registration");
                    return new HttpResponseMessage(HttpStatusCode.OK);
                } };
                using (var client = new ChatGptClient(vault, handler)) Assert(client.SignOutAsync("oaiapp_fixture", CancellationToken.None).GetAwaiter().GetResult(), "revocation confirmed");
                var data = vault.ReadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var signedOut = data.Accounts.Single(a => a.ClientId == "oaiapp_fixture");
                Assert(!signedOut.SignedIn && signedOut.IdToken == null && signedOut.Subject == "subject-fixture", "credentials cleared, mapping retained");
                Assert(data.Accounts.Single(a => a.ClientId == "other-client").SignedIn, "other registration unchanged");
            });
            test("ChatGPT selection preserves existing encrypted API key", () =>
            {
                var settings = PlanSettings(); string before = settings.ProtectedKey; string path = Path.Combine(root, "openai.json");
                settings.Save(null, path);
                var loaded = JsonConvert.DeserializeObject<OpenAiSettings>(File.ReadAllText(path));
                Assert(loaded.UseChatGpt && loaded.ProtectedKey == before && loaded.GetKey() == "api-must-not-be-used", "encrypted API key retained");
                Assert(!File.ReadAllText(path).Contains("api-must-not-be-used"), "API plaintext absent");
                loaded.UseChatGpt = false; loaded.Save(null, path);
                Assert(loaded.ProtectedKey == before, "switching back retains key");
            });
            test("ChatGPT local sign-out reports unconfirmed remote revocation", () =>
            {
                var vault = NewVault(root); Put(vault, Account());
                var handler = new Handler { Reply = request => Async(request.Method == HttpMethod.Get ?
                    Json(new JObject { ["issuer"] = ChatGptClient.Issuer, ["revocation_endpoint"] = ChatGptClient.Issuer + "/api/accounts/oauth/revoke" }) :
                    new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)) };
                using (var client = new ChatGptClient(vault, handler)) Assert(!client.SignOutAsync("oaiapp_fixture", CancellationToken.None).GetAwaiter().GetResult(), "remote revocation not falsely confirmed");
                Assert(!vault.ReadAsync(CancellationToken.None).GetAwaiter().GetResult().Accounts.Single().SignedIn && handler.Calls == 3, "local credentials cleared after bounded attempts");
            });
            test("ChatGPT diagnostics preserve status and identifiers without response secrets", () =>
            {
                using (var response = Json(new JObject { ["error"] = new JObject {
                    ["code"] = "subscription_sharing_route_not_supported", ["param"] = "model", ["message"] = "SECRET-FIXTURE" } }, HttpStatusCode.Forbidden))
                {
                    response.Headers.Add("x-request-id", "req_fixture123");
                    string message = OpenAiErrors.FromHttpAsync(response, true).GetAwaiter().GetResult().Message;
                    Assert(message.Contains("HTTP 403") && message.Contains("code=subscription_sharing_route_not_supported") && message.Contains("param=model") && message.Contains("request-id=req_fixture123") && !message.Contains("SECRET-FIXTURE"), "safe identifiers and status");
                }
                using (var response = Json(new JObject { ["detail"] = "Bearer SECRET-FIXTURE" }, HttpStatusCode.ServiceUnavailable))
                {
                    string message = OpenAiErrors.FromHttpAsync(response, true).GetAwaiter().GetResult().Message;
                    Assert(message.Contains("HTTP 503") && message.Contains("shape=detail") && !message.Contains("SECRET-FIXTURE"), "admission body shape only");
                }
                using (var response = Json(new JObject { ["error"] = new JObject { ["code"] = "sk-SECRET-FIXTURE", ["param"] = "SECRET-FIXTURE" } }, HttpStatusCode.BadRequest))
                {
                    response.Headers.Add("x-request-id", "Bearer SECRET-FIXTURE");
                    string message = OpenAiErrors.FromHttpAsync(response, true).GetAwaiter().GetResult().Message;
                    Assert(message.Contains("code=-") && message.Contains("param=-") && message.Contains("request-id=-") && !message.Contains("SECRET-FIXTURE"), "reject untrusted identifiers");
                }
                Assert(!OpenAiErrors.SafeMessage(new HttpRequestException("https://secret/SECRET-FIXTURE")).Contains("SECRET-FIXTURE"), "network message is sanitized");
            });
            test("ChatGPT malformed model catalogs fail explicitly", () =>
            {
                var vault = NewVault(root); Put(vault, Account());
                foreach (var body in new[] { new JObject { ["data"] = new JArray() }, new JObject { ["models"] = "invalid" } })
                using (var client = new ChatGptClient(vault, new Handler { Reply = request => Task.FromResult(Json(body)) }))
                {
                    try { client.ModelsAsync("oaiapp_fixture", CancellationToken.None).GetAwaiter().GetResult(); throw new Exception("catalog should fail"); }
                    catch (InvalidOperationException error) { Assert(error.Message == Ui.Text("cgCatalogFormat"), "explicit model format error"); }
                }
            });
            test("OpenAI 429 messages distinguish quota and throttle without exposing body", () =>
            {
                foreach (string code in new[] { "insufficient_quota", "credit_balance_exhausted", "rate_limit_exceeded", "subscription_sharing_usage_limit_exceeded" })
                using (var response = Json(new JObject { ["error"] = new JObject { ["code"] = code, ["message"] = "SECRET-FIXTURE-DO-NOT-EXPOSE" } }, (HttpStatusCode)429))
                {
                    var error = OpenAiErrors.FromHttpAsync(response, code.StartsWith("subscription")).GetAwaiter().GetResult();
                    Assert(!error.Message.Contains("SECRET-FIXTURE") && error.Message.Length > 20, "sanitized actionable message");
                }
            });
        }
        finally { Directory.Delete(root, true); }
    }

    private static async Task LoopbackLoginAsync(string root, bool returning = false, bool wrongSubject = false)
    {
        var vault = NewVault(root);
        if (returning) Put(vault, Account());
        string nonce = null, redirect = null, verifierChallenge = null;
        using (var rsa = new RSACryptoServiceProvider(2048) { PersistKeyInCsp = false })
        using (var local = new HttpClient(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false }))
        {
            var handler = new Handler { Reply = async request =>
            {
                if (request.RequestUri.AbsolutePath.EndsWith("jwks.json")) return Json(Jwks(rsa));
                var form = HttpUtility.ParseQueryString(await request.Content.ReadAsStringAsync());
                Assert(form["client_id"] == "oaiapp_fixture" && form["client_id"] != "dynamic_agent_client", "issued ID in exchange");
                Assert(form["redirect_uri"] == redirect && form["resource"] == ChatGptClient.Resource && form["client_secret"] == null, "exact callback, no client secret");
                using (var sha = SHA256.Create()) Assert(ChatGptClient.Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(form["code_verifier"]))) == verifierChallenge, "PKCE exchange binding");
                var claims = Claims("oaiapp_fixture", nonce);
                if (wrongSubject) claims["sub"] = "different-account-subject";
                return Json(Token(Jwt(rsa, claims)));
            } };
            Task callback = null;
            using (var client = new ChatGptClient(vault, handler))
            {
                ChatGptAccount account;
                try { account = await client.SignInAsync(returning ? "oaiapp_fixture" : null, CancellationToken.None, url =>
                {
                    var query = HttpUtility.ParseQueryString(new Uri(url).Query); nonce = query["nonce"]; redirect = query["redirect_uri"]; verifierChallenge = query["code_challenge"];
                    Assert(query["client_id"] == (returning ? "oaiapp_fixture" : "dynamic_agent_client"), "correct registration entrypoint");
                    callback = Task.Run(async () =>
                    {
                        using (var wrong = await local.GetAsync(redirect + "?state=wrong&code=ignored&client_id=oaiapp_fixture")) Assert(wrong.StatusCode == HttpStatusCode.BadRequest, "ignore unrelated callback");
                        using (var good = await local.GetAsync(redirect + "?state=" + query["state"] + "&code=code-fixture&client_id=oaiapp_fixture")) Assert(good.IsSuccessStatusCode, "loopback callback received");
                    });
                }); }
                catch (InvalidOperationException) when (wrongSubject)
                {
                    await callback;
                    var retained = await vault.ReadAsync(CancellationToken.None);
                    Assert(retained.Accounts.Single().AccessToken == "private-access-fixture" && retained.Accounts.Single().Subject == "subject-fixture", "existing identity not replaced");
                    return;
                }
                Assert(!wrongSubject, "wrong account must be rejected");
                await callback;
                Assert(account.Subject == "subject-fixture" && account.PlanEnabled && handler.Calls == 2, "verified identity and consent saved");
                var data = await vault.ReadAsync(CancellationToken.None);
                Assert(data.Accounts.Single().ClientId == "oaiapp_fixture" && data.HostId.StartsWith("urn:uuid:"), "registered session stored");
            }
        }
    }
}
