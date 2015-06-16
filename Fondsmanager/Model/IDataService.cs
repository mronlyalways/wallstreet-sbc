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
        void SetSpace(string space);

        void Login(FundRegistration r);

        void PlaceOrder(Order order);

        void CancelOrder(Order order);

        IEnumerable<string> ListOfSpaces();

        FundDepot LoadFundInformation();

        IEnumerable<ShareInformation> LoadMarketInformation();

        IEnumerable<Order> LoadPendingOrders();

        void AddNewMarketInformationAvailableCallback(Action callback);

        void AddNewPendingOrdersCallback(Action callback);

        void AddNewInvestorInformationAvailableCallback(Action callback);

        void RemoveNewInvestorInformationAvailableCallback(Action callback);
    }
}
