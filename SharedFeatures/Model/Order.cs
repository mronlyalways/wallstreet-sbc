using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    [Serializable]
    public class Order
    {
        public enum OrderStatus { OPEN, PARTIAL, DONE, DELETED };

        public enum OrderType { BUY, SELL }

        public string Id { get; set; }

        public string InvestorId { get; set; }

        public OrderType Type { get; set; }

        public string ShareName { get; set; }

        public double Limit { get; set; }

        public int TotalNoOfShares { get; set; }

        public int NoOfSharesProcessed { get; set; }

        public OrderStatus Status;
    }
}
