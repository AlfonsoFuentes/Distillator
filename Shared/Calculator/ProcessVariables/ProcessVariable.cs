using Shared.Calculator.Components;
using Shared.Thermodynamics.Methods;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.Calculator.ProcessVariables
{
    public enum VariableState
    {
        Empty,         // El campo está vacío, esperando entrada.
        UserDefined,   // El operador ingresó el dato a mano (Sagrado).
        CalculatedBy   // El motor o un equipo resolvió el dato (Bloqueado).
    }
    public class ProcessVariableUnitLess
    {
        // ==========================================
        // 1. ESTADO Y TOPOLOGÍA (Para la UI y el Solver)
        // ==========================================
        public VariableState State { get; private set; } = VariableState.Empty;

        // Guarda el nombre/ID del equipo o rutina que inyectó el cálculo
        public string OwnerId { get; private set; } = string.Empty;

        public bool CanBeDefinedByUI { get; private set; }

        // Propiedad mágica para Blazor: Si está calculada, la caja de texto se bloquea sola
        public bool IsReadOnly => State == VariableState.CalculatedBy;

        public string VariableName { get; private set; }
        // ==========================================
        // 2. EL DATO REAL (Tu clase Amount)
        // ==========================================
        public double Data { get; private set; }

        /// <summary>
        /// Constructor: Inicia la variable en 0, pero en estado Empty.
        /// Se le pasa la unidad por defecto con la que la UI arranca.
        /// </summary>
        public ProcessVariableUnitLess(string variableName, bool canBeDefinedByUI)
        {
            Data = 0;
            CanBeDefinedByUI = canBeDefinedByUI;
            VariableName = variableName;
        }

        // ==========================================
        // 3. MÉTODOS DE INTERACCIÓN (Setters)
        // ==========================================

        private const double Epsilon = 1e-9;
        public bool SetUserValue(double value)
        {
            if (Math.Abs(value - Data) > Epsilon || State == VariableState.Empty)
            {
                Data = value;
                State = VariableState.UserDefined;
                OwnerId = "User";
                return true;
            }
            return false;
        }

        /// <summary>
        /// Se llama cuando el motor termodinámico (o un equipo) resuelve la ecuación.
        /// </summary>
        public void SetCalculatedValue(double value, string ownerId)
        {
            Data = value;
            State = VariableState.CalculatedBy;
            OwnerId = ownerId;
        }

        /// <summary>
        /// Borra el dato. Se llama si el usuario limpia la caja de texto
        /// o si un equipo adyacente se desconecta.
        /// </summary>
        public void Reset()
        {
            State = VariableState.Empty;
            OwnerId = string.Empty;
            // No destruimos el Amount, solo decimos que su valor ya no es válido para el balance.
        }

        // ==========================================
        // 4. MÉTODOS PARA EL MOTOR TERMODINÁMICO (Getters)
        // ==========================================

        /// <summary>
        /// El Motor siempre llama a este método para asegurarse de obtener
        /// el valor en la unidad estandarizada que él necesita (ej. Kelvin o Bar),
        /// sin importar si el usuario lo ingresó en Fahrenheit o psi.
        /// </summary>
        public double GetValueAs()
        {
            if (State == VariableState.Empty)
                throw new InvalidOperationException("No se puede leer el valor de una variable Empty.");

            // Usamos el poder de tu clase Amount para que haga la conversión al vuelo
            return Data;
        }
    }
    public class ProcessVariable
    {
        // ==========================================
        // 1. ESTADO Y TOPOLOGÍA (Para la UI y el Solver)
        // ==========================================
        public VariableState State { get; private set; } = VariableState.Empty;

        // Guarda el nombre/ID del equipo o rutina que inyectó el cálculo
        public string OwnerId { get; private set; } = string.Empty;

        public bool CanBeDefinedByUI {  get; private set; }

        // Propiedad mágica para Blazor: Si está calculada, la caja de texto se bloquea sola
        public bool IsReadOnly => State == VariableState.CalculatedBy||!CanBeDefinedByUI;

        public string VariableName {  get; private set; }
        // ==========================================
        // 2. EL DATO REAL (Tu clase Amount)
        // ==========================================
        public Amount Data { get; private set; }

        /// <summary>
        /// Constructor: Inicia la variable en 0, pero en estado Empty.
        /// Se le pasa la unidad por defecto con la que la UI arranca.
        /// </summary>
        public ProcessVariable(string variableName,UnitMeasure defaultUnit, bool canBeDefinedByUI )
        {
            Data = new Amount(0.0, defaultUnit);
            CanBeDefinedByUI = canBeDefinedByUI;
            VariableName = variableName;
        }

        // Dentro de ProcessVariable
        private const double Epsilon = 1e-9; // Tolerancia de cambio

        public bool SetUserValue(double value, UnitMeasure unit)
        {
            // Obtenemos el valor nuevo en la unidad base para comparar "peras con peras"
            double newValueInBase = new Amount(value, unit).Value;
            double currentValueInBase = Data.Value;

            // Solo procedemos si el cambio es mayor a la tolerancia
            if (Math.Abs(newValueInBase - currentValueInBase) > Epsilon || State == VariableState.Empty)
            {
                Data.SetValue(value, unit);
                State = VariableState.UserDefined;
                OwnerId = "User";
                return true; // ¡Hubo un cambio real!
            }

            return false; // El valor es el mismo, no hagas nada
        }
        

        /// <summary>
        /// Se llama cuando el motor termodinámico (o un equipo) resuelve la ecuación.
        /// </summary>
        public void SetCalculatedValue(double value, UnitMeasure unit, string ownerId)
        {
            Data.SetValue(value, unit);
            State = VariableState.CalculatedBy;
            OwnerId = ownerId;
        }

        /// <summary>
        /// Borra el dato. Se llama si el usuario limpia la caja de texto
        /// o si un equipo adyacente se desconecta.
        /// </summary>
        public void Reset()
        {
            State = VariableState.Empty;
            OwnerId = string.Empty;
            // No destruimos el Amount, solo decimos que su valor ya no es válido para el balance.
        }

        // ==========================================
        // 4. MÉTODOS PARA EL MOTOR TERMODINÁMICO (Getters)
        // ==========================================

        /// <summary>
        /// El Motor siempre llama a este método para asegurarse de obtener
        /// el valor en la unidad estandarizada que él necesita (ej. Kelvin o Bar),
        /// sin importar si el usuario lo ingresó en Fahrenheit o psi.
        /// </summary>
        public double GetValueAs(UnitMeasure targetUnit)
        {
            if (State == VariableState.Empty)
                throw new InvalidOperationException("No se puede leer el valor de una variable Empty.");

            // Usamos el poder de tu clase Amount para que haga la conversión al vuelo
            return Data.ConvertedTo(targetUnit).Value;
        }
    }
    public class ProcessMethodProperty
    {
        // ==========================================
        // 1. ESTADO Y TOPOLOGÍA (UI y Solver)
        // ==========================================
        public VariableState State { get; private set; } = VariableState.Empty;

        public string OwnerId { get; private set; } = string.Empty;

        public bool CanBeDefinedByUI { get; private set; }

        // Si viene heredado de un equipo (CalculatedBy), la UI bloquea el dropdown.
        public bool IsReadOnly => State == VariableState.CalculatedBy || !CanBeDefinedByUI;

        public string VariableName { get; private set; }

        // ==========================================
        // 2. EL DATO REAL (ADN COMPLETO)
        // ==========================================
        public ThermodynamicMethodFullDto Data { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ProcessMethodProperty(string variableName, bool canBeDefinedByUI)
        {
            VariableName = variableName;
            CanBeDefinedByUI = canBeDefinedByUI;
            Data = null!; // Inicia nulo porque el estado es Empty
        }

        // ==========================================
        // 3. MÉTODOS DE INTERACCIÓN
        // ==========================================

        /// <summary>
        /// Cuando el usuario lo selecciona manualmente en la interfaz.
        /// </summary>
        public void SetUserValue(ThermodynamicMethodFullDto method)
        {
            Data = method ?? throw new ArgumentNullException(nameof(method));
            State = VariableState.UserDefined;
            OwnerId = "User";
        }

        /// <summary>
        /// Cuando un equipo o corriente aguas arriba le inyecta el método.
        /// </summary>
        public void SetCalculatedValue(ThermodynamicMethodFullDto method, string ownerId)
        {
            Data = method ?? throw new ArgumentNullException(nameof(method));
            State = VariableState.CalculatedBy;
            OwnerId = ownerId;
        }

        /// <summary>
        /// Se llama si el usuario lo borra o si el equipo que lo proveía se desconecta.
        /// </summary>
        public void Reset()
        {
            State = VariableState.Empty;
            OwnerId = string.Empty;
            Data = null!;
        }
       
        // ==========================================
        // 4. LECTURA SEGURA
        // ==========================================
        public ThermodynamicMethodFullDto GetMethod()
        {
            if (State == VariableState.Empty || Data == null)
                throw new InvalidOperationException($"El método termodinámico completo no ha sido definido para la variable '{VariableName}'.");

            return Data;
        }
    }
    public class ProcessCompositionProperty
    {
        private const double Epsilon = 1e-9;
        public VariableState State { get; private set; } = VariableState.Empty;
        public string OwnerId { get; private set; } = string.Empty;
        public bool CanBeDefinedByUI { get; private set; }

        // El "candado" dinámico para Blazor
        public bool IsReadOnly => State == VariableState.CalculatedBy || !CanBeDefinedByUI;

        private readonly List<StreamComponent> _components;
        public IReadOnlyList<StreamComponent> ComponentsList => _components;

        public ProcessCompositionProperty(List<StreamComponent> components, bool canBeDefinedByUI = true)
        {
            _components = components ?? throw new ArgumentNullException(nameof(components));
            CanBeDefinedByUI = canBeDefinedByUI;
        }

        /// <summary>
        /// Compara si un valor de fracción cambió significativamente.
        /// </summary>
        public bool HasFractionChanged(double currentVal, double newVal)
        {
            return Math.Abs(currentVal - newVal) > Epsilon || State == VariableState.Empty;
        }

        public void SetUserComposition()
        {
            State = VariableState.UserDefined;
            OwnerId = "User";
        }

        public void SetCalculatedComposition(List<StreamComponent> calculatedComponents, string ownerId)
        {
            _components.Clear();
            if (calculatedComponents != null) _components.AddRange(calculatedComponents);
            State = VariableState.CalculatedBy;
            OwnerId = ownerId;
        }

        public void Reset()
        {
            State = VariableState.Empty;
            OwnerId = string.Empty;
        }
        public void Clear()
        {
            _components.Clear();
            State = VariableState.Empty;
            OwnerId = string.Empty;
        }
        public void InitializeBaseStructure(List<StreamComponent> baseComponents)
        {
            // IMPORTANTE: No hacemos _components = baseComponents;
            // porque romperíamos la referencia que tiene MixtureBase.
            _components.Clear();
            if (baseComponents != null)
            {
                _components.AddRange(baseComponents);
            }
            State = VariableState.Empty;
            OwnerId = string.Empty;
        }
    }
}
