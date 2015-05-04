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
        public string TransactionId { get; set; }

        public long BrokerId { get; set; }

        public string ShareName { get; set; }

        public string BuyingOrderId { get; set; }

        public string SellingOrderId { get; set; }

        public string BuyerId { get; set; }

        public string SellerId { get; set; }

        public double PricePerShare { get; set; }

        public int NoOfSharesSold { get; set; }

        public double TotalCost
        {
            get
            {
                return PricePerShare * NoOfSharesSold;
            }
        }

        public double Provision
        {
            get
            {
                return TotalCost * 0.03;
            }
        }
    }
}
