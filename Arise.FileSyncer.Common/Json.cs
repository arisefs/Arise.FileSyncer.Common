using fastJSON;

namespace Arise.FileSyncer.Common
{
    public static class Json
    {
        private static readonly JSONParameters settings = new()
        {
            SerializeNullValues = false,
            EnableAnonymousTypes = true,
        };

        public static string Serialize(object obj)
        {
            return JSON.ToJSON(obj, settings);
        }

        public static string SerializeFormatted(object obj)
        {
            return JSON.ToNiceJSON(obj, settings);
        }

        public static T Deserialize<T>(string json)
        {
            return JSON.ToObject<T>(json, settings);
        }

        public static object Deserialize(string json)
        {
            return JSON.ToObject(json, settings);
        }

        public static object FillObject(object obj, string json)
        {
            return JSON.FillObject(obj, json);
        }
    }
}
