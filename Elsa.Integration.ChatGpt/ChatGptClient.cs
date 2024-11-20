using Elsa.Common.Logging;
using Elsa.Integration.ChatGpt.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Elsa.Integration.ChatGpt
{
    public class ChatGptClient : IChatGptClient
    {
        private readonly ILog _log;
        private readonly ChatGptClientConfig _config;
        private const int MaxRetries = 3;  // Maximální počet pokusů
        private const int RetryDelayMs = 5000;  // Doba čekání mezi pokusy (v milisekundách)
        private const string OpenAiUrl = "https://api.openai.com/v1/chat/completions";
        

        public ChatGptClient(ILog log, ChatGptClientConfig config)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(_config.ApiKey))
            {
                throw new ArgumentException("API Key is missing in configuration.");
            }
        }

        public OpenAiResponseBody Request(OpenAiRequestBody body)
        {
            int attempt = 0;

            while (attempt < MaxRetries)
            {
                try
                {
                    attempt++;
                    _log.Info($"Attempt {attempt} - Sending request to OpenAI API.");

                    using (var httpClient = new HttpClient())
                    {                        
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
                                             
                        var jsonRequestBody = JsonConvert.SerializeObject(body);
                        
                        var httpResponse = httpClient.PostAsync(OpenAiUrl,
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

                        if (httpResponse.IsSuccessStatusCode)
                        {                                                                                
                            var openAiResponse = JsonConvert.DeserializeObject<OpenAiResponseBody>(responseBody);

                            _log.Info("Response received from OpenAI API.");
                                                        
                            return openAiResponse;
                        }
                        else if (httpResponse.StatusCode == (System.Net.HttpStatusCode)429)  // Too Many Requests
                        {
                            _log.Error("Received 429 Too Many Requests. Retrying after delay...");
                            Thread.Sleep(RetryDelayMs);  
                        }
                        else
                        {
                            string errBody = null;
                            try
                            {
                                errBody = httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            }
                            catch {;}

                            var errorMessage = $"API call failed with status code: {httpResponse.StatusCode} '{errBody}'";
                            _log.Error(errorMessage);

                            throw new Exception(errorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error during request attempt {attempt}.", ex);

                    if (attempt >= MaxRetries)
                    {
                        _log.Error("Max retry attempts reached. Throwing exception.");
                        throw;  
                    }
                }
            }

            throw new Exception("Request failed after maximum retry attempts.");
        }
    }
}
