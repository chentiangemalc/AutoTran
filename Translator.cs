using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AutoTran
{
   

    public class Translator
    {
        Hashtable languageCache = new Hashtable();
        Hashtable languageCodesAndTitles = new Hashtable();
        string[] languageCodes;
        private string DetectLanguage(string text)
        {
            string detectUri = string.Format(Properties.Settings.Default.CognitiveServicesEndpoint, "detect");

            // Create request to Detect languages with Translator
            HttpWebRequest detectLanguageWebRequest = (HttpWebRequest)WebRequest.Create(detectUri);
            detectLanguageWebRequest.Headers.Add("Ocp-Apim-Subscription-Key", Properties.Settings.Default.CognitiveServicesApiKey);
            detectLanguageWebRequest.Headers.Add("Ocp-Apim-Subscription-Region", Properties.Settings.Default.CognitiveServicesRegion);
            detectLanguageWebRequest.ContentType = "application/json; charset=utf-8";
            detectLanguageWebRequest.Method = "POST";

            // Send request
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string jsonText = serializer.Serialize(text);

            string body = "[{ \"Text\": " + jsonText + " }]";
            byte[] data = Encoding.UTF8.GetBytes(body);

            detectLanguageWebRequest.ContentLength = data.Length;

            using (var requestStream = detectLanguageWebRequest.GetRequestStream())
                requestStream.Write(data, 0, data.Length);

            HttpWebResponse response = (HttpWebResponse)detectLanguageWebRequest.GetResponse();

            // Read and parse JSON response
            var responseStream = response.GetResponseStream();
            var jsonString = new StreamReader(responseStream, Encoding.GetEncoding("utf-8")).ReadToEnd();
            dynamic jsonResponse = serializer.DeserializeObject(jsonString);

            // Fish out the detected language code
            var languageInfo = jsonResponse[0];
            if (languageInfo["score"] > (decimal)0.5)
            {
                return languageInfo["language"];
            }
            else
                return "Unknown";
        }
        private void GetLanguagesForTranslate()
        {
            // Send request to get supported language codes
            string uri = String.Format(Properties.Settings.Default.CognitiveServicesEndpoint, "languages") + "&scope=translation";
            WebRequest WebRequest = WebRequest.Create(uri);
            WebRequest.Headers.Add("Accept-Language", "en");
            WebResponse response = null;
            // Read and parse the JSON response
            response = WebRequest.GetResponse();
            using (var reader = new StreamReader(response.GetResponseStream(), UnicodeEncoding.UTF8))
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(reader.ReadToEnd());
                var languages = result["translation"];

                 languageCodes = languages.Keys.ToArray();
                foreach (var kv in languages)
                {
                    languageCodesAndTitles.Add(kv.Value["name"], kv.Key);
                    if (!languageCodesAndTitles.ContainsKey(kv.Value["nativeName"]))
                    {
                        languageCodesAndTitles.Add(kv.Value["nativeName"], kv.Key);
                    }
                }
            }
        }

        public async Task<string> Translate(string text, string sourceLanguage, string targetLanguage)
        {
            if (languageCache.ContainsKey(text))
            {
                return languageCache[text].ToString();
            }
            string textToTranslate = text;

            string fromLanguage = sourceLanguage;
            string fromLanguageCode;

            // auto-detect source language if requested
            if (fromLanguage == "Detect")
            {
                fromLanguageCode = DetectLanguage(textToTranslate);
                if (!languageCodes.Contains(fromLanguageCode))
                {

                    languageCache[text] = "";
                    return "";
                }
            }
            else
                fromLanguageCode = languageCodesAndTitles[fromLanguage].ToString();

            string toLanguageCode = languageCodesAndTitles[targetLanguage].ToString();

          
            // handle null operations: no text or same source/target languages
            if (textToTranslate == "" || fromLanguageCode == toLanguageCode)
            {
                languageCache[text] = textToTranslate;
                return textToTranslate;
            }

            // send HTTP request to perform the translation
            string endpoint = string.Format(Properties.Settings.Default.CognitiveServicesEndpoint, "translate");
            string uri = string.Format(endpoint + "&from={0}&to={1}", fromLanguageCode, toLanguageCode);

            System.Object[] body = new System.Object[] { new { Text = textToTranslate } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", Properties.Settings.Default.CognitiveServicesApiKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", Properties.Settings.Default.CognitiveServicesRegion);
                request.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());

             
                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<List<Dictionary<string, List<Dictionary<string, string>>>>>(responseBody);
                    var translation = result[0]["translations"][0]["text"];

                    // Update the translation field
                    return translation;
            }

        }
        public Translator()
        {
            GetLanguagesForTranslate();
        }
    }
}

