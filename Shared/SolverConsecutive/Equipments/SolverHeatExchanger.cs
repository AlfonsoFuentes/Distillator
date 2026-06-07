using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments
{
    public class SolverHeatExchanger : SolverEquipmentBase
    {
        public IFacadeStream HotInlet { get; private set; } = null!;
        public IFacadeStream HotOutlet { get; private set; } = null!;
        public IFacadeStream ColdInlet { get; private set; } = null!;
        public IFacadeStream ColdOutlet { get; private set; } = null!;
        public NewVariable<PressureDrop> DeltaPHot { get; }
        public NewVariable<PressureDrop> DeltaPCold { get; }

        // ✅ LA MAGIA: Calores separados para permitir resolución aislada reactiva
        public NewVariable<EnergyFlow> TransferHeat { get; }

        public override string Name { get; }
        public override List<ISolverEquation> Equations => GetEquations().ToList();

        public void SetColdInlet(IFacadeStream stream)
        {
            ColdInlet = stream;
        }
        public void SetColdOutlet(IFacadeStream stream)
        {
            ColdOutlet = stream;
        }
        public void SetHotInlet(IFacadeStream stream)
        {
            HotInlet = stream;
        }
        public void SetHotOutlet(IFacadeStream stream)
        {
            HotOutlet = stream;
        }
        public SolverHeatExchanger(string name)
        {
            Name = name;
            DeltaPHot = new NewVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
            DeltaPCold = new NewVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);

            TransferHeat = new NewVariable<EnergyFlow>(new EnergyFlow(0, EnergyFlowUnits.J_sg), EnergyFlowUnits.Kcal_hr, 3000);
        }

        private IEnumerable<ISolverEquation> GetEquations()
        {
            yield return new HXPressureHotSideEquation(this);
            yield return new HXPressureColdSideEquation(this);
            yield return new HXConcentrationHotSideEquation(this);
            yield return new HXConcentrationColdSideEquation(this);
            yield return new HXMassBalanceHotSideEquation(this);
            yield return new HXMassBalanceColdSideEquation(this);

            yield return new HXMassEnergyBalanceHotSideEquation(this);
            yield return new HXMassEnergyBalanceColdSideEquation(this);
        }
    }

    public class HXPressureHotSideEquation : ISolverEquation
    {
        SolverHeatExchanger hx;
        public HXPressureHotSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} Hot Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return r;
            r.Add(hx.HotInlet.Pressure.GetSolverValue() - hx.DeltaPHot.GetSolverValue() - hx.HotOutlet.Pressure.GetSolverValue());

            return r;
        }
        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return v;
            v.Add(hx.HotInlet.Pressure);
            v.Add(hx.HotOutlet.Pressure);
            v.Add(hx.DeltaPHot);

            return v;
        }
    }
    public class HXPressureColdSideEquation : ISolverEquation
    {
        SolverHeatExchanger hx;
        public HXPressureColdSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} Cold Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return r;

            r.Add(hx.ColdInlet.Pressure.GetSolverValue() - hx.DeltaPCold.GetSolverValue() - hx.ColdOutlet.Pressure.GetSolverValue());
            return r;
        }
        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return v;

            v.Add(hx.ColdInlet.Pressure);
            v.Add(hx.ColdOutlet.Pressure);
            v.Add(hx.DeltaPCold);
            return v;
        }
    }

    public class HXMassBalanceHotSideEquation : ISolverEquation
    {
        SolverHeatExchanger hx;
        public HXMassBalanceHotSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - Hot Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return r;
            r.Add(hx.HotInlet.MassFlow.GetSolverValue() - hx.HotOutlet.MassFlow.GetSolverValue());

            return r;
        }
        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return v;
            v.Add(hx.HotInlet.MassFlow);
            v.Add(hx.HotOutlet.MassFlow);

            return v;
        }
    }
    public class HXMassBalanceColdSideEquation : ISolverEquation
    {
        SolverHeatExchanger hx;
        public HXMassBalanceColdSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - Cold Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return r;
            r.Add(hx.ColdInlet.MassFlow.GetSolverValue() - hx.ColdOutlet.MassFlow.GetSolverValue());

            return r;
        }
        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return v;
            v.Add(hx.ColdInlet.MassFlow);
            v.Add(hx.ColdOutlet.MassFlow);

            return v;
        }
    }

    public class HXConcentrationHotSideEquation : ISolverEquation
    {
        SolverHeatExchanger hx;
        public HXConcentrationHotSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - Hot Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.Concentration;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return r;
            int n = hx.HotInlet.Composition.Components.Count;
            for (int i = 0; i < n; i++)
            {
                r.Add(hx.HotInlet.Composition.Components[i].MassFraction.GetSolverValue() - hx.HotOutlet.Composition.Components[i].MassFraction.GetSolverValue());

            }
            return r;
        }
        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return v;
            int n = hx.HotInlet.Composition.Components.Count;
            for (int i = 0; i < n; i++)
            {
                v.Add(hx.HotInlet.Composition.Components[i].MassFraction);
                v.Add(hx.HotOutlet.Composition.Components[i].MassFraction);

            }
            return v;
        }
    }
    public class HXConcentrationColdSideEquation : ISolverEquation
    {
        SolverHeatExchanger hx;
        public HXConcentrationColdSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - Cold Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.Concentration;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return r;
            int n = hx.ColdInlet.Composition.Components.Count;
            for (int i = 0; i < n; i++)
            {

                r.Add(hx.ColdInlet.Composition.Components[i].MassFraction.GetSolverValue() - hx.ColdOutlet.Composition.Components[i].MassFraction.GetSolverValue());
            }
            return r;
        }
        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return v;
            int n = hx.ColdInlet.Composition.Components.Count;
            for (int i = 0; i < n; i++)
            {

                v.Add(hx.ColdInlet.Composition.Components[i].MassFraction);
                v.Add(hx.ColdOutlet.Composition.Components[i].MassFraction);
            }
            return v;
        }
    }



    public class HXMassEnergyBalanceHotSideEquation : ISolverEquation
    {
        SolverHeatExchanger hx;
        public HXMassEnergyBalanceHotSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - HotSide - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return r;

            double mH_in = hx.HotInlet.MassFlow.GetSolverValue();
         


            double hH_in = hx.HotInlet.MassEnthalpy.GetSolverValue();
            double hH_out = hx.HotOutlet.MassEnthalpy.GetSolverValue();

           
            double trasnferHeat = hx.TransferHeat.GetSolverValue();
            // Verificador redundante: Entrada total = Salida total
            r.Add(mH_in * hH_in - mH_in * hH_out - trasnferHeat);
            return r;
        }

        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return v;
            v.Add(hx.HotInlet.MassFlow);
         

            v.Add(hx.HotInlet.MassEnthalpy);
            v.Add(hx.HotOutlet.MassEnthalpy);

            v.Add(hx.TransferHeat);
            return v;
        }
    }
    public class HXMassEnergyBalanceColdSideEquation : ISolverEquation
    {
        SolverHeatExchanger hx;
        public HXMassEnergyBalanceColdSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - ColdSide - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return r;

            double mC_in = hx.ColdInlet.MassFlow.GetSolverValue();
           


            double hC_in = hx.ColdInlet.MassEnthalpy.GetSolverValue();
            double hC_out = hx.ColdOutlet.MassEnthalpy.GetSolverValue();

            double TransferHeat = hx.TransferHeat.GetSolverValue();

        
            // Verificador redundante: Entrada total = Salida total
            r.Add(mC_in * hC_in - mC_in * hC_out + TransferHeat);
            return r;
        }

        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return v;
            v.Add(hx.ColdInlet.MassFlow);
           

            v.Add(hx.ColdInlet.MassEnthalpy);
            v.Add(hx.ColdOutlet.MassEnthalpy);

            v.Add(hx.TransferHeat);
            return v;
        }
    }
}
