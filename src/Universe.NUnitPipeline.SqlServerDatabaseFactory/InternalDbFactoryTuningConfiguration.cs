using System;

namespace Universe.NUnitPipeline.SqlServerDatabaseFactory;

internal class InternalDbFactoryTuningConfiguration
{
    public static bool SkipTestSqlFactoryPlaygroundDatabase
    {
        get => GetBooleanVar("SKIP_TEST_SQL_FACTORY_PLAYGROUND_DATABASE");
        set => SetBooleanVar("SKIP_TEST_SQL_FACTORY_PLAYGROUND_DATABASE", value);
    } 

    static bool GetBooleanVar(string name)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return
            "1".Equals(raw, StringComparison.OrdinalIgnoreCase)
            || "True".Equals(raw, StringComparison.OrdinalIgnoreCase)
            || "Enable".Equals(raw, StringComparison.OrdinalIgnoreCase)
            || "Enabled".Equals(raw, StringComparison.OrdinalIgnoreCase)
            || "On".Equals(raw, StringComparison.OrdinalIgnoreCase);
    }

    static void SetBooleanVar(string name, bool value)
    {
        Environment.SetEnvironmentVariable(name, value.ToString());
    }


}