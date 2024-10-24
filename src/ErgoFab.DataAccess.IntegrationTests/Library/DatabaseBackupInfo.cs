using Universe.SqlServerJam;

namespace ErgoFab.DataAccess.IntegrationTests.Library;

public class DatabaseBackupInfo
{
    public string BackupName { get; set; }
    public BackupFileDescription[] BackupFiles { get; set; }
}
