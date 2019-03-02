using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using DS = TradingClient.DataProvider.TradingService;
using SignalState = TradingClient.Data.Contracts.State;
using ScriptingType = TradingClient.Data.Contracts.ScriptingType;
using Position = TradingClient.Data.Contracts.Position;
using AccountInfo = TradingClient.Data.Contracts.AccountInfo;
using Side = TradingClient.Data.Contracts.Side;
using TimeInForce = TradingClient.Data.Contracts.TimeInForce;
using OrderType = TradingClient.Data.Contracts.OrderType;
using TradingClient.Data.Contracts;

namespace TradingClient.DataProvider
{
    internal static class DataConverter
    {
        #region Server to Client

        #region Enum Converters

        internal static Side ToClientSide(this DS.Side side)
        {
            switch (side)
            {
                case DS.Side.Buy: return Side.Buy;
                case DS.Side.Sell: return Side.Sell;
                default: throw new InvalidEnumArgumentException("Unknown order side");
            }
        }

        internal static TimeInForce ToClientTIF(DS.TimeInForce tif)
        {
            switch (tif)
            {
                case DS.TimeInForce.FillOrKill: return TimeInForce.FillOrKill;
                case DS.TimeInForce.ImmediateOrCancel: return TimeInForce.ImmediateOrCancel;
                case DS.TimeInForce.GoodForDay: return TimeInForce.GoodForDay;
                case DS.TimeInForce.GoodTilCancelled: return TimeInForce.GoodTilCancelled;
                default: throw new InvalidEnumArgumentException("Unknown order TIF");
            }
        }

        internal static OrderType ToClientOrderType(DS.OrderType orderType)
        {
            switch (orderType)
            {
                case DS.OrderType.Limit: return OrderType.Limit;
                case DS.OrderType.Stop: return OrderType.Stop;
                case DS.OrderType.Market: return OrderType.Market;
                default: throw new InvalidEnumArgumentException("Unknown order type");
            }
        }

        internal static SignalState ToSignalState(DS.SignalState state)
        {
            switch (state)
            {
                case DS.SignalState.Stopped: return SignalState.Stopped;
                case DS.SignalState.Running: return SignalState.Working;
                case DS.SignalState.RunningSimulated: return SignalState.Paused;
                case DS.SignalState.Backtesting: return SignalState.Backtesting;
                case DS.SignalState.BacktestingPaused: return SignalState.BacktestPaused;
                default: throw new NotSupportedException($"Data Server signal state '{state}' is not supported");
            }
        }

        internal static ScriptingType ToClientScriptingType(DS.ScriptingType type)
        {
            switch (type)
            {
                case DS.ScriptingType.Indicator: return ScriptingType.Indicator;
                case DS.ScriptingType.Signal: return ScriptingType.Signal;
                default: throw new System.ComponentModel.InvalidEnumArgumentException(type.ToString());
            }
        }
        

        #endregion //Enum Converters
        
        #region Class Converters

        internal static Portfolio ToClientPortfolio(DS.Portfolio portfolio, IEnumerable<Signal> existingSignals)
        {
            if (portfolio == null)
                return null;

            var ret = new Portfolio
            {
                ID = portfolio.ID,
                User = portfolio.User,
                Name = portfolio.Name,
                BaseCurrency = portfolio.Currency,
                Accounts = new ObservableCollection<PortfolioAccount>(portfolio.Accounts.EmptyIfNull()
                    .Select(i => new PortfolioAccount
                    {
                        Name = i.Name,
                        BrokerName = i.BrokerName,
                        DataFeedName = i.DataFeedName,
                        Account = i.Account,
                        ID = i.ID,
                        UserName = i.UserName
                    })),
                Strategies = new ObservableCollection<Strategy>()
            };

            foreach (var s in portfolio.Strategies)
            {
                var strategy = new Strategy
                {
                    ID = s.ID,
                    Name = s.Name,
                    ExposedBalance = s.ExposedBalance,
                    Parent = ret,
                    Datafeeds = new ObservableCollection<string>(s.DataFeeds.EmptyIfNull()),
                    Signals = new ObservableCollection<Signal>()
                };

                foreach (var sig in s.Signals.EmptyIfNull())
                {
                    var fullName = $"{portfolio.Name}\\{s.Name}\\{sig}";
                    var existing = existingSignals?.FirstOrDefault(i => i.FullName == fullName);
                    strategy.Signals.Add(new Signal
                    {
                        FullName = fullName,
                        Parent = strategy,
                        State = existing != null ? existing.State : SignalState.New,
                        Selections = existing != null ? existing.Selections : new List<SignalSelection>(),
                        Parameters = existing != null ? existing.Parameters : new List<ScriptingParameterBase>()
                    });
                }

                ret.Strategies.Add(strategy);
            }

            return ret;
        }

