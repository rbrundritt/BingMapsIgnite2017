using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Services.Maps;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace _3DCars
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private class CarsToRender
        {
            public List<Car> Cars;
            public List<Car> Trucks;
            public List<MapRoute> Routes;
            public List<MapRoute> HighwayRoutes;
        }

        private string AssemblyName { get; set; }
        private string ResourcesRootPath { get; set; }

        private DispatcherTimer Timer;

        private new CarResources Resources;
        private CarsToRender SimulatedTraffic;

        public MainPageViewModel ViewModel;

        private const int NumberOfRoutes = 20;
        private const int UpdatesPerSecond = 60;

        private const string ServiceToken = "hUikWnph4qESMvOtIn4C~EBknoIBYklzUqS0Di0rttWFC4sRJzjq0U993LpJzV6S5yap2C8GrmSIPOgaHN1Wht0RpbcGXJDzvpt0Ny6pHlw~Al-HNAp4OV2It6cySlnSag3t4eIltWHCqBHgodIbhYAlCyRRwNYR_NU12j_v9hwb";

        public MainPage()
        {
            this.AssemblyName = Windows.ApplicationModel.Package.Current.DisplayName;
            this.ResourcesRootPath = AssemblyName + ".Resources.";

            this.InitializeComponent();

            MapService.ServiceToken = ServiceToken;
            MyMap.MapServiceToken = ServiceToken;

            this.Resources = CarResources.Instance;
            this.SimulatedTraffic = new CarsToRender();

            this.Timer = new DispatcherTimer();
            this.Timer.Tick += Timer_Tick;

            this.ViewModel = new MainPageViewModel();
            this.ViewModel.Connect(MyMap, Timer, Status);
            this.ViewModel.DemoCommand = new DelegateCommand(this.RunDemo);
            this.ViewModel.DemoClearCommand = new DelegateCommand(this.ResetDemo);
            this.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            this.ViewModel.MapToolbarEnabled = true;
        }

        private async void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.City = "Seattle";

            DemoButton.IsEnabled = false;
            await this.Resources.LoadAsync(CarResources.Models.All);
            DemoButton.IsEnabled = true;
        }

        private void MyMap_MapContextRequested(MapControl sender, MapContextRequestedEventArgs args)
        {
            contextMenu.ShowAt(sender, args.Position);
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainPageViewModel.CanPlay))
            {
                // If play cannot longer be pressed, see if the Demo needs to be kickstarted.
                if (!this.ViewModel.CanPlay &&
                    SimulatedTraffic.Cars == null)
                {
                    RunDemo();
                }
            }
            else if (e.PropertyName == nameof(MainPageViewModel.IsShowingRoutes))
            {
                if (this.ViewModel.IsShowingRoutes)
                {
                    DisplayRoutes();
                }
                else
                {
                    ClearRoutes();
                }
            }
        }

        private void GoToMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var frameworkElement = (sender as FrameworkElement);
            if (frameworkElement != null)
            {
                ViewModel.City = frameworkElement.Tag.ToString();
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            if (!this.Timer.IsEnabled) return;
#if PRINT_TIMESTAMP
            System.Diagnostics.Debug.WriteLine("Tick: " + DateTime.Now.TimeOfDay);
#endif

            if (SimulatedTraffic.Cars != null)
            {
                SimulatedTraffic.Cars.ForEach(car => car.Update());
            }

            if (SimulatedTraffic.Trucks != null)
            {
                SimulatedTraffic.Trucks.ForEach(truck => truck.Update());
            }
        }

        private void ResetDemo()
        {
            this.Timer.Stop();

            ClearCars();
            this.ViewModel.IsShowingRoutes = false;
            this.ViewModel.UpdateControls();
        }

        private async void RunDemo()
        {
            this.Timer.Stop();

            PlayButton.IsEnabled = false;
            PauseButton.IsEnabled = false;
            StopButton.IsEnabled = false;

            ClearCars();
            this.ViewModel.IsShowingRoutes = false;
            this.ViewModel.UpdateControls();

            var region = this.MyMap.GetVisibleRegion(MapVisibleRegionKind.Near);

            var pointGenerator = new PointGenerator();
            pointGenerator.Boundaries = region;

            var routeGenerator = new RouteGenerator(pointGenerator);

            var carModels = new List<string>()
            {
                Car.HammerheadRedUri,
                Car.HammerheadWhiteUri,
                Car.ShiftBlueUri,
                Car.ShiftGoldUri,
            };
            int model = 0;

            SimulatedTraffic.Cars = new List<Car>();
            SimulatedTraffic.Routes = await routeGenerator.GenerateRoutes(NumberOfRoutes);
            foreach (var route in SimulatedTraffic.Routes)
            {
                //var car = new PoliceCar();
                //var car = new SchoolBus();
                var car = new Car(carModels.ElementAt(model++ % carModels.Count));
                car.Connect(this.MyMap, route);
                SimulatedTraffic.Cars.Add(car);
            }

            SimulatedTraffic.Trucks = new List<Car>();
            SimulatedTraffic.HighwayRoutes = await routeGenerator.GenerateHighwayRoutes();
            foreach (var route in SimulatedTraffic.HighwayRoutes)
            {
                var truck = new SemiTruck();
                truck.Connect(this.MyMap, route);
                SimulatedTraffic.Trucks.Add(truck);

                var dumptruck = new DumpTruck(40);
                dumptruck.Connect(this.MyMap, route);
                SimulatedTraffic.Trucks.Add(dumptruck);

                var car = new Car(55, carModels.ElementAt(model++ % carModels.Count));
                car.Connect(this.MyMap, route);
                SimulatedTraffic.Cars.Add(car);
            }

            // Set the interval to 16ms (60 ticks / sec)
            this.Timer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / UpdatesPerSecond);
            this.Timer.Start();

            this.ViewModel.UpdateControls();

            PlayButton.IsEnabled = true;
            PauseButton.IsEnabled = true;
            StopButton.IsEnabled = true;
        }

        private void ClearCars()
        {
            this.Timer.Stop();

            if (SimulatedTraffic.Cars != null)
            {
                SimulatedTraffic.Cars.ForEach(car => car.Clear());
                SimulatedTraffic.Cars.Clear();
                SimulatedTraffic.Cars = null;
            }

            if (SimulatedTraffic.Trucks != null)
            {
                SimulatedTraffic.Trucks.ForEach(truck => truck.Clear());
                SimulatedTraffic.Trucks.Clear();
                SimulatedTraffic.Trucks = null;
            }
        }

        private void ClearRoutes()
        {
            this.MyMap.Routes.Clear();
        }

        private void DisplayRoutes()
        {
            if (SimulatedTraffic.Routes == null) return;

            var routeColors = new List<Windows.UI.Color>()
            {
                Windows.UI.Colors.CornflowerBlue,
                Windows.UI.Colors.Gold,
                Windows.UI.Colors.Lavender,
                Windows.UI.Colors.LavenderBlush,
                Windows.UI.Colors.LightBlue,
                Windows.UI.Colors.LightGray,
                Windows.UI.Colors.LightSkyBlue,
                Windows.UI.Colors.LightSteelBlue,
                Windows.UI.Colors.Orange,
                Windows.UI.Colors.SandyBrown,
                Windows.UI.Colors.Thistle,
                Windows.UI.Colors.YellowGreen,
            };

            int i = 0;
            foreach (var route in SimulatedTraffic.Routes)
            {
                var routeView = new MapRouteView(route);
                routeView.RouteColor = routeColors.ElementAt(i++ % routeColors.Count);
                this.MyMap.Routes.Add(routeView);
            }
        }
    }
}
