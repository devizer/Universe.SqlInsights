namespace Universe.NUnitPipeline.SqlServerDatabaseFactory;

public interface IDbConnectionString
{
	/// <summary>
	/// Getter for test body to use. It is assigned by infrastructure after DB is created and seeded.
	/// </summary>
	string ConnectionString { get; }
}