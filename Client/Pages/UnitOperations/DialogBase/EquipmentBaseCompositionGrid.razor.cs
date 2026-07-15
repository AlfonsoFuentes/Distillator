using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.ProcessFlowDiagram;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.ControlledVariables;
using System.Globalization;
using UnitSystem;

namespace Client.Pages.UnitOperations.DialogBase
{
    public partial class EquipmentBaseCompositionGrid
    {
        [Parameter] public List<EquipmentPort> Ports { get; set; } = new();
        [Parameter] public EventCallback OnEquipmentUpdated { get; set; }
        [CascadingParameter(Name = "IsProjectReadOnly")] public bool IsProjectReadOnly { get; set; }



        private IFacadeStream? _editingFacade;
        private ComponentFacade? _editingComponent;
        private bool _isEditingMass;
        private bool _isEditingMolar;
        private bool _isEditingGL;
        private double? _tempInputValue;
        private string? _rawInput;

        // 🔥 Verificar si es mezcla etanol/agua
        private bool IsEthanolWaterMixture(IFacadeStream facade)
        {
            if (facade?.Composition?.Components == null) return false;

            var components = facade.Composition.Components;
            var hasEthanol = components.Any(c => c.Name.Equals("Ethanol", StringComparison.OrdinalIgnoreCase));
            var hasWater = components.Any(c => c.Name.Equals("Water", StringComparison.OrdinalIgnoreCase));

            return hasEthanol && hasWater && components.Count == 2;
        }

        // 🔥 Determinar si debe mostrar tooltip
        private bool HasTooltip(string state)
        {
            return state == "calculated" || state == "blocked";
        }

        // 🔥 Obtener tooltip para flows
        private string GetFlowTooltip<T>(Variable<T>? variable) where T : Amount
        {
            if (variable == null) return "Not defined";
            if (!string.IsNullOrWhiteSpace(variable.Source) && variable.Source != "Undefined")
                return $"Calculated by: {variable.Source}";
            return "Calculated from mixture";
        }

        // 🔥 Lógica correcta para bloqueo de °GL
        private bool ShouldGlBeReadOnly(IFacadeStream facade)
        {
            var composition = facade.Composition;
            if (composition == null) return true;

            // Si el usuario definió por Molar%, el °GL está calculado
            if (composition.InputType == ComponentInputType.MolarFraction) return true;

            // Si el solver calculó Y el usuario no definió nada, el °GL está calculado
            if (composition.Source == CompositionSource.Solver && composition.InputType == ComponentInputType.None) return true;

            // En cualquier otro caso, el °GL es editable
            return false;
        }

        // 🔥 Obtener tooltip para °GL
        private string GetGlTooltip(IFacadeStream facade)
        {
            var composition = facade.Composition;
            if (composition == null) return "";

            if (composition.InputType == ComponentInputType.MolarFraction)
                return "Calculated by: Molar Fraction";

            if (composition.Source == CompositionSource.Solver && composition.InputType == ComponentInputType.None)
                return "Calculated by: Solver";

            var ethanol = facade.Composition.Components
                .FirstOrDefault(component => component.Name.Equals("Ethanol", StringComparison.OrdinalIgnoreCase));
            if (ethanol?.MassFraction.IsDefinedByUI == true)
            {
                return GetUserDefinitionTooltip(ethanol.MassFraction);
            }

            return "";
        }

        private double? GetGlValue(IFacadeStream facade, ComponentFacade comp)
        {
            if (comp.MassFraction.IsDefined && comp.MassFraction.GetDisplayValue() > 0 && comp.MassFraction.GetDisplayValue() <= 100)
            {
                return GetGLEtanol(comp.MassFraction.GetDisplayValue());
            }
            return null;
        }

        private string GetGlInputValue2(IFacadeStream facade, ComponentFacade comp)
        {
            bool isEditingThis = _editingFacade == facade && _editingComponent == comp && _isEditingGL;
            if (isEditingThis && _tempInputValue.HasValue)
                return _tempInputValue.Value.ToString("F1", CultureInfo.InvariantCulture);

            double? val = comp.MassFraction.IsDefined ? GetGLEtanol(comp.MassFraction.GetDisplayValue()) : null;
            return val.HasValue ? val.Value.ToString("F1", CultureInfo.InvariantCulture) : "";
        }

