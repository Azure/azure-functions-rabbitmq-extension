cd binding-library/java

#Build artifact and version
[XML]$xmlContent = Get-Content pom.xml
$artifactId = $xmlContent.project.artifactId
$version = $xmlContent.project.version
$outputXmlFileName = "$artifactid-$version"
# Write-Host "##vso[task.setvariable variable=MvnPackagePrefix;isOutput=true]$outputXmlFileName"

#Build
.\mvnBuild.bat

#Copy pom.xml to target fileName
Copy-Item pom.xml "target\$outputXmlFileName.xml"

#Copy to artifact staging directory
Copy-Item -Path "target\$outputXmlFileName.xml" -Destination $(Build.ArtifactStagingDirectory) -ErrorAction Stop -Verbose -Force
Copy-Item -Path "target\$outputXmlFileName.jar" -Destination $(Build.ArtifactStagingDirectory) -ErrorAction Stop -Verbose -Force
Copy-Item -Path "target\$outputXmlFileName-javadoc.jar" -Destination $(Build.ArtifactStagingDirectory) -ErrorAction Stop -Verbose -Force
Copy-Item -Path "target\$outputXmlFileName-sources.jar" -Destination $(Build.ArtifactStagingDirectory) -ErrorAction Stop -Verbose -Force
