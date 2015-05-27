using SharedFeatures.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;
using XcoSpaces.Kernel;
using XcoSpaces.Kernel.Selectors;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Broker
{
    public class Program
    {
        private static int brokerId;
        private static readonly Uri spaceServer = new Uri("xco://" + Environment.MachineName + ":" + 9000);
        private static readonly string kernelServer = Environment.MachineName + ":" + 9001;
        private static XcoSpace space;
        private static XcoQueue<Request> requestsQ;
        private static XcoList<Order> orders;
        private static XcoQueue<Order> orderQueue;
        private static XcoDictionary<string, InvestorDepot> investorDepots;
        private static XcoDictionary<string, FirmDepot> firmDepots;
        private static XcoQueue<FundDepot> fundDepotQueue;
        private static XcoDictionary<string, FundDepot> fundDepots;
        private static XcoList<Transaction> transactions;
        private static XcoList<ShareInformation> stockInformation;
        private static XcoQueue<string> stockInformationUpdates;

        static bool exitSystem = false;

        #region Trap application termination
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            Console.WriteLine("Exiting system due to external CTRL-C, or process kill, or shutdown");

            stockInformationUpdates.Dispose();
            stockInformation.Dispose();
            transactions.Dispose();
            firmDepots.Dispose();
            fundDepotQueue.Dispose();
            fundDepots.Dispose();
            investorDepots.Dispose();
            orderQueue.Dispose();
            orders.Dispose();
            requestsQ.Dispose();
            space.Dispose();

            Console.WriteLine("Cleanup complete");

            //allow main to run off
            exitSystem = true;

            //shutdown right away so there are no lingering threads
            Environment.Exit(-1);

            return true;
        }
        #endregion


        static void Main(string[] args)
        {

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            try
            {
                QueryBrokerId();
                space = new XcoSpace(0);
                transactions = space.Get<XcoList<Transaction>>("Transactions", spaceServer);
                orders = space.Get<XcoList<Order>>("Orders", spaceServer);
                orderQueue = space.Get<XcoQueue<Order>>("OrderQueue", spaceServer);
                investorDepots = space.Get<XcoDictionary<string, InvestorDepot>>("InvestorDepots", spaceServer);
                firmDepots = space.Get<XcoDictionary<string, FirmDepot>>("FirmDepots", spaceServer);
                fundDepots = space.Get<XcoDictionary<string, FundDepot>>("FundDepots", spaceServer);
                fundDepotQueue = space.Get<XcoQueue<FundDepot>>("FundDepotQueue", spaceServer);
                fundDepotQueue.AddNotificationForEntryEnqueued(OnFundDepotAddedToQueue);
                stockInformation = space.Get<XcoList<ShareInformation>>("StockInformation", spaceServer);
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

        private static void QueryBrokerId()
        {
            Console.WriteLine("Querying unique broker identification number (this may take a very long time)");
            using(XcoKernel kernel = new XcoKernel()) {
                kernel.Start(0);

                ContainerReference cref = kernel.GetNamedContainer(kernelServer, "BrokerIdContainer");

                List<IEntry> entries = kernel.Take(cref, null, System.Threading.Timeout.Infinite, new LindaSelector(1, null));

                if (entries.Count > 0)
                {
                    XcoSpaces.Kernel.Selectors.Tuple t = (XcoSpaces.Kernel.Selectors.Tuple) entries[0].Value;
                    TupleValue v = (TupleValue) t.Values[0];
                    brokerId = (int) v.Value;

                    kernel.Write(cref, null, System.Threading.Timeout.Infinite, new Entry(new XcoSpaces.Kernel.Selectors.Tuple(new TupleValue<int>(brokerId + 1))));

                    Console.WriteLine("Broker has id: " + brokerId);
                }
                else
                {
                    Environment.Exit(-1);
                }

                kernel.Stop();
                kernel.Dispose();
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
                                    var info = Utils.FindShare(stockInformation, request.FirmName);
                                    ShareInformation update = new ShareInformation()
                                    {
                                        FirmName = request.FirmName,
                                        NoOfShares = info.NoOfShares,
                                        PricePerShare = info.PricePerShare
                                    };
                                    Utils.ReplaceShare(stockInformation, update);
                                    Console.WriteLine("Add {0} shares to existing account \"{1}\"", request.Shares, request.FirmName);
                                }
                                else
                                {
                                    firmDepots.Add(request.FirmName, new FirmDepot() { FirmName = request.FirmName, OwnedShares = request.Shares });
                                    ShareInformation s = new ShareInformation()
                                    {
                                        FirmName = request.FirmName,
                                        NoOfShares = request.Shares,
                                        PricePerShare = request.PricePerShare
                                    };
                                    stockInformation.Add(s);
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

        public static void OnFundDepotAddedToQueue(XcoQueue<FundDepot> source, FundDepot depot)
        {

            using (XcoTransaction tx = space.BeginTransaction())
            {
                try
                {
                    FundDepot d = source.Dequeue(false);

                    if (d != null)
                    {
                            ShareInformation s = new ShareInformation()
                            {
                                FirmName = depot.FundID,
                                NoOfShares = depot.FundShares,
                                PricePerShare = depot.FundAssets / depot.FundShares,
                                isFund = true
                            };
                            stockInformation.Add(s);
                        
                        var orderId = depot.FundID + DateTime.Now.Ticks.ToString();
                        Order o = new Order()
                        {
                            Id = orderId,
                            InvestorId = depot.FundID,
                            ShareName = depot.FundID,
                            Type = Order.OrderType.SELL,
                            Limit = 0,
                            NoOfProcessedShares = 0,
                            TotalNoOfShares = depot.FundShares
                        };
                        orderQueue.Enqueue(o);

                    }

                    tx.Commit();
                }
                catch (XcoException e)
                {
                    Console.WriteLine(e.Message);
                    tx.Rollback();
                }
            }
        }

        private static void OnShareInformationAddedToQueue(XcoQueue<string> queue, string shareKey) {

            string s = null;

            try
            {
                s = queue.Dequeue(true);
            }
            catch (XcoException e)
            {
                Console.WriteLine(e.Message);
            }

            if (s != null)
            {
                using (XcoTransaction tx = space.BeginTransaction())
                {
                    try
                    {
                        Double stockPrice = Utils.FindPricePerShare(stockInformation, s);
                        Console.WriteLine("Received new stock price for {0}: {1:C}", s, stockPrice);
                        for (int i = 0; i < orders.Count; i++)
                        {
                            Order candidate = orders[i,true];
                            if (candidate.ShareName == s && !candidate.Status.Equals(Order.OrderStatus.DONE) && !candidate.Status.Equals(Order.OrderStatus.DELETED))
                            {
                                if ((candidate.Type.Equals(Order.OrderType.BUY) && candidate.Limit >= stockPrice) ||
                                    (candidate.Type.Equals(Order.OrderType.SELL) && candidate.Limit <= stockPrice))
                                {
                                    Console.WriteLine("New stock price: Order is eligible for processing: {0}", candidate);
                                    if (CanProcessOrder(candidate))
                                    {
                                        ProcessOrder(candidate, i);
                                    }
                                }
                            }
                        }

                        tx.Commit();
                    }
                    catch (XcoException e)
                    {
                        Console.WriteLine(e.Message);
                        tx.Rollback();
                    }
                }
            }

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

                if (o.ShareName == candidate.ShareName && candidate.Prioritize && (candidate.Status != Order.OrderStatus.DONE && candidate.Status != Order.OrderStatus.DELETED))
                {
                    if ((o.Type.Equals(Order.OrderType.BUY) && candidate.Type.Equals(Order.OrderType.SELL) && o.Limit >= Utils.FindPricePerShare(stockInformation, o.ShareName) && candidate.Limit <= Utils.FindPricePerShare(stockInformation, o.ShareName)) ||
                    (o.Type.Equals(Order.OrderType.SELL) && candidate.Type.Equals(Order.OrderType.BUY) && o.Limit <= Utils.FindPricePerShare(stockInformation, o.ShareName) && candidate.Limit >= Utils.FindPricePerShare(stockInformation, o.ShareName)))
                    {
                        match = candidate;
                        index = i;
                        break;
                    }
                }
            }

            if (match == null)
            {
                for (int i = 0; i < orders.Count; i++)
                {
                    Order candidate = orders[i];

                    if (o.ShareName == candidate.ShareName && (candidate.Status != Order.OrderStatus.DONE && candidate.Status != Order.OrderStatus.DELETED))
                    {
                        if ((o.Type.Equals(Order.OrderType.BUY) && candidate.Type.Equals(Order.OrderType.SELL) && o.Limit >= Utils.FindPricePerShare(stockInformation, o.ShareName) && candidate.Limit <= Utils.FindPricePerShare(stockInformation, o.ShareName)) ||
                        (o.Type.Equals(Order.OrderType.SELL) && candidate.Type.Equals(Order.OrderType.BUY) && o.Limit <= Utils.FindPricePerShare(stockInformation, o.ShareName) && candidate.Limit >= Utils.FindPricePerShare(stockInformation, o.ShareName)))
                        {
                            match = candidate;
                            index = i;
                            break;
                        }
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
                BrokerId = brokerId,
                ShareName = o1.ShareName,
                BuyerId = purchase.InvestorId,
                SellerId = sell.InvestorId,
                BuyingOrderId = purchase.Id,
                SellingOrderId = sell.Id,
                NoOfSharesSold = noOfSharesSold,
                PricePerShare = Utils.FindPricePerShare(stockInformation, o1.ShareName),
                PrioritizedBuyingOrder = purchase.Prioritize,
                PrioritizedSellingOrder = sell.Prioritize
            };

            return t;
        }

        private static bool IsAffordable(Transaction t)
        {
            return (t.TotalCost + t.BuyerProvision) <= investorDepots[t.BuyerId].Budget; 
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
            Order purchase = null;
            Order sell = null;
            int purchaseIndex = -1;
            int sellIndex = -1;

            if (o1.Type.Equals(Order.OrderType.BUY))
            {
                purchase = o1;
                sell = o2;
                purchaseIndex = index1;
                sellIndex = index2;
            }
            else
            {
                purchase = o2;
                sell = o1;
                purchaseIndex = index2;
                sellIndex = index1;
            }

            if (!IsAffordable(t))
            {
                
                purchase.Status = Order.OrderStatus.DELETED;
                
            }
            
            if (!HasEnoughShares(t))
            {
                sell.Status = Order.OrderStatus.DELETED;
            }

            if (purchaseIndex >= 0)
            {
                orders.RemoveAt(purchaseIndex);
                orders.Insert(purchaseIndex, purchase);
            }
            else
            {
                orders.Add(purchase);
            }

            if (sellIndex >= 0)
            {
                orders.RemoveAt(sellIndex);
                orders.Insert(sellIndex, sell);
            }
            else
            {
                orders.Add(sell);
            }
        }

        private static void ProcessOrder(Order o, int index)
        {
            int matchIndex = -1;
            Order match = FindMatchingOrderFor(o, ref matchIndex);

            if (match != null)
            {

                Transaction t = calculateTransaction(o, match);
                XcoTransaction tx = space.CurrentTransaction;

                if (HasEnoughShares(t) && IsAffordable(t))
                {
                    PerformTransaction(o, index, matchIndex, match, t);

                    if (index >= 0)
                    {
                        transactions.Add(t);
                        orders[index] = o;
                        orders[matchIndex] = match;
                    }
                    else if (tx != null) 
                    { 
                        tx.Commit();

                        transactions.Add(t);
                        orders[matchIndex] = match;
                        orderQueue.Enqueue(o);
                    }
                                       
                }
                else
                {
                    Punish(o, index, match, matchIndex, t);

                    tx.Commit();
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
            buyer.Budget -= (t.TotalCost + t.BuyerProvision);
            buyer.AddShares(t.ShareName, t.NoOfSharesSold);
            investorDepots[t.BuyerId] = buyer;

            if (investorDepots.ContainsKey(t.SellerId))
            {
                var seller = investorDepots[t.SellerId];
                seller.RemoveShares(t.ShareName, t.NoOfSharesSold);
                seller.Budget += (t.TotalCost - t.SellerProvision);
                investorDepots[t.SellerId] = seller;
            }
            else if (firmDepots.ContainsKey(t.SellerId))
            {
                var seller = firmDepots[t.SellerId];
                seller.OwnedShares -= t.NoOfSharesSold;
                firmDepots[t.SellerId] = seller;
            }
        }
    }
}
