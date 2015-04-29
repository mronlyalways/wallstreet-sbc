using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    [Serializable]
    public class InvestorDepot
    {
        public double Budget { get; set; }

        public Dictionary<string, int> Shares { get; set; }
    }
}
