using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    [Serializable]
    public class Transaction
    {
        public long Id { get; set; }

        public long TransactionId { get; set; }

        public long BrokerId { get; set; }

        public long SellingOrderId { get; set; }

        public long PurchaseOrderId { get; set; }

        public InvestorDepot Seller { get; set; }

        public InvestorDepot Buyer { get; set; }

        public double MarketValue { get; set; }

        public int NoOfSharesSold { get; set; }

        public double TotalCost { get; set; }

        public double Provision { get; set; }
    }
}
