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
using KhayratAlhaj.Resources.Localization;

namespace KhayratAlhaj.Pages
{
    public partial class MapPage : ContentPage
    {
        private ILayer? userLocationLayer;
        private ILayer? _routeLayer;
        private bool _isMapInitialized = false;
        private Microsoft.Maui.Devices.Sensors.Location? _userLocation;
        private string? _selectedLocationName;
        private string? _selectedLocationKey;
        private string _routeProfile = "foot";
        private readonly Services.RoutingService _routingService = new();

        // Cached guide data for Read More navigation
        private Models.Category? _guideCategory;
        private Models.SubCategory? _guideSubCategory;

        // Direct mapping: locationKey -> (CategoryId, SubCategoryId)
        private static readonly Dictionary<string, (int CatId, int SubId)> LocationGuideMap = new()
        {
            { "kaaba",       (3, 303) },
            { "arafat",      (3, 302) },
            { "muzdalifah",  (4, 401) },
            { "mina",        (4, 402) },
            { "safa",        (3, 304) },
            { "marwa",       (3, 304) }
        };

        // Location key -> description resource
        private static string GetLocationDescription(string key) => key switch
        {
            "kaaba"      => AppResources.KaabaDesc,
            "arafat"     => AppResources.ArafatDesc,
            "muzdalifah" => AppResources.MuzdalifahDesc,
            "mina"       => AppResources.MinaDesc,
            "safa"       => AppResources.SafaDesc,
            "marwa"      => AppResources.MarwaDesc,
            _            => string.Empty
        };

        // Hajj waypoints for routing
        private static readonly List<(double Lat, double Lon)> HajjWaypoints = new()
        {
            (21.4225, 39.8262),   // Kaaba
            (21.412275901764442, 39.89094110039919), // Mina
            (21.354070042012918, 39.98500670973953),   // Arafat
            (21.392408688619156, 39.91010931758093),   // Muzdalifah
            (21.413052539985806, 39.89021580688235), // Mina (return)
            (21.4225, 39.8262)    // Kaaba (return)
        };

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
                _ = LoadOsrmRouteAsync(); // Load OSRM route after map init
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

                    // Add Hajj route line layer (will be replaced by OSRM route)
                    _routeLayer = CreateFallbackRouteLayer();
                    map.Layers.Add(_routeLayer);

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

        private MemoryLayer CreateFallbackRouteLayer()
        {
            // Straight-line fallback route (used when OSRM is unavailable)
            var coordinates = HajjWaypoints
                .Select(p => SphericalMercator.FromLonLat(p.Lon, p.Lat))
                .Select(coord => new Coordinate(coord.x, coord.y))
                .ToArray();

            var lineString = new LineString(coordinates);
            var feature = new GeometryFeature { Geometry = lineString };

            feature.Styles.Add(new VectorStyle
            {
                Line = new Pen(Mapsui.Styles.Color.FromString("#3498DB"), 4)
                {
                    PenStyle = PenStyle.Dash,
                    PenStrokeCap = PenStrokeCap.Round
                }
            });

            return new MemoryLayer
            {
                Name = "Hajj Route",
                Features = new[] { feature },
                Style = null
            };
        }

        private MemoryLayer CreateOsrmRouteLayer(List<(double Lat, double Lon)> routeCoords, bool isDriving)
        {
            var coordinates = routeCoords
                .Select(p => SphericalMercator.FromLonLat(p.Lon, p.Lat))
                .Select(coord => new Coordinate(coord.x, coord.y))
                .ToArray();

            var lineString = new LineString(coordinates);
            var feature = new GeometryFeature { Geometry = lineString };

            // Walking = dashed blue, Driving = solid green
            feature.Styles.Add(new VectorStyle
            {
                Line = new Pen(
                    Mapsui.Styles.Color.FromString(isDriving ? "#27AE60" : "#3498DB"), 4)
                {
                    PenStyle = isDriving ? PenStyle.Solid : PenStyle.Dash,
                    PenStrokeCap = PenStrokeCap.Round
                }
            });

            return new MemoryLayer
            {
                Name = "Hajj Route",
                Features = new[] { feature },
                Style = null
            };
        }

