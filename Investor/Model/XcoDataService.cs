using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;
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
        private XcoList<ShareInformation> stockInformation;
        private XcoList<Order> orders;
        private XcoQueue<Order> orderQueue;
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
            stockInformation = space.Get<XcoList<ShareInformation>>("StockInformation", spaceServerUri);
            stockInformation.AddNotificationForEntryAdd(OnShareInformationAdded);
            orders = space.Get<XcoList<Order>>("Orders", spaceServerUri);
            orders.AddNotificationForEntryAdd(OnNewOrderAdded);
            orderQueue = space.Get<XcoQueue<Order>>("OrderQueue", spaceServerUri);
        }

        public void Login(Registration r)
        {
            registration = r;
            using (XcoTransaction tx = space.BeginTransaction())
            {
                try
                {
                    this.registrations.Enqueue(r);
                    tx.Commit();
                }
                catch (XcoException e)
                {
                    Console.WriteLine("Investor: " + e.Message);
                    tx.Rollback();
                }
            }
        }

        public void PlaceOrder(Order order)
        {
            orderQueue.Enqueue(order);
        }

        public void CancelOrder(Order order)
        {
            using (XcoTransaction tx = space.BeginTransaction())
            {

                try
                {
                    int index = -1;
                    for (int i = 0; i < orders.Count; i++)
                    {
                        
                        if (orders[i].Id.Equals(order.Id))
                        {

                            Console.WriteLine(orders[i]);
                            index = i;
                            break;
                        }
                    }

                    if (index >= 0)
                    {
                        orders.RemoveAt(index);
                        order.Status = Order.OrderStatus.DELETED;
                        orders.Add(order);
                    }

                    tx.Commit();
                }
                catch (XcoException e)
                {
                    tx.Rollback();
                }

            }
        }

        public InvestorDepot LoadInvestorInformation()
        {
            return depot;
        }

        public IEnumerable<ShareInformation> LoadMarketInformation()
        {
            shareInformationCache = new List<ShareInformation>();
            orderCache = new List<Order>();

            for (int i = 0; i < orders.Count; i++)
            {
                orderCache.Add(orders[i]);
            }
            using (XcoTransaction tx = space.BeginTransaction())
            {
                try
                {

                        for (int i = 0; i < stockInformation.Count; i++) {
                            ShareInformation s = stockInformation[i];

                        shareInformationCache.Add(new ShareInformation()
                        {
                            FirmName = s.FirmName,
                            NoOfShares = s.NoOfShares,
                            PurchasingVolume = GetPurchasingVolume(orderCache, s.FirmName),
                            SalesVolume = GetSalesVolume(orderCache, s.FirmName),
                            PricePerShare = s.PricePerShare
                        });
                    }

                    tx.Commit();
                }
                catch (XcoException e)
                {
                    Console.WriteLine("Investor: " + e.Message);
                    tx.Rollback();
                }
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

        private void OnShareInformationAdded(XcoList<ShareInformation> source, ShareInformation share, int index)
        {
            shareInformationCache.Add(share);
            ExecuteOnGUIThread(marketCallbacks, share);
        }

        private void OnNewOrderAdded(XcoList<Order> source, Order order, int index)
        {
            orderCache = orderCache.Where(x => x.Id != order.Id).ToList();
            orderCache.Add(order);
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
            var pendingOrders = orderCache.Where(x => x.InvestorId == depot.Email && x.NoOfOpenShares > 0 && x.Status != Order.OrderStatus.DONE && x.Status != Order.OrderStatus.DELETED);
            ExecuteOnGUIThread(pendingOrdersCallback, pendingOrders);
        }

        private int GetPurchasingVolume(IEnumerable<Order> orders, string key)
        {
            return orders.Where(x => x.ShareName == key && x.Type == Order.OrderType.BUY && x.Status != Order.OrderStatus.DONE && x.Status != Order.OrderStatus.DELETED).Sum(x => x.NoOfOpenShares);
        }

        private int GetSalesVolume(IEnumerable<Order> orders, string key)
        {
            return orders.Where(x => x.ShareName == key && x.Type == Order.OrderType.SELL && x.Status != Order.OrderStatus.DONE && x.Status != Order.OrderStatus.DELETED).Sum(x => x.NoOfOpenShares);
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
