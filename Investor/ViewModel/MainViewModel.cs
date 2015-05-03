using GalaSoft.MvvmLight;
using SharedFeatures.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Investor.Model;
using System;
using GalaSoft.MvvmLight.Command;

namespace Investor.ViewModel
{

    public class MainViewModel : ViewModelBase
    {
        private IDataService data;
        private InvestorDepot depot;
                
        public MainViewModel(IDataService data)
        {
            this.data = data;
            depot = data.Depot;
            MarketInformation = new ObservableCollection<ShareInformation>(data.LoadMarketInformation());
            PlaceBuyingOrderCommand = new RelayCommand(PlaceBuyingOrder, () => true);
            PlaceSellingOrderCommand = new RelayCommand(PlaceSellingOrder, () => true);
            LogoutCommand = new RelayCommand(Logout, () => true);
        }

        public string Email { get { return depot.Email; } }

        public double Budget { get { return depot.Budget; } }

        public ObservableCollection<ShareInformation> MarketInformation { get; set; }

        public ObservableCollection<ShareInformation> OwnedShares { get; set; }

        public ShareInformation SelectedBuyingShare { get; set; }

        public ShareInformation SelectedSellingShare { get; set; }

        public int NoOfSharesBuying { get; set; }

        public int NoOfSharesSelling { get; set; }

        public double UpperPriceLimit { get; set; }

        public double LowerPriceLimit { get; set; }

        public RelayCommand PlaceBuyingOrderCommand { get; private set; }

        public RelayCommand PlaceSellingOrderCommand { get; private set; }

        public RelayCommand LogoutCommand { get; private set; }

        private void OnNewMarketInformationAvailable(ShareInformation nu)
        {
            var tmp = MarketInformation.Where(x => x.FirmName.Equals(nu.FirmName));
            var old = tmp.Count() == 0 ? null : tmp.First();
            if (old != null)
            {
                MarketInformation.Insert(MarketInformation.IndexOf(old), nu);
                MarketInformation.Remove(old);
            }
            else
            {
                MarketInformation.Add(nu);
            }
        }

        private void PlaceBuyingOrder()
        {
            var id = Email + DateTime.Now.Ticks.ToString();
            var order = new Order() { Id = id, InvestorId = Email, Type = Order.OrderType.BUY, ShareName = SelectedBuyingShare.FirmName, Limit = UpperPriceLimit, TotalNoOfShares = NoOfSharesBuying };
            data.PlaceOrder(order);
        }

        private void PlaceSellingOrder()
        {
            var id = Email + DateTime.Now.Ticks.ToString();
            var order = new Order() { Id = id, InvestorId = Email, Type = Order.OrderType.SELL, ShareName = SelectedSellingShare.FirmName, Limit = LowerPriceLimit, TotalNoOfShares = NoOfSharesSelling };
            data.PlaceOrder(order);
        }

        private void Logout()
        {
            data.Dispose();
            App.Current.Shutdown();
        }
    }
}