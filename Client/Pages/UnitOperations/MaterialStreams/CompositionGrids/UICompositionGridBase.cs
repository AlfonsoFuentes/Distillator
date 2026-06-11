using Client.Services.EquipmentManagers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.ControlledVariables;
using System.Globalization;
using UnitSystem;

namespace Client.Pages.UnitOperations.MaterialStreams.CompositionGrids
{
    public abstract class UICompositionGridBase : ComponentBase
    {
        [Inject] protected WorkspaceManager _WM { get; set; } = null!;

        [Parameter] public CompositionOrchestrator Variable { get; set; } = default!;
        [Parameter] public EventCallback<CompositionOrchestrator> VariableChanged { get; set; }

        protected ComponentFacade? _editingComponent;
        protected bool _isEditingMass;
        protected bool _isEditingMolar;
        protected double? _tempInputValue;
        protected string? _rawInput;
        // ─────────────────────────────────────────────────────────
        // 🔹 MANEJO DE ESTADOS (STATE MACHINE) - SIMPLIFICADO
        // ─────────────────────────────────────────────────────────

        protected string GetCellState(ComponentFacade comp, bool isMass)
        {
            if (Variable?.Components == null) return "empty";

            var hasValue = isMass ? comp.MassFraction.IsDefined : comp.MolarFraction.IsDefined;
            var inputType = Variable.InputType;
            var source = Variable.Source; // 🔥 USAR Source del orchestrator

            // 🔥 Si viene del solver, TODO está calculado
            if (source == CompositionSource.Solver)
            {
                return hasValue ? "calculated" : "empty";
            }

            // Si no hay input type definido
            if (inputType == ComponentInputType.None)
                return hasValue ? "editable" : "empty";

            // Reglas de interbloqueo termodinámico
            if (inputType == ComponentInputType.MassFraction)
            {
                return isMass
                    ? (hasValue ? "editable" : "empty")
                    : (hasValue ? "calculated" : "blocked");
            }

            if (inputType == ComponentInputType.MolarFraction)
            {
                return !isMass
                    ? (hasValue ? "editable" : "empty")
                    : (hasValue ? "calculated" : "blocked");
            }

            return "empty";
        }

