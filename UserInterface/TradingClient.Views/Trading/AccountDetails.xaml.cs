using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TradingClient.Data.Contracts;

namespace TradingClient.Views
{
    /// <summary>
    /// Interaction logic for AccountDetails.xaml
    /// </summary>
    public partial class AccountDetails : UserControl
    {

        #region DependencyProperty

        public static readonly DependencyProperty AccountsProperty = DependencyProperty.Register(nameof(Accounts), typeof(ObservableCollection<AccountInfo>), typeof(AccountDetails), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedAccountProperty = DependencyProperty.Register(nameof(SelectedAccount), typeof(AccountInfo), typeof(AccountDetails), new PropertyMetadata(null));

        #endregion //DependencyProperty

        public AccountDetails()
        {
            InitializeComponent();
        }

        public ObservableCollection<AccountInfo> Accounts
        {
            get => (ObservableCollection<AccountInfo>)GetValue(AccountsProperty);
            set => SetValue(AccountsProperty, value);
        }

        public AccountInfo SelectedAccount
        {
            get => (AccountInfo)GetValue(SelectedAccountProperty);
            set => SetValue(SelectedAccountProperty, value);
        }
    }

}
