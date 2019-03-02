using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using NLog;
using TradingClient.Core;
using TradingClient.ViewModels;
using MainView = TradingClient.Views.MainView;

namespace TradingClient
{
    public partial class App
    {
        #region Properties

        private Logger Logger { get; }
        private ApplicationCore Core { get; set; }

        #endregion // Fields

        #region Constructors

        public App()
        {
            Logger = NLog.LogManager.GetCurrentClassLogger();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
        }

        #endregion // Constructors

        #region Override

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            Core = new ApplicationCore();

            var vm = new MainViewModel(Core);
            MainWindow = new MainView { DataContext = vm };
            vm.View = MainWindow;

            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Core.UnLoad();
            base.OnExit(e);
        }

        #endregion // Override

        #region Event Handlers

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Logger.Fatal(args.ExceptionObject as Exception, "Unhandled Exception");
        }

        #endregion // Event Handlers
    }
}