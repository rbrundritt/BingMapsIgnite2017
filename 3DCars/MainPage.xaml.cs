using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Services.Maps;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;

namespace _3DCars
{
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

        private const string ServiceToken = "Your_Bing_Maps_Key";

        private const int UpdatesPerSecond = 60;

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
            this.ViewModel.Connect(MyMap, Status);
            this.ViewModel.EnableToolbar();
            this.ViewModel.DemoCommand = new DelegateCommand(this.RunDemo);
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
            foreach (var car in SimulatedTraffic.Cars)
            {
                car.Update();
            }

            foreach (var truck in SimulatedTraffic.Trucks)
            {
                truck.Update();
            }
        }

        private async void RunDemo()
        {
            var region = this.MyMap.GetVisibleRegion(MapVisibleRegionKind.Near);

            var pointGenerator = new PointGenerator();
            pointGenerator.Boundaries = region;

            var routeGenerator = new RouteGenerator(pointGenerator);

            SimulatedTraffic.Cars = new List<Car>();
            SimulatedTraffic.Routes = await routeGenerator.GenerateRoutes(20);
            foreach (var route in SimulatedTraffic.Routes)
            {
                var car = new SchoolBus();
                //var car = new Car();
                //var car = new PoliceCar();
                //var car = new DumpTruck();
                //var car = new SemiTruck();
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
            }

            DisplayRoutes();

            // Set the interval to 16ms (60 ticks / sec)
            this.Timer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / UpdatesPerSecond);
            this.Timer.Start();
        }

        private void ClearRoutes()
        {
            this.MyMap.Routes.Clear();
        }

        private void DisplayRoutes()
        {
            var routeColors = new List<Windows.UI.Color>()
            {
                //Windows.UI.Colors.Aquamarine,
                //Windows.UI.Colors.Black,
                //Windows.UI.Colors.BlueViolet,
                //Windows.UI.Colors.Coral,
                //Windows.UI.Colors.DarkSeaGreen,

                Windows.UI.Colors.CornflowerBlue,
                Windows.UI.Colors.Gold,
                Windows.UI.Colors.Lavender,
                Windows.UI.Colors.LavenderBlush,
                Windows.UI.Colors.LightBlue,
                Windows.UI.Colors.LightGray,
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
