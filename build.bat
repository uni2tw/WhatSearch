cd C:\Git2019\WhatSearch\WhatSearch
rd .\bin\Release\net7.0\win-x64\publish /q /s
dotnet publish -r win-x64 -c Release
rem dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained 
md .\bin\Release\net7.0\win-x64\publish\config
move .\bin\Release\net7.0\win-x64\publish\*.config .\bin\Release\net7.0\win-x64\publish\config\
move .\bin\Release\net7.0\win-x64\publish\config.json .\bin\Release\net7.0\win-x64\publish\config\
move .\bin\Release\net7.0\win-x64\publish\.json .\bin\Release\net7.0\win-x64\publish\config\
explorer .\bin\Release\net7.0\win-x64\publish