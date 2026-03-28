using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#nullable disable
namespace UnitSystem
{
    public delegate UnitMeasure UnitResolveEventHandler(object sender, ResolveEventArgs args);

    /// <summary>
    /// Delegate representiong a unitdirectional unit conversion function.
    /// </summary>
    /// <param name="originalAmount">The amount to be converted.</param>
    /// <returns>The resulting amount.</returns>
    public delegate Amount ConversionFunction(Amount originalAmount);

    /// <summary>
    /// The UnitManager class provides services around unit naming and identification.
    /// </summary>
    /// <remarks>
    /// The UnitManager class contains static methods that access a singleton instance 
    /// of the class.
    /// </remarks>
    public class UnitManager
    {
        #region Fields

        private static UnitManager instance;

        // Stores for named units:
        private List<UnitMeasure> allUnits = new List<UnitMeasure>();
        private Dictionary<UnitType, List<UnitMeasure>> unitsByType = new Dictionary<UnitType, List<UnitMeasure>>();
        private Dictionary<string, UnitMeasure> unitsByName = new Dictionary<string, UnitMeasure>();
        private Dictionary<string, UnitMeasure> unitsBySymbol = new Dictionary<string, UnitMeasure>();

        private Dictionary<string, List<UnitMeasure>> unitsByFamily = new Dictionary<string, List<UnitMeasure>>();

        // Store for conversion functions:
        private Dictionary<UnitConversionKeySlot, UnitConversionValueSlot> conversions = new Dictionary<UnitConversionKeySlot, UnitConversionValueSlot>();

        #endregion Fields

        #region Public properties

