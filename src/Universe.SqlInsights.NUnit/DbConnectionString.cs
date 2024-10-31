﻿namespace ErgoFab.DataAccess.IntegrationTests.Library
{

    public class DbConnectionString : IDbConnectionString
    {
        // Setter for Test Framework
        public string ConnectionString { get; set; }
        public string Title { get; }

        public DbConnectionString(string connectionString, string title)
        {
            ConnectionString = connectionString;
            Title = title;
        }

        // Parameter value for Test Explorer
        public override string ToString() => Title;
    }
}