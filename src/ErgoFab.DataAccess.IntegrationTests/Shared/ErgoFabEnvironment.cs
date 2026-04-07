using System.Collections.Concurrent;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

static class ErgoFabEnvironment
{
	public static bool IgnoreCache => GetBooleanEnvVar("ERGOFAB_TESTS_IGNORE_CACHE");

	public static int AdditionalTestCount => _additionalTestCount.Value;

	public static bool NeedMuteEfLogs => GetBooleanEnvVar("ERGOFAB_TESTS_MUTE_EF_LOGS");

	public static int LargeTestOrganizationCount {
		get
		{
			// Not Used. Hardcoded 100_000 is enough
            var raw = Environment.GetEnvironmentVariable("ERGOFAB_LARGE_TEST_ORGANIZATION_COUNT");
			if (int.TryParse(raw, out var ret)) return ret;
			return 100_000;
		}
	}


    private static Lazy<int> _additionalTestCount = new Lazy<int>(() =>
    {
        var raw = Environment.GetEnvironmentVariable("ERGOFAB_TESTS_ADDITIONAL_COUNT");
        if (int.TryParse(raw, out var ret)) return ret;
        return 0;
    });


    static ConcurrentDictionary<string,bool> BooleanCache = new(StringComparer.InvariantCultureIgnoreCase);

    private static bool GetBooleanEnvVar(string name)
    {
        return BooleanCache.GetOrAdd(name, arg =>
        {
            var raw = Environment.GetEnvironmentVariable(arg)?.ToLower();
            return raw == "1" || raw == "true" || raw == "on";
        });
    }
}