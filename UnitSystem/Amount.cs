#nullable disable
using System.Text.Json.Serialization;

namespace UnitSystem
{

    public class Amount :
        ICloneable,
        IComparable,
        IComparable<Amount>,
        IConvertible,
        IEquatable<Amount>,
        IFormattable,
        IUnitConsumer
    {
        private static int equalityPrecision = 8;
        [JsonIgnore]
        protected double dvalue;

        [JsonIgnore]
        protected UnitMeasure unit;

        // 2. Aseguramos que el serializador use UnitName para reconstruir la unidad
        [JsonPropertyName("unitName")]
        public string UnitName
        {
            get => unit?.Name ?? unitname;
            set
            {
                unitname = value;
                if (unit != null!)
                {
                    // Evitamos lógica de conversión pesada durante la deserialización
                    dvalue = ConvertedTo(unitname).Value;
                }
                unit = UnitManager.GetUnitByName(unitname);
            }
        }

        // 3. Propiedad explícita para el valor que el JSON debe leer/escribir
        [JsonPropertyName("value")]
        public double Value
        {
            get { return dvalue; }
            set { dvalue = value; }
        }

        // 4. Ignoramos la propiedad compleja Unit, ya que se reconstruye vía UnitName
        [JsonIgnore]
        public UnitMeasure Unit
        {
            get { return unit; }
            set { var newvalue = ConvertedTo(value); SetValue(newvalue); }
        }
       

        // Tu constructor vacío ya existe, asegúrate de que no inicialice 
        // nada que rompa la deserialización.
        public Amount()
        {
        }
     

        string name = "";
        string unitname = "";
        public delegate void ValueChanged();
      

        //public ValueChanged OnValueChanged;

        #region Constructor methods
        public Amount(string unitName)
        {
            UnitName = unitName;
        }
        public Amount(UnitMeasure unit)
        {
            dvalue = 0;
            this.unit = unit;
            unitname = this.unit.Name;
        }


        public Amount(double dvalue, UnitMeasure unit)
        {

            this.dvalue = dvalue;
            this.unit = unit;
            unitname = this.unit.Name;

        }

        public Amount(Amount amo)
        {
            dvalue = amo.dvalue;

            name = amo.name;
            UnitName = amo.Unit.Name;
        }
        public Amount(double dvalue, string unitName)
        {
            this.dvalue = dvalue;
            UnitName = unitName;

        }
        public void SetValue(double dvalue, string unitName)
        {
            this.dvalue = dvalue;
            UnitName = unitName;

        }
        
        void SetValue(Amount amount)
        {
            SetValue(amount.dvalue, amount.unit);
        }



        public virtual void SetValue(double dvalue, UnitMeasure unit)
        {
            this.dvalue = dvalue;
            this.unit = unit;
            unitname = unit.Name;
        }
        public void SetValueNoEvent(Amount amount)
        {
            SetValueNoEvent(amount.dvalue, amount.unit);
        }
        public void SetValueNoEvent(double dvalue, UnitMeasure unit)
        {
            this.dvalue = dvalue;
            this.unit = unit;

        }

        public double GetValue(UnitMeasure unit)
        {

            return ConvertedTo(unit).Value;
        }


        public static Amount Zero(UnitMeasure unit)
        {
            return new Amount(0.0, unit);
        }

        public static Amount Zero(string unitName)
        {
            return new Amount(0.0, unitName);
        }

        #endregion Constructor methods

        #region Public implementation

        /// <summary>
        /// The precision to which two amounts are considered equal.
        /// </summary>
        public static int EqualityPrecision
        {
            get { return equalityPrecision; }
            set { equalityPrecision = value; }
        }

        /// <summary>
        /// Gets the raw dvalue of the amount.
        /// </summary>
       
        public List<UnitMeasure> UnitsList => UnitManager.GetUnitsByFamily(unit).ToList();

        public List<UnitMeasure> GetUnitList()
        {
            return UnitManager.GetUnitsByFamily(unit).ToList();
        }
        /// <summary>
        /// Gets the unit of the amount.
        /// </summary>
       

        /// <summary>
        /// Returns a unit that matches this amount.
        /// </summary>
        public UnitMeasure AsUnit()
        {
            return new UnitMeasure(dvalue + "*" + unit.Name, dvalue + "*" + unit.Symbol,
                Value * Unit, Unit.Family);
        }

        /// <summary>
        /// Returns a clone of the Amount object.
        /// </summary>
        public object Clone()
        {
            // Actually, as Amount is immutable, it can safely return itself:
            return this;
        }

        /// <summary>
        /// Returns a matching amount converted to the given unit and rounded
        /// up to the given number of decimals.
        /// </summary>
        public virtual Amount ConvertedTo(string unitName, int decimals)
        {
            return ConvertedTo(UnitManager.GetUnitByName(unitName), decimals);
        }

        /// <summary>
        /// Returns a matching amount converted to the given unit and rounded
        /// up to the given number of decimals.
        /// </summary>
        public virtual Amount ConvertedTo(UnitMeasure unit, int decimals)
        {
            return new Amount(Math.Round(UnitManager.ConvertTo(this, unit).Value, decimals), unit);
        }

        /// <summary>
        /// Returns a matching amount converted to the given unit.
        /// </summary>
        public virtual Amount ConvertedTo(string unitName)
        {
            return ConvertedTo(UnitManager.GetUnitByName(unitName));
        }

        /// <summary>
        /// Returns a matching amount converted to the given unit.
        /// </summary>
        public virtual Amount ConvertedTo(UnitMeasure unit)
        {
            // Let UnitManager perform conversion:
            return UnitManager.ConvertTo(this, unit);
        }

        /// <summary>
        /// Splits this amount into integral values of the given units
        /// except for the last amount which is rounded up to the number
        /// of decimals given.
        /// </summary>
        public virtual Amount[] Split(UnitMeasure[] units, int decimals)
        {
            Amount[] amounts = new Amount[units.Length];
            Amount rest = this;

            // Truncate for all but the last unit:
            for (int i = 0; i < units.Length - 1; i++)
            {
                amounts[i] = (Amount)rest.ConvertedTo(units[i]).MemberwiseClone();
                amounts[i].dvalue = Math.Truncate(amounts[i].dvalue);
                rest = rest - amounts[i];
            }

            // Handle the last unit:
            amounts[units.Length - 1] = rest.ConvertedTo(units[units.Length - 1], decimals);

            return amounts;
        }

        public override bool Equals(object obj)
        {
            return this == (obj as Amount)!;
        }

        public virtual bool Equals(Amount amount)
        {
            return this == amount;
        }

        public override int GetHashCode()
        {
            return dvalue.GetHashCode() ^ unit.GetHashCode();
        }

        /// <summary>
        /// Shows the default string representation of the amount. (The default format string is "GG").
        /// </summary>
        public override string ToString()
        {
            return ToString((string)null!, null!);
        }

        /// <summary>
        /// Shows a string representation of the amount, formatted according to the passed format string.
        /// </summary>
        public string ToString(string format)
        {
            return ToString(format, null!);
        }

        /// <summary>
        /// Shows the default string representation of the amount using the given format provider.
        /// </summary>
        public string ToString(IFormatProvider formatProvider)
        {
            return ToString((string)null!, formatProvider);
        }

        /// <summary>
        /// Shows a string representation of the amount, formatted according to the passed format string,
        /// using the given format provider.
        /// </summary>
        /// <remarks>
        /// Valid format strings are 'GG', 'GN', 'GS', 'NG', 'NN', 'NS' (where the first letter represents
        /// the dvalue formatting (General, Numeric), and the second letter represents the unit formatting
        /// (General, Name, Symbol)), or a custom number format with 'UG', 'UN' or 'US' (UnitGeneral,
        /// UnitName or UnitSymbol) representing the unit (i.e. "#,##0.00 UL"). The format string can also
        /// contains a '|' followed by a unit to convert to.
        /// </remarks>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null) format = "GG";

            if (formatProvider != null)
            {
                ICustomFormatter formatter = (formatProvider?.GetFormat(GetType())! as ICustomFormatter)!;
                if (formatter != null)
                {
                    return formatter.Format(format, this, formatProvider);
                }
            }

            string[] formats = format.Split('|');
            Amount amount = this;
            if (formats.Length >= 2)
            {
                if (formats[1] == "?")
                    amount = amount.ConvertedTo(UnitManager.ResolveToNamedUnit(amount.Unit, true));
                else
                    amount = amount.ConvertedTo(formats[1]);
            }

            switch (formats[0])
            {
                case "GG":
                    return string.Format(formatProvider, "{0:G} {1}", Math.Round(amount.Value, 2), amount.Unit).TrimEnd(null);
                case "GN":
                    return string.Format(formatProvider, "{0:G} {1:UN}", amount.Value, amount.Unit).TrimEnd(null);
                case "GS":
                    return string.Format(formatProvider, "{0:G} {1:US}", amount.Value, amount.Unit).TrimEnd(null);
                case "NG":
                    return string.Format(formatProvider, "{0:N} {1}", amount.Value, amount.Unit).TrimEnd(null);
                case "NN":
                    return string.Format(formatProvider, "{0:N} {1:UN}", amount.Value, amount.Unit).TrimEnd(null);
                case "NS":
                    return string.Format(formatProvider, "{0:N} {1:US}", amount.Value, amount.Unit).TrimEnd(null);
                default:
                    formats[0] = formats[0].Replace("UG", "\"" + amount.Unit.ToString("", formatProvider) + "\"");
                    formats[0] = formats[0].Replace("UN", "\"" + amount.Unit.ToString("UN", formatProvider) + "\"");
                    formats[0] = formats[0].Replace("US", "\"" + amount.Unit.ToString("US", formatProvider) + "\"");
                    return amount.Value.ToString(formats[0], formatProvider).TrimEnd(null);
            }
        }

