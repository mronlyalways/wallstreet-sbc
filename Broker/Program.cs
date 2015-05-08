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
    public class Program
    {
        private static readonly Uri spaceServer = new Uri("xco://" + Environment.MachineName + ":" + 9000);
        private static XcoSpace space;
        private static XcoDictionary<string, Order> orders;
        private static XcoQueue<Order> orderQueue;
        private static XcoDictionary<string, InvestorDepot> investorDepots;
        private static XcoDictionary<string, FirmDepot> firmDepots;
        private static XcoList<Transaction> transactions;
        private static XcoDictionary<string, Tuple<int, double>> stockInformation;
        private static XcoQueue<string> stockInformationUpdates;

        static void Main(string[] args)
        {
            try
            {
                space = new XcoSpace(0);
                transactions = space.Get<XcoList<Transaction>>("Transactions", spaceServer);
                orders = space.Get<XcoDictionary<string, Order>>("Orders", spaceServer);
                orderQueue = space.Get<XcoQueue<Order>>("OrderQueue", spaceServer);
                investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServer);
                firmDepots = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", spaceServer);
                stockInformation = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", spaceServer);
                stockInformationUpdates = space.Get<XcoQueue<string>>("StockInformationUpdates", spaceServer);
              
                stockInformationUpdates.AddNotificationForEntryEnqueued(OnStockInformationEntryAdded);
                orderQueue.AddNotificationForEntryEnqueued(OnOrderEntryAdded);

                HandleRequests();
            }
            catch (XcoException)
            {
                Console.WriteLine("Error: Unable to reach server.\nPress enter to exit ...");
                Console.ReadLine();
                if (space != null && space.IsOpen) { space.Close(); }
            }
        }

        private static void HandleRequests()
        {
            using (XcoSpace space = new XcoSpace(0))
            {
                try
                {
                    XcoQueue<Request> q = space.Get<XcoQueue<Request>>("RequestQ", spaceServer);
                    XcoDictionary<string, FirmDepot> firmDepot = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", spaceServer);
                    XcoDictionary<string, Tuple<int, double>> stockInformation = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", spaceServer);
                    XcoDictionary<string, Order> orders = space.Get<XcoDictionary<string, Order>>("Orders", spaceServer);
                    Request request;
                    FirmDepot depot;
                    while (true)
                    {
                        request = q.Dequeue(-1);
                        using (XcoTransaction transaction = space.BeginTransaction())
                        {
                            try
                            {
                                if (firmDepot.TryGetValue(request.FirmName, out depot))
                                {
                                    depot.OwnedShares += request.Shares;
                                    firmDepot[request.FirmName] = depot;
                                    var info = stockInformation[request.FirmName];
                                    stockInformation[request.FirmName] = new Tuple<int, double>(info.Item1 + request.Shares, info.Item2);
                                    Console.WriteLine("Add {0} shares to existing account \"{1}\"", request.Shares, request.FirmName);
                                }
                                else
                                {
                                    firmDepot.Add(request.FirmName, new FirmDepot() { FirmName = request.FirmName, OwnedShares = request.Shares });
                                    stockInformation.Add(request.FirmName, new Tuple<int, double>(request.Shares, request.PricePerShare));
                                    Console.WriteLine("Create new firm depot for \"{0}\" with {1} shares, selling for {2}", request.FirmName, request.Shares, request.PricePerShare);
                                }
                                var orderId = request.FirmName + DateTime.Now.Ticks.ToString();
                                orderQueue.Enqueue((new Order()
                                {
                                    Id = orderId,
                                    InvestorId = request.FirmName,
                                    ShareName = request.FirmName,
                                    Type = Order.OrderType.SELL,
                                    Limit = 0,
                                    NoOfProcessedShares = 0,
                                    TotalNoOfShares = request.Shares
                                }));
                                transaction.Commit();
                            }
                            catch (Exception)
                            {
                                transaction.Rollback();
                                q.Enqueue(request);
                            }
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

        private static void OnOrderEntryAdded(XcoQueue<Order> source, Order order)
        {
            Order o = null;
            using (XcoTransaction tx = space.BeginTransaction())
            {
                try
                {
                    o = source.Dequeue(true);
                                     
                }
                catch (XcoException e)
                {
                    Console.WriteLine(e.Message);
                    tx.Rollback();
                }
            }

            if (o != null && !o.Status.Equals(Order.OrderStatus.DONE))
            {
                Console.WriteLine("Process order:" + o.ToString() + " " + o.Status);
                ProcessOrder(ref o);
            }
            else if (o != null && !orders.ContainsKey(o.Id))
            {
                orders.Add(o.Id, order);
            }
        }

        private static void OnStockInformationEntryAdded(XcoQueue<string> source, string value)
        {

            string key = null;
            using (XcoTransaction tx = space.BeginTransaction())
            {
                try { 
                    key = source.Dequeue(false);
                    tx.Commit();
                } catch(XcoException) {
                    //Queue seems to be emtpy already
                    tx.Rollback();
                }

            }

            Order purchaseOrder = null;

            using (XcoTransaction tx = space.BeginTransaction())
            {

                if (key != null)
                {

                    try
                    {
                        foreach (String orderId in orders.Keys)
                        {
                            Order o = orders[orderId];
                            if (o.ShareName == key && o.Type.Equals(Order.OrderType.BUY) && o.Limit <= stockInformation[key].Item2)
                            {
                                orders.Remove(o.Id);
                                purchaseOrder = o;
                            }
                        }
                    }
                    catch (XcoException e)
                    {
                        Console.WriteLine(e.Message);
                        tx.Rollback();
                    }

                }
            }

            if (purchaseOrder != null)
            {
                ProcessOrder(ref purchaseOrder);
            }
        }

        private static void ProcessOrder(ref Order order)
        {
            using (XcoTransaction tx = space.BeginTransaction())
            {
                try
                {
                    Tuple<int, double> temp;
                    Double sharePrice;
                    Console.WriteLine("1");
                    if (stockInformation.TryGetValue(order.ShareName, out temp))
                    {
                        sharePrice = temp.Item2;

                        Order useMatch = null;

                        List<String> keys = new List<string>(orders.Keys);
                        keys = keys.OrderBy(x => x).ToList();
                        Console.WriteLine("2");
                        foreach (String orderId in orders.Keys)
                        {
                            Order o = orders[orderId];
                            if (o.ShareName.Equals(order.ShareName))
                            {
                                if ((order.Type.Equals(Order.OrderType.BUY) && order.Limit >= sharePrice && o.Type.Equals(Order.OrderType.SELL) && o.Limit <= sharePrice) ||
                                    (order.Type.Equals(Order.OrderType.SELL) && order.Limit <= sharePrice && o.Type.Equals(Order.OrderType.BUY) && o.Limit >= sharePrice))
                                {
                                    useMatch = o;
                                    break;
                                }
                            }
                        }
                        Console.WriteLine("3");

                        if (useMatch != null)
                        {
                            Console.WriteLine("4");
                            Double stockPrice = sharePrice;
                            Console.WriteLine("Processes matching orders: " + order.Id + " " + useMatch.Id);


                            var sharesProcessed = Math.Min(order.NoOfOpenShares, useMatch.NoOfOpenShares);

                            var totalCost = sharesProcessed * stockPrice;

                            Boolean buyMode = order.Type.Equals(Order.OrderType.BUY) ? true : false;

                            Transaction t = new Transaction()
                            {
                                TransactionId = order.Id + useMatch.Id,
                                BrokerId = 1L,
                                ShareName = order.ShareName,
                                BuyerId = buyMode ? order.InvestorId : useMatch.InvestorId,
                                SellerId = buyMode ? useMatch.InvestorId : order.InvestorId,
                                BuyingOrderId = buyMode ? order.Id : useMatch.Id,
                                SellingOrderId = buyMode ? useMatch.Id : order.Id,
                                NoOfSharesSold = sharesProcessed,
                                PricePerShare = stockPrice
                            };

                            Boolean affordable = (t.TotalCost + t.Provision) <= investorDepots[t.BuyerId].Budget;
                            Boolean enoughShares = false;

                            if (investorDepots.ContainsKey(t.SellerId))
                            {
                                enoughShares = investorDepots[t.SellerId].Shares[t.ShareName] >= t.NoOfSharesSold;
                            }
                            else if (firmDepots.ContainsKey(t.SellerId))
                            {
                                enoughShares = firmDepots[t.SellerId].OwnedShares >= t.NoOfSharesSold;
                            }

                            if (affordable && enoughShares)
                            {

                                order.NoOfProcessedShares += sharesProcessed;
                                useMatch.NoOfProcessedShares += sharesProcessed;
                                order.Status = (order.NoOfOpenShares == 0) ? Order.OrderStatus.DONE : Order.OrderStatus.PARTIAL;
                                useMatch.Status = (useMatch.NoOfOpenShares == 0) ? Order.OrderStatus.DONE : Order.OrderStatus.PARTIAL;

                                var buyer = investorDepots[t.BuyerId];
                                buyer.Budget -= (t.TotalCost + t.Provision);
                                buyer.AddShares(t.ShareName, t.NoOfSharesSold);
                                investorDepots[t.BuyerId] = buyer;

                                InvestorDepot seller;
                                if (investorDepots.TryGetValue(t.SellerId, out seller)) // if yes, then investor, else firm
                                {
                                    seller.Budget += (t.TotalCost);
                                    seller.RemoveShares(t.ShareName, t.NoOfSharesSold);
                                    investorDepots[t.SellerId] = seller;
                                }
                                else
                                {
                                    var firm = firmDepots[t.ShareName];
                                    firm.OwnedShares -= t.NoOfSharesSold;
                                    firmDepots[t.ShareName] = firm;
                                }
                                transactions.Add(t);

                                if (order.Status.Equals(Order.OrderStatus.DONE))
                                {
                                    orders.Add(order.Id, order);
                                } else 
                                {
                                    orderQueue.Enqueue(order);
                                }

                                if (useMatch.Status.Equals(Order.OrderStatus.DONE))
                                {
                                    orders.Add(order.Id, order);
                                }
                                else
                                {
                                    orderQueue.Enqueue(useMatch);
                                }
                            }
                            else
                            {
                                if (!affordable && enoughShares)
                                {
                                    if (order.Type.Equals(Order.OrderType.SELL))
                                    {
                                        orderQueue.Enqueue(order);
                                    }
                                    else
                                    {
                                        orderQueue.Enqueue(useMatch);
                                    }
                                }
                                else if (affordable && !enoughShares)
                                {
                                    if (order.Type.Equals(Order.OrderType.BUY))
                                    {
                                        orderQueue.Enqueue(order);
                                    }
                                    else
                                    {
                                        orderQueue.Enqueue(useMatch);
                                    }
                                }
                            }
                        }
                        else
                        {
                            orders.Add(order.Id, order);
                        }
                    }
                    else
                    {
                        orderQueue.Enqueue(order);
                    }

                    tx.Commit();
                }
                catch (XcoException e)
                {
                    Console.WriteLine(e.Message);
                    tx.Rollback();
                    orderQueue.Enqueue(order);
                }
            }
        }

        private static bool IsAffordableForBuyer(IEnumerable<Transaction> transactions)
        {
            var moneyNeeded = transactions.Sum(x => x.TotalCost + x.Provision);
            var balance = investorDepots[transactions.First().BuyerId].Budget;
            return balance >= moneyNeeded;
        }

        /// <summary>
        /// Function returns a set of matching orders for the given order and the given counterparts. If you provide a buying order and a set of selling orders,
        /// the result is a tuple consisting of the updated (i.e. adapted processed shares and status) buying order, the set of used selling orders (also updated) and the
        /// transactions that transfer the shares from buyer to seller.
        /// </summary>
        /// <param name="order">Buying or selling order that should be matched</param>
        /// <param name="counterParts">A list of selling orders (in case that order is a buying order) or vice versa</param>
        /// <param name="stockPrice">price that is used for the deal</param>
        /// <returns>A list of matching counterpart orders</returns>
        /*
        public static IEnumerable<Order> MatchOrders(Order order, double stockPrice)
        {
            //var transactions = new List<Transaction>();
            //var usedOrders = new List<Order>();
            //var sharesProcessedTotal = 0;
            /*
            while (matches.Count > 0 && sharesProcessedTotal <= order.NoOfOpenShares)
            {
                var match = matches.First();

                var sharesProcessed = Math.Min(order.NoOfOpenShares, match.NoOfOpenShares);
                
                var totalCost = sharesProcessed * stockPrice;
                transactions.Add(new Transaction()
                {
                    TransactionId = order.Id + match.Id,
                    BrokerId = 1L,
                    ShareName = order.ShareName,
                    BuyerId = buyMode ? order.InvestorId : match.InvestorId,
                    SellerId = buyMode ? match.InvestorId : order.InvestorId,
                    BuyingOrderId = buyMode ? order.Id : match.Id,
                    SellingOrderId = buyMode ? match.Id : order.Id,
                    NoOfSharesSold = sharesProcessed,
                    PricePerShare = stockPrice
                });
                 
                order.NoOfProcessedShares += sharesProcessed;
                match.NoOfProcessedShares += sharesProcessed;
                order.Status = (order.NoOfOpenShares == 0) ? Order.OrderStatus.DONE : Order.OrderStatus.PARTIAL;
                match.Status = (match.NoOfOpenShares == 0) ? Order.OrderStatus.DONE : Order.OrderStatus.PARTIAL;
                matches.Remove(match);
                
                usedOrders.Add(match);
                sharesProcessedTotal += sharesProcessed;
            }
        
            return matches;
        }
         */
    }
}
