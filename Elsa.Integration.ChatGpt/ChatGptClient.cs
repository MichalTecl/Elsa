using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Integration.ChatGpt.Model;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Elsa.Integration.ChatGpt
{
    public class ChatGptClient : IChatGptClient
    {
        private readonly ILog _log;
        private readonly ChatGptClientConfig _config;
        private static readonly object s_aiLogLock = new object();
        private static readonly Encoding s_logEncoding = new UTF8Encoding(false);
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 5000;
        private const string OpenAiUrl = "https://api.openai.com/v1/chat/completions";

        public ChatGptClient(ILog log, ChatGptClientConfig config)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(_config.ApiKey))
                throw new ArgumentException("API Key is missing in configuration.");
        }

        public OpenAiResponseBody Request(OpenAiRequestBody body)
        {
            int attempt = 0;
            var jsonRequestBody = JsonConvert.SerializeObject(body);

            while (attempt < MaxRetries)
            {
                try
                {
                    attempt++;
                    _log.Info($"Attempt {attempt} - Sending request to OpenAI API.");

                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

                        var httpResponse = httpClient.PostAsync(
                            OpenAiUrl,
                            new StringContent(jsonRequestBody, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();

                        string responseBody = null;

                        try
                        {
                            responseBody = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        }
                        catch (Exception)
                        {
                            _log.Error("Cannot read http response");
                        }

                        _log.SaveRequestProtocol("POST", OpenAiUrl, jsonRequestBody, responseBody);
                        SaveAiLog(jsonRequestBody, responseBody);

                        if (httpResponse.IsSuccessStatusCode)
                        {
                            var openAiResponse = JsonConvert.DeserializeObject<OpenAiResponseBody>(responseBody);
                            var firstChoice = openAiResponse?.Choices?[0];
                            var content = firstChoice?.Message?.Content;

                            if (string.Equals(firstChoice?.FinishReason, "content_filter", StringComparison.OrdinalIgnoreCase))
                            {
                                _log.Info("Response received from OpenAI API, but content was blocked by content_filter.");
                                return openAiResponse;
                            }

                            if (string.IsNullOrWhiteSpace(content))
                            {
                                var rawBodySnippet = TrimSingleLine(responseBody, 1000);
                                throw new InvalidOperationException(
                                    $"OpenAI returned successful HTTP response, but no message content was found. FinishReason={firstChoice?.FinishReason ?? "null"}. Raw body: {rawBodySnippet}");
                            }

                            _log.Info("Response received from OpenAI API.");
                            return openAiResponse;
                        }

                        if (httpResponse.StatusCode == (System.Net.HttpStatusCode)429)
                        {
                            _log.Error("Received 429 Too Many Requests. Retrying after delay...");
                            Thread.Sleep(RetryDelayMs);
                            continue;
                        }

                        var errorMessage = $"API call failed with status code: {httpResponse.StatusCode} '{responseBody}'";
                        _log.Error(errorMessage);
                        throw new Exception(errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error during request attempt {attempt}.", ex);
                    SaveAiLog(jsonRequestBody, ex.ToString());

                    if (attempt >= MaxRetries)
                    {
                        _log.Error("Max retry attempts reached. Throwing exception.");
                        throw;
                    }
                }
            }

            throw new Exception("Request failed after maximum retry attempts.");
        }

        private void SaveAiLog(string prompt, string result)
        {
            lock (s_aiLogLock)
            {
                var dir = Directory.CreateDirectory(@"C:\Elsa\Log\AI");
                var path = Path.Combine(dir.FullName, BuildAiLogFileName(prompt));

                var sb = new StringBuilder();
                sb.AppendLine("PROMPT:");
                sb.AppendLine(prompt ?? string.Empty);
                sb.AppendLine();
                sb.AppendLine("RESULT:");
                sb.AppendLine(result ?? string.Empty);

                File.WriteAllText(path, sb.ToString(), s_logEncoding);

                DirectorySizeKeeper.KeepSize(dir.FullName, (int)5e+8, (int)2e+8, _log);
            }
        }

        private static string BuildAiLogFileName(string prompt)
        {
            var promptStart = ExtractPromptStart(prompt);
            var fileName = $"{DateTime.Now:yyyyMMdd HHmmss} {promptStart} {Guid.NewGuid():N}.txt";
            return StringUtil.SanitizeFileName(fileName, '.');
        }

        private static string ExtractPromptStart(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "empty";

            try
            {
                var request = JsonConvert.DeserializeObject<OpenAiRequestBody>(prompt);
                var userMessage = request?.Messages?.Find(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content;

                if (!string.IsNullOrWhiteSpace(userMessage))
                    return TrimSingleLine(userMessage, 60);
            }
            catch
            {
                // best effort only
            }

            return TrimSingleLine(prompt, 60);
        }

        private static string TrimSingleLine(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "empty";

            text = text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
            return text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }
    }
}
