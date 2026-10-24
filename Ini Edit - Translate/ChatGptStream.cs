using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Ini_Edit___Translate
{
    internal static class ChatGptStream
    {
        private sealed class FinishedOutput
        {
            private readonly SortedDictionary<int, JObject> items = new SortedDictionary<int, JObject>();
            private readonly SortedDictionary<int, SortedDictionary<int, JObject>> parts = new SortedDictionary<int, SortedDictionary<int, JObject>>();
            private readonly SortedDictionary<int, SortedDictionary<int, StringBuilder>> textDeltas = new SortedDictionary<int, SortedDictionary<int, StringBuilder>>();
            private int events, deltas, itemDone, textDone;
            private bool refused;

            private static int Index(JToken token)
            {
                int value;
                return token?.Type == JTokenType.Integer && int.TryParse(token.ToString(), out value) && value >= 0 && value <= 1024 ? value : -1;
            }

            internal void Observe(JObject item)
            {
                events++;
                string type = (string)item["type"];
                if (type == "response.output_text.delta") deltas++;
                if (type == "response.refusal.delta" || type == "response.refusal.done") refused = true;
                int output = Index(item["output_index"]);
                if (output < 0) return;
                if (type == "response.output_text.delta" && item["delta"]?.Type == JTokenType.String)
                {
                    int deltaIndex = Index(item["content_index"]);
                    if (deltaIndex >= 0)
                    {
                        if (!textDeltas.TryGetValue(output, out var content)) textDeltas[output] = content = new SortedDictionary<int, StringBuilder>();
                        if (!content.TryGetValue(deltaIndex, out var text)) content[deltaIndex] = text = new StringBuilder();
                        text.Append((string)item["delta"]);
                    }
                }
                if (type == "response.output_item.done" && item["item"] is JObject message &&
                    (string)message["type"] == "message" &&
                    ((string)message["role"] == "assistant" || message["role"] == null))
                {
                    if (message["status"] != null && (string)message["status"] != "completed") throw new InvalidOperationException(Ui.Text("incompleteResponse"));
                    refused |= (message["content"] as JArray ?? new JArray()).OfType<JObject>().Any(p => (string)p["type"] == "refusal");
                    items[output] = (JObject)message.DeepClone(); itemDone++;
                }
                JObject part = null;
                if (type == "response.output_text.done" && item["text"]?.Type == JTokenType.String)
                    part = new JObject { ["type"] = "output_text", ["text"] = (string)item["text"] };
                else if (type == "response.content_part.done" && item["part"] is JObject eventPart &&
                    ((string)eventPart["type"] == "output_text" || (string)eventPart["type"] == "refusal")) part = (JObject)eventPart.DeepClone();
                else if (type == "response.refusal.done") part = new JObject { ["type"] = "refusal" };
                int index = Index(item["content_index"]);
                if (part != null && index >= 0)
                {
                    refused |= (string)part["type"] == "refusal";
                    if (!parts.TryGetValue(output, out var content)) parts[output] = content = new SortedDictionary<int, JObject>();
                    content[index] = part; textDone++;
                }
            }

            private static bool HasResult(JArray output) => output != null && output.OfType<JObject>()
                .Where(item => (string)item["type"] == "message")
                .SelectMany(item => (item["content"] as JArray ?? new JArray()).OfType<JObject>())
                .Any(part => (string)part["type"] == "refusal" || ((string)part["type"] == "output_text" && !string.IsNullOrEmpty((string)part["text"])));

            internal JObject Complete(JObject response)
            {
                if (refused) throw new InvalidOperationException(Ui.Text("refusedResponse"));
                if (HasResult(response["output"] as JArray)) return response;
                // This method is called only after explicit terminal success.
                // Never return these accumulated deltas on EOF, failure or cancellation.
                foreach (var output in textDeltas)
                {
                    if (!parts.TryGetValue(output.Key, out var content)) parts[output.Key] = content = new SortedDictionary<int, JObject>();
                    foreach (var part in output.Value)
                        if (!content.ContainsKey(part.Key)) content[part.Key] = new JObject { ["type"] = "output_text", ["text"] = part.Value.ToString() };
                }
                foreach (var pair in parts)
                    if (!items.ContainsKey(pair.Key) || !HasResult(new JArray(items[pair.Key])))
                    {
                        if (!pair.Value.Keys.SequenceEqual(Enumerable.Range(0, pair.Value.Count))) throw new InvalidOperationException(Ui.Text("incompleteResponse"));
                        items[pair.Key] = new JObject { ["type"] = "message", ["role"] = "assistant", ["status"] = "completed", ["content"] = new JArray(pair.Value.Values.Select(p => p.DeepClone())) };
                    }
                if (items.Count > 0)
                {
                    var output = new JArray((response["output"] as JArray ?? new JArray()).OfType<JObject>()
                        .Where(item => (string)item["type"] != "message").Select(item => item.DeepClone()));
                    foreach (var item in items.Values) output.Add(item.DeepClone());
                    response["output"] = output;
                }
                return response;
            }

            internal string Trace(JObject response, string transport)
            {
                var output = response["output"] as JArray ?? new JArray();
                return "transport=" + transport + "; events=" + events + "; deltas=" + deltas + "; item-done=" + itemDone +
                    "; text-done=" + textDone + "; output-items=" + output.Count + "; messages=" + output.OfType<JObject>().Count(item => (string)item["type"] == "message");
            }
        }
        // No partial result is applied. Accept only a terminal SSE completion or
        // a complete Responses JSON envelope, regardless of a misleading MIME header.
        internal static async Task<JObject> ReadAsync(Stream stream, CancellationToken cancel, Action<string> diagnostics = null)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true))
            using (cancel.Register(() => stream.Dispose()))
            {
                var line = new StringBuilder(); var data = new StringBuilder();
                var jsonBody = new StringBuilder(); bool? jsonMode = null;
                var finished = new FinishedOutput();
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
                                finished.Observe(item);
                                if (type == "response.completed")
                                {
                                    var response = item["response"] as JObject;
                                    if ((string)response?["status"] != "completed") throw new InvalidOperationException(Ui.Text("incompleteResponse"));
                                    response = finished.Complete(response);
                                    diagnostics?.Invoke(finished.Trace(response, "sse"));
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
                        diagnostics?.Invoke(finished.Trace(response, "json"));
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
