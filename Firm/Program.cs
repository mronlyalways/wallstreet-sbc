using SharedFeatures.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;

namespace Firm
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
                    q.Enqueue(new Request() { FirmName = "GOOG", PricePerShare = 556.0, Shares = 5000 });
                }
                catch(XcoException)
                {
                    Console.WriteLine("Unable to reach server.");
                }
            }
        }
    }
}
