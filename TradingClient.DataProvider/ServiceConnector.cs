using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TradingClient.DataProvider.TradingService;
using AccountInfo = TradingClient.Data.Contracts.AccountInfo;
using AvailableBrokerInfo = TradingClient.Data.Contracts.AvailableBrokerInfo;
using ScriptingParameterBase = TradingClient.Data.Contracts.ScriptingParameterBase;
using Signal = TradingClient.Data.Contracts.Signal;
using Indicator = TradingClient.Data.Contracts.Indicator;
using Order = TradingClient.Data.Contracts.Order;
using Position = TradingClient.Data.Contracts.Position;
using SeriesForUpdate = TradingClient.Data.Contracts.SeriesForUpdate;
using ReportField = TradingClient.Data.Contracts.ReportField;
using Side = TradingClient.Data.Contracts.Side;
using Status = TradingClient.Data.Contracts.Status;
using TimeInForce = TradingClient.Data.Contracts.TimeInForce;
using OrderType = TradingClient.Data.Contracts.OrderType;
using CreateSimulatedBrokerAccountInfo = TradingClient.Data.Contracts.CreateSimulatedBrokerAccountInfo;
using ScriptingType = TradingClient.DataProvider.TradingService.ScriptingType;
using TradingClient.Interfaces;
using TradingClient.Data.Contracts;

namespace TradingClient.DataProvider
{
    public class ServiceConnector
    {

        #region Constants

        private const string DisconnectByServer = "close session";
        private const string DisconnectByAnotherUser = "disconnected by another user";

        #endregion //Constants

        #region Fields
        
        private TimeSpan _sendHeartbeatInterval = TimeSpan.FromSeconds(15);
        private Thread _heartbeatThread;
        
        #endregion

        #region Events

        internal event EventHandler<EventArgs<List<Tick>>> OnNewTicks;
        internal event EventHandler<EventArgs<List<TradingService.DataFeed>>> DataFeedsList;
        internal event EventHandler<EventArgs<List<AvailableBrokerInfo>>> BrokerList;
        internal event EventHandler<EventArgs<List<TradingService.Portfolio>>> PortfolioList;
        internal event EventHandler<EventArgs<string, List<AccountInfo>>> BrokerLogin;
        internal event EventHandler<EventArgs<CreateSimulatedBrokerAccountInfo, string>> AddedNewBrokerAccount;
        internal event EventHandler<EventArgs<string>> BrokerLogout;
        internal event EventHandler<EventArgs<string, List<Data.Contracts.Security>>> AvailableSecurities;
        internal event EventHandler<EventArgs<HistoryDataResponse>> HistoricalData;
        internal event EventHandler<EventArgs<string>> Error;
        internal event EventHandler<EventArgs<AccountInfo>> AccountChanged;
        internal event EventHandler<EventArgs<PortfolioActionEventArgs>> PortfolioChanged;

        internal event EventHandler<EventArgs<List<Order>>> HistoricalOrdersAdded;
        internal event EventHandler<EventArgs<string, List<Order>>> OrdersChanged;
        internal event EventHandler<EventArgs<string, List<TradingService.Order>>> OrdersUpdated;
        internal event EventHandler<EventArgs<string, List<Position>>> PositionsChanged;
        internal event EventHandler<EventArgs<string, Position>> PositionUpdated;
        internal event EventHandler<EventArgs<Order, string>> OrderRejected;
        internal event EventHandler<EventArgs<string, Order>> NewHistoricalOrder;

        internal event EventHandler<EventArgs<ScriptingReceivedEventArgs>> ScriptingListReceived;
        internal event EventHandler<EventArgs<string, Indicator>> IndicatorInstanceAdded;
        internal event EventHandler<EventArgs<string, Signal>> SignalInstanceAdded;
        internal event EventHandler<EventArgs<Signal>> WorkingSignalInstanceReceived;
        internal event EventHandler<EventArgs<List<SeriesForUpdate>>> SeriesUpdated;
        internal event EventHandler<ScriptingDLLs> ScriptingDLLsReceived;

