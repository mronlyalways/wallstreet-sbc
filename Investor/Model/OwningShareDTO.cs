using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Investor.Model
{
    public class OwningShareDTO
    {
        public string ShareName
        {
            get;
            set;
        }

        public int Amount
        {
            get;
            set;
        }

        public double StockPrice
        {
            get;
            set;
        }

        public Double Value
        {
            get
            {
                return StockPrice * Amount;
            }
        }
    }
}
