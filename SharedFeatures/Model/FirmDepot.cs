using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    [Serializable]
    public class FirmDepot
    {
        public string FirmName { get; set; }

        public int OwnedShares { get; set; }
    }
}