        /// <summary>
        /// Static convenience ToString method, returns ToString of the amount,
        /// or empty string if amount is null.
        /// </summary>
        public static string ToString(Amount amount)
        {
            return ToString(amount, null, null);
        }

        /// <summary>
        /// Static convenience ToString method, returns ToString of the amount,
        /// or empty string if amount is null.
        /// </summary>
        public static string ToString(Amount amount, string format)
        {
            return ToString(amount, format, null);
        }

        /// <summary>
        /// Static convenience ToString method, returns ToString of the amount,
        /// or empty string if amount is null.
        /// </summary>
        public static string ToString(Amount amount, IFormatProvider formatProvider)
        {
            return ToString(amount, null, formatProvider);
        }

        /// <summary>
        /// Static convenience ToString method, returns ToString of the amount,
        /// or empty string if amount is null.
        /// </summary>
        public static string ToString(Amount amount, string format, IFormatProvider formatProvider)
        {
            if (amount == null) return string.Empty;
            else return amount.ToString(format, formatProvider);
        }

        #endregion Public implementation

        #region Mathematical operations

        /// <summary>
        /// Adds this with the amount (= this + amount).
        /// </summary>
        public virtual Amount Add(Amount amount)
        {
            var result = this + amount;
            return result;
        }
        public virtual Amount Abs()
        {
            return new Amount(Math.Abs(dvalue), unit);
        }

