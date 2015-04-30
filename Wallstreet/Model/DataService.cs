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
        private XcoSpace space;
        private XcoDictionary<string, double> stockPrices;
        private XcoQueue<Registration> investorDepotRegistrations;
        private XcoDictionary<string, InvestorDepot> investorDepots;
        private IList<Action<ShareInformation>> callbacks;

        public XcoDataService()
        {
            callbacks = new List<Action<ShareInformation>>();
            space = new XcoSpace(0);
            stockPrices = space.Get<XcoDictionary<string, double>>("StockPrices", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            stockPrices.AddNotificationForEntryAdd(OnEntryAdded);
            investorDepotRegistrations = space.Get<XcoQueue<Registration>>("InvestorRegistrations", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            investorDepotRegistrations.AddNotificationForEntryEnqueued(OnNewInvestorRegistrationAvailable);
            investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            
        }

        public IEnumerable<ShareInformation> LoadShareInformation()
        {
            IList<ShareInformation> info = new List<ShareInformation>();
            foreach (string key in stockPrices.Keys)
            {
                info.Add(new ShareInformation() { FirmName = key, PricePerShare = stockPrices[key], NoOfShares = 0 });
                // TODO: iterate through all investors and compute overall number of shares.
            }

            return info;
        }

        public void OnNewShareInformationAvailable(Action<ShareInformation> callback)
        {
            callbacks.Add(callback);
        }

        private void OnEntryAdded(XcoDictionary<string, double> source, string key, double price)
        {
            foreach (Action<ShareInformation> callback in callbacks)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    callback(new ShareInformation() { FirmName = key, NoOfShares = 0, PricePerShare = price });
                }), null);
                
            }
        }

        public void OnNewInvestorRegistrationAvailable(XcoQueue<Registration> source, Registration r)
        {
            using(XcoTransaction tx = space.BeginTransaction())
            {
                Registration reg = source.Dequeue();

                InvestorDepot depot;
                if (investorDepots.ContainsKey(reg.InvestorEmail)) 
                {
                   investorDepots.TryGetValue(reg.InvestorEmail, out depot);
                }
                else
                {
                    depot = new InvestorDepot();
                }

                depot.Budget += reg.Budget;

                if (investorDepots.ContainsKey(reg.InvestorEmail))
                {
                    investorDepots[reg.InvestorEmail] = depot;
                }
                else
                {
                    investorDepots.Add(reg.InvestorEmail, depot);
                }
                tx.Commit();
            }
        }

        public void Dispose()
        {
            space.Dispose();
        }
    }
}
