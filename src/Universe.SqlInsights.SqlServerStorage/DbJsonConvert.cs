namespace Universe.SqlInsights.SqlServerStorage
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    // Legacy Implementation is for tests only
    // Apps are using DbJsonConvert only 
    public class DbJsonConvertLegacy
    {
        public static readonly string Flawor = "Newtonsoft"; 
        private static readonly DefaultContractResolver TheContractResolver = new DefaultContractResolver();

        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            ContractResolver = TheContractResolver,
            MaxDepth = 128,
        };

        public static T Deserialize<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString, DefaultSettings);
        }

        public static string Serialize<T>(T argument)
        {
            return JsonConvert.SerializeObject(argument, DefaultSettings);
        }
    }

#if !NETSTANDARD2_0 && !NET461 && !NET5_0 && !NET5_0_OR_GREATER
    public class DbJsonConvert : DbJsonConvertLegacy
    {
    }
#else
        
    public class DbJsonConvert
    {
        public static readonly string Flawor = "System";
 
        public static string Serialize<T>(T argument)
        {
            // TODO: Could not load file or assembly 'Microsoft.Bcl.AsyncInterfaces, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference. (Exception from HRESULT: 0x80131040)
            return System.Text.Json.JsonSerializer.Serialize(argument);
        }

        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to?pivots=dotnet-8-0#serialization-behavior
        public static T Deserialize<T>(string jsonString)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString);
        }
    }

#endif

}