        /// <summary>
        /// Negates this (= -this).
        /// </summary>
        public virtual Amount Negate()
        {
            return -this;
        }

        /// <summary>
        /// Multiply this with amount (= this * amount).
        /// </summary>
        public virtual Amount Multiply(Amount amount)
        {
            return this * amount;
        }

        /// <summary>
        /// Multiply this with dvalue (= this * dvalue).
        /// </summary>
        public virtual Amount Multiply(double dvalue)
        {
            return this * dvalue;
        }

        /// <summary>
        /// Divides this by amount (= this / amount).
        /// </summary>
        public virtual Amount DivideBy(Amount amount)
        {
            return this / amount;
        }

        /// <summary>
        /// Divides this by dvalue (= this / dvalue).
        /// </summary>
        public virtual Amount DivideBy(double dvalue)
        {
            return this / dvalue;
        }

        /// <summary>
        /// Returns 1 over this amount (= 1 / this).
        /// </summary>
        public virtual Amount Inverse()
        {
            return 1.0 / this;
        }

        /// <summary>
        /// Raises this amount to the given power.
        /// </summary>
        public virtual Amount Power(double power)
        {
            return new Amount(Math.Pow(dvalue, power), unit.Power(power));
        }
        public static Amount Abs(Amount amount)
        {
            return new Amount(Math.Abs(amount.dvalue), amount.unit);
        }

