using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TradingClient.Interfaces;

namespace TradingClient.Common
{
    public class ExpandableObservableCollection<T> : ObservableCollection<T>,
                                                 ICustomTypeDescriptor, IExpandableObservableCollection<T>
    {
        public ExpandableObservableCollection()
        {

        }

        public ExpandableObservableCollection(IEnumerable<T> items)
        {
            AddRange(items);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        #region Use default TypeDescriptor stuff

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            PropertyDescriptorCollection pds = new PropertyDescriptorCollection(null);
            for (int i = 0; i < Count; i++)
            {
                pds.Add(new ItemPropertyDescriptor<T>(this, i));
            }

            return pds;
        }

        AttributeCollection ICustomTypeDescriptor.GetAttributes() =>
            TypeDescriptor.GetAttributes(this, noCustomTypeDesc: true);

        string ICustomTypeDescriptor.GetClassName() =>
            TypeDescriptor.GetClassName(this, noCustomTypeDesc: true);

        string ICustomTypeDescriptor.GetComponentName() =>
            TypeDescriptor.GetComponentName(this, noCustomTypeDesc: true);

        TypeConverter ICustomTypeDescriptor.GetConverter() =>
            TypeDescriptor.GetConverter(this, noCustomTypeDesc: true);

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() =>
            TypeDescriptor.GetDefaultEvent(this, noCustomTypeDesc: true);

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() =>
            TypeDescriptor.GetDefaultProperty(this, noCustomTypeDesc: true);

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType) =>
            TypeDescriptor.GetEditor(this, editorBaseType, noCustomTypeDesc: true);

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents() =>
            TypeDescriptor.GetEvents(this, noCustomTypeDesc: true);

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) =>
            TypeDescriptor.GetEvents(this, attributes, noCustomTypeDesc: true);

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) =>
            TypeDescriptor.GetProperties(this, attributes, noCustomTypeDesc: true);

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) =>
            this;

        #endregion
    }
}
