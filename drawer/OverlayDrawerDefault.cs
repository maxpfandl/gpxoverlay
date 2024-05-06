using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Threading;
using gpxoverlay.Gpx;

namespace gpxoverlay.OverlayDrawer
{
    public class OverlayDrawerDefault : IOverlayDrawer
    {
        public MemoryStream Draw(MemoryStream gpxStream, MemoryStream fotoStream, string title, double? elevation = null, double? dist = null,
            TimeSpan timeSpan = new TimeSpan(), int rotate = 0, bool reduce = true, string units = "metric", bool elevationGraph = true, string color = "#ffffff",
            string shadowcolor = "#000000", MemoryStream logoStream = null, bool moving = true, int trackThickness = 10, bool btmGradient = true, string speedmeasure = "pace")
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

                double distance = dist ?? GpsHelper.GetDistance(myTrack, metric);
                double elevationgain = elevation ?? GpsHelper.GetElevationGain(myTrack, metric);

                TimeSpan time;
                if (timeSpan > new TimeSpan())
                {
                    time = timeSpan;
                    moving = false;
                }
                else
                {
                    try
                    {
                        if (moving)
                        {
                            time = GpsHelper.GetMovingTime(myTrack);
                        }
                        else
                        {
                            time = GpsHelper.GetTime(myTrack);
                        }
                    }
                    catch
                    {
                        time = new TimeSpan(0, 0, 0);
                    }
                }

                var pace = GpsHelper.GetPace(distance, time);
                var speed = GpsHelper.GetSpeed(distance, time);

                var trackTitle = title;



                var descc1l1 = "Distance";
                var descc1l2 = $"{distance.ToString("0.00")}";
                var descc1l2c = metric ? $"km" : "mi";
                var descc1l3 = "";
                var descc1l4 = "";
                var descc1l4c = "";
                if (speedmeasure == "speed")
                {
                    descc1l3 = moving ? "Moving Speed" : "Speed";
                    descc1l4 = $"{speed.ToString("0.00")}";
                    descc1l4c = metric ? $"km/h" : "mph";
                }
                else
                {
                    descc1l3 = moving ? "Moving Pace" : "Pace";
                    descc1l4 = $"{pace.ToString(@"m\:ss")}";
                    descc1l4c = metric ? $"/km" : "/mi";
                }

                string descc2l1 = "";
                string descc2l2 = "";
                string descc2l2c = "";
                if ((elevationgain > 100 && metric) || elevationgain > 300)
                {
                    descc2l1 = "Elev. Gain";
                    descc2l2 = $"{elevationgain.ToString("0.00")}";
                    descc2l2c = metric ? $"m" : "ft";
                }
                else
                {
                    elevationGraph = false;
                }
                var descc2l3 = moving ? "Moving Time" : "Time";
                var descc2l4 = $"{time.ToString("c")}";


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

                var elevFactor = 150 / (maxElev - minElev);

                int elevIndex = 0;
                foreach (var pt in points)
                {
                    if (pt.Elevation.HasValue)
                    {
                        elevPoints.Add(new PointF(elevIndex, (float)((pt.Elevation.Value - minElev) * elevFactor)));
                        elevIndex++;
                    }
                    graphPoints.Add(new Point((int)(pt.Longitude * 1000000), (int)(pt.Latitude * 1000000)));
                }

                if (elevPoints.Count < 5)
                {
                    elevationGraph = false;
                }

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

                int size = 800;
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


                //check baziers
                var drawBezier = false;
                if (drawBezier)
                {
                    int remove = graphPoints.Count % 3;

                    if (remove == 2)
                    {
                        graphPoints.RemoveAt(graphPoints.Count / 2);
                    }

                    else if (remove == 0)
                    {
                        graphPoints.RemoveAt(graphPoints.Count / 3);
                        graphPoints.RemoveAt((int)((double)graphPoints.Count / 1.5));
                    }
                }

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

                            Font fontHeader = new Font("Fira Code Bold", 90, FontStyle.Regular, GraphicsUnit.Pixel);
                            Font fontDesc = new Font("Fira Code Regular", 70, FontStyle.Regular, GraphicsUnit.Pixel);
                            Font fontDescHead = new Font("Fira Code Light", 40, FontStyle.Regular, GraphicsUnit.Pixel);

