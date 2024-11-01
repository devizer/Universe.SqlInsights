﻿using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public static class ErgoFabTestCaseExtensions
{
    public static ErgoFabDbContext CreateErgoFabDbContext(this ErgoFabTestCase ergoFabTestCase)
    {
        return CreateErgoFabDbContext(ergoFabTestCase.ConnectionOptions);
    }

    public static ErgoFabDbContext CreateErgoFabDbContext(this IDbConnectionString dbConnectionString)
    {
        // TODO:
        // NO WAY
        // 1. Create DB using passed migration and and seeder
        // 2. START TRACE HERE 
        if (string.IsNullOrEmpty(dbConnectionString?.ConnectionString))
            throw new InvalidOperationException("dbConnectionString?.ConnectionString is null or empty. DB Test Pipeline is misconfigured");

        DbContextOptionsBuilder<ErgoFabDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<ErgoFabDbContext>();
        dbContextOptionsBuilder.UseSqlServer(dbConnectionString.ConnectionString, b => b.UseCompatibilityLevel(120));
        return new ErgoFabDbContext(dbContextOptionsBuilder.Options);
    }

}