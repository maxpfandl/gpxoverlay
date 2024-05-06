using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GeoCoordinatePortable;

namespace gpxoverlay.Gpx

{
    public class GpsHelper
    {

        private static GpxPointCollection<GpxPoint> SmoothList(GpxPointCollection<GpxPoint> list, int minCount)
        {
            GpxPointCollection<GpxPoint> result = new GpxPointCollection<GpxPoint>();
            List<int> distances = new List<int>();

            GeoCoordinate start = null;
            foreach (var pt in list)
            {
                if (start == null)
                {
                    start = new GeoCoordinate(pt.Latitude, pt.Longitude);
                }
                var current = new GeoCoordinate(pt.Latitude, pt.Longitude);
                var dist = start.GetDistanceTo(current);
                distances.Add((int)dist);
                start = current;
            }

            var threshold = (int)Math.Round(distances.GetMedian());
            Console.WriteLine($"Using Threshold: {threshold.ToString()}");

            start = null;
            foreach (var pt in list)
            {
                if (start == null)
                {
                    start = new GeoCoordinate(pt.Latitude, pt.Longitude);
                }
                var current = new GeoCoordinate(pt.Latitude, pt.Longitude);
                var dist = start.GetDistanceTo(current);
                if (dist < threshold)
                {
                    result.Add(pt);
                }
                start = current;

            }
            return result;
        }


        public static List<PointF> SmoothenList(int maxCount, List<PointF> list)
        {

            if (list.Count > maxCount)
            {
                var fac = list.Count / maxCount;
                List<PointF> newColleciton = new List<PointF>();
                int counter = 0;
                foreach (var point in list)
                {
                    counter++;
                    if (counter % fac == 0)
                    {
                        newColleciton.Add(point);
                    }
                }
                Console.WriteLine($"Shrunk list from {list.Count} to {newColleciton.Count}");
                return newColleciton;
            }
            Console.WriteLine($"Shrunk list from {list.Count} to {list.Count}");
            return list;
        }
        public static GpxPointCollection<GpxPoint> SmoothenList(int maxCount, GpxPointCollection<GpxPoint> list)
        {

            if (list.Count > maxCount)
            {
                int fac = list.Count / maxCount;
                GpxPointCollection<GpxPoint> newColleciton = new GpxPointCollection<GpxPoint>();
                int counter = 0;
                foreach (var point in list)
                {
                    counter++;
                    if (counter % fac == 0)
                    {
                        newColleciton.Add(point);
                    }
                }
                Console.WriteLine($"Shrunk list from {list.Count} to {newColleciton.Count}");
                return newColleciton;
            }
            Console.WriteLine($"Shrunk list from {list.Count} to {list.Count}");
            return list;
        }
        public static double GetDistance(GpxTrack track, bool metric)
        {
            double km = 0;
            var gpxPoints = track.ToGpxPoints();

            var start = new GeoCoordinate(gpxPoints.StartPoint.Latitude, gpxPoints.StartPoint.Longitude);

            foreach (var point in gpxPoints)
            {
                GeoCoordinate pt = new GeoCoordinate(point.Latitude, point.Longitude);
                if (pt.GetDistanceTo(start) > 10)
                {
                    km += start.GetDistanceTo(pt) / 1000;
                    start = pt;
                }
            }
            if (metric)
                return km;
            else
                return km * 0.621;
        }

        public static TimeSpan GetPace(double km, TimeSpan time)
        {
            TimeSpan span = time / km;
            return new TimeSpan(span.Hours, span.Minutes, span.Seconds); // cut off milliseconds
        }

        public static double GetSpeed(double distance, TimeSpan time)
        {
            var hours = time.TotalHours;
            return distance / hours;
        }

        public static TimeSpan GetMovingTime(GpxTrack track)
        {
            TimeSpan result = new TimeSpan();
            var gpxPoints = track.ToGpxPoints();

            var start = gpxPoints.StartPoint;
            var startGC = new GeoCoordinate(gpxPoints.StartPoint.Latitude, gpxPoints.StartPoint.Longitude);

            foreach (var point in gpxPoints)
            {
                GeoCoordinate pt = new GeoCoordinate(point.Latitude, point.Longitude);
                if (pt.GetDistanceTo(startGC) > 1)
                {
                    var time = point.Time.Value - start.Time.Value;
                    if (time < new TimeSpan(0, 0, 30))
                    {
                        result += point.Time.Value - start.Time.Value;
                    }
                }
                start = point;
                startGC = pt;
            }
            return result;
        }

        public static TimeSpan GetTime(GpxTrack track)
        {
            var points = track.ToGpxPoints();
            return points.EndPoint.Time.Value - points.StartPoint.Time.Value;
        }

