﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErgoFab.DataAccess.IntegrationTests.Library;
using Library;

namespace Shared.TestDatabaseDefinitions
{
    public class EmptyDatabase : IDatabaseDefinition
    {
        public static EmptyDatabase Instance = new EmptyDatabase();
        public string Title { get; } = "Empty Database";
        public string CacheKey { get; } = null;
        public void MigrateAndSeed(IDbConnectionString connectionOptions)
        {
        }
    }
}
