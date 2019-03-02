using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interactivity;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace TradingClient.BaseStyles.Behaviours
{
    public class ExpandPropertyGridBehavior : Behavior<PropertyGrid>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PropertyChanged += AssociatedObject_PropertyChanged;
        }

        private void AssociatedObject_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AssociatedObject.Properties))
            {
                AssociatedObject.ExpandAllProperties();
            }
        }

        protected override void OnChanged()
        {
            base.OnChanged();
            AssociatedObject.PropertyChanged -= AssociatedObject_PropertyChanged;
        }
    }
}
