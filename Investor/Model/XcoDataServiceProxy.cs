using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XcoSpaces;
using XcoSpaces.Collections;
using XcoSpaces.Exceptions;
using SharedFeatures;
using SharedFeatures.Model;
using GalaSoft.MvvmLight;

namespace Investor.Model
{
    class XcoDataServiceProxy : IDataService
    {
        private Dictionary<string, XcoDataService> dataServices;
        private string currentSpace;

        public XcoDataServiceProxy(IList<Uri> spaceServers)
        {

            dataServices = new Dictionary<string, XcoDataService>();

            foreach (Uri spaceUri in spaceServers)
            {
                XcoDataService xcoService = new XcoDataService(spaceUri);
                dataServices.Add(spaceUri.ToString(), xcoService);
            }

            if (spaceServers.Count() > 0)
            {
                currentSpace = spaceServers.First().ToString();
            }

        }

        public void Login(Registration r)
        {

            foreach (XcoDataService service in dataServices.Values)
            {
                service.Login(r);
            }
        }

        public void PlaceOrder(Order order)
        {
            if (currentSpace != null)
            {
                dataServices[currentSpace].PlaceOrder(order);
            }
        }

        public void CancelOrder(Order order)
        {
            if (currentSpace != null)
            {
                dataServices[currentSpace].CancelOrder(order);
            }
        }

        public void SetSpace(string space)
        {
            currentSpace = space;
        }

        public IEnumerable<string> ListOfSpaces()
        {
            return dataServices.Keys;
        }

        public InvestorDepot LoadInvestorInformation()
        {
            if (currentSpace != null)
            {
                return dataServices[currentSpace].LoadInvestorInformation();
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<ShareInformation> LoadMarketInformation()
        {
            if (currentSpace != null)
            {
                return dataServices[currentSpace].LoadMarketInformation();
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<Order> LoadPendingOrders()
        {
            if (currentSpace != null)
            {
                return dataServices[currentSpace].LoadPendingOrders();
            }
            else
            {
                return null;
            }
        }

        public void AddNewMarketInformationAvailableCallback(Action callback)
        {
            foreach (XcoDataService service in dataServices.Values)
            {
                service.AddNewMarketInformationAvailableCallback(callback);
            }
        }

        public void AddNewInvestorInformationAvailableCallback(Action callback)
        {
            foreach (XcoDataService service in dataServices.Values)
            {
                service.AddNewInvestorInformationAvailableCallback(callback);
            }
        }

        public void AddNewPendingOrdersCallback(Action callback)
        {
            foreach (XcoDataService service in dataServices.Values)
            {
                service.AddNewPendingOrdersCallback(callback);
            }
        }

        public void RemoveNewInvestorInformationAvailableCallback(Action callback)
        {
            foreach (XcoDataService service in dataServices.Values)
            {
                service.RemoveNewInvestorInformationAvailableCallback(callback);
            }
        }

        public void Dispose()
        {
            foreach (XcoDataService service in dataServices.Values)
            {
                service.Dispose();
            }
        }
    }
}