        /// <summary>
        /// The instance of the currently used UnitManager.
        /// </summary>
        public static UnitManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UnitManager();
                }
                return instance;
            }
            set { instance = value; }
        }

        #endregion Public properties

        #region Public methods - Registrations

        /// <summary>
        /// Registers both units and conversions based on the assemblies public types marked
        /// with [UnitDefinitionsClass] and [UnitConversionsClass] attributes.
        /// </summary>
        public static void RegisterByAssembly(Assembly assembly)
        {
            RegisterUnits(assembly);
            RegisterConversions(assembly);
        }

        /// <summary>
        /// Register a conversion function.
        /// </summary>
        /// <param name="fromUnit">The unit from which this conversion function allows conversion.</param>
        /// <param name="toUnit">The unit to which this conversion function allows conversion to.</param>
        /// <param name="conversionFunction">The unit conversion function.</param>
        /// <remarks>
        /// A unit conversion function is registered to convert from one unit to another. It will
        /// however be applied to convert from any unit of the same family of the fromUnit, to any
        /// unit family of the toUnit. For reverse conversion, a separate function must be registered.
        /// </remarks>
        public static void RegisterConversion(UnitMeasure fromUnit, UnitMeasure toUnit, ConversionFunction conversionFunction)
        {
            Instance.conversions[new UnitConversionKeySlot(fromUnit, toUnit)] = new UnitConversionValueSlot(fromUnit, toUnit, conversionFunction);
        }

        /// <summary>
        /// Registers a set of conversion functions by executing all public static void methods of 
        /// the given type. The methods are supposed to call the RegisterConversion method to register
        /// individual conversion functions.
        /// </summary>
        public static void RegisterConversions(Type unitConversionsClass)
        {
            object[] none = new object[0];
            foreach (MethodInfo method in unitConversionsClass.GetMethods(BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static))
            {
                if (method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
                {
                    method.Invoke(null, none);
                }
            }
        }

        /// <summary>
        /// Registers a set of conversion function by executing all public static void methods
        /// of public types marked with the [UnitConversionsClass] attribute in the given assembly.
        /// The methods are supposed to call the RegisterConversion method to register individual
        /// conversion functions.
        /// </summary>
        public static void RegisterConversions(Assembly assembly)
        {
            foreach (Type t in assembly.GetExportedTypes())
            {
                if (t.GetCustomAttributes(typeof(UnitConversionClassAttribute), false).Length > 0)
                {
                    RegisterConversions(t);
                }
            }
        }

        /// <summary>
        /// Event raised whenever a unit can not be resolved.
        /// </summary>
        public event UnitResolveEventHandler UnitResolve;

        /// <summary>
        /// Registers a unit.
        /// </summary>
        public static void RegisterUnit(UnitMeasure unit)
        {
            // Check precondition: unit <> null
            if (unit == null) throw new ArgumentNullException("unit");

            // Check if unit already registered:
            foreach (UnitMeasure u in Instance.allUnits)
            {
                if (ReferenceEquals(u, unit)) return;
            }

            // Register unit in allUnits:
            Instance.allUnits.Add(unit);

            // Register unit in unitsByType:
            if (!Instance.unitsByFamily.Any(x => x.Key == unit.Family))
            {
                Instance.unitsByFamily.Add(unit.Family, new List<UnitMeasure>());
            }
            Instance.unitsByFamily[unit.Family].Add(unit);


            try
            {
                Instance.unitsByType[unit.UnitType].Add(unit);
            }
            catch (KeyNotFoundException)
            {
                Instance.unitsByType[unit.UnitType] = new List<UnitMeasure>();
                Instance.unitsByType[unit.UnitType].Add(unit);
            }

            // Register unit by name and symbol:
            Instance.unitsByName[unit.Name] = unit;
            Instance.unitsBySymbol[unit.Symbol] = unit;
        }

        /// <summary>
        /// Register all public static fields of type Unit of the given class.
        /// </summary>
        public static void RegisterUnits(Type unitDefinitionClass)
        {
            foreach (FieldInfo field in unitDefinitionClass.GetFields(BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(UnitMeasure))
                {
                    RegisterUnit((UnitMeasure)field.GetValue(null));
                }
            }
        }

        /// <summary>
        /// Registers all public static fields of type Unit of classes
        /// marked with the [UnitDefinitionClass] attribute in the given
        /// assembly.
        /// </summary>
        public static void RegisterUnits(Assembly assembly)
        {
            foreach (Type t in assembly.GetExportedTypes())
            {
                if (t.GetCustomAttributes(typeof(UnitDefinitionClassAttribute), false).Length > 0)
                {
                    RegisterUnits(t);
                }
            }
        }

        #endregion Public methods - Registrations

        #region Public methods - Named units

        /// <summary>
        /// Retrieves the unit based on its name.
        /// If the unit is not found, a UnitResolve event is fired as last chance
        /// to resolve the unit.
        /// If the unit cannot be resolved, an UnknownUnitException is raised.
        /// </summary>
        public static UnitMeasure GetUnitByName(string name)
        {
            UnitMeasure result = null;

            // Try resolve unit by unitsByName:
            Instance.unitsByName.TryGetValue(name, out result);

            // Try resolve unit by UnitResolve event:
            if (result == null)
            {
                if (Instance.UnitResolve != null)
                {
                    foreach (UnitResolveEventHandler handler in Instance.UnitResolve.GetInvocationList())
                    {
                        result = handler(Instance, new ResolveEventArgs(name));
                        if (result != null)
                        {
                            RegisterUnit(result);
                            break;
                        }
                    }
                }
            }

            // Throw exception if unit resolution failed:
            if (result == null)
            {
                throw new UnknownUnitException(string.Format("No unit found named '{0}'.", name));
            }

            // Return result:
            return result;
        }

        /// <summary>
        /// Retrieves the unit based on its symbol.
        /// If the unit is not found, an UnknownUnitException is raised.
        /// </summary>
        public static UnitMeasure GetUnitBySymbol(string symbol)
        {
            UnitMeasure result = null;

            // Try resolve unit by unitsBySymbol:
            Instance.unitsBySymbol.TryGetValue(symbol, out result);

            // Throw exception if unit resolution failed:
            if (result == null)
            {
                throw new UnknownUnitException(string.Format("No unit found with symbol '{0}'.", symbol));
            }

            // Return result:
            return result;
        }

        /// <summary>
        /// Returns the unit types for which one or more units are registered.
        /// </summary>
        public static ICollection<UnitType> GetUnitTypes()
        {
            return Instance.unitsByType.Keys;
        }

        /// <summary>
        /// Returns all registered units.
        /// </summary>
        public static IList<UnitMeasure> GetUnits()
        {
            return Instance.allUnits;
        }

        /// <summary>
        /// Whether the given unit is already registered to the UnitManager.
        /// </summary>
        public static bool IsRegistered(UnitMeasure unit)
        {
            return Instance.allUnits.Contains(unit);
        }

        /// <summary>
        /// Returns all registered units of the given type.
        /// </summary>
        public static IList<UnitMeasure> GetUnits(UnitType unitType)
        {
            var list = Instance.unitsByType[unitType];
            return Instance.unitsByType[unitType];
        }
        public static IList<UnitMeasure> GetUnitsByFamily(UnitMeasure unit)
        {
            var list = Instance.unitsByFamily[unit.Family];
            return Instance.unitsByFamily[unit.Family];
        }
        /// <summary>
        /// Returns a registered unit that matches the given unit.
        /// </summary>
        /// <param name="unit">The unit for which to find a registered match.</param>
        /// <param name="selfIfNone">
        /// If true, returns the passed unit if no match is found,
        /// otherwise return null if no match is found.
        /// </param>
        /// <remarks>
        /// If the passed unit is named, the passed unit will be returned without
        /// checking if it is registered.
        /// </remarks>
        public static UnitMeasure ResolveToNamedUnit(UnitMeasure unit, bool selfIfNone)
        {
            if (unit.IsNamed) return unit;
            double factor = unit.Factor;
            if (Instance.unitsByType.ContainsKey(unit.UnitType))
            {
                foreach (UnitMeasure m in Instance.unitsByType[unit.UnitType])
                {
                    if (m.Factor == factor) return m;
                }
            }
            return selfIfNone ? unit : null;
        }

        #endregion Public methods - Named units

        #region Public methods - Unit conversions

        /// <summary>
        /// Converts the given amount to the given unit.
        /// </summary>
        public static Amount ConvertTo(Amount amount, UnitMeasure toUnit)
        {
            try
            {
                // Performance optimalization:
                if (ReferenceEquals(amount.Unit, toUnit))
                {
                    return amount;
                }

                // Perform conversion:
                if (amount.Unit.IsCompatibleTo(toUnit))
                {
                    return new Amount(amount.Value * amount.Unit.Factor / toUnit.Factor, toUnit);
                }
                else
                {
                    UnitConversionKeySlot expectedSlot = new UnitConversionKeySlot(amount.Unit, toUnit);
                    return Instance.conversions[expectedSlot].Convert(amount).ConvertedTo(toUnit);
                }
            }
            catch (KeyNotFoundException)
            {
                throw new UnitConversionException(amount.Unit, toUnit);
            }
        }

        #endregion Public methods - Unit conversions

        #region Private classes to represent slots in conversion dictionary

        /// <summary>
        /// Key slot in the internal conversions dictionary.
        /// </summary>
        private class UnitConversionKeySlot
        {
            private UnitType fromType, toType;

            public UnitConversionKeySlot(UnitMeasure from, UnitMeasure to)
            {
                fromType = from.UnitType;
                toType = to.UnitType;
            }

            public override bool Equals(object obj)
            {
                UnitConversionKeySlot other = obj as UnitConversionKeySlot;
                return fromType == other.fromType && toType == other.toType;
            }

            public override int GetHashCode()
            {
                if (fromType == null || toType == null)
                {
                    return -1;
                }
                return fromType.GetHashCode() ^ toType.GetHashCode();
            }
        }

        /// <summary>
        /// Value slot in the internal conversions dictionary.
        /// </summary>
        private class UnitConversionValueSlot
        {
            private UnitMeasure from, to;
            private ConversionFunction conversionFunction;

            public UnitConversionValueSlot(UnitMeasure from, UnitMeasure to, ConversionFunction conversionFunction)
            {
                this.from = from;
                this.to = to;
                this.conversionFunction = conversionFunction;
            }

            public Amount Convert(Amount amount)
            {
                return conversionFunction(amount.ConvertedTo(from));
            }
        }

        #endregion Private classes to represent slots in conversion dictionary	
    }
}
