using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    [Serializable]
    public class FundDepot
    {
        public FundDepot()
        {
            Shares = new Dictionary<string, int>();
        }

        public string FundID
        {
            get;
            set;
        }

        public int FundShares
        {
            get;
            set;
        }

        public double FundBank
        {
            get;
            set;
        }

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
            String output = "Fund:" + FundID + " Fund assets: " + this.FundBank + "; Shares: ";
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
