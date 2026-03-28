using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable disable
namespace UnitSystem
{
    [Serializable]
    public sealed class UnitMeasure : IComparable, IComparable<UnitMeasure>, IEquatable<UnitMeasure>, IFormattable
    {
        private static UnitMeasure none = new UnitMeasure("no unit", "no symbol", UnitType.None, "no unit");

        private string name;
        private string symbol;
        private double factor;

        private UnitType unitType;
        private bool isNamed;
        private string family;
        #region Constructor methods
        public UnitMeasure()
        {

        }
        public UnitMeasure(string name, string symbol, UnitType unitType, string family)
            : this(name, symbol, 1.0, unitType, true, family)
        {
        }

        public UnitMeasure(string name, string symbol, UnitMeasure baseUnit, string family)
            : this(name, symbol, baseUnit.factor, baseUnit.unitType, true, family)
        {
        }

        private UnitMeasure(string name, string symbol, double factor, UnitType unitType, bool isNamed, string family)
        {
            this.name = name;
            this.symbol = symbol;
            this.family = family;
            this.factor = factor;

            this.unitType = unitType;
            this.isNamed = isNamed;
        }
        public string Family => family;
        /// <summary>
        /// None unit.
        /// </summary>
        public static UnitMeasure None
        {
            get { return none; }
        }

        #endregion Constructor methods

        #region Public implementation

        /// <summary>
        /// Gets the name of the unit.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Gets the symbol of the unit.
        /// </summary>
        public string Symbol
        {
            get { return symbol; }
        }

        /// <summary>
        /// Gets the factor of the unit.
        /// </summary>
        public double Factor
        {
            get { return factor; }
        }

        /// <summary>
        /// Whether the unit is named.
        /// </summary>
        public bool IsNamed
        {
            get { return isNamed; }
        }

        /// <summary>
        /// Gets the type of the unit.
        /// </summary>
        public UnitType UnitType
        {
            get { return unitType; }
        }

        /// <summary>
        /// Checks whether the given unit is compatible to this one.
        /// Raises an exception if not compatible.
        /// </summary>
        /// <exception cref="UnitConversionException">Raised when units are not compatible.</exception>
        public void AssertCompatibility(UnitMeasure compatibleUnit)
        {
            if (!IsCompatibleTo(compatibleUnit)) throw new UnitConversionException(this, compatibleUnit);
        }

        /// <summary>
        /// Checks whether the passed unit is compatible with this one.
        /// </summary>
        public bool IsCompatibleTo(UnitMeasure otherUnit)
        {
            return unitType == (otherUnit ?? none).unitType;
        }

        /// <summary>
        /// Returns a unit by raising the present unit to the specified power.
        /// I.e. meter.Power(3) would return a cubic meter unit.
        /// </summary>
        public UnitMeasure Power(double power)
        {
            return new UnitMeasure(string.Concat('(', name, '^', power, ')'), symbol + '^' + power,
                (double)Math.Pow(factor, (double)power), unitType.Power(power), false, family);
        }

        /// <summary>
        /// Tests equality of both objects.
        /// </summary>
        public override bool Equals(object obj)
        {
            return this == obj as UnitMeasure;
        }

        /// <summary>
        /// Tests equality of both objects.
        /// </summary>
        public bool Equals(UnitMeasure unit)
        {
            return this == unit;
        }

        /// <summary>
        /// Returns the hashcode of this unit.
        /// </summary>
        public override int GetHashCode()
        {
            return factor.GetHashCode() ^ unitType.GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the unit.
        /// </summary>
        public override string ToString()
        {
            return ToString(null, null);
        }

        /// <summary>
        /// Returns a string representation of the unit.
        /// </summary>
        public string ToString(string format)
        {
            return ToString(format, null);
        }

        /// <summary>
        /// Returns a string representation of the unit.
        /// </summary>
        public string ToString(IFormatProvider formatProvider)
        {
            return ToString(null, formatProvider);
        }

        /// <summary>
        /// Returns a string representation of the unit.
        /// </summary>
        /// <remarks>
        /// The format string can be either 'UN' (Unit Name) or 'US' (Unit Symbol).
        /// </remarks>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (format == null) format = "US";

            if (formatProvider != null)
            {
                ICustomFormatter formatter = formatProvider.GetFormat(GetType()) as ICustomFormatter;
                if (formatter != null)
                {
                    return formatter.Format(format, this, formatProvider);
                }
            }

            switch (format)
            {
                case "UN":
                    return Name;
                case "US":
                default:
                    return Symbol;
            }
        }

        #endregion Public implementation

        #region Operator overloads

        public static bool operator ==(UnitMeasure left, UnitMeasure right)
        {
            // Special cases:
            if (ReferenceEquals(left, right))
                return true;

            // Compare content:
            left = left ?? none;
            right = right ?? none;
            return left.symbol == right.symbol && left.factor == right.factor && left.unitType == right.unitType;
        }

        public static bool operator !=(UnitMeasure left, UnitMeasure right)
        {
            return !(left == right);
        }

        public static UnitMeasure operator *(UnitMeasure left, UnitMeasure right)
        {
            left = left ?? none;
            right = right ?? none;
            return new UnitMeasure(string.Concat('(', left.name, '*', right.name, ')'), left.symbol + '*' + right.symbol,
                left.factor * right.factor, left.unitType * right.unitType, false, left.Family);
        }

        public static UnitMeasure operator *(UnitMeasure left, double right)
        {
            return right * left;
        }


        public static UnitMeasure operator *(double left, UnitMeasure right)
        {
            right = right ?? none;
            return new UnitMeasure(string.Concat('(', left.ToString(), '*', right.name, ')'), left.ToString() + '*' + right.symbol,
                left * right.factor, right.unitType, false, right.Family);
        }

        public static UnitMeasure operator /(UnitMeasure left, UnitMeasure right)
        {
            left = left ?? none;
            right = right ?? none;
            return new UnitMeasure(string.Concat('(', left.name, '/', right.name, ')'), left.symbol + '/' + right.symbol,
                left.factor / right.factor, left.unitType / right.unitType, false, left.Family);
        }


        public static UnitMeasure operator /(double left, UnitMeasure right)
        {
            right = right ?? none;
            return new UnitMeasure(string.Concat('(', left.ToString(), '*', right.name, ')'), left.ToString() + '*' + right.symbol,
                left / right.factor, right.unitType.Power(-1), false, right.Family);
        }


        public static UnitMeasure operator /(UnitMeasure left, double right)
        {
            left = left ?? none;
            return new UnitMeasure(string.Concat('(', left.name, '/', right.ToString(), ')'), left.symbol + '/' + right.ToString(),
               left.factor / right, left.unitType, false, left.Family);
        }

        #endregion Operator overloads

        #region IComparable implementation

        /// <summary>
        /// Compares the passed unit to the current one. Allows sorting units of the same type.
        /// </summary>
        /// <remarks>Only compatible units can be compared.</remarks>
        int IComparable.CompareTo(object obj)
        {
            return ((IComparable<UnitMeasure>)this).CompareTo((UnitMeasure)obj);
        }

        /// <summary>
        /// Compares the passed unit to the current one. Allows sorting units of the same type.
        /// </summary>
        /// <remarks>Only compatible units can be compared.</remarks>
        int IComparable<UnitMeasure>.CompareTo(UnitMeasure other)
        {
            AssertCompatibility(other);
            if (factor < other.factor) return -1;
            else if (factor > other.factor) return +1;
            else return 0;
        }

        #endregion IComparable implementation
    }
}
