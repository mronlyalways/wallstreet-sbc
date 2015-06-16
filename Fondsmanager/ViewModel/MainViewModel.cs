using GalaSoft.MvvmLight;
using SharedFeatures.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Fondsmanager.Model;
using System;
using GalaSoft.MvvmLight.Command;

namespace Fondsmanager.ViewModel
{

    public class MainViewModel : ViewModelBase
    {
        private IDataService data;
                
        public MainViewModel(IDataService data)
        {
            this.data = data;
            OwnedShares = new ObservableCollection<OwningShareDTO>();
            UpdateOwnedShares();
            data.AddNewInvestorInformationAvailableCallback(UpdateFundInformation);
            data.AddNewMarketInformationAvailableCallback(UpdateShareInformation);
            data.AddNewPendingOrdersCallback(() => RaisePropertyChanged(() => PendingOrders));
            PlaceBuyingOrderCommand = new RelayCommand(PlaceBuyingOrder, () => SelectedBuyingShare != null);
            PlaceSellingOrderCommand = new RelayCommand(PlaceSellingOrder, () => SelectedSellingShare != null);
            CancelPendingOrderCommand = new RelayCommand(CancelPendingOrder, () => SelectedPendingOrder != null && SelectedPendingOrder.Status == Order.OrderStatus.OPEN);
            LogoutCommand = new RelayCommand(Logout, () => true);
            ListOfSpaces = new ObservableCollection<string>(data.ListOfSpaces());
            SelectedSpace = data.ListOfSpaces().First();
        }

        private void UpdateFundInformation()
        {
            RaisePropertyChanged(() => FundAssets);
            UpdateOwnedShares();
        }

        private void UpdateOwnedShares()
        {
            var collection = new ObservableCollection<OwningShareDTO>();

            foreach (String shareName in data.LoadFundInformation().Shares.Keys)
            {
                 var infos = MarketInformation.Where(x => x.FirmName == shareName).ToList();
                ShareInformation info = infos.First();
                if (info != null) {
                    OwningShareDTO s = new OwningShareDTO()
                    {
                        ShareName = shareName,
                        Amount = data.LoadFundInformation().Shares[shareName],
                        StockPrice = info.PricePerShare
                    };
                    collection.Add(s);
                }
            }

            OwnedShares = collection;

            RaisePropertyChanged(() => FundAssets);

        }

        private void UpdateShareInformation()
        {
            RaisePropertyChanged(() => MarketInformation);
            UpdateOwnedShares();
        }

        public string FundID { get { return data.LoadFundInformation().FundID; } }

        public long FundShares { get {
            return data.LoadFundInformation().FundShares; 
        } }

        public double FundAssets
        {
            get
            {
                double value = 0;
                foreach (OwningShareDTO s in OwnedShares)
                {
                    value += s.Value;
                }

                value += data.LoadFundInformation().FundBank;

                return value;
            }
        }

        private string selectedSpace;

        public string SelectedSpace
        {
            get
            {
                return selectedSpace;
            }
            set
            {
                selectedSpace = value;
                RaisePropertyChanged(() => SelectedSpace);
                data.SetSpace(selectedSpace);
                RaisePropertyChanged(() => PendingOrders);
                UpdateFundInformation();
                UpdateShareInformation();
            }
        }

        private ObservableCollection<string> listOfSpaces;

        public ObservableCollection<string> ListOfSpaces
        {
            get
            {
                return listOfSpaces;
            }
            set
            {
                listOfSpaces = value;
                RaisePropertyChanged(() => ListOfSpaces);
            }
        }

        public ObservableCollection<ShareInformation> MarketInformation
        {
            get
            {
                return new ObservableCollection<ShareInformation>(from i in data.LoadMarketInformation() orderby i.FirmName select i);
            }
        }

        private ObservableCollection<OwningShareDTO> ownedShares;
        public ObservableCollection<OwningShareDTO> OwnedShares
        {
            get
            {
                return ownedShares;
            }
            set
            {
                ownedShares = new ObservableCollection<OwningShareDTO>(from i in value orderby i.ShareName select i); ;
                RaisePropertyChanged(() => OwnedShares);
            }
        }

        public ObservableCollection<Order> PendingOrders
        {
            get
            {
                return new ObservableCollection<Order>(from i in data.LoadPendingOrders() orderby i.Id select i);
            }
        }

