param(
	[Parameter(Mandatory=$true)][string]$versionPath,
	[Parameter(Mandatory=$true)][string]$javaPath
)

#Figure out mvn build number
$xmlContent = [XML] (Get-Content "$javaPath\\pom.xml")
$artifactId = $xmlContent.project.artifactId
$version = $xmlContent.project.version
$outputXmlFileName = ([string] ("$artifactid-$version")).Trim()
Write-Host "Prefix generated from pom.xml is $outputXmlFileName"
Write-Host "##vso[task.setvariable variable=MvnPackagePrefix;isOutput=true]$outputXmlFileName"

# Figure out nuget build number
$xml = [Xml] (Get-Content "$versionPath\\WebJobs.Extensions.RabbitMQ.csproj")
$version = ([string]($xml.Project.PropertyGroup.Version)).Trim()

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