        internal event EventHandler<EventArgs<ScriptingSavedEventArgs>> ScriptingSaved;
        internal event EventHandler<EventArgs<Dictionary<string, byte[]>>> SignalFilesReceived;
        internal event EventHandler<EventArgs<string>> ScriptingMessage;
        internal event EventHandler<EventArgs<string, List<string>>> ScriptingAlert;
        internal event EventHandler<EventArgs<SignalActionResponse>> SignalActionSet;
        internal event EventHandler<EventArgs<BacktestReportMessage>> BacktestProgressUpdated;
        internal event EventHandler<EventArgs<string, ScriptingType>> ScriptingExit;
        internal event EventHandler<EventArgs<string, ScriptingType>> ScriptingUnloaded;
        internal event EventHandler<ScriptingLogEventArgs> ScriptingLog;
        internal event EventHandler<ReportEventArgs> ScriptingReport;

        internal event Action<ConnectionState, string> ConnectionStatusChanged;

        #endregion

        #region Properties

        private Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private Binding Binding { get; set; }
        private ServiceCallback ServiceCallback { get; set; }
        private WCFConnectionClient WcfClient { get; set; }
        private InstanceContext WcfContext { get; set; }
        private EndpointAddress WcfAddress { get; set; }

        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.Disconnect;
        private ConnectionSettings ConnectionSettings { get; } = new ConnectionSettings();

        #endregion //Properties

        #region Initialize/Deinitialize

        private void Initialize()
        {
            Logger.Info(new string('-', 42));
            ServiceCallback = new ServiceCallback(this);
            WcfContext = new InstanceContext(ServiceCallback);
            Binding = new NetTcpBinding("NetTcpBinding_IWCFConnection");
            WcfAddress = new EndpointAddress($"net.tcp://{ConnectionSettings.Host}:{ConnectionSettings.Port}/TradingService");
            WcfClient = new WCFConnectionClient(WcfContext, Binding, WcfAddress);
        }

        private void Deinitialize()
        {
            Logger.Trace("Deinitializing connector");
            StopHeartbeat();

            DisposeCommunicationObject(WcfClient, TimeSpan.FromSeconds(0));
            WcfClient = null;
            DisposeCommunicationObject(WcfContext, TimeSpan.FromSeconds(5));
            WcfContext = null;

            ServiceCallback?.Dispose();
            ServiceCallback = null;
        }

        private void DisposeCommunicationObject(ICommunicationObject communicationObject, TimeSpan closeTimeOut)
        {
            try
            {
                if (communicationObject != null)
                {
                    if (communicationObject.State != CommunicationState.Faulted)
                        communicationObject.Close(closeTimeOut);
                    else
                        communicationObject.Abort();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Deinitialize");
            }
        }

        #endregion //Initialize/Deinitialize

        #region Login/Logout

        public string Login(string userName, string password, string host, int port)
        {
            ConnectionSettings.User = userName;
            ConnectionSettings.Password = password;
            ConnectionSettings.Host = host;
            ConnectionSettings.Port = port;

            Logger.Trace("Start Login");
            Logger.Info($"User {userName} try to login");

            Deinitialize();
            Initialize();

            var loginRequest = new LoginRequest
            {
                Login = userName,
                Password = password
            };

            var loginFailedText = "Login failed: ";

            try
            {
                WcfClient.Login(loginRequest);
            }
            catch (FaultException<TradingServerException> ex)
            {
                Logger.Error(ex, loginFailedText + ex.Detail.Reason);
                return ex.Detail.Reason;
            }
            catch (EndpointNotFoundException ex)
            {
                Logger.Error(ex, loginFailedText + ex.Message);
                return ex.Message;
            }
            catch (CommunicationException ex)
            {
                Logger.Error(ex, loginFailedText + ex.Message);
                return ex.Message;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, loginFailedText + ex.Message);
                return loginFailedText + "Internal error";
            }

            Logger.Info($"User {userName} logged in");

            SetConnectionStatus(ConnectionState.Connect, string.Empty);
            StartHeartbeat();
            

            return string.Empty;
        }

