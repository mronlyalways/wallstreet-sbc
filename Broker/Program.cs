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
        private static XcoDictionary<string, string> orderCoordinator;
        private static XcoDictionary<string, InvestorDepot> investorDepots;
        private static XcoDictionary<string, FirmDepot> firmDepots;
        private static XcoList<Transaction> transactions;
        private static IList<Order> pendingOrders;
        private static Dictionary<string, double> stockPriceCache;

        static void Main(string[] args)
        {
            try
            {
                space = new XcoSpace(0);
                transactions = space.Get<XcoList<Transaction>>("Transactions", spaceServer);
                orders = space.Get<XcoDictionary<string, Order>>("Orders", spaceServer);
                orderCoordinator = space.Get<XcoDictionary<string, string>>("OrderCoordinator", spaceServer);
                investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServer);
                firmDepots = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", spaceServer);
                XcoDictionary<string, Tuple<int, double>> stockInformation = space.Get<XcoDictionary<string, Tuple<int, double>>>("StockInformation", spaceServer);
                stockInformation.AddNotificationForEntryAdd(OnStockInformationEntryAdded);
                orders.AddNotificationForEntryAdd(OnOrderEntryAdded);
                orders.AddNotificationForEntryRemove(OnOrderEntryRemoved);
                pendingOrders = new List<Order>();
                stockPriceCache = new Dictionary<string, double>();

                using (XcoTransaction transaction = space.BeginTransaction())
                {
                    foreach (string key in orders.Keys)
                    {
                        pendingOrders.Add(orders[key]);
                    }

                    foreach (string key in stockInformation.Keys)
                    {
                        stockPriceCache.Add(key, stockInformation[key].Item2);
                    }

                    transaction.Commit();
                }
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
                                orders.Add(orderId, (new Order()
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

        private static void OnOrderEntryAdded(XcoDictionary<string, Order> source, string key, Order order)
        {
            if (LockId(order.Id) && !pendingOrders.Contains(order) && order.Status != Order.OrderStatus.DONE)
            {
                ProcessOrder(order);
                pendingOrders = pendingOrders.Where(x => x.Status != Order.OrderStatus.DONE).ToList();
                ReleaseLock(order.Id);
            }
        }

        private static void OnOrderEntryRemoved(XcoDictionary<string, Order> source, string key, Order order)
        {
            pendingOrders.Remove(order);
        }

        private static void OnStockInformationEntryAdded(XcoDictionary<string, Tuple<int, double>> source, string key, Tuple<int, double> value)
        {
            stockPriceCache[key] = value.Item2;
            LockId(key);
            for (int i = 0; i < pendingOrders.Count; i++)
            {
                var o = pendingOrders[i];
                if (o.Type == Order.OrderType.BUY && o.ShareName == key)
                {
                    
                    ProcessOrder(o);
                    pendingOrders = pendingOrders.Where(x => x.Status != Order.OrderStatus.DONE).ToList();
                }
            }
            ReleaseLock(key);
        }

        private static bool LockId(string id)
        {
            bool work;
            try
            {
                using (XcoTransaction transaction = space.BeginTransaction())
                {
                    string o;
                    work = !orderCoordinator.TryGetValue(id, out o, false);
                    {
                        orderCoordinator[id] = id;
                    }
                    transaction.Commit();
                }
            }
            catch (XcoException)
            {
                return false;
            }
            return work;
        }

        private static void ReleaseLock(string id)
        {
            orderCoordinator.Remove(id);
        }

        private static void ProcessOrder(Order order)
        {
            var result = MatchOrders(order, pendingOrders.Where(x => x.Type != order.Type).ToList(), stockPriceCache[order.ShareName]);
            var newOrder = result.Item1;
            var matches = result.Item2;
            var newTransactions = result.Item3;
            pendingOrders.Add(newOrder);
            if (matches.Count() > 0)
            {
                if (IsAffordableForBuyer(newTransactions))
                {
                    var oldMatches = pendingOrders.Where(x => matches.Select(y => y.Id).Contains(x.Id));
                    pendingOrders = pendingOrders.Except(oldMatches).Union(matches).ToList();

                    using (XcoTransaction transaction = space.BeginTransaction())
                    {
                        orders[newOrder.Id] = newOrder;
                        foreach (Order m in matches)
                        {
                            orders[m.Id] = m;
                        }
                        foreach (Transaction t in newTransactions)
                        {
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
                        }
                        transaction.Commit();
                    }
                }
                else
                {
                    pendingOrders.Remove(newOrder);
                    orders.Remove(order.ShareName);
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
        /// <returns>A tuple consisting of the updated order, a list of matching (and used) counterpart orders and the respective transactions</returns>
        public static Tuple<Order, IEnumerable<Order>, IEnumerable<Transaction>> MatchOrders(Order order, IList<Order> counterParts, double stockPrice)
        {
            var transactions = new List<Transaction>();
            var usedOrders = new List<Order>();
            var sharesProcessedTotal = 0;
            IList<Order> matches;
            bool buyMode = order.Type == Order.OrderType.BUY;

            if (buyMode)
            {
                matches = counterParts.Where(x => x.ShareName.Equals(order.ShareName) && x.Limit <= stockPrice && order.Limit >= stockPrice).ToList();
            }
            else
            {
                matches = counterParts.Where(x => x.ShareName.Equals(order.ShareName) && x.Limit >= stockPrice && order.Limit <= stockPrice).ToList();
            }

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
            return new Tuple<Order, IEnumerable<Order>, IEnumerable<Transaction>>(order, usedOrders, transactions);
        }
    }
}
