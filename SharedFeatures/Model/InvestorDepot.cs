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
        public string Email { get; set; }

        public double Budget { get; set; }

        public Dictionary<string, int> Shares { get; set; }

        public override string ToString()
        {
            String output = "Budget: " + this.Budget + "; Shares: ";
            if (this.Shares != null)
            {
                foreach (string key in this.Shares.Keys)
                {
                    output += key + ": " + this.Shares[key] + " - ";
                }
            }

            return output;
        }
    }
}
