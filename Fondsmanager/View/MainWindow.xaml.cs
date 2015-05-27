using System.Windows;
using Fondsmanager.ViewModel;

namespace Fondsmanager.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Closing += (s, e) => ViewModelLocator.Cleanup();
        }

        private void ListBox_Selected(object sender, RoutedEventArgs e)
        {

        }
    }
}