Set-SQLServer-Options -Title "SQL Server (local)" -Instance "(local)" -Options @{ 
  xp_cmdshell = $true; 
  "clr enabled" = $false; 
  "server trigger recursion" = $true; 
  "min server memory (MB)" = 7000; 
  "max server memory (MB)" = 16000; 
  "fill factor (%)" = 70 
}
