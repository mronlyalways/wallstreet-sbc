using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XcoSpaces;
using XcoSpaces.Collections;
using System.Reflection;

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

        public static T FindElement<T>(XcoList<T> list, String key, String propertyName)
        {
            T result = default(T);
            for (int i = 0; i < list.Count; i++)
            {
                    T d = list[i, true];
                    var property = d.GetType().GetProperty(propertyName).GetValue(d);

                    if (property.Equals(key))
                    {
                        result = d;
                        break;
                    }
            }

            return result;
        }

        public static void ReplaceElement<T>(XcoList<T> list, T element, String propertyName)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T e = list[i, true];
                var property1 = e.GetType().GetProperty(propertyName).GetValue(e);
                var property2 = element.GetType().GetProperty(propertyName).GetValue(element);
                if (property1.Equals(property2))
                {
                    list[i] = element;
                }
            }
        }

    }
}
