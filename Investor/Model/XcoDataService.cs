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
        private IList<Action<IEnumerable<Order>>> pendingOrdersCallback;
        private IList<ShareInformation> shareInformationCache;
        private IList<Order> orderCache;
        private Registration registration;
        private InvestorDepot depot;

        public XcoDataService()
        {
            space = new XcoSpace(0);
            marketCallbacks = new List<Action<ShareInformation>>();
            investorDepotCallbacks = new List<Action<InvestorDepot>>();
            pendingOrdersCallback = new List<Action<IEnumerable<Order>>>();
            shareInformationCache = new List<ShareInformation>();
            orderCache = new List<Order>();
            depot = null;
            registrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", spaceServerUri);
            investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServerUri);
            investorDepots.AddNotificationForEntryAdd(OnInvestorDepotAdded);
            stockInformation = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", spaceServerUri);
            stockInformation.AddNotificationForEntryAdd(OnShareInformationAdded);
            orders = space.Get<XcoDictionary<string, Order>>("Orders", spaceServerUri);
            orders.AddNotificationForEntryAdd(OnNewOrderAdded);
            orders.AddNotificationForEntryRemove(OnOrderRemoved);
        }

        public void Login(Registration r)
        {
            registration = r;
            this.registrations.Enqueue(r);
        }

        public void PlaceOrder(Order order)
        {
            orders.Add(order.Id, order);
        }

        public void CancelOrder(Order order)
        {
            orders.Remove(order.Id);
        }

        public InvestorDepot LoadInvestorInformation()
        {
            return depot;
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

        public IEnumerable<Order> LoadPendingOrders()
        {
            if (orderCache.Count == 0)
            {
                LoadMarketInformation();
            }
            return orderCache.Where(x => x.InvestorId == depot.Email && x.NoOfOpenShares > 0);
        }

        public void AddNewMarketInformationAvailableCallback(Action<ShareInformation> callback)
        {
            marketCallbacks.Add(callback);
        }

        public void AddNewInvestorInformationAvailableCallback(Action<InvestorDepot> callback)
        {
            investorDepotCallbacks.Add(callback);
        }

        public void AddNewPendingOrdersCallback(Action<IEnumerable<Order>> callback)
        {
            pendingOrdersCallback.Add(callback);
        }

        public void RemoveNewInvestorInformationAvailableCallback(Action<InvestorDepot> callback)
        {
            investorDepotCallbacks.Remove(callback);
        }

        private void OnInvestorDepotAdded(XcoDictionary<string, InvestorDepot> source, string key, InvestorDepot d)
        {
            if (this.registration != null && this.registration.Email == key)
            {
                depot = d;
                ExecuteOnGUIThread(investorDepotCallbacks, d);
            }
        }

        private void OnShareInformationAdded(XcoDictionary<string, Tuple<int, double>> source, string key, Tuple<int, double> info)
        {
            var share = new ShareInformation() { FirmName = key, NoOfShares = info.Item1, PurchasingVolume = 0, SalesVolume = 0, PricePerShare = info.Item2 };
            shareInformationCache.Add(share);
            ExecuteOnGUIThread(marketCallbacks, share);
        }

        private void OnNewOrderAdded(XcoDictionary<string, Order> source, string key, Order order)
        {
            orderCache = orderCache.Where(x => x.Id != order.Id).ToList();
            orderCache.Add(order);
            UpdateShareInformation(order);
        }

        private void OnOrderRemoved(XcoDictionary<string, Order> source, string key, Order order)
        {
            orderCache = orderCache.Where(x => x.Id != order.Id).ToList();
            UpdateShareInformation(order);
        }

        private void UpdateShareInformation(Order order)
        {
            var match = shareInformationCache.Where(x => x.FirmName == order.ShareName);
            var share = match.Count() > 0 ? match.First() : null;
            if (share != null)
            {
                share.PurchasingVolume = GetPurchasingVolume(orderCache, order.ShareName);
                share.SalesVolume = GetSalesVolume(orderCache, order.ShareName);
                ExecuteOnGUIThread(marketCallbacks, share);

                if (depot != null && order.InvestorId == depot.Email)
                {
                    UpdatePendingOrders();
                }
            }
        }

        private void UpdatePendingOrders()
        {
            var pendingOrders = orderCache.Where(x => x.InvestorId == depot.Email && x.NoOfOpenShares > 0);
            ExecuteOnGUIThread(pendingOrdersCallback, pendingOrders);
        }

        private int GetPurchasingVolume(IEnumerable<Order> orders, string key)
        {
            return orders.Where(x => x.ShareName == key && x.Type == Order.OrderType.BUY).Sum(x => x.NoOfOpenShares);
        }

        private int GetSalesVolume(IEnumerable<Order> orders, string key)
        {
            return orders.Where(x => x.ShareName == key && x.Type == Order.OrderType.SELL).Sum(x => x.NoOfOpenShares);
        }

        private void ExecuteOnGUIThread<T>(IEnumerable<Action<T>> callbacks, T arg)
        {
            foreach (Action<T> callback in callbacks)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    callback(arg);
                }), null);
            }
        }

        public void Dispose()
        {
            space.Close();
        }
    }
}
