using System;

namespace Universe.SqlInsights.SqlServerStorage
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class DoublePrecisionConverter_Newtonsoft : JsonConverter
    {
	    public override bool CanConvert(Type objectType) => objectType == typeof(double);
	    public override bool CanRead => false;
	    public override bool CanWrite => true;

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	    {
		    if (value is double d)
		    {
			    if (double.IsNaN(d) || double.IsInfinity(d))
			    {
				    writer.WriteNull();
				    return;
			    }

			    writer.WriteValue((decimal)Math.Round(d, 6));
		    }
	    }

	    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	    {
		    throw new NotSupportedException("Custom double reading should not be invoked by this converter.");
	    }
    }


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
            Converters = { new DoublePrecisionConverter_Newtonsoft() }
		};

        public static string Serialize<T>(T argument)
        {
	        return JsonConvert.SerializeObject(argument, DefaultSettings);
        }
        
        public static T Deserialize<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString, DefaultSettings);
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

        private static readonly System.Text.Json.JsonSerializerOptions _SystemSerializeOptions = new System.Text.Json.JsonSerializerOptions
        {
	        Converters = { new DoublePrecisionConverter() }
        };

		public static string Serialize<T>(T argument)
        {
            // Done: Could not load file or assembly 'Microsoft.Bcl.AsyncInterfaces, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The located assembly's manifest definition does not match the assembly reference. (Exception from HRESULT: 0x80131040)
            return System.Text.Json.JsonSerializer.Serialize(argument, _SystemSerializeOptions);
        }

        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to?pivots=dotnet-8-0#serialization-behavior
        public static T Deserialize<T>(string jsonString)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(jsonString);
        }
	}


    public class DoublePrecisionConverter : System.Text.Json.Serialization.JsonConverter<double>
    {
	    public override double Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
		    => reader.GetDouble();

	    public override void Write(System.Text.Json.Utf8JsonWriter writer, double value, System.Text.Json.JsonSerializerOptions options)
	    {
		    writer.WriteNumberValue((decimal)Math.Round(value, 6));
		}
    }

#endif

}