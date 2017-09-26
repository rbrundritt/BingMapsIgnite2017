using System.Collections.Generic;
using System.Windows.Input;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;

namespace _3DCars
{
    public class MainPageViewModel : ViewModel
    {
        private MapControl map;
        private TextBlock status;

        private string city;
        private MapProjection mapProjection = MapProjection.WebMercator;
        private MapStyle mapStyle = MapStyle.Road;

        /// <summary>
        /// Used to set map to lat and long for the Bay Area in San Francisco, US
        /// </summary>
        private static readonly Geopoint SanFranciscoGeopoint = new Geopoint(new BasicGeoposition() { Latitude = 37.779860, Longitude = -122.429000 });

        /// <summary>
        /// Used to set map to lat and long for the Puget Sound in Seattle, US
        /// </summary>
        private static readonly Geopoint SeattleGeopoint = new Geopoint(new BasicGeoposition() { Latitude = 47.604, Longitude = -122.329 });

        /// <summary>
        /// Used to set map to lat and long for the Coliseum in Rome, Italy
        /// </summary>
        private static readonly Geopoint RomeGeopoint = new Geopoint(new BasicGeoposition() { Latitude = 41.890170, Longitude = 12.493094 });

        private Dictionary<string, Geopoint> destinations = new Dictionary<string, Geopoint>()
        {
            { "Rome", RomeGeopoint },
            { "San Francisco", SanFranciscoGeopoint },
            { "Seattle", SeattleGeopoint },
        };

        public void Connect(MapControl map, TextBlock status)
        {
            this.map = map;
            this.status = status;
        }

        public void DebugOutputStatus()
        {
            this.status.Text =
                "\nProjection= " + this.map.MapProjection +
                "\nStyle= " + this.map.Style +
                "\nStyleSheet= " + GetMapStyleSheetName(this.map.StyleSheet);
        }

        public void EnableToolbar()
        {
            this.map.RotateInteractionMode = MapInteractionMode.PointerKeyboardAndControl;
            this.map.TiltInteractionMode = MapInteractionMode.PointerKeyboardAndControl;
            this.map.ZoomInteractionMode = MapInteractionMode.PointerKeyboardAndControl;
        }

        public string City
        {
            get { return this.city; }
            set
            {
                Geopoint location;
                if (destinations.TryGetValue(value, out location))
                {
                    this.city = value;

                    // Radius:
                    // 500 - Seattle 20+ blocks
                    // 1000 - Seattle Downtown
                    // 10000 - Seattle Area
                    // 20000 - Puget Sound Area
                    var scene = MapScene.CreateFromLocationAndRadius(location, 500, 0, 45);
#pragma warning disable CS4014
                    this.map.TrySetSceneAsync(scene);
#pragma warning restore CS4014

                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsGlobeProjection
        {
            get { return this.mapProjection == MapProjection.Globe; }
            set
            {
                if (value)
                {
                    this.SetMapProjection(MapProjection.Globe);
                }
                else
                {
                    this.SetMapProjection(MapProjection.WebMercator);
                }
            }
        }

        public MapProjection MapProjection
        {
            get { return this.mapProjection; }
            set
            {
                SetMapProjection(value);
            }
        }

        public MapStyle MapStyle
        {
            get { return this.mapStyle; }
            set
            {
                if (this.mapStyle != value)
                {
                    this.mapStyle = value;
                    this.map.Style = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        private void SetMapProjection(MapProjection mapProjection)
        {
            if (this.mapProjection != mapProjection)
            {
                this.mapProjection = mapProjection;
                this.map.MapProjection = mapProjection;
                this.NotifyPropertyChanged(nameof(IsGlobeProjection));
            }
        }

        private string GetMapStyleSheetName(MapStyleSheet styleSheet)
        {
            if (styleSheet == MapStyleSheet.Aerial())
            {
                return "Aerial";
            }
            else if (styleSheet == MapStyleSheet.AerialWithOverlay())
            {
                return "AerialWithOverlay";
            }
            else if (styleSheet == MapStyleSheet.RoadDark())
            {
                return "RoadDark";
            }
            else if (styleSheet == MapStyleSheet.RoadLight())
            {
                return "RoadLight";
            }
            else if (styleSheet == MapStyleSheet.RoadHighContrastDark())
            {
                return "RoadHighContrastDark";
            }
            else if (styleSheet == MapStyleSheet.RoadHighContrastLight())
            {
                return "RoadHighContrastLight";
            }
            else
            {
                return "Custom";
            }
        }

        public ICommand DemoCommand { get; set; }
        public ICommand OpenFileCommand { get; set; }
    }
}
