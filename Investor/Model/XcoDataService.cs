using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XcoSpaces;
using XcoSpaces.Collections;
using SharedFeatures;
using SharedFeatures.Model;

namespace Investor.Model
{
    class XcoDataService : IDataService
    {
        private XcoSpace space;
        private XcoQueue<Registration> registrations;
        private XcoDictionary<string, InvestorDepot> investorDepots;
        private InvestorDepot depot;
        private Registration registration;

        public event System.EventHandler DepotHasUpdates;

        public XcoDataService()
        {
            this.space = new XcoSpace(0);
            this.registrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            this.investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            this.investorDepots.AddNotificationForEntryAdd(OnNewInvestorDepotAdded);
        }

        public void login(Registration r)
        {
            this.registration = r;
            this.registrations.Enqueue(r);
        }

        public void OnNewInvestorDepotAdded(XcoDictionary<string, InvestorDepot> source, string key, InvestorDepot d)
        {
            if (this.registration != null && this.registration.InvestorEmail == key)
            {
                Console.WriteLine("Received depot!");
                this.depot = d;
                if (DepotHasUpdates != null)
                {
                    DepotHasUpdates(this, EventArgs.Empty);
                }    
            }
        }

        public InvestorDepot Depot()
        {
            return this.depot;
        }
    }
}
