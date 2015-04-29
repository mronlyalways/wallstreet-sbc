using GalaSoft.MvvmLight;
using SharedFeatures.Model;
using System.Collections.ObjectModel;
using System.Linq;
using Wallstreet.Model;

namespace Wallstreet.ViewModel
{

    public class MainViewModel : ViewModelBase
    {
        private IDataService data;

        public MainViewModel(IDataService data)
        {
            this.data = data;
            FirmDepots = new ObservableCollection<ShareInformation>(data.LoadShareInformation());
            data.OnNewShareInformationAvailable(nu =>
                {
                    var tmp = FirmDepots.Where(x => x.FirmName.Equals(nu.FirmName));
                    var old = tmp.Count() == 0 ? null : tmp.First();
                    if (old != null)
                    {
                        FirmDepots.Insert(FirmDepots.IndexOf(old), nu);
                        FirmDepots.Remove(old);
                    }
                    else
                    {
                        FirmDepots.Add(nu);
                    }
                });
        }

        public ObservableCollection<ShareInformation> FirmDepots { get; set; }

        public ObservableCollection<Transaction> Transactions { get; set; }

        public ObservableCollection<Order> Orders { get; set; }
    }
}