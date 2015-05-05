using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XcoSpaces;
using XcoSpaces.Collections;
using SharedFeatures;
using SharedFeatures.Model;
using GalaSoft.MvvmLight;

namespace Investor.Model
{
    class XcoDataService : IDataService
    {
        private readonly Uri spaceServerUri = new Uri("xco://" + Environment.MachineName + ":" + 9000);
        private XcoSpace space;
        private XcoQueue<Registration> registrations;
        private XcoDictionary<string, InvestorDepot> investorDepots;
        private XcoDictionary<string, Tuple<int, double>> stockInformation;
        private XcoDictionary<string, Order> orders;
        private IList<Action<ShareInformation>> marketCallbacks;
        private IList<Action<InvestorDepot>> investorDepotCallbacks;
        private IList<ShareInformation> shareInformationCache;
        private IList<Order> orderCache;

        private Registration registration;

        public XcoDataService()
        {
            space = new XcoSpace(0);
            marketCallbacks = new List<Action<ShareInformation>>();
            investorDepotCallbacks = new List<Action<InvestorDepot>>();
            shareInformationCache = new List<ShareInformation>();
            orderCache = new List<Order>();
            registrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", spaceServerUri);
            investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServerUri);
            investorDepots.AddNotificationForEntryAdd(OnInvestorDepotAdded);
            stockInformation = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", spaceServerUri);
            stockInformation.AddNotificationForEntryAdd(OnShareInformationAdded);
            orders = space.Get<XcoDictionary<string, Order>>("Orders", spaceServerUri);
            orders.AddNotificationForEntryAdd(OnNewOrderAdded);
            orders.AddNotificationForEntryRemove(OnOrderRemoved);
        }

        public InvestorDepot Depot { get; private set; }

        public void Login(Registration r)
        {
            registration = r;
            this.registrations.Enqueue(r);
        }

        public void Logout()
        {

        }

        public void PlaceOrder(Order order)
        {
            orders.Add(order.Id, order);
        }

        public IEnumerable<ShareInformation> LoadMarketInformation()
        {
            shareInformationCache = new List<ShareInformation>();
            orderCache = new List<Order>();
            foreach (string key in orders.Keys)
            {
                orderCache.Add(orders[key]);
            }
            
            foreach (string key in stockInformation.Keys)
            {
                var tuple = stockInformation[key];
                
                shareInformationCache.Add(new ShareInformation()
                {
                    FirmName = key,
                    NoOfShares = tuple.Item1,
                    PurchasingVolume = GetPurchasingVolume(orderCache, key),
                    SalesVolume = GetSalesVolume(orderCache, key),
                    PricePerShare = tuple.Item2
                });
            }

            return shareInformationCache;
        }

        public void AddNewMarketInformationAvailableCallback(Action<ShareInformation> callback)
        {
            marketCallbacks.Add(callback);
        }

        public void AddNewInvestorInformationAvailableCallback(Action<InvestorDepot> callback)
        {
            investorDepotCallbacks.Add(callback);
        }

        public void RemoveNewInvestorInformationAvailableCallback(Action<InvestorDepot> callback)
        {
            investorDepotCallbacks.Remove(callback);
        }

        private void OnInvestorDepotAdded(XcoDictionary<string, InvestorDepot> source, string key, InvestorDepot d)
        {
            if (this.registration != null && this.registration.Email == key)
            {
                Depot = d;
                foreach (Action<InvestorDepot> callback in investorDepotCallbacks)
                {
                    App.Current.Dispatcher.BeginInvoke(new Action(() => callback(d)), null);
                }
            }
        }

        private void OnShareInformationAdded(XcoDictionary<string, Tuple<int, double>> source, string key, Tuple<int, double> info)
        {
            foreach (Action<ShareInformation> callback in marketCallbacks)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    callback(new ShareInformation() { FirmName = key, NoOfShares = info.Item1, PurchasingVolume = 0, SalesVolume = 0, PricePerShare = info.Item2 });
                }), null);
            }
        }

        private void OnNewOrderAdded(XcoDictionary<string, Order> source, string key, Order order)
        {
            var share = shareInformationCache.Where(x => x.FirmName == order.ShareName).First();

            orderCache = orderCache.Where(x => x.Id != order.Id).ToList();
            orderCache.Add(order);

            share.PurchasingVolume = GetPurchasingVolume(orderCache, order.ShareName);
            share.SalesVolume = GetSalesVolume(orderCache, order.ShareName);
            foreach (Action<ShareInformation> callback in marketCallbacks)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    callback(share);
                }), null);
            }
        }

        private void OnOrderRemoved(XcoDictionary<string, Order> source, string key, Order order)
        {
            var share = shareInformationCache.Where(x => x.FirmName == order.ShareName).First();
            if (order.Type == Order.OrderType.BUY)
            {
                share.PurchasingVolume -= order.NoOfOpenShares;
            }
            else
            {
                share.SalesVolume -= order.NoOfOpenShares;
            }

            foreach (Action<ShareInformation> callback in marketCallbacks)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    callback(share);
                }), null);
            }
        }

        private int GetPurchasingVolume(IEnumerable<Order> orders, string key)
        {
            return orders.Where(x => x.ShareName == key && x.Type == Order.OrderType.BUY).Sum(x => x.NoOfOpenShares);
        }

        private int GetSalesVolume(IEnumerable<Order> orders, string key)
        {
            return orders.Where(x => x.ShareName == key && x.Type == Order.OrderType.SELL).Sum(x => x.NoOfOpenShares);
        }

        public void Dispose()
        {
            space.Remove(registrations);
            space.Remove(investorDepots);
            space.Remove(stockInformation);
            space.Remove(orders);
            investorDepots.ClearNotificationForEntryAdd();
            space.Close();
        }
    }
}
