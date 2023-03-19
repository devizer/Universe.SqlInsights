using System;
using Microsoft.Extensions.Configuration;

namespace Universe.SqlInsights.W3Api;

public static class BoolConfigurationExtensions
{
    public static bool GetBooleanValue(this IConfiguration config, string configPath)
    {
        var raw = config.GetValue<string>(configPath);
        return
            "True".Equals(raw, StringComparison.OrdinalIgnoreCase)
            || "On".Equals(raw, StringComparison.OrdinalIgnoreCase)
            || "1" == raw;
    }
        
}