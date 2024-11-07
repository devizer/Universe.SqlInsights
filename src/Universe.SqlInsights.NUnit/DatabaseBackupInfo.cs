using Universe.SqlServerJam;

namespace Universe.SqlInsights.NUnit;

// TODO: Move it to Jam
public class DatabaseBackupInfo222
{
    public string BackupName { get; set; }
    public BackupFileDescription[] BackupFiles { get; set; }
}