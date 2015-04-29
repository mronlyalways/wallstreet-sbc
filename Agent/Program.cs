using SharedFeatures.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;

namespace Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            using (XcoSpace space = new XcoSpace(0))
            {
                try
                {
                    XcoDictionary<string, double> stockPrices = space.Get<XcoDictionary<string, double>>("StockPrices", new Uri("xco://" + Environment.MachineName + ":" + 9000));
                    Random random = new Random();
                    while (true)
                    {
                        Thread.Sleep(3000);
                        stockPrices["GOOG"] = 563.30 + random.Next(100);
                        Thread.Sleep(3000);
                        stockPrices["AMZN"] = 262.51 + random.Next(50);
                        Thread.Sleep(3000);
                        stockPrices["Alete"] = 12.00 + random.Next(10);
                    }
                }
                catch (XcoException)
                {
                    Console.WriteLine("Unable to reach server.\nPress enter to exit.");
                    Console.ReadLine();
                }
            }
        }

        static double ComputeNewPrice(double oldPrice)
        {

            return 0;
        }
    }
}
