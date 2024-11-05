﻿using System.Text;
using Universe.SqlInsights.NUnit;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public class SqlServerTestsConfiguration : ISqlServerTestsConfiguration
{
    public static readonly SqlServerTestsConfiguration Instance = new SqlServerTestsConfiguration();
    // System
    public string MasterConnectionString { get; } = GetMasterConnectionString();

    // Microsoft (Trust Server Certificate=True;Encrypt=False)
    // public string MasterConnectionString { get; } = "Data Source=tcp:(local)\\dev_2019;Integrated Security=True;Pooling=true;Timeout=30;Trust Server Certificate=True;";
    public string Provider { get; } = "System";


    public string DbName { get; } = "Ergo Fab";
    public string BackupFolder { get; } = GetDbDataSubFolder("Integration Tests", "Backups");
    public string DatabaseDataFolder { get; } = GetDbDataSubFolder("Integration Tests", "Data");
    public string DatabaseLogFolder { get; } = GetDbDataSubFolder("Integration Tests", "Data");

    static string GetMasterConnectionString()
    {
        // TrustServerCertificate=True;?
        var raw = Environment.GetEnvironmentVariable("ERGOFAB_TESTS_MASTER_CONNECTIONSTRING");
        return
            string.IsNullOrEmpty(raw)
                ? "Data Source=(local);Integrated Security = SSPI;Pooling=true;Timeout=30;Encrypt=False;"
                : raw;

    }

    static string GetDbDataFolderRoot()
    {
        var raw = Environment.GetEnvironmentVariable("ERGOFAB_TESTS_DATA_FOLDER");
        return
            !string.IsNullOrEmpty(raw)
            ? raw
            : Directory.Exists("W:\\Temp") ? "W:\\Temp" : Path.GetFullPath(Path.DirectorySeparatorChar + "ErgoFab DB Data");
    }

    static string GetDbDataSubFolder(params string[] path)
    {
        var ret = new StringBuilder(GetDbDataFolderRoot());
        foreach (var s in path)
            ret.Append(Path.DirectorySeparatorChar).Append(s);

        return ret.ToString();
    }

}