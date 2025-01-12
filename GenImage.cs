using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static PollyAI5.GenImage;

namespace PollyAI5
{
    static public class GenImage
    {

        static public string model;
        static private Dictionary<string, ImageModel> imageModelsDict = ConfigReader.ReadConfig<ImageModel>("image.json");
        static HttpClient httpClient = new HttpClient();

        public static Bitmap GenerateImage(string prompt, ImageAspectRatio imageAspectRatio)
        {
            if (imageModelsDict[model].isFlux)
            {
                return GenerateFluxImage(prompt, imageAspectRatio, imageModelsDict[model].imageQuality);
            }
            else
            {
                return GenerateDalle3Image(prompt, imageAspectRatio, imageModelsDict[model].imageQuality);
            }
        }


        public static Bitmap GenerateFluxImage(string prompt, ImageAspectRatio imageAspectRatio, ImageQuality imageQuality,
        string outputFormat = "jpg", int outputQuality = 100, int? seed = null, bool disableSafetyChecker = true)
        {

            string aspectRatio;
            switch (imageAspectRatio)
            {
                case ImageAspectRatio.Square:
                    aspectRatio = "1:1";
                    break;
                case ImageAspectRatio.Landscape:
                    aspectRatio = "16:9";
                    break;
                default:
                    aspectRatio = "9:16";
                    break;
            }
            Bitmap bitmap = null;


            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", imageModelsDict[model].key);
            httpClient.Timeout = TimeSpan.FromSeconds(120);

            var input = new
            {
                prompt = prompt,
                aspect_ratio = aspectRatio,
                output_format = outputFormat,
                output_quality = outputQuality,
                //seed,
                disable_safety_checker = disableSafetyChecker
            };

            var requestBody = new
            {
                input
            };

            // Serialize the request body using Newtonsoft.Json
            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the POST request
            var response = httpClient.PostAsync(imageModelsDict[model].endpoint, content).Result;
            response.EnsureSuccessStatusCode();

            // Read the response body
            var responseBody = response.Content.ReadAsStringAsync().Result;
            var responseObject = JObject.Parse(responseBody);

            // Extract the image URL
            string imageUrl = responseObject["urls"]?["get"]?.Value<string>();

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", imageModelsDict[model].key);
                int retry = 0;
                while (retry <= 30)
                {
                    // Send the GET request to check the status
                    response = httpClient.GetAsync(imageUrl).Result;
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    responseObject = JObject.Parse(responseBody);

                    // Extract the status
                    var status = responseObject["status"]?.Value<string>();

                    if (status == "succeeded")
                    {
                        // Extract the final image URL and download the image
                        imageUrl = responseObject["output"][0].ToString();
                        bitmap = DownloadImageFromUrl(imageUrl);
                        return bitmap;
                    }
                    else if (status == "failed")
                    {
                        throw new Exception("Generation failed due to the server");
                    }

                    retry++;
                    Task.Delay(2000);
                }
            }
            return bitmap;

        }

        public static Bitmap DownloadImageFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL cannot be null or empty", nameof(url));

            Bitmap bitmap;

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    byte[] imageData = httpClient.GetByteArrayAsync(url).Result;

                    using (var stream = new System.IO.MemoryStream(imageData))
                    {
                        bitmap = new Bitmap(stream);

                        saveImg(bitmap);
                        return bitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error downloading or converting image: {ex.Message}", ex);
            }
        }

        static void saveImg(Bitmap bitmap)
        {
            string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, fileName);
            bitmap.Save(filePath, ImageFormat.Png);
        }


        public static Bitmap GenerateDalle3Image(string prompt, ImageAspectRatio imageAspectRatio, ImageQuality imageQuality, string style = "natural") //vivid
        {
            Bitmap bitmap = null;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("api-key", imageModelsDict[model].key);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string size;
                switch (imageAspectRatio)
                {
                    case ImageAspectRatio.Square:
                        size = "1024x1024";
                        break;
                    case ImageAspectRatio.Landscape:
                        size = "1792x1024";
                        break;
                    default:
                        size = "1024x1792";
                        break;
                }

                string quality = imageQuality == ImageQuality.High ? "hd" : "standard";

                var body = new
                {
                    prompt,
                    size,
                    n = 1, //The number of images to generate. Only n=1 is supported for DALL-E 3.
                    quality,
                    style
                };

                var response = client.PostAsync(imageModelsDict[model].endpoint, new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")).Result;
                response.EnsureSuccessStatusCode();

                string json = response.Content.ReadAsStringAsync().Result;
                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                string imageUrl = data.data[0].url;

                bitmap = DownloadImageFromUrl(imageUrl);

            }
            return bitmap;
        }
        static public List<string> GetAllModelNames()
        {
            return new List<string>(imageModelsDict.Keys);
        }

        public enum ImageAspectRatio
        {
            Square,      
            Landscape,   // long
            Portrait     // wide
        }

        public enum ImageQuality
        {
            Standard,
            High
        }

    }

    public class ImageModel : BaseModel
    {
        public ImageQuality imageQuality;
        public bool isFlux;
    }

}
