using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.SqlServerStorage
{
    public class StringsStorage
    {
        
        readonly IDbConnection Connection;
        readonly IDbTransaction Transaction;
        public const int MaxStartLength = 445;
        private static ConcurrentDictionary<CacheKey, long> Cache = new ConcurrentDictionary<CacheKey, long>();

        public StringsStorage(IDbConnection connection, IDbTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        public long? AcquireString(StringKind kind, string value)
        {
            if (value == null) return null;

            CacheInvalidation cacheInvalidation = new CacheInvalidation(this);
            cacheInvalidation.InvalidationProbe();

            var cacheKey = new CacheKey()
            {
                Kind = kind,
                Value = value
            };

            if (Cache.TryGetValue(cacheKey, out var idString))
            {
                return idString;
            }

            long? idStringNew = TryEvalAndRetry(
                () => $"Get Id of string @{kind} '{value}'",
                2,
                () => AcquireString_Impl(kind, value)
            );

            if (!idStringNew.HasValue)
                throw new InvalidCastException($"Unexpected null string id for @{kind} '{value}'. Lost Scope_Identity()");

            Cache[cacheKey] = idStringNew.Value;

            return idStringNew;
        }
        

        private long? AcquireString_Impl(StringKind kind, string value)
        {
            bool isMemoryOptimized = MetadataCache.IsMemoryOptimized(Connection);
            // ... WITH (UPDLOCK, rowlock) Where ...
            // TODO: The table option 'rowlock' is not supported with memory optimized tables.
            // TODO: The table option 'updlock' is not supported with memory optimized tables.
            string SqlSelect = $"Select IdString, StartsWith, Tail From SqlInsightsString {(isMemoryOptimized ? "" : "WITH (UPDLOCK, ROWLOCK)")} Where Kind = @Kind and StartsWith = @StartsWith";
            const string SqlInsert = "Insert SqlInsightsString(Kind, StartsWith, Tail) OUTPUT Inserted.IdString as IdString Values(@Kind, @StartsWith, @Tail);";

            bool doesFit = value.Length <= MaxStartLength;
            string startsWith = !doesFit ? value.Substring(0, MaxStartLength) : value;
            var query = Connection.Query<SelectStringsResult>(SqlSelect, new
            {
                Kind = (byte) kind,
                StartsWith = startsWith
            }, Transaction);

            foreach (SelectStringsResult str in query)
            {
                if (IsIt(startsWith, doesFit, value, str))
                {
                    return str.IdString;
                }
            }

            var queryInsert = Connection.Query<long>(SqlInsert, new
            {
                Kind = (byte) kind,
                StartsWith = startsWith,
                Tail = doesFit ? null : value.Substring(MaxStartLength)
            }, Transaction);

            return queryInsert.FirstOrDefault();
        }

        T TryEvalAndRetry<T>(Func<string> operationTitle, int retryCount, Func<T> function)
        {
            Exception error = null;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    var ret = function();
                    if (i > 0)
                        Console.WriteLine($"Warning! Success on attempt #{(i+1)} for {operationTitle()}");

                    return ret;
                }
                catch (Exception ex)
                {
                    error = ex;
                    Thread.Sleep(0);
                }
            }

            throw new InvalidOperationException($"Fail: {operationTitle()}", error);
        }

#if NETSTANDARD || NET5_0 || NET461 || NET5_0_OR_GREATER
        public async Task<IEnumerable<LongIdAndString>> GetAllStringsByKind(StringKind kind)
        {
            const string sql = "Select IdString, StartsWith, Tail From SqlInsightsString Where Kind = @Kind";
            var query = await Connection.QueryAsync<SelectStringsResult>(sql, new {Kind = (byte) kind});
            return query.Select(x => new LongIdAndString()
            {
                Id = x.IdString,
                Value = x.Tail == null ? x.StartsWith : (x.StartsWith + x.Tail)
            });
        }
#endif

        static bool IsIt(string startsWith, bool doesFit, string value, SelectStringsResult str)
        {
            if (doesFit)
            {
                return startsWith == str.StartsWith && str.Tail == null;
            }
            else
            {
                return value == (str.StartsWith + str.Tail);
            }
        }

        class SelectStringsResult
        {
            public long IdString { get; set; } 
            public string StartsWith { get; set; } 
            public string Tail { get; set; }
        }

        class CacheKey
        {
            public StringKind Kind;
            public string Value;

            public bool Equals(CacheKey other)
            {
                return Kind == other.Kind && Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                return Equals((CacheKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int) Kind * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                }
            }
        }

        internal static void ResetCacheForTests()
        {
            Cache.Clear();
        }

        class CacheInvalidation
        {
            private StringsStorage Strings;
            private static Stopwatch PrevProbe;
            private static Guid? PrevDbInstanceUid;
            private const int ProbeIntervalMilliseconds = 5000;

            public CacheInvalidation(StringsStorage strings)
            {
                Strings = strings;
            }

            public void InvalidationProbe()
            {
                var prevProbe = Volatile.Read(ref PrevProbe);
                bool existsPrevProbe = prevProbe != null;
                if (prevProbe == null)
                {
                    prevProbe = Stopwatch.StartNew();
                    Volatile.Write(ref PrevProbe, prevProbe);
                }

                if (!existsPrevProbe)
                {
                    PrevDbInstanceUid = ReadDbInstanceUid();
                    return;
                }

                var milliseconds = prevProbe.ElapsedMilliseconds;
                if (milliseconds <= ProbeIntervalMilliseconds) return;
                var nextDbInstanceUid = ReadDbInstanceUid();
                if (!PrevDbInstanceUid.HasValue || !nextDbInstanceUid.HasValue) return; // or Exception, caz here both values exists
                if (nextDbInstanceUid.GetValueOrDefault().Equals(PrevDbInstanceUid.GetValueOrDefault())) return;
                // Evict
                PrevDbInstanceUid = nextDbInstanceUid;
                PrevProbe.Restart();
                StringsStorage.ResetCacheForTests();
            }

            Guid? ReadDbInstanceUid()
            {
                var guids =
                    Strings.Connection.Query<Guid>(
                        "Select Top 1 DbInstanceUid From SqlInsightsKeyPathSummaryTimestamp With (NoLock);",
                        null,
                        Strings.Transaction
                    );

                return guids.FirstOrDefault();
            }
        }
    }

    public enum StringKind : byte
    {
        KeyPath = 1,
        AppName = 2,
        HostId = 3,
    }
}