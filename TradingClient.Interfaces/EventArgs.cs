using System;

namespace TradingClient.Interfaces
{
    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T val)
        {
            Value = val;
        }

        public T Value { get; }
    }

    public class EventArgs<T1, T2> : EventArgs
    {
        public EventArgs(T1 value1, T2 value2)
        {
            Value1 = value1;
            Value2 = value2;
        }

        public T1 Value1 { get; }

        public T2 Value2 { get; }
    }
}