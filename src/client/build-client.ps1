$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path (Split-Path (Split-Path $root -Parent) -Parent) 'outputs'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
function Find-Assembly([string]$name) { (Get-ChildItem 'C:\Windows\Microsoft.NET\assembly' -Recurse -Filter $name | Where-Object { $_.FullName -match 'GAC_(MSIL|64)' } | Select-Object -First 1).FullName }
$refs = @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll','System.Xaml.dll','System.Net.Http.dll') | ForEach-Object { '/reference:' + (Find-Assembly $_) }
$assets = Join-Path (Split-Path (Split-Path $root -Parent) -Parent) 'assets'
$resource = '/resource:{0},MarcaoBoostIcon.png' -f (Join-Path $assets 'MarcaoBoostIcon.png')
$nvidia = '/resource:{0},MarcaoGpuNvidia.exe' -f (Join-Path $root 'native\MarcaoGpuNvidia.exe')
$amd = '/resource:{0},MarcaoGpuAmd.exe' -f (Join-Path $root 'native\MarcaoGpuAmd.exe')
& $compiler /nologo /target:winexe /optimize+ /platform:x64 /main:MarcaoBoostWpf.ClientEntry /win32icon:"$assets\MarcaoBoost.ico" /win32manifest:"$root\client.manifest" $resource $nvidia $amd $refs /reference:System.dll /reference:System.Core.dll /reference:System.Web.Extensions.dll /out:"$out\MarcaoBoost.exe" "$root\WpfShared.cs" "$root\ClientApp.cs"
if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o Marcão Boost.' }
