using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using TradingClient.Interfaces;
using MessageBox = Xceed.Wpf.Toolkit.MessageBox;

namespace TradingClient.UIManager
{
    public class ViewFactory : IViewFactory
    {
        #region Fields

        private readonly List<UIBinding> _viewModel2View;

        #endregion // Fields

        #region Properties

        private Dispatcher Dispatcher => Application.Current.Dispatcher;

        #endregion // Properties

        #region Event Declarations

        public event EventHandler<EventArgs<string, string>> DepthViewAdded;

        #endregion // Event Declarations

        #region Constructors

        public ViewFactory()
        {
            _viewModel2View = new List<UIBinding>();
        }

        #endregion // Constructors

        #region Private

        private Window GetActiveWindow()
        {
            var window = default(Window);

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => window = GetActiveWindow());
            }
            else
            {
                window = Application.Current.Windows.OfType<Window>().FirstOrDefault(i => i.IsActive);
                if (window == null && Application.Current.MainWindow != null)
                    window = Application.Current.MainWindow.IsVisible ? Application.Current.MainWindow : null;
            }
            
            return window;
        }

        #endregion // Private

        #region Public

        public void RegisterView(Type viewModel, Type view)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            if (view == null)
                throw new ArgumentNullException(nameof(view));

            _viewModel2View.Add(new UIBinding(viewModel, view));
        }

        public DlgResult ShowMessage(string message, string caption, MsgBoxButton buttons, MsgBoxIcon image)
        {
            var dialogResult = default(DlgResult);
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => 
                {
                    dialogResult = ShowMessage(message, caption, buttons, image);
                });
            }
            else
            {
                var window = GetActiveWindow();
                var buttons4Box = EnumHelper.GetButtons4Message(buttons);
                var icon = EnumHelper.GetIcon4Message(image);

                var result = MessageBox.Show(null, message, caption, buttons4Box, icon);
                dialogResult = EnumHelper.GetDialogResult(result);
            }

            return dialogResult;
        }

        public void ShowMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowMessage(message));
            }
            else
            {
                if (String.IsNullOrWhiteSpace(message))
                    return;

                var window = GetActiveWindow();
                if (window == null)
                    MessageBox.Show(message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show(window, message, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void AddSymbol2Depth(string symbol, string df)
            => DepthViewAdded?.Invoke(this, new EventArgs<string, string>(symbol, df));

        #endregion // Public

        #region IViewFactory

        public void ShowView(IViewModelBase viewModel, Action closeAction = null)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowView(viewModel, closeAction));
                return;
            }

            try
            {
                var viewType = _viewModel2View.Single(item => item.ViewModel == viewModel.GetType()).View;
                var view = (Window)Activator.CreateInstance(viewType);
                view.DataContext = viewModel;

                view.Closed += (sender, args) =>
                {
                    if (closeAction != null)
                        closeAction.Invoke();

                    viewModel.Dispose();
                };

                view.Show();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.ToString(), "Erro", MsgBoxButton.OK, MsgBoxIcon.Error);
            }
        }

        public bool? ShowDialogView(IViewModelBase viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            var result = default(bool?);
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => result = ShowDialogView(viewModel));
            }
            else
            {
                var viewType = _viewModel2View.Single(item => item.ViewModel == viewModel.GetType()).View;
                try
                {
                    var view = (Window)Activator.CreateInstance(viewType);
                    view.DataContext = viewModel;

                    view.Closed += (sender, e) => viewModel.Dispose();

                    result = view.ShowDialog();
                }
                catch (Exception ex)
                {
                    ShowMessage(ex.ToString(), "Erro", MsgBoxButton.OK, MsgBoxIcon.Error);
                }
            }

            return result;
        }

        #endregion // IViewFactory

        #region Application

        public void ExitApplication(int exitCode) => 
            BeginInvoke(()=> Application.Current.Shutdown(exitCode));
        
        public void BeginInvoke(Action action)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.BeginInvoke(action);
            else
                action();
        }

        public void Invoke(Action action)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.Invoke(action);
            else
                action();
        }

        #endregion // Application

        #region File Dialogs

        public string ShowSaveFileDialog(string filter = null, string initialDirectory = null, string name = null)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = filter,
                InitialDirectory = initialDirectory,
                FileName = name
            };

            return ShowFileDialog(saveDialog);
        }

        public string ShowOpenFileDialog(string filter = null, string initialDirectory = null)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                InitialDirectory = initialDirectory
            };

            return ShowFileDialog(dialog);
        }

        private string ShowFileDialog(FileDialog fileDialog)
        {
            if (fileDialog == null)
                throw new ArgumentNullException(nameof(fileDialog));

            var activeWindow = GetActiveWindow();
            var result = fileDialog.ShowDialog(activeWindow);
            return result == true ? fileDialog.FileName : null;
        }

        public string ShowFolderDialog(string path = null)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                dialog.SelectedPath = path;

            var dialogResult = dialog.ShowDialog();
            return dialogResult == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
        }

        #endregion // File Dialogs
    }
}