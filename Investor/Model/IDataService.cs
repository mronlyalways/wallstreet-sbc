using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedFeatures;
using SharedFeatures.Model;

namespace Investor.Model
{
    public interface IDataService
    {
        void login(Registration r);
    }
}
