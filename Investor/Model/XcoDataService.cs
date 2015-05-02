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
        private XcoSpace space;
        private XcoQueue<Registration> registrations;
        private XcoDictionary<string, InvestorDepot> investorDepots;
        private IList<Action> callbacks;
        private Registration registration;

        public XcoDataService()
        {
            this.callbacks = new List<Action>();
            this.space = new XcoSpace(0);
            this.registrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            this.investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            this.investorDepots.AddNotificationForEntryAdd(OnNewInvestorDepotAdded);
        }

        public InvestorDepot Depot { get; private set; }

        public void Login(Registration r)
        {
            registration = r;
            this.registrations.Enqueue(r);
        }

        public void OnNewInvestorDepotAdded(XcoDictionary<string, InvestorDepot> source, string key, InvestorDepot d)
        {
            if (this.registration != null && this.registration.InvestorEmail == key)
            {
                Depot = d;
                foreach (Action callback in this.callbacks)
                {
                    App.Current.Dispatcher.BeginInvoke(new Action(() => callback()), null);
                }
                investorDepots.ClearNotificationForEntryAdd();
            }
        }

        public void OnRegistrationConfirmed(Action callback)
        {
            this.callbacks.Add(callback);
        }
    }
}
