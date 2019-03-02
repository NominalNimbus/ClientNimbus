using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using TradingClient.DataProvider.TradingService;
using TradingClient.Interfaces;
using Bar = TradingClient.Data.Contracts.Bar;
using Portfolio = TradingClient.Data.Contracts.Portfolio;
using CreateSimulatedBrokerAccountInfo = TradingClient.Data.Contracts.CreateSimulatedBrokerAccountInfo;
using Security = TradingClient.Data.Contracts.Security;
using TradingClient.Data.Contracts;

namespace TradingClient.DataProvider
{
    public class DataManager : IDataManager
    {
        private readonly ServiceConnector _serviceConnector;
        private readonly DataFeed _dataFeed;
        private readonly Dispatcher _dispatcher;

        public IBrokerManager Broker { get; private set; }
        public IScriptingManager ScriptingManager { get; private set; }
        public ObservableCollection<Portfolio> Portfolios { get; private set; }
        public List<string> DatafeedList => _dataFeed.Datafeeds;
        public List<Security> Instruments => _dataFeed.Securities;
        public bool IsConnected => _serviceConnector.ConnectionStatus == ConnectionState.Connect;
        public bool IsOffline => _dataFeed.IsOffline;

        #region Events

        public event Action<TickData> OnNewTick;

        public event Action<string, IEnumerable<Bar>> OnHistoryDataReady;

        public event EventHandler<EventArgs<PortfolioActionEventArgs>> OnPortfolioChanged;

        public event EventHandler<EventArgs<string>> OnDataFeedMessage
        {
            add => _serviceConnector.Error += value;
            remove => _serviceConnector.Error -= value;
        }

        public event Action<ConnectionState, string> OnConnectionStatusChanged
        {
            add => _serviceConnector.ConnectionStatusChanged += value;
            remove => _serviceConnector.ConnectionStatusChanged -= value;
        }

        #endregion

