using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Xml.Serialization;

namespace TradingClient.Data.Contracts
{
    [Serializable]
    public abstract class ScriptingParameterBase : ICloneable
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int ID { get; set; }
        public bool IsReadOnly { get; set; }
        public static Type[] DerivedClasses
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                    .Where(x => x.IsSubclassOf(typeof(ScriptingParameterBase))).ToArray();
            }
        }

        protected ScriptingParameterBase()
        {
            Name = string.Empty;
            Category = string.Empty;
        }

        protected ScriptingParameterBase(string name, string category, int id)
        {
            Name = name;
            Category = category;
            ID = id;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        public override bool Equals(object obj)
        {
            var parameter = obj as ScriptingParameterBase;
            if (parameter == null)
                return false;

            return parameter.ID.Equals(ID) && parameter.Category.Equals(Category) && parameter.ID.Equals(ID);
        }

        public int GetHashCode() =>
          ID.GetHashCode() ^ Name.GetHashCode() ^ Category.GetHashCode() ^ IsReadOnly.GetHashCode();
    }

    [Serializable]
    public class IntParam : ScriptingParameterBase
    {
        public int Value { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int StartValue { get; set; }
        public int StopValue { get; set; }
        public int Step { get; set; }

        public IntParam()
        {
            InitializeValues();
        }

        public IntParam(string name, string category, int ID)
            : base(name, category, ID)
        {
            InitializeValues();
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            var parameter = obj as IntParam;
            if (parameter == null)
                return false;

            return parameter.Value.Equals(Value);
        }

        private void InitializeValues()
        {
            MinValue = Int32.MinValue;
            MaxValue = Int32.MaxValue;
            StartValue = 0;
            StopValue = 100;
            Step = 1;
        }
    }

    [Serializable]
    public class DoubleParam : ScriptingParameterBase
    {
        public double Value { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double StartValue { get; set; }
        public double StopValue { get; set; }
        public double Step { get; set; }

        public DoubleParam()
        {
            InitializeValues();
        }

        public DoubleParam(string name, string category, int ID)
            : base(name, category, ID)
        {
            InitializeValues();
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            var parameter = obj as DoubleParam;
            if (parameter == null)
                return false;

            return parameter.Value.Equals(Value);
        }

        private void InitializeValues()
        {
            MinValue = Double.MinValue;
            MaxValue = Double.MaxValue;
            StartValue = 0.0;
            StopValue = 100.0;
            Step = 1.0;
        }
    }

    [Serializable]
    public class StringParam : ScriptingParameterBase
    {
        public string Value { get; set; }
        public List<string> AllowedValues { get; set; }

        public StringParam()
        {
            Value = string.Empty;
            AllowedValues = new List<string>();
        }

        public StringParam(string name, string category, int ID)
            : base(name, category, ID)
        {
            Value = string.Empty;
            AllowedValues = new List<string>();
        }

        public override object Clone()
        {
            var obj = MemberwiseClone() as StringParam;
            obj.AllowedValues = AllowedValues.ToList();
            return obj;
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            var parameter = obj as StringParam;
            if (parameter == null)
                return false;

            return parameter.Value.Equals(Value);
        }
    }

    [Serializable]
    public class BoolParam : ScriptingParameterBase
    {
        public bool Value { get; set; }

        public BoolParam()
        {
            InitializeValues();
        }

        public BoolParam(string name, string category, int ID)
            : base(name, category, ID)
        {
            InitializeValues();
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            var parameter = obj as BoolParam;
            if (parameter == null)
                return false;

            return parameter.Value.Equals(Value);
        }

        private void InitializeValues()
        {
            Value = true;
        }
    }

    [Serializable]
    public class SeriesParam : ScriptingParameterBase
    {
        public SeriesParam()
        {
        }

        public SeriesParam(string name, string category, int ID)
            : base(name, category, ID)
        {
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
                return false;

            var parameter = obj as SeriesParam;
            if (parameter == null)
                return false;

            return parameter.ID.Equals(ID) && parameter.Name.Equals(Name);
        }
    }
}
