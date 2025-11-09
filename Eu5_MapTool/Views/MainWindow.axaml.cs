using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Eu5_MapTool.cache;
using Eu5_MapTool.logic;
using Eu5_MapTool.Models;
using Eu5_MapTool.Services;
using Eu5_MapTool.ViewModels;

namespace Eu5_MapTool.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _vm;
        
        
        private double _scale = 1.0;
        private readonly double _minScale = 0.1;
        private readonly double _maxScale = 50.0;
        private double _translateX = 0;
        private double _translateY = 0;

        private Point _panStart;
        private double _panOriginX;
        private double _panOriginY;
        private bool _isPanning;
        
        private ToolType _activeTool = ToolType.Select;
        private PaintType _activePaintType = PaintType.LocationInfo;
        private Dictionary<AutoCompleteBox, AutoCompleteBox> _toolLocationSettings = new Dictionary<AutoCompleteBox, AutoCompleteBox>();
        private Dictionary<AutoCompleteBox, (NumericUpDown, AutoCompleteBox, AutoCompleteBox)> _toolPopSettings = new Dictionary<AutoCompleteBox, (NumericUpDown, AutoCompleteBox, AutoCompleteBox)>();
        
        private Color _inactiveColor = Colors.DarkSlateGray;
        private Color _activeColor = Colors.CornflowerBlue;

        public MainWindow()
        {
            InitializeComponent();
            UpdateTransform();
            ZoomLabel.Text = $"{_scale:P0}";
            _vm = new MainWindowViewModel();
            DataContext = _vm;
            
            this.Opened += async (_,_) => await ShowStartupDialogAsync();
            
            _vm.OnDoneLoadingMap += (_, _) =>
            {
                MapImage.Source = _vm.MapImage;
                ResetView();
            };
            
            ResetBtnColors(SelectBtn);
            ResetPaintTypeBtnColors(PaintTypeLocationBtn);
        }
        private async System.Threading.Tasks.Task ShowStartupDialogAsync()
        {
            var dialog = new StartupDialogWindow(_vm)
            {
                DataContext = new StartupDialogViewModel(_vm)
            };
            await dialog.ShowDialog(this);

            if (!dialog._vm.WasAccepted)
            {
                var app = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                app.Shutdown();
            }
                
        }

        private async void OpenButton_Click(object? sender, RoutedEventArgs e)
        {
            /*var dlg = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Images", Extensions = { "png", "jpg", "jpeg", "bmp", "gif" } }
                }
            };

            var result = await dlg.ShowAsync(this);
            if (result != null && result.Length > 0 && File.Exists(result[0]))
            {
                using var fs = File.OpenRead(result[0]);
                MapImage.Source = new Bitmap(fs);
                ResetView();
            }*/
        }

        private void ResetButton_Click(object? sender, RoutedEventArgs e) => ResetView();

        private void ResetView()
        {
            _scale = 1.0;
            _translateX = 0;
            _translateY = 0;
            UpdateTransform();
        }

        private void MapImage_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (MapImage.Source == null) return;

            // pointer position relative to image control
            var pointerPos = e.GetPosition(MapImage);
            var delta = e.Delta.Y;
            var factor = Math.Pow(1.2, delta);
            var newScale = Math.Clamp(_scale * factor, _minScale, _maxScale);

            // center zoom on cursor
            _translateX = (_translateX - pointerPos.X) * (newScale / _scale) + pointerPos.X;
            _translateY = (_translateY - pointerPos.Y) * (newScale / _scale) + pointerPos.Y;

            _scale = newScale;
            UpdateTransform();
        }

        private void MapImage_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var p = e.GetCurrentPoint(MapImage);
            if (p.Properties.IsMiddleButtonPressed)
            {
                _isPanning = true;
                _panStart = e.GetPosition(this);
                _panOriginX = _translateX;
                _panOriginY = _translateY;
                //MapImage.CapturePointer(e.Pointer);
                Cursor = new Cursor(StandardCursorType.Hand);
            }
        }

        private void MapImage_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPanning) return;

            var pos = e.GetPosition(this);
            var dx = pos.X - _panStart.X;
            var dy = pos.Y - _panStart.Y;

            _translateX = _panOriginX + dx;
            _translateY = _panOriginY + dy;
            UpdateTransform();
        }

        private void MapImage_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                //MapImage.ReleasePointerCapture(e.Pointer);
                Cursor = new Cursor(StandardCursorType.Arrow);
                return;
            }


            if (MapImage.Source is Bitmap bitmap)
            {
                var pos = e.GetPosition(MapImage);
                var (px, py) = GetBitmapCoords(MapImage, bitmap, pos);
                var color = GetPixelColor(bitmap, px, py);
                var hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                string id = hex.Replace("#", "").Trim().ToLower();
                Console.WriteLine(hex);
                if (_activeTool == ToolType.Paint)
                {
                    UpdateProvinceInfoPanel(id);
                    if (_activePaintType == PaintType.LocationInfo)
                    {
                        ProvinceLocation loc = default;
                        if (_vm.Provinces.ContainsKey(id))
                        {
                             loc = _vm.Provinces[id].LocationInfo;
                            
                        }
                        string topography = _toolLocationSettings.Where(x => x.Key.SelectedItem as string == "Topography")
                            .Select(x => x.Value.SelectedItem as string ?? loc.Topography).FirstOrDefault() ?? _vm.Cache.Topographies.GetCombined().First();
                        string vegetation = _toolLocationSettings.Where(x => x.Key.SelectedItem as string == "Vegetation")
                            .Select(x => x.Value.SelectedItem as string ?? loc.Vegetation).FirstOrDefault() ?? _vm.Cache.Vegetations.GetCombined().First();
                        string climate = _toolLocationSettings.Where(x => x.Key.SelectedItem as string == "Climate")
                            .Select(x => x.Value.SelectedItem as string ?? loc.Climate).FirstOrDefault() ?? _vm.Cache.Climates.GetCombined().First();
                        string religion = _toolLocationSettings.Where(x => x.Key.SelectedItem as string == "Religion")
                            .Select(x => x.Value.SelectedItem as string ?? loc.Religion).FirstOrDefault() ?? _vm.Cache.Religions.GetCombined().First();
                        string culture = _toolLocationSettings.Where(x => x.Key.SelectedItem as string == "Culture")
                            .Select(x => x.Value.SelectedItem as string ?? loc.Culture).FirstOrDefault() ?? _vm.Cache.Cultures.GetCombined().First();
                        string rawMaterial = _toolLocationSettings.Where(x => x.Key.SelectedItem as string == "Raw Material")
                            .Select(x => x.Value.SelectedItem as string ?? loc.RawMaterial).FirstOrDefault() ?? _vm.Cache.RawMaterials.GetCombined().First();
                        
                        _vm.OnPaint(id, topography, vegetation, climate, religion, culture, rawMaterial);
                        UpdateProvinceInfoPanel(id);
                    }
                    else if (_activePaintType == PaintType.PopInfo)
                    {
                        if(!HasFilledControlValues(_activePaintType))
                            return;
                        
                        List<PopDef> info = new List<PopDef>();
                        if (_vm.Provinces.ContainsKey(id))
                        {
                            if(_vm.Provinces[id].PopInfo != null)
                                info = new List<PopDef>(_vm.Provinces[id].PopInfo.Pops);
                            else
                                _vm.Provinces[id].PopInfo = new ProvincePopInfo();
                            
                        }

                        foreach (var kvp in _toolPopSettings)
                        {
                            PopDef pop = new PopDef
                            {
                                PopType = kvp.Key.SelectedItem as string,
                                Size = (float)Math.Round((float)kvp.Value.Item1.Value, 5),
                                Culture = kvp.Value.Item2.SelectedItem as string,
                                Religion = kvp.Value.Item3.SelectedItem as string
                            };
                            info.Add(pop);
                        }
                        
                        _vm.OnPaintPop(id, info);
                        UpdateProvinceInfoPanel(id);
                    }
                }

                if (_activeTool == ToolType.Select)
                {
                    UpdateProvinceInfoPanel(id);
                }
            }
            
        }
        private (int, int) GetBitmapCoords(Image img, Bitmap bitmap, Point pointerPos)
        {
            double controlWidth = img.Bounds.Width;
            double controlHeight = img.Bounds.Height;

            double imgWidth = bitmap.PixelSize.Width;
            double imgHeight = bitmap.PixelSize.Height;

            // Compute scale applied to the image
            double scale = Math.Min(controlWidth / imgWidth, controlHeight / imgHeight);

            // Compute top-left corner of the actual image inside the control
            double offsetX = (controlWidth - imgWidth * scale) / 2;
            double offsetY = (controlHeight - imgHeight * scale) / 2;

            // Pointer relative to the image
            double px = (pointerPos.X - offsetX) / scale;
            double py = (pointerPos.Y - offsetY) / scale;

            // Clamp to bitmap bounds
            px = Math.Clamp(px, 0, imgWidth - 1);
            py = Math.Clamp(py, 0, imgHeight - 1);

            return ((int)px, (int)py);
        }

        private unsafe Color GetPixelColor(Bitmap bitmap, int x, int y)
        {
            // Make sure we're inside the image bounds
            if (x < 0 || y < 0 || x >= bitmap.PixelSize.Width || y >= bitmap.PixelSize.Height)
                return Colors.Transparent;

            var pixel = new byte[4]; // BGRA

            fixed (byte* bufferPtr = pixel)
            {
                var destPtr = (IntPtr)bufferPtr;

                bitmap.CopyPixels(
                    new PixelRect(x, y, 1, 1), // area to copy
                    destPtr,                   // destination pointer
                    4,                         // buffer size (4 bytes per pixel)
                    4                          // stride (also 4 bytes for 1 pixel)
                );
            }

            // Avalonia bitmaps use BGRA format
            return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
        }

        private bool HasFilledControlValues(PaintType paintType)
        {
            if (paintType == PaintType.PopInfo)
            {
                foreach (var kvp in _toolPopSettings)
                {
                    var comboType = kvp.Key;
                    var sizeInput = kvp.Value.Item1;
                    var comboCulture = kvp.Value.Item2;
                    var comboReligion = kvp.Value.Item3;

                    if (string.IsNullOrWhiteSpace(comboType.SelectedItem as string) ||
                        sizeInput.Value == 0 ||
                        string.IsNullOrWhiteSpace(comboCulture.SelectedItem as string) ||
                        string.IsNullOrWhiteSpace(comboReligion.SelectedItem as string))
                    {
                        return false;
                    }
                }
            }
            else if (paintType == PaintType.LocationInfo)
            {
                foreach (var kvp in _toolLocationSettings)
                {
                    var comboType = kvp.Key;
                    var comboValue = kvp.Value;

                    if (string.IsNullOrWhiteSpace(comboType.SelectedItem as string) ||
                        string.IsNullOrWhiteSpace(comboValue.SelectedItem as string))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void UpdateProvinceInfoPanel(string hex)
        {
            PopInfoBlock.Children.Clear();

            // Use ViewModel's OnSelect method to update display
            // This will update all the bound ObservableProperties
            _vm.OnSelect(hex).Wait();

            // Get province info for additional UI updates that can't be bound
            if (_vm._paintedLocations.TryGetValue(hex, out var provInfo) || _vm.Provinces.TryGetValue(hex, out provInfo))
            {
                // Update name box (not bound to ViewModel)
                NameBox.Text = provInfo.Name;

                // Use provInfo (the actual province data) instead of selectedInfo
                var real_info = provInfo.LocationInfo;

                InfoId .Text = $"ID: {hex}";
                InfoTopo.Text = $"Topography: {real_info.Topography}";
                InfoVeg.Text = $"Vegetation: {real_info.Vegetation}";
                InfoClimate.Text = $"Climate: {real_info.Climate}";
                InfoReli.Text = $"Religion: {real_info.Religion}";
                InfoCulture.Text = $"Culture: {real_info.Culture}";
                InfoRgo.Text = $"Raw Material: {real_info.RawMaterial}";
                InfoHarbor.Text = $"Natural Harbor Suitability: {real_info.NaturalHarborSuitability}";

                // Generate pop info UI dynamically (can't be done via binding)
                if (provInfo.PopInfo != null && provInfo.PopInfo.Pops != null)
                {
                    foreach (var popInfo in provInfo.PopInfo.Pops)
                    {
                        var popText = new TextBlock
                        {
                            Text = $"Type: {popInfo.PopType}, Size: {popInfo.Size} \n Culture: {popInfo.Culture}, Religion: {popInfo.Religion}",
                            Margin = new Thickness(0, 2, 0, 2)
                        };
                        PopInfoBlock.Children.Add(popText);
                    }
                }
            }
            else
            {
                // Province not found - update name box only
                NameBox.Text = hex + " (not found in files)";
            }
        }

        private void UpdateTransform()
        {
            MapImage.RenderTransform = new TransformGroup
            {
                Children = new Transforms() { 
                    new ScaleTransform(_scale, _scale), 
                    new TranslateTransform(_translateX, _translateY) }
            };
            ZoomLabel.Text = $"{_scale:P0}";
        }

        private void SelectBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            _activeTool = ToolType.Select;
           ResetBtnColors(SelectBtn);
        }

        private void PaintBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            _activeTool = ToolType.Paint;
            ResetBtnColors(PaintBtn);
        }

        private void ProvinceBtn_OnClick(object? sender, RoutedEventArgs e)
        {
            _activeTool = ToolType.Province;
            ResetBtnColors(ProvinceBtn);
        }

        private void ResetBtnColors(Button selected)
        {
            SelectBtn.Background = new SolidColorBrush(_inactiveColor);
            PaintBtn.Background = new SolidColorBrush(_inactiveColor);
            ProvinceBtn.Background = new SolidColorBrush(_inactiveColor);
            
            selected.Background = new SolidColorBrush(_activeColor);
            
            // actually this will now also serve to reset the tool options area
            ToolSettingsPanel.IsVisible = _activeTool == ToolType.Paint;
        }

        private void AddToolApplierClick(object? sender, RoutedEventArgs e)
        {
            if((_activePaintType == PaintType.LocationInfo && _toolLocationSettings.Count == 6 ) || _toolLocationSettings.Any(x => string.IsNullOrWhiteSpace(x.Key.SelectedItem as string)))
                return; // cannot create more filters than there are filter options
            
            if (_activeTool == ToolType.Paint)
            {
                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 2, 2)
                };


                if (_activePaintType == PaintType.LocationInfo)
                {
                    List<string> typeInfo = new List<string>
                    {
                        "Topography",
                        "Vegetation",
                        "Climate",
                        "Religion",
                        "Culture",
                        "Raw Material"
                    };
                    var comboType = new AutoCompleteBox
                    {
                        Width = 120, 
                        ItemsSource = typeInfo,
                        Margin = new Thickness(0, 0, 5, 0),
                        FilterMode = AutoCompleteFilterMode.Contains, MinimumPrefixLength = 0
                    };
                    
                    // i uhh... gotta remove the type if its active already
                    if(_toolLocationSettings.Count != 0)
                        comboType.ItemsSource = typeInfo.Where(x => _toolLocationSettings.Keys.All(y => y.SelectedItem as string != x)).ToList();
                    
                    var comboValue = new AutoCompleteBox
                    {
                        Width = 120,
                        ItemsSource = new List<string> {"Select Type!"},
                        Margin = new Thickness(0, 0, 5, 0),
                        FilterMode = AutoCompleteFilterMode.Contains, MinimumPrefixLength = 0
                    };
                    
                    comboType.SelectionChanged += (s, ev) =>
                    {
                        string type = comboType.SelectedItem as string ?? "Topography";
                        switch (type)
                        {
                            case "Topography":
                                comboValue.ItemsSource = _vm.Cache.Topographies.GetCombined().ToList();
                                break;
                            case "Vegetation":
                                comboValue.ItemsSource = _vm.Cache.Vegetations.GetCombined().ToList();
                                break;  
                            case "Climate":
                                comboValue.ItemsSource = _vm.Cache.Climates.GetCombined().ToList();
                                break;
                            case "Religion":            
                                comboValue.ItemsSource = _vm.Cache.Religions.GetCombined().ToList();
                                break;
                            case "Culture": 
                                comboValue.ItemsSource = _vm.Cache.Cultures.GetCombined().ToList();    
                                break;
                            case "Raw Material":
                                comboValue.ItemsSource = _vm.Cache.RawMaterials.GetCombined().ToList();
                                break;
                            default:
                                return;
                        }
                        

                    };
                    var removeBtn = GenerateRemoveBtn(row, comboType);
                    
                    comboType.GotFocus += AutoCompleteBox_OnGotFocus;
                    comboValue.GotFocus += AutoCompleteBox_OnGotFocus;
                    
                    row.Children.Add(comboType);
                    row.Children.Add(comboValue);
                    row.Children.Add(removeBtn);
                    
                    _toolLocationSettings[comboType] = comboValue;
                    
                    ToolOptionsStack.Children.Add(row);
                    
                }
                else if (_activePaintType == PaintType.PopInfo)
                {
                    
                    // pop type
                    var comboType = new AutoCompleteBox
                    {
                        Width = 180,
                        ItemsSource = _vm.Cache.PopTypes.GetCombined().ToList(),
                        Margin = new Thickness(-197, 0, 5, 0),
                        FilterMode = AutoCompleteFilterMode.Contains, MinimumPrefixLength = 0
                    };

                    // for size duh
                    var sizeInput = new NumericUpDown
                    {
                        Width = 180,
                        Minimum = (decimal)0.001,
                        Maximum = 100000,
                        Increment = (decimal)0.001,
                        Margin = new Thickness(-197, 0, 5, 0),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        
                    };
                    
                    // culture select
                    var comboCulture = new AutoCompleteBox
                    {
                        Width = 180,
                        ItemsSource = _vm.Cache.Cultures.GetCombined().ToList(),
                        Margin = new Thickness(-197, 0, 5, 0),
                        FilterMode = AutoCompleteFilterMode.Contains, MinimumPrefixLength = 0
                    };
                    // religion select
                    var comboReligion = new AutoCompleteBox
                    {
                        Width = 180,
                        ItemsSource = _vm.Cache.Religions.GetCombined().ToList(),
                        Margin = new Thickness(-197, 0, 5, 0),
                        FilterMode = AutoCompleteFilterMode.Contains, MinimumPrefixLength = 0
                    };
                    
                    var removeBtn = GenerateRemoveBtn(row, comboType, 20);
                    
                    var seperator = new Separator(); 

                    row.Orientation = Orientation.Vertical;

                    comboType.GotFocus += AutoCompleteBox_OnGotFocus;
                    comboCulture.GotFocus += AutoCompleteBox_OnGotFocus;
                    comboReligion.GotFocus += AutoCompleteBox_OnGotFocus;
                    
                    row.Children.Add(comboType);
                    row.Children.Add(sizeInput);
                    row.Children.Add(comboCulture);
                    row.Children.Add(comboReligion);
                    row.Children.Add(removeBtn);
                    row.Children.Add(seperator);

                    _toolPopSettings[comboType] = (sizeInput, comboCulture, comboReligion);

                    ToolOptionsStack.Children.Add(row);
                    
                }



            }
        }

        private Button GenerateRemoveBtn(StackPanel row, AutoCompleteBox comboType, double height = 25)
        {
            var removeBtn = new Button
            {
                Content = "X",
                Background = new SolidColorBrush(Colors.Red),
                Foreground = new SolidColorBrush(Colors.White),
                Width = 25,
                Height = height,
                Margin = new Thickness(0, 0, 5, 0)
            };
            removeBtn.Click += (s, ev) =>
            {
                ToolOptionsStack.Children.Remove(row);
                _toolLocationSettings.Remove(comboType);
            };
            
            return removeBtn;
        }

        private void OnSelectPaintTypeLoc(object? sender, RoutedEventArgs e)
        {
            _activePaintType = PaintType.LocationInfo;  
            ResetPaintTypeBtnColors(PaintTypeLocationBtn);
            ToolOptionsStack.Children.Clear();
            foreach (var kvp in _toolLocationSettings)
            {
                var comboType = kvp.Key;
                var comboValue = kvp.Value;

                // Remove from previous visual parent if present
                if (comboType.Parent is Panel oldParent1)
                    oldParent1.Children.Remove(comboType);

                if (comboValue.Parent is Panel oldParent2)
                    oldParent2.Children.Remove(comboValue);

                // Recreate the row and add the controls
                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 2, 2)
                };
                
                var removeBtn = GenerateRemoveBtn(row, comboType);

                row.Children.Add(comboType);
                row.Children.Add(comboValue);
                row.Children.Add(removeBtn);

                ToolOptionsStack.Children.Add(row);
            }
        }

        private void OnSelectPaintPop(object? sender, RoutedEventArgs e)
        {
            _activePaintType = PaintType.PopInfo;  
            ResetPaintTypeBtnColors(PaintTypePopBtn);
            ToolOptionsStack.Children.Clear();
            foreach (var kvp in _toolPopSettings)
            {
                var comboType = kvp.Key;
                var sizeInput = kvp.Value.Item1;
                var comboCulture = kvp.Value.Item2;
                var comboReligion = kvp.Value.Item3;
                
                // Remove from previous visual parent if present
                if (comboType.Parent is Panel oldParent1)
                    oldParent1.Children.Remove(comboType);
                if (sizeInput.Parent is Panel oldParent2)
                    oldParent2.Children.Remove(sizeInput);
                if (comboCulture.Parent is Panel oldParent3)
                    oldParent3.Children.Remove(comboCulture);
                if (comboReligion.Parent is Panel oldParent4)
                    oldParent4.Children.Remove(comboReligion);
                
                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 2, 2)
                };
                var removeBtn = GenerateRemoveBtn(row, comboType, 20);
                
                var seperator = new Separator();

                row.Orientation = Orientation.Vertical;
                
                row.Children.Add(comboType);
                row.Children.Add(sizeInput);
                row.Children.Add(comboCulture);
                row.Children.Add(comboReligion);
                row.Children.Add(removeBtn);
                row.Children.Add(seperator);
                
                ToolOptionsStack.Children.Add(row);
            }
        }
        
        private void ResetPaintTypeBtnColors(Button selected)
        {
            PaintTypeLocationBtn.Background = new SolidColorBrush(_inactiveColor);
            PaintTypePopBtn.Background = new SolidColorBrush(_inactiveColor);
            
            selected.Background = new SolidColorBrush(_activeColor);
        }

        private async void WriteEdits_Click(object? sender, RoutedEventArgs e)
        {
           await _vm.WriteChanges();
           
           _vm.LoadProvinces();
        }

        private void UpdateNameEvent(object? sender, TextChangedEventArgs textChangedEventArgs)
        {
            _vm.UpdateProvinceName(NameBox.Text);
        }

        private void AutoCompleteBox_OnGotFocus(object? sender, GotFocusEventArgs e)
        {
            if (sender is AutoCompleteBox box)
            {
                if(string.IsNullOrWhiteSpace(box.Text))
                    box.IsDropDownOpen = true;
                box.Text = box.Text;
            }
        }

    }
}