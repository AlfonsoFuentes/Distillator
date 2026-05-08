using Shared.Thermodynamics.ControlledVariables;
using UnitSystem;


public class NewControlledVariableAmount<T> : INewVariableAmount<T> where T : Amount
{
    private readonly Func<double, UnitMeasure, T> _factory;
    public Action? OnPropagateVariable { get; set; }
    public double InitValue { get; }
    public NewControlledVariableAmount(T _Value,
        UnitMeasure unitUI,
        UnitMeasure unitSolver,
        Func<double, UnitMeasure, T> factory, double _initvalue)
    {
        Value = _Value;
        UnitForUI = unitUI;
        UnitForSolver = unitSolver;
        _factory = factory;
        InitValue = _initvalue;
    }
    public NewControlledVariableAmount(T _Value,
       UnitMeasure unitUI,
       UnitMeasure unitSolver,
       Func<double, UnitMeasure, T> factory)
    {
        Value = _Value;
        UnitForUI = unitUI;
        UnitForSolver = unitSolver;
        _factory = factory;
        InitValue = 0;
    }

    public double? SpecifiedValue { get; protected set; }
    public UnitMeasure UnitForUI { get; private set; }
    public double SolverValue { get; set; }

    public UnitMeasure UnitForSolver { get; }

    public void SetValueFromUI(T value)
    {
        double v = value.GetValue(UnitForSolver);
        Value = value;

        SolverValue = v;
        SpecifiedValue = v;
        IsSpecifiedbyUI = true;
        OnSetMaterialStreamValue?.Invoke();
        OnGoToLocalCalculation?.Invoke();
        OnPropagateVariable?.Invoke();
        OnExecuteSolver?.Invoke();
    }
    public void SetValueFromLocalCalculatedVariable(T value)
    {
        double v = value.GetValue(UnitForSolver);
        Value = value;
        SolverValue = v;
        SpecifiedValue = v;
        IsEspecifiedbyLocalCalculation = true;
        OnSetMaterialStreamValue?.Invoke();
        OnAddLocalCalculatedVariable?.Invoke(this);
        OnPropagateVariable?.Invoke();

    }

    public void SetValueFromSolver(double value)
    {
        SolverValue = value;

        IsSpecifiedbySolver = true;



        Value.SetValue(value, UnitForSolver);

        OnSetMaterialStreamValue?.Invoke();
        OnGoToLocalCalculation?.Invoke();
    }

    public void ClearFromUI()
    {
        IsSpecifiedbyUI = false;
        IsSpecifiedbySolver = false;
        SpecifiedValue = null;
        OnGoToLocalCalculation?.Invoke();
        OnPropagateVariable?.Invoke();
        OnExecuteSolver?.Invoke();

    }

    public void ChangeUnitForUI(UnitMeasure newUnit)
    {
        if (newUnit == null)
            throw new ArgumentNullException(nameof(newUnit));

        UnitForUI = newUnit;
    }

    public double GetDisplayValue()
    {
        var amount = _factory(SolverValue, UnitForSolver);
        return amount.GetValue(UnitForUI);
    }

    public string GetDisplayUnit()
    {
        return UnitForUI.Symbol;
    }

    public T GetDisplayAmount()
    {
        return _factory(SolverValue, UnitForSolver);
    }

    public T Value { get; set; }
    public int Index { get; set; }

    public bool IsSpecifiedbyUI { get; private set; }



    public Action? OnSetMaterialStreamValue { get; set; }
    public Action? OnGoToLocalCalculation { get; set; }
    public Action<INewVariable>? OnAddLocalCalculatedVariable { get; set; }
    public Action? OnExecuteSolver { get; set; }
    public bool IsEspecifiedbyLocalCalculation { get; set; }

    public bool IsEspecified => IsSpecifiedbyUI || IsSpecifiedbySolver || IsEspecifiedbyLocalCalculation;

    public bool IsSpecifiedbySolver { get; set; }

    public void ClearFromSolver()
    {
        if (!IsSpecifiedbySolver) return;

        IsSpecifiedbySolver = false;
        SpecifiedValue = null;
        // opcional: resetear valor o dejar último valor
        OnGoToLocalCalculation?.Invoke();
    }
}



