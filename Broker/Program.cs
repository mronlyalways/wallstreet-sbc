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
        private static readonly Uri spaceServer = new Uri("xco://" + Environment.MachineName + ":" + 9000);
        //private static XcoSpace space;
        //private static IList<Order> pendingBuyingOrders;
        private static IList<Order> pendingSellingOrders;
        private static Dictionary<string, double> stockPrices;

        static void Main(string[] args)
        {
            using (XcoSpace space = new XcoSpace(0))
            {
                HandleRequests(space);
            }
        }

        private static void HandleRequests(XcoSpace space)
        {
            try
            {
                XcoQueue<Request> q = space.Get<XcoQueue<Request>>("RequestQ", spaceServer);
                XcoDictionary<string, FirmDepot> firmDepot = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", spaceServer);
                XcoDictionary<string, double> stockPrices = space.Get<XcoDictionary<string, double>>("StockPrices", spaceServer);
                XcoList<Order> orders = space.Get<XcoList<Order>>("Orders", spaceServer);
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

                    orders.Add(new Order() { Id = request.FirmName + DateTime.Now.Ticks.ToString(), InvestorId = request.FirmName + "@foo", ShareName = request.FirmName, Type = Order.OrderType.SELL, Limit = 0, NoOfProcessedShares = 0, TotalNoOfShares = request.Shares });
                }
            }
            catch (XcoException e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("Unable to reach server.\nPress enter to exit.");
                Console.ReadLine();
            }
        }

        private static void OnOrderEntryAdded(XcoList<Order> source, Order order, int index)
        {
            var currentPrice = stockPrices[order.ShareName];
            if (order.Type == Order.OrderType.BUY)
            {
                var match = pendingSellingOrders.Where(x => x.ShareName.Equals(order.ShareName) && x.Limit <= currentPrice && order.Limit >= currentPrice).First();




            }
            else
            {

            }
        }
    }
}
