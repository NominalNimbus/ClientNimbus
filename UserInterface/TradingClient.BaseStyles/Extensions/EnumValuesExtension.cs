using System;
using System.Collections;
using System.Windows.Markup;

namespace TradingClient.BaseStyles.Extensions
{
    public class EnumValuesExtension : MarkupExtension
    {
        public EnumValuesExtension(Type enumType)
        {
            EnumType = enumType;
        }

        public Type EnumType { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider) =>
            EnumType == null ? throw new NullReferenceException() : new ArrayList(Enum.GetValues(EnumType));

    }
}