        private string GetCellState(IFacadeStream facade, ComponentFacade comp, bool isMass)
        {
            var composition = facade.Composition;
            if (composition == null) return "empty";

            var hasValue = isMass ? comp.MassFraction.IsDefined : comp.MolarFraction.IsDefined;
            var inputType = composition.InputType;
            var source = composition.Source;

            // 🔥 Si viene del solver Y no hay input de usuario, TODO está calculado
            if (source == CompositionSource.Solver && inputType == ComponentInputType.None)
            {
                return hasValue ? "calculated" : "empty";
            }

            if (IsProjectReadOnly)
            {
                return hasValue ? "calculated" : "empty";
            }

            if (inputType == ComponentInputType.None)
                return hasValue ? "editable" : "empty";

            if (inputType == ComponentInputType.MassFraction)
                return isMass ? (hasValue ? "editable" : "empty") : (hasValue ? "calculated" : "blocked");

            if (inputType == ComponentInputType.MolarFraction)
                return !isMass ? (hasValue ? "editable" : "empty") : (hasValue ? "calculated" : "blocked");

            return "empty";
        }

        private bool IsCellReadOnly(string state)
        {
            return state == "calculated" || state == "blocked";
        }

        private string? GetCellTooltip(IFacadeStream facade, ComponentFacade comp, bool isMass)
        {
            var composition = facade.Composition;
            if (composition == null) return null;

            var state = GetCellState(facade, comp, isMass);
            var variable = isMass ? comp.MassFraction : comp.MolarFraction;

            if (variable.IsDefinedByUI)
            {
                return GetUserDefinitionTooltip(variable);
            }

            if (state == "calculated")
            {
                if (!string.IsNullOrWhiteSpace(variable.Source) && variable.Source != "Undefined")
                    return $"Calculated by: {variable.Source}";
                return "Calculated from mixture composition";
            }

            if (state == "blocked")
                return "Blocked: Define via " + (isMass ? "Mole %" : "Mass %");

            return null;
        }

        private static string GetUserDefinitionTooltip<T>(Variable<T> variable) where T : Amount
        {
            var userName = string.IsNullOrWhiteSpace(variable.DefinedByUserName)
                ? "User"
                : variable.DefinedByUserName;

            if (!variable.DefinedAtUtc.HasValue)
            {
                return $"Defined by: {userName}";
            }

            var localDate = variable.DefinedAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return $"Defined by: {userName}\n{localDate}";
        }

        private string FormatFraction(double? fraction)
        {
            if (!fraction.HasValue) return "";
            var val = fraction.Value;
            return val == 0 ? "0.00" : (Math.Abs(val) >= 1.0 ? val.ToString("F2") : val.ToString("F6").TrimEnd('0').TrimEnd('.'));
        }

        private string FormatFlow(double? value)
        {
            if (!value.HasValue) return "-";
            var val = value.Value;
            return val == 0 ? "0.00" : (Math.Abs(val) >= 1.0 ? val.ToString("F2") : val.ToString("F6").TrimEnd('0').TrimEnd('.'));
        }
        private string GetInputValue(IFacadeStream facade, ComponentFacade comp, bool isMass)
        {
            bool isEditingThis = _editingFacade == facade && _editingComponent == comp && ((isMass && _isEditingMass) || (!isMass && _isEditingMolar));

            // 🔥 Si estamos editando, mostramos EXACTAMENTE lo que se tecleó, sin formatear
            if (isEditingThis) return _rawInput ?? "";

            var val = isMass ? comp.MassFraction.GetDisplayValue() : comp.MolarFraction.GetDisplayValue();
            var isDefined = isMass ? comp.MassFraction.IsDefined : comp.MolarFraction.IsDefined;

            return isDefined ? FormatFraction(val) : "";
        }

