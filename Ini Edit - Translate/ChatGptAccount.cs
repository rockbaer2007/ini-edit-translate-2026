using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Ini_Edit___Translate
{
    internal sealed class ChatGptAccount
    {
        public string ClientId { get; set; }
        public string Subject { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string IdToken { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public string[] Scopes { get; set; } = new string[0];
        [JsonIgnore] public bool PlanEnabled => Scopes.Contains("chatgpt.tokens.use.direct") && Scopes.Contains("resource.invoke");
        [JsonIgnore] public bool SignedIn => !string.IsNullOrEmpty(RefreshToken) || !string.IsNullOrEmpty(AccessToken);
        public override string ToString() => (string.IsNullOrEmpty(Email) ? Ui.Text("cgAccount") : Email) +
            " · " + (ClientId ?? "").Substring(Math.Max(0, (ClientId ?? "").Length - 8)) +
            (SignedIn ? (PlanEnabled ? "" : " · " + Ui.Text("cgNoConsent")) : " · " + Ui.Text("cgSignedOut"));
    }

    internal sealed class ChatGptData
    {
        public string HostId { get; set; } = "urn:uuid:" + Guid.NewGuid().ToString();
        public List<ChatGptAccount> Accounts { get; set; } = new List<ChatGptAccount>();
    }

    // Separate from openai.json: signing in/out never removes an API key.
    internal sealed class ChatGptVault
    {
        internal static readonly string DefaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UGSo", "IET2026", "chatgpt-session.dat");
        internal readonly string FilePath;
        internal ChatGptVault(string filePath = null) { FilePath = filePath ?? DefaultPath; }

        internal async Task<Lease> LockAsync(CancellationToken cancel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            // A file lease works across processes and async continuations; no rotating-token races.
            for (int attempt = 0; attempt < 240; attempt++)
            {
                cancel.ThrowIfCancellationRequested();
                FileStream held = null;
                try { held = new FileStream(FilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
                catch (IOException) { }
                if (held != null)
                {
                    try
                    {
                        var data = File.Exists(FilePath)
                            ? JsonConvert.DeserializeObject<ChatGptData>(Encoding.UTF8.GetString(ProtectedData.Unprotect(
                                File.ReadAllBytes(FilePath), null, DataProtectionScope.CurrentUser)))
                            : new ChatGptData();
                        if (data == null || data.Accounts == null || !Uri.IsWellFormedUriString(data.HostId, UriKind.Absolute))
                            throw new InvalidDataException();
                        return new Lease(this, held, data);
                    }
                    catch { held.Dispose(); throw new InvalidOperationException(Ui.Text("cgStorageError")); }
                }
                await Task.Delay(250, cancel).ConfigureAwait(false);
            }
            throw new InvalidOperationException(Ui.Text("cgSessionBusy"));
        }

        internal async Task<ChatGptData> ReadAsync(CancellationToken cancel)
        {
            using (var lease = await LockAsync(cancel).ConfigureAwait(false)) return lease.Data;
        }

        internal sealed class Lease : IDisposable
        {
            private readonly ChatGptVault vault;
            private readonly FileStream held;
            internal readonly ChatGptData Data;
            internal Lease(ChatGptVault vault, FileStream held, ChatGptData data) { this.vault = vault; this.held = held; Data = data; }
            internal void Save()
            {
                string temporary = vault.FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    byte[] bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Data)), null, DataProtectionScope.CurrentUser);
                    using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                    if (File.Exists(vault.FilePath)) File.Replace(temporary, vault.FilePath, null);
                    else File.Move(temporary, vault.FilePath);
                }
                catch { throw new InvalidOperationException(Ui.Text("cgStorageError")); }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
            public void Dispose() { held.Dispose(); }
        }
    }
}
