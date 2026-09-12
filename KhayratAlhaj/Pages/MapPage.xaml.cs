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
        private ILayer? _selectedPointLayer;
        private ImageStyle? _userLocationImageStyle;
        private double _currentHeading;
        private ILayer? _routeLayer;
        private ILayer? _userRouteLayer;
        private bool _isMapInitialized = false;
        private Microsoft.Maui.Devices.Sensors.Location? _userLocation;
        private string? _selectedLocationName;
        private string? _selectedLocationKey;
        private (double Lat, double Lon, string Name)? _directionsTarget;
        private (double Lat, double Lon)? _routeOrigin;
        private string _userRouteProfile = "foot";
        private bool _showResetAfterGetDirections;
        private readonly Services.RoutingService _routingService = new();

        // Cached guide data for Read More navigation
        private Models.Category? _guideCategory;
        private Models.SubCategory? _guideSubCategory;

        // Direct mapping: locationKey -> (CategoryId, SubCategoryId)
        // Aligned with the current categories.json structure: Category 2 "Hajj Rituals"
        // owns subcategories 201-208 covering the rites in chronological order.
        private static readonly Dictionary<string, (int CatId, int SubId)> LocationGuideMap = new()
        {
            { "kaaba",       (2, 202) }, // Tawaf and Sa'i
            { "arafat",      (2, 204) }, // Standing at Arafat
            { "muzdalifah",  (2, 205) }, // Stopping at Muzdalifah
            { "mina",        (2, 203) }, // Day of Tarwiyah (heading to Mina)
            { "safa",        (2, 202) }, // Tawaf and Sa'i
            { "marwa",       (2, 202) }  // Tawaf and Sa'i
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
            GetDirectionsButton.Text = AppResources.GetDirections;
            UpdateRouteToggleUI();
            UpdateResetDirectionsButtonVisibility();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            FlowDirection = Services.LocalizationService.GetFlowDirection();
            GetDirectionsButton.Text = AppResources.GetDirections;
            
            if (!_isMapInitialized)
            {
                await InitializeMapAsync();
                _isMapInitialized = true;
                
                // Defer location requests with a delay to avoid blocking main thread
                // during other critical operations like Quran loading
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500); // Give main thread time to process other events
                    await RequestLocationPermissionAndShowUserLocationAsync();
                    await LoadOsrmRouteAsync(); // Load OSRM route after location is ready
                });
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
            _currentHeading = e.Reading.HeadingMagneticNorth;

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

                if (_userLocationImageStyle != null)
                {
                    // Keep the location icon aligned with device orientation.
                    _userLocationImageStyle.SymbolRotation = _currentHeading;
                    HajjMapControl?.Map?.Refresh();
                }
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
                    // OSM tile usage policy requires a unique, identifying User-Agent
                    // or requests are blocked with 403 (osm.wiki/blocked).
                    map.Layers.Add(OpenStreetMap.CreateTileLayer(
                        userAgent: "KhayratAlhaj/1.0 (+https://github.com/khayrat-alhaj; hajj-guide-app)"));

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

        private MemoryLayer CreateUserRouteLayer(List<(double Lat, double Lon)> routeCoords, bool isDriving)
        {
            var coordinates = routeCoords
                .Select(p => SphericalMercator.FromLonLat(p.Lon, p.Lat))
                .Select(coord => new Coordinate(coord.x, coord.y))
                .ToArray();

            var lineString = new LineString(coordinates);
            var feature = new GeometryFeature { Geometry = lineString };

            feature.Styles.Add(new VectorStyle
            {
                Line = new Pen(
                    Mapsui.Styles.Color.FromString(isDriving ? "#F39C12" : "#E91E63"), 5)
                {
                    PenStyle = isDriving ? PenStyle.Solid : PenStyle.Dash,
                    PenStrokeCap = PenStrokeCap.Round
                }
            });

            return new MemoryLayer
            {
                Name = "User Directions",
                Features = new[] { feature },
                Style = null
            };
        }

        // Known Hajj locations shown as pins and used as reference points for
        // distance calculations from any point the user selects on the map.
        private readonly record struct HajjLocation(string Name, string Key, double Lat, double Lon, string Color);

        private static IEnumerable<HajjLocation> GetKnownLocations() => new[]
        {
            new HajjLocation(AppResources.Kaaba, "kaaba", 21.4225, 39.8262, "#E74C3C"), // Red
            new HajjLocation(AppResources.Arafat, "arafat", 21.354070042012918, 39.98500670973953, "#3498DB"), // Blue
            new HajjLocation(AppResources.Muzdalifah, "muzdalifah", 21.392408688619156, 39.91010931758093, "#9B59B6"), // Purple
            new HajjLocation(AppResources.Mina, "mina", 21.413052539985806, 39.89021580688235, "#27AE60"), // Green
            new HajjLocation(AppResources.Safa, "safa", 21.421814, 39.827207, "#E67E22"), // Orange
            new HajjLocation(AppResources.Marwa, "marwa", 21.424796, 39.827194, "#1ABC9C") // Teal
        };

        private MemoryLayer CreatePinLayer()
        {
            // Define Hajj locations with colors and location keys for guide mapping
            var locations = GetKnownLocations();

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
            _userLocationImageStyle = AddFallbackPilgrimStyle(feature);

            // Create memory layer
            return new MemoryLayer
            {
                Name = "User Location", 
                Features = new[] { feature },
                Style = null
            };
        }

        private ImageStyle AddFallbackPilgrimStyle(PointFeature feature)
        {
            // Compass icon using Mapsui v5 ImageStyle with embedded SVG resource.
            var imageStyle = new ImageStyle
            {
                Image = new Mapsui.Styles.Image
                {
                    Source = "embedded://KhayratAlhaj.Resources.Images.pilgrim_location.svg",
                },
                SymbolScale = 0.9,
                RelativeOffset = new RelativeOffset(0.0, 0.0),
                SymbolRotation = _currentHeading,
            };

            feature.Styles.Add(imageStyle);
            return imageStyle;
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
                            HideSelectedPointPopup();
                            
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
                                _directionsTarget = (pinLat, pinLon, name);
                                
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
                                if (feature["lat"] != null && feature["lon"] != null)
                                {
                                    var pinLat = Convert.ToDouble(feature["lat"]);
                                    var pinLon = Convert.ToDouble(feature["lon"]);
                                    _directionsTarget = (pinLat, pinLon, name);
                                }

                                PopupDistance.Text = "--";
                                PopupETA.Text = "--";
                            }
                            
                            _showResetAfterGetDirections = false;
                            UpdateResetDirectionsButtonVisibility();
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
                else if (mapInfo?.WorldPosition != null)
                {
                    // No pin was hit: treat the tap as a user-selected reference point and
                    // show its distance to every known Hajj location. This works anywhere
                    // in the world, so it is useful even when the device is not physically
                    // near Makkah (where live GPS-based directions are not meaningful).
                    var (lon, lat) = SphericalMercator.ToLonLat(mapInfo.WorldPosition.X, mapInfo.WorldPosition.Y);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        LocationPopup.IsVisible = false;
                        ShowSelectedPointDistances(lat, lon);
                    });
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

        private void HideSelectedPointPopup()
        {
            SelectedPointPopup.IsVisible = false;
            RemoveSelectedPointMarker();
        }

        private void OnCloseSelectedPointPopupClicked(object? sender, EventArgs e)
        {
            HideSelectedPointPopup();
        }

        private void ShowSelectedPointDistances(double lat, double lon)
        {
            UpdateSelectedPointMarker(lat, lon);

            SelectedPointCoordinatesLabel.Text = $"{lat:F5}, {lon:F5}";

            SelectedPointDistancesList.Children.Clear();
            foreach (var location in GetKnownLocations())
            {
                SelectedPointDistancesList.Children.Add(CreateDistanceRow(location, lat, lon));
            }

            SelectedPointPopup.IsVisible = true;
        }

        private View CreateDistanceRow(HajjLocation destination, double originLat, double originLon)
        {
            var distanceKm = Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(
                originLat, originLon, destination.Lat, destination.Lon, DistanceUnits.Kilometers);

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto)
                },
                Padding = new Thickness(0, 4)
            };

            var nameLabel = new Label
            {
                Text = destination.Name,
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center
            };
            nameLabel.SetAppThemeColor(Label.TextColorProperty,
                Microsoft.Maui.Graphics.Color.FromArgb("#2C3E50"),
                Microsoft.Maui.Graphics.Color.FromArgb("#ECF0F1"));

            var distanceLabel = new Label
            {
                Text = FormatDistance(distanceKm),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.End,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(8, 0)
            };
            distanceLabel.SetAppThemeColor(Label.TextColorProperty,
                Microsoft.Maui.Graphics.Color.FromArgb("#3498DB"),
                Microsoft.Maui.Graphics.Color.FromArgb("#5DADE2"));

            var chevronLabel = new Label
            {
                Text = AppResources.NavChevron,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            chevronLabel.SetAppThemeColor(Label.TextColorProperty,
                Microsoft.Maui.Graphics.Color.FromArgb("#BDC3C7"),
                Microsoft.Maui.Graphics.Color.FromArgb("#7F8C8D"));

            Grid.SetColumn(nameLabel, 0);
            Grid.SetColumn(distanceLabel, 1);
            Grid.SetColumn(chevronLabel, 2);
            row.Children.Add(nameLabel);
            row.Children.Add(distanceLabel);
            row.Children.Add(chevronLabel);

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await DrawRouteFromSelectedPointAsync(originLat, originLon, destination);
            row.GestureRecognizers.Add(tapGesture);

            return row;
        }

        private async Task DrawRouteFromSelectedPointAsync(double originLat, double originLon, HajjLocation destination)
        {
            try
            {
                _showResetAfterGetDirections = true;
                _routeOrigin = (originLat, originLon);
                _directionsTarget = (destination.Lat, destination.Lon, destination.Name);

                // Close the selected-point modal now that a destination was chosen.
                SelectedPointPopup.IsVisible = false;

                RouteToggleContainer.IsVisible = true;
                await LoadUserDirectionsAsync();
                UpdateResetDirectionsButtonVisibility();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error drawing route from selected point: {ex.Message}");
            }
        }

        private static string FormatDistance(double distanceKm)
        {
            return distanceKm < 1 ? $"{distanceKm * 1000:F0} m" : $"{distanceKm:F1} km";
        }

        private void UpdateSelectedPointMarker(double lat, double lon)
        {
            if (HajjMapControl?.Map == null) return;

            RemoveSelectedPointMarker();

            var point = SphericalMercator.FromLonLat(lon, lat);
            var feature = new PointFeature(point.ToMPoint());
            feature["name"] = AppResources.SelectedPointTitle;
            feature.Styles.Add(ImageStyles.CreatePinStyle(
                fillColor: Mapsui.Styles.Color.FromString("#F1C40F"),
                symbolScale: 1.2));

            _selectedPointLayer = new MemoryLayer
            {
                Name = "Selected Point",
                Features = new[] { feature },
                Style = null
            };

            HajjMapControl.Map.Layers.Add(_selectedPointLayer);
            HajjMapControl.Map.Refresh();
        }

        private void RemoveSelectedPointMarker()
        {
            if (_selectedPointLayer != null && HajjMapControl?.Map?.Layers.Contains(_selectedPointLayer) == true)
            {
                HajjMapControl.Map.Layers.Remove(_selectedPointLayer);
                HajjMapControl.Map.Refresh();
            }
            _selectedPointLayer = null;
        }

        private void OnClosePopupClicked(object? sender, EventArgs e)
        {
            LocationPopup.IsVisible = false;
            ResetGuidePopupState();
        }

        private bool HasActiveDirections()
        {
            return _userRouteLayer != null || _directionsTarget != null || _routeOrigin != null;
        }

        private void UpdateResetDirectionsButtonVisibility()
        {
            var isVisible = _showResetAfterGetDirections && HasActiveDirections();

            if (ResetDirectionsButton != null)
            {
                ResetDirectionsButton.IsVisible = false;
            }

            if (ResetDirectionsFloatingButton != null)
            {
                ResetDirectionsFloatingButton.IsVisible = isVisible;
            }
        }

        private void OnResetDirectionsClicked(object? sender, EventArgs e)
        {
            try
            {
                _showResetAfterGetDirections = false;
                _directionsTarget = null;
                _routeOrigin = null;

                if (_userRouteLayer != null && HajjMapControl?.Map?.Layers.Contains(_userRouteLayer) == true)
                {
                    HajjMapControl.Map.Layers.Remove(_userRouteLayer);
                }

                if (HajjMapControl?.Map != null)
                {
                    var orphanedDirectionLayers = HajjMapControl.Map.Layers
                        .Where(l => string.Equals(l.Name, "User Directions", StringComparison.Ordinal))
                        .ToList();

                    foreach (var layer in orphanedDirectionLayers)
                    {
                        HajjMapControl.Map.Layers.Remove(layer);
                    }
                }

                _userRouteLayer = null;

                RouteInfoPanel.IsVisible = false;
                RouteToggleContainer.IsVisible = false;
                LocationPopup.IsVisible = false;
                RouteDistanceLabel.Text = string.Empty;
                RouteETALabel.Text = string.Empty;

                HideSelectedPointPopup();
                UpdateResetDirectionsButtonVisibility();
                HajjMapControl?.Map?.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting directions: {ex.Message}");
            }
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

                        // Show summary + Read More, hide View Guide button
                        GuideSummaryLabel.Text = subCategory.Content ?? string.Empty;
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

                // Keep icon-only button state stable while processing.
                ShareLocationButton.IsEnabled = false;

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
            }
        }

        // ==================== Route Toggle ====================

        private async void OnWalkingClicked(object? sender, EventArgs e)
        {
            if (_userRouteProfile == "foot") return;
            _userRouteProfile = "foot";
            UpdateRouteToggleUI();

            if (_directionsTarget != null)
            {
                await LoadUserDirectionsAsync();
            }
        }

        private async void OnDrivingClicked(object? sender, EventArgs e)
        {
            if (_userRouteProfile == "car") return;
            _userRouteProfile = "car";
            UpdateRouteToggleUI();

            if (_directionsTarget != null)
            {
                await LoadUserDirectionsAsync();
            }
        }

        private void UpdateRouteToggleUI()
        {
            var isWalking = _userRouteProfile == "foot";
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
                var result = await _routingService.GetRouteAsync(HajjWaypoints, "foot");

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
                            _routeLayer = CreateOsrmRouteLayer(result.Coordinates, false);
                            HajjMapControl?.Map?.Layers.Insert(1, _routeLayer); // Insert above tile layer but below pins

                            // Show route info
                            RouteDistanceLabel.Text = result.FormattedDistance;
                            RouteETALabel.Text = $"{result.FormattedDuration} {AppResources.Walking}";
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

        private async void OnGetDirectionsClicked(object? sender, EventArgs e)
        {
            try
            {
                GetDirectionsButton.IsEnabled = false;

                if (_directionsTarget == null)
                    return;

                // A direct "Get Directions" tap always routes from the device's real
                // location, overriding any previously selected reference point.
                _routeOrigin = null;

                if (_userLocation == null)
                {
                    await RequestLocationPermissionAndShowUserLocationAsync();
                }

                if (_userLocation == null)
                {
                    await DisplayAlertAsync(
                        AppResources.UnableToGetLocation,
                        AppResources.LocationPermissionRequired,
                        AppResources.OK);
                    return;
                }

                _showResetAfterGetDirections = true;
                RouteToggleContainer.IsVisible = true;
                await LoadUserDirectionsAsync();
                LocationPopup.IsVisible = false;
                UpdateResetDirectionsButtonVisibility();
            }
            finally
            {
                GetDirectionsButton.IsEnabled = true;
            }
        }

        private async Task LoadUserDirectionsAsync()
        {
            var origin = _routeOrigin ?? (_userLocation != null
                ? (_userLocation.Latitude, _userLocation.Longitude)
                : ((double Lat, double Lon)?)null);

            if (origin == null || _directionsTarget == null)
                return;

            try
            {
                var waypoints = new List<(double Lat, double Lon)>
                {
                    (origin.Value.Lat, origin.Value.Lon),
                    (_directionsTarget.Value.Lat, _directionsTarget.Value.Lon)
                };

                var result = await _routingService.GetRouteAsync(waypoints, _userRouteProfile);

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        if (_userRouteLayer != null && HajjMapControl?.Map?.Layers.Contains(_userRouteLayer) == true)
                        {
                            HajjMapControl.Map.Layers.Remove(_userRouteLayer);
                        }

                        if (result != null && result.Coordinates.Count > 1)
                        {
                            _userRouteLayer = CreateUserRouteLayer(result.Coordinates, _userRouteProfile == "car");
                            HajjMapControl?.Map?.Layers.Insert(2, _userRouteLayer);

                            RouteDistanceLabel.Text = result.FormattedDistance;
                            RouteETALabel.Text = $"{result.FormattedDuration} {(_userRouteProfile == "foot" ? AppResources.Walking : AppResources.Driving)}";
                            RouteInfoPanel.IsVisible = true;
                            UpdateResetDirectionsButtonVisibility();

                            CenterMapOnRoute(result.Coordinates);
                        }
                        else
                        {
                            await DisplayAlertAsync(
                                AppResources.RouteInfo,
                                AppResources.OfflineRouteWarning,
                                AppResources.OK);
                        }

                        HajjMapControl?.Map?.Refresh();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating user route layer: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"User directions loading error: {ex.Message}");
            }
        }

        private void CenterMapOnRoute(List<(double Lat, double Lon)> routeCoords)
        {
            if (HajjMapControl?.Map?.Navigator == null || routeCoords.Count == 0)
                return;

            try
            {
                var minLat = routeCoords.Min(p => p.Lat);
                var maxLat = routeCoords.Max(p => p.Lat);
                var minLon = routeCoords.Min(p => p.Lon);
                var maxLon = routeCoords.Max(p => p.Lon);

                var centerLat = (minLat + maxLat) / 2;
                var centerLon = (minLon + maxLon) / 2;
                var center = SphericalMercator.FromLonLat(centerLon, centerLat);

                var maxDistanceKm = Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(
                    minLat, minLon, maxLat, maxLon, DistanceUnits.Kilometers);

                var zoomLevel = maxDistanceKm switch
                {
                    < 0.5 => 17,
                    < 1.5 => 15,
                    < 4 => 14,
                    < 10 => 12,
                    < 25 => 11,
                    < 60 => 10,
                    _ => 9
                };

                HajjMapControl.Map.Navigator.CenterOn(new MPoint(center.x, center.y));
                HajjMapControl.Map.Navigator.ZoomTo(zoomLevel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error centering map on route: {ex.Message}");
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
