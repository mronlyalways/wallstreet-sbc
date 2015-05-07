using NUnit.Framework;
using SharedFeatures.Model;
using System;
using System.Linq;
using System.Collections.Generic;

namespace BrokerTest
{
    [TestFixture]
    public class OrderMatchingTest
    {
        /*
        [Test]
        public void TestExactMatch_ShouldCompleteWithOneTransaction()
        {
            var buy = new Order() { ShareName = "GOOG", TotalNoOfShares = 10, NoOfProcessedShares = 0, Limit = 210 };
            var sell = new Order() { ShareName = "GOOG", TotalNoOfShares = 10, NoOfProcessedShares = 0, Limit = 200 };
            var stockPrice = 205;
            var result = Broker.Program.MatchOrders(buy, new List<Order> { sell }, stockPrice);
            var b = result.Item1;
            var s = result.Item2;
            var t = result.Item3;
            Assert.AreEqual(0, b.NoOfOpenShares);
            Assert.AreEqual(1, s.Count());
            Assert.AreEqual(0, s.First().NoOfOpenShares);
            Assert.AreEqual(Order.OrderStatus.DONE, b.Status);
            Assert.AreEqual(Order.OrderStatus.DONE, s.First().Status);
            Assert.AreEqual(1, t.Count());
            var transaction = t.First();

            Assert.AreEqual(b.TotalNoOfShares, transaction.NoOfSharesSold);
            Assert.AreEqual(205*10, transaction.TotalCost);
            Assert.AreEqual(205 * 10 * 0.03, transaction.Provision);
        }

        [Test]
        public void TestDifferentStock_ShouldNotProcessOrders()
        {
            var buy = new Order() { ShareName = "AAPL", TotalNoOfShares = 5, NoOfProcessedShares = 0, Limit = 300 };
            var sell = new Order() { ShareName = "GOOG", TotalNoOfShares = 5, NoOfProcessedShares = 0, Limit = 100 };
            var stockPrice = 200;
            var result = Broker.Program.MatchOrders(buy, new List<Order> { sell }, stockPrice);
            var b = result.Item1;
            var s = result.Item2;
            var t = result.Item3;
            Assert.AreEqual(0, b.NoOfProcessedShares);
            Assert.AreEqual(0, s.Count());
            Assert.AreEqual(0, t.Count());
        }

        [Test]
        public void TestSellingMoreThanBuying_ShouldCompleteAndUpdateSellingOrderToPartial()
        {
            var buy = new Order() { ShareName = "MSFT", TotalNoOfShares = 5, NoOfProcessedShares = 0, Limit = 200 };
            var sell = new Order() { ShareName = "MSFT", TotalNoOfShares = 15, NoOfProcessedShares = 0, Limit = 150 };
            var stockPrice = 180;
            var result = Broker.Program.MatchOrders(buy, new List<Order> { sell }, stockPrice);
            var b = result.Item1;
            var s = result.Item2;
            var t = result.Item3;
            Assert.AreEqual(0, b.NoOfOpenShares);
            Assert.AreEqual(1, s.Count());
            Assert.AreEqual(10, s.First().NoOfOpenShares);
            Assert.AreEqual(Order.OrderStatus.DONE, b.Status);
            Assert.AreEqual(Order.OrderStatus.PARTIAL, s.First().Status);
            Assert.AreEqual(1, t.Count());
            var transaction = t.First();

            Assert.AreEqual(b.TotalNoOfShares, transaction.NoOfSharesSold);
            Assert.AreEqual(5 * 180, transaction.TotalCost);
        }

        [Test]
        public void TestBuyingMoreThanSelling_ShouldCompleteAndUpdateBuyingOrderToPartial()
        {
            var buy = new Order() { ShareName = "IBM", TotalNoOfShares = 12, NoOfProcessedShares = 0, Limit = 120 };
            var sell = new Order() { ShareName = "IBM", TotalNoOfShares = 4, NoOfProcessedShares = 0, Limit = 80 };
            var stockPrice = 100;
            var result = Broker.Program.MatchOrders(buy, new List<Order> { sell }, stockPrice);
            var b = result.Item1;
            var s = result.Item2;
            var t = result.Item3;
            Assert.AreEqual(8, b.NoOfOpenShares);
            Assert.AreEqual(1, s.Count());
            Assert.AreEqual(0, s.First().NoOfOpenShares);
            Assert.AreEqual(Order.OrderStatus.PARTIAL, b.Status);
            Assert.AreEqual(Order.OrderStatus.DONE, s.First().Status);
            Assert.AreEqual(1, t.Count());
            var transaction = t.First();

            Assert.AreEqual(s.First().TotalNoOfShares, transaction.NoOfSharesSold);
            Assert.AreEqual(4 * 100, transaction.TotalCost);
        }

        [Test]
        public void TestLimitExceeded_ShouldNotProcessOrders()
        {
            var buy = new Order() { ShareName = "GOOG", TotalNoOfShares = 10, NoOfProcessedShares = 0, Limit = 210 };
            var sell = new Order() { ShareName = "GOOG", TotalNoOfShares = 10, NoOfProcessedShares = 0, Limit = 200 };
            var stockPrice = 240;
            var result = Broker.Program.MatchOrders(buy, new List<Order> { sell }, stockPrice);
            var b = result.Item1;
            var s = result.Item2;
            var t = result.Item3;
            Assert.AreEqual(0, b.NoOfProcessedShares);
            Assert.AreEqual(0, s.Count());
            Assert.AreEqual(0, t.Count());
        }

        [Test]
        public void TestMultipleSellsForOneBuy_ShouldProcessOrders()
        {
            var buy = new Order() { ShareName = "GOOG", TotalNoOfShares = 10, NoOfProcessedShares = 0, Limit = 210 };
            var sell1 = new Order() { ShareName = "GOOG", TotalNoOfShares = 5, NoOfProcessedShares = 0, Limit = 200 };
            var sell2 = new Order() { ShareName = "GOOG", TotalNoOfShares = 5, NoOfProcessedShares = 0, Limit = 200 };
            var stockPrice = 205;
            var result = Broker.Program.MatchOrders(buy, new List<Order> { sell1, sell2 }, stockPrice);
            var b = result.Item1;
            var s = result.Item2;
            var t = result.Item3;
            Assert.AreEqual(10, b.NoOfProcessedShares);
            Assert.AreEqual(2, s.Count());
            Assert.AreEqual(2, t.Count());
        }
         */
    }
        
}
