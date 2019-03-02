using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.CommandWpf;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;

namespace TradingClient.ViewModels
{
    public class PortfolioDetailsViewModel : ViewModelBase
    {
       
        #region Members

        private readonly Portfolio _portfolio;
        private bool _isBusy;
        private string _name;
        private string _baseCurrency;
        private bool _editing;
        private int _selectedTabIndex;
        private AccountWithConnection _selectedAccount;
        private StrategyItem _selectedStrategy;

        #endregion //Members

        #region Properties

        private IApplicationCore Core { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (value != _isBusy)
                {
                    _isBusy = value;
                    OnPropertyChanged("IsBusy");
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged("SelectedTabIndex");
                }
            }
        }

        public AccountWithConnection SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (_selectedAccount != value)
                {
                    _selectedAccount = value;
                    OnPropertyChanged("SelectedAccount");
                    RemoveAccountCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public StrategyItem SelectedStrategy
        {
            get => _selectedStrategy;
            set
            {
                if (_selectedStrategy != value)
                {
                    _selectedStrategy = value;
                    OnPropertyChanged("SelectedStrategyItem");
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string BaseCurrency
        {
            get => _baseCurrency;
            set
            {
                if (_baseCurrency != value)
                {
                    _baseCurrency = value;
                    OnPropertyChanged("BaseCurrency");
                }
            }
        }

        public bool IsEditing => _editing;

        public ObservableCollection<AccountWithConnection> Accounts { get; private set; }

        public ObservableCollection<StrategyItem> Strategies { get; private set; }

        public IEnumerable<string> AvailableCurrencies { get; private set; }

        public IEnumerable<string> AvailableDatafeeds { get; private set; }

        #endregion Properties

        #region Commands

        public ICommand AddAccountCommand { get; private set; }

        public RelayCommand RemoveAccountCommand { get; private set; }

        public ICommand AddStrategyCommand { get; private set; }

        public ICommand RemoveStrategyCommand { get; private set; }

        public ICommand SubmitCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        #endregion Commands
        
        public PortfolioDetailsViewModel(IApplicationCore core, Portfolio portfolio, 
            bool editing = false, Strategy selectedStrategy = null)
        {
            Core = core;
            _portfolio = portfolio;
            _editing = editing;
            AvailableDatafeeds = Core.DataManager.DatafeedList;
            AvailableCurrencies = Core.DataManager.Broker.AvailableCurrencies; 
            Name = _portfolio.Name;
            SubmitCommand = new RelayCommand(Submit);
            CancelCommand = new RelayCommand(SetFalseResult);
            AddAccountCommand = new RelayCommand(AddAccount);
            RemoveAccountCommand = new RelayCommand(RemoveAccount, CanAccountRemoveExecute);
            AddStrategyCommand = new RelayCommand(AddStrategy);
            RemoveStrategyCommand = new RelayCommand(RemoveStrategy, CanStrategyRemoveExecute);
            Core.DataManager.OnPortfolioChanged += DataProviderOnPortfolioChanged;
            BaseCurrency = portfolio.BaseCurrency;
            Strategies = new ObservableCollection<StrategyItem>(_portfolio.Strategies
                .Select(i => new StrategyItem(i)));
            Accounts = new ObservableCollection<AccountWithConnection>(_portfolio.Accounts
                .Select(p => new AccountWithConnection(p, GetConnectionState(p))));
            

            if (selectedStrategy != null)
            {
                SelectedTabIndex = 1;
                var item = Strategies.FirstOrDefault(i => i.ID == selectedStrategy.ID && i.Name == selectedStrategy.Name);
                if (item == null)  //new strategy to add
                {
                    Strategies.Add(new StrategyItem(selectedStrategy));
                    SelectedStrategy = Strategies[Strategies.Count - 1];
                }
                else
                {
                    SelectedStrategy = item;
                }
            }
        }

        private void DataProviderOnPortfolioChanged(object sender, EventArgs<PortfolioActionEventArgs> eventArgs)
        {
            if (!string.IsNullOrEmpty(eventArgs.Value.PortfolioName) && eventArgs.Value.PortfolioName == Name)
            {
                if (string.IsNullOrEmpty(eventArgs.Value.Error))
                {
                    Core.DataManager.OnPortfolioChanged -= DataProviderOnPortfolioChanged;
                    if (_editing)
                        UpdateWorkingSignalStrategyParams();

                    if (eventArgs.Value.IsRemoving)  //remove portfolio folder
                    {
                        Core.PathManager.DeletePortfolioStrategySignalFolder(Core.Settings.UserName,
                            eventArgs.Value.PortfolioName);
                    }

                    DialogResult = true;
                }
                IsBusy = false;
            }
        }

        private void AddAccount()
        {
            var viewModel = new SelectBrokerAccountViewModel(Core);
            var res = Core.ViewFactory.ShowDialogView(viewModel);
            if (res.HasValue && res.Value && viewModel.SelectedAccount != null)
            {   
                int count = 1;
                while(Accounts.Any(p => p.Item.Name == "Account " + count))
                    count++;

                var acct = new PortfolioAccount
                {
                    Name = "Account " + count,
                    BrokerName = viewModel.SelectedAccount.BrokerName,
                    DataFeedName = viewModel.SelectedAccount.DataFeedName,
                    Account = viewModel.SelectedAccount.Account,
                    UserName = viewModel.SelectedAccount.UserName,
                    ID = -1
                };
                Accounts.Add(new AccountWithConnection(acct, "Connected"));
            }
        }

        private void RemoveAccount()
        {
            if (SelectedAccount != null)
            {
                Core.ViewFactory.Invoke(() =>
                {
                    Accounts.Remove(SelectedAccount);
                    SelectedAccount = null;
                });
            }
        }

        private void AddStrategy()
        {
            int count = 1;
            while (Strategies.Any(i => i.Name == "Strategy " + count))
                count++;

            Strategies.Add(new StrategyItem("Strategy " + count));
        }

        private void RemoveStrategy()
        {
            if (SelectedStrategy != null)
            {
                Core.ViewFactory.Invoke(() =>
                {
                    Strategies.Remove(SelectedStrategy);
                    SelectedStrategy = null;
                });
            }
        }

        private void Submit()
        {
            var error = ValidateData();
            if (!string.IsNullOrWhiteSpace(error))
            {
                Core.ViewFactory.ShowMessage(error);
                return;
            }

            if (_editing && Core.DataManager.Portfolios.SelectMany(i => i.Strategies).SelectMany(i => i.Signals)
                .Any(i => i.State == State.Working))
            {
                var dlgRes = Core.ViewFactory.ShowMessage("Changing strategy parameters might influence \r\n" 
                    + "risk management logic for running signals. \r\nContinue anyway?", 
                    "Confirm", MsgBoxButton.YesNo, MsgBoxIcon.Question);
                if (dlgRes != DlgResult.Yes)
                    return;
            }

            IsBusy = true;
            Task.Run(() =>
            {
                var strategies = new Strategy[Strategies.Count];
                for (int i = 0; i < strategies.Length; i++)
                {
                    strategies[i] = new Strategy
                    {
                        ID = Strategies[i].ID,
                        Name = Strategies[i].Name,
                        ExposedBalance = Strategies[i].ExposedBalance,
                        Datafeeds = new ObservableCollection<string>(Strategies[i].Datafeeds),
                        Signals = new ObservableCollection<Signal>()
                    };

                    var sigNames = Strategies[i].Signals.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim()).Distinct();
                    foreach (var item in sigNames)
                    {
                        strategies[i].Signals.Add(new Signal
                        {
                            FullName = $"{Name}\\{strategies[i].Name}\\{item}",
                            Parent = strategies[i],
                            Parameters = new List<ScriptingParameterBase>(),
                            Selections = new List<SignalSelection>()
                        });
                    }
                }

                var portfolio = new Portfolio
                {
                    ID = _portfolio.ID,
                    Name = Name,
                    BaseCurrency = BaseCurrency,
                    User = _portfolio.User,
                    Accounts = new ObservableCollection<PortfolioAccount>(Accounts.Select(i => i.Item).ToList()),
                    Strategies = new ObservableCollection<Strategy>(strategies)
                };

                if (_editing)
                    Core.DataManager.UpdatePortfolio(portfolio);
                else
                    Core.DataManager.CreatePortfolio(portfolio);
            });
        }

        private string ValidateData()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "Please enter portfolio name";

            if (!Extentions.IsUserObjectNameValid(Name))
                return "Portfolio name is invalid";

            if (string.IsNullOrEmpty(BaseCurrency))
                return "Please specify base currency";

            if (Core.DataManager.Portfolios.Count(p => p.Name == Name) > (_editing ? 1 : 0))
                return "Portfolio with specified name already exists";

            if (!Accounts.Any())
                return "Please add at least one account for this portfolio";

            var accNames = Accounts.Select(i => i.Item.Name).ToArray();
            if (accNames.Length != accNames.Distinct().Count())
                return "Account names must be unique";

            if (Strategies.Any(i => string.IsNullOrWhiteSpace(i.Name) || !i.Datafeeds.Any()))
                return "Please fill in strategy properties";

            var stratNames = Strategies.Select(i => i.Name).ToArray();
            if (stratNames.Length != stratNames.Distinct().Count())
                return "Strategy names must be unique";

            foreach (var name in stratNames)
            {
                if (!Extentions.IsUserObjectNameValid(name))
                    return "Invalid strategy name: " + name;
            }

            return null;
        }

        private void UpdateWorkingSignalStrategyParams()
        {
            foreach (var item in Core.DataManager.Portfolios
                .SelectMany(i => i.Strategies).SelectMany(i => i.Signals)
                .Where(i => i.State == State.Working))
            {
                Core.DataManager.ScriptingManager.UpdateSignalStrategy(item.FullName, new StrategyParams(item.Parent));
            }
        }
        
        private string GetConnectionState(PortfolioAccount item)
        {
            var isConnected = Core.DataManager.Broker.ActiveAccounts
                .Any(i => i.BrokerName.Equals(item.BrokerName) && i.UserName.Equals(item.UserName));
            return isConnected ? "Connected" : "Disconnected";
        }

        private void SetFalseResult() => DialogResult = false;

        private bool CanAccountRemoveExecute() => SelectedAccount != null;

        private bool CanStrategyRemoveExecute() => SelectedStrategy != null;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                Core.DataManager.OnPortfolioChanged -= DataProviderOnPortfolioChanged; 
        }
        
        #region Helper classes

        public class AccountWithConnection
        {
            public PortfolioAccount Item { get; set; }
            public string Connection { get; set; }

            public AccountWithConnection(PortfolioAccount acct, string connection)
            {
                Item = (PortfolioAccount)acct.Clone();
                Connection = connection ?? string.Empty;
            }
        }

        public class StrategyItem : INotifyPropertyChanged
        {
            private string _name;
            private ObservableCollection<string> _datafeeds;
            private string _signals;
            private decimal _exposedBalance;

            public int ID { get; set; }

            public string Name
            {
                get => _name;
                set
                {
                    if (value != _name)
                    {
                        _name = value;
                        OnPropertyChanged("Name");
                    }
                }
            }

            public ObservableCollection<string> Datafeeds
            {
                get => _datafeeds;
                set
                {
                    if (value != _datafeeds)
                    {
                        _datafeeds = value;
                        OnPropertyChanged("Datafeeds");
                    }
                }
            }
                    
            public decimal ExposedBalance
            {
                get => _exposedBalance;
                set
                {
                    if (value != _exposedBalance)
                    {
                        _exposedBalance = value;
                        OnPropertyChanged("ExposedBalance");
                    }
                }
            }

            public string Signals
            {
                get => _signals;
                set
                {
                    if (value != _signals)
                    {
                        _signals = value;
                        OnPropertyChanged("Signals");
                    }
                }
            }

            public bool HasSignals => !string.IsNullOrWhiteSpace(Signals);

            public event PropertyChangedEventHandler PropertyChanged;

            public StrategyItem(string name)
            {
                Name = name ?? string.Empty;
                Datafeeds = new ObservableCollection<string>();
                Signals = string.Empty;
            }

            public StrategyItem(Strategy strategy)
            {
                ID = strategy.ID;
                Name = strategy.Name;
                ExposedBalance = strategy.ExposedBalance;
                Datafeeds = new ObservableCollection<string>( strategy.Datafeeds);
                Signals = strategy.Signals.Any() ? string.Join(", ", strategy.Signals.Select(i => i.Name)) : string.Empty;
            }

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

    }
}