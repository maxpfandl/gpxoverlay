using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using gpxoverlay.Gpx;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;


namespace gpxoverlay.OverlayDrawer
{
    public class OverlayDrawerTrackOnly : IOverlayDrawer
    {
        public MemoryStream Draw(MemoryStream gpxStream, MemoryStream fotoStream, string title, double? elevation = null, double? dist = null,
            TimeSpan timeSpan = new TimeSpan(), int rotate = 0, bool reduce = true, string units = "metric", bool elevationGraph = true,
            string color = "#ffffff", string shadowcolor = "#000000", MemoryStream logoStream = null, bool moving = true, int trackThickness=10,
            bool btmGradient = true, string speedmeasure = "pace")
        {
            GpxTrack myTrack = DrawerHelper.LoadTrack(gpxStream);

            Brush baseBrush;
            Brush shadowBrush;

            baseBrush = new SolidBrush(ColorTranslator.FromHtml(color));
            shadowBrush = new SolidBrush(ColorTranslator.FromHtml(shadowcolor));
            if (myTrack != null)
            {

                CultureInfo ci = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentCulture = ci;

                var filenameOut = Path.GetTempFileName();
                bool metric = units.Equals("metric");
                double elevationgain = elevation ?? GpsHelper.GetElevationGain(myTrack, metric);
                double distance = dist ?? GpsHelper.GetDistance(myTrack, metric);
                var points = myTrack.ToGpxPoints();
                if (reduce)
                {

                    points = (GpxPointCollection<GpxPoint>)GpsHelper.SmoothenList(1000, points);
                }

                var graphPoints = new List<Point>();
                var elevPoints = new List<PointF>();
                var mapGraphPoints = new List<Point>();
                double maxElev = points.GetMaxElevation() ?? 0;
                double minElev = points.GetMinElevation() ?? 0;


                foreach (var pt in points)
                {
                    graphPoints.Add(new Point((int)(pt.Longitude * 1000000), (int)(pt.Latitude * 1000000)));
                }

                var lblDist = "Distance";
                var lblDistVal = $"{distance.ToString("0.00")} " + (metric ? $"km" : "mi");

                string lblElev = "Elev. Gain";
                string lblElevVal = $"{elevationgain.ToString("0.00")} " + (metric ? $"m" : "ft");

                if (reduce)
                {
                    graphPoints = GpsHelper.DouglasPeuckerReduction(graphPoints, 200);
                }

                int maxLong = int.MinValue;
                int minLong = int.MaxValue;
                int minLat = int.MaxValue;
                int maxLat = int.MinValue;
                foreach (var point in graphPoints)
                {
                    int longitude = (int)(point.X);
                    int latitude = (int)(point.Y);
                    if (maxLong < longitude)
                        maxLong = longitude;
                    if (minLong > longitude)
                        minLong = longitude;

                    if (maxLat < latitude)
                        maxLat = latitude;
                    if (minLat > latitude)
                        minLat = latitude;

                }

                int rangeLong = maxLong - minLong;
                int rangeLat = maxLat - minLat;

                int size = 2000;
                int factor = 0;

                if (rangeLong < rangeLat)
                {
                    factor = rangeLat / size;
                }
                else
                {
                    factor = rangeLong / size;
                }

                foreach (var point in graphPoints)
                {
                    int longitude = (point.X - minLong) / factor;
                    int latitude = (point.Y - minLat) / factor;
                    mapGraphPoints.Add(new Point(longitude, latitude));
                }
                rangeLat = rangeLat / factor;
                rangeLong = rangeLong / factor;
                graphPoints = mapGraphPoints;



                using (var bitmap = Bitmap.FromStream(fotoStream))
                {


                    switch (rotate)
                    {
                        case 90:
                            bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            break;
                        case 180:
                            bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                            break;
                        case 270:
                            bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            break;
                        default:
                            break;
                    }

                    decimal width = bitmap.Width;
                    decimal height = bitmap.Height;
                    decimal target = 2048;

                    if (height < width)
                    {
                        decimal scaling = target / height;
                        width = (int)(width * scaling);
                        height = (int)(height * scaling);
                    }
                    else
                    {
                        decimal scaling = target / width;
                        width = (int)(width * scaling);
                        height = (int)(height * scaling);
                    }



                    using (var tempBitmap = new Bitmap(bitmap, (int)width, (int)height))
                    {
                        using (Graphics grp = Graphics.FromImage(tempBitmap))

                        {

                            grp.InterpolationMode = InterpolationMode.High;
                            grp.SmoothingMode = SmoothingMode.HighQuality;
                            grp.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                            grp.CompositingQuality = CompositingQuality.HighQuality;

                            Font fontDesc = new Font("Fira Code Bold", 80, FontStyle.Bold, GraphicsUnit.Pixel);
                            Font fontDescHead = new Font("Fira Code Bold", 50, FontStyle.Bold, GraphicsUnit.Pixel);

                            SizeF sizeLblDist = grp.MeasureString(lblDist, fontDescHead);
                            SizeF sizeLblDistVal = grp.MeasureString(lblDistVal, fontDesc);



                            SizeF sizeLblElev = grp.MeasureString(lblElev, fontDescHead);
                            SizeF sizeLblElevVal = grp.MeasureString(lblElevVal, fontDesc);

                            int offset = 200;
                            var sizeLables = sizeLblDistVal.Width + sizeLblElevVal.Width + offset;



                            Point topLeft = new Point((int)(tempBitmap.Width / 2 - sizeLables / 2), tempBitmap.Height - 200);
                            Point bottomLeft = new Point((int)(tempBitmap.Width / 2 - sizeLables / 2), tempBitmap.Height - 140);





                            Point topRight = new Point((int)(tempBitmap.Width / 2 + offset), tempBitmap.Height - 200);
                            Point bottomRight = new Point((int)(tempBitmap.Width / 2 + offset), tempBitmap.Height - 140);

                            var gradBaseColor = Color.FromArgb(0, Color.Black);
                            var gradDarkColor = Color.FromArgb(200, Color.Black);
                            int vertOffsetGrad = 200;

                            var gradientBrush = new LinearGradientBrush(new Point(tempBitmap.Width / 2, tempBitmap.Height - vertOffsetGrad), new Point(tempBitmap.Width / 2, tempBitmap.Height), gradBaseColor, gradDarkColor);


                            grp.FillRectangle(gradientBrush, 0, tempBitmap.Height - vertOffsetGrad, tempBitmap.Width, vertOffsetGrad);

                            grp.DrawString(lblDist, fontDescHead, baseBrush, topLeft);
                            grp.DrawString(lblDistVal, fontDesc, baseBrush, bottomLeft);

                            grp.DrawString(lblElev, fontDescHead, baseBrush, topRight);
                            grp.DrawString(lblElevVal, fontDesc, baseBrush, bottomRight);

                            using (var mapBitmap = new Bitmap(rangeLong + trackThickness*5, rangeLat + trackThickness*5))
                            using (Graphics grpmap = Graphics.FromImage(mapBitmap))
                            {
                                grpmap.InterpolationMode = InterpolationMode.High;
                                grpmap.SmoothingMode = SmoothingMode.HighQuality;
                                grpmap.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                                grpmap.CompositingQuality = CompositingQuality.HighQuality;
                                List<Point> shadowList = new List<Point>();
                                List<Point> intent = new List<Point>();
                                foreach (var pt in graphPoints)
                                {
                                    intent.Add(new Point(pt.X + trackThickness, pt.Y + trackThickness));
                                }
                                graphPoints = intent;
                                int shadowOffset = trackThickness/2;
                                foreach (var pt in graphPoints)
                                {
                                    shadowList.Add(new Point(pt.X + shadowOffset, pt.Y - shadowOffset));
                                }

                                grpmap.DrawCurve(new Pen(shadowBrush, trackThickness), shadowList.ToArray());
                                grpmap.DrawCurve(new Pen(baseBrush, trackThickness), graphPoints.ToArray());

                                mapBitmap.RotateFlip(RotateFlipType.Rotate180FlipX);



                                var mapWidth = (int)(mapBitmap.Width / 1.5);
                                float mapFactor = 1;

                                if (mapWidth < mapBitmap.Height)
                                {
                                    mapFactor = (float)(tempBitmap.Height - 300F) / mapBitmap.Height;
                                }
                                else
                                {
                                    mapFactor = (float)(tempBitmap.Width - 300F) / mapWidth;
                                }
                                Console.WriteLine($"Height: {mapBitmap.Height}; Width: {mapWidth}; Factor: {mapFactor}");
                                using (var tmpMap = new Bitmap(mapBitmap, new Size((int)(mapWidth * mapFactor), (int)(mapBitmap.Height * mapFactor))))
                                {
                                    grp.DrawImage(tmpMap, tempBitmap.Width / 2 - tmpMap.Width / 2, tempBitmap.Height / 2 - tmpMap.Height / 2 - 100);
                                }

                            }

                            MemoryStream result = new MemoryStream();
                            tempBitmap.Save(result, ImageFormat.Jpeg);
                            return result;
                        }
                    }

                }
            }
            return null;
        }


    }
}