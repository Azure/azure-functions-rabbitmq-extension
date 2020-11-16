cd ../binding-library/java

#Build artifact and version
[XML]$xmlContent = Get-Content pom.xml
$artifactId = $xmlContent.project.artifactId
$version = $xmlContent.project.version
$outputXmlFileName = "$artifactid-$version.xml"
Write-Host "##vso[task.setvariable variable=MvnPackagePrefix;isOutput=true]$outputXmlFileName"

#Build
#.\mvnBuild.bat

#Copy pom.xml to target fileName
Copy-Item pom.xml "target\$outputXmlFileName"
