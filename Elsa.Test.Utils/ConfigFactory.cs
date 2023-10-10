using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Test.Utils
{
    public static class ConfigFactory
    {
        private const string configRoot = @"C:\Elsa\DEV\Config";

        public static T Get<T>(T def, string configFileName = null) 
        {
            Directory.CreateDirectory(configRoot);

            var fname = Path.Combine(configRoot, configFileName ?? $"{typeof(T).Name}.config.json");

            if (!File.Exists(fname)) 
            {
                var json = JsonConvert.SerializeObject(def);
                File.WriteAllText(fname, json);
                return def;
            }

            var fjson = File.ReadAllText(fname);

            return JsonConvert.DeserializeObject<T>(fjson);
        }

        public static T Get<T>(string configFileName = null) where T : new() 
        {
            return Get<T>(new T(), configFileName);
        }
    }
}
