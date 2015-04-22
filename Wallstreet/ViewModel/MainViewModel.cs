using GalaSoft.MvvmLight;
using SharedFeatures.Model;
using System.Collections.ObjectModel;

namespace Wallstreet.ViewModel
{

    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            
        }

        public ObservableCollection<FirmDepot> FirmDepots { get; set; }

        public ObservableCollection<Transaction> Transactions { get; set; }

        public ObservableCollection<Order> Orders { get; set; }
    }
}