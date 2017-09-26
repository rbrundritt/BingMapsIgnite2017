using System.Collections.Generic;
using System.Windows.Input;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;

namespace _3DCars
{
    public class MainPageViewModel : ViewModel
    {
        private MapControl map;
        private DispatcherTimer timer;
        private TextBlock status;

        private string city;
        private MapProjection mapProjection = MapProjection.WebMercator;
        private MapStyle mapStyle = MapStyle.Road;

        private bool showingMapToolbar;
        private bool showingRoutes;

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

        public void Connect(MapControl map, DispatcherTimer timer, TextBlock status)
        {
            this.map = map;
            this.timer = timer;
            this.status = status;
        }

        public void DebugOutputStatus()
        {
            this.status.Text =
                "\nProjection= " + this.map.MapProjection +
                "\nStyle= " + this.map.Style +
                "\nStyleSheet= " + GetMapStyleSheetName(this.map.StyleSheet);
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

        public bool CanPlay
        {
            get { return !this.timer.IsEnabled;  }
            set
            {
                if (this.timer.IsEnabled == value)
                {
                    if (!this.timer.IsEnabled)
                    {
                        this.timer.Start();
                        this.NotifyPropertyChanged(nameof(CanPause));
                        this.NotifyPropertyChanged(nameof(CanStop));
                    }
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool CanPause
        {
            get { return this.timer.IsEnabled; }
            set
            {
                if (this.timer.IsEnabled != value)
                {
                    if (value)
                    {
                        this.timer.Start();
                        this.NotifyPropertyChanged(nameof(CanPlay));
                        this.NotifyPropertyChanged(nameof(CanStop));
                    }
                    else
                    {
                        this.timer.Stop();
                        this.NotifyPropertyChanged(nameof(CanPlay));
                    }
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool CanStop
        {
            get { return this.timer.IsEnabled; }
            set
            {
                if (this.timer.IsEnabled != value)
                {
                    if (this.timer.IsEnabled)
                    {
                        this.timer.Stop();
                        this.NotifyPropertyChanged(nameof(CanPause));
                        this.NotifyPropertyChanged(nameof(CanPlay));
                    }
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool IsSymbolicMap
        {
            get { return this.mapStyle == MapStyle.Road; }
            set
            {
                if (value)
                {
                    SetMapMode(MapStyle.Road);
                }
                else
                {
                    SetMapMode(MapStyle.AerialWithRoads);
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

        public bool IsShowingRoutes
        {
            get { return this.showingRoutes;  }
            set
            {
                if (this.showingRoutes != value)
                {
                    this.showingRoutes = value;
                    this.NotifyPropertyChanged();
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

        public bool MapToolbarEnabled
        {
            get { return this.showingMapToolbar; }
            set
            {
                if (this.showingMapToolbar != value)
                {
                    this.showingMapToolbar = value;

                    var mode = value ?
                        MapInteractionMode.PointerKeyboardAndControl :
                        MapInteractionMode.Auto;
                    this.map.RotateInteractionMode = mode;
                    this.map.TiltInteractionMode = mode;
                    this.map.ZoomInteractionMode = mode;
                }
            }
        }

        public void UpdateControls()
        {
            NotifyPropertyChanged(nameof(CanPlay));
            NotifyPropertyChanged(nameof(CanPause));
            NotifyPropertyChanged(nameof(CanStop));
        }

        private void SetMapMode(MapStyle mode)
        {
            if (this.mapStyle != mode)
            {
                this.mapStyle = mode;

                var stylesheet = (mode == MapStyle.Road) ?
                    MapStyleSheet.RoadLight() :
                    MapStyleSheet.AerialWithOverlay();
                this.map.StyleSheet = stylesheet;
                this.NotifyPropertyChanged(nameof(IsSymbolicMap));
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
        public ICommand DemoClearCommand { get; set; }
    }
}
