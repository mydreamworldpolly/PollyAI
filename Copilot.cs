using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using static PollyAI5.GenImage;

namespace PollyAI5
{
    internal class Copilot
    {
        string systemPrompt = @"
You are a helpful assistant. If necessary, use provided tools to answer questions. 
When presented with a math problem, logic problem, or other problem benefiting from systematic thinking, think through it step by step before giving its final answer.
Say I don't know if you don't have the answer.
";
        public AI ai = new AI("","api.json");
        public Bitmap bitmap;
        public int maxSearchNum = 5;

        public Copilot()
        {

        }

        public async Task Work(int maxtoken, float creative, string model, AI mainAI, string memory)
        {
            bitmap = null;

            ai.DialogEntries = DialogEntry.DeepCopy(mainAI.DialogEntries);
            ai.DialogEntries.Insert(0, new DialogEntry { Character = "system", DialogText = systemPrompt });
            ai.DialogEntries[1] = new DialogEntry { Character = "user", DialogText = ai.DialogEntries[1].DialogText };

            if (memory != "")
                ai.DialogEntries[1].DialogText = ai.DialogEntries[1].DialogText + $"Note: When solving problems, the following supplementary knowledge is considered to be knowledge you have already mastered：<{memory}>";

            var bing = GenerateFunction("search_web", "returns Bing search results", new List<(string, string, string, bool)> { ("query", "string", "well-formed web search query", true) });
            var draw = GenerateFunction("generate_image", "calls an AI model to create an image", new List<(string, string, string, bool)> { ("prompt", "string",
                "detailed text description of the desired image in English.For instance:cute anime girl with massive fluffy fennec ears and a big fluffy tail blonde messy long hair blue eyes wearing a maid outfit with a long black gold leaf pattern dress and a white apron mouth open holding a fancy black forest cake with candles on top in the kitchen of an old dark Victorian mansion lit by candlelight with a bright window to the foggy forest and very expensive stuff everywhere",
                true),
            ("size", "string",
                "size of the desired image, value could be one of follows:Square,Landscape,Portrait. Default choice is Landscape",
                true),
            });

            var functions = new List<object> { bing, draw };

            FunctionExecutor executor = new FunctionExecutor(ExecuteFunction);

            await ai.Chat(maxtoken, creative, model, functions, executor,false);

            mainAI.AddMessage(ai.DialogEntries[ai.DialogEntries.Count - 1].DialogText);

            if (bitmap != null)
                mainAI.DialogEntries[mainAI.DialogEntries.Count-1].Image = bitmap;

        }

        private string ExecuteFunction(string functionName, string functionArgs)
        {
            switch (functionName)
            {
                case "search_web":
                    var query = JObject.Parse(functionArgs)["query"].ToString();

                    string searchResults;
                    try
                    {
                        var r = Bing.SearchAndGetContentsAsync(query, maxSearchNum).Result;
                        searchResults = string.Join(" ", r);
                    }
                    catch (Exception ex)
                    {
                        searchResults = ex.Message;
                    }

                    return searchResults;

                case "generate_image":
                    var prompt = JObject.Parse(functionArgs)["prompt"].ToString();
                    var size = (ImageAspectRatio)Enum.Parse(typeof(ImageAspectRatio), JObject.Parse(functionArgs)["size"].ToString());

                    try
                    {
                        bitmap = GenImage.GenerateImage(prompt, size);
                        return "image generated successfully";
                    }
                    catch (Exception ex)
                    {
                        return "error:" + ex.Message;
                    }
                default:
                    return $"未知函数: {functionName}";
            }
        }

        public static object GenerateFunction(string functionName, string functionDescription, List<(string name, string type, string description, bool required)> parameters)
        {
            var propertiesDictionary = new Dictionary<string, object>();
            var requiredParameters = new List<string>();

            foreach (var param in parameters)
            {
                var paramObject = new Dictionary<string, object>
        {
            { "type", param.type }
        };

                if (!string.IsNullOrEmpty(param.description))
                {
                    paramObject["description"] = param.description;
                }

                propertiesDictionary[param.name] = paramObject;

                if (param.required)
                {
                    requiredParameters.Add(param.name);
                }
            }

            return new
            {
                type = "function",
                function = new
                {
                    name = functionName,
                    description = functionDescription,
                    parameters = new
                    {
                        type = "object",
                        properties = propertiesDictionary,
                        required = requiredParameters.ToArray()
                    }
                }
            };
        }
    }
}
