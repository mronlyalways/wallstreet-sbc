using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XcoSpaces;
using XcoSpaces.Collections;

namespace SharedFeatures.Model
{
    public class Utils
    {
        public static double FindPricePerShare(XcoList<ShareInformation> list, string firmName)
        {
            double result = 0;
            for (int i = 0; i < list.Count; i++)
            {
                ShareInformation s = list[i,true];
                if (s.FirmName == firmName)
                {
                    result = s.PricePerShare;
                    break;
                }
            }

            return result;
        }

        public static ShareInformation FindShare(XcoList<ShareInformation> list, string firmName)
        {
            ShareInformation result = null;
            for (int i = 0; i < list.Count; i++)
            {
                ShareInformation s = list[i,true];
                if (s.FirmName == firmName)
                {
                    result = s;
                    break;
                }
            }
            return result;
        }

        public static void ReplaceShare(XcoList<ShareInformation> list, ShareInformation share)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ShareInformation s = list[i,true];
                if (s.FirmName == share.FirmName)
                {
                    list[i] = share;
                    break;
                }
            }
        }

        public static FundDepot FindFundDepot(XcoList<FundDepot> list, String key)
        {
            FundDepot result = null;
            for (int i = 0; i < list.Count; i++)
            {
                FundDepot d = list[i, true];
                if (d.FundID == key)
                {
                    result = d;
                    break;
                }
            }

            return result;
        }

        public static void ReplaceFundDepot(XcoList<FundDepot> list, FundDepot depot)
        {
            for (int i = 0; i < list.Count; i++)
            {
                FundDepot d = list[i, true];
                if (d.FundID == depot.FundID)
                {
                    list[i] = depot;
                }
            }
        }

        public static InvestorDepot FindInvestorDepot(XcoList<InvestorDepot> list, String key)
        {
            InvestorDepot result = null;
            for (int i = 0; i < list.Count; i++)
            {
                InvestorDepot d = list[i, true];
                if (d.Email == key)
                {
                    result = d;
                    break;
                }
            }

            return result;
        }

        public static void ReplaceInvestorDepot(XcoList<InvestorDepot> list, InvestorDepot depot)
        {
            for (int i = 0; i < list.Count; i++)
            {
                InvestorDepot d = list[i, true];
                if (d.Email == depot.Email)
                {
                    list[i] = depot;
                }
            }
        }

    }
}
