using System.Windows;
using Wallstreet.ViewModel;
using System;

namespace Wallstreet
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
            Closing += (s, e) => ResourceLocator.Cleanup();
        }
    }
}