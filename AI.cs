using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PollyAI5
{
    public class AI
    {
        public ObservableCollection<DialogEntry> DialogEntries { get; set; } = new ObservableCollection<DialogEntry>();
        public int maxRound = -1;
        public int trimDialogMode = 1;

        private Dictionary<string, AIModel> aiModelsDict;

        public AI(string systemPrompt,string configPath)
        {
            DialogEntries.Add(new DialogEntry
            {
                Character = "system",
                DialogText = systemPrompt,
                Image = null
            });
            aiModelsDict = ConfigReader.ReadConfig<AIModel>(configPath);
        }

        public void AddMessage(string prompt, int insertAt = -1, string character = "user", Bitmap image = null)
        {

            var newEntry = new DialogEntry
            {
                Character = character,
                DialogText = prompt,
                Image = image
            };

            if (insertAt == -1)
            {
                DialogEntries.Add(newEntry);
            }
            else if (insertAt >= 0)
            {
                DialogEntries.Insert(insertAt, newEntry);
            }
        }


        public async Task Chat(int maxtokens, float creative, string model, List<object> functions = null, FunctionExecutor executor = null,bool isAsync=true)
        {

            AIModel aiModel = GetModelFromName(model);

            DialogEntries.Add(new DialogEntry
            {
                Character = "assistant",
                DialogText = "",
                Image = null
            });

            await SendToModel(aiModel, maxtokens, creative, DialogEntries, functions, executor, isAsync);


            if (maxRound > 0)
            {
                TrimDialogEntries(aiModel);
            }
        }


        //prepare messages for sending to GPT api
        private async Task SendToModel(AIModel aiModel, int maxtokens, float creative, ObservableCollection<DialogEntry> DialogEntries, List<object> functions, FunctionExecutor executor, bool isAsync)
        {
            var messages = new List<dynamic>();

            foreach (var dialogEntry in DialogEntries)
            {
                if (string.IsNullOrEmpty(dialogEntry.DialogText)) continue;

                if (dialogEntry.Character == null)
                {
                    dialogEntry.Character = "user";
                }

                if (dialogEntry.Character == "user" || dialogEntry.Character == "system" || dialogEntry.Character == "assistant")
                {
                    object message;
                    if (dialogEntry.Character == "system" || dialogEntry.Image == null || !(dialogEntry.Image is Bitmap bitmapImage) )
                    {
                        message = new { role = dialogEntry.Character, content = dialogEntry.DialogText };
                    }
                    else
                    {
                        var textContent = new { type = "text", text = dialogEntry.DialogText };
                        var base64Image = DialogEntry.ImageToDataUrl(dialogEntry.Image);
                        var imageUrl = new { url = base64Image };
                        var imageContent = new { type = "image_url", image_url = imageUrl };

                        message = new { role = dialogEntry.Character, content = new object[] { textContent, imageContent } };
                    }

                    messages.Add(message);
                }
            }
            var requestBody = new Dictionary<string, object>
            {
                { "messages", messages },
                { "temperature", creative },
                { "max_tokens", maxtokens }
            };

            if (functions != null && functions.Count > 0)
            {
                requestBody["tools"] = functions;
                requestBody["tool_choice"] = "auto";
            }

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(300);
                
                if (aiModel.model_name == "Azure")
                {
                    httpClient.DefaultRequestHeaders.Add("api-key", aiModel.key);
                }
                else
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aiModel.key);
                    if (aiModel.model_name != "")
                    {
                        requestBody["model"] = aiModel.model_name;
                    }
                }
                
                if(isAsync)
                    await SendMessagesAsync(aiModel,requestBody, httpClient, messages, functions, executor);
                else
                    await SendMessages(aiModel, requestBody, httpClient, messages, functions, executor);
            } 
        }

        //send to GPT api and process ToolCall if in need
        private async Task SendMessagesAsync(AIModel aiModel,Dictionary<string, object> requestBody, HttpClient httpClient, List<dynamic> messages, List<object> functions, FunctionExecutor executor)
        {
            requestBody["stream"] = true;
            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, aiModel.endpoint); // 创建 HttpRequestMessage
            request.Content = jsonContent; // 设置请求内容

            using (var response2 = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)) // 使用 SendAsync 和 ResponseHeadersRead
            {
                response2.EnsureSuccessStatusCode();

                using (var stream = await response2.Content.ReadAsStreamAsync()) // 后续流式读取代码保持不变
                using (var reader = new System.IO.StreamReader(stream))
                {

                    int count = 0;
                    while (!reader.EndOfStream)
                    {
                        string line = await reader.ReadLineAsync();
                        if (line != null && line.StartsWith("data:"))
                        {
                            string data = line.Substring(5).Trim(); // Remove "data: " prefix and trim whitespace
                            if (data != "[DONE]") // End of stream marker
                            {
                                JObject jsonDocument = JObject.Parse(data); // Parse with JObject

                                if (jsonDocument.ContainsKey("error"))
                                {
                                    throw new Exception("error:" + data);
                                }

                                if (jsonDocument["choices"] != null && jsonDocument["choices"].Count() != 0)
                                {
                                    var choice = jsonDocument["choices"][0]["delta"];

                                    if (jsonDocument["choices"][0]["delta"] != null)
                                    {
                                        if (jsonDocument["choices"][0]["delta"]["content"] != null)
                                        {
                                            string text = jsonDocument["choices"][0]["delta"]["content"]?.ToString(); // Access properties using JObject indexer and convert to string
                                            if (!string.IsNullOrEmpty(text))
                                            {
                                                DialogEntries[DialogEntries.Count - 1].DialogText += text;
                                                count++;

                                                if (count > 5)
                                                {
                                                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));// Force UI update
                                                    await Dispatcher.Yield(DispatcherPriority.Render);
                                                    count = 0;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        DialogEntries[DialogEntries.Count - 1].DialogText = "cannot get GPT response";
                                    }
                                }

                            }
                        }
                    }
                }
            }
        }

        private async Task SendMessages(AIModel aiModel, Dictionary<string, object> requestBody, HttpClient httpClient, List<dynamic> messages, List<object> functions, FunctionExecutor executor)
        {
            var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = httpClient.PostAsync(aiModel.endpoint, jsonContent).Result;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var jsonResult = JObject.Parse(result);
            if (jsonResult.ContainsKey("error"))
            {
                throw new Exception("error:" + result);
            }

            var choice = jsonResult["choices"][0]["message"];
            if (choice["tool_calls"] != null && choice["tool_calls"].Count() != 0 && choice["tool_calls"].Type == JTokenType.Array)
            {
                var toolCalls = (JArray)choice["tool_calls"];
                messages.Add(choice);

                foreach (var toolCall in toolCalls)
                {
                    if (toolCall["function"] != null)
                    {
                        string functionName = toolCall["function"]["name"].ToString();
                        string functionArgs = toolCall["function"]["arguments"].ToString();

                        // Toolcall
                        string functionResult = executor(functionName, functionArgs);

                        object toolMessage = new
                        {
                            tool_call_id = toolCall["id"],
                            role = "tool",
                            name = functionName,
                            content = functionResult
                        };

                        messages.Add(toolMessage);
                    }
                }
                await SendMessages(aiModel,requestBody,httpClient, messages, functions, executor);

            }
            else if (choice["content"] != null)
            {
                DialogEntries[DialogEntries.Count - 1].DialogText = choice["content"].ToString();
            }
            else
            {
                DialogEntries[DialogEntries.Count - 1].DialogText = "cannot get GPT response";
            }
        }

        public AIModel GetModelFromName(string model)
        {
            return aiModelsDict.ContainsKey(model) ? aiModelsDict[model] : aiModelsDict.First().Value;
        }


        public List<string> GetAllModelNames()
        {
            return new List<string>(aiModelsDict.Keys);
        }

        //trim dialogs to limit length
        public void TrimDialogEntries(AIModel aiModel)
        {
            if (DialogEntries.Count <= maxRound) return;

            if (trimDialogMode == 1)
            {
                // keep system prompt, trim others to maxRound
                while (DialogEntries.Count > maxRound + 1)
                {
                    DialogEntries.RemoveAt(1); 
                }
            }
            else if (trimDialogMode == 2)
            {
                // summarize oldest conversation
                int halfCount = (DialogEntries.Count - 1) / 2; // exclude system prompt
                var entriesToSummarize = new ObservableCollection<DialogEntry>();
                for (int i = 1; i <= halfCount; i++)
                {
                    entriesToSummarize.Add(DialogEntries[i]);
                }
                entriesToSummarize.Insert(0, new DialogEntry { Character = "system", DialogText = "act as a meticulous and skilled summarizer secretary, summarizing the conversation between the user and the assistant below, and output a brief dialogue summary. Example output: In the previous conversation, the user mentioned..., and the assistant discussed...", Image = null });
                SendToModel(aiModel, 500, 0, entriesToSummarize, null, null,false).Wait();

                // delete old conversations
                for (int i = 0; i < halfCount; i++)
                {
                    DialogEntries.RemoveAt(2); // exclude system prompt and summarized info
                }
            }
        }
    }

    public delegate string FunctionExecutor(string functionName, string functionArgs);

    public class DialogEntry : INotifyPropertyChanged
    {
        private string character;
        private string dialogText;
        private Bitmap image;
        private BitmapSource imageSource;


        public string Character
        {
            get => character;
            set { character = value; OnPropertyChanged(nameof(Character)); }
        }

        public string DialogText
        {
            get => dialogText;
            set { dialogText = value; OnPropertyChanged(nameof(DialogText)); }
        }

        public Bitmap Image
        {
            get => image;
            set
            {
                image = value;
                ImageSource = image?.ToBitmapSource(); // update BitmapSource
                OnPropertyChanged(nameof(Image));
            }
        }

        public BitmapSource ImageSource
        {
            get => imageSource;
            private set
            {
                imageSource = value;
                OnPropertyChanged(nameof(ImageSource));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //image to base64
        public static string ImageToDataUrl(Bitmap image)
        {
            if (image == null)
                return string.Empty;

            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, ImageFormat.Jpeg); 
                string base64EncodedData = Convert.ToBase64String(memoryStream.ToArray());
                return $"data:image/jpeg;base64,{base64EncodedData}";
            }
        }


        public static ObservableCollection<DialogEntry> DeepCopy(ObservableCollection<DialogEntry> original)
        {
            ObservableCollection<DialogEntry> copy = new ObservableCollection<DialogEntry>();
            foreach (DialogEntry entry in original)
            {
                Bitmap clonedImage = entry.Image != null ? new Bitmap(entry.Image) : null;
                copy.Add(new DialogEntry
                {
                    Character = entry.Character,
                    DialogText = entry.DialogText,
                    Image = clonedImage
                });
            }
            return copy;
        }
    }

    //Bitmap to BitmapSource
    public static class BitmapExtensions
    {
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            if (bitmap == null)
                return null;

            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap); 
            }
        }
    }
}