        public DataManager()
        {
            _serviceConnector = new ServiceConnector();
            _serviceConnector.PortfolioList += ConnectorOnPortfolioList;
            _serviceConnector.PortfolioChanged += ConnectorOnPortfolioChanged;

            _dataFeed = new DataFeed(_serviceConnector);
            _dataFeed.OnTicks += DataManagerOnQuote;
            _dataFeed.OnHistoricalData += DataManagerOnHistory;

            Broker = new BrokerManager(_serviceConnector, this);
            ScriptingManager = new ScriptingManager(_serviceConnector);
            Portfolios = new ObservableCollection<Portfolio>();
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public void Subscribe(string symbol, string df, object subscriber)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));
            _dataFeed.SubscribeTick(symbol, df, subscriber);
        }

        public void Unsubscribe(string symbol, string df, object subscriber)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));
            _dataFeed.UnsubscribeTick(symbol, df, subscriber);
        }

        public void GetHistory(string id, string symbol, string df, TimeFrame tf, int interval,
            DateTime from, DateTime to, int bars, int level, bool? includeWeekendData)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Argument can not be empty", nameof(id));
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Argument can not be empty", nameof(symbol));

            _dataFeed.GetHistoricalData(id, symbol, df, tf, interval, from, to, bars, level, includeWeekendData);
        }

        public Security GetInstrumentFromDataFeed(string symbol, string df)
        {
            if (symbol == null || df == null)
                throw new ArgumentNullException(nameof(symbol));
            return _dataFeed.GetSecurityFromDataFeed(symbol, df);
        }

        public Security GetInstrumentFromBroker(string symbol, string broker)
        {
            if (symbol == null || broker == null)
                throw new ArgumentNullException(nameof(symbol));

            var account =
                Broker.ActiveAccounts.FirstOrDefault(p => p.BrokerName.Equals(broker, StringComparison.InvariantCultureIgnoreCase) &&
                                                        p.AvailableSymbols.Any( q => q.Symbol.Equals(symbol, StringComparison.InvariantCultureIgnoreCase)));

            return
                account?.AvailableSymbols.FirstOrDefault(q => q.Symbol.Equals(symbol,StringComparison.InvariantCultureIgnoreCase));
        }

        public TickData GetTick(string symbol, string df)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));
            return _dataFeed.GetLastTick(symbol, df);
        }

        public List<string> GetAvailableSymbols(string dataFeed)
        {
            return _dataFeed.GetDataFeedSymbols(dataFeed);
        }

        public void CreatePortfolio(Portfolio portfolio)
        {
            _serviceConnector.Send(new PortfolioActionRequest
            {
                Action = PortfolioAction.Add,
                Portfolio = DataConverter.ToServerPortfolio(portfolio)
            });
        }

        public void UpdatePortfolio(Portfolio portfolio)
        {
            _serviceConnector.Send(new PortfolioActionRequest
            {
                Action = PortfolioAction.Edit,
                Portfolio = DataConverter.ToServerPortfolio(portfolio)
            });
        }

        public void DeletePortfolio(Portfolio portfolio)
        {
            _serviceConnector.Send(new PortfolioActionRequest
            {
                Action = PortfolioAction.Remove,
                Portfolio = DataConverter.ToServerPortfolio(portfolio)
            });
        }

        public void CreateSimulatedAccount(CreateSimulatedBrokerAccountInfo account)
        {
            _serviceConnector.Send(new CreateSimulatedBrokerAccountRequest
            {
                Account = DataConverter.ToDsCreateSimulatedAccount(account)
            });
        }

        #region Login/Logout

        public void Login(string host, int port, string userName, string password, Action<string> resultAction)
        {
            var res = _serviceConnector.Login(userName, password, host, port);
            if (!string.IsNullOrEmpty(res))
            {
                resultAction(res);
                return;
            }

            res = _dataFeed.SendInitializeRequests();

            if (!string.IsNullOrEmpty(res))
            {
                _serviceConnector.Logout();
            }
            else
            {
                try { Broker.Initialize(); }
                catch (Exception ex)
                {
                    _serviceConnector.Logout();
                    resultAction(ex.Message);
                    return;
                }
            }

            resultAction(res);
        }

        public string Reconnect()
        {
            Logout();
            var res = _serviceConnector.ReLogin();

            if (string.IsNullOrEmpty(res))
            {
                _dataFeed.ResubscribeAll();
                _serviceConnector.UpdateUserInfoAfterReLogin();
            }

            return res;
        }

        public void Logout() =>
            _serviceConnector.Logout();

        #endregion

        #region Events

        private void DataManagerOnQuote(object sender, EventArgs<List<TickData>> args)
        {
            if (OnNewTick != null)
            {
                foreach (var quote in args.Value)
                    OnNewTick(quote);
            }
        }

        private void DataManagerOnHistory(object sender, EventArgs<string, List<Bar>> args)
        {
            OnHistoryDataReady?.Invoke(args.Value1, args.Value2);
        }

        private void ConnectorOnPortfolioChanged(object sender, EventArgs<PortfolioActionEventArgs> eventArgs)
        {
            InvokeInUI(() =>
            {
                if (string.IsNullOrEmpty(eventArgs.Value.Error) && eventArgs.Value.Portfolio is TradingService.Portfolio)
                {
                    var dsPortfolio = (TradingService.Portfolio)eventArgs.Value.Portfolio;
                    var portfolio = Portfolios.FirstOrDefault(p => p.ID == dsPortfolio.ID);

                    if (portfolio == null && !eventArgs.Value.IsRemoving)
                    {
                        bool added = false;
                        for (int i = 0; i < Portfolios.Count; i++)
                        {
                            if (dsPortfolio.ID < Portfolios[i].ID)
                            {
                                Portfolios.Insert(i, DataConverter.ToClientPortfolio(dsPortfolio, ScriptingManager.Signals));
                                added = true;
                                break;
                            }
                        }
                        if (!added)
                            Portfolios.Add(DataConverter.ToClientPortfolio(dsPortfolio, ScriptingManager.Signals));
                    }
                    else
                    {
                        var idx = Portfolios.IndexOf(portfolio);
                        Portfolios.Remove(portfolio);
                        if(!eventArgs.Value.IsRemoving && idx > -1)
                            Portfolios.Insert(idx, DataConverter.ToClientPortfolio(dsPortfolio, ScriptingManager.Signals));
                    }
                }

                OnPortfolioChanged?.Invoke(this, eventArgs);
            });
        }

        private void ConnectorOnPortfolioList(object sender, EventArgs<List<TradingService.Portfolio>> eventArgs)
        {
            InvokeInUI(() =>
            {
                Portfolios.Clear();
                foreach (var portfolio in eventArgs.Value.OrderBy(i => i.ID))
                    Portfolios.Add(DataConverter.ToClientPortfolio(portfolio, ScriptingManager.Signals));
            });
        }

        private void InvokeInUI(Action action)
        {
            if (!_dispatcher.CheckAccess())
                _dispatcher.Invoke(action);
            else
                action();
        }

        #endregion
    }
}