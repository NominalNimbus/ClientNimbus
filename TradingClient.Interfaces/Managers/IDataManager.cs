using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;

namespace TradingClient.Interfaces
{
    public interface IDataManager
    {
        #region Properties

        bool IsConnected { get; }

        bool IsOffline { get; }

        IBrokerManager Broker { get; }

        IScriptingManager ScriptingManager { get; }

        List<Security> Instruments { get; }

        ObservableCollection<Portfolio> Portfolios { get; }

        List<string> DatafeedList { get; }

        #endregion //Properties

        #region Events

        event EventHandler<EventArgs<PortfolioActionEventArgs>> OnPortfolioChanged;

        event EventHandler<EventArgs<string>> OnDataFeedMessage;

        event Action<string, IEnumerable<Bar>> OnHistoryDataReady;

        event Action<ConnectionState, string> OnConnectionStatusChanged;

        event Action<TickData> OnNewTick;

        #endregion //Events

        #region Methods

        void Login(string host, int port, string userName, string password, Action<string> resultAction);

        void Logout();

        string Reconnect();

        void Subscribe(string symbol, string df, object subscriber);

        void Unsubscribe(string symbol, string df, object subscriber);

        void GetHistory(string id, string symbol, string df, TimeFrame tf, int interval, DateTime from, DateTime to, int bars, int level, bool? includeWeekendData);

        Security GetInstrumentFromDataFeed(string symbol, string df);

        Security GetInstrumentFromBroker(string symbol, string broker);

        TickData GetTick(string symbol, string df);

        List<string> GetAvailableSymbols(string dataFeed);

        void CreatePortfolio(Portfolio portfolio);

        void UpdatePortfolio(Portfolio portfolio);

        void DeletePortfolio(Portfolio portfolio);

        void CreateSimulatedAccount(CreateSimulatedBrokerAccountInfo account);

        #endregion //Methods
    }
}