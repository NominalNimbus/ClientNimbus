using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NLog;
using TradingClient.DataProvider.TradingService;
using Bar = TradingClient.Data.Contracts.Bar;
using Security = TradingClient.Data.Contracts.Security;
using MarketLevel2 = TradingClient.Data.Contracts.MarketLevel2;
using TradingClient.Interfaces;
using TradingClient.Data.Contracts;

namespace TradingClient.DataProvider
{
    public class DataFeed
    {
        #region Memebers and Events

        private readonly ServiceConnector _serviceConnector;
        private readonly List<TradingService.DataFeed> _datafeeds = new List<TradingService.DataFeed>();
        private readonly List<Security> _securities = new List<Security>();
        private AutoResetEvent _initAutoResetEvent;
        
        private readonly Dictionary<Security, List<object>> _ticksSubscribers = new Dictionary<Security, List<object>>();
        private readonly Dictionary<Security, TickData> _lastTicks = new Dictionary<Security, TickData>();
        public event EventHandler<EventArgs<List<TickData>>> OnTicks;

        private readonly Dictionary<string, HistoryRequest> _historicalDataRequests = new Dictionary<string, HistoryRequest>();
        public event EventHandler<EventArgs<string, List<Bar>>> OnHistoricalData;

        #endregion //Memebers

        #region Constructor

        public DataFeed(ServiceConnector connector)
        {
            _serviceConnector = connector ?? throw new ArgumentNullException(nameof(connector));

            connector.OnNewTicks += ConnectorOnNewTicks;
            connector.HistoricalData += ConnectorOnHistoricalData;
            connector.Error += ConnectorOnError;
        }

        #endregion //Constructor

        #region Properties

        public bool IsOffline => 
            _datafeeds.All(p => !p.IsStarted);  

        internal List<Security> Securities
        {
            get
            {
                lock (_securities)
                    return _securities.ToList();
            }
        }

        internal List<string> Datafeeds =>
            _datafeeds.Select(feed => feed.Name).ToList();
        
        private Logger Logger => LogManager.GetCurrentClassLogger();

        #endregion //Properties

        #region Connector events

        private void ConnectorOnNewTicks(object sender, EventArgs<List<Tick>> args)
        {
            var ticks = args.Value.Where(tick => _datafeeds.Any(p => p.Name == tick.Symbol.DataFeed)).Select(DataConverter.ToClientTick).ToList();

            lock (_lastTicks)
            {
                foreach (var tick in ticks)
                {
                    var instrument = GetSecurityFromDataFeed(tick.Symbol, tick.DataFeed);
                    if (instrument == null)
                        continue;

                    TickData t;
                    if (!_lastTicks.TryGetValue(instrument, out t) || tick.Time >= t.Time)
                    {
                            _lastTicks[instrument] = tick;
                    }
                }
            }

            lock (_ticksSubscribers)
                ticks = ticks.Where(tick => _ticksSubscribers.Keys.Any(p => p.Symbol.Equals(tick.Symbol) && p.DataFeed.Equals(tick.DataFeed))).ToList();

            OnTicks?.Invoke(this, new EventArgs<List<TickData>>(ticks));
        }

        private void ConnectorOnHistoricalData(object sender, EventArgs<HistoryDataResponse> args)
        {
            HistoryRequest request;
            lock (_historicalDataRequests)
            {
                if (!_historicalDataRequests.TryGetValue(args.Value.ID, out request))
                    return;
            }

            request.Bars.AddRange(args.Value.Bars.Select(DataConverter.ToClientBar));

            if (!args.Value.Tail)
                return;

            lock (_historicalDataRequests)
                _historicalDataRequests.Remove(args.Value.ID);

            OnHistoricalData?.Invoke(this, new EventArgs<string, List<Bar>>(args.Value.ID,
                request.Bars.OrderBy(bar => bar.Timestamp).ToList()));
        }

        private void ConnectorOnError(object sender, EventArgs<string> args)
        {
            string requestId;
            lock (_historicalDataRequests)
            {
                requestId = _historicalDataRequests.Keys.FirstOrDefault(k => args.Value.Contains(k));
                if (requestId != null)
                    _historicalDataRequests.Remove(requestId);
            }

            if (string.IsNullOrEmpty(requestId))
                return;

            OnHistoricalData?.Invoke(this, new EventArgs<string, List<Bar>>(requestId, new List<Bar>()));
        }

        private void ConnectorOnDataFeeds(object sender, EventArgs<List<TradingService.DataFeed>> args)
        {
            _datafeeds.Clear();
            _datafeeds.AddRange(args.Value);

            if (_initAutoResetEvent == null)
            {
                return;
            }

            lock (_securities)
            {
                _securities.Clear();

                foreach (var datafeed in _datafeeds)
                    _securities.AddRange(datafeed.Symbols.Select(DataConverter.ToClientSecurity));
            }

            _initAutoResetEvent?.Set();
        }

