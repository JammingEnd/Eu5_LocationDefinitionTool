// csharp

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Eu5_MapTool.cache;
using Eu5_MapTool.Models;
using Eu5_MapTool.Services;
using Eu5_MapTool.Services.Repository;
using Eu5_MapTool.Settings;
using Eu5_MapTool.ViewModels;

namespace Eu5_MapTool.Views
{
    public partial class StartupDialogWindow : Window
    {
        public readonly StartupDialogViewModel _vm;
        private readonly MainWindowViewModel _mainVM;
        private Settings.Settings _settings;

        public StartupDialogWindow(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();
            _vm = new StartupDialogViewModel(mainWindowViewModel);
            DataContext = _vm;
            _mainVM = mainWindowViewModel;
            LoadSettings();

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

                // ======== Load Cache using CacheLoaderService ========
                var cacheLoader = new CacheLoaderService();
                Cache cache = await cacheLoader.LoadCacheAsync(_vm.DirectoryA!, _vm.DirectoryB!);
                _mainVM.SetCache(cache);

                // ======== Load Map Image ========
                _mainVM.LoadMapImage(_vm.DirectoryB!);

                // ======== Initialize ORM (Unit of Work) ========
                Console.WriteLine("Initializing ORM (Unit of Work) with direct parser usage...");

                // Create repository with directory paths (uses parsers directly, no old services!)
                var provinceRepository = new ProvinceRepository(_vm.DirectoryA, _vm.DirectoryB);
                await provinceRepository.LoadAsync();

                // Create transaction manager for backup/rollback support
                string backupDir = Path.Combine(Path.GetTempPath(), "Eu5MapTool_Backup");
                var transactionManager = new TransactionManager(backupDir);

                // Create Unit of Work
                var unitOfWork = new UnitOfWork(provinceRepository, transactionManager);

                // Initialize the ViewModel with Unit of Work
                _mainVM.InitializeUnitOfWork(unitOfWork);

                // Populate ViewModel's Provinces dictionary from repository
                var allProvinces = await unitOfWork.Provinces.GetAllAsync();
                _mainVM.LoadProvinces(allProvinces.ToDictionary(p => p.Id, p => p));

                Console.WriteLine($"✓ ORM initialized with direct parsers. No dependency on old services!");
                // ===============================================

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