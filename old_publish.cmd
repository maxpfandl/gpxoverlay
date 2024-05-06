REM rmdir /q /s \\192.168.0.15\docker\rssfeed\bin\
REM dotnet publish -c Release -o \\192.168.0.15\docker\rssfeed\bin\
rmdir /q /s d:\Documents\Development\publishfolder\
dotnet publish -c Release -o d:\Documents\Development\publishfolder\
cd d:\Documents\Development\publishfolder\
scp -r * ttrss:/home/madmap/tmp/gpx
ssh ttrss '/home/madmap/tmp/deployGpx.sh'
