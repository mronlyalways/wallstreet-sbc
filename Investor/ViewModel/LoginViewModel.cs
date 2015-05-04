using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Investor.Model;
using Investor.View;
using SharedFeatures.Model;

namespace Investor.ViewModel
{

    public class LoginViewModel : ViewModelBase
    {
        private IDataService data;
        private bool submitted;

        public LoginViewModel(IDataService data)
        {
            this.data = data;
            data.AddNewInvestorInformationAvailableCallback(OnRegistrationConfirmed);
            SubmitCommand = new RelayCommand(Submit, () => !Email.Equals(string.Empty) && Budget > 0 && !submitted);
            Email = string.Empty;
            Budget = 0;
            ButtonText = "Submit";
            submitted = false;
        }

        private string email;
        public string Email
        {
            get
            {
                return email;
            }
            set
            {
                email = value;
                RaisePropertyChanged(() => Email);
            }
        }

        private double budget;
        public double Budget
        {
            get
            {
                return budget;
            }
            set
            {
                budget = value;
                RaisePropertyChanged(() => Budget);
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
            data.Login(new Registration() { Email = Email, Budget = Budget });
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