using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedFeatures;
using SharedFeatures.Model;
using GalaSoft.MvvmLight;

namespace Investor.Model
{
    public interface IDataService
    {
        void Login(Registration r);

        void OnRegistrationConfirmed(Action callback);

        InvestorDepot Depot { get; }
    }
}
