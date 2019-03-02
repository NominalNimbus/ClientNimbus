using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using TradingClient.Common;
using TradingClient.Interfaces;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class ScriptingLogViewModel : DocumentViewModel, IScriptingLogViewModel
    {
        #region Properties

        private IApplicationCore Core { get; }

        public override string Title => "Scripting Log";

        public override DocumentType DocumentType => DocumentType.ScriptingLog;

        public ObservableCollection<ScriptingLogItem> LogItems { get; private set; }

        public ICommand ClearCommand { get; private set; }

        #endregion

        public ScriptingLogViewModel(IApplicationCore core)
        {
            Core = core;
            Core.ScriptingLogManager.OnNewLogMessage += ScriptiongLogManagerOnLogMessage;

            LogItems = new ObservableCollection<ScriptingLogItem>();

            ClearCommand = new RelayCommand(() => LogItems.Clear(), () => LogItems.Count > 0);
        }

        private void ScriptiongLogManagerOnLogMessage(object sender, ScriptingMessageEventArgs args)
        {
            var messageDate = args.DateTime == DateTime.MinValue ? DateTime.Now : args.DateTime;
            var message = string.IsNullOrEmpty(args.Writer) ? args.Message : $"{args.Writer}: {args.Message}";
            var m = new ScriptingLogItem(messageDate, message);

            Core.ViewFactory.BeginInvoke(() => LogItems.Insert(0, m));
        }
    }

}