using SharedFeatures.Model;
using System;
using System.Threading;
using System.Reactive.Linq;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;
using System.Runtime.InteropServices;

namespace Agent
{
    class Program
    {
        static private IObservable<long> timer;
        static int counter = 0;
        static private XcoSpace space;
        static private XcoList<ShareInformation> stockPrices;
        static private XcoQueue<string> stockPricesUpdates;
        static private XcoList<Order> orders;
        static private XcoList<FundDepot> fundDepots;

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

            orders.Dispose();
            stockPricesUpdates.Dispose();
            stockPrices.Dispose();
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
                    space = new XcoSpace(0);
                    stockPrices = space.Get<XcoList<ShareInformation>>("StockInformation", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                    stockPricesUpdates = space.Get<XcoQueue<string>>("StockInformationUpdates", new Uri("xco://"+ Environment.MachineName + ":" + 9000));
                    orders = space.Get<XcoList<Order>>("Orders", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                    fundDepots = space.Get<XcoList<FundDepot>>("FundDepots", new Uri("xco://" + Environment.MachineName + ":" + 9000));

                    if (args.Length > 0 && args[0].Equals("-Manual"))
                    {
                        Console.WriteLine("Type \"list\" to list all shares and set the price by typing <sharename> <price>");
                        while (true)
                        {
                            var input = Console.ReadLine();
                            if (input.Equals("list"))
                            {
                                for (int i = 0; i < stockPrices.Count; i++)
                                {
                                    ShareInformation s = stockPrices[i];
                                    Console.WriteLine(s.FirmName + "\t" + s.PricePerShare);
                                }
                            }
                            else
                            {
                                var info = input.Split(' ');
                                var stock = Utils.FindShare(stockPrices, info[0]);
                                ShareInformation s = new ShareInformation()
                                {
                                    FirmName = info[0],
                                    NoOfShares = stock.NoOfShares,
                                    PricePerShare = Double.Parse(input.Split(' ')[1])
                                };
                                Utils.ReplaceShare(stockPrices, s);
                            }
                        }
                    }
                    else
                    {

                    timer = Observable.Interval(TimeSpan.FromSeconds(2));
                    timer.Subscribe(_ => UpdateStockPrices());
                    Thread.Sleep(1000);

                    while (true)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (XcoException)
            {
                Console.WriteLine("Unable to reach server.\nPress enter to exit.");
                Console.ReadLine();
                if (space != null && space.IsOpen) { space.Close(); }
            }
        }

        static void UpdateStockPrices()
        {
            using (XcoTransaction tx = space.BeginTransaction())
            {
                Console.WriteLine("UPDATE stock prices: " + DateTime.Now);

                try
                {
                    for (int i = 0; i < stockPrices.Count; i++)
                    {
                        ShareInformation oldPrice = stockPrices[i, true];

                        if (oldPrice.isFund)
                        {

                            FundDepot depot = Utils.FindFundDepot(fundDepots, oldPrice.FirmName);
                            ShareInformation newPrice = new ShareInformation()
                            {
                                FirmName = oldPrice.FirmName,
                                NoOfShares = oldPrice.NoOfShares,
                                PricePerShare = depot.FundBank / depot.FundShares,
                                isFund = true
                            };

                            if (!oldPrice.PricePerShare.Equals(newPrice.PricePerShare))
                            {
                                Console.WriteLine("Update {0} from {1} to {2}.", newPrice.FirmName, oldPrice.PricePerShare, newPrice.PricePerShare);
                                Utils.ReplaceShare(stockPrices, newPrice);
                                stockPricesUpdates.Enqueue(newPrice.FirmName, true);
                            }
                        }
                        else
                        {
                            long pendingBuyOrders = PendingOrders(oldPrice.FirmName, Order.OrderType.BUY);
                            long pendingSellOrders = PendingOrders(oldPrice.FirmName, Order.OrderType.SELL);
                            double x = ComputeNewPrice(oldPrice.PricePerShare, pendingBuyOrders, pendingSellOrders);
                            ShareInformation newPrice = new ShareInformation()
                            {
                                FirmName = oldPrice.FirmName,
                                NoOfShares = oldPrice.NoOfShares,
                                PricePerShare = x,
                                isFund = false
                            };
                            Console.WriteLine("Update {0} from {1} to {2}.", newPrice.FirmName, oldPrice.PricePerShare, newPrice.PricePerShare);

                            Utils.ReplaceShare(stockPrices, newPrice);
                            stockPricesUpdates.Enqueue(newPrice.FirmName, true);

                        }
                    }

                    RandomlyUpdateASingleStock();

                    tx.Commit();
                }
                catch (XcoException e)
                {
                    Console.WriteLine("Could not update stock due to: " + e.Message);
                    tx.Rollback();
                }
            }
        }

        private static void RandomlyUpdateASingleStock()
        {
            counter++;
            if (stockPrices.Count > 0 && counter % 3 == 0)
            {
                counter = 0;
                Random rrd = new Random();
                ShareInformation oldPrice = stockPrices[rrd.Next(0, stockPrices.Count - 1), true];

                int tries = 1;

                while (oldPrice.isFund && tries < stockPrices.Count)
                {
                    oldPrice = stockPrices[rrd.Next(0, stockPrices.Count - 1), true];
                }

                if (!oldPrice.isFund)
                {
                    Console.WriteLine("UPDATE stock price {0} randomly: {1}", oldPrice.FirmName, DateTime.Now);

                    double x = Math.Max(1, oldPrice.PricePerShare * (1 + (rrd.Next(-3, 3) / 100.0)));
                    ShareInformation newPrice = new ShareInformation()
                    {
                        FirmName = oldPrice.FirmName,
                        NoOfShares = oldPrice.NoOfShares,
                        PricePerShare = x
                    };
                    Console.WriteLine("Update {0} from {1} to {2}.", newPrice.FirmName, oldPrice.PricePerShare, newPrice.PricePerShare);

                    Utils.ReplaceShare(stockPrices, newPrice);
                    stockPricesUpdates.Enqueue(newPrice.FirmName);
                }
            }
        }

        static private long PendingOrders(string stockName, Order.OrderType orderType)
        {
                long stocks = 0;

                for (int i = 0; i < orders.Count; i++)
                {
                    Order order = orders[i];
                    if (order.ShareName == stockName && (order.Status == Order.OrderStatus.OPEN || order.Status == Order.OrderStatus.PARTIAL) && order.Type == orderType)
                    {
                        stocks += order.NoOfOpenShares;
                    }
                }

                return stocks;
        }

        static double ComputeNewPrice(double oldPrice, long pendingBuyOrders, long pendingSellOrders)
        {
            double d = Math.Max(1, pendingBuyOrders + pendingSellOrders);
            double n = (double)(pendingBuyOrders - pendingSellOrders);
            double x = (1 + ((n / d) * (1.0 / 16.0)));

            return Math.Max(1, oldPrice * x);
        }
    }
}
