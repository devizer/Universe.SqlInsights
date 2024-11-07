using System.Diagnostics;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shared.TestDatabaseDefinitions;
using Universe.GenericTreeTable;
using Universe.NUnitPipeline;

namespace ErgoFab.DataAccess.IntegrationTests;

[NUnitPipelineAction]
[TempTestAssemblyAction]
public class ModelTests
{
    [Test]
    [ErgoFabTestCaseSource(42)]
    public void ShowModel(ErgoFabTestCase testCase)
    {
        IModel model = testCase.CreateErgoFabDbContext().Model;
        Console.WriteLine(model.BuildModelDescription());
    }

}