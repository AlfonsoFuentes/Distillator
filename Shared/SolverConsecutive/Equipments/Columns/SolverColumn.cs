using Shared.SolverConsecutive.Equipments.Columns.Orchestrador;
using Shared.SolverQwen.Stream;
using Shared.Thermodynamics.PureComponents;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments.Columns
{
    public enum ColumnStateType { Created, PartiallyConnected, ReadyToCalculate, Solved }
    /// </summary>
    public class SolverColumn : SolverEquipmentBase
    {


        public Variable<Pressure> TopPressure { get; set; }
        public Variable<PressureDrop> DeltaP { get; set; }
        public Variable<Pressure> BottomPressure { get; set; }

        // ====================================================================
        // CORRIENTES DE ENTRADA
        // ====================================================================
        public List<IFacadeStream> Feeds { get; private set; } = new();
        public IFacadeStream? RefluxInlet { get; private set; }
        public IFacadeStream? VaporInlet { get; private set; }

        // ====================================================================
        // CORRIENTES DE SALIDA
        // ====================================================================
        public IFacadeStream? VaporOutlet { get; private set; }
        public IFacadeStream? BottomOutlet { get; private set; }
        public List<IFacadeStream> SideDraws { get; private set; } = new();

        public override List<ISolverEquation> Equations => GetEquations().ToList();
        public ColumnResult? CalculationResult { get; private set; }
        public bool IsCalculationCompleted { get; private set; } = false;

        // ====================================================================
        // ORQUESTADOR
        // ====================================================================
        public IColumnCalculationOrchestrator? Orchestrator { get; private set; }
        public IFacadeStream? GetFirstAvailableStream()
        {
            if (Feeds != null && Feeds.Any()) return Feeds.First();
            if (RefluxInlet != null) return RefluxInlet;
            if (VaporInlet != null) return VaporInlet;
            if (VaporOutlet != null) return VaporOutlet;
            if (BottomOutlet != null) return BottomOutlet;
            if (SideDraws != null && SideDraws.Any()) return SideDraws.First();

            return null;
        }
        public override async Task PostSolveAsync()
        {
            try
            {
                // 1. Crear orquestador si no existe
                if (Orchestrator == null)
                {
                    Orchestrator = new ColumnCalculationOrchestrator(this);
                }

                CalculationResult = await Orchestrator.CalculateAsync();



                Console.WriteLine($"✅ Columna {Name}: Cálculo completado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en PostSolveAsync de {Name}: {ex.Message}");
                IsCalculationCompleted = false;
                CalculationResult = null;
            }
        }


        public SolverColumn(string name)
        {
            Name = name;
            TopPressure = new Variable<Pressure>(new Pressure(101325, PressureUnits.Pascala), PressureUnits.Bara, 100000);
            DeltaP = new Variable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
            BottomPressure = new Variable<Pressure>(new Pressure(101325, PressureUnits.Pascala), PressureUnits.Bara, 100000);



        }


        public void AddFeed(IFacadeStream feed)
        {
            if (Feeds.Contains(feed)) return;
            Inlets.Add(feed);
            Feeds.Add(feed);
            feed.EquipmentOutlet = this;
        }
        public void RemoveSideDraw(IFacadeStream stream)
        {
            if (!SideDraws.Contains(stream)) return;
            Outlets.Remove(stream);
            SideDraws.Remove(stream);
            stream.EquipmentInlet = null!;
        }
        public void RemoveFeed(IFacadeStream stream)
        {
            if (!Feeds.Contains(stream)) return;
            Inlets.Remove(stream);
            Feeds.Remove(stream);
            stream.EquipmentOutlet = null!;
        }
        public void AddSideDraw(IFacadeStream draw)
        {
            if (SideDraws.Contains(draw)) return;
            Outlets.Add(draw);
            SideDraws.Add(draw);
            draw.EquipmentInlet = this;
        }
        public void SetRefluxInlet(IFacadeStream stream)
        {
            if (stream != null)
            {
                Inlets.Add(stream);
                RefluxInlet = stream;
                RefluxInlet.EquipmentOutlet = this;

            }
        }
        public void UnSetRefluxInlet()
        {
            if (RefluxInlet == null) return;
            Inlets.Remove(RefluxInlet);
            RefluxInlet?.EquipmentOutlet = null!;
            RefluxInlet = null!;
        }

        public void SetVaporInlet(IFacadeStream stream)
        {
            if (stream != null)
            {
                Inlets.Add(stream);
                VaporInlet = stream;
                VaporInlet.EquipmentOutlet = this;

            }
        }
        public void UnSetVaporInlet()
        {
            if (VaporInlet == null) return;
            Inlets.Remove(VaporInlet);
            VaporInlet?.EquipmentOutlet = null!;
            VaporInlet = null!;
        }
        public void SetTopVaporOutlet(IFacadeStream stream)
        {
            if (stream != null)
            {
                Outlets.Add(stream);
                VaporOutlet = stream;
                VaporOutlet.EquipmentInlet = this;

            }
        }
        public void UnSetTopVaporOutlet()
        {
            if (VaporOutlet == null) return;
            Outlets.Remove(VaporOutlet);
            VaporOutlet?.EquipmentInlet = null!;
            VaporOutlet = null!;
        }

        public void SetBottomOutlet(IFacadeStream stream)
        {
            if (stream != null)
            {
                Outlets.Add(stream);
                BottomOutlet = stream;
                BottomOutlet.EquipmentInlet = this;

            }
        }
        public void UnSetBottomOutlet()
        {
            if (BottomOutlet == null) return;
            Outlets.Remove(BottomOutlet);
            BottomOutlet?.EquipmentInlet = null!;
            BottomOutlet = null!;
        }



        // ====================================================================
        // ESTADO DEL EQUIPO
        // ====================================================================
        public ColumnStateType State => GetState();

        private ColumnStateType GetState()
        {
            // 1. TOPOLOGÍA MÍNIMA: 
            // Para que sea una columna (o absorbedora/stripper) debe tener al menos una alimentación, 
            // una salida de destilado (Tope) y una salida de fondos (Bottom).
            if (Feeds.Count == 0 || VaporOutlet == null || BottomOutlet == null || RefluxInlet == null || VaporInlet == null)
                return ColumnStateType.PartiallyConnected;

            // 2. ESPECIFICACIÓN MÍNIMA DE DISEÑO:
            // El solver necesita conocer la presión del tope y al menos el DeltaP o la del fondo
            // para poder barrer las presiones de los platos.
            bool hasTopPressure = TopPressure.IsDefined;
            bool hasDeltaPOrBottom = DeltaP.IsDefined || BottomPressure.IsDefined;

            if (!hasTopPressure || !hasDeltaPOrBottom)
                return ColumnStateType.ReadyToCalculate;

            // 3. ESTADO RESUELTO:
            // Si el motor logró hacer el balance general y las corrientes salientes tienen masa calculada.
            if (VaporOutlet.State == StreamStateType.Calculated && BottomOutlet.State == StreamStateType.Calculated)
                return ColumnStateType.Solved;

            return ColumnStateType.ReadyToCalculate;
        }

        private IEnumerable<ISolverEquation> GetEquations()
        {
            yield return new ColumnPressureTopEquation(this);
            yield return new ColumnPressureDeltaPEquation(this);
            yield return new ColumnPressureBottomEquation(this);
            yield return new ColumnEnergyBalanceEquation(this);
            // Backup legacy: la V2 de specifications usa ColumnEnergyBalanceEquation regular.
            // yield return new ColumnMassBalanceEquationSpec(this);
        }
        
    }


    // ECUACIÓN 1: Presión del Tope (INDEPENDIENTE)
    public class ColumnPressureTopEquation : ISolverEquation
    {
        private readonly SolverColumn _column;

        public ColumnPressureTopEquation(SolverColumn column) => _column = column;

        public string Name => $"{EquationType} - Top - {_column.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;

        public List<IVariable> Variables => GetVariables();
        public List<double> Residuals => GetResiduals();
        public List<double> GetResiduals()
        {
            var residuals = new List<double>();

            if (_column.VaporOutlet == null) return residuals;

            double pTope = _column.TopPressure.GetSolverValue();
            double pVapor = _column.VaporOutlet.Pressure.GetSolverValue();

            residuals.Add(pVapor - pTope);

            return residuals;
        }

        private List<IVariable> GetVariables()
        {
            var variables = new List<IVariable>();

            variables.Add(_column.TopPressure);

            if (_column.VaporOutlet != null)
            {
                variables.Add(_column.VaporOutlet.Pressure);
            }

            return variables;
        }

        public SolverEquationTypeModifier EquationTypeModifer => SolverEquationTypeModifier.Regular;
    }

    // ECUACIÓN 2: Relación DeltaP (INDEPENDIENTE)
    public class ColumnPressureDeltaPEquation : ISolverEquation
    {
        private readonly SolverColumn _column;
        public SolverEquationTypeModifier EquationTypeModifer => SolverEquationTypeModifier.Regular;
        public ColumnPressureDeltaPEquation(SolverColumn column) => _column = column;

        public string Name => $"{EquationType} - DeltaP - {_column.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        public List<double> GetResiduals()
        {
            var residuals = new List<double>();

            double pTope = _column.TopPressure.GetSolverValue();
            double pBottom = _column.BottomPressure.GetSolverValue();
            double deltaP = _column.DeltaP.GetSolverValue();

            residuals.Add(pBottom - (pTope + deltaP));

            return residuals;
        }

        private List<IVariable> GetVariables()
        {
            var variables = new List<IVariable>();

            variables.Add(_column.TopPressure);
            variables.Add(_column.BottomPressure);
            variables.Add(_column.DeltaP);

            return variables;
        }
    }

    // ECUACIÓN 3: Presión del Fondo (INDEPENDIENTE)
    public class ColumnPressureBottomEquation : ISolverEquation
    {
        private readonly SolverColumn _column;
        public SolverEquationTypeModifier EquationTypeModifer => SolverEquationTypeModifier.Regular;
        public ColumnPressureBottomEquation(SolverColumn column) => _column = column;

        public string Name => $"{EquationType} - Bottom - {_column.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        public List<double> GetResiduals()
        {
            var residuals = new List<double>();

            if (_column.BottomOutlet == null && _column.SideDraws.Count == 0)
                return residuals;

            double pBottom = _column.BottomPressure.GetSolverValue();

            if (_column.BottomOutlet != null)
            {
                double pLiquid = _column.BottomOutlet.Pressure.GetSolverValue();
                residuals.Add(pLiquid - pBottom);
            }

            foreach (var sidedraw in _column.SideDraws)
            {
                double pSide = sidedraw.Pressure.GetSolverValue();
                residuals.Add(pSide - pBottom);
            }

            return residuals;
        }

        private List<IVariable> GetVariables()
        {
            var variables = new List<IVariable>();

            variables.Add(_column.BottomPressure);

            if (_column.BottomOutlet != null)
            {
                variables.Add(_column.BottomOutlet.Pressure);
            }

            foreach (var sidedraw in _column.SideDraws)
            {
                variables.Add(sidedraw.Pressure);
            }

            return variables;
        }
    }


    public class ColumnEnergyBalanceEquation : ISolverEquation
    {
        private readonly SolverColumn _column;
        public SolverEquationTypeModifier EquationTypeModifer => SolverEquationTypeModifier.Regular;
        public ColumnEnergyBalanceEquation(SolverColumn column)
        {
            _column = column;
        }

        public string Name => $"{EquationType} - {_column.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<IVariable> Variables => GetVariables();

        public List<double> GetResiduals()
        {
            var residuals = new List<double>();

            //if (_column.VaporOutlet == null ||
            //    _column.BottomOutlet == null ||
            //    _column.RefluxInlet == null ||
            //    _column.VaporInlet == null ||
            //    _column.Feeds == null ||
            //    _column.Feeds.Count == 0)
            //{
            //    return residuals; // Retorna vacío, el solver ignora esta ecuación
            //}

            double totalEnergyIn = 0;
            double totalEnergyOut = 0;
            double totalmassIn = 0;
            double totalmassOut = 0;

            double massCompIn = 0;
            double massCompoOut = 0;

            if (_column.VaporOutlet != null)
            {
                double mVaporOutlet = _column.VaporOutlet.MassFlow.GetSolverValue();
                double HVaporOutlet = _column.VaporOutlet.MassEnthalpy.GetSolverValue();
                totalmassOut += mVaporOutlet;
                totalEnergyOut += mVaporOutlet * HVaporOutlet;
               if(_column.VaporOutlet.Composition!=null)
                {
                    for (int i = 0; i < _column.VaporOutlet.Composition.Components.Count - 1; i++)
                    {
                        var compo = _column.VaporOutlet.Composition.Components[i];
                        massCompoOut += compo.MassFraction.GetSolverValue() * mVaporOutlet;
                    }
                }


            }

            if (_column.BottomOutlet != null)
            {
                double mBottomOutlet = _column.BottomOutlet.MassFlow.GetSolverValue();
                double HBottomOutlet = _column.BottomOutlet.MassEnthalpy.GetSolverValue();
                totalmassOut += mBottomOutlet;
                totalEnergyOut += mBottomOutlet * HBottomOutlet;
                if (_column.BottomOutlet.Composition != null)
                {
                    for (int i = 0; i < _column.BottomOutlet.Composition.Components.Count - 1; i++)
                    {
                        var compo = _column.BottomOutlet.Composition.Components[i];
                        massCompoOut += compo.MassFraction.GetSolverValue() * mBottomOutlet;
                    }
                }

            }
            if (_column.VaporInlet != null)
            {
                double mVaporInlet = _column.VaporInlet.MassFlow.GetSolverValue();
                double HVaporInlet = _column.VaporInlet.MassEnthalpy.GetSolverValue();
                totalmassIn += mVaporInlet;
                totalEnergyIn += mVaporInlet * HVaporInlet;

                if (_column.VaporInlet.Composition != null)
                {
                    for (int i = 0; i < _column.VaporInlet.Composition.Components.Count - 1; i++)
                    {
                        var compo = _column.VaporInlet.Composition.Components[i];
                        massCompIn += compo.MassFraction.GetSolverValue() * mVaporInlet;
                    }
                }
            }

            if (_column.RefluxInlet != null)
            {
                double mRefluxInlet = _column.RefluxInlet.MassFlow.GetSolverValue();
                double HRefluxInlet = _column.RefluxInlet.MassEnthalpy.GetSolverValue();
                totalmassIn += mRefluxInlet;
                totalEnergyIn += mRefluxInlet * HRefluxInlet;

                if (_column.RefluxInlet.Composition != null)
                {
                    for (int i = 0; i < _column.RefluxInlet.Composition.Components.Count - 1; i++)
                    {
                        var compo = _column.RefluxInlet.Composition.Components[i];
                        massCompIn += compo.MassFraction.GetSolverValue() * mRefluxInlet;
                    }
                }
            }


            foreach (var sidedraw in _column.SideDraws)
            {
                double msidedraw = sidedraw.MassFlow.GetSolverValue();
                double Hsidedraw = sidedraw.MassEnthalpy.GetSolverValue();
                totalmassOut += msidedraw;
                totalEnergyOut += msidedraw * Hsidedraw;
                if (sidedraw.Composition != null)
                {
                    for (int i = 0; i < sidedraw.Composition.Components.Count - 1; i++)
                    {
                        var compo = sidedraw.Composition.Components[i];
                        massCompoOut += compo.MassFraction.GetSolverValue() * msidedraw;
                    }
                }
            }
            foreach (var feed in _column.Feeds)
            {
                double mfeed = feed.MassFlow.GetSolverValue();
                double Hfeed = feed.MassEnthalpy.GetSolverValue();
                totalmassIn += mfeed;
                totalEnergyIn += mfeed * Hfeed;
                if (feed.Composition != null)
                {
                    for (int i = 0; i < feed.Composition.Components.Count - 1; i++)
                    {
                        var compo = feed.Composition.Components[i];
                        massCompIn += compo.MassFraction.GetSolverValue() * mfeed;
                    }
                }
            }


            // Sumar energía de entrada

            residuals.Add(massCompIn - massCompoOut);
            residuals.Add(totalmassIn - totalmassOut);
            residuals.Add(totalEnergyIn - totalEnergyOut);

            return residuals;
        }

        private List<IVariable> GetVariables()
        {
            var variables = new List<IVariable>();

            if (_column.VaporOutlet != null)
            {
                variables.Add(_column.VaporOutlet.MassEnthalpy);

                variables.Add(_column.VaporOutlet.MassFlow);


            }

            if (_column.BottomOutlet != null)
            {
                variables.Add(_column.BottomOutlet.MassEnthalpy);
                variables.Add(_column.BottomOutlet.MassFlow);


            }
            if (_column.VaporInlet != null)
            {
                variables.Add(_column.VaporInlet.MassEnthalpy);
                variables.Add(_column.VaporInlet.MassFlow);


            }

            if (_column.RefluxInlet != null)
            {
                variables.Add(_column.RefluxInlet.MassEnthalpy);
                variables.Add(_column.RefluxInlet.MassFlow);


            }


            foreach (var sidedraw in _column.SideDraws)
            {
                variables.Add(sidedraw.MassEnthalpy);
                variables.Add(sidedraw.MassFlow);
            }
            foreach (var feed in _column.Feeds)
            {
                variables.Add(feed.MassEnthalpy);
                variables.Add(feed.MassFlow);

            }


            return variables;
        }
    }
   


}