        internal static Signal ToClientSignal(DS.Signal signal)
        {
            if (signal == null)
                return null;

            //var parameters = signal.Parameters.Select(ToClientParameter).ToList();
            //if (state == Common.Enums.Scripting.State.Working)
            //    parameters.ForEach(i => i.IsReadOnly = true);

            return new Signal
            {
                FullName = signal.Name,
                State = ToSignalState(signal.State),
                Parameters = new List<ScriptingParameterBase>(signal.Parameters.Select(ToClientParameter).ToList()),
                Selections = new List<SignalSelection>(signal.Selections
                    .Select(i => new SignalSelection(i.DataFeed, i.Symbol)
                    {
                        Symbol = i.Symbol,
                        BarCount = i.BarCount,
                        DataFeed = i.DataFeed,
                        MarketDataSlot = i.MarketDataSlot,
                        IsSimulated = signal.State == DS.SignalState.RunningSimulated,
                        TimeFrame = (TimeFrame)i.Timeframe,
                        Interval = i.TimeFactor
                    }))
            };
        }
        
        internal static ScriptingParameterBase ToClientParameter(DS.ScriptingParameterBase parameter)
        {
            if (parameter is DS.IntParam)
            {
                return new IntParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((DS.IntParam)parameter).Value,
                    MinValue = ((DS.IntParam)parameter).MinValue,
                    MaxValue = ((DS.IntParam)parameter).MaxValue,
                    StartValue = ((DS.IntParam)parameter).StartValue,
                    StopValue = ((DS.IntParam)parameter).StopValue,
                    Step = ((DS.IntParam)parameter).Step
                };
            }

            if (parameter is DS.DoubleParam)
            {
                return new DoubleParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((DS.DoubleParam)parameter).Value,
                    MinValue = ((DS.DoubleParam)parameter).MinValue,
                    MaxValue = ((DS.DoubleParam)parameter).MaxValue,
                    StartValue = ((DS.DoubleParam)parameter).StartValue,
                    StopValue = ((DS.DoubleParam)parameter).StopValue,
                    Step = ((DS.DoubleParam)parameter).Step
                };
            }

