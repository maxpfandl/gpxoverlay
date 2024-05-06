using System.Drawing;
using System.IO;
using gpxoverlay.Gpx;

namespace gpxoverlay.OverlayDrawer
{
    public class DrawerHelper
    {
        public static GpxTrack LoadTrack(MemoryStream gpxStream)
        {
            GpxTrack result = null;
            using (var gpxReader = new GpxReader(gpxStream))
            {
                while (gpxReader.Read())
                {
                    switch (gpxReader.ObjectType)
                    {
                        case GpxObjectType.Track:
                            result = gpxReader.Track;
                            break;
                    }
                }
            }

            return result;
        }

        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            if (red > 255)
                red = 255;
            if (green > 255)
                green = 255;
            if (blue > 255)
                blue = 255;

            if (red < 0)
                red = 0;
            if (green < 0)
                green = 0;
            if (blue < 0)
                blue = 0;

            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }
    }
}