using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace TradingClient.BaseStyles.Behaviours
{
    public class ColumnsBindingBehavior : Behavior<DataGrid>
    {
        private ObservableCollection<DataGridColumn> _dataGridColumns;

        public ObservableCollection<DataGridColumn> Columns
        {
            get { return (ObservableCollection<DataGridColumn>)base.GetValue(ColumnsProperty); }
            set { base.SetValue(ColumnsProperty, value); }
        }

        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns",
            typeof(ObservableCollection<DataGridColumn>), typeof(ColumnsBindingBehavior),
                new PropertyMetadata(OnDataGridColumnsPropertyChanged));

        protected override void OnAttached()
        {
            base.OnAttached();
            this._dataGridColumns = AssociatedObject.Columns;
        }

        private static void OnDataGridColumnsPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var context = source as ColumnsBindingBehavior;

            var oldItems = e.OldValue as ObservableCollection<DataGridColumn>;
            if (oldItems != null)
            {
                foreach (var one in oldItems)
                    context._dataGridColumns.Remove(one);

                oldItems.CollectionChanged -= context.CollectionChanged;
            }

            var newItems = e.NewValue as ObservableCollection<DataGridColumn>;
            if (newItems != null)
            {
                foreach (var one in newItems)
                    context._dataGridColumns.Add(one);

                newItems.CollectionChanged += context.CollectionChanged;
            }
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (DataGridColumn one in e.NewItems)
                            _dataGridColumns.Add(one);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (DataGridColumn one in e.OldItems)
                            _dataGridColumns.Remove(one);
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    _dataGridColumns.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    _dataGridColumns.Clear();
                    if (e.NewItems != null)
                    {
                        foreach (DataGridColumn one in e.NewItems)
                            _dataGridColumns.Add(one);
                    }
                    break;
            }
        }
    }
}