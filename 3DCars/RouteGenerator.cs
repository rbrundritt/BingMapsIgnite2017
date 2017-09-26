using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace _3DCars
{
    public sealed partial class MainPage
    {
        private class RouteGenerator
        {
            private const int NumberOfWaypoints = 3;
            private readonly MapRouteOptimization DefaultRouteOptimization = MapRouteOptimization.Time;
            private readonly MapRouteRestrictions DefaultRouteRestrictions = MapRouteRestrictions.Highways;

            private static readonly Geopoint Aurora = new Geopoint(new BasicGeoposition() { Latitude = 47.620560, Longitude = -122.343736 });
            private static readonly Geopoint AuroraBridge = new Geopoint(new BasicGeoposition() { Latitude = 47.649755, Longitude = -122.347325 });
            private static readonly Geopoint SeattlePort = new Geopoint(new BasicGeoposition() { Latitude = 47.598753, Longitude = -122.339051 });

            private static readonly Geopoint SafecoField = new Geopoint(new BasicGeoposition() { Latitude = 47.590270, Longitude = -122.333240 });
            private static readonly Geopoint HuskyStadium = new Geopoint(new BasicGeoposition() { Latitude = 47.649075, Longitude = -122.302361 });

            private static readonly Geopoint I5Exit167n = new Geopoint(new BasicGeoposition() { Latitude = 47.619530, Longitude = -122.328210 });
            private static readonly Geopoint I5Exit166s = new Geopoint(new BasicGeoposition() { Latitude = 47.623065, Longitude = -122.328675 });

            private static readonly Geopoint I5Exit164n = new Geopoint(new BasicGeoposition() { Latitude = 47.590163, Longitude = -122.320816 });
            private static readonly Geopoint I5Exit164s = new Geopoint(new BasicGeoposition() { Latitude = 47.590137, Longitude = -122.321074 });

            private PointGenerator pointGenerator;

            public RouteGenerator(PointGenerator pointGenerator)
            {
                this.pointGenerator = pointGenerator;
            }

            public async Task<List<MapRoute>> GenerateRoutes(int n)
            {
                var routes = new List<MapRoute>();

                while (n > 0)
                {
                    var route = await NextRoute();
                    if (route != null)
                    {
                        routes.Add(route);
                        n--;
                    }
                }

                return routes;
            }

            public async Task<List<MapRoute>> GenerateHighwayRoutes()
            {
                var routes = new List<MapRoute>();

                var result = await MapRouteFinder.GetDrivingRouteAsync(Aurora, SeattlePort);
                if (result.Status == MapRouteFinderStatus.Success)
                {
                    routes.Add(result.Route);
                }

                result = await MapRouteFinder.GetDrivingRouteAsync(SafecoField, Aurora);
                if (result.Status == MapRouteFinderStatus.Success)
                {
                    routes.Add(result.Route);
                }

                result = await MapRouteFinder.GetDrivingRouteAsync(HuskyStadium, SafecoField);
                if (result.Status == MapRouteFinderStatus.Success)
                {
                    routes.Add(result.Route);
                }

                result = await MapRouteFinder.GetDrivingRouteAsync(I5Exit166s, I5Exit164s);
                if (result.Status == MapRouteFinderStatus.Success)
                {
                    routes.Add(result.Route);
                }

                result = await MapRouteFinder.GetDrivingRouteAsync(I5Exit164n, I5Exit167n);
                if (result.Status == MapRouteFinderStatus.Success)
                {
                    routes.Add(result.Route);
                }

                result = await MapRouteFinder.GetDrivingRouteAsync(I5Exit166s, SafecoField);
                if (result.Status == MapRouteFinderStatus.Success)
                {
                    routes.Add(result.Route);
                }

                return routes;
            }

            private async Task<MapRoute> NextRoute()
            {
                var routeOptions = new MapRouteDrivingOptions
                {
                    InitialHeading = 0,
                    RouteOptimization = DefaultRouteOptimization,
                    RouteRestrictions = DefaultRouteRestrictions,
                };
                var waypoints = GenerateWaypoints(NumberOfWaypoints);

                var result = await MapRouteFinder.GetDrivingRouteFromEnhancedWaypointsAsync(waypoints, routeOptions);
                if (result.Status != MapRouteFinderStatus.Success)
                {
                    System.Diagnostics.Debug.WriteLine("Error: Cannot generate route (" + result.Status.ToString() + ").");
                    return null;
                }

                return result.Route;
            }

            private List<EnhancedWaypoint> GenerateWaypoints(int n)
            {
                var waypoints = new List<EnhancedWaypoint>();

                var start = pointGenerator.Next();
                waypoints.Add(new EnhancedWaypoint(start, WaypointKind.Stop));

                for (int i = 0; i < n; ++i)
                {
                    var point = pointGenerator.Next();
                    var waypoint = new EnhancedWaypoint(point, WaypointKind.Via);
                }

                var stop = pointGenerator.Next();
                waypoints.Add(new EnhancedWaypoint(stop, WaypointKind.Stop));

                return waypoints;
            }
        }
    }
}
