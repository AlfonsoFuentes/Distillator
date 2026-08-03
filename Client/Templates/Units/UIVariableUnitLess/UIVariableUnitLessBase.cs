using Client.Services.ProjectWorkspace;
using Client.Services.Security;
using Distillator.Domain.Inputs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Shared.SolverConsecutive;
using UnitSystem;

namespace Client.Templates.Units.UIVariableUnitLess
{
    public abstract class UIVariableUnitLessBase : ComponentBase
    {
        [Inject] protected FlowsheetManager FlowsheetManager { get; set; } = null!;
        [Inject] protected VariableInputCommandHandler VariableInputCommandHandler { get; set; } = null!;
        [Inject] protected CustomAuthenticationStateProvider UserAuthProvider { get; set; } = null!;

        [Parameter] public string Label { get; set; } = string.Empty;
        [Parameter] public Variable<UnitLess> Variable { get; set; } = default!;
        [Parameter] public EventCallback<Variable<UnitLess>> VariableChanged { get; set; }
        [Parameter] public bool IsReadOnly { get; set; } = false;
        [CascadingParameter(Name = "IsProjectReadOnly")] public bool IsProjectReadOnly { get; set; }

        // Estado interno del componente
        protected string? _tempInputValue;
        protected bool _isEditing;
        protected ElementReference _inputRef;

        // Determinar si es readonly basado en flags del nuevo sistema
        protected bool IsEffectivelyReadOnly => IsProjectReadOnly || IsReadOnly || Variable?.IsCalculated == true;

        // Obtener clase CSS basada en fuente
        protected string GetSourceClass()
        {
            if (Variable == null) return "source-none";
            if (Variable.IsDefinedByUI) return "source-userinterface";
            if (Variable.IsCalculated)
                return "source-other";
            return "source-none";
        }

        // Obtener valor para display
        protected string GetDisplayValue()
        {
            if (_isEditing) return _tempInputValue ?? "";
            if (Variable == null || !Variable.IsDefined) return "<Not defined>";

            // 🔥 FIX: Para UnitLess, usar el valor numérico directamente
            var numericValue = Variable.Value?.Value;
            if (numericValue.HasValue)
            {
                return ProcessVariableHelper.FormatDisplayValue(numericValue.Value);
            }

            return "<Not defined>";
        }

        // 🔥 NUEVO: Determinar si debe mostrar tooltip de origen
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

        // ========== MÉTODOS DE INTERACCIÓN ==========

        protected void HandleInput(ChangeEventArgs e) => _tempInputValue = e.Value?.ToString();

        protected async Task HandleKeyUp(KeyboardEventArgs e)
        {
            if (IsEffectivelyReadOnly) return;

            // Delete para borrar definición
            if (e.Key == "Delete")
            {
                if (Variable != null)
                {
                    var result = VariableInputCommandHandler.Apply(new ClearVariableInputCommand<UnitLess>(Variable));
                    if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);

                    if (result.Changed)
                    {
                        FlowsheetManager.MarkFacadeStateChanged();
                    }

                    if (result.ShouldRunSimulation && FlowsheetManager != null)
                    {
                        FlowsheetManager.RunSimulation();
                    }
                }
                _isEditing = false;
                _tempInputValue = null;
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

            // Si borró todo con backspace, cancelar edición sin borrar
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
                var currentUnit = UnitLessUnits.None;

                var user = UserAuthProvider.CurrentUser;
                var result = VariableInputCommandHandler.Apply(
                    new SetVariableInputCommand<UnitLess>(
                        Variable,
                        newVal.Value,
                        currentUnit,
                        user?.Id.ToString(),
                        user?.DisplayName));
                if (VariableChanged.HasDelegate) await VariableChanged.InvokeAsync(Variable);

                if (result.Changed)
                {
                    FlowsheetManager.MarkFacadeStateChanged();
                }

                if (result.ShouldRunSimulation && FlowsheetManager != null)
                {
                    FlowsheetManager.RunSimulation();
                }
            }

            _isEditing = false;
            _tempInputValue = null;
        }

        protected async Task EnterEditMode()
        {
            if (!IsEffectivelyReadOnly)
            {
                _isEditing = true;
                if (Variable != null && Variable.IsDefined)
                {
                    // 🔥 FIX: Usar el valor numérico directo
                    var numericValue = Variable.Value?.Value;
                    _tempInputValue = numericValue.HasValue
                        ? ProcessVariableHelper.FormatDisplayValue(numericValue.Value)
                        : "";
                }
                else
                {
                    _tempInputValue = "";
                }
                StateHasChanged();
                await Task.Delay(50);
                try { await _inputRef.FocusAsync(); } catch { }
            }
        }
    }
}
