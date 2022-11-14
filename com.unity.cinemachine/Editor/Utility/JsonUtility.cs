using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cinemachine.Editor
{
    public static class JsonSerialization
    {
        [CanBeNull]
        public static T Deserialize<T>(string json)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings()
                {
                    Error = delegate(object sender, ErrorEventArgs args)
                    {
                        args.ErrorContext.Handled = true;
                    }
                });
                return result;
            }
            catch (Exception)
            {
                // Trapping everything since we're returning default(T) if we cannot deserialize
            }

            return default;
        }

        public static string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value);
        }
    }
}