                            SizeF sizec1l1 = grp.MeasureString(descc1l1, fontDescHead);
                            SizeF sizec1l2 = grp.MeasureString(descc1l2, fontDesc);
                            SizeF sizec1l3 = grp.MeasureString(descc1l3, fontDescHead);
                            SizeF sizec1l4 = grp.MeasureString(descc1l4, fontDesc);

                            SizeF sizec2l1 = grp.MeasureString(descc2l1, fontDescHead);
                            SizeF sizec2l2 = grp.MeasureString(descc2l2, fontDesc);
                            SizeF sizec2l3 = grp.MeasureString(descc2l3, fontDescHead);
                            SizeF sizec2l4 = grp.MeasureString(descc2l4, fontDesc);

                            Point posc1l1 = new Point(55, (tempBitmap.Height - ((int)sizec1l4.Height + (int)sizec1l3.Height + (int)sizec1l2.Height + (int)sizec1l1.Height + 38)));
                            Point posc1l2 = new Point(50, (tempBitmap.Height - ((int)sizec1l4.Height + (int)sizec1l3.Height + (int)sizec1l2.Height + 50)));
                            Point posc1l2c = new Point(50 + (int)sizec1l2.Width, (tempBitmap.Height - ((int)sizec1l4.Height + (int)sizec1l3.Height + (int)sizec1l2.Height + 22)));
                            Point posc1l3 = new Point(55, (tempBitmap.Height - ((int)sizec1l4.Height + (int)sizec1l3.Height + 38)));
                            Point posc1l4 = new Point(50, (tempBitmap.Height - ((int)sizec1l4.Height + 50)));
                            Point posc1l4c = new Point(50 + (int)sizec1l4.Width, (tempBitmap.Height - ((int)sizec1l4.Height + 22)));



                            Point posc1l1sdw = new Point(posc1l1.X + 5, posc1l1.Y - 5);
                            Point posc1l2sdw = new Point(posc1l2.X + 5, posc1l2.Y - 5);
                            Point posc1l2sdwc = new Point(posc1l2c.X + 5, posc1l2c.Y - 5);
                            Point posc1l3sdw = new Point(posc1l3.X + 5, posc1l3.Y - 5);
                            Point posc1l4sdw = new Point(posc1l4.X + 5, posc1l4.Y - 5);
                            Point posc1l4sdwc = new Point(posc1l4c.X + 5, posc1l4c.Y - 5);

                            int offLeft = 350;
                            Point posc2l1 = new Point(55 + offLeft, (tempBitmap.Height - ((int)sizec1l4.Height + (int)sizec1l3.Height + (int)sizec1l2.Height + (int)sizec1l1.Height + 38)));
                            Point posc2l2 = new Point(50 + offLeft, (tempBitmap.Height - ((int)sizec1l4.Height + (int)sizec1l3.Height + (int)sizec1l2.Height + 50)));
                            Point posc2l2c = new Point(50 + offLeft + (int)sizec2l2.Width, (tempBitmap.Height - ((int)sizec1l4.Height + (int)sizec1l3.Height + (int)sizec1l2.Height + 22)));
                            Point posc2l3 = new Point(55 + offLeft, (tempBitmap.Height - ((int)sizec1l4.Height + (int)sizec1l3.Height + 38)));
                            Point posc2l4 = new Point(50 + offLeft, (tempBitmap.Height - ((int)sizec1l4.Height + 50)));



                            Point posc2l1sdw = new Point(posc2l1.X + 5, posc2l1.Y - 5);
                            Point posc2l2sdw = new Point(posc2l2.X + 5, posc2l2.Y - 5);
                            Point posc2l2sdwc = new Point(posc2l2c.X + 5, posc2l2c.Y - 5);
                            Point posc2l3sdw = new Point(posc2l3.X + 5, posc2l3.Y - 5);
                            Point posc2l4sdw = new Point(posc2l4.X + 5, posc2l4.Y - 5);




