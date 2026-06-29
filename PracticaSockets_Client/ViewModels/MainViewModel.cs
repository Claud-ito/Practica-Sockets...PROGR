using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace PracticaSockets_Client.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private object _currentViewModel;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public Tab1ViewModel Tab1 { get; } = new Tab1ViewModel();
        public Tab2ViewModel Tab2 { get; } = new Tab2ViewModel();
        public Tab3ViewModel Tab3 { get; } = new Tab3ViewModel();
        public Tab4ViewModel Tab4 { get; } = new Tab4ViewModel();
        public Tab5ViewModel Tab5 { get; } = new Tab5ViewModel();
        public Tab6ViewModel Tab6 { get; } = new Tab6ViewModel();

        public MainViewModel()
        {
            CurrentViewModel = Tab1;
        }
    }
}
