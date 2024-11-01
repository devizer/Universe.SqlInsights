using Library;

namespace ErgoFab.DataAccess.IntegrationTests.Library;

public class TestDbConnectionString : IDbConnectionString
{
    public bool Postponed { get; set; } = false;
    public IDatabaseDefinition ManagedBy { get; protected set; }

    // Setter for Test Framework
    public string ConnectionString { get; set; }
    public string Title { get; protected set; }

    protected TestDbConnectionString()
    {
    }

    public TestDbConnectionString(string connectionString, string title)
    {
        // Warning! this database always created by migration+seeder
        ConnectionString = connectionString;
        Title = title;
        Postponed = false;
    }

    
    // Connection string is assigned by pipeline
    public static TestDbConnectionString CreatePostponed(IDatabaseDefinition dbDefinition)
    {
        return new TestDbConnectionString()
        {
            Postponed = true,
            ManagedBy = dbDefinition,
            Title = dbDefinition.Title,
            ConnectionString = null
        };
    }


    // Parameter value for Test Explorer
    public override string ToString() => Title;


}