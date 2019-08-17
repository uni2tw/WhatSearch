dotnet publish -c release -r win10-x64
robocopy C:\Git2019\WhatSearch\WhatSearch\bin\release\netcoreapp3.0\win10-x64\publish C:\Git2019\WhatSearch_publish /MIR
del C:\Git2019\WhatSearch_publish\config.json
robocopy C:\Git2019\WhatSearch\contents C:\Git2019\WhatSearch_publish\contents /MIR
pause