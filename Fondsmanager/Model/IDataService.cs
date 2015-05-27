using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedFeatures;
using SharedFeatures.Model;
using GalaSoft.MvvmLight;

namespace Fondsmanager.Model
{
    public interface IDataService : IDisposable
    {
        void Login(FundRegistration r);

        void PlaceOrder(Order order);

        void CancelOrder(Order order);

        FundDepot LoadFundInformation();

        IEnumerable<ShareInformation> LoadMarketInformation();

        IEnumerable<Order> LoadPendingOrders();

        void AddNewMarketInformationAvailableCallback(Action<ShareInformation> callback);

        void AddNewPendingOrdersCallback(Action<IEnumerable<Order>> callback);

        void AddNewInvestorInformationAvailableCallback(Action<FundDepot> callback);

        void RemoveNewInvestorInformationAvailableCallback(Action<FundDepot> callback);
    }
}
