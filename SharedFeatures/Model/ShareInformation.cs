using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    public class ShareInformation
    {
        public string FirmName;

        public int NoOfShares;
        
        public double PricePerShare;

        public override string ToString()
        {
            return FirmName + ", " + NoOfShares + ", " + PricePerShare;
        }
    }
}
