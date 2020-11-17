#Build
cd binding-library/java
.\mvnBuild.bat

#Copy pom.xml to target fileName
Copy-Item pom.xml "target\$outputXmlFileName.xml"
