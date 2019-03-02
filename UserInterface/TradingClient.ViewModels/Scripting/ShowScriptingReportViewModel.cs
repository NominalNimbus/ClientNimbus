using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;

namespace TradingClient.ViewModels
{
    public class ShowScriptingReportViewModel : ViewModelBase
    {

        #region Fields

        private bool _isBusy;
        private IEnumerable<string> _strategies;
        private string _selectedStrategy;
        private DateTime _fromTime = DateTime.Now.Date;
        private DateTime _toTime = DateTime.Now;
        private ObservableCollection<ReportField> _reportFields;
        private ICommand _getReportCommand;
        private ICommand _exportCommand;

        #endregion

        #region Properties

        private IApplicationCore Core { get; }
        
        public DateTime FromTime
        {
            get => _fromTime;
            set
            {
                _fromTime = value;
                OnPropertyChanged(nameof(FromTime));
            }
        }

        public DateTime ToTime
        {
            get => _toTime;
            set
            {
                _toTime = value;
                OnPropertyChanged(nameof(ToTime));
            }
        }

        public IEnumerable<string> Strategies
        {
            get => _strategies;
            set
            {
                _strategies = value;
                OnPropertyChanged(nameof(Strategies));
            }
        }

        public string SelectedStrategy
        {
            get => _selectedStrategy;
            set
            {
                _selectedStrategy = value;
                OnPropertyChanged(nameof(SelectedStrategy));
            }
        }

        public ObservableCollection<ReportField> ReportFields
        {
            get => _reportFields;
            set
            {
                _reportFields = value;
                OnPropertyChanged(nameof(ReportFields));
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        #endregion

        #region Commands

        public ICommand GetReportCommand => _getReportCommand ?? (_getReportCommand = new RelayCommand(() =>
        {
            Core.DataManager.ScriptingManager.GetReport(SelectedStrategy, FromTime, ToTime, ApplyReport);
            IsBusy = true;
        }));

        public ICommand ExportCommand => _exportCommand ?? (_exportCommand = new RelayCommand(async () =>
        {
            if (ReportFields == null || !ReportFields.Any())
                return;
            
            var path = Core.ViewFactory.ShowSaveFileDialog("CSV files (*.csv)|*.csv", Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Report");

            await Task.Run(() =>
            {
                var result = "SignalName,Symbol,Side,OrderType,Quantity,TimeInForce,Status,SignalGeneratedDateTime,OrderGeneratedDate,OrderFilledDate,SignalToOrderSpan,OrderFillingDelay"
                             + Environment.NewLine;
                foreach (var reportField in ReportFields)
                    result += $"{reportField.SignalName},{reportField.Symbol},{reportField.Side},{reportField.OrderType}," +
                              $"{reportField.Quantity},{reportField.TimeInForce},{reportField.Status},{reportField.SignalGeneratedDateTime:d/MM/yy HH:mm:ss.fff tt}," +
                              $"{reportField.OrderGeneratedDate:d/MM/yy HH:mm:ss.fff tt},{reportField.OrderFilledDate:d/MM/yy HH:mm:ss.fff tt},{reportField.SignalToOrderSpan}," +
                              $"{reportField.OrderFillingDelay}{Environment.NewLine}";

                try
                {
                    if (!string.IsNullOrEmpty(path))
                        File.WriteAllText(path, result);
                }
                catch (Exception ex)
                {
                    Core.ViewFactory.ShowMessage($"Error ocurred {ex.Message}", "Error", MsgBoxButton.OK,
                        MsgBoxIcon.Error);
                }
            });
        }));

        #endregion

        #region Constructor

        public ShowScriptingReportViewModel(IApplicationCore core, IEnumerable<string> strategies)
        {
            Core = core;
            Strategies = strategies;
            SelectedStrategy = Strategies.FirstOrDefault();
        }

        #endregion

        #region Private

        private void ApplyReport(IEnumerable<ReportField> reportFields)
        {
            ReportFields = new ObservableCollection<ReportField>(reportFields);
            IsBusy = false;
        }

        #endregion

    }
}