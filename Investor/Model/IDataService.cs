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
        InvestorDepot Depot { get; }
        
        void Login(Registration r);

        void Logout();

        void PlaceOrder(Order order);

        IEnumerable<ShareInformation> LoadMarketInformation();

        void AddNewMarketInformationAvailableCallback(Action<ShareInformation> callback);

        void AddNewInvestorInformationAvailableCallback(Action<InvestorDepot> callback);

        void RemoveNewInvestorInformationAvailableCallback(Action<InvestorDepot> callback);

        void AddNewOrderAvailableCallback(Action<Order> callback);

        void AddOrderRemovedCallback(Action<Order> callback);
    }
}
