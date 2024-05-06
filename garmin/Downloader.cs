using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace gpxoverlay.garmin
{
    public class Downloader
    {
        private string _user;
        private string _pw;
        IMemoryCache _cache;
        IConfiguration _config;

        public Downloader(string user, string pw, IMemoryCache cache, IConfiguration config)
        {
            _user = user;
            _pw = pw;
            _cache = cache;
            _config = config;
        }
        public MemoryStream GetLastActivity()
        {
            MemoryStream result = _cache.GetOrCreate<MemoryStream>(_user, entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(10));

                string tmpFilePath = "garmin_" + _user;


                string jsonFile = Path.Combine(tmpFilePath, "activities.json");
                if (File.Exists(jsonFile))
                    File.Delete(jsonFile);

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = _config["Garmin:Bin"];
                psi.Arguments = $"{_config["Garmin:Script"]} --username {_user} --password {_pw} -f gpx -c 1 --directory {tmpFilePath}";

                Process proc = new Process
                {
                    StartInfo = psi
                };
                proc.Start();
                proc.WaitForExit();

                if (proc.ExitCode == 0 && Directory.Exists(tmpFilePath))
                {

                    JsonSerializerOptions options = new JsonSerializerOptions()
                    {
                        Converters = { new DateTimeConverter() }
                    };

                    var activities = JsonSerializer.Deserialize<List<GarminActivity>>(File.ReadAllText(jsonFile), options);

                    string lastFile = Path.Combine(tmpFilePath, "activity_" + activities[0].ActivityId + ".gpx");

                    if (!String.IsNullOrEmpty(lastFile))
                    {
                        MemoryStream memStream = new MemoryStream();
                        File.OpenRead(lastFile).CopyTo(memStream);
                        return memStream;
                    }
                }
                return null;
            });

            if (result == null)
                throw new ApplicationException("Could not get File from Garmin");

            return result;

        }
    }
}