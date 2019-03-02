using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.Xml.Serialization;

namespace TradingClient.Data.Contracts
{
    public class BacktestResult : INotifyPropertyChanged
    {
        #region Members

        private string _signalFullName;
        private int _index;
        private int _slot;
        private bool _isAggregated;
        private int _numberOfTrades;
        private int _totalNumberOfPositions;
        private int _numberOfProfitablePositions;
        private int _numberOfLosingPositions;
        private double _totalProfit;
        private double _totalLoss;
        private double _percentProfit;
        private double _largestProfit;
        private double _largestLoss;
        private double _maximumDrawDown;
        private double _maximumDrawDownMonteCarlo;
        private double _compoundMonthlyROR;
        private double _standardDeviation;
        private double _standardDeviationAnnualized;
        private double _downsideDeviationMar10;
        private double _valueAddedMonthlyIndex;
        private double _sharpeRatio;
        private double _sortinoRatioMAR5;
        private double _annualizedSortinoRatioMAR5;
        private double _sterlingRatioMAR5;
        private double _calmarRatio;
        private double _riskRewardRatio;

        #endregion

        #region Properties

        public string SignalFullName
        {
            get => _signalFullName;
            set
            {
                if (_signalFullName != value)
                {
                    _signalFullName = value;
                    OnPropertyChanged("SignalFullName");
                }
            }
        }

        [CsvSerializeIgnoreAttribute]
        public string SignalName => _signalFullName?.Contains("\\") == true
            ? _signalFullName.Substring(_signalFullName.LastIndexOf('\\') + 1)
            : _signalFullName;

        [CsvSerializeIgnoreAttribute]
        public string Title => _isAggregated
            ? (SignalName + ".Test" + _index)
            : (Selection.Symbol + " " + Extentions.GetTimeFrameString(Selection.TimeFrame, Selection.Interval));

        public int Index
        {
            get => _index;
            set
            {
                if (_index != value)
                {
                    _index = value;
                    OnPropertyChanged("Index");
                }
            }
        }

        public int Slot
        {
            get => _slot;
            set
            {
                if (_slot != value)
                {
                    _slot = value;
                    OnPropertyChanged("Slot");
                }
            }
        }

        public bool IsAggregated
        {
            get => _isAggregated;
            set
            {
                if (_isAggregated != value)
                {
                    _isAggregated = value;
                    OnPropertyChanged("IsAggregated");
                }
            }
        }

        [CsvSerializeIgnoreAttribute]
        public bool IsNotAggregated => !_isAggregated;

        public int NumberOfTrades
        {
            get => _numberOfTrades;
            set
            {
                if (_numberOfTrades != value)
                {
                    _numberOfTrades = value;
                    OnPropertyChanged("NumberOfTrades");
                }
            }
        }

        public int TotalNumberOfPositions
        {
            get => _totalNumberOfPositions;
            set
            {
                if (_totalNumberOfPositions != value)
                {
                    _totalNumberOfPositions = value;
                    OnPropertyChanged("TotalNumberOfPositions");
                }
            }
        }

        public int NumberOfProfitablePositions
        {
            get => _numberOfProfitablePositions;
            set
            {
                if (_numberOfProfitablePositions != value)
                {
                    _numberOfProfitablePositions = value;
                    OnPropertyChanged("NumberOfProfitablePositions");
                }
            }
        }

        public int NumberOfLosingPositions
        {
            get => _numberOfLosingPositions;
            set
            {
                if (_numberOfLosingPositions != value)
                {
                    _numberOfLosingPositions = value;
                    OnPropertyChanged("NumberOfLosingPositions");
                }
            }
        }

        public double TotalProfit
        {
            get => _totalProfit;
            set
            {
                if (_totalProfit != value)
                {
                    _totalProfit = value;
                    OnPropertyChanged("TotalProfit");
                }
            }
        }

        public double TotalLoss
        {
            get => _totalLoss;
            set
            {
                if (_totalLoss != value)
                {
                    _totalLoss = value;
                    OnPropertyChanged("TotalLoss");
                }
            }
        }

        public double PercentProfit
        {
            get => _percentProfit;
            set
            {
                if (_percentProfit != value)
                {
                    _percentProfit = value;
                    OnPropertyChanged("PercentProfit");
                }
            }
        }

        public double LargestProfit
        {
            get => _largestProfit;
            set
            {
                if (_largestProfit != value)
                {
                    _largestProfit = value;
                    OnPropertyChanged("LargestProfit");
                }
            }
        }

        public double LargestLoss
        {
            get => _largestLoss;
            set
            {
                if (_largestLoss != value)
                {
                    _largestLoss = value;
                    OnPropertyChanged("LargestLoss");
                }
            }
        }

        public double MaximumDrawDown
        {
            get => _maximumDrawDown;
            set
            {
                if (_maximumDrawDown != value)
                {
                    _maximumDrawDown = value;
                    OnPropertyChanged("MaximumDrawDown");
                }
            }
        }

        public double MaximumDrawDownMonteCarlo
        {
            get => _maximumDrawDownMonteCarlo;
            set
            {
                if (_maximumDrawDownMonteCarlo != value)
                {
                    _maximumDrawDownMonteCarlo = value;
                    OnPropertyChanged("MaximumDrawDownMonteCarlo");
                }
            }
        }

