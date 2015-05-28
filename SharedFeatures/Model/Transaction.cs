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

        public bool PrioritizedSellingOrder { get; set; }

        public bool PrioritizedBuyingOrder { get; set; }

        public bool IsFund { get; set; }

        public double TotalCost
        {
            get
            {
                return PricePerShare * NoOfSharesSold;
            }
        }

        public double SellerProvision
        {
            get
            {
                int prioritizationMultiplier = 1;
                if (PrioritizedSellingOrder)
                {
                    prioritizationMultiplier = 2;
                }
                return TotalCost * 0.03 * prioritizationMultiplier;
            }
        }

        public double BuyerProvision
        {
            get
            {
                int prioritizationMultiplier = 1;
                if (PrioritizedBuyingOrder)
                {
                    prioritizationMultiplier = 2;
                }
                return TotalCost * 0.03 * prioritizationMultiplier;
            }
        }

        public double FundProvision
        {
            get
            {
                if (IsFund)
                {
                    return TotalCost * 0.02;
                }
                else
                {
                    return 0.0;
                }
            }
        }
    }
}
