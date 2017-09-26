using System;
using Windows.Devices.Geolocation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace _3DCars
{
    public sealed partial class MainPage
    {
        private class PointGenerator
        {
            private readonly AltitudeReferenceSystem AltitudeReference = AltitudeReferenceSystem.Terrain;

            private Geopath boundaries;
            private GeoboundingBox box;

            private Random generator = new Random(777);

            public Geopath Boundaries
            {
                get { return this.boundaries; }
                set
                {
                    var box = GeoboundingBox.TryCompute(value.Positions, AltitudeReference);
                    this.box = box ?? throw new ArgumentException("Invalid boundaries");
                    this.boundaries = value;
                }
            }

            public Geopoint Next()
            {
                var position = new BasicGeoposition
                {
                    Latitude = NextLatitude(),
                    Longitude = NextLongitude()
                };
                return new Geopoint(position, AltitudeReference);
            }

            private double NextLatitude()
            {
                var latitude = Lerp(
                    this.box.NorthwestCorner.Latitude,
                    this.box.SoutheastCorner.Latitude,
                    generator.NextDouble());
                return latitude;
            }

            private double NextLongitude()
            {
                var longitude = Lerp(
                    this.box.NorthwestCorner.Longitude,
                    this.box.SoutheastCorner.Longitude,
                    generator.NextDouble());
                return longitude;
            }

            private static double Lerp(double v0, double v1, double t)
            {
                return (1 - t) * v0 + t * v1;
            }
        }
    }
}
