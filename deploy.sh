sudo chown -R dotnet /home/madmap/deployscripts/gpx/*
sudo chgrp -R dotnet /home/madmap/deployscripts/gpx/*
sudo chmod 644 /home/madmap/deployscripts/gpx/*
sudo chmod 744 /home/madmap/deployscripts/gpx/gpxoverlay
sudo find /home/madmap/deployscripts/gpx/ -type d -exec chmod 755 {} \;
sudo rsync -a /home/madmap/deployscripts/gpx/* /var/dotnet/gpxoverlay/
sudo rm -r /home/madmap/deployscripts/gpx/*
sudo systemctl restart gpxoverlay.service