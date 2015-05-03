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
    public interface IDataService : IDisposable
    {
        void Login(Registration r);

        void AddRegistrationConfirmedCallback(Action callback);

        IEnumerable<ShareInformation> LoadMarketInformation();

        void AddNewMarketInformationAvailableCallback(Action<ShareInformation> callback);

        void PlaceOrder(Order order);

        InvestorDepot Depot { get; }
    }
}
