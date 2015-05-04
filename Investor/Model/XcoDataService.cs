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
        private Registration registration;

        public XcoDataService()
        {
            marketCallbacks = new List<Action<ShareInformation>>();
            investorDepotCallbacks = new List<Action<InvestorDepot>>();
            space = new XcoSpace(0);
            registrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", spaceServerUri);
            investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServerUri);
            investorDepots.AddNotificationForEntryAdd(OnInvestorDepotAdded);
            stockInformation = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", spaceServerUri);
            stockInformation.AddNotificationForEntryAdd(OnShareInformationAdded);
            orders = space.Get<XcoDictionary<string, Order>>("Orders", spaceServerUri);
        }

        public InvestorDepot Depot { get; private set; }

        public void Login(Registration r)
        {
            registration = r;
            this.registrations.Enqueue(r);
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

        public void PlaceOrder(Order order)
        {
            orders.Add(order.Id, order);
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
                    callback(new ShareInformation() { FirmName = key, NoOfShares = info.Item1, PricePerShare = info.Item2 });
                }), null);
            }
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
