using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace TradingClient.Common
{
    public class ItemPropertyDescriptor<T> : PropertyDescriptor
    {
        #region Members

        private readonly ObservableCollection<T> _owner;
        private readonly int _index;

        #endregion //Members

        public ItemPropertyDescriptor(ObservableCollection<T> owner, int index)
          : base("#" + index, null)
        {
            _owner = owner;
            _index = index;
        }

        #region Overrides

        public override AttributeCollection Attributes
        {
            get
            {
                var attributes = TypeDescriptor.GetAttributes(GetValue(null), false);
                if (!attributes.OfType<ExpandableObjectAttribute>().Any())
                {
                    var newAttributes = new Attribute[attributes.Count + 1];
                    attributes.CopyTo(newAttributes, newAttributes.Length - 1);
                    newAttributes[newAttributes.Length - 1] = new ExpandableObjectAttribute();
                    attributes = new AttributeCollection(newAttributes);
                }
                return attributes;
            }
        }

        public override bool CanResetValue(object component) => false;

        public override object GetValue(object component) => Value;

        private T Value => _owner[_index];

        public override void ResetValue(object component) => throw new NotImplementedException();

        public override void SetValue(object component, object value) => _owner[_index] = (T)value;

        public override bool ShouldSerializeValue(object component) => false;

        public override Type ComponentType => _owner.GetType();

        public override bool IsReadOnly => false;

        public override Type PropertyType => Value?.GetType();

        #endregion //Overrides
    }
}