        public void Logout(ConnectionState status = ConnectionState.Disconnect, string reason = "")
        {
            if (WcfClient?.State == CommunicationState.Opened && ConnectionStatus == ConnectionState.Connect)
            {
                try
                {
                    WcfClient.LogOut();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, string.Empty);
                }
            }

            Deinitialize();
            SetConnectionStatus(status, reason);
        }

        public string ReLogin() =>
            Login(ConnectionSettings.User, ConnectionSettings.Password, ConnectionSettings.Host, ConnectionSettings.Port);

        public void UpdateUserInfoAfterReLogin() =>
            WcfClient.MessageIn(new UpdateUserInfoRequest());

        #endregion

        #region Heartbeat

        private void StartHeartbeat()
        {
            _heartbeatThread = new Thread(HeartbeatMethod) { IsBackground = true };
            _heartbeatThread.Start();
        }

        private void StopHeartbeat()
        {
            if (_heartbeatThread?.IsAlive == true)
            {
                _heartbeatThread.Join();
                _heartbeatThread = null;
            }
        }

        private void HeartbeatMethod()
        {
            while (true)
            {
                Task.Run(() => Send(new HeartbeatRequest()));
                Thread.Sleep(_sendHeartbeatInterval);
                if (ConnectionStatus != ConnectionState.Connect)
                    return;
            }
        }

        #endregion //Heartbeat

        #region Helper Methods

        private void SetConnectionStatus(ConnectionState status, string reason)
        {
            if (status == ConnectionStatus)
                return;

            ConnectionStatus = status;
            if (status == ConnectionState.Connect)
                Logger.Info("Connection status changed: " + status);
            else
            {
                Logger.Warn($"Connection status changed: {status} (reason: {(string.IsNullOrWhiteSpace(reason) ? "not specified" : reason)})");
            }

            ConnectionStatusChanged?.Invoke(status, reason);
        }

