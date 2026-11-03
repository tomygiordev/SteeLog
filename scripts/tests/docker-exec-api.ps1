function Invoke-DockerExec {

 begin { $lines = [Collections.Generic.List[string]]::new() }
 process { if ($null -ne $_) { $lines.Add([string]$_) } }
 end {
  $args2 = @($args); if ($args2[0] -eq '-i') { $args2 = @($args2 | Select-Object -Skip 1) }
  $container = $args2[0]; $cmd = @($args2 | Select-Object -Skip 1)
  if ($lines.Count -gt 0) {
   $file = Join-Path $env:TEMP ('sql-'+[guid]::NewGuid().ToString('N')+'.sql'); $remote='/tmp/'+[IO.Path]::GetFileName($file)
   [IO.File]::WriteAllText($file, ($lines -join "`n"), [Text.UTF8Encoding]::new($false))
   & docker cp $file "${container}:$remote" | Out-Null
   if ($LASTEXITCODE -ne 0) { throw 'docker cp failed' }
   $cmd += @('-f',$remote)
  }
  $body=@{AttachStdout=$true;AttachStderr=$true;Cmd=$cmd} | ConvertTo-Json -Compress
  $id=(Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:2375/containers/$container/exec" -ContentType 'application/json' -Body $body).Id
  $r=Invoke-WebRequest -Method Post -Uri "http://127.0.0.1:2375/exec/$id/start" -ContentType 'application/json' -Body '{"Detach":false,"Tty":false}' -UseBasicParsing
  [byte[]]$bytes=$r.Content; $pos=0
  while ($pos -lt $bytes.Length) { if (($bytes.Length-$pos) -lt 8) {throw 'Truncated Docker frame'}; $stream=$bytes[$pos]; $len=([int]$bytes[$pos+4] -shl 24) -bor ([int]$bytes[$pos+5] -shl 16) -bor ([int]$bytes[$pos+6] -shl 8) -bor [int]$bytes[$pos+7]; $pos+=8; if ($len -lt 0 -or ($pos+$len) -gt $bytes.Length) {throw 'Invalid Docker frame length'}; $msg=[Text.Encoding]::UTF8.GetString($bytes,$pos,$len); if($stream -eq 1) { [Console]::Out.Write($msg) } elseif ($stream -eq 2) { [Console]::Error.Write($msg) } else {throw 'Invalid Docker stream'}; $pos+=$len }
  $state=Invoke-RestMethod -Uri "http://127.0.0.1:2375/exec/$id/json"; if($state.Running -or $null -eq $state.ExitCode) {throw 'Docker exec incomplete'}; $global:LASTEXITCODE=[int]$state.ExitCode
 }
}
