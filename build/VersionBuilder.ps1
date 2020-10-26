param(
	[Parameter(Mandatory=$true)][string]$releaseVersionNumber,
	[Parameter(Mandatory=$true)][string]$buildCounter
)

$fullVersionNumber = $releaseVersionNumber
if (-not($env:Build_SourceBranch -contains 'rel')) {
	$fullVersionNumber = "$fullVersionNumber.$buildCounter"
}

Write-Host "Full version is $fullVersionNumber"
Write-Host "##vso[task.setvariable variable=FullVersionNumber;isOutput=true]$fullVersionNumber"