using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using gpxoverlay.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using gpxoverlay.OverlayDrawer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Configuration;

namespace gpxoverlay.Controllers
{
    public class HomeController : Controller
    {
        private IMemoryCache _cache;
        private ILogger<HomeController> _log;
        private IConfiguration _config;
        public HomeController(IMemoryCache memoryCache, ILogger<HomeController> log, IConfiguration configuration)
        {
            _cache = memoryCache;
            _log = log;
            _config = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Info()
        {
            return View();
        }

        public IActionResult Features()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Sample()
        {
            OverlayDrawerDefault drawer = new OverlayDrawerDefault();

            using (var gpxStreamFile = System.IO.File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sample.gpx")))
            using (var fotoStreamFile = System.IO.File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "sample.jpg")))
            using (var gpxStream = new MemoryStream())
            using (var fotoStream = new MemoryStream())
            {

                await gpxStreamFile.CopyToAsync(gpxStream);
                await fotoStreamFile.CopyToAsync(fotoStream);

                gpxStream.Position = 0;
                fotoStream.Position = 0;

                var result = drawer.Draw(gpxStream, fotoStream, "Trailrun Husarentempel");

                if (result != null)
                {
                    string filename = "sample_overlay.jpg";
                    string contentType = "image/jpeg";

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = true,
                    };

                    return File(result.ToArray(), contentType);
                }
            }
            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> CreateOverlay(string command, IFormFile gpx, IFormFile foto, string title, double? elev,
            double? dist, TimeSpan time, int rotate, bool reduce, string guser, string gpw, string useTrack, string units, bool elevationGraph,
            string color, string shadowcolor, IFormFile logo, string movingType, string layout, int trackThickness, bool btmGradient, string speedmeasure)
        {

            var download = command.Equals("Download Overlay");

            List<string> logLines = new List<string>();
            string header = (Request.Headers["CF-Connecting-IP"].FirstOrDefault() ?? Request.Headers["X-Forwarded-For"].FirstOrDefault());

            var logtitle = title;
            if (String.IsNullOrEmpty(title))
                logtitle = "NoTitle";

            var downloadString = download ? "Download" : "NoDownload";
            _log.LogInformation($"{downloadString}|{logtitle}|{useTrack}|{units}|{color}");
            using (var gpxStream = new MemoryStream())
            using (var logoStream = new MemoryStream())
            using (var fotoStream = new MemoryStream())
            {

                if (useTrack.Equals("garmin"))
                {
                    throw new ApplicationException("Sorry, currently Garmin is not supported anymore (they are making it hard to export activities and I'm not able to afford the 5,000.- API access....");
                    garmin.Downloader downloader = new garmin.Downloader(guser, gpw, _cache, _config);
                    MemoryStream stream = downloader.GetLastActivity();
                    stream.Position = 0;
                    await stream.CopyToAsync(gpxStream);

                }
                else
                {
                    await gpx.CopyToAsync(gpxStream);
                }

                if (logo != null)
                {
                    await logo.CopyToAsync(logoStream);
                }

                if (foto == null)
                {

                    using (var fotoStreamFile = System.IO.File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "white.jpg")))
                    {
                        await fotoStreamFile.CopyToAsync(fotoStream);
                    }
                }
                else
                {
                    await foto.CopyToAsync(fotoStream);
                }

                gpxStream.Position = 0;
                fotoStream.Position = 0;
                var moving = movingType == "moving";
                MemoryStream result;
                IOverlayDrawer drawer;
                if (layout.Equals("trackOnly"))
                {
                    drawer = new OverlayDrawerTrackOnly();
                }
                else
                {
                    drawer = new OverlayDrawerDefault();
                }

                result = drawer.Draw(gpxStream, fotoStream, title, elev, dist, time, rotate, reduce, units, elevationGraph, color, shadowcolor, logoStream, moving, trackThickness, btmGradient, speedmeasure);

                if (result != null)
                {
                    string filename = "black";
                    if (foto != null)
                    {
                        filename = Path.GetFileNameWithoutExtension(foto.FileName) + "_overlay.jpg";
                    }

                    string contentType = "image/jpeg";

                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = true,
                    };
                    
                    if (download)
                    {
                        return File(result.ToArray(), contentType, filename);
                    }
                    else
                    {
                        return File(result.ToArray(), contentType);
                    }

                }
            }
            return Ok();
        }
        
        [HttpPost]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
