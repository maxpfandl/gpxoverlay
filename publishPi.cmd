REM rmdir /q /s \\192.168.0.15\docker\rssfeed\bin\
REM dotnet publish -c Release -o \\192.168.0.15\docker\rssfeed\bin\
rmdir /q /s d:\Documents\Development\publishfolder\
dotnet publish -c Release -r ubuntu-arm64 -o d:\Documents\Development\publishfolder\ --self-contained false
cd d:\Documents\Development\publishfolder\
wsl rsync -a -v /mnt/d/Documents/Development/publishfolder/ piServer:/var/dotnet/gpxoverlay/

