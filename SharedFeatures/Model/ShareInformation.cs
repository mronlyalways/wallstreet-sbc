using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    [Serializable]
    public class ShareInformation
    {
        public string FirmName { get; set; }

        public int NoOfShares { get; set; }

        public int PurchasingVolume { get; set; }

        public int SalesVolume { get; set; }

        public double PricePerShare { get; set; }

        public bool isFund { get; set; }
    }
}
