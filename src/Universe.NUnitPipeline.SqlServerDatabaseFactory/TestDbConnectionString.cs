namespace Universe.NUnitPipeline.SqlServerDatabaseFactory
{
    public class TestDbConnectionString : IDbConnectionString
    {
        public bool Postponed { get; set; } = false;
        public IDatabaseDefinition ManagedBy { get; protected set; }

        // Setter for the Test Framework (DbTestPipeline)
        // Getter for Test Case
        public string ConnectionString { get; set; }

        // Title is a copy of ManagedBy.Title
        public string Title { get; protected set; }

        protected TestDbConnectionString()
        {
        }

        public TestDbConnectionString(string connectionString, string title)
        {
            // Info! this database always created by migration+seeder
            ConnectionString = connectionString;
            Title = title;
            Postponed = false;
        }

        public TestDbConnectionString(IDatabaseDefinition managedBy)
        {
            ManagedBy = managedBy;
            Postponed = true;
            Title = managedBy.Title;
            ConnectionString = null;
        }


        // Connection string will be assigned by DbTestPipeline
        public static TestDbConnectionString CreatePostponed(IDatabaseDefinition dbDefinition)
        {
            return new TestDbConnectionString(dbDefinition);
        }

        // Parameter value for Test Explorer
        public override string ToString() => Title;
    }
}