        public double CompoundMonthlyROR
        {
            get => _compoundMonthlyROR;
            set
            {
                if (_compoundMonthlyROR != value)
                {
                    _compoundMonthlyROR = value;
                    OnPropertyChanged("CompoundMonthlyROR");
                }
            }
        }

        public double StandardDeviation
        {
            get => _standardDeviation;
            set
            {
                if (_standardDeviation != value)
                {
                    _standardDeviation = value;
                    OnPropertyChanged("StandardDeviation");
                }
            }
        }

        public double StandardDeviationAnnualized
        {
            get => _standardDeviationAnnualized;
            set
            {
                if (_standardDeviationAnnualized != value)
                {
                    _standardDeviationAnnualized = value;
                    OnPropertyChanged("StandardDeviationAnnualized");
                }
            }
        }

        public double DownsideDeviationMar10
        {
            get => _downsideDeviationMar10;
            set
            {
                if (_downsideDeviationMar10 != value)
                {
                    _downsideDeviationMar10 = value;
                    OnPropertyChanged("DownsideDeviationMar10");
                }
            }
        }

        public double ValueAddedMonthlyIndex
        {
            get => _valueAddedMonthlyIndex;
            set
            {
                if (_valueAddedMonthlyIndex != value)
                {
                    _valueAddedMonthlyIndex = value;
                    OnPropertyChanged("ValueAddedMonthlyIndex");
                }
            }
        }

        public double SharpeRatio
        {
            get => _sharpeRatio;
            set
            {
                if (_sharpeRatio != value)
                {
                    _sharpeRatio = value;
                    OnPropertyChanged("SharpeRatio");
                }
            }
        }

        public double SortinoRatioMAR5
        {
            get => _sortinoRatioMAR5;
            set
            {
                if (_sortinoRatioMAR5 != value)
                {
                    _sortinoRatioMAR5 = value;
                    OnPropertyChanged("SortinoRatioMAR5");
                }
            }
        }

        public double AnnualizedSortinoRatioMAR5
        {
            get => _annualizedSortinoRatioMAR5;
            set
            {
                if (_annualizedSortinoRatioMAR5 != value)
                {
                    _annualizedSortinoRatioMAR5 = value;
                    OnPropertyChanged("AnnualizedSortinoRatioMAR5");
                }
            }
        }

        public double SterlingRatioMAR5
        {
            get => _sterlingRatioMAR5;
            set
            {
                if (_sterlingRatioMAR5 != value)
                {
                    _sterlingRatioMAR5 = value;
                    OnPropertyChanged("SterlingRatioMAR5");
                }
            }
        }

        public double CalmarRatio
        {
            get => _calmarRatio;
            set
            {
                if (_calmarRatio != value)
                {
                    _calmarRatio = value;
                    OnPropertyChanged("CalmarRatio");
                }
            }
        }

        public double RiskRewardRatio
        {
            get => _riskRewardRatio;
            set
            {
                if (_riskRewardRatio != value)
                {
                    _riskRewardRatio = value;
                    OnPropertyChanged("RiskRewardRatio");
                }
            }
        }

        [CsvSerializeSub]
        public SignalSelection Selection { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        [CsvSerializeDictionary]
        public SerializableDictionary<string, string> Parameters { get; set; }

        [XmlIgnore]
        [CsvSerializeIgnoreAttribute]
        public byte[] CompressedTrades { get; set; }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public BacktestResult()
        {
            Parameters = new SerializableDictionary<string, string>();
        }

        public Dictionary<DateTime, decimal> GetTrades()
        {
            var result = new Dictionary<DateTime, decimal>();
            if (CompressedTrades == null || CompressedTrades.Length < 5)
                return result;

            try
            {
                string tradesString = null;
                using (var input = new System.IO.MemoryStream(CompressedTrades))
                using (var unzipped = new GZipStream(input, CompressionMode.Decompress))
                using (var output = new System.IO.MemoryStream())
                {
                    unzipped.CopyTo(output);
                    tradesString = System.Text.Encoding.UTF8.GetString(output.ToArray());
                }

                if (tradesString == null || tradesString.Length < 10)
                    return new Dictionary<DateTime, decimal>(0);

                int prevIdx = 0;
                int nextIdx = 0;
                string[] parts = null;
                while (nextIdx < tradesString.Length - 1)
                {
                    prevIdx = nextIdx;
                    nextIdx = tradesString.IndexOf(';', prevIdx) + 1;
                    if (nextIdx < 1)
                        break;

                    parts = tradesString.Substring(prevIdx, nextIdx - prevIdx - 1).Split('|');  //time|price|qty
                    if (parts.Length > 2)
                    {
                        var time = DateTime.ParseExact(parts[0], "yyyy-MM-dd HH:mm:ss.fff",
                            System.Globalization.CultureInfo.InvariantCulture);
                        result[time] = Decimal.Parse(parts[1]);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("Failed to unpack backtest trades: " + e.Message);
                return new Dictionary<DateTime, decimal>(0);
            }

            return result;
        }
    }
}