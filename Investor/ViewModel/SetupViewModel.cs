using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Investor.View;

namespace Investor.ViewModel
{
    public class SetupViewModel : ViewModelBase
    {

        public SetupViewModel()
        {
            SubmitCommand = new RelayCommand(Submit);
        }

        private string url;

        public string Url
        {
            get
            {
                return url;
            }
            set
            {
                url = value;
                RaisePropertyChanged(() => Url);
            }
        }

        public RelayCommand SubmitCommand { get; private set; }

        public void Submit()
        {
            string[] urls = url.Split(',');
            IList<Uri> list = new List<Uri>();
            foreach (string s in urls)
            {
                list.Add(new Uri(s));
            }
            ViewModelLocator.BindXcoDataService(list);
            Messenger.Default.Send<NotificationMessage>(new NotificationMessage(this, "Close"));
            var LoginWindow = new LoginWindow();
            LoginWindow.Show();
            this.Cleanup();
        }

    }
}
