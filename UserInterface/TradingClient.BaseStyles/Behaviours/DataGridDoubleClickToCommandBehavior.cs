using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace TradingClient.BaseStyles.Behaviours
{
    public class DataGridDoubleClickToCommandBehavior : Behavior<DataGrid>
    {
        #region Dependency Property

        private static readonly PropertyMetadata MetaData = new PropertyMetadata(null);

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command),
            typeof (ICommand), typeof (DataGridDoubleClickToCommandBehavior), MetaData);

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        #endregion //Dependency Property

        #region Overrides

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseDoubleClick += Grid_OnMouseDoubleClick;
            AssociatedObject.MouseLeftButtonDown += Grid_MouseLeftButtonDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseDoubleClick -= Grid_OnMouseDoubleClick;
            AssociatedObject.MouseLeftButtonDown -= Grid_MouseLeftButtonDown;
        }

        #endregion //Overrides

        #region Events

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
            {
                var dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                if (dgr != null && !dgr.IsMouseOver)
                    dgr.IsSelected = false;
            }
        }
            
        private void Grid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Command == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            var parent = e.OriginalSource as DependencyObject;
            while (parent != null)
            {
                if (parent is DataGridColumnHeader)
                    return;

                parent = VisualTreeHelper.GetParent(parent);
            }

            if (Command.CanExecute(null))
                Command.Execute(null);
        }

        #endregion //Events
    }
}