Set-SQLServer-Options -Title "SQL Server $ENV:SQL_INSTANCE_NAME" -Instance "$ENV:SQL_INSTANCE_NAME" -Options @{ 
  xp_cmdshell = $true; 
  "clr enabled" = $false; 
  "server trigger recursion" = $true; 
  "min server memory (MB)" = 7000; 
  "max server memory (MB)" = 16000; 
  "fill factor (%)" = 70;
}

# Experimental
# if ($ENV:SQL -match "LocalDB" -and $ENV:SQL -notmatch "2012") {
if ($ENV:SQL -match "LocalDB") {
Set-SQLServer-Options -Title "SQL Server $ENV:SQL_INSTANCE_NAME" -Instance "$ENV:SQL_INSTANCE_NAME" -Options @{ 
  'user instance timeout' = 600; 
}
}