        protected string? GetCellTooltip(ComponentFacade comp, bool isMass)
        {
            var state = GetCellState(comp, isMass);

            if (state == "calculated")
            {
                // 🔥 Usar Source de la variable para mostrar tooltip específico
                var variable = isMass ? comp.MassFraction : comp.MolarFraction;
                if (!string.IsNullOrWhiteSpace(variable.Source) && variable.Source != "Undefined")
                    return $"Calculated by: {variable.Source}";
                return "Calculated from mixture composition";
            }

            if (state == "blocked")
                return "Blocked: Define via " + (isMass ? "Mole %" : "Mass %");

            return null;
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 COMMIT Y CLEAR - SIMPLIFICADOS
        // ─────────────────────────────────────────────────────────

        protected async Task CommitValue(ComponentFacade comp, bool isMass)
        {
            if (!_tempInputValue.HasValue) return;

            // 1. Definir el valor ingresado en el componente
            if (isMass)
                comp.MassFraction.SetValueFromUI(new Percentage(_tempInputValue.Value, PercentageUnits.Percentage));
            else
                comp.MolarFraction.SetValueFromUI(new Percentage(_tempInputValue.Value, PercentageUnits.Percentage));

            // 2. Actualizar InputType en la composición
            Variable.InputType = isMass ? ComponentInputType.MassFraction : ComponentInputType.MolarFraction;

            // 3. VALIDACIÓN CRÍTICA: ¿La suma de TODOS los componentes da ~100%?
            var sum = Variable.Components
                .Sum(c => isMass ? c.MassFraction.GetDisplayValue() : c.MolarFraction.GetDisplayValue());

            // 4. SI suma ≈ 100% → disparar cálculo
            if (sum >= 99 && sum <= 101)
            {
                _WM?.RunSimulation();
            }

            ResetEditingState();
            if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
        }

        protected async Task ClearCell()
        {
            // 🔥 Limpiar toda la composición
            Variable.Clear();

            // Disparar el solver después de limpiar
            _WM?.RunSimulation();

            ResetEditingState();
            if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
        }

        protected bool IsCellReadOnly(ComponentFacade comp, bool isMass)
        {
            var state = GetCellState(comp, isMass);
            return state == "calculated" || state == "blocked";
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 DETECCIÓN DE INPUT TYPE
        // ─────────────────────────────────────────────────────────

        protected ComponentInputType DetectInputType()
        {
            // 🔥 Simplemente retornar el InputType del orchestrator
            return Variable?.InputType ?? ComponentInputType.None;
        }

        // ─────────────────────────────────────────────────────────
        // 🔹 FORMATOS
        // ─────────────────────────────────────────────────────────

        protected string FormatFraction(double? fraction)
        {
            if (!fraction.HasValue) return "<Not defined>";
            var val = fraction.Value;
            return val == 0 ? "0.00" : (Math.Abs(val) >= 1.0 ? val.ToString("F2", CultureInfo.InvariantCulture) : val.ToString("F6", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.'));
        }

        protected string FormatFlow(double? value)
        {
            if (!value.HasValue) return "";
            var val = value.Value;
            return val == 0 ? "0.00" : (Math.Abs(val) >= 1.0 ? val.ToString("F2", CultureInfo.InvariantCulture) : val.ToString("F6", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.'));
        }
        protected string GetInputValue(ComponentFacade comp, bool isMass)
        {
            bool isEditingThis = _editingComponent == comp && ((isMass && _isEditingMass) || (!isMass && _isEditingMolar));

            if (isEditingThis)
            {
                // 🔥 Retorna el texto crudo sin formatear para no mover el cursor
                return _rawInput ?? "";
            }

            var val = isMass ? comp.MassFraction.GetDisplayValue() : comp.MolarFraction.GetDisplayValue();
            var isDefined = isMass ? comp.MassFraction.IsDefined : comp.MolarFraction.IsDefined;

            return isDefined ? FormatFraction(val) : "";
        }


        protected void HandleFocus(ComponentFacade comp, bool isMass)
        {
            if (IsCellReadOnly(comp, isMass)) return;

            _editingComponent = comp;
            _isEditingMass = isMass;
            _isEditingMolar = !isMass;

            var isDefined = isMass ? comp.MassFraction.IsDefined : comp.MolarFraction.IsDefined;

            if (isDefined)
            {
                var val = isMass ? comp.MassFraction.GetDisplayValue() : comp.MolarFraction.GetDisplayValue();
                _tempInputValue = val;

                // 🔥 FIX: Usamos tu FormatFraction para respetar los decimales (evitamos los miles de decimales)
                _rawInput = FormatFraction(val);
            }
            else
            {
                _tempInputValue = null;
                _rawInput = "";
            }
        }

        protected async Task HandleBlur(ComponentFacade comp, bool isMass)
        {
            // 🔥 FIX: Si ya no estamos editando este componente (porque el "Enter" ya limpió el estado), cancelamos el Blur.
            if (_editingComponent != comp) return;

            ParseRawInput(); // Convertimos el string a double

            if (_tempInputValue.HasValue)
            {
                await CommitValue(comp, isMass);
            }
            else
            {
                // Si el usuario dejó la celda vacía, limpiamos
                if (string.IsNullOrWhiteSpace(_rawInput))
                {
                    await ClearCell();
                }
                else
                {
                    // Si escribió algo inválido, cancelamos
                    ResetEditingState();
                }
            }
        }


        protected void HandleInput(ChangeEventArgs e, ComponentFacade comp, bool isMass)
        {
            // 🔥 Solo guardamos lo que el usuario escribe, sin evaluar matemáticamente
            _rawInput = e.Value?.ToString();
        }

        private void ParseRawInput()
        {
            if (double.TryParse(_rawInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                _tempInputValue = value;
            else
                _tempInputValue = null;
        }

        protected async Task HandleKeyDown(KeyboardEventArgs e, ComponentFacade comp, bool isMass)
        {
            if (e.Key == "Enter")
            {
                ParseRawInput(); // Convertimos el string a double
                await CommitValue(comp, isMass);
            }
            // Nota: Removí el borrado por Delete/Backspace aquí para que no interfiera 
            // cuando estás editando texto normalmente (borrando un solo número).
            // Si borras todo el input, el Blur se encargará de vaciar la celda.
        }
         
        protected void ResetEditingState()
        {
            _editingComponent = null;
            _isEditingMass = false;
            _isEditingMolar = false;
            _tempInputValue = null;
            _rawInput = null; // 🔥 Limpiar el string crudo
        }
   

        protected IEnumerable<ComponentFacade> GetSortedComponents()
        {
            if (Variable?.Components == null) return Enumerable.Empty<ComponentFacade>();
            var components = Variable.Components.ToList();
            var water = components.FirstOrDefault(c => c.Name.Equals("Water", StringComparison.OrdinalIgnoreCase));
            if (water != null) { components.Remove(water); components.Add(water); }
            return components;
        }
    }
   

   
}
