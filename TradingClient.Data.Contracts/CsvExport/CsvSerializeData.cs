using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TradingClient.Data.Contracts
{
    internal class CsvSerializeData
    {
        public PropertyInfo PropertyInfo { get; }

        public CsvSerializeData(PropertyInfo property) => PropertyInfo = property;

        public virtual IEnumerable<string> GetHeaders<T>(IEnumerable<T> items)
        {
            yield return PropertyInfo.Name;
        }

        public virtual IEnumerable<string> GetValue(object item)
        {
            yield return item?.ToString();
        }
    }

    internal class CsvSerializeSubData : CsvSerializeData
    {
        public List<CsvSerializeData> Items { get; } = new List<CsvSerializeData>();

        public CsvSerializeSubData(PropertyInfo property, Type t) :
            base(property)
        {
            foreach (var p in t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).OrderBy(x => x.MetadataToken).ToList())
            {
                if (GetPropertyAttribute<CsvSerializeIgnoreAttribute>(p) != null)
                    continue;

                var includeAttribute = GetPropertyAttribute<CsvSerializeSubAttribute>(p);
                if (includeAttribute != null)
                {
                    Items.Add(new CsvSerializeSubData(p, p.PropertyType));
                    continue;
                }

                var dictionaryAttribute = GetPropertyAttribute<CsvSerializeDictionaryAttribute>(p);
                if (dictionaryAttribute != null)
                {
                    Items.Add(new CsvSerializeDictionaryData(p, p.PropertyType));
                    continue;
                }

                Items.Add(new CsvSerializeData(p));
            }
        }

        public override IEnumerable<string> GetHeaders<T>(IEnumerable<T> items)
        {
            foreach (var p in Items)
            {
                foreach (var item in p.GetHeaders(items.Select(s => GetValue(p.PropertyInfo, s))))
                {
                    yield return item;
                }
            }
        }

        public override IEnumerable<string> GetValue(object data)
        {
            foreach (var p in Items)
            {
                foreach (var item in p.GetValue(GetValue(p.PropertyInfo, data)))
                {
                    yield return item;
                }
            }
        }

        private A GetPropertyAttribute<A>(PropertyInfo property) where A : Attribute
         => property.GetCustomAttributes<A>().FirstOrDefault() as A;

        private object GetValue(PropertyInfo property, object obj) 
            => obj == null ? null : property.GetValue(obj);
    }

    internal class CsvSerializeDictionaryData : CsvSerializeData
    {
        List<string> Keys { get; } = new List<string>();

        public CsvSerializeDictionaryData(PropertyInfo property, Type t) :
            base(property)
        {

        }

        public override IEnumerable<string> GetHeaders<T>(IEnumerable<T> items)
        {
            Keys.Clear();
            foreach (var item in items)
            {
                if (item is IDictionary dic)
                {
                    foreach (var k in dic.Keys)
                    {
                        var keyValue = k.ToString();
                        if(!Keys.Contains(keyValue))
                            Keys.Add(keyValue);
                    }
                }
            }

            return Keys;
        }

        public override IEnumerable<string> GetValue(object data)
        {
            var dictionaty = data as IDictionary;

            foreach (var k in Keys)
            {
                if (dictionaty?.Contains(k) == true)
                    yield return dictionaty[k].ToString();
                else
                    yield return string.Empty;
            }
        }

    }
}
