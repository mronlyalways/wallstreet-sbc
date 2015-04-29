using SharedFeatures.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;

namespace SpaceServer
{
    class Server
    {
        static void Main(string[] args)
        {
            using (XcoSpace space = new XcoSpace(9000))
            {
                XcoQueue<Request> qRequests = new XcoQueue<Request>();
                space.Add(qRequests, "RequestQ");
                qRequests.AddNotificationForEntryEnqueued(OnRequestEntryEnqueued);

                XcoDictionary<string, FirmDepot> firmDepots = new XcoDictionary<string, FirmDepot>();
                space.Add(firmDepots, "FirmDepots");
                firmDepots.AddNotificationForEntryAdd(OnDepotEntryAdded);

                XcoDictionary<string, double> stockPrices = new XcoDictionary<string, double>();
                space.Add(stockPrices, "StockPrices");

                while (true)
                {
                    try
                    {
                        // subscribe to containers and log activity
                    }
                    catch (XcoException e)
                    {
                        // log errors
                    }
                }
            }
        }

        static void OnRequestEntryEnqueued(XcoQueue<Request> source, Request request)
        {
            Console.WriteLine("New request for {0}, publishing {1} shares for {2} Euros.", request.FirmName, request.Shares, request.PricePerShare);
        }

        static void OnDepotEntryAdded(XcoDictionary<string, FirmDepot> source, string key, FirmDepot depot)
        {
            Console.WriteLine("New depot entry for {0}, publishing {1} shares.", key, depot.OwnedShares);
        }
    }
}
