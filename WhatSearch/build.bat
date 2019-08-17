dotnet publish -c release -r win10-x64
robocopy C:\Git2019\WhatSearch\WhatSearch\bin\release\netcoreapp3.0\win10-x64\publish C:\Git2019\WhatSearch\publish /MIR
delete C:\Git2019\WhatSearch\publish\config.json
robocopy C:\Git2019\WhatSearch\contents C:\Git2019\WhatSearch\publish\contents /MIR
pause