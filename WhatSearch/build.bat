dotnet publish -r win-x64 -c Release
robocopy C:\Git2019\WhatSearch\WhatSearch\bin\Release\netcoreapp3.1\win-x64\publish C:\Git2019\WhatSearch_publish /MIR
del C:\Git2019\WhatSearch_publish\config.json
del C:\Git2019\WhatSearch_publish\Log4Net.config
robocopy C:\Git2019\WhatSearch\contents C:\Git2019\WhatSearch_publish\contents /MIR
pause