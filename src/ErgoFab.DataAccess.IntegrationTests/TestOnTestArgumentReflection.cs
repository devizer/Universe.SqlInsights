using System.Runtime.InteropServices;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Shared.TestDatabaseDefinitions;
using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;
using Universe.SqlInsights.NUnit;

namespace ErgoFab.DataAccess.IntegrationTests;


[NUnitPipelineAction]
[TestFixture]
public class TestOnTestArgumentReflection
{
    [TestCase(1)]
    [TestCase(2f)]
    [TestCase(4d)]
    [TestCase("String")]
    public void NotFound<T>(T arg)
    {
        ExpectNotFound(arg);
    }

    [Test]
    public void TestAsIsPostponed()
    {
        TestDbConnectionString asIs = TestDbConnectionString.CreatePostponed(EmptyDatabase.Instance);
        ExpectFound(asIs, 1, 1);
    }

    [Test]
    public void TestErgoFabTestCase()
    {
        TestDbConnectionString asIs = TestDbConnectionString.CreatePostponed(EmptyDatabase.Instance);
        ErgoFabTestCase testCase = new ErgoFabTestCase() { ConnectionOptions = asIs };
        ExpectFound(testCase, 1, 1);
    }

    [Test]
    public void TestWindowsDirInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var dir = Path.Combine(Environment.GetEnvironmentVariable("SystemDrive"), "Windows");
            var info = new DirectoryInfo(dir);
            ExpectNotFound(info);
        }

        ExpectNotFound("Ж:\\Діск");
    }

    [Test]
    public void TestAbsentDirInfo()
    {
        ExpectNotFound($"Ж:{Path.DirectorySeparatorChar}Діск");
    }

    [Test]
    public void TestInnerPostponed()
    {
        TestDbConnectionString asIs = TestDbConnectionString.CreatePostponed(EmptyDatabase.Instance);

        var next2 = new { Db = (object)asIs };
        ExpectFound(next2, 1, 1);

        var next = new { Db123 = asIs };
        ExpectFound(next, 1, 1);

    }

    [Test]
    public void TestInnerInnerPostponed()
    {
        TestDbConnectionString asIs = TestDbConnectionString.CreatePostponed(EmptyDatabase.Instance);

        var next2 = new { InnerInner = new { Db = (object)asIs }  };
        ExpectFound(next2, 1, 1);

        var next = new { InnerInner = new { Db = asIs } };
        ExpectFound(next, 1, 1);

    }




    void ExpectFound(object arg, int count, int countPostponed)
    {
        List<TestDbConnectionString> found = TestArgumentReflection.FindTestDbConnectionStrings(arg);
        if (found.Count != count) Assert.Fail($"Expected Count={count} found TestDbConnectionString instances for [{arg?.GetType()}] = {arg}. But Actual Count is {found.Count}");
        var actualCountPostponed = found.Count(x => x.Postponed);
        if (actualCountPostponed != countPostponed) Assert.Fail($"Expected CountPostponed={count} found TestDbConnectionString instances for [{arg?.GetType()}] = {arg}. But Actual Count is {found.Count}");
    }

    void ExpectNotFound(object arg)
    {
        List<TestDbConnectionString> found = TestArgumentReflection.FindTestDbConnectionStrings(arg);
        if (found.Count > 0) Assert.Fail($"Expected zero found TestDbConnectionString instances for [{arg?.GetType()}] = {arg}");
    }


}