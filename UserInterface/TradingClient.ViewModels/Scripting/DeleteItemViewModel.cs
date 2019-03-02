using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.Command;
using TradingClient.Common;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;

namespace TradingClient.ViewModels
{
    public class DeleteItemViewModel : ViewModelBase
    {
        private Action<string> _deleteFunction;

        public DeleteItemViewModel(IEnumerable<string> items, Action<string> deleteFunction,string title)
        {
            Items = new ObservableCollection<string>(items);
            _deleteFunction = deleteFunction;
            Title = title;

            DeleteCommand = new RelayCommand(DeleteCommanExecution);
            CloseCommand = new RelayCommand(() => { DialogResult = false; });
        }
        
        #region Properties

        public ObservableCollection<string> Items { get; private set; }

        public string SelectedItem { get; set; }

        public string Title { get; set; }

        public ICommand DeleteCommand { get; set; }

        public ICommand CloseCommand { get; set; }

        #endregion //Properties
        
        private void DeleteCommanExecution()
        {
            if (SelectedItem == null)
            {
                MessageBox.Show($"Please select item");
                return;
            }

            var dialogResult = MessageBox.Show($"Do you want to delete : {SelectedItem}",
                "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (dialogResult != MessageBoxResult.Yes)
                return;

            _deleteFunction.Invoke(SelectedItem);
            Items.Remove(SelectedItem);
            SelectedItem = null;
        }
    }
}