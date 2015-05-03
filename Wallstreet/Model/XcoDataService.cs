using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XcoSpaces;
using XcoSpaces.Collections;
using SharedFeatures.Model;

namespace Wallstreet.Model
{
    public class XcoDataService : IDataService, IDisposable
    {
        private readonly Uri spaceServerUri = new Uri("xco://" + Environment.MachineName + ":" + 9000);
        private XcoSpace space;
        private XcoDictionary<string, double> stockPrices;
        private XcoQueue<Registration> investorDepotRegistrations;
        private XcoDictionary<string, InvestorDepot> investorDepots;
        private XcoList<Order> orders;
        private IList<Action<ShareInformation>> marketCallbacks;
        private IList<Action<Order>> orderAddedCallbacks;
        private IList<Action<Order>> orderRemovedCallbacks;

        public XcoDataService()
        {
            marketCallbacks = new List<Action<ShareInformation>>();
            orderAddedCallbacks = new List<Action<Order>>();
            orderRemovedCallbacks = new List<Action<Order>>();
            space = new XcoSpace(0);
            stockPrices = space.Get<XcoDictionary<string, double>>("StockPrices", spaceServerUri);
            stockPrices.AddNotificationForEntryAdd(OnShareInformationEntryAdded);
            investorDepotRegistrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", spaceServerUri);
            investorDepotRegistrations.AddNotificationForEntryEnqueued(OnRegistrationEntryAdded);
            investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServerUri);
            orders = space.Get<XcoList<Order>>("Orders", spaceServerUri);
            orders.AddNotificationForEntryAdd(OnOrderEntryAdded);
            orders.AddNotificationForEntryRemove(OnOrderEntryRemoved);
        }

        public IEnumerable<ShareInformation> LoadMarketInformation()
        {
            IList<ShareInformation> info = new List<ShareInformation>();
            foreach (string key in stockPrices.Keys)
            {
                info.Add(new ShareInformation() { FirmName = key, PricePerShare = stockPrices[key], NoOfShares = 0 });
                // TODO: iterate through all investors and compute overall number of shares.
            }

            return info;
        }

        public IEnumerable<Order> LoadOrders()
        {
            IList<Order> info = new List<Order>();

            using (XcoTransaction transaction = space.BeginTransaction())
            {
                for (int i = 0; i < orders.Count; i++)
                {
                    info.Add(orders[i]);
                }
                transaction.Commit();
            }
            return info;
        }

        public void AddNewMarketInformationAvailableCallback(Action<ShareInformation> callback)
        {
            marketCallbacks.Add(callback);
        }

        public void AddNewOrderAddedCallback(Action<Order> callback)
        {
            orderAddedCallbacks.Add(callback);
        }

        public void AddOrderRemovedCallback(Action<Order> callback)
        {
            orderRemovedCallbacks.Add(callback);
        }

        #region XCO Container Callbacks

        private void OnRegistrationEntryAdded(XcoQueue<Registration> source, Registration r)
        {
            using(XcoTransaction tx = space.BeginTransaction())
            {
                Registration reg = source.Dequeue();

                InvestorDepot depot;
                if (investorDepots.ContainsKey(reg.Email)) 
                {
                   depot = investorDepots[reg.Email];
                }
                else
                {
                    depot = new InvestorDepot() { Email = reg.Email };
                }

                depot.Budget += reg.Budget;

                if (investorDepots.ContainsKey(reg.Email))
                {
                    investorDepots[reg.Email] = depot;
                }
                else
                {
                    investorDepots.Add(reg.Email, depot);
                }
                tx.Commit();
            }
        }

        private void OnShareInformationEntryAdded(XcoDictionary<string, double> source, string key, double price)
        {
            ExecuteOnGUIThread<ShareInformation>(marketCallbacks, new ShareInformation() { FirmName = key, NoOfShares = 0, PricePerShare = price });
        }

        private void OnOrderEntryAdded(XcoList<Order> source, Order order, int index)
        {
            ExecuteOnGUIThread<Order>(orderAddedCallbacks, order);
        }

        private void OnOrderEntryRemoved(XcoList<Order> source, Order order, int index)
        {
            ExecuteOnGUIThread<Order>(orderRemovedCallbacks, order);
        }

        #endregion

        private void ExecuteOnGUIThread<T>(IList<Action<T>> callbacks, T arg)
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
            space.Dispose();
        }
    }
}
