using PracticaSockets_Client.ViewModels;
using System.Windows;

namespace PracticaSockets_Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
