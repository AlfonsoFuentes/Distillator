using Shared.SolverQwen.Stream;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments
{
    public enum FlashTankStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }
    public class SolverDrum : SolverEquipmentBase
    {
        public IFacadeStream Feed { get; private set; } = null!;
        public IFacadeStream VaporOutlet { get; private set; } = null!;
        public IFacadeStream LiquidOutlet { get; private set; } = null!;

        public override List<ISolverEquation> Equations => GetEquations().ToList();

        public SolverDrum(string name)
        {
            Name = name;
        }
        public void SetFeed(IFacadeStream stream)
        {
            if (stream != null)
            {
                Inlets.Add(stream);
                Feed = stream;
                Feed.EquipmentOutlet = this;

            }
        }
        public void UnSetFeed()
        {
            if (Feed == null) return;
            Inlets.Remove(Feed);
            Feed.EquipmentOutlet = null!;
            Feed = null!;
        }

        public void SetVaporOutlet(IFacadeStream stream)
        {
            if (stream != null)
            {
                Outlets.Add(stream);
                VaporOutlet = stream;
                VaporOutlet.EquipmentInlet = this;

            }
        }
        public void UnSetVaporOutlet()
        {
            if (VaporOutlet == null) return;
            Outlets.Remove(VaporOutlet);
            VaporOutlet.EquipmentInlet = null!;
            VaporOutlet = null!;
        }

        public void SetLiquidOutlet(IFacadeStream stream)
        {
            if (stream != null)
            {
                Outlets.Add(stream);
                LiquidOutlet = stream;
                LiquidOutlet.EquipmentInlet = this;

            }
        }
        public void UnSetLiquidOutlet()
        {
            if (LiquidOutlet == null) return;
            Outlets.Remove(LiquidOutlet);
            LiquidOutlet.EquipmentInlet = null!;
            LiquidOutlet = null!;
        }




        //public override IEnumerable<ISolverEquipment> GetEquipmentInlets(IFacadeStream stream)
        //{
        //    if (stream == null) yield return null!;
        //    if (stream == VaporOutlet || stream == LiquidOutlet) yield return Feed.EquipmentInlet;

        //    yield  break;

        //}
        //public override IEnumerable<ISolverEquipment> GetEquipmentOutlets(IFacadeStream stream)
        //{
        //    if (stream == null) yield return null!;
        //    if (stream == Feed)
        //    {
        //        yield return VaporOutlet.EquipmentOutlet;
        //        yield return LiquidOutlet.EquipmentOutlet;
        //    }

        //    yield break;
        //}
        public FlashTankStateType State => GetState();

        private FlashTankStateType GetState()
        {
            // Verificar conexiones mínimas
            bool hasMinimumConnections = Feed != null &&
                                         VaporOutlet != null &&
                                         LiquidOutlet != null;

            if (!hasMinimumConnections) return FlashTankStateType.PartiallyConnected;

            // El drum no tiene variables de diseño específicas, solo necesita las conexiones
            return FlashTankStateType.ReadyToCalculate;
        }

        // ====================================================================
        // GENERADOR DE ECUACIONES
        // ====================================================================
        private IEnumerable<ISolverEquation> GetEquations()
        {
            yield return new DrumPressureEquation(this);
            yield return new DrumConcentrationEquation(this);
            yield return new DrumEnthalpyEquation(this);
            yield return new DrumMassEnergyBalanceEquation(this);
            // Backup legacy: la V2 de specifications usa DrumMassEnergyBalanceEquation regular.
            // yield return new DrumMassBalanceEquationSpec(this);
        }
    }


    public class DrumPressureEquation : ISolverEquation
    {
        SolverDrum drum;
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        public DrumPressureEquation(SolverDrum _drum) { drum = _drum; }
        public string Name => $"{EquationType} - {drum.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return r;

            double pF = drum.Feed.Pressure.GetSolverValue();
            double pV = drum.VaporOutlet.Pressure.GetSolverValue();
            double pL = drum.LiquidOutlet.Pressure.GetSolverValue();


            r.Add(pF - pV); // P_Vapor = P_Feed - ΔP_V
            r.Add(pV - pL); // P_Liquid = P_Vapor - ΔP_L
            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return v;
            v.Add(drum.Feed.Pressure);
            v.Add(drum.VaporOutlet.Pressure);
            v.Add(drum.LiquidOutlet.Pressure);

            return v;
        }
    }



    public class DrumEnthalpyEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverDrum drum;
        public DrumEnthalpyEquation(SolverDrum _drum) { drum = _drum; }
        public string Name => $"{EquationType} - {drum.Name}";
        public SolverEquationType EquationType => SolverEquationType.Enthalpy;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return r;

            var feed = drum.Feed;
            var vapOutlet = drum.VaporOutlet;
            var liqOutlet = drum.LiquidOutlet;

            double h_vaporPhase_feed = feed.VaporPhase.MassEnthalpy.GetValue(feed.MassEnthalpy.InternalUnit) / feed.MassEnthalpy.NormalizeValue;
            double h_liquidPhase_feed = feed.LiquidPhase.MassEnthalpy.GetValue(feed.MassEnthalpy.InternalUnit) / feed.MassEnthalpy.NormalizeValue;

            double hVaporGuess = vapOutlet.MassEnthalpy.GetSolverValue();
            double hLiquidGuess = liqOutlet.MassEnthalpy.GetSolverValue();

            double resV = hVaporGuess - h_vaporPhase_feed;
            double resL = hLiquidGuess - h_liquidPhase_feed;

            r.Add(resV);
            r.Add(resL);


            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return v;

            v.Add(drum.VaporOutlet.MassEnthalpy);
            v.Add(drum.LiquidOutlet.MassEnthalpy);
            return v;
        }
    }

    public class DrumConcentrationEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverDrum drum;
        public DrumConcentrationEquation(SolverDrum _drum) { drum = _drum; }
        public string Name => $"{EquationType} - {drum.Name}";
        public SolverEquationType EquationType => SolverEquationType.Concentration;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return r;


            var feed = drum.Feed;
            var vapOutlet = drum.VaporOutlet;
            var liqOutlet = drum.LiquidOutlet;
            var residuals = new List<double>();

            int numComponents = feed.Composition.Components.Count;
            for (int i = 0; i < numComponents; i++)
            {
                var comp = feed.Composition.Components[i];
                var localComponentVapor = feed.VaporPhase.Components.FirstOrDefault(x => x.Id == comp.Id);
                var localComponentLiquido = feed.LiquidPhase.Components.FirstOrDefault(x => x.Id == comp.Id);

                if (localComponentVapor != null && localComponentLiquido != null)
                {
                    double vaporoutlet = vapOutlet.Composition.Components[i].MassFraction.GetSolverValue();
                    double liquidoutlet = liqOutlet.Composition.Components[i].MassFraction.GetSolverValue();

                    double resV = vaporoutlet - localComponentVapor.MassFraction;
                    double resL = liquidoutlet - localComponentLiquido.MassFraction;



                    residuals.Add(resV);
                    residuals.Add(resL);
                }
            }
            return residuals;

        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return v;



            for (int i = 0; i < drum.Feed.Composition.Components.Count; i++)
            {

                v.Add(drum.VaporOutlet.Composition.Components[i].MassFraction);
                v.Add(drum.LiquidOutlet.Composition.Components[i].MassFraction);
            }
            return v;
        }
    }

    public class DrumMassEnergyBalanceEquation : ISolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Regular;
        SolverDrum drum;
        public DrumMassEnergyBalanceEquation(SolverDrum _drum) { drum = _drum; }
        public string Name => $"{EquationType} - {drum.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return r;
            var feed = drum.Feed;
            var vapOutlet = drum.VaporOutlet;
            var liqOutlet = drum.LiquidOutlet;

            double hFeed = feed.MassEnthalpy.GetSolverValue();
            double hvapor = vapOutlet.MassEnthalpy.GetSolverValue();
            double hliquid = liqOutlet.MassEnthalpy.GetSolverValue();

            double mFeedSolver = feed.MassFlow.GetSolverValue();
            double mVaporSolver = vapOutlet.MassFlow.GetSolverValue();
            double mLiquidSolver = liqOutlet.MassFlow.GetSolverValue();

            double resMass = mFeedSolver - mVaporSolver - mLiquidSolver;
            double resEnergy = (mFeedSolver * hFeed) - (mVaporSolver * hvapor) - (mLiquidSolver * hliquid);

            r.Add(resMass);
            r.Add(resEnergy);

            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return v;

            v.Add(drum.VaporOutlet.MassFlow);
            v.Add(drum.LiquidOutlet.MassFlow);

            return v;
        }
    }
    /*
    // Backup legacy: la V2 de specifications usa DrumMassEnergyBalanceEquation regular.
    public class DrumMassBalanceEquationSpec : ISpecSolverEquation
    {
        public SolverEquationTypeModifier EquationTypeModifer { get; } = SolverEquationTypeModifier.Spec;
        SolverDrum drum;
        public DrumMassBalanceEquationSpec(SolverDrum _drum) { drum = _drum; }
        public string Name => $"{EquationType} - {drum.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return r;
            var feed = drum.Feed;
            var vapOutlet = drum.VaporOutlet;
            var liqOutlet = drum.LiquidOutlet;

            double hFeed = feed.MassEnthalpy.GetSolverValue();
            double hvapor = vapOutlet.MassEnthalpy.GetSolverValue();
            double hliquid = liqOutlet.MassEnthalpy.GetSolverValue();

            double mFeedSolver = feed.MassFlow.GetSolverValue();
            double mVaporSolver = vapOutlet.MassFlow.GetSolverValue();
            double mLiquidSolver = liqOutlet.MassFlow.GetSolverValue();

            double resMass = mFeedSolver - mVaporSolver - mLiquidSolver;
            double resEnergy = (mFeedSolver * hFeed) - (mVaporSolver * hvapor) - (mLiquidSolver * hliquid);

            r.Add(resMass);
            r.Add(resEnergy);

            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return v;

            v.Add(drum.VaporOutlet.MassFlow);
            v.Add(drum.LiquidOutlet.MassFlow);

            return v;
        }
        public IEnumerable<IFacadeStream> AsociatedStreams
        {
            get
            {
                foreach (var inlet in drum.Inlets)
                    yield return inlet;
                foreach (var outlet in drum.Outlets) yield return outlet;

            }
        }
    }
    */
}
