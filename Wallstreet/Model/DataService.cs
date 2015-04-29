using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XcoSpaces;
using XcoSpaces.Collections;

namespace Wallstreet.Model
{
    public class XcoDataService : IDataService, IDisposable
    {
        private XcoSpace space;
        private XcoDictionary<string, double> stockPrices;
        private IList<Action<ShareInformation>> callbacks;

        public XcoDataService()
        {
            callbacks = new List<Action<ShareInformation>>();
            space = new XcoSpace(0);
            stockPrices = space.Get<XcoDictionary<string, double>>("StockPrices", new Uri("xco://" + Environment.MachineName + ":" + 9000));
            stockPrices.AddNotificationForEntryAdd(OnEntryAdded);
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

        public void Dispose()
        {
            space.Dispose();
        }
    }
}
