using System;
using Autodesk.Revit.DB;

namespace RevitClaudeMCP.Utils
{
    /// <summary>
    /// Helper methods for geometry operations
    /// </summary>
    public static class GeometryHelper
    {
        /// <summary>
        /// Create XYZ point from coordinates
        /// </summary>
        public static XYZ CreatePoint(double x, double y, double z)
        {
            return new XYZ(x, y, z);
        }

        /// <summary>
        /// Create Line from two points
        /// </summary>
        public static Line CreateLine(XYZ start, XYZ end)
        {
            if (start.IsAlmostEqualTo(end))
            {
                throw new ArgumentException("Start and end points cannot be the same");
            }

            return Line.CreateBound(start, end);
        }

        /// <summary>
        /// Create Line from coordinates
        /// </summary>
        public static Line CreateLine(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            XYZ start = new XYZ(x1, y1, z1);
            XYZ end = new XYZ(x2, y2, z2);
            return CreateLine(start, end);
        }

        /// <summary>
        /// Calculate distance between two points
        /// </summary>
        public static double Distance(XYZ point1, XYZ point2)
        {
            return point1.DistanceTo(point2);
        }

        /// <summary>
        /// Calculate midpoint between two points
        /// </summary>
        public static XYZ Midpoint(XYZ point1, XYZ point2)
        {
            return (point1 + point2) / 2.0;
        }

        /// <summary>
        /// Create offset point
        /// </summary>
        public static XYZ OffsetPoint(XYZ point, XYZ direction, double distance)
        {
            XYZ normalized = direction.Normalize();
            return point + (normalized * distance);
        }

        /// <summary>
        /// Convert feet to millimeters (Revit uses feet internally)
        /// </summary>
        public static double FeetToMm(double feet)
        {
            return feet * 304.8;
        }

        /// <summary>
        /// Convert millimeters to feet (Revit uses feet internally)
        /// </summary>
        public static double MmToFeet(double mm)
        {
            return mm / 304.8;
        }

        /// <summary>
        /// Convert feet to meters
        /// </summary>
        public static double FeetToMeters(double feet)
        {
            return feet * 0.3048;
        }

        /// <summary>
        /// Convert meters to feet
        /// </summary>
        public static double MetersToFeet(double meters)
        {
            return meters / 0.3048;
        }

        /// <summary>
        /// Degrees to radians
        /// </summary>
        public static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        /// <summary>
        /// Radians to degrees
        /// </summary>
        public static double RadiansToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        /// <summary>
        /// Create rotation transform
        /// </summary>
        public static Transform CreateRotation(XYZ origin, XYZ axis, double angleDegrees)
        {
            double angleRadians = DegreesToRadians(angleDegrees);
            return Transform.CreateRotation(axis.Normalize(), angleRadians);
        }

        /// <summary>
        /// Create translation transform
        /// </summary>
        public static Transform CreateTranslation(XYZ translation)
        {
            return Transform.CreateTranslation(translation);
        }

        /// <summary>
        /// Get bounding box center
        /// </summary>
        public static XYZ GetBoundingBoxCenter(BoundingBoxXYZ bbox)
        {
            if (bbox == null)
                throw new ArgumentNullException(nameof(bbox));

            return (bbox.Min + bbox.Max) / 2.0;
        }

        /// <summary>
        /// Format XYZ as string
        /// </summary>
        public static string FormatPoint(XYZ point, string unit = "feet")
        {
            if (point == null)
                return "null";

            switch (unit.ToLower())
            {
                case "mm":
                    return $"({FeetToMm(point.X):F2}, {FeetToMm(point.Y):F2}, {FeetToMm(point.Z):F2}) mm";
                case "m":
                case "meters":
                    return $"({FeetToMeters(point.X):F3}, {FeetToMeters(point.Y):F3}, {FeetToMeters(point.Z):F3}) m";
                default:
                    return $"({point.X:F3}, {point.Y:F3}, {point.Z:F3}) ft";
            }
        }
    }
}
