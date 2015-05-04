using GalaSoft.MvvmLight;
using SharedFeatures.Model;
using System.Collections.Generic;
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
            MarketInformation = new ObservableCollection<ShareInformation>(data.LoadMarketInformation());
            Transactions = new ObservableCollection<Transaction>(data.LoadTransactions());
            var orders = data.LoadOrders();
            BuyingOrders = new ObservableCollection<Order>(orders.Where(x => x.Type == Order.OrderType.BUY));
            SellingOrders = new ObservableCollection<Order>(orders.Where(x => x.Type == Order.OrderType.SELL));

            data.AddNewMarketInformationAvailableCallback(OnNewMarketInformationAvailable);
            data.AddNewOrderAddedCallback(OnNewOrderAdded);
            data.AddNewTransactionAddedCallback(Transactions.Add);
        }

        public ObservableCollection<ShareInformation> MarketInformation { get; set; }

        public ObservableCollection<Transaction> Transactions { get; set; }

        public ObservableCollection<Order> BuyingOrders { get; set; }

        public ObservableCollection<Order> SellingOrders { get; set; }

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

        private void OnNewOrderAdded(Order order)
        {
            if (order.Type == Order.OrderType.BUY)
            {
                BuyingOrders.Add(order);
            }
            else
            {
                SellingOrders.Add(order);
            }
        }

        private void OnOrderRemoved(Order order)
        {
            if (order.Type == Order.OrderType.BUY)
            {
                BuyingOrders.Remove(order);
            }
            else
            {
                SellingOrders.Remove(order);
            }
        }
    }
}