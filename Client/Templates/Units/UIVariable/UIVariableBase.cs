using Client.Services.ProjectWorkspace;
using Client.Services.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.SolverConsecutive;
using Shared.Thermodynamics.ControlledVariables;
using UnitSystem;

namespace Client.Templates.Units.UIVariable
{

    public abstract class UIVariableBase<T> : ComponentBase, IDisposable where T : Amount
    {
        [Inject] protected FlowsheetManager FlowsheetManager { get; set; } = null!;
        [Inject] protected CustomAuthenticationStateProvider UserAuthProvider { get; set; } = null!;
        [Parameter] public string Label { get; set; } = string.Empty;
        [Parameter] public Variable<T> Variable { get; set; } = default!;
        [Parameter] public EventCallback<Variable<T>> VariableChanged { get; set; }
        [Parameter] public bool IsReadOnly { get; set; } = false;
        [CascadingParameter(Name = "IsProjectReadOnly")] public bool IsProjectReadOnly { get; set; }

        protected string? _tempInputValue;
        protected bool _isEditing;
        protected bool _showUnitSelector;
        protected ElementReference _inputRef;
        private Variable<T>? _subscribedVariable;

        protected override void OnParametersSet()
        {
            if (_subscribedVariable != Variable)
            {
                if (_subscribedVariable != null)
                    _subscribedVariable.ValueChanged -= OnVariableValueChanged;
                _subscribedVariable = Variable;
                if (_subscribedVariable != null)
                    _subscribedVariable.ValueChanged += OnVariableValueChanged;
            }
        }

        private void OnVariableValueChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            if (_subscribedVariable != null)
                _subscribedVariable.ValueChanged -= OnVariableValueChanged;
        }

        // 🔥 NUEVO: Determinar si es readonly basado en flags del nuevo sistema
        protected bool IsEffectivelyReadOnly => IsProjectReadOnly || IsReadOnly || Variable?.IsCalculated == true;

        // 🔥 NUEVO: Obtener clase CSS basada en fuente (mapeo de flags a string)
        protected string GetSourceClass()
        {
            if (Variable == null) return "source-none";
            if (Variable.IsDefinedByUI) return "source-userinterface";
            if (Variable.IsCalculated )
                return "source-other";
            return "source-none";
        }

        // 🔥 NUEVO: Obtener valor para display usando el nuevo método
        protected string GetDisplayValue()
        {
            if (_isEditing) return _tempInputValue ?? "";
            if (Variable == null || !Variable.IsDefined) return "<Not defined>";
            return ProcessVariableHelper.FormatDisplayValue(Variable.GetDisplayValue());
        }

        protected bool ShouldShowSourceTooltip()
        {
            if (Variable == null || !Variable.IsDefined) return false;

            return Variable.IsDefinedByUI || Variable.IsCalculated;
        }

        // 🔥 NUEVO: Obtener texto del tooltip basado en la fuente
        protected string GetSourceTooltipText()
        {
            if (Variable == null || !Variable.IsDefined) return "";

            return Variable.DataProcedence switch
            {
                VariableDefinedBy.StreamCalculated => "Calculated by: Stream",
                VariableDefinedBy.Solver => "Calculated by: Solver",
                VariableDefinedBy.Specification => "Calculated by: Equipment",
                VariableDefinedBy.UserInput => GetUserDefinitionTooltip(),
               
                _ => ""
            };
        }

        protected string GetUserDefinitionTooltip()
        {
            var userName = string.IsNullOrWhiteSpace(Variable.DefinedByUserName)
                ? "User"
                : Variable.DefinedByUserName;

            if (!Variable.DefinedAtUtc.HasValue)
            {
                return $"Defined by: {userName}";
            }

            var localDate = Variable.DefinedAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return $"Defined by: {userName}\n{localDate}";
        }

        protected void HandleInput(ChangeEventArgs e) => _tempInputValue = e.Value?.ToString();

        protected async Task HandleKeyUp(KeyboardEventArgs e)
        {
            if (IsEffectivelyReadOnly) return;

            // 🔥 Delete para borrar definición (comportamiento idéntico)
            if (e.Key == "Delete")
            {
                Variable?.ClearFromUI(); // 🔥 NUEVO: ClearFromUI en lugar de ClearValue
                if (FlowsheetManager != null)
                {
                    // Usa 'await' si tu interfaz dice: Task RunSimulation();
                    // Usa llamada normal si dice: void RunSimulation();
                    FlowsheetManager.RunSimulation();
                }
                _isEditing = false;
                _tempInputValue = null;
                if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
                return;
            }

            // Enter para confirmar
            
            if (e.Key == "Enter" || e.Key == "NumpadEnter")
            {
                await CommitValue();
            }
            // Escape para cancelar
            else if (e.Key == "Escape")
            {
                _isEditing = false;
                _tempInputValue = null;
                StateHasChanged();
            }
        }

        protected async Task CommitValue()
        {
            if (IsEffectivelyReadOnly || !_isEditing) return;

            // Si el usuario hizo clic y se fue sin tocar nada
            if (_tempInputValue == null)
            {
                _isEditing = false;
                StateHasChanged();
                return;
            }

            // Si borró todo con backspace (no con Delete), cancelar edición sin borrar
            if (string.IsNullOrWhiteSpace(_tempInputValue))
            {
                _tempInputValue = null;
                _isEditing = false;
                StateHasChanged();
                return;
            }

            // Parsear y guardar valor
            var newVal = ProcessVariableHelper.ParseInput(_tempInputValue);
            if (newVal.HasValue && Variable?.Value != null)
            {
                var currentUnit = Variable.UnitForUI; // 🔥 NUEVO: UnitForUI en lugar de GetPreferredUnit
                if (currentUnit != null && newVal.HasValue)
                {
                    // Usar el factory para crear nueva instancia del tipo T

                    var newValue = Variable.Value;
                    newValue.SetValue(newVal.Value, currentUnit); // 🔥 NUEVO: SetValue con valor y unidad

                    var user = UserAuthProvider.CurrentUser;
                    Variable.SetValueFromUI(newValue, user?.Id.ToString(), user?.DisplayName);
                    if (FlowsheetManager != null)
                    {
                        // Usa 'await' si tu interfaz dice: Task RunSimulation();
                        // Usa llamada normal si dice: void RunSimulation();
                        FlowsheetManager.RunSimulation();
                    }
                }
            }

            _isEditing = false;
            _tempInputValue = null;
            if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
        }

        protected async Task EnterEditMode()
        {
            if (!IsEffectivelyReadOnly)
            {
                _isEditing = true;
                if (Variable != null && Variable.IsDefined)
                {
                    _tempInputValue = ProcessVariableHelper.FormatDisplayValue(Variable.GetDisplayValue());
                }
                else
                {
                    _tempInputValue = "";
                }
                StateHasChanged();
                await Task.Delay(50); // Dar tiempo a Blazor para renderizar el input
                try { await _inputRef.FocusAsync(); } catch { }
            }
        }

        protected async Task SelectUnit(UnitMeasure unit)
        {
            if (Variable == null) return;
            Variable.ChangeUnitForUI(unit); // 🔥 NUEVO: ChangeUnitForUI en lugar de SetPreferredUnit
            _showUnitSelector = false;
            if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);
            StateHasChanged(); // Forzar re-render para actualizar display
        }
    }
}