            if (parameter is DS.StringParam)
            {
                return new StringParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((DS.StringParam)parameter).Value,
                    AllowedValues = ((DS.StringParam)parameter).AllowedValues.ToList()
                };
            }

            if (parameter is DS.BoolParam)
            {
                return new BoolParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((DS.BoolParam)parameter).Value
                };
            }

            if (parameter is DS.SeriesParam)
            {
                return new SeriesParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID
                };
            }

            return null;
        }
        
        internal static Series ToClientSeries(DS.Series series)
        {
            var result = new Series(series.Name, series.ID);

            foreach (var value in series.Values)
                result.Values.Add(value.Date, value.Value);

            return result;
        }

        internal static SeriesForUpdate ToClientSeries(DS.SeriesForUpdate series)
        {
            return new SeriesForUpdate(series.SeriesID, series.IndicatorName, series.Values);
        }

        internal static SignalSelection ToClientSelection(DS.Selection selection)
        {
            if (selection == null)
                return null;

            return new SignalSelection(selection.DataFeed, selection.Symbol)
            {
                TimeFrame = (TimeFrame)selection.Timeframe,
                Interval = selection.TimeFactor,
                BarCount = selection.BarCount,
                Level = selection.Level,
                MarketDataSlot = selection.MarketDataSlot,
                Leverage = selection.Leverage,
                Slippage = selection.Slippage
            };
        }

        internal static Security ToClientSecurity(DS.Security security)
        {
            return new Security
            {
                Symbol = security.Symbol,
                Digits = security.Digit,
                Point = security.PriceIncrement,
                ContractSize = security.ContractSize,
                MarginRate = security.MarginRate,
                AssetClass = security.AssetClass,
                BaseCurrency = security.BaseCurrency,
                MarketClose = security.MarketClose,
                MarketOpen = security.MarketOpen,
                MaxPosition = security.MaxPosition,
                Name = security.Name,
                PriceIncrement = security.PriceIncrement,
                QtyIncrement = security.QtyIncrement,
                Id = security.SecurityId,
                UnitOfMeasure = security.UnitOfMeasure,
                UnitPrice = security.UnitPrice,
                DataFeed = security.DataFeed
            };
        }

        internal static List<BacktestResult> ToClientBacktestResults(DS.BacktestResults results)
        {
            if (results == null)
                return null;

            if (results.Summaries == null)
                results.Summaries = new List<DS.BacktestSummary>(0);

            var ret = new List<BacktestResult>(results.Summaries.Count + 1);

            //aggregated entry
            ret.Add(new BacktestResult
            {
                SignalFullName = results.SignalName,
                Slot = results.Slot,
                Index = results.Index,
                IsAggregated = true,
                StartDate = results.StartDate,
                EndDate = results.EndDate
            });
            if (results.Summaries.Count > 0)
            {
                ret[0].AnnualizedSortinoRatioMAR5 = results.Summaries.Average(i => i.AnnualizedSortinoRatioMAR5);
                ret[0].CalmarRatio = results.Summaries.Average(i => i.CalmarRatio);
                ret[0].CompoundMonthlyROR = results.Summaries.Average(i => i.CompoundMonthlyROR);
                ret[0].DownsideDeviationMar10 = results.Summaries.Average(i => i.DownsideDeviationMar10);
                ret[0].LargestLoss = results.Summaries.Max(i => Math.Abs(i.LargestLoss));
                ret[0].LargestProfit = results.Summaries.Max(i => i.LargestProfit);
                ret[0].MaximumDrawDown = results.Summaries.Max(i => i.MaximumDrawDown);
                ret[0].MaximumDrawDownMonteCarlo = results.Summaries.Max(i => i.MaximumDrawDownMonteCarlo);
                ret[0].NumberOfLosingPositions = results.Summaries.Sum(i => i.NumberOfLosingTrades);
                ret[0].NumberOfProfitablePositions = results.Summaries.Sum(i => i.NumberOfProfitableTrades);
                ret[0].PercentProfit = results.Summaries.Average(i => i.PercentProfit);
                ret[0].RiskRewardRatio = results.Summaries.Average(i => i.RiskRewardRatio);
                ret[0].SharpeRatio = results.Summaries.Average(i => i.SharpeRatio);
                ret[0].SortinoRatioMAR5 = results.Summaries.Average(i => i.SortinoRatioMAR5);
                ret[0].StandardDeviation = results.Summaries.Average(i => i.StandardDeviation);
                ret[0].StandardDeviationAnnualized = results.Summaries.Average(i => i.StandardDeviationAnnualized);
                ret[0].SterlingRatioMAR5 = results.Summaries.Average(i => i.SterlingRatioMAR5);
                ret[0].NumberOfTrades = results.Summaries.Sum(i => i.NumberOfTradeSignals);
                ret[0].TotalNumberOfPositions = results.Summaries.Sum(i => i.NumberOfTrades);
                ret[0].TotalLoss = results.Summaries.Sum(i => i.TotalLoss);
                ret[0].TotalProfit = results.Summaries.Sum(i => i.TotalProfit);
                ret[0].ValueAddedMonthlyIndex = results.Summaries.Average(i => i.ValueAddedMonthlyIndex);
            }
            foreach (var p in results.Parameters)
            {
                var parts = p.Split(new[] { '|' }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                    ret[0].Parameters.Add(parts[0], parts[1]);
            }

            //sub-entries
            foreach (var item in results.Summaries)
            {
                var summary = new BacktestResult
                {
                    SignalFullName = results.SignalName,
                    Index = results.Index,
                    Slot = results.Slot,
                    Selection = ToClientSelection(item.Selection),
                    StartDate = results.StartDate,
                    EndDate = results.EndDate,
                    AnnualizedSortinoRatioMAR5 = item.AnnualizedSortinoRatioMAR5,
                    CalmarRatio = item.CalmarRatio,
                    CompoundMonthlyROR = item.CompoundMonthlyROR,
                    DownsideDeviationMar10 = item.DownsideDeviationMar10,
                    LargestLoss = item.LargestLoss,
                    LargestProfit = item.LargestProfit,
                    MaximumDrawDown = item.MaximumDrawDown,
                    MaximumDrawDownMonteCarlo = item.MaximumDrawDownMonteCarlo,
                    NumberOfLosingPositions = item.NumberOfLosingTrades,
                    NumberOfProfitablePositions = item.NumberOfProfitableTrades,
                    PercentProfit = item.PercentProfit,
                    RiskRewardRatio = item.RiskRewardRatio,
                    SharpeRatio = item.SharpeRatio,
                    SortinoRatioMAR5 = item.SortinoRatioMAR5,
                    StandardDeviation = item.StandardDeviation,
                    StandardDeviationAnnualized = item.StandardDeviationAnnualized,
                    SterlingRatioMAR5 = item.SterlingRatioMAR5,
                    NumberOfTrades = item.NumberOfTradeSignals,
                    TotalNumberOfPositions = item.NumberOfTrades,
                    TotalLoss = item.TotalLoss,
                    TotalProfit = item.TotalProfit,
                    ValueAddedMonthlyIndex = item.ValueAddedMonthlyIndex,
                    CompressedTrades = item.TradesCompressed
                };

                foreach (var p in results.Parameters)
                {
                    var parts = p.Split(new[] { '|' }, 2, StringSplitOptions.None);
                    if (parts.Length == 2)
                        summary.Parameters.Add(parts[0], parts[1]);
                }

                ret.Add(summary);
            }

            return ret;
        }

        internal static Position ToClientPosition(this DS.Position pos)
        {
            return new Position(pos.Symbol)
            {
                Quantity = pos.Quantity,
                Side = pos.PositionSide.ToClientSide(),
                AvgOpenCost = pos.Price,//
                Profit = pos.Profit,
                ProfitPips = pos.PipProfit,
                CurrentPrice = pos.CurrentPrice,
                BrokerName = pos.BrokerName,
                AccountId = pos.AccountId,
                Margin = pos.Margin
            };
        }

        internal static AccountInfo ToClientAccount(this DS.AccountInfo account)
        {
            return new AccountInfo
            {
                UserName = account.UserName,
                ID = account.ID,
                DataFeedName = account.DataFeedName,
                BrokerName = account.BrokerName,
                Profit = account.Profit,
                Password = account.Password,
                Currency = account.Currency,
                Equity = account.Equity,
                Margin = account.Margin,
                Url = account.Uri,
                Balance = account.Balance,
                Account = account.Account,
                BalanceDecimals = account.BalanceDecimals,
                IsMarginAccount = account.IsMarginAccount
            };
        }

        #endregion //Class Converters

        #region Helper Methods

        private static bool IsSameInstruments(DS.Selection instrument1, DS.Selection instrument2)
        {
            if (instrument1 == null || instrument2 == null)
                return false;

            return instrument1.Symbol == instrument2.Symbol
                && instrument1.DataFeed == instrument2.DataFeed
                && instrument1.TimeFactor == instrument2.TimeFactor
                && instrument1.Timeframe == instrument2.Timeframe
                && instrument1.Level == instrument2.Level;
        }

        #endregion //Helper Methods

        #endregion

        #region Client to Server

        #region Enum Converters

        internal static DS.PriceType ToDsPriceType(PriceType type)
        {
            switch (type)
            {
                case PriceType.Mean: return DS.PriceType.Unspecified;
                case PriceType.Bid: return DS.PriceType.Bid;
                case PriceType.Ask: return DS.PriceType.Ask;
                default: throw new NotSupportedException($"Can't convert {type} to data server price type");
            }
        }

        internal static DS.SignalAction ToDsSignalAction(SignalAction action)
        {
            if (Enum.IsDefined(typeof(DS.SignalAction), (int)action))
                return (DS.SignalAction)action;
            else
                throw new NotSupportedException($"Signal action '{action}' is not supported on server side");
        }


        #endregion //Enum Converters

        #region Class Converters

        internal static DS.Portfolio ToServerPortfolio(Portfolio portfolio)
        {
            if (portfolio == null)
                return null;

            return new DS.Portfolio
            {
                ID = portfolio.ID,
                Name = portfolio.Name,
                User = portfolio.User,
                Currency = portfolio.BaseCurrency,
                Accounts = new List<DS.PortfolioAccount>(portfolio.Accounts.EmptyIfNull()
                    .Select(ToServerPortfolioAccount)),
                Strategies = new List<DS.PortfolioStrategy>(portfolio.Strategies.EmptyIfNull()
                    .Select(s => new DS.PortfolioStrategy
                    {
                        ID = s.ID,
                        Name = s.Name,
                        DataFeeds = s.Datafeeds.EmptyIfNull().ToList(),
                        Signals = s.Signals.EmptyIfNull().Select(i => i.Name).ToList(),
                        ExposedBalance = s.ExposedBalance
                    }))
            };
        }

        internal static DS.PortfolioAccount ToServerPortfolioAccount(PortfolioAccount account)
        {
            return new DS.PortfolioAccount
            {
                ID = account.ID,
                Name = account.Name,
                BrokerName = account.BrokerName,
                DataFeedName = account.DataFeedName,
                Account = account.Account,
                UserName = account.UserName
            };
        }

        internal static DS.ScriptingParameterBase ToDsScriptingParameters(ScriptingParameterBase parameter)
        {
            if (parameter is IntParam)
            {
                return new DS.IntParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((IntParam)parameter).Value,
                    MinValue = ((IntParam)parameter).MinValue,
                    MaxValue = ((IntParam)parameter).MaxValue,
                    StartValue = ((IntParam)parameter).StartValue,
                    StopValue = ((IntParam)parameter).StopValue,
                    Step = ((IntParam)parameter).Step
                };
            }

            if (parameter is DoubleParam)
            {
                return new DS.DoubleParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((DoubleParam)parameter).Value,
                    MinValue = ((DoubleParam)parameter).MinValue,
                    MaxValue = ((DoubleParam)parameter).MaxValue,
                    StartValue = ((DoubleParam)parameter).StartValue,
                    StopValue = ((DoubleParam)parameter).StopValue,
                    Step = ((DoubleParam)parameter).Step
                };
            }
            
            if (parameter is StringParam)
            {
                return new DS.StringParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((StringParam)parameter).Value,
                    AllowedValues = ((StringParam)parameter).AllowedValues.ToList()
                };
            }


            if (parameter is BoolParam)
            {
                return new DS.BoolParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID,
                    Value = ((BoolParam)parameter).Value
                };
            }

            if (parameter is SeriesParam)
            {
                return new DS.SeriesParam
                {
                    Name = parameter.Name,
                    Category = parameter.Category,
                    ID = parameter.ID
                };
            }

            return null;
        }

        internal static DS.Selection ToDsSelection(SignalSelection selection, bool? includeWeekendData = null)
        {
            return new DS.Selection
            {
                Symbol = selection.Symbol,
                DataFeed = selection.DataFeed,
                BarCount = selection.BarCount,
                IncludeWeekendData = includeWeekendData,
                Timeframe = (DS.Timeframe)selection.TimeFrame,
                TimeFactor = selection.Interval,
                MarketDataSlot = selection.MarketDataSlot,
                Level = selection.Level,
                Leverage = selection.Leverage,
                Slippage = selection.Slippage
            };
        }

        internal static DS.Selection ToDsSelection(IndicatorReqParams parameters)
        {
            return new DS.Selection
            {
                Symbol = parameters.Symbol,
                DataFeed = parameters.DataFeed,
                BarCount = parameters.BarCount,
                From = parameters.From,
                To = parameters.To,
                IncludeWeekendData = parameters.IncludeWeekendData,
                Timeframe = (DS.Timeframe)parameters.Timeframe,
                TimeFactor = parameters.Interval,
                BidAsk = (DS.PriceType)parameters.PriceType,
                Level = parameters.Level
            };
        }

    
        internal static DS.StrategyParams ToDsStrategyParams(StrategyParams parameters)
        {
            if (parameters == null)
                return null;

            return new DS.StrategyParams()
            {
                StrategyID = parameters.StrategyID,
                ExposedBalance = parameters.ExposedBalance
            };
        }

        internal static DS.BacktestSettings ToDsBacktestSettings(SignalBacktestSettings sigSettings, 
            StrategyBacktestSettings stratSettings)
        {
            if (sigSettings == null || stratSettings == null)
                return null;

            Dictionary<DS.Selection, List<DS.Bar>> barData = null;
            if (sigSettings.UseBarData && sigSettings.BarDataDirectory != null 
                && Directory.Exists(sigSettings.BarDataDirectory))
            {
                barData = new Dictionary<DS.Selection, List<DS.Bar>>();
                foreach (var file in Directory.GetFiles(sigSettings.BarDataDirectory, "*.csv"))
                {
                    var selection = ParseSelection(Path.GetFileNameWithoutExtension(file));
                    if (selection != null)
                    {
                        var bars = Extentions.ParseBarData(file);
                        if (bars != null && bars.Count > 1)
                            barData[selection] = bars.Select(ToDsBar).ToList();
                    }
                }
            }

            return new DS.BacktestSettings
            {
                StartDate = sigSettings.UseTimeInterval ? sigSettings.StartDate : DateTime.MinValue,
                EndDate = sigSettings.UseTimeInterval ? sigSettings.EndDate : DateTime.MinValue,
                BarsBack = sigSettings.UseBarCount ? sigSettings.BarsBack : 0,
                InitialBalance = stratSettings.InitialBalance,
                Risk = stratSettings.Risk,
                TransactionCosts = stratSettings.TransactionCosts,
                BarData = barData
            };
        }

        internal static DS.Bar ToDsBar(Bar bar)
        {
            return new DS.Bar
            {
                Date = bar.Timestamp,
                OpenBid = bar.OpenBid,
                OpenAsk = bar.OpenAsk,
                HighBid = bar.HighBid,
                HighAsk = bar.HighAsk,
                LowBid = bar.LowBid,
                LowAsk = bar.LowAsk,
                CloseBid = bar.CloseBid,
                CloseAsk = bar.CloseAsk,
                VolumeBid = bar.VolumeBid,
                VolumeAsk = bar.VolumeAsk
            };
        }

        internal static Bar ToClientBar(DS.Bar bar)
        {
            return new Bar
            {
                Timestamp = bar.Date,
                OpenBid = bar.OpenBid,
                OpenAsk = bar.OpenAsk,
                HighBid = bar.HighBid,
                HighAsk = bar.HighAsk,
                LowBid = bar.LowBid,
                LowAsk = bar.LowAsk,
                CloseBid = bar.CloseBid,
                CloseAsk = bar.CloseAsk,
                VolumeBid = (long)bar.VolumeBid,
                VolumeAsk = (long)bar.VolumeAsk
            };
        }

        internal static DS.CreateSimulatedBrokerAccountInfo ToDsCreateSimulatedAccount(CreateSimulatedBrokerAccountInfo account)
        {
            return new DS.CreateSimulatedBrokerAccountInfo
            {
                BrokerName = account.BrokerName,
                AccountName = account.AccountName,
                Currency = account.Currency,
                Ballance = account.Ballance
            };
        }

        internal static CreateSimulatedBrokerAccountInfo ToClientCreateSimulatedAccount(DS.CreateSimulatedBrokerAccountInfo account)
        {
            return new CreateSimulatedBrokerAccountInfo(account.BrokerName)
            {
                AccountName = account.AccountName,
                Currency = account.Currency,
                Ballance = account.Ballance
            };
        }

        private static DS.Selection ParseSelection(string selection)
        {
            if (String.IsNullOrWhiteSpace(selection))
                return null;

            //expected format example: EUR-USD M5 Ask
            selection = selection.ToUpper();
            var parts = selection.Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return null;

            DS.Timeframe tf;
            if (parts[1] == "MN" || parts[1] == "MONTH")
                tf = DS.Timeframe.Month;
            else if (parts[1][0] == 'D')
                tf = DS.Timeframe.Day;
            else if (parts[1][0] == 'H')
                tf = DS.Timeframe.Hour;
            else if (parts[1][0] == 'M')
                tf = DS.Timeframe.Minute;
            else if (parts[1][0] == 'T')
                tf = DS.Timeframe.Tick;
            else
                return null;

            int interval = 1;
            if (tf != DS.Timeframe.Month && parts[1].Length > 1)
                Int32.TryParse(parts[1].Substring(1), out interval);

            DS.PriceType type = DS.PriceType.Unspecified;
            if (parts.Length > 2)
            {
                if (parts[2] == "BID")
                    type = DS.PriceType.Bid;
                else if (parts[2] == "ASK")
                    type = DS.PriceType.Ask;
            }
            
            return new DS.Selection
            {
                Symbol = parts[0].Replace('-', '/'),
                Timeframe = tf,
                TimeFactor = interval,
                BidAsk = type
            };
        }

        internal static TickData ToClientTick(DS.Tick tick)
        {
            var level2 = new List<MarketLevel2>();

            if (tick.Level2 != null)
            {
                level2.AddRange(tick.Level2.ToList().Select(marketLevel2 => new MarketLevel2
                {
                    Ask = marketLevel2.AskPrice,
                    Bid = marketLevel2.BidPrice,
                    AskSize = (long)marketLevel2.AskSize,
                    BidSize = (long)marketLevel2.BidSize,
                    Level = marketLevel2.DomLevel,
                    DailyLevel2AskSize = marketLevel2.DailyLevel2AskSize,
                    DailyLevel2BidSize = marketLevel2.DailyLevel2BidSize,
                }));

                level2.Sort((a, b) => a.Level.CompareTo(b.Level));
            }

            return new TickData
            {
                Symbol = tick.Symbol.Symbol,
                DataFeed = tick.DataFeed,
                Time = tick.Date,
                Ask = tick.Ask,
                AskSize = tick.AskSize,
                Bid = tick.Bid,
                BidSize = tick.BidSize,
                Volume = (long)tick.Volume,
                LastPrice = tick.Price,
                Level2 = level2
            };
        }

        #endregion //Class Converters

        #endregion
    }
}
