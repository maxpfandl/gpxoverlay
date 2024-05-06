using System;
using System.IO;

namespace gpxoverlay.OverlayDrawer
{
    public interface IOverlayDrawer
    {
        public MemoryStream Draw(MemoryStream gpxStream, MemoryStream fotoStream, string title, double? elevation, double? dist,
            TimeSpan timeSpan, int rotate, bool reduce, string units, bool elevationGraph, string color, string shadowcolor,
            MemoryStream logoStream, bool moving = true, int trackThickness = 10, bool btmGradient = true, string speedmeasure = "pace");
    }
}