        private MemoryLayer CreatePinLayer()
        {
            // Define Hajj locations with colors and location keys for guide mapping
            var locations = new[]
            {
                new { Name = AppResources.Kaaba, Key = "kaaba", Lat = 21.4225, Lon = 39.8262, Color = "#E74C3C" }, // Red
                new { Name = AppResources.Arafat, Key = "arafat", Lat = 21.354070042012918, Lon = 39.98500670973953, Color = "#3498DB" }, // Blue
                new { Name = AppResources.Muzdalifah, Key = "muzdalifah", Lat = 21.392408688619156, Lon = 39.91010931758093, Color = "#9B59B6" }, // Purple
                new { Name = AppResources.Mina, Key = "mina", Lat = 21.413052539985806, Lon = 39.89021580688235, Color = "#27AE60" }, // Green
                new { Name = AppResources.Safa, Key = "safa", Lat = 21.421814, Lon = 39.827207, Color = "#E67E22" }, // Orange
                new { Name = AppResources.Marwa, Key = "marwa", Lat = 21.424796, Lon = 39.827194, Color = "#1ABC9C" } // Teal
            };

            var features = locations.Select(location =>
            {
                var point = SphericalMercator.FromLonLat(location.Lon, location.Lat);
                var feature = new PointFeature(point.ToMPoint());
                feature["name"] = location.Name;
                feature["locationKey"] = location.Key;
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
            feature["name"] = AppResources.YourLocation;

            // Semi-transparent accuracy ring
            feature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 2.0,
                SymbolType = SymbolType.Ellipse,
                Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(33, 150, 243, 40)), // Blue with low opacity
                Outline = new Pen(new Mapsui.Styles.Color(33, 150, 243, 80), 1),
            });

            // Pilgrim-style user marker with layered styles
            AddFallbackPilgrimStyle(feature);

            // Create memory layer
            return new MemoryLayer
            {
                Name = "User Location", 
                Features = new[] { feature },
                Style = null
            };
        }

