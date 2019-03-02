using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using NLog;
using TradingClient.Common;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{

    public class BrokerLoginViewModel : ViewModelBase
    {
        #region Members

        private readonly IMainViewModel _mainView;
        private readonly bool _reLogin;

        private bool _isLoginIn;
        private AccountBrokerInfo _selectedItem;

        #endregion //Members

        #region Properties

        private static Logger Logger { get; } = NLog.LogManager.GetCurrentClassLogger();

        private IApplicationCore Core { get; }

        public bool IsLoginIn
        {
            get => _isLoginIn;
            private set
            {
                if (value.Equals(_isLoginIn))
                    return;
                _isLoginIn = value;
                OnPropertyChanged(nameof(IsLoginIn));
            }
        }

        public AccountBrokerInfo SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (value != null && value.Equals(_selectedItem))
                    return;

                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                RemoveCommand.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<AccountBrokerInfo> Items { get; private set; }

        public ObservableCollection<AvailableBrokerInfo> Brokers { get; private set; }
                
        public string SelectedBroker
        {
            get => null;
            set
            {
                if (value != null)
                {
                    AddAccountCommandExecute(value);
                }
            }
        }

        #endregion //Properties

        #region Commands

        public ICommand LoginCommand { get; private set; }

        public RelayCommand RemoveCommand { get; private set; }

        public ICommand CreateAccountCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        #endregion Commands

        public BrokerLoginViewModel(IApplicationCore core, IMainViewModel mainView, bool reLogin = false)
        {
            Core = core;
            _mainView = mainView;
            _reLogin = reLogin;

            Brokers = new ObservableCollection<AvailableBrokerInfo>(Core.DataManager.Broker.Brokers);

            Core.ViewFactory.Invoke(() =>
            {
                if (!reLogin)
                {
                    Items = new ObservableCollection<AccountBrokerInfo>(core.Settings.Accounts
                            .Select(item => CreateAccountInfoFromAccount(item.ToAccountInfo())).ToList());
                }
                else
                {
                    Items = new ObservableCollection<AccountBrokerInfo>(Core.DataManager.Broker.ActiveAccounts
                            .Select(item => CreateAccountInfoFromAccount((AccountInfo)item.Clone())).ToList());
                }

                var BindItems = (CollectionView)CollectionViewSource.GetDefaultView(Items);
                BindItems.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
            });

            LoginCommand = new RelayCommand(LoginCommandExecute);
            CancelCommand = new RelayCommand(CancelCommandExecute);
            RemoveCommand = new RelayCommand(RemoveAccountCommandExecute, CanRemoveExecute);
            CreateAccountCommand = new RelayCommand(CreateAccountCommandExecute);

            Core.DataManager.Broker.OnAddedNewBrokerAccount += Broker_OnAddedNewBrokerAccount;
        }

        protected override void Dispose(bool disposing)
        {
            Core.DataManager.Broker.OnAddedNewBrokerAccount -= Broker_OnAddedNewBrokerAccount;
            base.Dispose(disposing);
        }

        #region Command Execute methods

        private void LoginCommandExecute()
        {
            if (!ValidateAccounts())
                return;

            IsLoginIn = true;
            Task.Factory.StartNew(() =>
            {
                if (_reLogin)
                    ReLogin();
                else
                    Login(Items.Select(s=>s.AccountInfo).ToList());
            });
        }

        private void CancelCommandExecute() => 
            DialogResult = false;

        private void RemoveAccountCommandExecute()
        {
            if (SelectedItem == null)
                return;

            Core.ViewFactory.Invoke(() =>
            {
                if (Core.DataManager.ScriptingManager
                    .Signals.Any(i => i.State == State.Working))
                {
                    Core.ViewFactory.ShowMessage("Please stop running signals first",
                        "Error", MsgBoxButton.OK, MsgBoxIcon.Warning);
                }
                else
                {
                    Items.Remove(SelectedItem);
                    SelectedItem = Items.FirstOrDefault();
                }
            });
        }
        
        private bool CanRemoveExecute() =>
            SelectedItem != null;

        private void CreateAccountCommandExecute()
        {
            if (SelectedItem == null)
                return;

            var broker = GetBrokerByName(SelectedItem.BrokerName);
            using (var vm = new CreateSimulatedAccountViewModel(Core, broker))
            {
                if (Core.ViewFactory.ShowDialogView(vm) == true)
                {
                    Core.DataManager.CreateSimulatedAccount(vm.Account.Account);
                }
            }
        }

        private void AddAccountCommandExecute(string brokerName)
        {
            Core.ViewFactory.Invoke(() =>
            {
                var account = CreateAccountInfoFromAccount(new AccountInfo() { BrokerName = brokerName });
                Items.Add(account);
                OnPropertyChanged(nameof(Items));
                SelectedItem = account;
            });
        }

        #endregion //Command Execute methods

        #region private methods

        private void Login(List<AccountInfo> items)
        {
            try
            {
                Core.DataManager.Broker.Login(items, error =>
                {
                    IsLoginIn = false;
                    if (string.IsNullOrEmpty(error))
                    {
                        Core.Settings.Accounts = Core.DataManager.Broker.ActiveAccounts
                            .Select(a => new AccountSetting(a)).ToList<IAccountSetting>();
                        SelectBrokerAccount();
                        DialogResult = true;
                    }
                    else
                    {
                        Core.ViewFactory.ShowMessage(error, "Error", MsgBoxButton.OK,
                            MsgBoxIcon.Warning);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Login");
                IsLoginIn = false;
                Core.ViewFactory.ShowMessage(ex.Message, "Warning", MsgBoxButton.OK, MsgBoxIcon.Warning);
            }
        }

        private void ReLogin()
        {
            try
            {
                var logoutAccounts = Core.DataManager.Broker.ActiveAccounts.Where(p => !Items.Any(q => q.AccountInfo.Equals(p)))
                    .ToList();
                if (logoutAccounts.Count > 0)
                {
                    var res = Core.DataManager.Broker.Logout(logoutAccounts);
                    if (!string.IsNullOrEmpty(res))
                    {
                        Core.ViewFactory.ShowMessage(res, "Error", MsgBoxButton.OK, MsgBoxIcon.Warning);
                        return;
                    }

                    SelectBrokerAccount();
                }

                var loginAccounts = Items.Select(i=>i.AccountInfo).Where(p => !Core.DataManager.Broker.ActiveAccounts.Any(q => q.Equals(p)))
                    .ToList();
                if (loginAccounts.Count > 0)
                    Login(loginAccounts);
                else
                    DialogResult = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "ReLogin");
                IsLoginIn = false;
                Core.ViewFactory.ShowMessage(ex.Message, "Warning", MsgBoxButton.OK, MsgBoxIcon.Warning);
            }
        }

        private void SelectBrokerAccount()
        {
            if (Core.DataManager.Broker.ActiveAccounts.Count > 0)
            {
                var broker =
                    Core.DataManager.Broker.ActiveAccounts.FirstOrDefault(p =>
                        p.BrokerName.Equals(Core.Settings.DefaultBrokerName) &&
                        p.UserName.Equals(Core.Settings.DefaultBrokerAccount)) ??
                    Core.DataManager.Broker.ActiveAccounts.First();

                if (broker != null)
                {
                    broker.IsDefault = true;
                    Core.DataManager.Broker.DefaultAccount = broker;
                }
            }
        }

        private AvailableBrokerInfo GetBrokerByName(string name)
            => Brokers.FirstOrDefault(b => b.BrokerName == name);
        
        private void Broker_OnAddedNewBrokerAccount(object sender, Interfaces.EventArgs<CreateSimulatedBrokerAccountInfo, string> e)
        {
            var broker = Brokers.FirstOrDefault(b => b.BrokerName == e.Value1.BrokerName);
            if (broker != null)
            {
                Core.ViewFactory.Invoke(() =>
                {
                    string message = string.IsNullOrEmpty(e.Value2) ? "Account added successfully" : $"Can't add account. Error:{e.Value2}";
                    _mainView.ShowNotification(message, 5);

                    if (string.IsNullOrEmpty(e.Value2) && !broker.Accounts.Contains(e.Value1.AccountName))
                    {
                        broker.Accounts.Add(e.Value1.AccountName);
                        foreach (var item in Items.OfType<SimulatedAccountBrokerInfoItem>())
                        {
                            if (item.BrokerName == e.Value1.BrokerName && !item.AccountList.Contains(e.Value1.AccountName))
                            {
                                item.AccountList.Add(e.Value1.AccountName);
                            }
                        }
                        if (SelectedItem != null && SelectedItem is SimulatedAccountBrokerInfoItem simulated)
                        {
                            simulated.AccountName = e.Value1.AccountName;
                        }
                    }
                });
            }
        }

        private AccountBrokerInfo CreateAccountInfoFromAccount(AccountInfo account)
        {
            var brokerInfo = Brokers.FirstOrDefault(b => b.BrokerName == account.BrokerName);
            if (string.IsNullOrEmpty(account.DataFeedName))
                account.DataFeedName = brokerInfo.DataFeedName;

            switch (brokerInfo.BrokerType)
            {
                case BrokerType.Live:
                    return new LiveAccountBrokerInfoItem(account, brokerInfo.Url);
                case BrokerType.Demo:
                    return new LiveAccountBrokerInfoItem(account, brokerInfo.Url, "Demo");
                case BrokerType.Simulated:
                    return new SimulatedAccountBrokerInfoItem(account, new ObservableCollection<string>(brokerInfo.Accounts))
                    {
                        DataFeeds = new ObservableCollection<string>(Core.DataManager.DatafeedList)
                    };
                case BrokerType.None:
                default:
                    return null;
            }
        }

        private bool ValidateAccounts()
        {
            var error = SelectedItem?.Validate();
            if(!string.IsNullOrEmpty(error))
            {
                Core.ViewFactory.ShowMessage(error, "Error", MsgBoxButton.OK, MsgBoxIcon.Warning);
                return false;
            }

            foreach (var item in Items)
            {
                error = item?.Validate();
                if (!string.IsNullOrEmpty(error))
                {
                    SelectedItem = item;
                    Core.ViewFactory.ShowMessage(error, "Error", MsgBoxButton.OK, MsgBoxIcon.Warning);
                    return false;
                }
            }

            //find duplicate
            for(int i=0; i<Items.Count;i++)
            {
                for (int j = i + 1; j < Items.Count; j++)
                {
                    if(Items[j].IsDuplicateAccount(Items[i].BrokerName, Items[i].Account))
                    {
                        Core.ViewFactory.ShowMessage($"Duplicate {Items[i].Account} account in {Items[i].BrokerName}", "Error", MsgBoxButton.OK, MsgBoxIcon.Warning);
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion //private methods

    }

}