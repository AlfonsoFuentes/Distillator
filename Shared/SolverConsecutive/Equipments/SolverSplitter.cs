using Shared.SolverQwen.Stream;

namespace Shared.SolverConsecutive.Equipments
{
    public enum SplitterStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }
    public class SolverSplitter : SolverEquipmentBase
    {
        public IFacadeStream Inlet { get; set; } = null!;
        public List<IFacadeStream> Outlets { get; set; } = new();

        public override List<ISolverEquation> Equations => GetEquations().ToList();

        public SolverSplitter(string name)
        {
            Name = name;
        }

        public void SetInlet(IFacadeStream stream)
        {
            Inlet = stream;
        }

        public void AddOutlet(IFacadeStream stream)
        {
            Outlets.Add(stream);
        }

        public void RemoveOutlet(IFacadeStream stream)
        {
            Outlets.Remove(stream);
        }

        // ====================================================================
        // ESTADO DEL EQUIPO
        // ====================================================================
        public SplitterStateType State => GetState();

        private SplitterStateType GetState()
        {
            // Verificar conexiones mínimas
            bool hasMinimumConnections = Inlet != null && Outlets.Count > 0;

            if (!hasMinimumConnections) return SplitterStateType.PartiallyConnected;

            // El splitter no tiene variables de diseño específicas, solo necesita las conexiones
            return SplitterStateType.ReadyToCalculate;
        }

        // ====================================================================
        // GENERADOR DE ECUACIONES
        // ====================================================================
        private IEnumerable<ISolverEquation> GetEquations()
        {
            yield return new SplitterPressureEquation(this);
            yield return new SplitterConcentrationEquation(this);
            yield return new SplitterMassBalanceEquation(this);
            yield return new SplitterEnthalpyEquation(this);
        }
    }

    public class SplitterPressureEquation : ISolverEquation
    {
        SolverSplitter splitter;
        public SplitterPressureEquation(SolverSplitter _splitter) { splitter = _splitter; }
        public string Name => $"{EquationType} - {splitter.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (splitter.Inlet == null || splitter.Outlets.Count==0) return r;

            double pIn = splitter.Inlet.Pressure.GetSolverValue();

            foreach (var outlet in splitter.Outlets)
            {
                double pOut = outlet.Pressure.GetSolverValue();
                r.Add(pIn - pOut);
            }
            return r;
        }

           
        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (splitter.Inlet == null ||  splitter.Outlets.Count == 0) return v;
            v.Add(splitter.Inlet.Pressure);
            foreach (var outlet in splitter.Outlets)
            {
                v.Add(outlet.Pressure);
            }
            return v;
        }
    }

    public class SplitterMassBalanceEquation : ISolverEquation
    {
        SolverSplitter splitter;
        public SplitterMassBalanceEquation(SolverSplitter _splitter) { splitter = _splitter; }
        public string Name => $"{EquationType} - {splitter.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (splitter.Inlet == null || splitter.Outlets.Count == 0) return r;

            double mIn = splitter.Inlet.MassFlow.GetSolverValue();

            double mOutTotal = splitter.Outlets.Sum(outlet => outlet.MassFlow.GetSolverValue());

            r.Add(mIn - mOutTotal);
            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (splitter.Inlet == null || splitter.Outlets.Count == 0) return v;
            v.Add(splitter.Inlet.MassFlow);
            foreach (var outlet in splitter.Outlets)
            {
                v.Add(outlet.MassFlow);
            }
            return v;
        }
    }

    public class SplitterConcentrationEquation : ISolverEquation
    {
        SolverSplitter splitter;
        public SplitterConcentrationEquation(SolverSplitter _splitter) { splitter = _splitter; }
        public string Name => $"{EquationType} - {splitter.Name}";
        public SolverEquationType EquationType => SolverEquationType.Concentration;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (splitter.Inlet == null || splitter.Outlets.Count == 0) return r;

            int n = splitter.Inlet.Composition.Components.Count;
            for (int i = 0; i < n; i++)
            {
                double zIn = splitter.Inlet.Composition.Components[i].MassFraction.GetSolverValue();
                foreach (var outlet in splitter.Outlets)
                {
                    double xOut = outlet.Composition.Components[i].MassFraction.GetSolverValue();
                    r.Add(zIn - xOut);
                }
            }
            return r;
        }

       
        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (splitter.Inlet == null || splitter.Outlets.Count == 0) return v;

            int n = splitter.Inlet.Composition.Components.Count;
            for (int i = 0; i < n; i++)
            {
                v.Add(splitter.Inlet.Composition.Components[i].MassFraction);
                foreach (var outlet in splitter.Outlets)
                {
                    v.Add(outlet.Composition.Components[i].MassFraction);
                }
            }
            return v;
        }
    }

    public class SplitterEnthalpyEquation : ISolverEquation
    {
        SolverSplitter splitter;
        public SplitterEnthalpyEquation(SolverSplitter _splitter) { splitter = _splitter; }
        public string Name => $"{EquationType} - {splitter.Name}";
        public SolverEquationType EquationType => SolverEquationType.Enthalpy;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        List<double> GetResiduals()
        {
            List<double> r = new();
            if (splitter.Inlet == null || splitter.Outlets.Count == 0) return r;

            double hIn = splitter.Inlet.MassEnthalpy.GetSolverValue();
            foreach (var outlet in splitter.Outlets)
            {
                double hOut = outlet.MassEnthalpy.GetSolverValue();
                r.Add(hIn - hOut);
            }
            return r;
        }

        List<IVariable> GetVariables()
        {
            List<IVariable> v = new();
            if (splitter.Inlet == null || splitter.Outlets.Count == 0) return v;
            v.Add(splitter.Inlet.MassEnthalpy);
            foreach (var outlet in splitter.Outlets)
            {
                v.Add(outlet.MassEnthalpy);
            }
            return v;
        }
    }

    


}
