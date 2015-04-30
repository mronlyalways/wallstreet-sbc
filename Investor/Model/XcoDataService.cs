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

        public XcoDataService()
        {
            this.space = new XcoSpace(0);
            this.registrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            this.investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));
        }

        public void login(Registration r)
        {
            this.registrations.Enqueue(r);
        }
    }
}
