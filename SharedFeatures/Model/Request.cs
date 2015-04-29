using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    [Serializable]
    public class Request
    {
        public String FirmName { get; set; }

        public int Shares { get; set; }

        public double PricePerShare { get; set; }
    }
}