        #endregion Mathematical operations

        #region Operator overloads

        /// <summary>
        /// Compares two amounts.
        /// </summary>
        public static bool operator ==(Amount left, Amount right)
        {
            // Check references:
            if (ReferenceEquals(left, right))
                return true;
            else if (ReferenceEquals(left, null))
                return false;
            else if (ReferenceEquals(right, null))
                return false;

            // Check dvalue:
            try
            {
                return Math.Round(left.dvalue, equalityPrecision)
                    == Math.Round(right.ConvertedTo(left.Unit).dvalue, equalityPrecision);
            }
            catch (UnitConversionException)
            {
                return false;
            }
        }

        /// <summary>
        /// Compares two amounts.
        /// </summary>
        public static bool operator !=(Amount left, Amount right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Compares two amounts of compatible units.
        /// </summary>
        public static bool operator <(Amount left, Amount right)
        {
            Amount rightConverted = right.ConvertedTo(left.unit);
            return left != rightConverted && left.dvalue < rightConverted.dvalue;
        }

        /// <summary>
        /// Compares two amounts of compatible units.
        /// </summary>
        public static bool operator <=(Amount left, Amount right)
        {
            Amount rightConverted = right.ConvertedTo(left.unit);
            return left == rightConverted || left.dvalue < rightConverted.dvalue;
        }

        /// <summary>
        /// Compares two amounts of compatible units.
        /// </summary>
        public static bool operator >(Amount left, Amount right)
        {
            Amount rightConverted = right.ConvertedTo(left.unit);
            return left != rightConverted && left.dvalue > rightConverted.dvalue;
        }

        /// <summary>
        /// Compares two amounts of compatible units.
        /// </summary>
        public static bool operator >=(Amount left, Amount right)
        {
            Amount rightConverted = right.ConvertedTo(left.unit);
            return left == rightConverted || left.dvalue > rightConverted.dvalue;
        }

        /// <summary>
        /// Unary '+' operator.
        /// </summary>
        public static Amount operator +(Amount right)
        {
            return right;
        }

        /// <summary>
        /// Additions two amounts of compatible units.
        /// </summary>
        public static Amount operator +(Amount left, Amount right)
        {
            if (left == null && right == null) return null;
            left = left ?? Zero(right != null ? right.unit : UnitMeasure.None);
            right = right ?? Zero(left.Unit);
            return new Amount(left.dvalue + right.ConvertedTo(left.unit).dvalue, left.unit);
        }

        /// <summary>
        /// Unary '-' operator.
        /// </summary>
        public static Amount operator -(Amount right)
        {
            if (ReferenceEquals(right, null))
                return null;
            else
                return new Amount(-right.dvalue, right.unit);
        }

        /// <summary>
        /// Substracts two amounts of compatible units.
        /// </summary>
        public static Amount operator -(Amount left, Amount right)
        {
            return left + -right;
        }

        /// <summary>
        /// Multiplies two amounts.
        /// </summary>
        public static Amount operator *(Amount left, Amount right)
        {
            if (ReferenceEquals(left, null))
                return null;
            else if (ReferenceEquals(right, null))
                return null;
            else
                return new Amount(left.dvalue * right.dvalue, left.unit * right.unit);
        }

        /// <summary>
        /// Divides two amounts.
        /// </summary>
        public static Amount operator /(Amount left, Amount right)
        {
            if (ReferenceEquals(left, null))
                return null;
            else if (ReferenceEquals(right, null))
                return null;
            else
                return new Amount(left.dvalue / right.dvalue, left.unit / right.unit);
        }

        /// <summary>
        /// Multiplies an amount with a double dvalue.
        /// </summary>
        public static Amount operator *(Amount left, double right)
        {
            if (ReferenceEquals(left, null))
                return null;
            else
                return new Amount(left.dvalue * right, left.unit);
        }

        /// <summary>
        /// Divides an amount by a double dvalue.
        /// </summary>
        public static Amount operator /(Amount left, double right)
        {
            if (ReferenceEquals(left, null))
                return null;
            else
                return new Amount(left.dvalue / right, left.unit);
        }

        /// <summary>
        /// Multiplies a double dvalue with an amount.
        /// </summary>
        public static Amount operator *(double left, Amount right)
        {
            if (ReferenceEquals(right, null))
                return null;
            else
                return new Amount(left * right.dvalue, right.unit);
        }

        /// <summary>
        /// Divides a double dvalue by an amount.
        /// </summary>
        public static Amount operator /(double left, Amount right)
        {
            if (ReferenceEquals(right, null))
                return null;
            else
                return new Amount(left / right.dvalue, 1.0 / right.unit);
        }

        /// <summary>
        /// Casts a double dvalue to an amount expressed in the None unit.
        /// </summary>
        public static explicit operator Amount(double dvalue)
        {
            return new Amount(dvalue, UnitMeasure.None);
        }

        /// <summary>
        /// Casts an amount expressed in the None unit to a double.
        /// </summary>
        public static explicit operator double?(Amount amount)
        {
            try
            {
                if (amount == null) return null;
                else return amount.ConvertedTo(UnitMeasure.None).Value;
            }
            catch (UnitConversionException)
            {
                throw new InvalidCastException("An amount can only be casted to a numeric type if it is expressed in a None unit.");
            }
        }

        #endregion Operator overloads

        #region IConvertible implementation

        TypeCode IConvertible.GetTypeCode()
        {
            return TypeCode.Object;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException("An Amount cannot be converted to boolean.");
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            throw new InvalidCastException("An Amount cannot be converted to byte.");
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException("An Amount cannot be converted to char.");
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException("An Amount cannot be converted to DateTime.");
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return (decimal)(double)this;
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return (double)this;
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return (short)(double)this;
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return (int)(double)this;
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return (long)(double)this;
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            throw new InvalidCastException("An Amount cannot be converted to signed byte.");
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return (float)(double)this;
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return ToString(provider);
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(double))
            {
                return Convert.ToDouble(this);
            }
            else if (conversionType == typeof(float))
            {
                return Convert.ToSingle(this);
            }
            if (conversionType == typeof(decimal))
            {
                return Convert.ToDecimal(this);
            }
            else if (conversionType == typeof(short))
            {
                return Convert.ToInt16(this);
            }
            else if (conversionType == typeof(int))
            {
                return Convert.ToInt32(this);
            }
            else if (conversionType == typeof(long))
            {
                return Convert.ToInt64(this);
            }
            else if (conversionType == typeof(string))
            {
                return Convert.ToString(this, provider);
            }
            else
            {
                throw new InvalidCastException(string.Format("An Amount cannot be converted to the requested type {0}.", conversionType));
            }
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            throw new InvalidCastException("An Amount cannot be converted to unsigned Int16.");
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            throw new InvalidCastException("An Amount cannot be converted to unsigned Int32.");
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            throw new InvalidCastException("An Amount cannot be converted to unsigned Int64.");
        }

        #endregion IConvertible implementation

        #region IComparable implementation

        /// <summary>
        /// Compares two amounts of compatible units.
        /// </summary>
        int IComparable.CompareTo(object obj)
        {
            Amount other = obj as Amount;
            if (other == null) return +1;
            return ((IComparable<Amount>)this).CompareTo(other);
        }

        /// <summary>
        /// Compares two amounts of compatible units.
        /// </summary>
        int IComparable<Amount>.CompareTo(Amount other)
        {
            if (this < other) return -1;
            else if (this > other) return +1;
            else return 0;
        }

        #endregion IComparable implementation
        public void SetValueManometric(double value, UnitMeasure unit)
        {
            SetValue(value + PhysicsConstant.ManometricPressure.ConvertedTo(unit).Value, unit);



        }
        public void SetValueManometric(Amount amo)
        {
            SetValue(amo.dvalue + PhysicsConstant.ManometricPressure.ConvertedTo(amo.unit).Value, amo.unit);



        }
        public double GetValueManometric(UnitMeasure unit)
        {
            return (this - PhysicsConstant.ManometricPressure).ConvertedTo(unit).Value;

        }
        public string ValueUnit => $"{GetValue(unit)} {Unit.Name}";
    }

}