﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        private ConcurrentDictionary<CacheKey, long> Cache = new ConcurrentDictionary<CacheKey, long>();
        private static object SyncCache = new object();

        public StringsStorage(IDbConnection connection, IDbTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        public long? AcquireString(StringKind kind, string value)
        {
            if (value == null) return null;

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
                () => $"Get Id of string @{kind} '{value}' failed",
                2,
                () => AcquireString_Impl(kind, value)
            );

            if (!idStringNew.HasValue)
                throw new InvalidCastException($"Unexpected null string id for @{kind} '{value}'. Lost Scope_Identity()");

            Cache[cacheKey] = idStringNew.Value;

            return idStringNew;
        }
        
        const string SqlSelect = "Select IdString, StartsWith, Tail From SqlInsightsString WITH (UPDLOCK) Where Kind = @Kind and StartsWith = @StartsWith";
        const string SqlInsert = "Insert SqlInsightsString(Kind, StartsWith, Tail) Values(@Kind, @StartsWith, @Tail); Select Scope_Identity();";

        private long? AcquireString_Impl(StringKind kind, string value)
        {
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

        T TryEvalAndRetry<T>(Func<string> failTitle, int retryCount, Func<T> function)
        {
            Exception error = null;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    return function();
                }
                catch (Exception ex)
                {
                    error = ex;
                    Thread.Sleep(0);
                }
            }

            throw new InvalidOperationException(failTitle(), error);
        }

#if NETSTANDARD
        public async Task<IEnumerable<LongAndString>> GetAllStringsByKind(StringKind kind)
        {
            const string sql = "Select IdString, StartsWith, Tail From SqlInsightsString Where Kind = @Kind";
            var query = await Connection.QueryAsync<SelectStringsResult>(sql, new {Kind = (byte) kind});
            return query.Select(x => new LongAndString()
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
    }

    public enum StringKind : byte
    {
        KeyPath = 1,
        AppName = 2,
        HostId = 3,
    }
}