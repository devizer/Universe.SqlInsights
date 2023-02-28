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

#if !NETSTANDARD2_0 && !NET462
    public class DbJsonConvert : DbJsonConvertLegacy
    {
    } 
#else        
        
    public class DbJsonConvert
    {
        public static readonly string Flawor = "System";
 
        public static string Serialize<T>(T argument)
        {
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