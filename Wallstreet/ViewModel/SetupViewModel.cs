using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace Wallstreet.ViewModel
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
            ResourceLocator.BindXcoDataService(new Uri(url));
            Messenger.Default.Send<NotificationMessage>(new NotificationMessage(this, "Close"));
            var MainWindow = new MainWindow();
            MainWindow.Show();
            this.Cleanup();
        }
    }
}
