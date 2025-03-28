﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Universe.NUnitPipeline.SqlServerDatabaseFactory
{
    // public for testing only
    public static class TestArgumentReflection
    {
        public static List<TestDbConnectionString> FindTestDbConnectionStrings(object[] arg)
        {
            List<TestDbConnectionString> ret = new List<TestDbConnectionString>();
            if (arg == null) return ret;
            foreach (var a in arg)
            {
                var list = FindTestDbConnectionStrings(a);
                ret.AddRange(list);
            }

            return ret;
        }
        public static List<TestDbConnectionString> FindTestDbConnectionStrings(object arg)
        {
            List<TestDbConnectionString> ret = new List<TestDbConnectionString>();
            if (arg is TestDbConnectionString found1)
            {
                ret.Add(found1);
                return ret;
            }

            if (arg == null) return ret;
            EnumProperties(arg.GetType(), arg, 1, ret);

            var countPostponed = ret.Count(x => x.Postponed);
            if (countPostponed > 1)
            {
                // TODO: Assign to all the instances the same value?
                throw new InvalidOperationException("Found more than one postponed DB Connection Options");
            }

            return ret;
        }

        static void EnumProperties(Type argType, object argInstance, int depth, List<TestDbConnectionString> results)
        {
            if (depth > 7) return;

            var properties = argType.GetProperties();
            foreach (var property in properties)
            {
                if (property.GetIndexParameters().Length > 0) continue;
                var propertyType = property.PropertyType;
                if (propertyType.GetTypeInfo().IsValueType) continue;
                // Next Check is for first level property only
                // if (!propertyType.IsAssignableFrom(typeof(TestDbConnectionString))) continue;
                if (typeof(IEnumerable).IsAssignableFrom(propertyType)) continue;
                var subInstance = property.GetValue(argInstance, new object[0]);
                if (subInstance is IEnumerable) continue;
                if (subInstance is TestDbConnectionString found)
                {
                    results.Add(found);
                }

                if (subInstance != null)
                {
                    EnumProperties(subInstance.GetType(), subInstance, depth + 1, results);
                }
            }
        }
    }
}
