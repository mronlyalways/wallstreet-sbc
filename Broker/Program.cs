using SharedFeatures.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;

namespace Broker
{
    class Program
    {
        static void Main(string[] args)
        {
            using (XcoSpace space = new XcoSpace(0))
            {
                try
                {
                    XcoQueue<Request> q = space.Get<XcoQueue<Request>>("RequestQ", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                    XcoDictionary<string, FirmDepot> firmDepot = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                    XcoDictionary<string, double> stockPrices = space.Get<XcoDictionary<string, double>>("StockPrices", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                    Request request;
                    FirmDepot depot;
                    while (true)
                    {
                        request = q.Dequeue(-1);

                        if (firmDepot.TryGetValue(request.FirmName, out depot))
                        {
                            depot.OwnedShares += request.Shares;
                            firmDepot[request.FirmName] = depot;
                            Console.WriteLine("Add {0} shares to existing account \"{1}\"", request.Shares, request.FirmName);
                        }
                        else
                        {
                            firmDepot.Add(request.FirmName, new FirmDepot() { FirmName = request.FirmName, OwnedShares = request.Shares });
                            stockPrices.Add(request.FirmName, request.PricePerShare);
                            Console.WriteLine("Create new firm depot for \"{0}\" with {1} shares, selling for {2}", request.FirmName, request.Shares, request.PricePerShare);
                        }
                    }
                }
                catch (XcoException e)
                {
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine("Unable to reach server.\nPress enter to exit.");
                    Console.ReadLine();
                }
            }
        }
    }
}
