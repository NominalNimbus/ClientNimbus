using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace TradingClient.Data.Contracts
{
    public class Signal : INotifyPropertyChanged
    {
        private string _fullName;
        private State _state;
        private SignalBacktestSettings _btSettings;
        private float _btProgress;
        private bool _isBtMode;

        public event PropertyChangedEventHandler PropertyChanged;

        #region Properties

        public string Name => string.IsNullOrWhiteSpace(_fullName)
            ? _fullName : _fullName.Substring(_fullName.LastIndexOf('\\') + 1);

        public string FullName
        {
            get => _fullName;
            set
            {
                if (value != _fullName)
                {
                    _fullName = value;
                    OnPropertyChanged("FullName");
                }
            }
        }

        public Strategy Parent { get; set; }

        public List<ScriptingParameterBase> Parameters { get; set; }

        public List<SignalSelection> Selections { get; set; }

        public ObservableCollection<SignalInstruments> GroupedInstruments
        {
            get
            {
                var feeds = Selections.Select(i => i.DataFeed).Distinct().ToArray();
                var result = new ObservableCollection<SignalInstruments>();
                foreach (var feed in feeds)
                {
                    var item = new SignalInstruments
                    {
                        DataFeedName = feed,
                        Instruments = new ObservableCollection<SignalSymbol>()
                    };

                    foreach (var instrument in Selections.Where(i => i.DataFeed == feed).GroupBy(i => i.Symbol))
                    {
                        item.Instruments.Add(new SignalSymbol
                        {
                            Symbol = instrument.Key,
                            SymbolsWithTimeframes = new ObservableCollection<string>(instrument
                                .Select(i => $"Slot {i.MarketDataSlot}: {i.Symbol}.{Extentions.GetTimeFrameString(i.TimeFrame, i.Interval)}"))
                        });
                    }
                    result.Add(item);
                }
                return result;
            }
        }

        public SignalBacktestSettings BacktestSettings
        {
            get => _btSettings;
            set
            {
                if (_btSettings != value)
                {
                    _btSettings = value;
                    OnPropertyChanged("BacktestSettings");
                }
            }
        }

        public bool IsBacktesting => _state == State.Backtesting || _state == State.BacktestPaused;

        public float BacktestProgress
        {
            get => _btProgress;
            set
            {
                if (value != _btProgress)
                {
                    _btProgress = value;
                    OnPropertyChanged("BacktestProgress");
                }
            }
        }

        public ObservableCollection<BacktestResult> BacktestResults { get; set; }

        public bool IsInBacktestMode
        {
            get => _isBtMode;
            set
            {
                if (value != _isBtMode)
                {
                    _isBtMode = value;
                    OnPropertyChanged("IsInBacktestMode");
                }
            }
        }

        public State State
        {
            get => _state;
            set
            {
                if (value != _state)
                {
                    _state = value;
                    OnPropertyChanged("State");
                    OnPropertyChanged("IsBacktesting");
                    if (_state != State.Backtesting && _state != State.BacktestPaused && _btProgress != 0F)
                        BacktestProgress = 0F;
                    if (_state != State.Stopped && _state != State.New)
                        IsInBacktestMode = IsBacktesting;
                }
            }
        }

        public bool IsDeployed => _state != State.New;

        public bool IsParamSpaceSet
        {
            get
            {
                if (NumericParamsCount == 0)
                    return true;

                foreach (var p in Parameters)
                {
                    if (p is IntParam)
                    {
                        var i = (IntParam)p;
                        if (i.StartValue != 0 || i.Step != 0 || i.StopValue != 0)
                            return true;
                    }
                    else if (p is DoubleParam)
                    {
                        var d = (DoubleParam)p;
                        if (d.StartValue != 0.0 || d.Step != 0.0 || d.StopValue != 0.0)
                            return true;
                    }
                }

                return false;
            }
        }

        public bool IsDefaultParamSpaceUsed
        {
            get
            {
                if (NumericParamsCount == 0)
                    return false;

                foreach (var p in Parameters)
                {
                    if (p is IntParam)
                    {
                        var i = (IntParam)p;
                        if (i.StartValue == i.StopValue && i.Step == i.StartValue)
                            return true;
                    }
                    else if (p is DoubleParam)
                    {
                        var d = (DoubleParam)p;
                        if (d.StartValue == d.StopValue && d.Step == d.StartValue)
                            return true;
                    }
                }

                return false;
            }
        }

        public int NumericParamsCount
        {
            get
            {
                return Parameters != null ? Parameters.Count(p => p is IntParam || p is DoubleParam) : 0;
            }
        }

        public int NumberOfParamCombinations
        {
            get
            {
                if (ValidateParamSpace() != null)
                    return 0;

                var paramSpaces = new List<List<double>>();
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (Parameters[i] is IntParam)
                    {
                        paramSpaces.Add(new List<double>());
                        var p = Parameters[i] as IntParam;
                        for (int j = p.StartValue; j <= p.StopValue; j += p.Step)
                        {
                            paramSpaces[paramSpaces.Count - 1].Add(j);
                            if (p.StartValue > p.StopValue || p.Step <= 0)
                                break;
                        }
                    }
                    else if (Parameters[i] is DoubleParam)
                    {
                        paramSpaces.Add(new List<double>());
                        var p = Parameters[i] as DoubleParam;
                        var val = p.StartValue;
                        do
                        {
                            paramSpaces[paramSpaces.Count - 1].Add(val);
                            val += p.Step;
                            if (p.StartValue > p.StopValue || p.Step <= 0.0)
                                break;
                        }
                        while (val <= p.StopValue);
                    }
                }

                var @params = Extentions.CartesianProduct(paramSpaces);
                return @params.Count() * Selections.Select(i => i.MarketDataSlot).Distinct().Count();
            }
        }

        #endregion

        public Signal()
        {
            Parameters = new List<ScriptingParameterBase>();
            Selections = new List<SignalSelection>();
            BacktestSettings = new SignalBacktestSettings();
            BacktestResults = new ObservableCollection<BacktestResult>();
        }

        /// <summary>
        /// Sets default values to parameter space values (start, step, stop)
        /// </summary>
        /// <param name="file">Optional: file to load parameter space values from</param>
        /// <remarks>
        /// Default parameter value would be an existing parameter value
        /// </remarks>
        public void SetDefaultParamSpace(string file = null)
        {
            //try to load default values from file
            var paramsFromFile = new Dictionary<string, decimal[]>(Parameters.Count);
            if (file != null && System.IO.File.Exists(file))
            {
                foreach (var line in System.IO.File.ReadAllLines(file))
                {
                    var p = line.Split(new[] { ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length == 4)
                    {
                        try
                        {
                            paramsFromFile[p[0]] = new[]
                            {
                                decimal.Parse(p[1]),
                                decimal.Parse(p[2]),
                                decimal.Parse(p[3])
                            };
                        }
                        catch { }
                    }
                }
            }

            foreach (var p in Parameters)
            {
                if (p is IntParam)
                {
                    var i = (IntParam)p;
                    //optional: ignore parameters that have already been set
                    if (i.StartValue == 0 && i.StopValue == 0)
                    {
                        if (paramsFromFile.ContainsKey(p.Name))
                        {
                            i.StartValue = (int)paramsFromFile[p.Name][0];
                            i.Step = (int)paramsFromFile[p.Name][1];
                            i.StopValue = (int)paramsFromFile[p.Name][2];
                        }
                        else
                        {
                            i.StartValue = i.Step = i.StopValue = i.Value;
                        }
                    }
                }
                else if (p is DoubleParam)
                {
                    var d = (DoubleParam)p;
                    //optional: ignore parameters that have already been set
                    if (d.StartValue == 0.0 && d.StopValue == 0.0)
                    {
                        if (paramsFromFile.ContainsKey(p.Name))
                        {
                            d.StartValue = (double)paramsFromFile[p.Name][0];
                            d.Step = (double)paramsFromFile[p.Name][1];
                            d.StopValue = (double)paramsFromFile[p.Name][2];
                        }
                        else
                        {
                            d.StartValue = d.Step = d.StopValue = d.Value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates parameter space values
        /// </summary>
        /// <returns>Error message or null if parameters look good</returns>
        public string ValidateParamSpace()
        {
            if (NumericParamsCount == 0)
                return "Signal doesn't have any parameters to be backtested";

            foreach (var p in Parameters)
            {
                double start;
                double step;
                double stop;

                if (p is IntParam)
                {
                    var i = (IntParam)p;
                    start = i.StartValue;
                    step = i.Step;
                    stop = i.StopValue;
                }
                else if (p is DoubleParam)
                {
                    var d = (DoubleParam)p;
                    start = d.StartValue;
                    step = d.Step;
                    stop = d.StopValue;
                }
                else
                {
                    continue;
                }

                if (start == 0 && stop == 0 && step == 0)
                    continue;  //optional: ignore/pass unset parameters 

                if ((start > stop) || (start <= stop && step <= 0.0))
                    return "Invalid parameter space values for " + p.Name + " parameter";
            }

            return null;
        }

        /// <summary>
        /// Save backtest results (if any) to a file
        /// </summary>
        /// <param name="file">File path</param>
        public void SaveBacktestResults(string file)
        {
            if (BacktestResults == null || BacktestResults.Count == 0)
                return;

            try
            {
                var dir = new FileInfo(file).Directory.FullName;
                Extentions.Serialize(BacktestResults.ToArray(), file);  //will skip the trades
                foreach (var item in BacktestResults)  //save trades to separate files
                {
                    if (item.IsAggregated || item.CompressedTrades == null)
                        continue;

                    var trades = item.GetTrades();
                    if (trades == null || trades.Count == 0)
                        continue;

                    string fileName = Path.Combine(dir, GetTradesFileName(item.Title, item.Index, "tsv"));
                    using (TextWriter writer = new StreamWriter(fileName, false))
                    {
                        foreach (var trade in trades)
                            writer.WriteLine(trade.Key.ToString("yyyyMMdd HH:mm:ss.fff") + "\t" + trade.Value);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.TraceError("Failed to save backtest results: " + e.Message);
            }
        }

        /// <summary>
        /// Load backtest results from file
        /// </summary>
        /// <param name="file">File path</param>
        public void LoadBacktestResults(string file)
        {
            //summary
            var loaded = Extentions.Deserialize<BacktestResult[]>(file);
            if (loaded == null || loaded.Length == 0)
                return;

            //trades
            var dir = new FileInfo(file).Directory.FullName;
            this.BacktestResults = new ObservableCollection<BacktestResult>(loaded);
            foreach (var item in loaded)
            {
                if (item.IsAggregated)
                    continue;

                var f = Path.Combine(dir, GetTradesFileName(item.Title, item.Index, "tsv"));
                if (!File.Exists(f))
                    continue;

                var sb = new System.Text.StringBuilder();
                using (TextReader reader = new StreamReader(f))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(new[] { '\t', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1)
                            sb.Append(string.Join("|", parts) + ";");
                    }
                }
                item.CompressedTrades = Extentions.Compress(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
            }
        }

        /// <summary>
        /// Export backtest result to csv file
        /// </summary>
        /// <param name="file">File path</param>
        public void ExportBacktestResultToCsv(string file)
        {
            if (BacktestResults == null || BacktestResults.Count == 0)
                return;

            Extentions.CsvSerialize(BacktestResults, file);
        }

        private static string GetTradesFileName(string selection, int index, string extension)
        {
            return $"{selection.Replace('/', '-')} ({index}).{extension}";
        }
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SignalInstruments
    {
        public string DataFeedName { get; set; }

        public ObservableCollection<SignalSymbol> Instruments { get; set; }
    }

    public class SignalSymbol
    {
        public string Symbol { get; set; }

        public ObservableCollection<string> SymbolsWithTimeframes { get; set; }
    }
}
