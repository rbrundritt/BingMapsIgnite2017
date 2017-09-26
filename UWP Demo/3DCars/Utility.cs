using System;
using System.Collections.Generic;
using Windows.Devices.Geolocation;

namespace Utility
{
    class Spatial
    {
        public const double ApproximateEarthRadiusInMeters = 6378137.0;

        public static List<BasicGeoposition> CalculateGeodesic(List<BasicGeoposition> positions, int n)
        {
            var result = new List<BasicGeoposition>();

            for (int i = 0; i < positions.Count - 1; i++)
            {
                double latitude1 = DegreesToRadians(positions[i].Latitude);
                double longitude1 = DegreesToRadians(positions[i].Longitude);
                double latitude2 = DegreesToRadians(positions[i + 1].Latitude);
                double longitude2 = DegreesToRadians(positions[i + 1].Longitude);

                // Calculate the total extent of the route           
                var d = 2 * Math.Asin(Math.Sqrt(Math.Pow((Math.Sin((latitude1 - latitude2) / 2)), 2) + Math.Cos(latitude1) * Math.Cos(latitude2) * Math.Pow((Math.Sin((longitude1 - longitude2) / 2)), 2)));

                // Calculate positions at fixed intervals along the route
                for (int k = 0; k <= n; k++)
                {
                    var f = (k / (double)n);
                    var A = Math.Sin((1 - f) * d) / Math.Sin(d);
                    var B = Math.Sin(f * d) / Math.Sin(d);

                    // Obtain 3D Cartesian coordinates of each point
                    var x = A * Math.Cos(latitude1) * Math.Cos(longitude1) + B * Math.Cos(latitude2) * Math.Cos(longitude2);
                    var y = A * Math.Cos(latitude1) * Math.Sin(longitude1) + B * Math.Cos(latitude2) * Math.Sin(longitude2);
                    var z = A * Math.Sin(latitude1) + B * Math.Sin(latitude2);

                    // Convert these to latitude/longitude
                    var latitude = Math.Atan2(z, Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)));
                    var longitude = Math.Atan2(y, x);

                    var position = new BasicGeoposition()
                    {
                        Latitude = RadiansToDegrees(latitude),
                        Longitude = RadiansToDegrees(longitude)
                    };

                    result.Add(position);
                }
            }

            return result;
        }
        public static BasicGeoposition CalculatePosition(BasicGeoposition origin, double heading, double distance)
        {
            double latitudeO = DegreesToRadians(origin.Latitude);
            double longitudeO = DegreesToRadians(origin.Longitude);
            double headingO = DegreesToRadians(heading);

            double centralAngle = distance / ApproximateEarthRadiusInMeters;
            double latitude = Math.Asin(Math.Sin(latitudeO) * Math.Cos(centralAngle) + Math.Cos(latitudeO) * Math.Sin(centralAngle) * Math.Cos(headingO));
            double longitude = longitudeO + Math.Atan2(Math.Sin(headingO) * Math.Sin(centralAngle) * Math.Cos(latitudeO), Math.Cos(centralAngle) - Math.Sin(latitudeO) * Math.Sin(latitude));

            var position = new BasicGeoposition()
            {
                Latitude = RadiansToDegrees(latitude),
                Longitude = RadiansToDegrees(longitude)
            };
            return position;
        }

        public static double DistanceInMeters(BasicGeoposition pointA, BasicGeoposition pointB)
        {
            double latitudeA = DegreesToRadians(pointA.Latitude);
            double longitudeA = DegreesToRadians(pointA.Longitude);
            double latitudeB = DegreesToRadians(pointB.Latitude);
            double longitudeB = DegreesToRadians(pointB.Longitude);

            double longitudeDelta = Math.Abs(longitudeA - longitudeB);

            double numeratorPart1 = Math.Cos(latitudeA) * Math.Sin(longitudeDelta);
            numeratorPart1 = numeratorPart1 * numeratorPart1;

            double numeratorPart2 = Math.Cos(latitudeA) * Math.Sin(latitudeB) -
                Math.Sin(latitudeA) * Math.Cos(latitudeB) * Math.Cos(longitudeDelta);
            numeratorPart2 = numeratorPart2 * numeratorPart2;

            double x = Math.Sqrt(numeratorPart1 + numeratorPart2);
            double y = Math.Sin(latitudeA) * Math.Sin(latitudeB) +
                Math.Cos(latitudeA) * Math.Cos(latitudeB) * Math.Cos(longitudeDelta);

            return ApproximateEarthRadiusInMeters * Math.Atan2(x, y);
        }

        public static double HeadingInDegrees(BasicGeoposition pointA, BasicGeoposition pointB)
        {
            double latitudeA = DegreesToRadians(pointA.Latitude);
            double latitudeB = DegreesToRadians(pointB.Latitude);
            double longitudeDelta = DegreesToRadians(pointB.Longitude - pointA.Longitude);

            double x = Math.Cos(latitudeB) * Math.Sin(longitudeDelta);
            double y = Math.Cos(latitudeA) * Math.Sin(latitudeB) -
                Math.Sin(latitudeA) * Math.Cos(latitudeB) * Math.Cos(longitudeDelta);
            return RadiansToDegrees(Math.Atan2(x, y));
        }

        public static double DegreesToRadians(double angleInDegrees)
        {
            return angleInDegrees * Math.PI / 180.0;
        }

        public static double RadiansToDegrees(double angleInRadians)
        {
            return angleInRadians * 180.0 / Math.PI;
        }
    }
}