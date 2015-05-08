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
        private static XcoQueue<Request> requestsQ;
        private static XcoList<Order> orders;
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
                orders = space.Get<XcoList<Order>>("Orders", spaceServer);
                orderQueue = space.Get<XcoQueue<Order>>("OrderQueue", spaceServer);
                investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServer);
                firmDepots = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", spaceServer);
                stockInformation = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", spaceServer);
                stockInformationUpdates = space.Get<XcoQueue<string>>("StockInformationUpdates", spaceServer);

                orderQueue.AddNotificationForEntryEnqueued(OnOrderAddedToOrderQueue);
                stockInformationUpdates.AddNotificationForEntryEnqueued(OnShareInformationAddedToQueue);

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
                try
                {
                    requestsQ = space.Get<XcoQueue<Request>>("RequestQ", spaceServer);
                    Request request;
                    FirmDepot depot;
                    while (true)
                    {
                        request = requestsQ.Dequeue(-1);
                        using (XcoTransaction transaction = space.BeginTransaction())
                        {
                            try
                            {
                                if (firmDepots.TryGetValue(request.FirmName, out depot))
                                {
                                    depot.OwnedShares += request.Shares;
                                    firmDepots[request.FirmName] = depot;
                                    var info = stockInformation[request.FirmName];
                                    stockInformation[request.FirmName] = new Tuple<int, double>(info.Item1 + request.Shares, info.Item2);
                                    Console.WriteLine("Add {0} shares to existing account \"{1}\"", request.Shares, request.FirmName);
                                }
                                else
                                {
                                    firmDepots.Add(request.FirmName, new FirmDepot() { FirmName = request.FirmName, OwnedShares = request.Shares });
                                    stockInformation.Add(request.FirmName, new Tuple<int, double>(request.Shares, request.PricePerShare));
                                    Console.WriteLine("Create new firm depot for \"{0}\" with {1} shares, selling for {2}", request.FirmName, request.Shares, request.PricePerShare);
                                }
                                var orderId = request.FirmName + DateTime.Now.Ticks.ToString();
                                Order o = new Order()
                                {
                                    Id = orderId,
                                    InvestorId = request.FirmName,
                                    ShareName = request.FirmName,
                                    Type = Order.OrderType.SELL,
                                    Limit = 0,
                                    NoOfProcessedShares = 0,
                                    TotalNoOfShares = request.Shares
                                };
                                orderQueue.Enqueue(o);
                                transaction.Commit();
                            }
                            catch (Exception)
                            {
                                transaction.Rollback();
                                requestsQ.Enqueue(request);
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

        private static void OnShareInformationAddedToQueue(XcoQueue<string> queue, string shareKey) {

            //TODO Martin!!!

        }

        private static void OnOrderAddedToOrderQueue(XcoQueue<Order> queue, Order order)
        {
            Order o = null;
            try
            {
                o = queue.Dequeue(1000);
            }
            catch (XcoException e)
            {
                Console.WriteLine(e.Message);
            }
            
            if (o != null)
            {
                Console.WriteLine("Dequeued order: {0}", o);

                using (XcoTransaction tx = space.BeginTransaction())
                {
                    try
                    {
                        if (CanProcessOrder(o))
                        {
                            Console.WriteLine("Process order: " + o);
                            int i = -1;
                            ProcessOrder(o, i);
                            //Transaction will be committed within ProcessOrder(...)
                        }
                        else
                        {
                            orders.Add(o);
                            tx.Commit();
                        }

                    }
                    catch (XcoException e)
                    {
                        Console.WriteLine(e.Message);
                        tx.Rollback();
                        orderQueue.Enqueue(o);
                    }
                }

            }
        }

        private static bool CanProcessOrder(Order o)
        {
            int index = 0;
            return FindMatchingOrderFor(o, ref index) != null && (o.Status != Order.OrderStatus.DONE && o.Status != Order.OrderStatus.DELETED);
        }

        private static Order FindMatchingOrderFor(Order o, ref int index)
        {
            Order match = null;
            for (int i = 0; i < orders.Count; i++)
            {
                Order candidate = orders[i];

                if (o.ShareName == candidate.ShareName && (candidate.Status != Order.OrderStatus.DONE && candidate.Status != Order.OrderStatus.DELETED)) {
                    if ((o.Type.Equals(Order.OrderType.BUY) && candidate.Type.Equals(Order.OrderType.SELL) && o.Limit >= stockInformation[o.ShareName].Item2 && candidate.Limit <= stockInformation[o.ShareName].Item2) ||
                    (o.Type.Equals(Order.OrderType.SELL) && candidate.Type.Equals(Order.OrderType.BUY) && o.Limit <= stockInformation[o.ShareName].Item2 && candidate.Limit >= stockInformation[o.ShareName].Item2))
                    {
                        match = candidate;
                        index = i;
                    }
                }
            }

            return match;
        }

        private static Transaction calculateTransaction(Order o1, Order o2)
        {
            Order purchase = null;
            Order sell = null;
            if (o1.Type.Equals(Order.OrderType.BUY)) {
                purchase = o1;
                sell = o2;
            } else {
                purchase = o2;
                sell = o1;
            }

            var noOfSharesSold = Math.Min(o1.NoOfOpenShares, o2.NoOfOpenShares);

            Transaction t = new Transaction()
            {
                TransactionId = o1.Id + o2.Id,
                BrokerId = 1L,
                ShareName = o1.ShareName,
                BuyerId = purchase.InvestorId,
                SellerId = sell.InvestorId,
                BuyingOrderId = purchase.Id,
                SellingOrderId = sell.Id,
                NoOfSharesSold = noOfSharesSold,
                PricePerShare = stockInformation[o1.ShareName].Item2
            };

            return t;
        }

        private static bool IsAffordable(Transaction t)
        {
            return (t.TotalCost + t.Provision) <= investorDepots[t.BuyerId].Budget; 
        }

        private static bool HasEnoughShares(Transaction t)
        {
            bool enoughShares = false;

            if (investorDepots.ContainsKey(t.SellerId))
            {
                if (investorDepots[t.SellerId].Shares[t.ShareName] >= t.NoOfSharesSold)
                {
                    enoughShares = true;
                }
            }
            else if (firmDepots.ContainsKey(t.SellerId))
            {
                if (firmDepots[t.SellerId].OwnedShares >= t.NoOfSharesSold)
                {
                    enoughShares = true;
                }
            }

            return enoughShares;
        }

        private static void Punish(Order o1, int index1, Order o2, int index2, Transaction t)
        {

        }

        private static void ProcessOrder(Order o, int index)
        {
            int matchIndex = -1;
            Order match = FindMatchingOrderFor(o, ref matchIndex);

            if (match != null)
            {

                Transaction t = calculateTransaction(o, match);

                if (HasEnoughShares(t) && IsAffordable(t))
                {
                    PerformTransaction(o, index, matchIndex, match, t);
                    
                }
                else
                {
                    Punish(o, index, match, matchIndex, t);
                }
            }
        }

        private static void PerformTransaction(Order o, int index, int matchIndex, Order match, Transaction t)
        {
            o.NoOfProcessedShares += t.NoOfSharesSold;
            match.NoOfProcessedShares += t.NoOfSharesSold;
            o.Status = (o.NoOfOpenShares == 0) ? Order.OrderStatus.DONE : Order.OrderStatus.PARTIAL;
            match.Status = (match.NoOfOpenShares == 0) ? Order.OrderStatus.DONE : Order.OrderStatus.PARTIAL;

            var buyer = investorDepots[t.BuyerId];
            buyer.Budget -= (t.TotalCost + t.Provision);
            buyer.AddShares(t.ShareName, t.NoOfSharesSold);
            investorDepots[t.BuyerId] = buyer;

            if (investorDepots.ContainsKey(t.SellerId))
            {
                var seller = investorDepots[t.SellerId];
                seller.RemoveShares(t.ShareName, t.NoOfSharesSold);
                seller.Budget += t.TotalCost;
                investorDepots[t.SellerId] = seller;
            }
            else if (firmDepots.ContainsKey(t.SellerId))
            {
                var seller = firmDepots[t.SellerId];
                seller.OwnedShares -= t.NoOfSharesSold;
                firmDepots[t.SellerId] = seller;
            }

            if (index >= 0)
            {
                orders.RemoveAt(index);
            }

            if (matchIndex >= 0)
            {
                orders.RemoveAt(matchIndex);
            }

            XcoTransaction tx = space.CurrentTransaction;

            if (tx != null)
            {
                tx.Commit();
                orderQueue.Enqueue(match);
                orderQueue.Enqueue(o);
            }
        }
    }
}
