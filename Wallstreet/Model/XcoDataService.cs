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
        private XcoDictionary<string, Tuple<int, double>> stockInformation;
        private XcoQueue<Registration> investorDepotRegistrations;
        private XcoDictionary<string, InvestorDepot> investorDepots;
        private XcoDictionary<string, Order> orders;
        private XcoList<Transaction> transactions;
        private IList<Action<ShareInformation>> marketCallbacks;
        private IList<Action<Order>> orderAddedCallbacks;
        private IList<Action<Order>> orderRemovedCallbacks;
        private IList<Action<Transaction>> transactionAddedCallbacks;

        public XcoDataService()
        {
            marketCallbacks = new List<Action<ShareInformation>>();
            orderAddedCallbacks = new List<Action<Order>>();
            orderRemovedCallbacks = new List<Action<Order>>();
            transactionAddedCallbacks = new List<Action<Transaction>>();
            space = new XcoSpace(0);
            stockInformation = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", spaceServerUri);
            stockInformation.AddNotificationForEntryAdd(OnShareInformationEntryAdded);
            investorDepotRegistrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", spaceServerUri);
            investorDepotRegistrations.AddNotificationForEntryEnqueued(OnRegistrationEntryAdded);
            investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServerUri);
            orders = space.Get<XcoDictionary<string, Order>>("Orders", spaceServerUri);
            orders.AddNotificationForEntryAdd(OnOrderEntryAdded);
            orders.AddNotificationForEntryRemove(OnOrderEntryRemoved);
            transactions = space.Get<XcoList<Transaction>>("Transactions", spaceServerUri);
            transactions.AddNotificationForEntryAdd(OnTransactionEntryAdded);

            processPendingRegistrations();
        }

        public IEnumerable<ShareInformation> LoadMarketInformation()
        {
            IList<ShareInformation> info = new List<ShareInformation>();
            foreach (string key in stockInformation.Keys)
            {
                var tuple = stockInformation[key];
                info.Add(new ShareInformation() { FirmName = key, NoOfShares = tuple.Item1, PricePerShare = tuple.Item2 });
            }

            return info;
        }

        public IEnumerable<Order> LoadOrders()
        {
            var info = new List<Order>();

            using (XcoTransaction transaction = space.BeginTransaction())
            {
                foreach (string key in orders.Keys)
                {
                    info.Add(orders[key]);
                }
                transaction.Commit();
            }
            return info;
        }

        public IEnumerable<Transaction> LoadTransactions()
        {
            var info = new List<Transaction>();

            using (XcoTransaction transaction = space.BeginTransaction())
            {
                for (int i = 0; i < transactions.Count; i++)
                {
                    info.Add(transactions[i]);
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

        public void AddNewTransactionAddedCallback(Action<Transaction> callback)
        {
            transactionAddedCallbacks.Add(callback);
        }

        private void processPendingRegistrations()
        {
            using (XcoTransaction tx = space.BeginTransaction())
            {

                while (investorDepotRegistrations.Count > 0)
                {
                    Registration reg = investorDepotRegistrations.Dequeue();
                    HandleRegistration(reg);
                }

                tx.Commit();
            }
        }

        #region XCO Container Callbacks

        private void OnRegistrationEntryAdded(XcoQueue<Registration> source, Registration r)
        {
            using(XcoTransaction tx = space.BeginTransaction())
            {
                Registration reg = source.Dequeue();

                HandleRegistration(reg);
                tx.Commit();
            }
        }

        private void HandleRegistration(Registration reg)
        {
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
        }

        private void OnShareInformationEntryAdded(XcoDictionary<string, Tuple<int, double>> source, string key, Tuple<int, double> info)
        {
            ExecuteOnGUIThread<ShareInformation>(marketCallbacks, new ShareInformation() { FirmName = key, NoOfShares = info.Item1, PricePerShare = info.Item2 });
        }

        private void OnOrderEntryAdded(XcoDictionary<string, Order> source, string key, Order order)
        {
            ExecuteOnGUIThread<Order>(orderAddedCallbacks, order);
        }

        private void OnOrderEntryRemoved(XcoDictionary<string, Order> source, string key, Order order)
        {
            ExecuteOnGUIThread<Order>(orderRemovedCallbacks, order);
        }

        private void OnTransactionEntryAdded(XcoList<Transaction> source, Transaction transaction, int index)
        {
            ExecuteOnGUIThread<Transaction>(transactionAddedCallbacks, transaction);
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
