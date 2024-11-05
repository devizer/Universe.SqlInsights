using Universe.SqlServerJam;

namespace ErgoFab.DataAccess.IntegrationTests.Library;

// TODO: Move it to Jam
public class DatabaseBackupInfo
{
    public string BackupName { get; set; }
    public BackupFileDescription[] BackupFiles { get; set; }
}