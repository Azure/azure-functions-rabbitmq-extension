param(
	[Parameter(Mandatory=$true)][string]$mvnPrefix
)

Write-Host "Mvn prefix is $mvnPrefix"

#Build
cd binding-library/java
.\mvnBuild.bat

#Copy items to be published
mkdir ToBePublished
Copy-Item pom.xml "ToBePublished\$mvnPrefix.xml"
Copy-Item "target\$mvnPrefix.jar" "ToBePublished\$mvnPrefix.jar"
Copy-Item "target\$mvnPrefix-javadoc.jar" "ToBePublished\$mvnPrefix-javadoc.jar"
Copy-Item "target\$mvnPrefix-sources.jar" "ToBePublished\$mvnPrefix-sources.jar"



