using System;
using System.Runtime.Remoting.Messaging;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.AspNetLegacy
{
    internal sealed class LegacySqlProfiler
    {
        private LegacySqlProfiler()
        {
        }

        internal static ISqlInsightsConfiguration SqlInsightsConfiguration;
    }

    public class LegacySqlProfilerContext
    {
        // An app should pick it app, inject into DI Container using ISqlInsightsConfiguration.SqlClientAppNameFormat
        public string ActionId { get; set; }

        // An app should assign ActionKeyPath using (by Filter, or by Module)
        public SqlInsightsActionKeyPath ActionKeyPath { get; set; }

        [ThreadStatic]
        private static LegacySqlProfilerContext TlsInstance = null;
        private const string LogicalDataName = "LegacySqlProfilerContext";

        public static LegacySqlProfilerContext Instance
        {
            set
            {
                TlsInstance = value;
                CallContext.LogicalSetData(LogicalDataName, value);
            }
            get
            {
                var ret = TlsInstance;
                if (ret != null) return ret;
                ret =  (LegacySqlProfilerContext) CallContext.LogicalGetData(LogicalDataName);

                if (ret == null)
                {
                    ret = Instance = new LegacySqlProfilerContext();
                }
                
                return ret;
            }
        }
    }
}
