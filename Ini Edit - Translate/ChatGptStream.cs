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
        // No partial result is applied. Accept only a terminal SSE completion or
        // a complete Responses JSON envelope, regardless of a misleading MIME header.
        internal static async Task<JObject> ReadAsync(Stream stream, CancellationToken cancel)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
            using (cancel.Register(() => stream.Dispose()))
            {
                var line = new StringBuilder(); var data = new StringBuilder();
                var jsonBody = new StringBuilder(); bool? jsonMode = null;
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
                        if (!jsonMode.HasValue)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                if (char.IsWhiteSpace(buffer[i])) continue;
                                if (buffer[i] == '<') throw FormatError("html");
                                if (buffer[i] != '{' && buffer[i] != 'd' && buffer[i] != 'e' && buffer[i] != 'i' && buffer[i] != 'r' && buffer[i] != ':') throw FormatError("unknown-text");
                                jsonMode = buffer[i] == '{'; break;
                            }
                        }
                        if (jsonMode == true) { jsonBody.Append(buffer, 0, count); continue; }
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
                    if (jsonMode == true)
                    {
                        JObject response;
                        try { response = JObject.Parse(jsonBody.ToString()); }
                        catch (Newtonsoft.Json.JsonException) { throw FormatError("invalid-json"); }
                        var error = response["error"] as JObject;
                        if (error != null)
                        {
                            string code = error["code"]?.Type == JTokenType.String ? (string)error["code"] : null;
                            throw new InvalidOperationException(OpenAiErrors.FromCode(code, true).Message +
                                Environment.NewLine + "JSON; shape=error-object; code=" + OpenAiErrors.SafeCode(code));
                        }
                        if (response["detail"] != null) throw FormatError("detail");
                        if ((string)response["object"] != "response" || !(response["output"] is JArray)) throw FormatError("json-object");
                        if ((string)response["status"] != "completed") throw new InvalidOperationException(Ui.Text("incompleteResponse"));
                        return response;
                    }
                }
                catch (Exception) when (cancel.IsCancellationRequested) { throw new OperationCanceledException(cancel); }
                throw new InvalidOperationException(Ui.Text("incompleteResponse"));
            }
        }

        private static Exception FormatError(string shape) => new InvalidOperationException(Ui.Text("cgStreamFormat") + "; shape=" + shape);
    }
}
