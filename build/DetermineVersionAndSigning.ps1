param(
	[Parameter(Mandatory=$true)][string]$releaseVersionNumber,
	[Parameter(Mandatory=$true)][string]$buildCounter
)

$fullVersionNumber = $releaseVersionNumber
$signArtifacts = $true
$sourceBranch = $env:BUILD_SOURCEBRANCH

if (-not($env:BUILD_SOURCEBRANCH.Contains('release'))) {
	$fullVersionNumber = "$fullVersionNumber.$buildCounter"
	$signArtifacts = $false
}

Write-Host "Full version is $fullVersionNumber"
Write-Host "SignArtifacts is $signArtifacts"
Write-Host "##vso[task.setvariable variable=SignArtifacts;isOutput=true]$signArtifacts"
Write-Host "##vso[task.setvariable variable=FullVersionNumber;isOutput=true]$fullVersionNumber"