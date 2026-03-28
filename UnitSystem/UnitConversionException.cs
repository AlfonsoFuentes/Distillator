using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
#nullable disable
namespace UnitSystem
{
    /// <summary>
    /// Exception thrown when a unit conversion failed, i.e. because you are converting
    /// amounts from one unit into another non-compatible unit.
    /// </summary>
  
    public class UnitConversionException : InvalidOperationException
    {
        public UnitConversionException() : base() { }

        public UnitConversionException(string message) : base(message) { }

        public UnitConversionException(UnitMeasure fromUnit, UnitMeasure toUnit) : this(string.Format("Failed to convert from unit '{0}' to unit '{1}'. UnitsSystem are not compatible and no conversions are defined.", fromUnit.Name, toUnit.Name)) { }

        public UnitConversionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    
    public class UnknownUnitException : ApplicationException
    {

        public UnknownUnitException() : base() { }

        public UnknownUnitException(string message) : base(message) { }

        public UnknownUnitException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
