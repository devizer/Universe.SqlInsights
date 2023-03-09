$source="$($ENV:INDEX_HTML)"
$src=(Get-Content -Path "$source")
$pattern="SQL_INSIGHTS_W3API_URL_PLACEHOLDER"
$p1=$src.IndexOf($pattern)
$p2=$p1 + $pattern.Length
Write-Host "p1=[$p1], p2=[$p2], Length=$($src.Length)"

$s1=$src.Substring(0,$p1)
$s2=$src.Substring($p2)

$dest=@"
#!/usr/bin/env bash
if [[ -n "`${SQL_INSIGHTS_W3API_URL:-}" ]]; then
  echo "[SqlInsights W3App Config] Overriding SQL_INSIGHTS_W3API_URL for index.html by '`$SQL_INSIGHTS_W3API_URL'"
  echo '$s1'
"@
$dest += "`${SQL_INSIGHTS_W3API_URL:-}" + @"
'$s2' > /usr/share/nginx/html/index.html
fi
"@

echo $dest > config-w3app.sh
