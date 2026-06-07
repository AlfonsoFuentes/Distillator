using Shared.SolverQwen.Stream;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.SolverConsecutive.Equipments
{
    //public class SolverMixer : ISolverEquipment
    //{
    //    public List<IFacadeStream> Inlets { get; private set; } = new();

    //    public IFacadeStream Outlet { get; private set; } = null!;
    //    public string Name { get; }
    //    public override List<ISolverEquation> Equations => GetEquations().ToList();

    //    public SolverMixer(string name) => Name = name;

    //    public void AddInlet(IFacadeStream inlet)
    //    {
    //        Inlets.Add(inlet);
    //    }
    //    public void SetOutlet(IFacadeStream outlet)
    //    {
    //        Outlet = outlet;
    //    }
    //    public void RemoveInlet(IFacadeStream inlet)
    //    {
    //        Inlets.Remove(inlet);
    //    }
    //    private IEnumerable<ISolverEquation> GetEquations()
    //    {
    //        yield return new MixerPressureEquation(this);
    //        yield return new MixerConcentrationEquation(this);
    //        yield return new MixerMassBalanceEquation(this);
    //        yield return new MixerEnthalpyEquation(this);
    //        yield return new MixerMassEnergyBalanceEquation(this);
    //    }
    //}

    //public class MixerPressureEquation : ISolverEquation
    //{
    //    SolverMixer mixer;
    //    public MixerPressureEquation(SolverMixer _mixer) => mixer = _mixer;
    //    public string Name => $"{EquationType} - {mixer.Name}";
    //    public SolverEquationType EquationType => SolverEquationType.Pressure;
    //    public List<double> Residuals => GetResiduals();
    //    public List<INewVariable> Variables => GetVariables();

    //    List<double> GetResiduals()
    //    {
    //        List<double> r = new();
    //        if (mixer.Inlet1 == null || mixer.Inlet2 == null || mixer.Outlet == null) return r;
    //        r.Add(mixer.Inlet1.Pressure.GetSolverValue() - mixer.Outlet.Pressure.GetSolverValue());
    //        r.Add(mixer.Inlet2.Pressure.GetSolverValue() - mixer.Outlet.Pressure.GetSolverValue());
    //        return r;
    //    }
    //    List<INewVariable> GetVariables()
    //    {
    //        List<INewVariable> v = new();
    //        if (mixer.Inlet1 == null || mixer.Inlet2 == null || mixer.Outlet == null) return v;
    //        v.Add(mixer.Inlet1.Pressure); v.Add(mixer.Inlet2.Pressure); v.Add(mixer.Outlet.Pressure);
    //        return v;
    //    }
    //}

    //public class MixerMassBalanceEquation : ISolverEquation
    //{
    //    SolverMixer mixer;
    //    public MixerMassBalanceEquation(SolverMixer _mixer) => mixer = _mixer;
    //    public string Name => $"{EquationType} - {mixer.Name}";
    //    public SolverEquationType EquationType => SolverEquationType.MassBalance;
    //    public List<double> Residuals => GetResiduals();
    //    public List<INewVariable> Variables => GetVariables();

    //    List<double> GetResiduals()
    //    {
    //        List<double> r = new();
    //        if (mixer.Inlet1 == null || mixer.Inlet2 == null || mixer.Outlet == null) return r;
    //        r.Add(mixer.Inlet1.MassFlow.GetSolverValue() + mixer.Inlet2.MassFlow.GetSolverValue() - mixer.Outlet.MassFlow.GetSolverValue());
    //        return r;
    //    }
    //    List<INewVariable> GetVariables()
    //    {
    //        List<INewVariable> v = new();
    //        if (mixer.Inlet1 == null || mixer.Inlet2 == null || mixer.Outlet == null) return v;
    //        v.Add(mixer.Inlet1.MassFlow); v.Add(mixer.Inlet2.MassFlow); v.Add(mixer.Outlet.MassFlow);
    //        return v;
    //    }
    //}

    //public class MixerConcentrationEquation : ISolverEquation
    //{
    //    SolverMixer mixer;
    //    public MixerConcentrationEquation(SolverMixer _mixer) => mixer = _mixer;
    //    public string Name => $"{EquationType} - {mixer.Name}";
    //    public SolverEquationType EquationType => SolverEquationType.Concentration;
    //    public List<double> Residuals => GetResiduals();
    //    public List<INewVariable> Variables => GetVariables();

    //    List<double> GetResiduals()
    //    {
    //        List<double> r = new();
    //        if (mixer.Inlet1 == null || mixer.Inlet2 == null || mixer.Outlet == null) return r;
    //        int n = mixer.Outlet.Composition.Components.Count;
    //        for (int i = 0; i < n; i++)
    //        {
    //            double m1 = mixer.Inlet1.MassFlow.GetSolverValue();
    //            double m2 = mixer.Inlet2.MassFlow.GetSolverValue();
    //            double mOut = mixer.Outlet.MassFlow.GetSolverValue();
    //            double z1 = mixer.Inlet1.Composition.Components[i].MassFraction.GetSolverValue();
    //            double z2 = mixer.Inlet2.Composition.Components[i].MassFraction.GetSolverValue();
    //            double zOut = mixer.Outlet.Composition.Components[i].MassFraction.GetSolverValue();

    //            r.Add(m1 * z1 + m2 * z2 - mOut * zOut);
    //        }
    //        return r;
    //    }
    //    List<INewVariable> GetVariables()
    //    {
    //        List<INewVariable> v = new();
    //        if (mixer.Inlet1 == null || mixer.Inlet2 == null || mixer.Outlet == null) return v;
    //        int n = mixer.Outlet.Composition.Components.Count;
    //        v.Add(mixer.Inlet1.MassFlow); v.Add(mixer.Inlet2.MassFlow); v.Add(mixer.Outlet.MassFlow);
    //        for (int i = 0; i < n; i++)
    //        {
    //            v.Add(mixer.Inlet1.Composition.Components[i].MassFraction);
    //            v.Add(mixer.Inlet2.Composition.Components[i].MassFraction);
    //            v.Add(mixer.Outlet.Composition.Components[i].MassFraction);
    //        }
    //        return v;
    //    }
    //}

    //public class MixerEnthalpyEquation : ISolverEquation
    //{
    //    SolverMixer mixer;
    //    public MixerEnthalpyEquation(SolverMixer _mixer) => mixer = _mixer;
    //    public string Name => $"{EquationType} - {mixer.Name}";
    //    public SolverEquationType EquationType => SolverEquationType.Enthalpy;
    //    public List<double> Residuals => GetResiduals();
    //    public List<INewVariable> Variables => GetVariables();

    //    List<double> GetResiduals()
    //    {
    //        List<double> r = new();
    //        if (mixer.Inlet1 == null || mixer.Inlet2 == null || mixer.Outlet == null) return r;

    //        double m1 = mixer.Inlet1.MassFlow.GetSolverValue();
    //        double m2 = mixer.Inlet2.MassFlow.GetSolverValue();
    //        double mOut = mixer.Outlet.MassFlow.GetSolverValue();
    //        double h1 = mixer.Inlet1.MassEnthalpy.GetSolverValue();
    //        double h2 = mixer.Inlet2.MassEnthalpy.GetSolverValue();
    //        double hOut = mixer.Outlet.MassEnthalpy.GetSolverValue();

    //        // Mezcla adiabática: Σ(ṁ·h)_in = (ṁ·h)_out
    //        r.Add(m1 * h1 + m2 * h2 - mOut * hOut);
    //        return r;
    //    }
    //    List<INewVariable> GetVariables()
    //    {
    //        List<INewVariable> v = new();
    //        if (mixer.Inlet1 == null || mixer.Inlet2 == null || mixer.Outlet == null) return v;
    //        v.Add(mixer.Inlet1.MassFlow); v.Add(mixer.Inlet2.MassFlow); v.Add(mixer.Outlet.MassFlow);
    //        v.Add(mixer.Inlet1.MassEnthalpy); v.Add(mixer.Inlet2.MassEnthalpy); v.Add(mixer.Outlet.MassEnthalpy);
    //        return v;
    //    }
    //}

    //public class MixerMassEnergyBalanceEquation : ISolverEquation
    //{
    //    SolverMixer mixer;
    //    public MixerMassEnergyBalanceEquation(SolverMixer _mixer) => mixer = _mixer;
    //    public string Name => $"{EquationType} - {mixer.Name}";
    //    public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
    //    public List<double> Residuals => GetResiduals();
    //    public List<INewVariable> Variables => GetVariables();

    //    List<double> GetResiduals()
    //    {
    //        List<double> r = new();
    //        if (mixer.Inlet1 == null || mixer.Inlet2 == null || mixer.Outlet == null) return r;

    //        double m1 = mixer.Inlet1.MassFlow.GetSolverValue();
    //        double m2 = mixer.Inlet2.MassFlow.GetSolverValue();
    //        double mOut = mixer.Outlet.MassFlow.GetSolverValue();
    //        double h1 = mixer.Inlet1.MassEnthalpy.GetSolverValue();
    //        double h2 = mixer.Inlet2.MassEnthalpy.GetSolverValue();
    //        double hOut = mixer.Outlet.MassEnthalpy.GetSolverValue();

    //        r.Add(m1 * h1 + m2 * h2 - mOut * hOut);
    //        return r;
    //    }
    //    List<INewVariable> GetVariables()
    //    {
    //        List<INewVariable> v = new();
    //        if (mixer.Inlet1 == null || mixer.Inlet2 == null || mixer.Outlet == null) return v;
    //        v.Add(mixer.Inlet1.MassFlow); v.Add(mixer.Inlet2.MassFlow); v.Add(mixer.Outlet.MassFlow);
    //        v.Add(mixer.Inlet1.MassEnthalpy); v.Add(mixer.Inlet2.MassEnthalpy); v.Add(mixer.Outlet.MassEnthalpy);
    //        return v;
    //    }
    //}
}
