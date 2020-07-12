cd WhatSearch
rd .\bin\Release\net5.0\win-x64\publish /q /s
dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained
md .\bin\Release\net5.0\win-x64\publish\config
move .\bin\Release\net5.0\win-x64\publish\*.config .\bin\Release\net5.0\win-x64\publish\config\
move .\bin\Release\net5.0\win-x64\publish\config.json .\bin\Release\net5.0\win-x64\publish\config\
move .\bin\Release\net5.0\win-x64\publish\.json .\bin\Release\net5.0\win-x64\publish\config\
copy .\bin\Release\net5.0\win-x64\*.Views.dll .\bin\Release\net5.0\win-x64\publish\
xcopy ..\contents .\bin\Release\net5.0\win-x64\publish\assets /y /s /i
explorer .\bin\Release\net5.0\win-x64\publish