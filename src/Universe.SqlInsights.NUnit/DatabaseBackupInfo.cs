using Universe.SqlServerJam;

namespace Universe.SqlInsights.NUnit;

// TODO: Move it to Jam
public class DatabaseBackupInfo
{
    public string BackupName { get; set; }
    public BackupFileDescription[] BackupFiles { get; set; }
}