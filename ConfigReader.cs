using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace PollyAI5
{
    public class BaseModel
    {
        public string name;
        public string endpoint;
        public string key;
    }

    public class AIModel : BaseModel
    {
        public string model_name;
        public int contextLength;
        public int outputLength;
    }

    public class Config<T>
    {
        public List<T> models { get; set; }
    }

    public static class ConfigReader
    {
        public static Dictionary<string, T> ReadConfig<T>(string path) where T : BaseModel
        {
            Dictionary<string, T> models = new Dictionary<string, T>();


            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);


            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException("Configuration file not found.", configFilePath);
            }

            var jsonContent = File.ReadAllText(configFilePath);
            var config = JsonConvert.DeserializeObject<Config<T>>(jsonContent);

            foreach (var model in config.models)
            {
                models[model.name] = model;
            }
            return models;
        }
    }





}
