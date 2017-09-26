using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Utility;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls.Maps;

namespace _3DCars
{
    public class CarResources
    {
        static readonly CarResources instance = new CarResources();

        private Dictionary<string, MapModel3D> cache = new Dictionary<string, MapModel3D>();

        [FlagsAttribute]
        public enum Models
        {
            HammerheadBlack = 0x00000001,
            HammerheadRed = 0x00000002,
            HammerheadSilver = 0x00000004,
            HammerheadWhite = 0x00000008,
            AllHammerheadCars = 0x0000000F,

            ShiftBlack = 0x00000010,
            ShiftBlue = 0x00000020,
            ShiftGold = 0x00000040,
            ShiftGray = 0x00000080,
            AllShiftCars = 0x000000F0,

            PoliceCar = 0x00000100,
            SchoolBus = 0x00000200,
            SemiTruck = 0x00000400,
            DumpTruck = 0x00000800,

            All = 0x00000FFF,
        }

        public static CarResources Instance
        {
            get { return instance; }
        }

        private CarResources()
        {
        }

        public MapModel3D Get(string uri)
        {
            System.Diagnostics.Debug.Assert(Has(uri));
            return this.cache[uri];
        }

        public bool Has(string uri)
        {
            return this.cache.ContainsKey(uri);
        }

        // Imports the 3D models w/ smooth shading 
        public async Task<bool> LoadAsync(Models flags)
        {
            bool result = true;
            if (flags.HasFlag(Models.HammerheadBlack) ||
                flags.HasFlag(Models.HammerheadSilver))
            {
                result = false;
            }
            if (flags.HasFlag(Models.HammerheadRed))
            {
                result &= await LoadFromUriAsyncIfNeeded(Car.HammerheadRedUri);
            }
            if (flags.HasFlag(Models.HammerheadWhite))
            {
                result &= await LoadFromUriAsyncIfNeeded(Car.HammerheadWhiteUri);
            }
            if (flags.HasFlag(Models.ShiftBlack) ||
                flags.HasFlag(Models.ShiftGray))
            {
                result = false;
            }
            if (flags.HasFlag(Models.ShiftBlue))
            {
                result &= await LoadFromUriAsyncIfNeeded(Car.ShiftBlueUri);
            }
            if (flags.HasFlag(Models.ShiftGold))
            {
                result &= await LoadFromUriAsyncIfNeeded(Car.ShiftGoldUri);
            }
            if (flags.HasFlag(Models.PoliceCar))
            {
                result &= await LoadFromUriAsyncIfNeeded(PoliceCar.Uri);
            }
            if(flags.HasFlag(Models.SchoolBus))
            {
                result &= await LoadFromUriAsyncIfNeeded(SchoolBus.Uri);
            }
            if (flags.HasFlag(Models.SemiTruck))
            {
                result &= await LoadFromUriAsyncIfNeeded(SemiTruck.Uri);
            }
            if (flags.HasFlag(Models.DumpTruck))
            {
                result &= await LoadFromUriAsyncIfNeeded(DumpTruck.Uri);
            }

            return result;
        }

        private async Task<bool> LoadFromUriAsyncIfNeeded(string uri)
        {
            if (!Has(uri))
            {
                var model = await MapModel3D.CreateFrom3MFAsync(
                    RandomAccessStreamReference.CreateFromUri(new Uri(uri)),
                    MapModel3DShadingOption.Smooth);
                if (model == null)
                {
                    throw new ArgumentException("Cannot load " + uri);
                }
                this.cache.Add(uri, model);
            }
            return true;
        }

        private async void LoadFromFileAsync()
        {
            var modelOpenPicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.Thumbnail,
            };
            modelOpenPicker.FileTypeFilter.Add(".3mf");

