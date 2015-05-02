using GalaSoft.MvvmLight;
using SharedFeatures.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Investor.Model;
using System;

namespace Investor.ViewModel
{

    public class MainViewModel : ViewModelBase
    {
        private IDataService data;
        private InvestorDepot depot;
        
        public MainViewModel(IDataService data)
        {
            this.data = data;
            depot = data.Depot;
        }
    }
}