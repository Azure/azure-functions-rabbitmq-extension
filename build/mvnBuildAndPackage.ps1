param(
	[Parameter(Mandatory=$true)][string]$mvnPrefix
)

Write-Host "Mvn prefix is $mvnPrefix"

#Build
cd binding-library/java
.\mvnBuild.bat

#Copy pom.xml to target fileName
Copy-Item pom.xml "target\$mvnPrefix.xml"