            StorageFile file3MF = await modelOpenPicker.PickSingleFileAsync();
            if (file3MF != null)
            {
                // Import the 3D model w/ smooth shading 
                var selectedCar = await MapModel3D.CreateFrom3MFAsync(
                    RandomAccessStreamReference.CreateFromFile(file3MF),
                    MapModel3DShadingOption.Smooth);
            }
        }
    }

    public class Car
    {
        private const double DefaultHeading = -90;
        private const int UpdatesPerSecond = 45;

        private MapControl map;

        private MapElement3D mapElement;
        private MapRoute route;

        private double distanceToNextManeuver;
        private double speed;

        private MapRouteLeg lastLeg;
        private int lastLegIndex;
        private int lastLegManeuverIndex;

        private List<BasicGeoposition> currentPositions;
        private int currentPositionIndex;
        private List<BasicGeoposition> remainingPositions;

        private List<BasicGeoposition> currentSegmentPositions;
        private int currentSegmentPositionIndex;

        private Geopoint lastLocation;

        private MapRouteLeg CurrentLeg
        {
            get { return this.lastLeg; }
        }

        private MapRouteManeuver CurrentManeuver
        {
            get { return this.lastLeg.Maneuvers.ElementAt(this.lastLegManeuverIndex); }
        }

        private MapRouteManeuver NextManeuver
        {
            get
            {
                if (this.lastLegManeuverIndex + 1 < this.lastLeg.Maneuvers.Count)
                {
                    return this.lastLeg.Maneuvers.ElementAt(this.lastLegManeuverIndex + 1);
                }
                else if (this.lastLegIndex + 1 < this.route.Legs.Count)
                {
                    return this.route.Legs.ElementAt(this.lastLegIndex + 1).Maneuvers.First();
                }
                return null;
            }
        }

        private double Speed
        {
            get { return this.speed; }
        }

        private double SpeedWithTraffic
        {
            // use average speed
            get { return CurrentLeg.LengthInMeters / CurrentLeg.EstimatedDuration.TotalSeconds; }
        }

        private double SpeedWithoutTraffic
        {
            // use average speed without traffic
            get { return CurrentLeg.LengthInMeters / CurrentLeg.DurationWithoutTraffic.TotalSeconds; }
        }

        public const string Uri = HammerheadRedUri;
        public const string HammerheadBlackUri = "ms-appx:///Resources/HammerheadBlackCar.3mf";
        public const string HammerheadRedUri = "ms-appx:///Resources/HammerheadRedCar.3mf";
        public const string HammerheadSilverUri = "ms-appx:///Resources/HammerheadSilverCar.3mf";
        public const string HammerheadWhiteUri = "ms-appx:///Resources/HammerheadWhiteCar.3mf";
        public const string ShiftBlackUri = "ms-appx:///Resources/ShiftBlackCar.3mf";
        public const string ShiftBlueUri = "ms-appx:///Resources/ShiftBlueCar.3mf";
        public const string ShiftGoldUri = "ms-appx:///Resources/ShiftGoldCar.3mf";
        public const string ShiftGrayUri = "ms-appx:///Resources/ShiftGrayCar.3mf";

        public Car(string uri = Uri, float scale = 0.75f) : this(35, uri, scale)
        {
        }

        public Car(float speed, string uri = Uri, float scale = 0.75f)
        {
            this.mapElement = new MapElement3D()
            {
                Model = CarResources.Instance.Get(uri),
                Scale = new Vector3(scale, scale, scale),
            };
            this.speed = speed;
        }

        public void Connect(MapControl map, MapRoute route)
        {
            this.map = map;
            this.route = route;
        }

        public void Clear()
        {
            this.currentPositions = null;
            this.currentPositionIndex = 0;
            this.remainingPositions = null;
            this.currentSegmentPositions = null;
            this.currentSegmentPositionIndex = 0;

            this.lastLeg = null;
            this.lastLegIndex = 0;
            this.lastLegManeuverIndex = 0;
            this.lastLocation = null;

            this.map.MapElements.Remove(this.mapElement);
        }

        public void Update()
        {
            if (this.lastLocation != null &&
                this.lastLocation.Position.Equals(this.route.Path.Positions.Last()))
            {
                Clear();
                return;
            }

            if (this.lastLeg == null)
            {
                this.lastLeg = this.route.Legs.First();
                this.lastLegIndex = 0;
                this.lastLegManeuverIndex = 0;

                this.mapElement.Heading = this.lastLeg.Maneuvers.First().StartHeading + DefaultHeading;
                this.mapElement.Location = this.lastLeg.Maneuvers.First().StartingPoint;
                this.map.MapElements.Add(this.mapElement);

                this.remainingPositions = CurrentLeg.Path.Positions.ToList();
                UpdateCurrentPositions();
            }
            else
            {
                UpdateCurrentPositions();

                //System.Diagnostics.Debug.Assert(currentSegmentPositionIndex < this.currentSegmentPositions.Count);
                var nextLocation = currentSegmentPositionIndex < this.currentSegmentPositions.Count ?
                    this.currentSegmentPositions.ElementAt(currentSegmentPositionIndex) :
                    this.route.Path.Positions.Last();

                if (!nextLocation.Equals(lastLocation.Position))
                {
                    this.mapElement.Heading = Spatial.HeadingInDegrees(lastLocation.Position, nextLocation) + DefaultHeading;
                    this.mapElement.Location = new Geopoint(nextLocation, AltitudeReferenceSystem.Terrain);
                }
            }

            lastLocation = this.mapElement.Location;
            //UpdateDistanceToNextManeuver();
        }

        private void UpdateCurrentPositions()
        {
            if (this.currentPositions == null)
            {
                System.Diagnostics.Debug.Assert(NextManeuver != null);
                var nextPosition = NextManeuver.StartingPoint.Position;

                this.currentPositionIndex = 0;
                this.currentPositions = this.remainingPositions.TakeWhile(position => !position.Equals(nextPosition)).ToList();
                this.remainingPositions = this.remainingPositions.SkipWhile(position => !position.Equals(nextPosition)).ToList();

                this.currentSegmentPositionIndex = 0;
                this.currentSegmentPositions = this.currentPositions.Count >= 2 ?
                    this.currentPositions.GetRange(this.currentPositionIndex, 2) :
                    this.currentPositions;
            }
            else if (this.currentSegmentPositionIndex + 1 < this.currentSegmentPositions.Count)
            {
                this.currentSegmentPositionIndex++;
                return;
            }
            else if (this.currentPositionIndex + 1 < this.currentPositions.Count)
            {
                this.currentPositionIndex++;

                if (this.currentPositionIndex + 1 == this.currentPositions.Count)
                {
                    var nextPosition = (this.lastLegManeuverIndex + 1 < this.lastLeg.Maneuvers.Count) ?
                        NextManeuver.StartingPoint.Position :
                        this.route.Path.Positions.Last();

                    this.currentSegmentPositionIndex = 0;
                    this.currentSegmentPositions = new List<BasicGeoposition>() { this.currentPositions.Last(), nextPosition };
                }
                else
                {
                    this.currentSegmentPositionIndex = 0;
                    this.currentSegmentPositions = this.currentPositions.GetRange(this.currentPositionIndex, 2);
                }
            }
            else if (this.lastLegManeuverIndex + 1 < this.lastLeg.Maneuvers.Count)
            {
                //System.Diagnostics.Debug.Assert(this.remainingPositions.Count > 0);
                this.lastLegManeuverIndex++;

                var nextPosition = (this.lastLegManeuverIndex + 1 < this.lastLeg.Maneuvers.Count) ?
                    NextManeuver.StartingPoint.Position :
                    this.route.Path.Positions.Last();

                this.currentPositionIndex = 0;
                this.currentPositions = this.remainingPositions.TakeWhile(position => !position.Equals(nextPosition)).ToList();
                this.remainingPositions = this.remainingPositions.SkipWhile(position => !position.Equals(nextPosition)).ToList();

                this.currentSegmentPositionIndex = 0;
                this.currentSegmentPositions = this.currentPositions.Count >= 2 ?
                    this.currentPositions.GetRange(this.currentPositionIndex, 2) :
                    this.currentPositions;
            }
            else if (this.lastLegIndex + 1 < this.route.Legs.Count)
            {
                this.lastLegIndex++;
                this.lastLegManeuverIndex = 0;
                this.currentPositionIndex = 0;
                this.currentSegmentPositionIndex = 0;
            }

            if (this.currentSegmentPositions.Count >= 2)
            {
                var t = Spatial.DistanceInMeters(currentSegmentPositions.First(), currentSegmentPositions.Last()) / Speed;
                var n = (int)(t * UpdatesPerSecond / currentSegmentPositions.Count) - 2;
                if (n > 0)
                {
                    this.currentSegmentPositions = Spatial.CalculateGeodesic(currentSegmentPositions, n);
                }
            }
        }

        private void UpdateDistanceToNextManeuver()
        {
            this.distanceToNextManeuver = (NextManeuver == null) ?
                Spatial.DistanceInMeters(this.lastLocation.Position, this.currentPositions.Last()) :
                Spatial.DistanceInMeters(this.lastLocation.Position, NextManeuver.StartingPoint.Position);
        }

        private void PrintPosition(BasicGeoposition position)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(" Position=" + ToString(position));
#endif
        }

        private void PrintRoute(MapRoute route)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Route: Legs=" + route.Legs.Count);

            foreach (var leg in route.Legs)
            {
                System.Diagnostics.Debug.WriteLine(" Maneuvers=" + leg.Maneuvers.Count);
                foreach (var maneuver in leg.Maneuvers)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "\t" + maneuver.Kind.ToString() +
                        ", Start= " + ToString(maneuver.StartingPoint.Position) +
                        ", StartHeading= " + maneuver.StartHeading +
                        ", EndHeading= " + maneuver.EndHeading
                        );
                }

                System.Diagnostics.Debug.WriteLine(" Path=" + leg.Path.Positions.Count);
                foreach (var position in leg.Path.Positions)
                {
                    System.Diagnostics.Debug.Write(" " + ToString(position));
                }
                System.Diagnostics.Debug.WriteLine("");
            }
