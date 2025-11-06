// csharp

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Eu5_MapTool.cache;
using Eu5_MapTool.Models;
using Eu5_MapTool.Services;
using Eu5_MapTool.Settings;
using Eu5_MapTool.ViewModels;

namespace Eu5_MapTool.Views
{
    public partial class StartupDialogWindow : Window
    {
        public readonly StartupDialogViewModel _vm;
        private readonly MainWindowViewModel _mainVM;
        private readonly AppStorageService _storageService;
        private Settings.Settings _settings;
        
        public StartupDialogWindow(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();
            _vm = new StartupDialogViewModel(mainWindowViewModel);
            DataContext = _vm;
            _mainVM = mainWindowViewModel;
            LoadSettings();

            _storageService = new AppStorageService();
            
            Closing += OnWindowClosing;
        }

        private async void LoadSettings()
        {
            _settings = await SettingsService.LoadAsync();
            
            if (!string.IsNullOrWhiteSpace(_settings.LastUsedDirectoryA))
            {
                _vm.SetPath(_settings.LastUsedDirectoryA, true);
                dirA_txt.Text = _settings.LastUsedDirectoryA;
            }
            if (!string.IsNullOrWhiteSpace(_settings.LastUsedDirectoryB))
            {
                _vm.SetPath(_settings.LastUsedDirectoryB, false);
                dirB_txt.Text = _settings.LastUsedDirectoryB;
            }
            
        }

        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_mainVM.Cache == null)
            {
                Environment.Exit(0);
            }
        }
        private async void BrowseDirectoryA_Click(object? sender, RoutedEventArgs e)
        {
            await BrowseAndSetDirectoryAsync(_vm.DirectoryA, true);
        }

        private async void BrowseDirectoryB_Click(object? sender, RoutedEventArgs e)
        {
            await BrowseAndSetDirectoryAsync(_vm.DirectoryB, false);
        }

        private async Task BrowseAndSetDirectoryAsync(string? directory, bool dirA)
        {
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider != null)
            {
                var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Directory",
                    AllowMultiple = false
                });
                if (folders.Count > 0)
                {
                    var folder = folders[0];
                    _vm.SetPath(folder.Path.LocalPath, dirA);

                    if (dirA)
                    {
                        dirA_txt.Text = folder.Path.LocalPath;
                        _settings.LastUsedDirectoryA = folder.Path.LocalPath;
                        await SettingsService.SaveAsync(_settings);
                    }
                    else
                    {
                        dirB_txt.Text = folder.Path.LocalPath;
                        _settings.LastUsedDirectoryB = folder.Path.LocalPath;
                        await SettingsService.SaveAsync(_settings);
                    }
                    
                }
                else
                {
                    Console.WriteLine("Root doesnt contain subfolders, are you sure this is the correct directory?");
                }
            }
        }

        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            if (_vm.WasAccepted && !string.IsNullOrWhiteSpace(_vm.DirectoryA) && !string.IsNullOrWhiteSpace(_vm.DirectoryB))
            {
                Console.WriteLine("Loading directories:");
                _storageService.SetDirectories(_vm.DirectoryA!, _vm.DirectoryB!);


                CulturesC cache_cultures = new CulturesC(
                    await _storageService.LoadCultureListAsync(_vm.DirectoryA),
                    await _storageService.LoadCultureListAsync(_vm.DirectoryB)
                );
                ReligionC cache_religions = new ReligionC(
                    await _storageService.LoadReligionListAsync(_vm.DirectoryA),
                    await _storageService.LoadReligionListAsync(_vm.DirectoryB)
                );
                TopographyC cache_topographies = new TopographyC(
                    await _storageService.LoadTopographyListAsync(_vm.DirectoryA),
                    await _storageService.LoadTopographyListAsync(_vm.DirectoryB)
                );
                ClimateC cache_climates = new ClimateC(
                    await _storageService.LoadClimateListAsync(_vm.DirectoryA),
                    await _storageService.LoadClimateListAsync(_vm.DirectoryB)
                );
                VegetationC cache_vegetations = new VegetationC(
                    await _storageService.LoadVegetationListAsync(_vm.DirectoryA),
                    await _storageService.LoadVegetationListAsync(_vm.DirectoryB)
                );
                RawMaterialsC cache_rawMaterials = new RawMaterialsC(
                    await _storageService.LoadRawMaterialListAsync(_vm.DirectoryA),
                    await _storageService.LoadRawMaterialListAsync(_vm.DirectoryB)
                );
                PopTypesC cache_popTypes = new PopTypesC(
                    await _storageService.LoadPopTypeListAsync(_vm.DirectoryA),
                    await _storageService.LoadPopTypeListAsync(_vm.DirectoryB)
                );
                
                Cache cache = new Cache(
                    cache_religions,
                    cache_cultures,
                    cache_topographies,
                    cache_vegetations,
                    cache_climates,
                    cache_rawMaterials,
                    cache_popTypes
                    );
                
                _mainVM.SetCache(cache);

                Dictionary<string, ProvinceInfo> infos = await _storageService.LoadModdedAsync();
                
                _mainVM.LoadProvinces(infos);
                
                _mainVM.LoadMapImage(_storageService);
                
                this.Close();
            }
        }

        private void dirA_txtChange(object? sender, TextChangedEventArgs e)
        {
            _vm.SetPath(dirA_txt.Text, true);
        }

        private void dirB_txtChange(object? sender, TextChangedEventArgs e)
        {
            _vm.SetPath(dirB_txt.Text, false);
        }
    }
}