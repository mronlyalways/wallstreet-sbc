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
                ShareInformation s = list[i];
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
                ShareInformation s = list[i];
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
                ShareInformation s = list[i];
                if (s.FirmName == share.FirmName)
                {
                    list[i] = share;
                    break;
                }
            }
        }
    }
}
