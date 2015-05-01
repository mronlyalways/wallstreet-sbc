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
        private IDataService service;
        public InvestorDepot depot;
        public MainViewModel(IDataService service)
        {
            this.service = service;
            this.service.OnUpdateForInvestorDepotAvailable(d =>
            {
                Console.WriteLine("Depot has an update:{0}{1}", System.Environment.NewLine, d.ToString());
                this.depot = d;
            });
        }

        public void Register(string email, string budget)
        {
            double b;

            if (!double.TryParse(budget, out b))
            {
                b = 0;
            }

            if (b >= 0 && email.Length > 0)
            {
                Registration r = new Registration() { Budget = b, InvestorEmail = email };
                this.service.Login(r);
            }
        }
    }

}