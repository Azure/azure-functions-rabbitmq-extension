param(
	[Parameter(Mandatory=$true)][string]$versionPath
)

# Figure out the build number
$version = [System.IO.File]::ReadAllText("$versionPath\\version.txt")

$buildReason = $env:BUILD_REASON
$branch = $env:BUILD_SOURCEBRANCH

if ($buildReason -eq "PullRequest") {
  # parse PR title to see if we should pack this
  $response = Invoke-RestMethod api.github.com/repos/$env:BUILD_REPOSITORY_ID/pulls/$env:SYSTEM_PULLREQUEST_PULLREQUESTNUMBER
  $title = $response.title.ToLowerInvariant()
  Write-Host "Pull request '$title'"
  if ($title.Contains("[pack]")) {
    Write-Host "##vso[task.setvariable variable=BuildArtifacts;isOutput=true]true"
    Write-Host "Setting 'BuildArtifacts' to true."
  }
}

Write-Host "Version is $version"
Write-Host "##vso[task.setvariable variable=BuildNumber;isOutput=true]$version"
