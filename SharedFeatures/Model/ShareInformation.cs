using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    public class ShareInformation
    {
        public string FirmName { get; set; }

        public int NoOfShares { get; set; }

        public double PricePerShare { get; set; }

        public override string ToString()
        {
            return FirmName + ", " + NoOfShares + ", " + PricePerShare;
        }
    }
}
