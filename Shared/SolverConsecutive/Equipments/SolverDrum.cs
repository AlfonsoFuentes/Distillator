using Shared.SolverQwen.Stream;
using System;
using System.Collections.Generic;
using System.Text;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments
{
    public class SolverDrum : SolverEquipmentBase
    {
        public IFacadeStream Feed { get;private set; } = null!;
        public IFacadeStream VaporOutlet { get; private set; } = null!;
        public IFacadeStream LiquidOutlet { get; private set; } = null!;
        public override string Name { get; }

        public void SetFeed(IFacadeStream feed)
        {
            Feed = feed;
        }
        public void SetVaporOutlet(IFacadeStream vaporOutlet)
        {
            VaporOutlet = vaporOutlet;
        }
        public void SetLiquidOutlet(IFacadeStream liquidOutlet)
        {
            LiquidOutlet = liquidOutlet;
        }
        public override List<ISolverEquation> Equations => GetEquations().ToList();

        public SolverDrum(string name)
        {
            Name = name;

        }

        private IEnumerable<ISolverEquation> GetEquations()
        {
            yield return new DrumPressureEquation(this);
            yield return new DrumConcentrationEquation(this);

            yield return new DrumEnthalpyEquation(this);
            yield return new DrumMassEnergyBalanceEquation(this);
        }
    }

    public class DrumPressureEquation : ISolverEquation
    {
        SolverDrum drum;
        public DrumPressureEquation(SolverDrum _drum) { drum = _drum; }
        public string Name => $"{EquationType} - {drum.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

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

        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return v;
            v.Add(drum.Feed.Pressure);
            v.Add(drum.VaporOutlet.Pressure);
            v.Add(drum.LiquidOutlet.Pressure);

            return v;
        }
    }

   

    public class DrumEnthalpyEquation : ISolverEquation
    {
        SolverDrum drum;
        public DrumEnthalpyEquation(SolverDrum _drum) { drum = _drum; }
        public string Name => $"{EquationType} - {drum.Name}";
        public SolverEquationType EquationType => SolverEquationType.Enthalpy;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

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

        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return v;
       
            v.Add(drum.VaporOutlet.MassEnthalpy);
            v.Add(drum.LiquidOutlet.MassEnthalpy);
            return v;
        }
    }

    public class DrumConcentrationEquation : ISolverEquation
    {
        SolverDrum drum;
        public DrumConcentrationEquation(SolverDrum _drum) { drum = _drum; }
        public string Name => $"{EquationType} - {drum.Name}";
        public SolverEquationType EquationType => SolverEquationType.Concentration;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

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

        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
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
        SolverDrum drum;
        public DrumMassEnergyBalanceEquation(SolverDrum _drum) { drum = _drum; }
        public string Name => $"{EquationType} - {drum.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

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

        List<INewVariable> GetVariables()
        {
            List<INewVariable> v = new();
            if (drum.Feed == null || drum.VaporOutlet == null || drum.LiquidOutlet == null) return v;
          
            v.Add(drum.VaporOutlet.MassFlow);
            v.Add(drum.LiquidOutlet.MassFlow);
        
            return v;
        }
    }
}
