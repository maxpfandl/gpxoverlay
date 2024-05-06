REM rmdir /q /s \\192.168.0.15\docker\rssfeed\bin\
REM dotnet publish -c Release -o \\192.168.0.15\docker\rssfeed\bin\
rmdir /q /s d:\Documents\Development\publishfolder\
dotnet publish -c Release -r ubuntu-arm64 -o d:\Documents\Development\publishfolder\ --self-contained false
cd d:\Documents\Development\publishfolder\
wsl rsync --no-perms --no-owner --no-group -a -v /mnt/d/Documents/Development/publishfolder/ piServer:/home/madmap/deployscripts/gpx/
ssh piServer '/home/madmap/deployscripts/gpxoverlay.sh'
