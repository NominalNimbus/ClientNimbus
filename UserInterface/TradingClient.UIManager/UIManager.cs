using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Interfaces;
using TradingClient.ViewModels;
using TradingClient.Views;

namespace TradingClient.UIManager
{
    public class UIManager : IUIManager
    {
        public UIManager()
        {
            ViewFactory = new ViewFactory();
        }

        public IViewFactory ViewFactory { get; }

        public void RegisterViews()
        {
            ViewFactory.RegisterView(typeof(LoginViewModel), typeof(LoginView));
            ViewFactory.RegisterView(typeof(BrokerLoginViewModel), typeof(BrokerLoginView));
            ViewFactory.RegisterView(typeof(ReconnectViewModel), typeof(ReconnectView));
            ViewFactory.RegisterView(typeof(PlaceOrderViewModel), typeof(PlaceOrderView));
            ViewFactory.RegisterView(typeof(ScriptingSetupViewModel), typeof(ScriptingSetupView));
            ViewFactory.RegisterView(typeof(DeleteItemViewModel), typeof(DeleteItemView));
            ViewFactory.RegisterView(typeof(AlertViewModel), typeof(AlertBaseView));
            ViewFactory.RegisterView(typeof(SignalOutputViewModel), typeof(AlertBaseView));
            ViewFactory.RegisterView(typeof(SelectBrokerAccountViewModel), typeof(SelectBrokerAccountView));
            ViewFactory.RegisterView(typeof(PortfolioDetailsViewModel), typeof(PortfolioAccountsView));
            ViewFactory.RegisterView(typeof(SelectSignalViewModel), typeof(SelectSignalView));
            ViewFactory.RegisterView(typeof(EditStringViewModel), typeof(EditStringView));
            ViewFactory.RegisterView(typeof(CheckableDialogViewModel), typeof(CheckableDialogView));
            ViewFactory.RegisterView(typeof(ShowScriptingReportViewModel), typeof(ShowScriptingReportView));
            ViewFactory.RegisterView(typeof(CreateSimulatedAccountViewModel), typeof(CreateSimulatedAccountView));
        }

    }
}
