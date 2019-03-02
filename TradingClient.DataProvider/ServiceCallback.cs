using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.DataProvider.TradingService;

namespace TradingClient.DataProvider
{
    internal class ServiceCallback : IWCFConnectionCallback, IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private ServiceConnector _connector;

        public ServiceCallback(ServiceConnector referenceHolder)
        {
            _connector = referenceHolder;
        }

        public void Dispose()
        {
            _connector = null;
        }

        public void MessageOut(ResponseMessage message)
        {
            if (message is LoginResponse)
                throw new NotSupportedException();
            else if (message is GetDataFeedListResponse)
                _connector.OnDataFeedList((GetDataFeedListResponse)message);
            else if (message is TradingInfoResponse)
                _connector.OnTradingInfo((TradingInfoResponse)message);
            else if (message is BrokersAvailableSecuritiesResponse)
                _connector.OnAvailableSecurities((BrokersAvailableSecuritiesResponse)message);
            else if (message is BrokerLoginResponse)
                _connector.OnBrokerLogin((BrokerLoginResponse)message);
            else if (message is BrokerLogoutResponse)
                _connector.OnBrokerLogout((BrokerLogoutResponse)message);
            else if (message is PositionChangedResponse)
                _connector.OnPositionsChanged((PositionChangedResponse)message);
            else if (message is TickDataResponse)
                _connector.OnNewTick((TickDataResponse)message);
            else if (message is HistoryDataResponse)
                _connector.OnHistoricalData((HistoryDataResponse)message);
            else if (message is HeartbeatResponse)
                _connector.OnHeartbeat((HeartbeatResponse)message);

            #region Trading 

            else if (message is AccountInfoChangedResponse)
                _connector.OnAccountInfoUpdated((AccountInfoChangedResponse)message);
            else if (message is HistoricalOrdersListResponse)
                _connector.OnHistoricalOrdersList((HistoricalOrdersListResponse)message);
            else if (message is OrdersUpdatedResponse)
                _connector.OnOrdersUpdated((OrdersUpdatedResponse)message);
            else if (message is OrdersChangedResponse)
                _connector.OnOrdersChanged((OrdersChangedResponse)message);
            else if (message is OrderRejectionResponse)
                _connector.OnOrderRejected((OrderRejectionResponse)message);
            else if (message is PositionUpdatedResponse)
                _connector.OnPositionUpdated((PositionUpdatedResponse)message);
            else if (message is PortfolioActionResponse)
                _connector.OnPortfolioAction((PortfolioActionResponse)message);
            else if (message is HistoricalOrderResponse)
                _connector.OnHistoricalOrder((HistoricalOrderResponse)message);
            else if (message is CreateSimulatedBrokerAccountResponse)
                _connector.OnCreateSimulatedBrokerAccount((CreateSimulatedBrokerAccountResponse)message);
            else if (message is ErrorMessageResponse)
                _connector.OnError((ErrorMessageResponse)message);

            #endregion //Trading 

            #region Scripting

            else if (message is ScriptingMessageResponse)
                _connector.OnScriptingMessage((ScriptingMessageResponse)message);
            else if (message is ScriptingResponse)
                _connector.OnScripting((ScriptingResponse)message);
            else if (message is SignalDataResponse)
                _connector.OnSignalData((SignalDataResponse)message);
            else if (message is IndicatorSeriesUpdatedResponse)
                _connector.OnIndicatorSeriesUpdated((IndicatorSeriesUpdatedResponse)message);
            else if (message is ScriptingInstanceCreatedResponse)
                _connector.OnIndicatorCreated((ScriptingInstanceCreatedResponse)message);
            else if (message is ScriptingDataSavedResponse)
                _connector.OnScriptingSaved((ScriptingDataSavedResponse)message);
            else if (message is ScriptingDataRemoveResponse)
                _connector.OnScriptingDataRemove((ScriptingDataRemoveResponse)message);
            else if (message is ScriptingInstanceUnloadedResponse)
                _connector.OnScriptingUnloaded((ScriptingInstanceUnloadedResponse)message);
            else if (message is SignalActionResponse)
                _connector.OnSignalAction((SignalActionResponse)message);
            else if (message is BacktestReportMessage)
                _connector.OnBacktestUpdate((BacktestReportMessage)message);
            else if (message is ScriptingExitResponse)
                _connector.OnScriptingExit((ScriptingExitResponse)message);
            else if (message is ScriptingOutput)
                _connector.OnScriptingLog((ScriptingOutput)message);
            else if (message is ScriptingReportResponse)
                _connector.OnScriptingReport((ScriptingReportResponse)message);

            #endregion //Scripting
            else
                _logger.Warn("Unknown incoming message type: " + message.GetType().Name);
        }
    }
}
