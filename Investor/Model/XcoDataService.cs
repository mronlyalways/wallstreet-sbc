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
    class XcoDataService : ObservableObject, IDataService
    {
        private XcoSpace space;
        private XcoQueue<Registration> registrations;
        private XcoDictionary<string, InvestorDepot> investorDepots;
        private InvestorDepot depot;
        private Registration registration;
        private IList<Action<InvestorDepot>> callbacks;

        public XcoDataService()
        {
            this.callbacks = new List<Action<InvestorDepot>>();
            this.space = new XcoSpace(0);
            this.registrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            this.investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            this.investorDepots.AddNotificationForEntryAdd(OnNewInvestorDepotAdded);
        }

        public void Login(Registration r)
        {
            this.registration = r;
            this.registrations.Enqueue(r);
        }

        public void OnNewInvestorDepotAdded(XcoDictionary<string, InvestorDepot> source, string key, InvestorDepot d)
        {
            if (this.registration != null && this.registration.InvestorEmail == key)
            {
                this.depot = d;

                foreach (Action<InvestorDepot> callback in this.callbacks) {
                    callback(d);
                }   
            }
        }

        public void OnUpdateForInvestorDepotAvailable(Action<InvestorDepot> callback)
        {
            this.callbacks.Add(callback);
        }

        public InvestorDepot Depot()
        {
            return this.depot;
        }
    }
}
