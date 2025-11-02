// csharp

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Eu5_MapTool.cache;
using Eu5_MapTool.Models;
using Eu5_MapTool.Services;
using Eu5_MapTool.ViewModels;

namespace Eu5_MapTool.Views
{
    public partial class StartupDialogWindow : Window
    {
        public readonly StartupDialogViewModel _vm;
        private readonly MainWindowViewModel _mainVM;
        private readonly AppStorageService _storageService;
        
        public StartupDialogWindow(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();
            _vm = new StartupDialogViewModel(mainWindowViewModel);
            DataContext = _vm;
            _mainVM = mainWindowViewModel;

            _storageService = new AppStorageService();
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
                    
                    if(dirA)
                        dirA_txt.Text = folder.Path.LocalPath;
                    else
                        dirB_txt.Text = folder.Path.LocalPath;
                    
                }
            }
        }

        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            if (_vm.WasAccepted)
            {
                
                _storageService.SetDirectories(_vm.DirectoryA!, _vm.DirectoryB!);
                //TODO: loadig file from directories and initialize app state
                Cache cache = new Cache();
                
                _mainVM.SetCache(cache);

                Dictionary<string, ProvinceInfo> infos = await _storageService.LoadModdedAsync();
                
                
                _mainVM.LoadProvinces(infos);
                
                 _mainVM.LoadMapImage(_storageService);
                
                this.Close();
            }
        }
    }
}