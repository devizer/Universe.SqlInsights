using System.Data.Common;
using Microsoft.Data.SqlClient;
using Universe.NUnitPipeline;

namespace ErgoFab.DataAccess.IntegrationTests;

[NUnitPipelineAction]
[TempTestAssemblyAction]
public class TestConnectionStrings
{

    [Test]
    public async Task Test1System()
    {
        string
            cs1 = "Data Source=tcp:(local);Integrated Security=True;Pooling=true;Timeout=30;TrustServerCertificate=True;",
            cs2 = "Server=(local)";

        foreach (var cs in new string[] { cs1, cs2})
        {
            Try1System(System.Data.SqlClient.SqlClientFactory.Instance, cs);

            Microsoft.Data.SqlClient.SqlConnectionStringBuilder mcsb = new SqlConnectionStringBuilder(cs);
            mcsb.Pooling = false;
            Console.WriteLine(@$"
FROM: {cs}
  TO: {mcsb.ConnectionString}");

        }
    }

    private void Try1System(DbProviderFactory factory, string cs)
    {
        var b = factory.CreateConnectionStringBuilder();
        b.ConnectionString = cs;
        int letDebug = b.Count;
    }

    [Test]
    public void CompareMicrosoftAndSystem()
    {
        System.Data.SqlClient.SqlConnectionStringBuilder bs = new System.Data.SqlClient.SqlConnectionStringBuilder("Server=(local);Database=DB123");
        Microsoft.Data.SqlClient.SqlConnectionStringBuilder bm = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder("Server=(local);Database=DB123");

        var builders = new DbConnectionStringBuilder[] { bs, bm };
        List<string[]> totalKeys = new List<string[]>();
        foreach (var b in builders)
        {
            var bName = b.GetType().Namespace.Split('.').First();
            Console.WriteLine($"{bName}: {b.Count} specified parameters, {b.Keys.Count} total keys");
            var keyNames = b.Keys.OfType<string>().OrderBy(x => x).ToArray();
            totalKeys.Add(keyNames);
            foreach (var bKey in keyNames)
            {
                var bValue = b[bKey.ToString()];
                Console.WriteLine($"   {bKey}: {(bValue == null ? "null" : bValue)}");
            }
            Console.WriteLine("");

        }

        Console.WriteLine("COMMON KEYS");
        foreach (var k1 in totalKeys[0])
        {
            if (totalKeys[1].Contains(k1)) Console.WriteLine($"   {k1}");
            
        }
    }
}