        private ShareInformation selectedBuyingShare;
        public ShareInformation SelectedBuyingShare
        {
            get
            {
                return selectedBuyingShare;
            }
            set
            {
                selectedBuyingShare = value;
                RaisePropertyChanged(() => SelectedBuyingShare);
                PlaceBuyingOrderCommand.RaiseCanExecuteChanged();
            }
        }

        private OwningShareDTO selectedSellingShare;
        public OwningShareDTO SelectedSellingShare
        {
            get
            {
                return selectedSellingShare;
            }
            set
            {
                selectedSellingShare = value;
                RaisePropertyChanged(() => SelectedSellingShare);
                PlaceSellingOrderCommand.RaiseCanExecuteChanged();
            }
        }

        private Order selectedPendingOrder;
        public Order SelectedPendingOrder
        {
            get
            {
                return selectedPendingOrder;
            }
            set
            {
                selectedPendingOrder = value;
                RaisePropertyChanged(() => SelectedPendingOrder);
                CancelPendingOrderCommand.RaiseCanExecuteChanged();
            }
        }

        private int noOfSharesBuying;
        public int NoOfSharesBuying
        {
            get
            {
                return noOfSharesBuying;
            }
            set
            {
                noOfSharesBuying = value;
                RaisePropertyChanged(() => NoOfSharesBuying);
            }
        }

        private int noOfSharesSelling;
        public int NoOfSharesSelling
        {
            get
            {
                return noOfSharesSelling;
            }
            set
            {
                noOfSharesSelling = value;
                RaisePropertyChanged(() => NoOfSharesSelling);
            }
        }

        private double upperPriceLimit;
        public double UpperPriceLimit
        {
            get
            {
                return upperPriceLimit;
            }
            set
            {
                upperPriceLimit = value;
                RaisePropertyChanged(() => UpperPriceLimit);
            }
        }

        private double lowerPriceLimit;
        public double LowerPriceLimit
        {
            get
            {
                return lowerPriceLimit;
            }
            set
            {
                lowerPriceLimit = value;
                RaisePropertyChanged(() => LowerPriceLimit);
            }
        }

        private bool prioritizeBuying;

        public bool PrioritizeBuying
        {
            get
            {
                return prioritizeBuying;
            }
            set
            {
                prioritizeBuying = value;
                RaisePropertyChanged(() => PrioritizeBuying);
            }
        } 

        private bool prioritizeSelling;

        public bool PrioritizeSelling
        {
            get
            {
                return prioritizeSelling;
            }
            set
            {
                prioritizeSelling = value;
                RaisePropertyChanged(() => PrioritizeSelling);
            }
        } 

        public RelayCommand PlaceBuyingOrderCommand { get; private set; }

        public RelayCommand PlaceSellingOrderCommand { get; private set; }

        public RelayCommand CancelPendingOrderCommand { get; private set; }

        public RelayCommand LogoutCommand { get; private set; }

        private void OnNewMarketInformationAvailable(ShareInformation nu)
        {
            var tmp = MarketInformation.Where(x => x.FirmName.Equals(nu.FirmName));
            var old = tmp.Count() == 0 ? null : tmp.First();
            if (old != null)
            {
                MarketInformation.Insert(MarketInformation.IndexOf(old), nu);
                MarketInformation.Remove(old);
            }
            else
            {
                MarketInformation.Add(nu);
            }
        }

        private void PlaceBuyingOrder()
        {
            var id = FundID + DateTime.Now.Ticks.ToString();
            var order = new Order() { Id = id, InvestorId = FundID, Type = Order.OrderType.BUY, ShareName = SelectedBuyingShare.FirmName, Limit = UpperPriceLimit, TotalNoOfShares = NoOfSharesBuying, NoOfProcessedShares = 0, Prioritize = PrioritizeBuying };
            data.PlaceOrder(order);
        }

        private void PlaceSellingOrder()
        {
            var id = FundID + DateTime.Now.Ticks.ToString();
            var order = new Order() { Id = id, InvestorId = FundID, Type = Order.OrderType.SELL, ShareName = SelectedSellingShare.ShareName, Limit = LowerPriceLimit, TotalNoOfShares = NoOfSharesSelling, NoOfProcessedShares = 0, Prioritize = PrioritizeSelling };
            data.PlaceOrder(order);
        }

        private void CancelPendingOrder()
        {
            data.CancelOrder(SelectedPendingOrder);
        }

        private void Logout()
        {
            data.Dispose();
            App.Current.Shutdown();
        }
    }
}