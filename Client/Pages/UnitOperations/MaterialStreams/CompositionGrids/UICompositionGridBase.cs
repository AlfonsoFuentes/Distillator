using Client.Services.ProjectWorkspace;
using Client.Services.Security;
using Distillator.Domain.Inputs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.SolverConsecutive;
using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.ControlledVariables;
using System.Globalization;
using UnitSystem;

namespace Client.Pages.UnitOperations.MaterialStreams.CompositionGrids
{
    public abstract class UICompositionGridBase : ComponentBase
    {
        [Inject] protected FlowsheetManager FlowsheetManager { get; set; } = null!;
        [Inject] protected CompositionInputCommandHandler CompositionInputCommandHandler { get; set; } = null!;
        [Inject] protected CustomAuthenticationStateProvider UserAuthProvider { get; set; } = null!;

        [Parameter] public CompositionOrchestrator Variable { get; set; } = default!;
        [Parameter] public EventCallback<CompositionOrchestrator> VariableChanged { get; set; }
        [Parameter] public EventCallback OnCompositionUpdated { get; set; }
        [CascadingParameter(Name = "IsProjectReadOnly")] public bool IsProjectReadOnly { get; set; }

        protected ComponentFacade? _editingComponent;
        protected bool _isEditingMass;
        protected bool _isEditingMolar;
        protected double? _tempInputValue;
        protected string? _rawInput;

        protected string GetCellState(ComponentFacade comp, bool isMass)
        {
            if (Variable?.Components == null) return "empty";

            var hasValue = isMass ? comp.MassFraction.IsDefined : comp.MolarFraction.IsDefined;
            var inputType = Variable.InputType;
            var source = Variable.Source;

            if (source == CompositionSource.Solver)
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

        protected static string GetUserDefinitionTooltip<T>(Variable<T> variable) where T : Amount
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

        protected async Task CommitValue(ComponentFacade comp, bool isMass)
        {
            if (IsProjectReadOnly) return;
            if (!_tempInputValue.HasValue) return;

            var user = UserAuthProvider.CurrentUser;
            var result = CompositionInputCommandHandler.Apply(
                new SetCompositionFractionCommand(
                    Variable,
                    comp,
                    isMass ? ComponentInputType.MassFraction : ComponentInputType.MolarFraction,
                    _tempInputValue.Value,
                    user?.Id.ToString(),
                    user?.DisplayName));

            ResetEditingState();
            await NotifyCompositionCommittedAsync();
            if (result.ShouldRunSimulation)
            {
                FlowsheetManager?.RunSimulation();
            }

            FlowsheetManager?.MarkFacadeStateChanged();
        }

        protected async Task ClearCell()
        {
            if (IsProjectReadOnly) return;
            var result = CompositionInputCommandHandler.Apply(new ClearCompositionInputCommand(Variable));
            ResetEditingState();
            await NotifyCompositionCommittedAsync();
            if (result.ShouldRunSimulation)
            {
                FlowsheetManager?.RunSimulation();
            }

            FlowsheetManager?.MarkFacadeStateChanged();
        }

        protected async Task NotifyCompositionCommittedAsync()
        {
            if (VariableChanged.HasDelegate)
            {
                await VariableChanged.InvokeAsync(Variable);
            }

            if (OnCompositionUpdated.HasDelegate)
            {
                await OnCompositionUpdated.InvokeAsync();
            }

        }

        protected bool IsCellReadOnly(ComponentFacade comp, bool isMass)
        {
            var state = GetCellState(comp, isMass);
            return state == "calculated" || state == "blocked";
        }

        protected ComponentInputType DetectInputType()
        {
            return Variable?.InputType ?? ComponentInputType.None;
        }

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
                return _rawInput ?? "";
            }

            var val = isMass ? comp.MassFraction.GetDisplayValue() : comp.MolarFraction.GetDisplayValue();
            var isDefined = isMass ? comp.MassFraction.IsDefined : comp.MolarFraction.IsDefined;

            return isDefined ? FormatFraction(val) : "";
        }

        protected void HandleFocus(ComponentFacade comp, bool isMass)
        {
            if (IsProjectReadOnly) return;
            if (IsCellReadOnly(comp, isMass)) return;

            _editingComponent = comp;
            _isEditingMass = isMass;
            _isEditingMolar = !isMass;

            var isDefined = isMass ? comp.MassFraction.IsDefined : comp.MolarFraction.IsDefined;

            if (isDefined)
            {
                var val = isMass ? comp.MassFraction.GetDisplayValue() : comp.MolarFraction.GetDisplayValue();
                _tempInputValue = val;
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
            if (IsProjectReadOnly) return;
            if (_editingComponent != comp) return;

            ParseRawInput();

            if (_tempInputValue.HasValue)
            {
                await CommitValue(comp, isMass);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_rawInput))
                {
                    await ClearCell();
                }
                else
                {
                    ResetEditingState();
                }
            }
        }

        protected void HandleInput(ChangeEventArgs e, ComponentFacade comp, bool isMass)
        {
            if (IsProjectReadOnly) return;
            _rawInput = e.Value?.ToString();
        }

        private void ParseRawInput()
        {
            if (double.TryParse(_rawInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                _tempInputValue = value;
            else
                _tempInputValue = null;
        }

        // 🔥 CAMBIADO DE HandleKeyDown A HandleKeyUp
        protected async Task HandleKeyUp(KeyboardEventArgs e, ComponentFacade comp, bool isMass)
        {
            if (IsProjectReadOnly) return;
            if (e.Key == "Enter")
            {
                ParseRawInput();
                await CommitValue(comp, isMass);
            }
            else if (e.Key == "Delete" && string.IsNullOrWhiteSpace(_rawInput))
            {
                await ClearCell();
            }
            else if (e.Key == "Backspace" && string.IsNullOrWhiteSpace(_rawInput))
            {
                await ClearCell();
            }
        }

        protected void ResetEditingState()
        {
            _editingComponent = null;
            _isEditingMass = false;
            _isEditingMolar = false;
            _tempInputValue = null;
            _rawInput = null;
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
