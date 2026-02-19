. "$PSScriptRoot\Functions.ps1"

echo "Query SQL Server '$ENV:SQL_INSTANCE_NAME' Medium Version"
try { 
  $sql_ver = Query-SqlServer-Version -Title "Instance $ENV:SQL_INSTANCE_NAME" -ConnectionString "$ENV:SQLINSIGHTS_CONNECTION_STRING"
  if ($sql_ver) { 
    Write-Line -TextGreen "Query-SqlServer-Version for Medium Version SUCCESS: $sql_ver"
    Write-Artifact-Info "SQL-SERVER-MEDIUM-VERSION.TXT" "$sql_ver" 
  }
}
catch {}

echo "Query SQL Server '$ENV:SQL_INSTANCE_NAME' Title"
try { 
  $sql_ver = Query-SqlServer-Version -Title "Instance $ENV:SQL_INSTANCE_NAME" -ConnectionString "$ENV:SQLINSIGHTS_CONNECTION_STRING" -Kind "Title"
  if ($sql_ver) {
    Write-Line -TextGreen "Query-SqlServer-Version for Title SUCCESS: $sql_ver"
    Write-Artifact-Info "SQL-SERVER-TITLE.TXT" "$sql_ver"
  }
}
catch {}
