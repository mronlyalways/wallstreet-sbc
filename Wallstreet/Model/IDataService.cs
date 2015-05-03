using SharedFeatures.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wallstreet.Model
{
    public interface IDataService
    {
        IEnumerable<ShareInformation> LoadMarketInformation();

        IEnumerable<Order> LoadOrders();

        void AddNewMarketInformationAvailableCallback(Action<ShareInformation> callback);

        void AddNewOrderAddedCallback(Action<Order> callback);

        void AddOrderRemovedCallback(Action<Order> callback);
    }
}
