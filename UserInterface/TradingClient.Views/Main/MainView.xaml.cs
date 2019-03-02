using System;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void MainView_OnDeactivated(object sender, EventArgs e) =>
            Keyboard.ClearFocus();
    }
}