        private string GetGlInputValue(IFacadeStream facade, ComponentFacade comp)
        {
            bool isEditingThis = _editingFacade == facade && _editingComponent == comp && _isEditingGL;

            // 🔥 Lo mismo para los °GL
            if (isEditingThis) return _rawInput ?? "";

            double? val = comp.MassFraction.IsDefined ? GetGLEtanol(comp.MassFraction.GetDisplayValue()) : null;
            return val.HasValue ? val.Value.ToString("F1", CultureInfo.InvariantCulture) : "";
        }
        private string GetInputValue2(IFacadeStream facade, ComponentFacade comp, bool isMass)
        {
            bool isEditingThis = _editingFacade == facade && _editingComponent == comp && ((isMass && _isEditingMass) || (!isMass && _isEditingMolar));
            if (isEditingThis && _tempInputValue.HasValue)
                return FormatFraction(_tempInputValue)  ;

            var val = isMass ? comp.MassFraction.GetDisplayValue() : comp.MolarFraction.GetDisplayValue();
            var isDefined = isMass ? comp.MassFraction.IsDefined : comp.MolarFraction.IsDefined;

            var result = isDefined ? FormatFraction(val) : "";
            return result;
        }
        private void HandleFocus(IFacadeStream facade, ComponentFacade comp, bool isMass, bool isGL)
        {
            if (IsProjectReadOnly) return;
            if (isGL && ShouldGlBeReadOnly(facade)) return;

            var state = GetCellState(facade, comp, isMass);
            if (IsCellReadOnly(state)) return;

            _editingFacade = facade;
            _editingComponent = comp;
            _isEditingGL = isGL;
            _isEditingMass = !isGL && isMass;
            _isEditingMolar = !isGL && !isMass;

            if (isGL)
            {
                _tempInputValue = comp.MassFraction.IsDefined ? GetGLEtanol(comp.MassFraction.GetDisplayValue()) : null;
                // 🔥 Inicializa el string crudo con solo 1 decimal
                _rawInput = _tempInputValue.HasValue ? _tempInputValue.Value.ToString("F1", CultureInfo.InvariantCulture) : "";
            }
            else
            {
                var isDefined = isMass ? comp.MassFraction.IsDefined : comp.MolarFraction.IsDefined;
                if (isDefined)
                {
                    var val = isMass ? comp.MassFraction.GetDisplayValue() : comp.MolarFraction.GetDisplayValue();
                    _tempInputValue = val;
                    // 🔥 Inicializa el string crudo respetando tu lógica de FormatFraction
                    _rawInput = FormatFraction(val);
                }
                else
                {
                    _tempInputValue = null;
                    _rawInput = "";
                }
            }
        }
       
        private void HandleInput(ChangeEventArgs e)
        {
            if (IsProjectReadOnly) return;
            // 🔥 SOLO guardamos lo que el usuario escribe, el parseo lo haremos al final
            _rawInput = e.Value?.ToString();
        }