        internal void Send(RequestMessage message)
        {
            if (ConnectionStatus != ConnectionState.Connect)
                return;

            try
            {
                WcfClient.MessageIn(message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Send message error");
                if (WcfClient?.State != CommunicationState.Opened)
                    SetConnectionStatus(ConnectionState.LostConnection, "Connection lost");
            }
        }
        
        #endregion //Helper Methods

        #region Server Callbacks
              
        internal void OnBrokerLogin(BrokerLoginResponse message)
        {
            BrokerLogin?.Invoke(this, new EventArgs<string, List<AccountInfo>>(message.Error,
                message.Accounts.Select(p => p.ToClientAccount()).ToList()));
        }

        internal void OnDataFeedList(GetDataFeedListResponse message)
        {
            DataFeedsList?.Invoke(this, new EventArgs<List<TradingService.DataFeed>>(message.DataFeeds));
        }

        internal void OnBrokerLogout(BrokerLogoutResponse message)
        {
            BrokerLogout?.Invoke(this, new EventArgs<string>(message.Error));
        }

        internal void OnAvailableSecurities(BrokersAvailableSecuritiesResponse message)
        {
            AvailableSecurities?.Invoke(this, new EventArgs<string, List<Data.Contracts.Security>>(message.BrokerId,
                message.Securities.Select(DataConverter.ToClientSecurity).ToList()));
        }

        internal void OnPositionsChanged(PositionChangedResponse message)
        {
            PositionsChanged?.Invoke(this, new EventArgs<string, List<Position>>(message.AccountID,
                message.Positions.ToList().Select(p => p.ToClientPosition()).ToList()));
        }

        internal void OnPositionUpdated(PositionUpdatedResponse message)
        {
            PositionUpdated?.Invoke(this, new EventArgs<string, Position>(message.AccountID,
                message.Position.ToClientPosition()));
        }

        internal void OnTradingInfo(TradingInfoResponse message)
        {
            var brokers = message.Brokers.Select(b => new AvailableBrokerInfo(b.BrokerName, b.DataFeedName,b.Accounts, b.Url, (Data.Contracts.BrokerType)b.BrokerType)).ToList();
            BrokerList?.Invoke(this, new EventArgs<List<AvailableBrokerInfo>>(brokers));
            PortfolioList?.Invoke(this, new EventArgs<List<TradingService.Portfolio>>(message.Portfolios));
        }

        internal void OnCreateSimulatedBrokerAccount(CreateSimulatedBrokerAccountResponse message)
        {
            AddedNewBrokerAccount?.Invoke(this, new EventArgs<CreateSimulatedBrokerAccountInfo, string>(DataConverter.ToClientCreateSimulatedAccount(message.Account), message.Error));
        }

        internal void OnNewTick(TickDataResponse message) =>
            OnNewTicks?.Invoke(this, new EventArgs<List<Tick>>(message.Tick));

        internal void OnHistoricalData(HistoryDataResponse message) =>
            HistoricalData?.Invoke(this, new EventArgs<HistoryDataResponse>(message));

        internal void OnHeartbeat(HeartbeatResponse message)
        {
            switch (message.Text)
            {
                case DisconnectByServer:
                    Logout(ConnectionState.ServerDisconnected, "Disconnected by server side");
                    break;
                case DisconnectByAnotherUser:
                    Logout(ConnectionState.DisconnectedByAnotherUser, "Disconnected by server side. Reason : Another user connected with the same credentials");
                    break;
            }
        }

        internal void OnError(ErrorMessageResponse message)
        {
            Logger.Info("ErrorInfo message: " + message.Error);
            Error?.Invoke(this, new EventArgs<string>(message.Error));
        }

        internal void OnPortfolioAction(PortfolioActionResponse message)
        {
            PortfolioChanged?.Invoke(this, new EventArgs<PortfolioActionEventArgs>(new PortfolioActionEventArgs
            {
                PortfolioName = message.Portfolio.Name,
                Portfolio = message.Portfolio,
                Error = message.Error,
                IsRemoving = message.Action == PortfolioAction.Remove
            }));
        }

        internal void OnHistoricalOrder(HistoricalOrderResponse message)
        {
            if (NewHistoricalOrder == null)
                return;

            var order = message.HistoricalOrder;
            var status = order.FilledQuantity > 0 ? Status.Filled : Status.Canceled;
            var historicalOrder = new Order(order.UserID, order.Symbol, status == Status.Filled ? order.FilledQuantity : order.CancelledQuantity,
                DataConverter.ToClientOrderType(order.OrderType), order.OrderSide.ToClientSide(), order.Commission,
                status == Status.Filled ? order.AvgFillPrice : order.Price, order.OpenDate, DataConverter.ToClientTIF(order.TimeInForce))
            {
                OrderStatus = status,
                BrokerName = order.BrokerName,
                AccountId = order.AccountId,
                BrokerID = order.BrokerID,
                ServerSide = order.ServerSide,
                OpeningQty = order.OpeningQty,
                ClosingQty = order.ClosingQty
            };

            NewHistoricalOrder?.Invoke(this, new EventArgs<string, Order>(message.AccountID, historicalOrder));
        }

        internal void OnScriptingMessage(ScriptingMessageResponse message) =>
            ScriptingAlert?.Invoke(this, new EventArgs<string, List<string>>(message.Id, message.Message));

        internal void OnScripting(ScriptingResponse message)
        {
            if (ScriptingListReceived == null)
                return;

            var indicators = new Dictionary<string, List<ScriptingParameterBase>>();
            var signals = new List<Signal>();

            foreach (var indicator in message.Indicators.ToList())
                indicators.Add(indicator.Key, indicator.Value.Select(DataConverter.ToClientParameter).ToList());

            foreach (var sig in message.Signals ?? new List<TradingService.Signal>(0))
            {
                var working = message.WorkingSignals.FirstOrDefault(i => i.Name == sig.Name);
                signals.Add(DataConverter.ToClientSignal(working ?? sig));
            }

            ScriptingListReceived?.Invoke(this, new EventArgs<ScriptingReceivedEventArgs>(
                new ScriptingReceivedEventArgs(indicators, signals, message.DefaultIndicators)));

            //
            if (ScriptingDLLsReceived != null)
            {
                Task.Run(() =>
                {
                    ScriptingDLLsReceived?.Invoke(this, new ScriptingDLLs
                    {
                        CommonObjectsDllVersion = message.CommonObjectsDllVersion,
                        ScriptingDllVersion = message.ScriptingDllVersion,
                        BacktesterDllVersion = message.BacktesterDllVersion,
                        CommonObjectsDll = message.CommonObjectsDll,
                        ScriptingDll = message.ScriptingDll,
                        BacktesterDll = message.BacktesterDll
                    });
                });
            }

            foreach (var sig in message.WorkingSignals ?? new List<TradingService.Signal>(0))
                WorkingSignalInstanceReceived?.Invoke(this, new EventArgs<Signal>(DataConverter.ToClientSignal(sig)));

            Send(new SignalDataRequest());
        }

        internal void OnSignalData(SignalDataResponse response)
        {
            if (SignalFilesReceived != null)
            {
                Task.Run(() => SignalFilesReceived?.Invoke(this, new EventArgs<Dictionary<string, byte[]>>(response.Data)));
            }
        }

        internal void OnScriptingLog(ScriptingOutput response)
        {
            ScriptingLog?.Invoke(this, new ScriptingLogEventArgs
            {
                Value = new List<ScriptingLogData>(response.Outputs.Select(o => new ScriptingLogData(o.Message, o.WriterName, o.DateTime)))
            });
        }

        internal void OnScriptingReport(ScriptingReportResponse response)
        {
            ScriptingReport?.Invoke(this, new ReportEventArgs
            {
                Id = response.Id,
                ReportFields = new List<ReportField>(response.ReportFields.Select(s => new ReportField
                {
                    SignalName = s.SignalName,
                    OrderFilledDate = s.OrderFilledDate,
                    OrderGeneratedDate = s.OrderGeneratedDate,
                    Quantity = s.Quantity,
                    Side = ConvertSide(s.Side),
                    SignalGeneratedDateTime = s.SignalGeneratedDateTime,
                    Symbol = s.Symbol,
                    TimeInForce = ConvertTIF(s.TimeInForce),
                    OrderType = ConvertType(s.TradeType),
                    SignalToOrderSpan = s.SignalToOrderSpan,
                    OrderFillingDelay = s.OrderFillingDelay,
                    DBOrderEntryDate = s.DBOrderEntryDate,
                    DBSignalEntryDate = s.DBSignalEntryDate
                }))
            });
        }

        #region Converters

        private static Side ConvertSide(TradingService.Side side) => 
            side == TradingService.Side.Buy ? Side.Buy : Side.Sell;
        

        private static TimeInForce ConvertTIF(TradingService.TimeInForce tif)
        {
            switch (tif)
            {
                case TradingService.TimeInForce.FillOrKill:
                    return TimeInForce.FillOrKill;
                case TradingService.TimeInForce.ImmediateOrCancel:
                    return TimeInForce.ImmediateOrCancel;
                case TradingService.TimeInForce.GoodForDay:
                    return TimeInForce.GoodForDay;
                case TradingService.TimeInForce.GoodTilCancelled:
                    return TimeInForce.GoodTilCancelled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tif), tif, null);
            }
        }

        private static OrderType ConvertType(TradingService.OrderType type)
        {
            switch (type)
            {
                case TradingService.OrderType.Limit:
                    return OrderType.Limit;
                case TradingService.OrderType.Market:
                    return OrderType.Market;
                case TradingService.OrderType.Stop:
                    return OrderType.Stop;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        #endregion

        internal void OnScriptingExit(ScriptingExitResponse message)
        {
            ScriptingExit?.Invoke(this, new EventArgs<string, ScriptingType>(message.Id, message.ScriptingType));
        }

        internal void OnScriptingSaved(ScriptingDataSavedResponse message)
        {
            if (ScriptingSaved == null)
                return;

            if (string.IsNullOrEmpty(message.Error))
            {
                try
                {
                    ScriptingSaved(this, new EventArgs<ScriptingSavedEventArgs>(
                        new ScriptingSavedEventArgs(message.Parameters.Select(DataConverter.ToClientParameter),
                        message.Path, DataConverter.ToClientScriptingType(message.ScriptingType))));
                }
                catch (Exception ex) { Logger.Error(ex); }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(message.Error))
                    ScriptingMessage?.Invoke(this, new EventArgs<string>(message.Error));
            }
        }

        internal void OnScriptingDataRemove(ScriptingDataRemoveResponse message)
        {
            if (!string.IsNullOrWhiteSpace(message.Error))
                ScriptingMessage?.Invoke(this, new EventArgs<string>(message.Error));
        }

        internal void OnScriptingUnloaded(ScriptingInstanceUnloadedResponse message)
        {
            ScriptingUnloaded?.Invoke(this, new EventArgs<string, ScriptingType>(message.Name, message.ScriptingType));
        }

        internal void OnSignalAction(SignalActionResponse message)
        {
            SignalActionSet?.Invoke(this, new EventArgs<SignalActionResponse>(message));
        }

        internal void OnBacktestUpdate(BacktestReportMessage message)
        {
            BacktestProgressUpdated?.Invoke(this, new EventArgs<BacktestReportMessage>(message));
        }

        internal void OnIndicatorCreated(ScriptingInstanceCreatedResponse message)
        {
            if (message.Script == null)
            {
                IndicatorInstanceAdded(this, new EventArgs<string, Indicator>(message.RequestID, null));
                return;
            }

            if (message.ScriptingType == ScriptingType.Indicator)
            {
                if (IndicatorInstanceAdded != null)
                {
                    if (!(message.Script is TradingService.Indicator indicatorBase))
                    {
                        IndicatorInstanceAdded(this, new EventArgs<string, Indicator>(message.RequestID, null));
                        return;
                    }

                    var indicator = new Indicator
                    {
                        Name = indicatorBase.Name,
                        ID = indicatorBase.ID,
                        DisplayName = indicatorBase.DisplayName,
                        Parameters = indicatorBase.Parameters.Select(DataConverter.ToClientParameter).ToList(),
                        IsOverlay = indicatorBase.IsOverlay,
                        Series = indicatorBase.Series.Select(DataConverter.ToClientSeries).ToList()
                    };

                    if (indicator.Name == "PL" && indicator.Parameters.Count > 0)
                        indicator.Parameters[indicator.Parameters.Count - 1].IsReadOnly = true;

                    IndicatorInstanceAdded(this, new EventArgs<string, Indicator>(message.RequestID, indicator));
                }
            }
            else if (message.ScriptingType == ScriptingType.Signal)
            {
                if (SignalInstanceAdded != null)
                {
                    var sig = message.Script as TradingService.Signal;
                    SignalInstanceAdded(this, new EventArgs<string, Signal>(message.RequestID,
                        DataConverter.ToClientSignal(sig)));
                }
            }
          
        }

        internal void OnIndicatorSeriesUpdated(IndicatorSeriesUpdatedResponse message)
        {
            SeriesUpdated?.Invoke(this, new EventArgs<List<SeriesForUpdate>>(message
                .Series.Select(DataConverter.ToClientSeries).ToList()));
        }

        #endregion

        #region OMS

        internal void OnAccountInfoUpdated(AccountInfoChangedResponse message)
        {
            if (AccountChanged == null)
                return;

            ThreadPool.QueueUserWorkItem(p => AccountChanged(this, new EventArgs<AccountInfo>(new AccountInfo
            {
                Balance = message.Account.Balance,
                Equity = message.Account.Equity,
                Margin = message.Account.Margin,
                Profit = message.Account.Profit,
                UserName = message.Account.UserName,
                Account = message.Account.Account,
                Currency = message.Account.Currency,
                ID = message.Account.ID,
                DataFeedName = message.Account.DataFeedName,
                Password = message.Account.Password,
                BrokerName = message.Account.BrokerName,
                Url = message.Account.Uri,
                BalanceDecimals = message.Account.BalanceDecimals
            })));

        }

        internal void OnHistoricalOrdersList(HistoricalOrdersListResponse message)
        {
            if (HistoricalOrdersAdded == null)
                return;

            var orders = new List<Order>();
            foreach (var order in message.HistoricalOrders)
            {
                try
                {
                    var status = order.FilledQuantity > 0 ? Status.Filled : Status.Canceled;
                    orders.Add(new Order(order.UserID, order.Symbol, status == Status.Filled ? order.FilledQuantity : order.CancelledQuantity,
                        DataConverter.ToClientOrderType(order.OrderType), order.OrderSide.ToClientSide(), order.Commission,
                        status == Status.Filled ? order.AvgFillPrice : order.Price, order.OpenDate, DataConverter.ToClientTIF(order.TimeInForce))
                    {
                        OrderStatus = status,
                        BrokerName = order.BrokerName,
                        AccountId = order.AccountId,
                        BrokerID = order.BrokerID,
                        OpeningQty = order.OpeningQty,
                        ClosingQty = order.ClosingQty,
                        ServerSide = order.ServerSide
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to parse historical order");
                }
            }

            HistoricalOrdersAdded(this, new EventArgs<List<Order>>(orders));
        }

        internal void OnOrdersUpdated(OrdersUpdatedResponse message)
        {
            OrdersUpdated?.Invoke(this, new EventArgs<string, List<TradingService.Order>>(message.AccountID,
                message.Orders.ToList()));
        }

        internal void OnOrdersChanged(OrdersChangedResponse message)
        {
            if (OrdersChanged == null)
                return;

            var orders = new List<Order>();
            foreach (var order in message.Orders)
            {
                if (order == null) continue;

                if (order.OrderType == TradingService.OrderType.Market || order.OpenQuantity > 0
                    || order.Quantity != order.FilledQuantity + order.CancelledQuantity)
                {
                    orders.Add(new Order(order.UserID, order.Symbol, order.Quantity, DataConverter.ToClientOrderType(order.OrderType),
                        order.OrderSide.ToClientSide(), order.Commission, order.Price == 0M ? order.AvgFillPrice : order.Price,
                        order.OpenDate, DataConverter.ToClientTIF(order.TimeInForce))
                    {
                        CurrentPrice = order.CurrentPrice,
                        SLOffset = order.SLOffset,
                        TPOffset = order.TPOffset,
                        ProfitPips = order.PipProfit,
                        Profit = order.Profit,
                        FilledQuantity = order.FilledQuantity,
                        BrokerName = order.BrokerName,
                        AccountId = order.AccountId,
                        BrokerID = order.BrokerID,
                        OpeningQty = order.OpeningQty,
                        ClosingQty = order.ClosingQty,
                        ServerSide = order.ServerSide
                    });
                }
            }

            OrdersChanged(this, new EventArgs<string, List<Order>>(message.AccountID, orders));
        }

        internal void OnOrderRejected(OrderRejectionResponse message)
        {
            if (OrderRejected == null)
                return;

            var o = message.Order;

            var order = new Order(o.UserID, o.Symbol, o.Quantity, DataConverter.ToClientOrderType(o.OrderType),
                o.OrderSide.ToClientSide(), o.Commission, o.AvgFillPrice, o.OpenDate,
                DataConverter.ToClientTIF(o.TimeInForce))
            {
                CurrentPrice = o.CurrentPrice,
                SLOffset = o.SLOffset,
                TPOffset = o.TPOffset,
                ProfitPips = o.PipProfit,
                Profit = o.Profit,
                FilledQuantity = o.FilledQuantity,
                BrokerName = o.BrokerName,
                AccountId = o.AccountId,
                BrokerID = o.BrokerID,
                ServerSide = o.ServerSide
            };

            OrderRejected(this, new EventArgs<Order, string>(order, message.Msg));
        }

        #endregion

     
    }
}