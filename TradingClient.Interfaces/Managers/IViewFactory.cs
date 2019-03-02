using System;

namespace TradingClient.Interfaces
{
    public interface IViewFactory
    {
        event EventHandler<EventArgs<string, string>> DepthViewAdded;
        
        void RegisterView(Type viewModel, Type view);

        void ShowView(IViewModelBase viewModel, Action action = null);

        bool? ShowDialogView(IViewModelBase viewModel);
        
        void ShowMessage(string message);

        DlgResult ShowMessage(string message, string caption, MsgBoxButton button, MsgBoxIcon image);
        
        string ShowSaveFileDialog(string filter = null, string initialDirectory = null, string name = null);

        string ShowOpenFileDialog(string filter = null, string initialDirectory = null);

        string ShowFolderDialog(string path = null);

        void BeginInvoke(Action action);

        void Invoke(Action action);

        void ExitApplication(int exitCode);
        
        void AddSymbol2Depth(string symbol, string df);
    }
}