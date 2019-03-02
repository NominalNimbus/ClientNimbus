using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

using TradingClient.Data.Contracts;

namespace TradingClient.Interfaces
{
    public interface IBrokerManager : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Is user logged in and trading is allowed
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// All filled orders
        /// </summary>
        List<Order> OrderActivity { get; }

        /// <summary>
        /// All pending orders 
        /// </summary>
        List<Order> PendingOrders { get; }

        /// <summary>
        /// All canceled orders due current session
        /// </summary>
        List<Order> SessionOrderHistory { get; }

        /// <summary>
        /// Active broker accounts
        /// </summary>
        ObservableCollection<AccountInfo> ActiveAccounts { get; }

        AccountInfo DefaultAccount { get; set; }
        
        /// <summary>
        /// Available brokers on server
        /// </summary>
        List<AvailableBrokerInfo> Brokers { get; }

        /// <summary>
        /// Current trading positions
        /// </summary>
        List<Position> Positions { get; }

        IEnumerable<string> AvailableCurrencies { get; }

        event EventHandler<EventArgs> OnAccountStateChanged;
        event EventHandler<EventArgs<Order, string>> OnPlaceOrderError;
        event EventHandler<EventArgs<Order>> OrderUpdated;
        event EventHandler<EventArgs<Position>> PositionUpdated;
        event EventHandler<EventArgs> ActiveOrdersChanged;
        event EventHandler<EventArgs> HistoricalOrdersChanged;
        event EventHandler<EventArgs> PositionsChanged;
        event EventHandler<EventArgs<CreateSimulatedBrokerAccountInfo, string>> OnAddedNewBrokerAccount;

        void CancelOrder(Order order, string account);
        void CancelOrder(string id, string account);
        void PlaceOrder(Order order, string account);
        void ClosePosition(string symbol, string account);
        void RequestMoreOrders(int count, int skip);
        void Login(List<AccountInfo> accounts, Action<string> loginResult);
        string Logout(List<AccountInfo> accounts);
        void Reconnect();
        void Initialize();

        /// <summary>
        /// Modify stop loss and take profit of market or pending order
        /// </summary>
        /// <param name="order">Original order</param>
        /// <param name="sl">New top loss, null value for removing</param>
        /// <param name="tp">New take profit, null value for removing</param>
        void ModifySL_TP(Order order, decimal? sl, decimal? tp, bool serverSide);

        decimal GetMarginRateForSymbol(string symbol, string df);
    }
}