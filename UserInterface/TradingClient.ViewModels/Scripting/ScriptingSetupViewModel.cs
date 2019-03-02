using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;
using AvalonWizard.Mvvm;
using GalaSoft.MvvmLight.Command;
using TradingClient.Interfaces;
using System.Linq;
using TradingClient.Common;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModels
{
    public class ScriptingSetupViewModel : ViewModelBase
    {

        #region Members

        private readonly ScriptingType _scriptingType;
        private ScriptingParameterTypes _emptyScriptingSelectedType = (ScriptingParameterTypes)22;

        #endregion //Members

        #region Constructor

        public ScriptingSetupViewModel(IApplicationCore c, ScriptingType type)
        {
            Core = c;
            _scriptingType = type;

            switch (_scriptingType)
            {
                case ScriptingType.Signal:
                    Settings = new SignalSettings();
                    break;
                case ScriptingType.Indicator:
                    Settings = new IndicatorSettings();
                    break;
            }

            Title = $"{Settings.ScriptType} Setup";

            OkCommand = new RelayCommand(OkCommandExecute);
            CancelCommand = new RelayCommand(CancelCommandExecute);
            RemoveParameterCommand = new RelayCommand<ScriptingParameter>(RemoveParameterExecute);
            AddSeriesCommand = new RelayCommand(AddSeriesCommandExecute);
            RemoveSeriesCommand = new RelayCommand<SeriesSettings>(RemoveSeriesCommandExecute);
        }

        #endregion //Constructor

        #region Properties

        private IApplicationCore Core { get; }

        public string Title { get; private set; }

        public IScriptingSettings Settings { get; set; }
        
        public ScriptingParameterTypes SelectedType
        {
            get => _emptyScriptingSelectedType;
            set
            {
                if (value != _emptyScriptingSelectedType)
                {
                    AddParameter(value);
                }
            }
        }

        #endregion //Properties

        #region Commands

        public ICommand OkCommand { get; private set; }

        public ICommand CancelCommand { get; private set; }

        public ICommand RemoveParameterCommand { get; private set; }

        public ICommand AddSeriesCommand { get; private set; }

        public ICommand RemoveSeriesCommand { get; private set; }

        #endregion //Commands

        #region Commands Execution

        private void OkCommandExecute()
        {
            var error = ValidateParameters();
            if(!string.IsNullOrEmpty(error))
            {
                Core.ViewFactory.ShowMessage(error);
                return;
            }
            GenerateScript();

            DialogResult = true;
        }

        private void CancelCommandExecute() =>
            DialogResult = false;

        private void RemoveParameterExecute(ScriptingParameter property)
        {
            Settings.Parameters.Remove(property);
            Settings.RefreshParameters();
        }

        private void AddSeriesCommandExecute()
        {
            if(Settings is IndicatorSettings indicatorSettings)
            {
                indicatorSettings.Series.Add(new SeriesSettings());
                indicatorSettings.RefreshSeries();
            }
        }

        private void RemoveSeriesCommandExecute(SeriesSettings series)
        {
            if (Settings is IndicatorSettings indicatorSettings)
            {
                indicatorSettings.Series.Remove(series);
                indicatorSettings.RefreshSeries();
            }
        }

        #endregion //Commands Execution

        #region Private methods

        private void AddParameter(ScriptingParameterTypes type)
        {
            object defaultValue = null;
            
            switch (type)
            {
                case ScriptingParameterTypes.Int:
                    defaultValue = 0;
                    break;
                case ScriptingParameterTypes.Double:
                    defaultValue = 0.0;
                    break;
                case ScriptingParameterTypes.String:
                    defaultValue = string.Empty;
                    break;
                case ScriptingParameterTypes.Bool:
                    defaultValue = true;
                    break;
            }

            Settings.Parameters.Add(new ScriptingParameter(type, value: defaultValue));
            Settings.RefreshParameters();
        }

        private string ValidateParameters()
        {
            var names = new List<string>();
            var solutionNames = new List<string>();
            switch (_scriptingType)
            {
                case ScriptingType.Indicator:
                    names.AddRange(Core.DataManager.ScriptingManager.Indicators.Select(p => p.Key));
                    solutionNames.AddRange(Core.DataManager.ScriptingManager.GetScriptNamesFromDirectory(Core.PathManager.IndicatorsDirectory));
                    break;
                case ScriptingType.Signal:
                    names.AddRange(Core.DataManager.ScriptingManager.Signals.Select(p => p.Name));
                    solutionNames.AddRange(Core.DataManager.ScriptingManager.GetScriptNamesFromDirectory(Core.PathManager.SignalsDirectory));
                    break;
                default:
                    break;
            }

            return Settings.ValidateSettings(names, solutionNames);
        }

        private void GenerateScript()
        {
            string error;

            if (Settings is IndicatorSettings)
                error = Core.ScriptingGenerator.CreateIndicator(Core.PathManager.IndicatorsDirectory, Settings as IndicatorSettings);
            else if (Settings is SignalSettings)
                error = Core.ScriptingGenerator.CreateSignal(Core.PathManager.SignalsDirectory, Settings as SignalSettings);
            else
                error = "Invalid settings type: " + Settings;

            if (!string.IsNullOrEmpty(error))
                Core.ViewFactory.ShowMessage(error);
        }

        #endregion //Private methods

    }

}
