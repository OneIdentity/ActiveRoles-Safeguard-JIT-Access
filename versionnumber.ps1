[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$SourceDir,
    [Parameter(Mandatory=$true, Position=1)]
    [string]$SemanticVersion,
    [Parameter(Mandatory=$true, Position=2)]
    [string]$BuildId,
    [Parameter(Mandatory=$true, Position=3)]
    [bool]$IsPrerelease
)

Write-Host "SemanticVersion = $SemanticVersion"
Write-Host "BuildId = $BuildId"

$local:BuildNumber = ($BuildId - 170000)
Write-Host "BuildNumber = $($local:BuildNumber)"

$local:PackageCodeMarker = "9999.9999.9999"
$local:AssemblyCodeMarker = "9999.9999.9999.9999"
$local:AssemblyVersion = "${SemanticVersion}.$($local:BuildNumber)"
if ($IsPrerelease)
{
    $local:PackageVersion = "${SemanticVersion}-dev-$($local:BuildNumber)"
}
else
{
    $local:PackageVersion = $SemanticVersion
}
Write-Host "PackageCodeMarker = $($local:PackageCodeMarker)"
Write-Host "AssemblyCodeMarker = $($local:AssemblyCodeMarker)"
Write-Host "PackageVersion = $($local:PackageVersion)"
Write-Host "AssemblyVersion = $($local:AssemblyVersion)"

Write-Host "Replacing markers in Common library assembly"
$local:ProjectFile = (Join-Path $PSScriptRoot "src\Common\Common.csproj")
(Get-Content $local:ProjectFile -Raw).replace($local:AssemblyCodeMarker, $local:AssemblyVersion) | Set-Content -Encoding UTF8 $local:ProjectFile
(Get-Content $local:ProjectFile -Raw).replace($local:PackageCodeMarker, $local:PackageVersion) | Set-Content -Encoding UTF8 $local:ProjectFile

Write-Host "Replacing markers in Service assembly"
$local:ProjectFile = (Join-Path $PSScriptRoot "src\Service\Properties\AssemblyInfo.cs")
(Get-Content $local:ProjectFile -Raw).replace($local:AssemblyCodeMarker, $local:AssemblyVersion) | Set-Content -Encoding UTF8 $local:ProjectFile


Write-Output "##vso[task.setvariable variable=AssemblyVersion;]$($local:AssemblyVersion)"
Write-Output "##vso[task.setvariable variable=PackageVersion;]$($local:PackageVersion)"
