using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;
using SharedFeatures.Model;

namespace Wallstreet.Model
{
    public class XcoDataService : IDataService, IDisposable
    {
        private readonly Uri spaceServerUri = new Uri("xco://" + Environment.MachineName + ":" + 9000);
        private XcoSpace space;
        private XcoList<ShareInformation> stockInformation;
        private XcoQueue<Registration> investorDepotRegistrations;
        private XcoDictionary<string, InvestorDepot> investorDepots;
        private XcoQueue<FundRegistration> fundDepotRegistrations;
        private XcoList<FundDepot> fundDepots;
        private XcoDictionary<string, FirmDepot> firmDepots;
        private XcoQueue<FundDepot> fundDepotQueue;
        private XcoList<Order> orders;
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
            stockInformation = space.Get<XcoList<ShareInformation>>("StockInformation", spaceServerUri);
            stockInformation.AddNotificationForEntryAdd(OnShareInformationEntryAdded);
            investorDepotRegistrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", spaceServerUri);
            investorDepotRegistrations.AddNotificationForEntryEnqueued(OnRegistrationEntryAdded);
            investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServerUri);
            fundDepotRegistrations = space.Get<XcoQueue<FundRegistration>>("FundRegistrations", spaceServerUri);
            fundDepotRegistrations.AddNotificationForEntryEnqueued(OnFundRegistrationEntryAdded);
            fundDepots = space.Get<XcoList<FundDepot>>("FundDepots", spaceServerUri);
            fundDepotQueue = space.Get<XcoQueue<FundDepot>>("FundDepotQueue", spaceServerUri);
            firmDepots = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", spaceServerUri);
            orders = space.Get<XcoList<Order>>("Orders", spaceServerUri);
            orders.AddNotificationForEntryAdd(OnOrderEntryAdded);
            orders.AddNotificationForEntryRemove(OnOrderEntryRemoved);
            transactions = space.Get<XcoList<Transaction>>("Transactions", spaceServerUri);
            transactions.AddNotificationForEntryAdd(OnTransactionEntryAdded);

            processPendingRegistrations();
        }

        public IEnumerable<ShareInformation> LoadMarketInformation()
        {
            IList<ShareInformation> info = new List<ShareInformation>();

            using (XcoTransaction tx = space.BeginTransaction())
            {
                try
                {

                    for (int i = 0; i < stockInformation.Count; i++)
                    {
                        info.Add(stockInformation[i]);
                    }

                        tx.Commit();
                }
                catch (XcoException e)
                {
                    Console.WriteLine("Wallstreet: " + e.Message);
                    tx.Rollback();
                }
            }

            return info;
        }

        public IEnumerable<Order> LoadOrders()
        {
            var info = new List<Order>();

            using (XcoTransaction transaction = space.BeginTransaction())
            {
                for(int i = 0; i < orders.Count; i++) {
                    info.Add(orders[i]);
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
                try
                {
                    Registration reg = source.Dequeue();

                    HandleRegistration(reg);
                    tx.Commit();
                }
                catch (XcoException e)
                {
                    Console.WriteLine(e.Message);
                    tx.Rollback();
                }
            }
        }

        private void OnFundRegistrationEntryAdded(XcoQueue<FundRegistration> source, FundRegistration r)
        {
            using (XcoTransaction tx = space.BeginTransaction())
            {
                try
                {
                    FundRegistration reg = source.Dequeue();

                    HandleFundRegistration(reg);
                    tx.Commit();
                }
                catch (XcoException e)
                {
                    Console.WriteLine(e.Message);
                    tx.Rollback();
                }
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

        private void HandleFundRegistration(FundRegistration reg)
        {
            FundDepot depot = Utils.FindFundDepot(fundDepots, reg.FundID);
            if (depot == null)
            {
                if (!investorDepots.ContainsKey(reg.FundID) && !firmDepots.ContainsKey(reg.FundID))
                {
                    depot = new FundDepot() { FundID = reg.FundID, FundShares = reg.FundShares, FundAssets = reg.FundAssets };
                    fundDepots.Add(depot);
                    fundDepotQueue.Enqueue(depot);
                }
            }
        }

        private void OnShareInformationEntryAdded(XcoList<ShareInformation> source, ShareInformation share, int index)
        {
            ExecuteOnGUIThread(marketCallbacks, share);
        }

        private void OnOrderEntryAdded(XcoList<Order> source, Order order, int key)
        {
            ExecuteOnGUIThread(orderAddedCallbacks, order);
        }

        private void OnOrderEntryRemoved(XcoList<Order> source, Order order, int key)
        {
            ExecuteOnGUIThread(orderRemovedCallbacks, order);
        }

        private void OnTransactionEntryAdded(XcoList<Transaction> source, Transaction transaction, int index)
        {
            ExecuteOnGUIThread(transactionAddedCallbacks, transaction);
        }

        #endregion

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
            space.Dispose();
        }
    }
}