        private static void AddFallbackPilgrimStyle(PointFeature feature)
        {
            // Pilgrim icon using Mapsui v5 ImageStyle with embedded SVG resource
            feature.Styles.Add(new ImageStyle
            {
                Image = new Mapsui.Styles.Image
                {
                    Source = "embedded://KhayratAlhaj.Resources.Images.pilgrim_location.svg",
                },
                SymbolScale = 0.8,
                RelativeOffset = new RelativeOffset(0.0, 0.35),
            });
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
            
            // Do NOT dispose the map here — OnDisappearing fires when pushing
            // a new page (e.g. Read More → ContentDetailPage). Disposing the map
            // would leave it blank when the user navigates back, because
            // _isMapInitialized prevents re-initialization in OnAppearing.
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
                    var locationKey = feature["locationKey"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(name) && name != AppResources.YourLocation)
                    {
                        _selectedLocationName = name;
                        _selectedLocationKey = locationKey;
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            // Reset expanded guide state when opening a new pin
                            ResetGuidePopupState();
                            
                            PopupTitle.Text = name;
                            
                            // Set location description
                            PopupDescription.Text = !string.IsNullOrEmpty(locationKey) 
                                ? GetLocationDescription(locationKey) 
                                : string.Empty;
                            
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
            ResetGuidePopupState();
        }

        private async void OnViewGuideClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedLocationKey))
                return;

            try
            {
                // Use direct location-to-category ID mapping
                if (LocationGuideMap.TryGetValue(_selectedLocationKey, out var guideRef))
                {
                    var dataService = new Services.DataService();
                    var categories = await dataService.GetCategoriesAsync();
                    
                    var category = categories.FirstOrDefault(c => c.Id == guideRef.CatId);
                    var subCategory = category?.Subcategories.FirstOrDefault(s => s.Id == guideRef.SubId);
                    
                    if (category != null && subCategory != null)
                    {
                        // Store for Read More navigation
                        _guideCategory = category;
                        _guideSubCategory = subCategory;

                        // Truncate content for summary preview
                        var content = subCategory.Content ?? string.Empty;
                        var summary = content.Length > 200
                            ? content[..200] + "\u2026"
                            : content;

                        // Show summary + Read More, hide View Guide button
                        GuideSummaryLabel.Text = summary;
                        GuideSummaryLabel.IsVisible = true;
                        ReadMoreButton.IsVisible = true;
                        ViewGuideButton.IsVisible = false;
                        return;
                    }
                }
                
                // If no mapping found, inform the user
                await DisplayAlertAsync(
                    _selectedLocationName ?? string.Empty,
                    AppResources.NoGuideContentFound,
                    AppResources.OK);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading guide: {ex.Message}");
                await DisplayAlertAsync(
                    AppResources.Error,
                    $"{AppResources.CouldNotOpenGuide} {_selectedLocationName}",
                    AppResources.OK);
            }
        }

        private async void OnReadMoreClicked(object? sender, EventArgs e)
        {
            if (_guideCategory != null && _guideSubCategory != null)
            {
                await Navigation.PushAsync(new ContentDetailPage(_guideCategory, _guideSubCategory));
            }
        }

        private void ResetGuidePopupState()
        {
            GuideSummaryLabel.IsVisible = false;
            ReadMoreButton.IsVisible = false;
            ViewGuideButton.IsVisible = true;
            _guideCategory = null;
            _guideSubCategory = null;
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
                        AppResources.Error,
                        AppResources.LocationPermissionRequired,
                        AppResources.OK);
                    return;
                }

                // Show loading indicator
                ShareLocationButton.IsEnabled = false;
                ShareLocationButton.Text = AppResources.GettingLocation;

                // Get current location
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    // Create shareable location text
                    var locationText = $"{AppResources.MyCurrentLocation}:\n" +
                                     $"{AppResources.Latitude}: {location.Latitude:F6}\n" +
                                     $"{AppResources.Longitude}: {location.Longitude:F6}\n" +
                                     $"Google Maps: https://www.google.com/maps/place/{location.Latitude},{location.Longitude}";

                    // Share the location
                    await Share.Default.RequestAsync(new ShareTextRequest
                    {
                        Text = locationText,
                        Title = AppResources.ShareMyLocation
                    });
                }
                else
                {
                    await DisplayAlertAsync(
                        AppResources.Error,
                        AppResources.UnableToGetLocation,
                        AppResources.OK);
                }
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlertAsync(
                    AppResources.Error,
                    AppResources.LocationSharingNotSupported,
                    AppResources.OK);
            }
            catch (PermissionException)
            {
                await DisplayAlertAsync(
                    AppResources.Error,
                    AppResources.LocationPermissionDenied,
                    AppResources.OK);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    AppResources.Error,
                    $"{AppResources.ErrorSharingLocation}: {ex.Message}",
                    AppResources.OK);
            }
            finally
            {
                // Restore button state
                ShareLocationButton.IsEnabled = true;
                ShareLocationButton.Text = AppResources.ShareLocation;
            }
        }

        // ==================== Route Toggle ====================

        private async void OnWalkingClicked(object? sender, EventArgs e)
        {
            if (_routeProfile == "foot") return;
            _routeProfile = "foot";
            UpdateRouteToggleUI();
            await LoadOsrmRouteAsync();
        }

        private async void OnDrivingClicked(object? sender, EventArgs e)
        {
            if (_routeProfile == "car") return;
            _routeProfile = "car";
            UpdateRouteToggleUI();
            await LoadOsrmRouteAsync();
        }

        private void UpdateRouteToggleUI()
        {
            var isWalking = _routeProfile == "foot";
            WalkingButton.BackgroundColor = isWalking
                ? Microsoft.Maui.Graphics.Color.FromArgb("#3498DB")
                : Colors.Transparent;
            WalkingButton.TextColor = isWalking
                ? Colors.White
                : Microsoft.Maui.Graphics.Color.FromArgb("#7F8C8D");
            DrivingButton.BackgroundColor = !isWalking
                ? Microsoft.Maui.Graphics.Color.FromArgb("#27AE60")
                : Colors.Transparent;
            DrivingButton.TextColor = !isWalking
                ? Colors.White
                : Microsoft.Maui.Graphics.Color.FromArgb("#7F8C8D");
        }

        private async Task LoadOsrmRouteAsync()
        {
            try
            {
                var result = await _routingService.GetRouteAsync(HajjWaypoints, _routeProfile);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        // Remove old route layer
                        if (_routeLayer != null && HajjMapControl?.Map?.Layers.Contains(_routeLayer) == true)
                        {
                            HajjMapControl.Map.Layers.Remove(_routeLayer);
                        }

                        if (result != null && result.Coordinates.Count > 1)
                        {
                            // Create route layer from OSRM geometry
                            _routeLayer = CreateOsrmRouteLayer(result.Coordinates, _routeProfile == "car");
                            HajjMapControl?.Map?.Layers.Insert(1, _routeLayer); // Insert above tile layer but below pins

                            // Show route info
                            RouteDistanceLabel.Text = result.FormattedDistance;
                            RouteETALabel.Text = $"{result.FormattedDuration} {(_routeProfile == "foot" ? AppResources.Walking : AppResources.Driving)}";
                            RouteInfoPanel.IsVisible = true;
                        }
                        else
                        {
                            // Fallback to straight-line route
                            _routeLayer = CreateFallbackRouteLayer();
                            HajjMapControl?.Map?.Layers.Insert(1, _routeLayer);
                            RouteInfoPanel.IsVisible = false;

                            // Show offline warning
                            _ = DisplayAlertAsync(
                                AppResources.RouteInfo,
                                AppResources.OfflineRouteWarning,
                                AppResources.OK);
                        }

                        HajjMapControl?.Map?.Refresh();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating route layer: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OSRM route loading error: {ex.Message}");
                
                // Fallback
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_routeLayer != null && HajjMapControl?.Map?.Layers.Contains(_routeLayer) == true)
                    {
                        HajjMapControl.Map.Layers.Remove(_routeLayer);
                    }
                    _routeLayer = CreateFallbackRouteLayer();
                    HajjMapControl?.Map?.Layers.Insert(1, _routeLayer);
                    RouteInfoPanel.IsVisible = false;
                    HajjMapControl?.Map?.Refresh();
                });
            }
        }

        private async void OnMyLocationClicked(object? sender, EventArgs e)
        {
            try
            {
                MyLocationButton.IsEnabled = false;
                await RequestLocationPermissionAndShowUserLocationAsync();

                if (_userLocation != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            if (HajjMapControl?.Map?.Navigator != null)
                            {
                                var userPoint = SphericalMercator.FromLonLat(
                                    _userLocation.Longitude, _userLocation.Latitude);
                                var mPoint = new MPoint(userPoint.x, userPoint.y);
                                HajjMapControl.Map.Navigator.CenterOn(mPoint);
                                HajjMapControl.Map.Navigator.ZoomTo(16);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error centering on user location: {ex.Message}");
                        }
                    });
                }
                else
                {
                    await DisplayAlertAsync(
                        AppResources.UnableToGetLocation,
                        AppResources.LocationPermissionRequired,
                        "OK");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"My location error: {ex.Message}");
            }
            finally
            {
                MyLocationButton.IsEnabled = true;
            }
        }
    }
}