        public static double GetElevationGain(GpxTrack track, bool metric)
        {
            double elev = 0;
            var gpxPoints = track.ToGpxPoints();

            var start = gpxPoints.StartPoint;
            var startGC = new GeoCoordinate(gpxPoints.StartPoint.Latitude, gpxPoints.StartPoint.Longitude);

            foreach (var point in gpxPoints)
            {
                GeoCoordinate pt = new GeoCoordinate(point.Latitude, point.Longitude);
                if (pt.GetDistanceTo(startGC) > 60)
                {
                    if (start.Elevation.HasValue && point.Elevation.HasValue)
                    {
                        if (start.Elevation < point.Elevation)
                            elev += point.Elevation.Value - start.Elevation.Value;
                    }

                    start = point;
                    startGC = pt;
                }
            }
            if (metric)
                return elev;
            else
                return elev * 3.28;
        }

        /// <summary>
        /// Uses the Douglas Peucker algorithm to reduce the number of points.
        /// </summary>
        /// <param name="Points">The points.</param>
        /// <param name="Tolerance">The tolerance.</param>
        /// <returns></returns>
        public static List<Point> DouglasPeuckerReduction
            (List<Point> Points, Double Tolerance)
        {
            if (Points == null || Points.Count < 3)
                return Points;

            Int32 firstPoint = 0;
            Int32 lastPoint = Points.Count - 1;
            List<Int32> pointIndexsToKeep = new List<Int32>();

            //Add the first and last index to the keepers
            pointIndexsToKeep.Add(firstPoint);
            pointIndexsToKeep.Add(lastPoint);

            //The first and the last point cannot be the same
            while (Points[firstPoint].Equals(Points[lastPoint]))
            {
                lastPoint--;
            }

            DouglasPeuckerReduction(Points, firstPoint, lastPoint,
            Tolerance, ref pointIndexsToKeep);

            List<Point> returnPoints = new List<Point>();
            pointIndexsToKeep.Sort();
            foreach (Int32 index in pointIndexsToKeep)
            {
                returnPoints.Add(Points[index]);
            }

            return returnPoints;
        }

        /// <summary>
        /// Douglases the peucker reduction.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="firstPoint">The first point.</param>
        /// <param name="lastPoint">The last point.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="pointIndexsToKeep">The point index to keep.</param>
        private static void DouglasPeuckerReduction(List<Point>
            points, Int32 firstPoint, Int32 lastPoint, Double tolerance,
            ref List<Int32> pointIndexsToKeep)
        {
            Double maxDistance = 0;
            Int32 indexFarthest = 0;

            for (Int32 index = firstPoint; index < lastPoint; index++)
            {
                Double distance = PerpendicularDistance
                    (points[firstPoint], points[lastPoint], points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            if (maxDistance > tolerance && indexFarthest != 0)
            {
                //Add the largest point that exceeds the tolerance
                pointIndexsToKeep.Add(indexFarthest);

                DouglasPeuckerReduction(points, firstPoint,
                indexFarthest, tolerance, ref pointIndexsToKeep);
                DouglasPeuckerReduction(points, indexFarthest,
                lastPoint, tolerance, ref pointIndexsToKeep);
            }
        }

        /// <summary>
        /// The distance of a point from a line made from point1 and point2.
        /// </summary>
        /// <param name="pt1">The PT1.</param>
        /// <param name="pt2">The PT2.</param>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        public static Double PerpendicularDistance
            (Point Point1, Point Point2, Point Point)
        {
            //Area = |(1/2)(x1y2 + x2y3 + x3y1 - x2y1 - x3y2 - x1y3)|   *Area of triangle
            //Base = v((x1-x2)²+(x1-x2)²)                               *Base of Triangle*
            //Area = .5*Base*H                                          *Solve for height
            //Height = Area/.5/Base

            Double area = Math.Abs(.5 * (Point1.X * Point2.Y + Point2.X *
            Point.Y + Point.X * Point1.Y - Point2.X * Point1.Y - Point.X *
            Point2.Y - Point1.X * Point.Y));
            Double bottom = Math.Sqrt(Math.Pow(Point1.X - Point2.X, 2) +
            Math.Pow(Point1.Y - Point2.Y, 2));
            Double height = area / bottom * 2;

            return height;

            //Another option
            //Double A = Point.X - Point1.X;
            //Double B = Point.Y - Point1.Y;
            //Double C = Point2.X - Point1.X;
            //Double D = Point2.Y - Point1.Y;

            //Double dot = A * C + B * D;
            //Double len_sq = C * C + D * D;
            //Double param = dot / len_sq;

            //Double xx, yy;

            //if (param < 0)
            //{
            //    xx = Point1.X;
            //    yy = Point1.Y;
            //}
            //else if (param > 1)
            //{
            //    xx = Point2.X;
            //    yy = Point2.Y;
            //}
            //else
            //{
            //    xx = Point1.X + param * C;
            //    yy = Point1.Y + param * D;
            //}

            //Double d = DistanceBetweenOn2DPlane(Point, new Point(xx, yy));
        }

    }

}