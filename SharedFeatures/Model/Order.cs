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

        public int NoOfProcessedShares { get; set; }

        public int NoOfOpenShares
        {
            get
            {
                return TotalNoOfShares - NoOfProcessedShares;
            }
        }

        public OrderStatus Status { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as Order;
            if (other != null)
            {
                var result = Id.Equals(other.Id) 
                    && InvestorId.Equals(other.InvestorId) 
                    && Type == other.Type 
                    && ShareName.Equals(other.ShareName) 
                    && Limit == other.Limit 
                    && TotalNoOfShares == other.TotalNoOfShares 
                    && Status == other.Status;
                return result;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return base.ToString() + " Id:" + Id + " Status:" + Status + " InvestorId:" + InvestorId + " Type:" + Type + " ShareName:" + ShareName + " Limit:" +Limit + " TotalNoOfShares:" + TotalNoOfShares ;
        }

        public override int GetHashCode()
        {
            return TotalNoOfShares * NoOfProcessedShares;
        }
    }
}
