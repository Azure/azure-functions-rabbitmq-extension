param(
	[Parameter(Mandatory=$true)][string]$mvnPrefix,
	[Parameter(Mandatory=$true)][string]$mvnVersion
)

Write-Host "Mvn prefix is $mvnPrefix"

#Build
cd binding-library/java
.\mvnBuild.bat

#Copy items to be published
mkdir ToBePublished\$mvnVersion
Copy-Item pom.xml "ToBePublished\$mvnVersion\$mvnPrefix.pom"
Copy-Item "target\$mvnPrefix.jar" "ToBePublished\$mvnVersion\$mvnPrefix.jar"
Copy-Item "target\$mvnPrefix-javadoc.jar" "ToBePublished\$mvnVersion\$mvnPrefix-javadoc.jar"
Copy-Item "target\$mvnPrefix-sources.jar" "ToBePublished\$mvnVersion\$mvnPrefix-sources.jar"