        // 🔥 Método auxiliar que convierte el string a número solo cuando presionas Enter o das Clic fuera
        private void ParseRawInput()
        {
            if (double.TryParse(_rawInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                _tempInputValue = value;
            else
                _tempInputValue = null;
        }
       
        private async Task HandleKeyDown(KeyboardEventArgs e, IFacadeStream facade, ComponentFacade comp, bool isMass, bool isGL)
        {
            if (IsProjectReadOnly) return;
            if (e.Key == "Enter")
            {
                ParseRawInput(); // Convertimos el string a double
                await CommitValue(facade, comp, isMass, isGL);
            }
            else if (e.Key == "Delete")
            {
                await ClearCell(facade, comp);
            }
        }

        private async Task HandleBlur(IFacadeStream facade, ComponentFacade comp, bool isMass, bool isGL)
        {
            if (IsProjectReadOnly) return;
            // 🔥 FIX: Evita el bug donde borrar la edición limpiaba todo. 
            // Si la celda ya no es la activa (porque el Enter ya reseteó el estado), cancelamos.
            if (_editingFacade != facade || _editingComponent != comp) return;

            ParseRawInput(); // Convertimos el string a double

            if (_tempInputValue.HasValue)
            {
                await CommitValue(facade, comp, isMass, isGL);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_rawInput))
                    await ClearCell(facade, comp);
                else
                    ResetEditingState(); // Cancelamos la edición si escribió letras o caracteres inválidos
            }
        }
        private async Task HandleKeyDown2(KeyboardEventArgs e, IFacadeStream facade, ComponentFacade comp, bool isMass, bool isGL)
        {
            if (IsProjectReadOnly) return;
            if (e.Key == "Enter") await CommitValue(facade, comp, isMass, isGL);
            else if (e.Key == "Delete" || e.Key == "Backspace") await ClearCell(facade, comp);
        }

       
        private async Task CommitValue(IFacadeStream facade, ComponentFacade comp, bool isMass, bool isGL)
        {
            if (IsProjectReadOnly) return;
            if (!_tempInputValue.HasValue) return;
            var composition = facade.Composition;
            if (composition == null) return;

            if (isGL)
            {
                var massFraction = CalcularMasicoDesdeGL(_tempInputValue.Value);
                var water = composition.Components.FirstOrDefault(c => c.Name.Equals("Water", StringComparison.OrdinalIgnoreCase));
                if (water != null)
                {
                    var user = AuthProvider.CurrentUser;
                    comp.MassFraction.SetValueFromUI(new Percentage(massFraction, PercentageUnits.Percentage), user?.Id.ToString(), user?.DisplayName);
                    water.MassFraction.SetValueFromUI(new Percentage(100 - massFraction, PercentageUnits.Percentage), user?.Id.ToString(), user?.DisplayName);
                    composition.InputType = ComponentInputType.MassFraction;
                }
            }
            else
            {
                var user = AuthProvider.CurrentUser;
                if (isMass)
                    comp.MassFraction.SetValueFromUI(new Percentage(_tempInputValue.Value, PercentageUnits.Percentage), user?.Id.ToString(), user?.DisplayName);
                else
                    comp.MolarFraction.SetValueFromUI(new Percentage(_tempInputValue.Value, PercentageUnits.Percentage), user?.Id.ToString(), user?.DisplayName);

                composition.InputType = isMass ? ComponentInputType.MassFraction : ComponentInputType.MolarFraction;
            }

            var sum = composition.Components
                .Sum(c => (composition.InputType == ComponentInputType.MassFraction)
                    ? c.MassFraction.GetDisplayValue()
                    : c.MolarFraction.GetDisplayValue());

            if (sum >= 99 && sum <= 101)
            {
                FSM.RunSimulation();
            }

            ResetEditingState();
            if (OnEquipmentUpdated.HasDelegate) await OnEquipmentUpdated.InvokeAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task ClearCell(IFacadeStream facade, ComponentFacade comp)
        {
            if (IsProjectReadOnly) return;
            var composition = facade.Composition;
            if (composition == null) return;

            composition.Clear();
            composition.InputType = ComponentInputType.None;
            FSM.RunSimulation();

            ResetEditingState();
            if (OnEquipmentUpdated.HasDelegate) await OnEquipmentUpdated.InvokeAsync();
            await InvokeAsync(StateHasChanged);
        }

        private void ResetEditingState()
        {
            _editingFacade = null;
            _editingComponent = null;
            _isEditingMass = false;
            _isEditingMolar = false;
            _isEditingGL = false;
            _tempInputValue = null;
            _rawInput = null; // 🔥 MUY IMPORTANTE LIMPIARLO AQUÍ
        }

        private IFacadeStream? GetFacadeForPort(EquipmentPort port)
        {
            if (!port.ConnectedElementId.HasValue) return null;
            return FSM.GetFacadeForConnectedId(port.ConnectedElementId.Value);
        }

       

        private List<string> GetDistinctComponents()
        {
            var allComps = new HashSet<string>();
            foreach (var port in Ports)
            {
                var facade = GetFacadeForPort(port);
                if (facade?.Composition?.Components != null)
                    foreach (var c in facade.Composition.Components) allComps.Add(c.Name);
            }
            return allComps.OrderBy(c => c == "Ethanol" ? 0 : c == "Water" ? 1 : 2).ThenBy(c => c).ToList();
        }

        private ComponentFacade? GetComponent(IFacadeStream facade, string compName) =>
            facade.Composition.Components?.FirstOrDefault(c => c.Name.Equals(compName, StringComparison.OrdinalIgnoreCase));

        private double CalcularMasicoDesdeGL(double GL)
        {
            if (GL >= 100) return 100; if (GL <= 0) return 0;
            return ((1.881755917e-9 * Math.Pow(GL, 4)) - (1.161368119e-7 * Math.Pow(GL, 3)) + (1.36632945e-5 * Math.Pow(GL, 2)) + (7.8756832e-3 * GL)) * 100;
        }

        private double GetGLEtanol(double masico)
        {
            if (masico <= 0) return 0; if (masico >= 100) return 100;
            double sup = 100, inf = 0, GL = 0;
            for (int i = 0; i < 60; i++)
            {
                GL = (sup + inf) / 2;
                double pm = CalcularMasicoDesdeGL(GL);
                if (Math.Abs(pm - masico) < 1e-4) break;
                if (pm > masico) sup = GL; else inf = GL;
            }
            return GL;
        }

    }
}
