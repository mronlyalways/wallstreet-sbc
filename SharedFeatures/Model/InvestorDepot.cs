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
        public InvestorDepot()
        {
            Shares = new Dictionary<string, int>();
        }

        public string Email { get; set; }

        public double Budget { get; set; }

        public Dictionary<string, int> Shares { get; set; }

        public void AddShares(string shareName, int coming)
        {
            int current = 0;
            Shares.TryGetValue(shareName, out current);
            Shares[shareName] = current += coming;
        }

        public void RemoveShares(string shareName, int going)
        {
            int current = 0;
            Shares.TryGetValue(shareName, out current);
            Shares[shareName] = current -= going;
        }

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
