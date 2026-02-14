Import-DevOps

Enumerate-Plain-SQLServer-Downloads | % { [pscustomobject] $_ } | ft

$jobs=@()
foreach($meta in Enumerate-Plain-SQLServer-Downloads) { 
  $sql = $meta.NormalizedKeywords
  $run_on = '2025'
  $container_tag = $null
  if ($sql -like '2005*' -or $sql -like '2008*') { $container_tag = "2022" }
  elseif ($sql -like '2012*' -or $sql -like '2014*') { $container_tag = "2016" } 
  $jobs += [pscustomobject] @{ SQL=$sql; OS=$run_on; SQL_CONTAINER_SUFFIX=$container_tag }
}

$matrix_object = @{ include = $jobs }
$matrix_string_mini = $matrix_object | ConvertTo-Json -Depth 64 -Compress
$matrix_string_formatted = $matrix_object | ConvertTo-Json -Depth 64

Say "Github Matrix Formatted"
Write-Host $matrix_string_formatted

Say "Github Matrix Mini"
Write-Host $matrix_string_mini

if ($env:GITHUB_OUTPUT) {
  Say "Generating GITHUB_OUTPUT variable 'matrix'"
  $utf8NoBom = New-Object System.Text.UTF8Encoding $false
  $outputLine = "matrix=$matrix_string_mini" + [System.Environment]::NewLine
  [System.IO.File]::AppendAllText($env:GITHUB_OUTPUT, $outputLine, $utf8NoBom)
}