                            Point posTitle = new Point(50, 50);

                            Point posTitleSdw = new Point(posTitle.X + 5, posTitle.Y - 5);




                            using (var mapBitmap = new Bitmap(rangeLong + trackThickness * 5, rangeLat + trackThickness * 5))
                            using (Graphics grpmap = Graphics.FromImage(mapBitmap))
                            {


                                if (btmGradient)
                                {
                                    var gradBaseColor = Color.FromArgb(0, Color.Black);
                                    var gradDarkColor = Color.FromArgb(150, Color.Black);
                                    int vertOffsetGrad = 400;

                                    var gradientBrush = new LinearGradientBrush(new Point(tempBitmap.Width / 2, tempBitmap.Height - vertOffsetGrad), new Point(tempBitmap.Width / 2, tempBitmap.Height), gradBaseColor, gradDarkColor);


                                    grp.FillRectangle(gradientBrush, 0, tempBitmap.Height - vertOffsetGrad + 5, tempBitmap.Width, vertOffsetGrad);
                                }

                                if (btmGradient && !String.IsNullOrEmpty(title))
                                {
                                    var gradBaseColor = Color.FromArgb(0, Color.Black);
                                    var gradDarkColor = Color.FromArgb(100, Color.Black);
                                    int vertOffsetGrad = 200;

                                    var gradientBrush = new LinearGradientBrush(new Point(tempBitmap.Width / 2, 0), new Point(tempBitmap.Width / 2, 205), gradDarkColor, gradBaseColor);


                                    grp.FillRectangle(gradientBrush, 0, 0, tempBitmap.Width, vertOffsetGrad);
                                }


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
                                int shadowOffset = trackThickness / 2;
                                foreach (var pt in graphPoints)
                                {
                                    shadowList.Add(new Point(pt.X + shadowOffset, pt.Y + shadowOffset));
                                }
                                if (drawBezier)
                                {
                                    grpmap.DrawBeziers(new Pen(shadowBrush, trackThickness), shadowList.ToArray());
                                    grpmap.DrawBeziers(new Pen(baseBrush, trackThickness), graphPoints.ToArray());
                                }
                                else
                                {
                                    grpmap.DrawCurve(new Pen(shadowBrush, trackThickness), shadowList.ToArray());
                                    grpmap.DrawCurve(new Pen(baseBrush, trackThickness), graphPoints.ToArray());

                                }
                                mapBitmap.RotateFlip(RotateFlipType.Rotate180FlipX);


                                var mapWidth = (int)(mapBitmap.Width / 1.5);
                                using (var tmpMap = new Bitmap(mapBitmap, new Size(mapWidth, mapBitmap.Height)))
                                {
                                    if (elevationGraph)
                                    {
                                        grp.DrawImage(tmpMap, tempBitmap.Width - tmpMap.Width - 50, tempBitmap.Height - tmpMap.Height - 70 - 150);
                                    }
                                    else
                                    {
                                        grp.DrawImage(tmpMap, tempBitmap.Width - tmpMap.Width - 50, tempBitmap.Height - tmpMap.Height - 50);
                                    }
                                }
                                // elevPoints = GpsHelper.SmoothenList(mapBitmap.Width, elevPoints);
                                if (elevationGraph)
                                {
                                    using (var elevBitmap = new Bitmap(elevPoints.Count, 155))
                                    using (Graphics grpelev = Graphics.FromImage(elevBitmap))
                                    {

                                        grpelev.InterpolationMode = InterpolationMode.High;
                                        grpelev.SmoothingMode = SmoothingMode.HighQuality;
                                        grpelev.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                                        grpelev.CompositingQuality = CompositingQuality.HighQuality;

                                        foreach (var point in elevPoints)
                                        {
                                            var sdwPoint = point;
                                            sdwPoint.Y = sdwPoint.Y + 5;
                                            sdwPoint.X = sdwPoint.X + 5;

                                            grpelev.DrawLine(new Pen(shadowBrush, 4), new PointF(sdwPoint.X, sdwPoint.Y), new PointF(sdwPoint.X, point.Y + 1));
                                            grpelev.DrawLine(new Pen(baseBrush, 4), new PointF(point.X, point.Y), new PointF(point.X, 0));
                                        }
                                        // grpelev.FillClosedCurve(baseBrush, elevPoints.ToArray());
                                        elevBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                                        using (var tmpMap = new Bitmap(elevBitmap, new Size(mapWidth, elevBitmap.Height)))
                                        {
                                            grp.DrawImage(tmpMap, tempBitmap.Width - tmpMap.Width - 50, tempBitmap.Height - tmpMap.Height - 50);
                                        }

                                    }
                                }


                            }


