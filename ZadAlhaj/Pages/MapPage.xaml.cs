using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Mapsui.Widgets;
using NetTopologySuite.Geometries;

namespace ZadAlhaj.Pages
{
    public partial class MapPage : ContentPage
    {
        private const string ErrorTitle = "Error";

        private ILayer? userLocationLayer;
        private bool _isMapInitialized = false;
        private Microsoft.Maui.Devices.Sensors.Location? _userLocation;
        private string? _selectedLocationName;

        public MapPage()
        {
            InitializeComponent();
            FlowDirection = Services.LocalizationService.GetFlowDirection();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            if (!_isMapInitialized)
            {
                await InitializeMapAsync();
                _isMapInitialized = true;
                _ = RequestLocationPermissionAndShowUserLocationAsync();
            }
            
            // Start compass
            if (Compass.Default.IsSupported)
            {
                if (!Compass.Default.IsMonitoring)
                {
                    Compass.Default.ReadingChanged += Compass_ReadingChanged;
                    Compass.Default.Start(SensorSpeed.UI);
                }
            }
        }

        private void Compass_ReadingChanged(object? sender, CompassChangedEventArgs e)
        {
            // Calculate Qibla bearing (approximate bearing from current location to Kaaba)
            // For simplicity, we use a fixed bearing if user location is unknown, 
            // or calculate it if we have the user's location.
            double qiblaBearing = 136.0; // Default approximate bearing from North America/Europe to Makkah
            
            if (_userLocation != null)
            {
                qiblaBearing = CalculateBearing(_userLocation.Latitude, _userLocation.Longitude, 21.4225, 39.8262);
            }

            // The compass reading is the device's heading relative to magnetic north.
            // We want the needle to point to the Qibla.
            // So we rotate the needle by (Qibla Bearing - Device Heading)
            double rotation = qiblaBearing - e.Reading.HeadingMagneticNorth;
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CompassNeedle.Rotation = rotation;
            });
        }

        private double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = (lon2 - lon1) * Math.PI / 180.0;
            lat1 = lat1 * Math.PI / 180.0;
            lat2 = lat2 * Math.PI / 180.0;

            var y = Math.Sin(dLon) * Math.Cos(lat2);
            var x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
            var brng = Math.Atan2(y, x);

            return (brng * 180.0 / Math.PI + 360.0) % 360.0;
        }

        private async Task InitializeMapAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    // Create the map on a background thread
                    var map = new Mapsui.Map();

                    // Add OpenStreetMap tile layer (no API key required!)
                    map.Layers.Add(OpenStreetMap.CreateTileLayer());

                    // Add Hajj route line layer
                    var routeLayer = CreateRouteLayer();
                    map.Layers.Add(routeLayer);

                    // Add Hajj location pins
                    var pinLayer = CreatePinLayer();
                    map.Layers.Add(pinLayer);

                    // Hide attribution widgets (top-left and bottom-right info boxes)
                    map.Widgets.Clear();

                    // Return to main thread to assign map
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (HajjMapControl != null)
                        {
                            HajjMapControl.Map = map;
                        }
                    });
                });

                // Center on Kaaba after map is loaded
                await Task.Delay(800); // Wait for map to initialize
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (HajjMapControl?.Map?.Navigator != null)
                        {
                            var kaabaLocation = SphericalMercator.FromLonLat(39.8262, 21.4225);
                            var mPoint = new MPoint(kaabaLocation.x, kaabaLocation.y);
                            HajjMapControl.Map.Navigator.CenterOn(mPoint);
                            HajjMapControl.Map.Navigator.ZoomTo(12); // Zoom level
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error centering map: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Map initialization error: {ex.Message}");
            }
        }

        private MemoryLayer CreateRouteLayer()
        {
            // Define Hajj route sequence (Kaaba -> Mina -> Arafat -> Muzdalifah -> Mina -> Kaaba)
            var routePoints = new[]
            {
                new { Lat = 21.4225, Lon = 39.8262 }, // Kaaba
                new { Lat = 21.412275901764442, Lon = 39.89094110039919 }, // Mina
                new { Lat = 21.3547, Lon = 39.9839 }, // Arafat
                new { Lat = 21.4069, Lon = 39.9375 }, // Muzdalifah
                new { Lat = 21.412275901764442, Lon = 39.89094110039919 }, // Mina (return)
                new { Lat = 21.4225, Lon = 39.8262 }  // Kaaba (return for Tawaf)
            };

            // Convert to map coordinates
            var coordinates = routePoints
                .Select(p => SphericalMercator.FromLonLat(p.Lon, p.Lat))
                .Select(coord => new Coordinate(coord.x, coord.y))
                .ToArray();

            // Create line string
            var lineString = new LineString(coordinates);

            // Create feature
            var feature = new GeometryFeature
            {
                Geometry = lineString
            };

            // Create line style
            feature.Styles.Add(new VectorStyle
            {
                Line = new Pen(Mapsui.Styles.Color.FromString("#E74C3C"), 5)
                {
                    PenStyle = PenStyle.Solid,
                    PenStrokeCap = PenStrokeCap.Round
                }
            });

            // Create memory layer
            var layer = new MemoryLayer
            {
                Name = "Hajj Route",
                Features = new[] { feature },
                Style = null
            };

            return layer;
        }

        private MemoryLayer CreatePinLayer()
        {
            // Define Hajj locations with colors matching the SVG pins
            var locations = new[]
            {
                new { Name = ZadAlhaj.Resources.Localization.AppResources.Kaaba, Lat = 21.4225, Lon = 39.8262, Color = "#E74C3C" }, // Red
                new { Name = ZadAlhaj.Resources.Localization.AppResources.Arafat, Lat = 21.3547, Lon = 39.9839, Color = "#3498DB" }, // Blue
                new { Name = ZadAlhaj.Resources.Localization.AppResources.Muzdalifah, Lat = 21.4069, Lon = 39.9375, Color = "#9B59B6" }, // Purple
                new { Name = ZadAlhaj.Resources.Localization.AppResources.Mina, Lat = 21.413052539985806, Lon = 39.89021580688235, Color = "#27AE60" }, // Green
                new { Name = ZadAlhaj.Resources.Localization.AppResources.SafaMarwa, Lat = 21.4233, Lon = 39.8266, Color = "#E67E22" } // Orange
            };

            var features = locations.Select(location =>
            {
                var point = SphericalMercator.FromLonLat(location.Lon, location.Lat);
                var feature = new PointFeature(point.ToMPoint());
                feature["name"] = location.Name;
                feature["lat"] = location.Lat;
                feature["lon"] = location.Lon;
                
                // Use Mapsui v5 pin style with custom color
                feature.Styles.Add(ImageStyles.CreatePinStyle(
                    fillColor: Mapsui.Styles.Color.FromString(location.Color),
                    symbolScale: 1.2
                ));
                
                return feature;
            }).ToArray();

            return new MemoryLayer
            {
                Name = "Hajj Locations",
                Features = features,
                Style = null
            };
        }

        private async Task RequestLocationPermissionAndShowUserLocationAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status == PermissionStatus.Granted)
                {
                    await ShowUserLocationAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Location permission error: {ex.Message}");
            }
        }

        private async Task ShowUserLocationAsync()
        {
            try
            {
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    // Store user location for ETA/distance calculations and compass
                    _userLocation = location;
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            // Remove existing user location layer if any
                            if (userLocationLayer != null && HajjMapControl?.Map?.Layers.Contains(userLocationLayer) == true)
                            {
                                HajjMapControl.Map.Layers.Remove(userLocationLayer);
                            }

                            // Create user location layer
                            userLocationLayer = CreateUserLocationLayer(location.Latitude, location.Longitude);
                            HajjMapControl?.Map?.Layers.Add(userLocationLayer);

                            // Refresh map
                            HajjMapControl?.Map?.Refresh();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error adding user location: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
            }
        }

        private MemoryLayer CreateUserLocationLayer(double lat, double lon)
        {
            // Convert lat/lon to map coordinates
            var point = SphericalMercator.FromLonLat(lon, lat);

            // Create feature using PointFeature
            var feature = new PointFeature(point.ToMPoint());
            feature["name"] = ZadAlhaj.Resources.Localization.AppResources.YourLocation;

            // Use blue circle for current location
            feature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 1.2,
                SymbolType = SymbolType.Ellipse,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromString("#2196F3")),
                Outline = new Pen(Mapsui.Styles.Color.White, 4),
                RelativeOffset = new RelativeOffset(0.0, 0.5)
            });

            // Create memory layer
            return new MemoryLayer
            {
                Name = "User Location", 
                Features = new[] { feature },
                Style = null
            };
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Stop compass monitoring
            if (Compass.Default.IsSupported && Compass.Default.IsMonitoring)
            {
                Compass.Default.ReadingChanged -= Compass_ReadingChanged;
                Compass.Default.Stop();
            }
            
            // Clean up map resources
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (HajjMapControl?.Map != null)
                    {
                        HajjMapControl.Map.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing map: {ex.Message}");
                }
            });
        }

        // ==================== Interactive Pin Popup ====================
        
        private void OnMapInfo(object? sender, MapInfoEventArgs e)
        {
            try
            {
                // Mapsui v5: use GetMapInfo with specific layers
                var hajjLayer = HajjMapControl?.Map?.Layers.FirstOrDefault(l => l.Name == "Hajj Locations");
                if (hajjLayer == null) return;
                
                var mapInfo = e.GetMapInfo(new[] { hajjLayer });
                
                if (mapInfo?.Feature != null)
                {
                    var feature = mapInfo.Feature;
                    var name = feature["name"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(name) && name != ZadAlhaj.Resources.Localization.AppResources.YourLocation)
                    {
                        _selectedLocationName = name;
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            PopupTitle.Text = name;
                            
                            // Calculate distance and ETA if user location is known
                            if (_userLocation != null && feature["lat"] != null && feature["lon"] != null)
                            {
                                var pinLat = Convert.ToDouble(feature["lat"]);
                                var pinLon = Convert.ToDouble(feature["lon"]);
                                
                                var distanceKm = Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(
                                    _userLocation.Latitude, _userLocation.Longitude,
                                    pinLat, pinLon,
                                    DistanceUnits.Kilometers);

                                if (distanceKm < 1)
                                {
                                    PopupDistance.Text = $"{distanceKm * 1000:F0} m";
                                }
                                else
                                {
                                    PopupDistance.Text = $"{distanceKm:F1} km";
                                }

                                // Average walking speed ~5 km/h
                                var etaHours = distanceKm / 5.0;
                                if (etaHours < 1)
                                {
                                    PopupETA.Text = $"{etaHours * 60:F0} min";
                                }
                                else
                                {
                                    PopupETA.Text = $"{etaHours:F1} hr";
                                }
                            }
                            else
                            {
                                PopupDistance.Text = "--";
                                PopupETA.Text = "--";
                            }
                            
                            LocationPopup.IsVisible = true;
                        });
                    }
                    else
                    {
                        // Clicked on empty area or user location - hide popup
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            LocationPopup.IsVisible = false;
                        });
                    }
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        LocationPopup.IsVisible = false;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling map info: {ex.Message}");
            }
        }

        private void OnClosePopupClicked(object? sender, EventArgs e)
        {
            LocationPopup.IsVisible = false;
        }

        private async void OnViewGuideClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedLocationName))
                return;

            try
            {
                // Try to find a category/subcategory matching the selected location
                var dataService = new Services.DataService();
                var categories = await dataService.GetCategoriesAsync();
                
                foreach (var category in categories)
                {
                    var matchingSub = category.Subcategories
                        .FirstOrDefault(s => s.Name.Contains(_selectedLocationName, StringComparison.OrdinalIgnoreCase)
                                          || _selectedLocationName.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingSub != null)
                    {
                        await Navigation.PushAsync(new ContentDetailPage(category, matchingSub));
                        return;
                    }
                }
                
                // If no exact match, inform the user
                await DisplayAlertAsync(
                    _selectedLocationName,
                    "No guide content found for this location.",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to guide: {ex.Message}");
                await DisplayAlertAsync(
                    ZadAlhaj.Resources.Localization.AppResources.Error ?? ErrorTitle,
                    $"Could not open guide for {_selectedLocationName}.",
                    "OK");
            }
        }

        private async void OnShareLocationClicked(object sender, EventArgs e)
        {
            try
            {
                // Check location permission
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync(
                        ZadAlhaj.Resources.Localization.AppResources.Error ?? ErrorTitle,
                        "Location permission is required to share your location.",
                        "OK");
                    return;
                }

                // Show loading indicator
                ShareLocationButton.IsEnabled = false;
                ShareLocationButton.Text = "📍 Getting Location...";

                // Get current location
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    // Create shareable location text
                    var locationText = $"My Current Location:\n" +
                                     $"Latitude: {location.Latitude:F6}\n" +
                                     $"Longitude: {location.Longitude:F6}\n" +
                                     $"Google Maps: https://maps.google.com/?q={location.Latitude},{location.Longitude}";

                    // Share the location
                    await Share.Default.RequestAsync(new ShareTextRequest
                    {
                        Text = locationText,
                        Title = "Share My Location"
                    });
                }
                else
                {
                    await DisplayAlertAsync(
                        ZadAlhaj.Resources.Localization.AppResources.Error ?? ErrorTitle,
                        "Unable to get your current location. Please try again.",
                        "OK");
                }
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlertAsync(
                    ZadAlhaj.Resources.Localization.AppResources.Error ?? ErrorTitle,
                    "Location sharing is not supported on this device.",
                    "OK");
            }
            catch (PermissionException)
            {
                await DisplayAlertAsync(
                    ZadAlhaj.Resources.Localization.AppResources.Error ?? ErrorTitle,
                    "Location permission was denied.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    ZadAlhaj.Resources.Localization.AppResources.Error ?? ErrorTitle,
                    $"Error sharing location: {ex.Message}",
                    "OK");
            }
            finally
            {
                // Restore button state
                ShareLocationButton.IsEnabled = true;
                ShareLocationButton.Text = "📍 Share Location";
            }
        }
    }
}