        #endregion //Connector events

        #region Public Metmbers
        
        public void SubscribeTick(string symbol, string df, object subscriber)
        {
            lock (_ticksSubscribers)
            {
                var instrument = GetSecurityFromDataFeed(symbol, df);
                if (instrument == null)
                    return;

                if (!_ticksSubscribers.TryGetValue(instrument, out var instrumentSubscribers))
                {
                    instrumentSubscribers = new List<object> { subscriber };
                    _ticksSubscribers.Add(instrument, instrumentSubscribers);
                    SendSubscribeMessage(symbol, df);
                }
                else
                {
                    if (!instrumentSubscribers.Contains(subscriber))
                        instrumentSubscribers.Add(subscriber);
                }
            }
        }
        
        public void UnsubscribeTick(string symbol, string df, object subscriber)
        {
            lock (_ticksSubscribers)
            {
                var instrument = GetSecurityFromDataFeed(symbol, df);
                if (instrument == null || !_ticksSubscribers.TryGetValue(instrument, out var instrumentSubscribers))
                    return;

                instrumentSubscribers.Remove(subscriber);
                if(instrumentSubscribers.Count == 0)
                {
                    _ticksSubscribers.Remove(instrument);
                    _serviceConnector.Send(new UnsubscribeRequest { Symbol = GetServerSecurity(symbol, df) });
                }
            }
        }

        public void ResubscribeAll()
        {
            lock (_ticksSubscribers)
            {
                foreach (var instrument in _ticksSubscribers)
                    SendSubscribeMessage(instrument.Key.Symbol, instrument.Key.DataFeed);
            }
        }

        public void GetHistoricalData(string id, string symbol, string df, TimeFrame tf, int interval, 
            DateTime from, DateTime to, int bars, int level, bool? includeWeekendData)
        {
            var request = new HistoryRequest
            {
                From = from,
                To = to,
                BarsCount = bars,
                Interval = interval,
                Periodicity = (Timeframe)tf,
                Level = level,
                IncludeWeekendData = includeWeekendData
            };

            lock (_historicalDataRequests)
                _historicalDataRequests.Add(id, request);

            var historyRequest = new Selection
            {
                Id = id,
                Symbol = symbol.ToUpper(),
                DataFeed = df,
                BarCount = request.BarsCount,
                From = request.From,
                To = request.To,
                Timeframe = request.Periodicity,
                TimeFactor = request.Interval,
                Level = (byte)request.Level,
                IncludeWeekendData = includeWeekendData
            };

            _serviceConnector.Send(new TradingService.HistoryDataRequest { Selection = historyRequest });
        }

        public Security GetSecurityFromDataFeed(string symbol, string df)
        {
            lock (_securities)
                return _securities.FirstOrDefault(instrument => symbol.EqualsValue(instrument.Symbol) 
                    && instrument.DataFeed.Equals(df, StringComparison.InvariantCultureIgnoreCase));
        }

        public TickData GetLastTick(string symbol, string df)
        {
            lock (_lastTicks)
            {
                var instrument = GetSecurityFromDataFeed(symbol, df);
                if (instrument == null)
                    return null;

                if (_lastTicks.TryGetValue(instrument, out var quote))
                    return quote;
            }
            return null;
        }

        public List<string> GetDataFeedSymbols(string dataFeed)
        {
            var feed = _datafeeds.FirstOrDefault(i => i.Name.Equals(dataFeed, StringComparison.OrdinalIgnoreCase));
            return (feed != null && feed.Symbols.Any()) 
                ? feed.Symbols.Select(i => i.Symbol).OrderBy(i => i).ToList()
                : new List<string>(0);
        }

        #endregion //Public Metmbers

        #region Private members

        internal string SendInitializeRequests()
        {
            _initAutoResetEvent = new AutoResetEvent(false);

            _serviceConnector.DataFeedsList += ConnectorOnDataFeeds;
            _serviceConnector.Send(new GetDataFeedListRequest());
            _serviceConnector.Send(new ScriptingRequest());

            var res = _initAutoResetEvent.WaitOne(TimeSpan.FromSeconds(100));
            _initAutoResetEvent = null;

            _serviceConnector.DataFeedsList -= ConnectorOnDataFeeds;

            return res
                ? (_datafeeds.Any()? null :"Data feeds list is empty.")
                : "receiving data feeds from server timed out.";
        }
        
        private void SendSubscribeMessage(string symbol, string df) =>
            _serviceConnector.Send(new SubscribeRequest { Symbol = GetServerSecurity(symbol, df) });

        private TradingService.Security GetServerSecurity(string symbol, string df)
        {
            var datafeed = _datafeeds.FirstOrDefault(p => df.Equals(p.Name, StringComparison.OrdinalIgnoreCase));
            return datafeed?.Symbols.FirstOrDefault(p => p.Symbol.Equals(symbol));
        }

        #endregion //Private members
       
    }
}