using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Fondsmanager.Model;
using Fondsmanager.View;
using SharedFeatures.Model;

namespace Fondsmanager.ViewModel
{

    public class LoginViewModel : ViewModelBase
    {
        private IDataService data;
        private bool submitted;

        public LoginViewModel(IDataService data)
        {
            this.data = data;
            data.AddNewInvestorInformationAvailableCallback(OnRegistrationConfirmed);
            SubmitCommand = new RelayCommand(Submit, () => !FundID.Equals(string.Empty) && FundAssests >= 0 && FundShares >= 0 && !submitted);
            FundID = string.Empty;
            FundAssests = 0;
            FundShares = 0;
            ButtonText = "Submit";
            submitted = false;
        }

        private string fundid;
        public string FundID
        {
            get
            {
                return fundid;
            }
            set
            {
                fundid = value;
                RaisePropertyChanged(() => FundID);
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        private double fundassets;
        public double FundAssests
        {
            get
            {
                return fundassets;
            }
            set
            {
                fundassets = value;
                RaisePropertyChanged(() => FundAssests);
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        private long fundshares;

        public long FundShares
        {
            get
            {
                return fundshares;
            }
            set
            {
                fundshares = value;
                RaisePropertyChanged(() => FundShares);
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        private string buttonText;
        public string ButtonText
        {
            get
            {
                return buttonText;
            }
            set
            {
                buttonText = value;
                RaisePropertyChanged(() => ButtonText);
                SubmitCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand SubmitCommand { get; private set; }

        public void Submit()
        {
            //TODO: data.Login(new Registration() { Email = Email, Budget = Budget });
            ButtonText = "Waiting for confirmation ...";
            submitted = true;
            SubmitCommand.RaiseCanExecuteChanged();
        }

        public void OnRegistrationConfirmed(InvestorDepot depot)
        {
            Messenger.Default.Send<NotificationMessage>(new NotificationMessage(this, "Close"));
            var MainWindow = new MainWindow();
            MainWindow.Show();
            data.RemoveNewInvestorInformationAvailableCallback(OnRegistrationConfirmed);
            this.Cleanup();
        }
    }
}