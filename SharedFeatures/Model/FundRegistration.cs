using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    [Serializable]
    public class FundRegistration
    {
        public String FundID
        {
            get;
            set;
        }

        public double FundAssets
        {
            get;
            set;
        }

        public long FundShares
        {
            get;
            set;
        }
    }
}