#endif
        }

        private string ToString(BasicGeoposition position)
        {
            return "{ Lat= " + position.Latitude + ", Long= " + position.Longitude + " }";
        }

    }

    public class PoliceCar : Car
    {
        public new const string Uri = "ms-appx:///Resources/ShiftCopBlackCar.3mf";

        public PoliceCar(string uri = Uri, float scale = 0.75f) : base(uri, scale)
        {
        }
        public PoliceCar(float speed, string uri = Uri, float scale = 0.75f) : base(speed, uri, scale)
        {
        }
    }

    public class SchoolBus : Car
    {
        public new const string Uri = "ms-appx:///Resources/SchoolBus.3mf";

        public SchoolBus(string uri = Uri, float scale = 0.0275f) : base(uri, scale)
        {
        }
        public SchoolBus(float speed, string uri = Uri, float scale = 0.0275f) : base(speed, uri, scale)
        {
        }
    }

    public class SemiTruck : Car
    {
        public new const string Uri = "ms-appx:///Resources/SemiTruck.3mf";

        public SemiTruck(string uri = Uri, float scale = 0.0075f) : base(uri, scale)
        {
        }
        public SemiTruck(float speed, string uri = Uri, float scale = 0.0075f) : base(speed, uri, scale)
        {
        }
    }

    public class DumpTruck : Car
    {
        public new const string Uri = RedUri;
        public const string RedUri = "ms-appx:///Resources/DumpTruckRed.3mf";
        public const string YellowUri = "ms-appx:///Resources/DumpTruckYellow.3mf";

        public DumpTruck(string uri = Uri, float scale = 0.0075f) : base(uri, scale)
        {
        }
        public DumpTruck(float speed, string uri = Uri, float scale = 0.0075f) : base(speed, uri, scale)
        {
        }
    }
}
