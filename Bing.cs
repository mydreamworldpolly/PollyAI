using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PollyAI5
{
    static class Bing
    {

        private static string key;
        private static string endpoint;
        private static bool tmp = ReadConfig("search.json");
        private const int maxlength= 600;

        static public bool ReadConfig(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException("Configuration file not found.", configFilePath);
            }
            var jsonContent = File.ReadAllText(configFilePath);
            var config = JObject.Parse(jsonContent);
            key = config["key"].ToString();
            endpoint = config["endpoint"].ToString();
            return true;
        }

        public static async Task<List<string>> SearchAndGetContentsAsync(string query, int num, int lengthLimit = maxlength, bool isDetailed = true)
        {
            var results = new List<string>();
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

                var response = await httpClient.GetAsync($"{endpoint}?q={Uri.EscapeDataString(query)}&mkt=zh-CN&count=" + num.ToString()).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                var webPages = json["webPages"];
                if (webPages == null) return results;

                if (isDetailed)
                {
                    var tasks = webPages["value"].Select(async item =>
                    {
                        var url = item["url"].Value<string>();
                        try
                        {
                            return await GetWebPageContentAsync(url, lengthLimit);
                        }
                        catch
                        {
                            return null;
                        }
                    });

                    results = (await Task.WhenAll(tasks)).Where(r => r != null).ToList();
                }


                return results;
            }
        }

        private static async Task<string> GetWebPageContentAsync(string url, int lengthLimit)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(20);
                var response = await httpClient.GetAsync(url).ConfigureAwait(false);

                try
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    var r = ExtractMainContent(content, lengthLimit);
                    return r;

                }
                catch
                { return ""; }

            }
        }

        private static Encoding DetectEncoding(string content)
        {
            try
            {
                return Encoding.GetEncoding(content);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static string ExtractMainContent(string content, int length)
        {
            try
            {
                // scan Encoding
                var encoding = DetectEncoding(content);
                if (encoding != null && encoding != Encoding.UTF8)
                {
                    content = Encoding.GetEncoding(encoding.WebName).GetString(Encoding.UTF8.GetBytes(content));
                }

                var doc = new HtmlDocument();
                doc.LoadHtml(content);

                string extractedText = doc.DocumentNode.InnerText;
                extractedText = extractedText.Replace("\n", "").Replace("\t", "").Replace("\r", "").Replace(" ", "");
                // limit and trim text length
                if (extractedText.Length > length)
                {
                    extractedText = extractedText.Substring(0, length);
                }

                return extractedText;
            }
            catch
            {
                return "";
            }
        }

    }


}