                            using (var textBitmap = new Bitmap((int)width, (int)height))
                            using (Graphics grpText = Graphics.FromImage(textBitmap))
                            {

                                grpText.InterpolationMode = InterpolationMode.High;
                                grpText.SmoothingMode = SmoothingMode.HighQuality;
                                grpText.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                                grpText.CompositingQuality = CompositingQuality.HighQuality;

                                grpText.DrawString(trackTitle, fontHeader, shadowBrush, posTitleSdw);
                                grpText.DrawString(trackTitle, fontHeader, baseBrush, posTitle);

                                grpText.DrawString(descc1l1, fontDescHead, shadowBrush, posc1l1sdw);
                                grpText.DrawString(descc1l2, fontDesc, shadowBrush, posc1l2sdw);
                                grpText.DrawString(descc1l2c, fontDescHead, shadowBrush, posc1l2sdwc);
                                grpText.DrawString(descc1l3, fontDescHead, shadowBrush, posc1l3sdw);
                                grpText.DrawString(descc1l4, fontDesc, shadowBrush, posc1l4sdw);
                                grpText.DrawString(descc1l4c, fontDescHead, shadowBrush, posc1l4sdwc);

                                grpText.DrawString(descc2l1, fontDescHead, shadowBrush, posc2l1sdw);
                                grpText.DrawString(descc2l2, fontDesc, shadowBrush, posc2l2sdw);
                                grpText.DrawString(descc2l2c, fontDescHead, shadowBrush, posc2l2sdwc);
                                grpText.DrawString(descc2l3, fontDescHead, shadowBrush, posc2l3sdw);
                                grpText.DrawString(descc2l4, fontDesc, shadowBrush, posc2l4sdw);



                                grpText.DrawString(descc1l1, fontDescHead, baseBrush, posc1l1);
                                grpText.DrawString(descc1l2, fontDesc, baseBrush, posc1l2);
                                grpText.DrawString(descc1l2c, fontDescHead, baseBrush, posc1l2c);
                                grpText.DrawString(descc1l3, fontDescHead, baseBrush, posc1l3);
                                grpText.DrawString(descc1l4, fontDesc, baseBrush, posc1l4);
                                grpText.DrawString(descc1l4c, fontDescHead, baseBrush, posc1l4c);

                                grpText.DrawString(descc2l1, fontDescHead, baseBrush, posc2l1);
                                grpText.DrawString(descc2l2, fontDesc, baseBrush, posc2l2);
                                grpText.DrawString(descc2l2c, fontDescHead, baseBrush, posc2l2c);
                                grpText.DrawString(descc2l3, fontDescHead, baseBrush, posc2l3);
                                grpText.DrawString(descc2l4, fontDesc, baseBrush, posc2l4);

                                grp.DrawImage(textBitmap, new Point(0, 0));

                            }

                            if (logoStream != null && logoStream.Length > 0)
                            {
                                using (var logoBitmap = Bitmap.FromStream(logoStream))
                                {
                                    var maxWidth = 500;
                                    double factorLogo = 1;
                                    if (logoBitmap.Width > maxWidth)
                                    {
                                        factorLogo = logoBitmap.Width / maxWidth;
                                    }
                                    var newWidth = logoBitmap.Width / factorLogo;
                                    var newHeight = logoBitmap.Height / factorLogo;

                                    using (var tmpLogo = new Bitmap(logoBitmap, new Size((int)newWidth, (int)newHeight)))
                                    {
                                        grp.DrawImage(tmpLogo, (int)tempBitmap.Width - (int)newWidth - 50, 50);
                                    }
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