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
        private XcoDictionary<string, double> stockPrices;
        private XcoList<Order> orders;
        private IList<Action> registrationCallbacks;
        private IList<Action<ShareInformation>> marketCallbacks;
        private Registration registration;

        public XcoDataService()
        {
            registrationCallbacks = new List<Action>();
            marketCallbacks = new List<Action<ShareInformation>>();
            space = new XcoSpace(0);
            registrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", spaceServerUri);
            investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServerUri);
            investorDepots.AddNotificationForEntryAdd(OnInvestorDepotAdded);
            stockPrices = space.Get<XcoDictionary<string, double>>("StockPrices", spaceServerUri);
            stockPrices.AddNotificationForEntryAdd(OnShareInformationAdded);
            orders = space.Get<XcoList<Order>>("Orders", spaceServerUri);
        }

        public InvestorDepot Depot { get; private set; }

        public void Login(Registration r)
        {
            registration = r;
            this.registrations.Enqueue(r);
        }

        public void AddRegistrationConfirmedCallback(Action callback)
        {
            this.registrationCallbacks.Add(callback);
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

        public void AddNewMarketInformationAvailableCallback(Action<ShareInformation> callback)
        {
            marketCallbacks.Add(callback);
        }

        public void PlaceOrder(Order order)
        {
            orders.Add(order);
        }

        private void OnInvestorDepotAdded(XcoDictionary<string, InvestorDepot> source, string key, InvestorDepot d)
        {
            if (this.registration != null && this.registration.Email == key)
            {
                Depot = d;
                foreach (Action callback in this.registrationCallbacks)
                {
                    App.Current.Dispatcher.BeginInvoke(new Action(() => callback()), null);
                }
                investorDepots.ClearNotificationForEntryAdd();
            }
        }

        private void OnShareInformationAdded(XcoDictionary<string, double> source, string key, double price)
        {
            foreach (Action<ShareInformation> callback in marketCallbacks)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    callback(new ShareInformation() { FirmName = key, NoOfShares = 0, PricePerShare = price });
                }), null);
            }
        }

        public void Dispose()
        {
            space.Remove(registrations);
            space.Remove(investorDepots);
            space.Remove(stockPrices);
            space.Remove(orders);
            space.Close();
        }
    }
}
