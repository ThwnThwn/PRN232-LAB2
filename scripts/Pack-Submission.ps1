#requires -Version 7.0
[CmdletBinding()]
param([string]$OutputPath)

$ErrorActionPreference = 'Stop'
$solutionRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $OutputPath) {
    $OutputPath = Join-Path ([IO.Path]::GetDirectoryName($solutionRoot)) 'PRN232_LAB2_SE182422_VANCHAUTRUONGTHUAN.zip'
}
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Force ([IO.Path]::GetDirectoryName($OutputPath)) | Out-Null
Add-Type -AssemblyName System.IO.Compression
$stream = [IO.File]::Open($OutputPath, [IO.FileMode]::Create)
$archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
$count = 0
try {
    foreach ($file in (Get-ChildItem -LiteralPath $solutionRoot -Recurse -File -Force)) {
        $relative = [IO.Path]::GetRelativePath($solutionRoot, $file.FullName).Replace('\', '/')
        if ($relative -match '(^|/)(bin|obj|\.vs|\.vscode|\.git|test-results)/' -or
            $file.Name -eq '.env' -or $file.Extension -in '.zip','.user','.mdf','.ldf','.bak','.bacpac') { continue }
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relative,
            [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        $count++
    }
} finally {
    $archive.Dispose()
    $stream.Dispose()
}
Write-Host "Packaged $count files: $OutputPath"
