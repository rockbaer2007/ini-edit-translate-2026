using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Iet.Update;
using Newtonsoft.Json.Linq;

namespace Ini_Edit___Translate
{
    internal enum UpdatePhase { Downloading, Validating, Ready }

    internal sealed class InlineProgress<T> : IProgress<T>
    {
        private readonly Action<T> report;
        internal InlineProgress(Action<T> report) { this.report = report; }
        public void Report(T value) { report(value); }
    }

    internal sealed class GithubRelease
    {
        internal Version Version;
        internal string Notes, ZipName, ZipUrl, ChecksumsUrl, Digest;
    }

    internal sealed class GithubUpdateClient : IDisposable
    {
        internal const string Repository = "rockbaer2007/ini-edit-translate-2026";
        private readonly HttpClient http;
        internal GithubUpdateClient(HttpMessageHandler handler = null)
        {
            http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false });
            http.Timeout = TimeSpan.FromMinutes(3);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("INI-Edit-Translate-2026");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }

        internal static bool Trusted(Uri uri)
        {
            return uri.Scheme == "https" && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo) &&
                new[] { "api.github.com", "github.com", "release-assets.githubusercontent.com", "objects.githubusercontent.com" }
                .Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<HttpResponseMessage> Get(Uri uri, CancellationToken cancel)
        {
            for (int redirect = 0; redirect < 6; redirect++)
            {
                if (!Trusted(uri)) throw new InvalidDataException("Untrusted update URL.");
                var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancel);
                if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                {
                    var location = response.Headers.Location;
                    response.Dispose();
                    if (location == null) throw new InvalidDataException("Invalid redirect.");
                    uri = new Uri(uri, location);
                    continue;
                }
                if (!response.IsSuccessStatusCode)
                {
                    try { response.EnsureSuccessStatusCode(); }
                    finally { response.Dispose(); }
                }
                return response;
            }
            throw new InvalidDataException("Too many redirects.");
        }

        private async Task<byte[]> Read(Uri uri, int maximum, CancellationToken cancel, IProgress<int> progress = null)
        {
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
            deadline.CancelAfter(TimeSpan.FromMinutes(3));
            cancel = deadline.Token;
            using (var response = await Get(uri, cancel))
            {
                long? length = response.Content.Headers.ContentLength;
                if (length > maximum) throw new InvalidDataException("Download too large.");
                using (var source = await response.Content.ReadAsStreamAsync())
                using (var result = new MemoryStream())
                {
                    byte[] buffer = new byte[81920];
                    int count;
                    while ((count = await source.ReadAsync(buffer, 0, buffer.Length, cancel)) != 0)
                    {
                        if (result.Length + count > maximum) throw new InvalidDataException("Download too large.");
                        await result.WriteAsync(buffer, 0, count, cancel);
                        if (length > 0) progress?.Report((int)Math.Min(100, 100 * result.Length / length.Value));
                    }
                    if (length.HasValue && result.Length != length.Value) throw new InvalidDataException("Incomplete download.");
                    return result.ToArray();
                }
            }
            }
        }

        internal async Task<GithubRelease> Latest(CancellationToken cancel)
        {
            byte[] bytes = await Read(new Uri("https://api.github.com/repos/" + Repository + "/releases/latest"), 2 * 1024 * 1024, cancel);
            var data = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            string tag = (string)data["tag_name"];
            Version version;
            if ((bool?)data["draft"] == true || (bool?)data["prerelease"] == true ||
                tag == null || !Regex.IsMatch(tag, @"^v\d+\.\d+\.\d+(\.\d+)?$") ||
                !Version.TryParse(tag.Substring(1), out version)) throw new InvalidDataException("Unsupported release.");
            string name = "INI-Edit-Translate-2026-" + tag + "-windows.zip";
            var assets = data["assets"] as JArray;
            if (assets == null) throw new InvalidDataException("Release assets missing.");
            var zip = assets.Children<JObject>().Where(asset => (string)asset["name"] == name).ToArray();
            var checksums = assets.Children<JObject>().Where(asset => (string)asset["name"] == "SHA256SUMS.txt").ToArray();
            if (zip.Length != 1 || checksums.Length != 1) throw new InvalidDataException("Release files missing or ambiguous.");
            string prefix = "https://github.com/" + Repository + "/releases/download/" + tag + "/";
            if ((string)zip[0]["browser_download_url"] != prefix + name ||
                (string)checksums[0]["browser_download_url"] != prefix + "SHA256SUMS.txt")
                throw new InvalidDataException("Unexpected release download.");
            return new GithubRelease
            {
                Version = new Version(UpdateCore.Normalize(version.ToString())), Notes = ((string)data["body"] ?? "").Trim(),
                ZipName = name, ZipUrl = prefix + name, ChecksumsUrl = prefix + "SHA256SUMS.txt",
                Digest = (string)zip[0]["digest"]
            };
        }

        internal async Task<UpdateJob> Download(GithubRelease release, string directory, CancellationToken cancel, IProgress<int> progress, IProgress<UpdatePhase> phase = null)
        {
            phase?.Report(UpdatePhase.Downloading);
            byte[] checksumBytes = await Read(new Uri(release.ChecksumsUrl), 128 * 1024, cancel);
            string[] matches = System.Text.Encoding.UTF8.GetString(checksumBytes).Split('\n')
                .Select(line => Regex.Match(line.Trim().TrimStart('\ufeff'), @"^([a-fA-F0-9]{64})\s+\*?(.+)$"))
                .Where(match => match.Success && match.Groups[2].Value == release.ZipName)
                .Select(match => match.Groups[1].Value.ToLowerInvariant()).ToArray();
            if (matches.Length != 1) throw new InvalidDataException("Checksum missing or ambiguous.");
            string digest = release.Digest;
            if (!string.IsNullOrWhiteSpace(digest) && digest != "sha256:" + matches[0])
                throw new InvalidDataException("GitHub digest differs.");
            byte[] zip = await Read(new Uri(release.ZipUrl), 128 * 1024 * 1024, cancel,
                new InlineProgress<int>(value => progress?.Report(Math.Max(0, Math.Min(90, value * 90 / 100))))).ConfigureAwait(false);
            cancel.ThrowIfCancellationRequested();
            phase?.Report(UpdatePhase.Validating);
            progress?.Report(92);
            string zipPath = Path.Combine(directory, release.ZipName);
            var job = new UpdateJob { ZipPath = zipPath, Sha256 = matches[0], Version = release.Version.ToString() };
            // Reject corrupt or unexpected packages while the current application is still open.
            await Task.Run(() => {
                cancel.ThrowIfCancellationRequested();
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(zipPath, zip);
                UpdateCore.Extract(job, Path.Combine(directory, "validation"), cancel);
            }, cancel).ConfigureAwait(false);
            cancel.ThrowIfCancellationRequested();
            progress?.Report(100);
            phase?.Report(UpdatePhase.Ready);
            return job;
        }

        public void Dispose() { http.Dispose(); }
    }
}
