namespace UnitSystem
{
    public interface IAmount
    {
        string sValue { get; set; }
        UnitMeasure Unit { get; set; }
        List<UnitMeasure> UnitsList { get; }
        double Value { get; set; }

        Amount Abs();
        Amount Add(Amount amount);
        UnitMeasure AsUnit();
        object Clone();
        Amount ConvertedTo(string unitName);
        Amount ConvertedTo(string unitName, int decimals);
        Amount ConvertedTo(UnitMeasure unit);
        Amount ConvertedTo(UnitMeasure unit, int decimals);
        Amount DivideBy(Amount amount);
        Amount DivideBy(double dvalue);
        bool Equals(Amount amount);
        bool Equals(object obj);
        int GetHashCode();
        double GetValue(UnitMeasure unit);
        double GetValueManometric(UnitMeasure unit);
        Amount Inverse();
        Amount Multiply(Amount amount);
        Amount Multiply(double dvalue);
        Amount Negate();
        Amount Power(double power);
        void SetUnit(string unit);
        void SetUnit(UnitMeasure unit);
        void SetValue(Amount amount);
        void SetValue(double dvalue, string unitName);
        void SetValue(double dvalue, UnitMeasure unit);
        void SetValueManometric(Amount amo);
        void SetValueManometric(double value, UnitMeasure unit);
        void SetValueNoEvent(Amount amount);
        void SetValueNoEvent(double dvalue, UnitMeasure unit);
        Amount[] Split(UnitMeasure[] units, int decimals);
        string ToString();
        string ToString(IFormatProvider formatProvider);
        string ToString(string format);
        string ToString(string format, IFormatProvider formatProvider);
    }
}