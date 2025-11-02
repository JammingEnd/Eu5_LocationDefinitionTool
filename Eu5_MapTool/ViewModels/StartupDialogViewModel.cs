// csharp

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Eu5_MapTool.ViewModels
{
    public partial class StartupDialogViewModel : ObservableObject
    {
        private readonly MainWindowViewModel _mainVM;
        public bool WasAccepted { get; private set; }
        
        [ObservableProperty]
        private string? _directoryA;
        
        [ObservableProperty]
        private string? _directoryB = "/mnt/seagate/eu5/tools/testdata/textfiles/";


        

        public StartupDialogViewModel(MainWindowViewModel mainWindowViewModel)
        {
            WasAccepted = false;
            _mainVM = mainWindowViewModel;
        }

        private bool CanSave() => !string.IsNullOrWhiteSpace(DirectoryA) && !string.IsNullOrWhiteSpace(DirectoryB);

        [RelayCommand]
        private void SaveAndContinue()
        {
            //TODO: loadig file from directories and initialize app state
            
            
            
            _mainVM._writerService.SetWriteDirectory(DirectoryB);
            
        }

        partial void OnDirectoryAChanged(string? value)
        {
            ((RelayCommand)SaveAndContinueCommand).NotifyCanExecuteChanged();
        }


        public void SetPath(string path, bool dirA)
        {
            Console.WriteLine(path);
            if (dirA)
            {
                DirectoryA = path;
                WasAccepted = true;
            }
                
            else
                DirectoryB = path;
        }
    }
}