using Shared.SolverQwen.Stream;
using Shared.SolverQwen.Variables;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments
{
    public enum HeatExchangerStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }
    public class SolverHeatExchanger : SolverEquipmentBase
    {
        public IFacadeStream HotInlet { get; private set; } = null!;
        public IFacadeStream HotOutlet { get; private set; } = null!;
        public IFacadeStream ColdInlet { get; private set; } = null!;
        public IFacadeStream ColdOutlet { get; private set; } = null!;

        public Variable<PressureDrop> DeltaPHot { get; set; }
        public Variable<PressureDrop> DeltaPCold { get; set; }
        public Variable<EnergyFlow> TransferHeat { get; set; }

        public override List<ISolverEquation> Equations => GetEquations().ToList();

        public SolverHeatExchanger(string name)
        {
            Name = name;
            DeltaPHot = new Variable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
            DeltaPCold = new Variable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
            TransferHeat = new Variable<EnergyFlow>(new EnergyFlow(0, EnergyFlowUnits.J_sg), EnergyFlowUnits.Kcal_hr, 3000);
        }
        
       
        public void SetColdInlet(IFacadeStream stream)
        {
            if (stream != null)
            {
                Inlets.Add(stream);
                ColdInlet = stream;
                ColdInlet.EquipmentOutlet = this;

            }
        }
        public void UnSetColdInlet()
        {
            if (ColdInlet == null) return;
            Inlets.Remove(ColdInlet);
            ColdInlet.EquipmentOutlet = null!;
            ColdInlet = null!;
        }
        public void SetColdOutlet(IFacadeStream stream)
        {
            if (stream != null)
            {
                Outlets.Add(stream);
                ColdOutlet = stream;
                ColdOutlet.EquipmentInlet = this;

            }
        }
        public void UnSetColdOutlet()
        {
            if (ColdOutlet == null) return;
            Outlets.Remove(ColdOutlet);
            ColdOutlet.EquipmentInlet = null!;
            ColdOutlet = null!;
        }
        public void SetHotInlet(IFacadeStream stream)
        {
            if (stream != null)
            {
                Inlets.Add(stream);
                HotInlet = stream;
                HotInlet.EquipmentOutlet = this;

            }
        }
        public void UnSetHotInlet()
        {
            if (HotInlet == null) return;
            Inlets.Remove(HotInlet);
            HotInlet.EquipmentOutlet = null!;
            HotInlet = null!;
        }

        public void SetHotOutlet(IFacadeStream stream)
        {
            if (stream != null)
            {
                Outlets.Add(stream);
                HotOutlet = stream;
                HotOutlet.EquipmentInlet = this;

            }
        }
        public void UnSetHotOutlet()
        {
            if (HotOutlet == null) return;
            Outlets.Remove(HotOutlet);
            HotOutlet.EquipmentInlet = null!;
            HotOutlet = null!;
        }

       


        // ====================================================================
        // ESTADO DEL EQUIPO
        // ====================================================================
        public HeatExchangerStateType State => GetState();

        private HeatExchangerStateType GetState()
        {
            // Verificar conexiones mínimas (4 corrientes principales)
            bool hasMinimumConnections = HotInlet != null &&
                                         HotOutlet != null &&
                                         ColdInlet != null &&
                                         ColdOutlet != null;

            if (!hasMinimumConnections) return HeatExchangerStateType.PartiallyConnected;

            // Verificar si al menos una especificación de diseño está definida
            bool hasDesignSpec = DeltaPHot.IsDefined ||
                                 DeltaPCold.IsDefined ||
                                 TransferHeat.IsDefined;

            if (!hasDesignSpec) return HeatExchangerStateType.ReadyToCalculate;

            return HeatExchangerStateType.Solved;
        }

        // ====================================================================
        // GENERADOR DE ECUACIONES
        // ====================================================================
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
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverHeatExchanger hx;
        public HXPressureHotSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} Hot Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return r;
            r.Add(hx.HotInlet.Pressure.GetSolverValue() - hx.DeltaPHot.GetSolverValue() - hx.HotOutlet.Pressure.GetSolverValue());

            return r;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return v;
            v.Add(hx.HotInlet.Pressure);
            v.Add(hx.HotOutlet.Pressure);
            v.Add(hx.DeltaPHot);

            return v;
        }
    }
    public class HXPressureColdSideEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverHeatExchanger hx;
        public HXPressureColdSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} Cold Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return r;

            r.Add(hx.ColdInlet.Pressure.GetSolverValue() - hx.DeltaPCold.GetSolverValue() - hx.ColdOutlet.Pressure.GetSolverValue());
            return r;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return v;

            v.Add(hx.ColdInlet.Pressure);
            v.Add(hx.ColdOutlet.Pressure);
            v.Add(hx.DeltaPCold);
            return v;
        }
    }

    public class HXMassBalanceHotSideEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverHeatExchanger hx;
        public HXMassBalanceHotSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - Hot Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return r;
            r.Add(hx.HotInlet.MassFlow.GetSolverValue() - hx.HotOutlet.MassFlow.GetSolverValue());

            return r;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return v;
            v.Add(hx.HotInlet.MassFlow);
            v.Add(hx.HotOutlet.MassFlow);

            return v;
        }
    }
    public class HXMassBalanceColdSideEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverHeatExchanger hx;
        public HXMassBalanceColdSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - Cold Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return r;
            r.Add(hx.ColdInlet.MassFlow.GetSolverValue() - hx.ColdOutlet.MassFlow.GetSolverValue());

            return r;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return v;
            v.Add(hx.ColdInlet.MassFlow);
            v.Add(hx.ColdOutlet.MassFlow);

            return v;
        }
    }

    public class HXConcentrationHotSideEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverHeatExchanger hx;
        public HXConcentrationHotSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - Hot Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.Concentration;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (hx.HotInlet == null || hx.HotOutlet == null) return r;
            int n = hx.HotInlet.Composition.Components.Count;
            for (int i = 0; i < n; i++)
            {
                double inlet = hx.HotInlet.Composition.Components[i].MassFraction.GetSolverValue();
                double outlet = hx.HotOutlet.Composition.Components[i].MassFraction.GetSolverValue();
                r.Add(inlet - outlet);

            }
            return r;
        }
        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
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
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverHeatExchanger hx;
        public HXConcentrationColdSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - Cold Side - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.Concentration;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

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
        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
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
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverHeatExchanger hx;
        public HXMassEnergyBalanceHotSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - HotSide - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

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

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
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
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverHeatExchanger hx;
        public HXMassEnergyBalanceColdSideEquation(SolverHeatExchanger _hx) => hx = _hx;
        public string Name => $"{EquationType} - ColdSide - {hx.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

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

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (hx.ColdInlet == null || hx.ColdOutlet == null) return v;
            v.Add(hx.ColdInlet.MassFlow);


            v.Add(hx.ColdInlet.MassEnthalpy);
            v.Add(hx.ColdOutlet.MassEnthalpy);

            v.Add(hx.TransferHeat);
            return v;
        }
    }

    
}
