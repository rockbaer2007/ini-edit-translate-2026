using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Ini_Edit___Translate
{
    internal static class ChatGptStream
    {
        // No partial result is ever applied. Only the terminal completed event is accepted.
        internal static async Task<JObject> ReadAsync(Stream stream, CancellationToken cancel)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
            using (cancel.Register(() => stream.Dispose()))
            {
                var line = new StringBuilder(); var data = new StringBuilder();
                var buffer = new char[4096]; int total = 0;
                try
                {
                    while (true)
                    {
                        cancel.ThrowIfCancellationRequested();
                        int count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                        if (count == 0) break;
                        total += count;
                        if (total > 2 * 1024 * 1024) throw new InvalidOperationException(Ui.Text("cgStreamSize"));
                        for (int i = 0; i < count; i++)
                        {
                            char c = buffer[i];
                            if (c != '\n') { line.Append(c); continue; }
                            string value = line.ToString().TrimEnd('\r'); line.Clear();
                            if (value.StartsWith("data:", StringComparison.Ordinal))
                            { if (data.Length > 0) data.Append('\n'); data.Append(value.Substring(5).TrimStart(' ')); }
                            else if (value.Length == 0 && data.Length > 0)
                            {
                                string json = data.ToString(); data.Clear();
                                if (json == "[DONE]") throw new InvalidOperationException(Ui.Text("incompleteResponse"));
                                JObject item;
                                try { item = JObject.Parse(json); } catch { throw new InvalidOperationException(Ui.Text("cgStreamFormat")); }
                                string type = (string)item["type"];
                                if (type == "response.completed")
                                {
                                    var response = item["response"] as JObject;
                                    if ((string)response?["status"] != "completed") throw new InvalidOperationException(Ui.Text("incompleteResponse"));
                                    return response;
                                }
                                if (type == "response.failed" || type == "error")
                                    throw OpenAiErrors.FromStream((string)item["response"]?["error"]?["code"] ??
                                        (string)item["error"]?["code"] ?? (string)item["code"]);
                                if (type == "response.incomplete") throw new InvalidOperationException(Ui.Text("incompleteResponse"));
                            }
                        }
                    }
                }
                catch (Exception) when (cancel.IsCancellationRequested) { throw new OperationCanceledException(cancel); }
                throw new InvalidOperationException(Ui.Text("incompleteResponse"));
            }
        }
    }
}
