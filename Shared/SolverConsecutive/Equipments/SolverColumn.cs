using Shared.SolverQwen.Stream;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using UnitSystem;

namespace Shared.SolverConsecutive.Equipments
{


    /// </summary>
    public class SolverColumn : SolverEquipmentBase
    {
        // ====================================================================
        // PARÁMETROS DE DISEÑO
        // ====================================================================
        public override string Name { get; }

        public NewVariable<UnitLess> RefluxRelation { get; set; }
        public NewVariable<Pressure> TopPressure { get; }

        /// <summary>
        /// Caída de presión total de la columna (P_fondo = P_tope + ΔP)
        /// </summary>
        public NewVariable<PressureDrop> DeltaP { get; }

        public NewVariable<Pressure> BottomPressure { get; }

        /// <summary>
        /// Lista de alimentaciones (pueden ser múltiples)
        /// </summary>
        public List<IFacadeStream> Feeds { get; private set; } = new();

        /// <summary>
        /// Reflujo que regresa desde el condensador (Tope)
        /// </summary>
        public IFacadeStream? RefluxInlet { get; private set; }

        /// <summary>
        /// Vapor que regresa desde el recalentador (Fondo)
        /// </summary>
        public IFacadeStream? VaporInlet { get; private set; }

        // ====================================================================
        // CORRIENTES DE SALIDA
        // ====================================================================

        /// <summary>
        /// Vapor que sale hacia el condensador (Tope)
        /// </summary>
        public IFacadeStream? VaporOutlet { get; private set; }

        /// <summary>
        /// Líquido que sale hacia el recalentador (Fondo)
        /// </summary>
        public IFacadeStream? BottomOutlet { get; private set; }

        /// <summary>
        /// Extracciones laterales (pueden ser múltiples)
        /// </summary>
        public List<IFacadeStream> SideDraws { get; private set; } = new();

        // ====================================================================
        // PROPIEDADES DEL EQUIPO
        // ====================================================================

        public override List<ISolverEquation> Equations => GetEquations().ToList();

        // ====================================================================
        // CONSTRUCTOR
        // ====================================================================

        public SolverColumn(string name)
        {
            Name = name;
            TopPressure = new NewVariable<Pressure>(new Pressure(101325, PressureUnits.Pascala), PressureUnits.Bara, 100000);
            DeltaP = new NewVariable<PressureDrop>(new PressureDrop(0, PressureDropUnits.Pascal), PressureDropUnits.Bar, 100000);
            BottomPressure = new NewVariable<Pressure>(new Pressure(101325, PressureUnits.Pascala), PressureUnits.Bara, 100000);
            RefluxRelation = new NewVariable<UnitLess>(new UnitLess(1), UnitLessUnits.None, 1);
        }

        // ====================================================================
        // MÉTODOS PARA CONECTAR CORRIENTES
        // ====================================================================

        public void AddFeed(IFacadeStream feed)
        {
            Feeds.Add(feed);
        }

        public void AddSideDraw(IFacadeStream draw)
        {
            SideDraws.Add(draw);
        }

        public void SetRefluxInlet(IFacadeStream stream)
        {
            RefluxInlet = stream;
        }

        public void SetVaporInlet(IFacadeStream stream)
        {
            VaporInlet = stream;
        }

        public void SetTopVaporOutlet(IFacadeStream stream)
        {
            VaporOutlet = stream;
        }

        public void SetBottomOutlet(IFacadeStream stream)
        {
            BottomOutlet = stream;
        }

        // ====================================================================
        // GENERADOR DE ECUACIONES
        // ====================================================================

        private IEnumerable<ISolverEquation> GetEquations()
        {
            // 1. Ecuación de Presión (Entradas a P_fondo, Salidas a P_tope)
            yield return new ColumnPressureTopEquation(this);

            yield return new ColumnPressureDeltaPEquation(this);
            yield return new ColumnPressureBottomEquation(this);

            // 3. Balance de Energía
            yield return new ColumnEnergyBalanceEquation(this);
        }






    }


    // ECUACIÓN 1: Presión del Tope (INDEPENDIENTE)
    public class ColumnPressureTopEquation : ISolverEquation
    {
        private readonly SolverColumn _column;

        public ColumnPressureTopEquation(SolverColumn column) => _column = column;

        public string Name => $"{EquationType} - Top - {_column.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;

        public List<INewVariable> Variables => GetVariables();
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

        private List<INewVariable> GetVariables()
        {
            var variables = new List<INewVariable>();

            variables.Add(_column.TopPressure);

            if (_column.VaporOutlet != null)
            {
                variables.Add(_column.VaporOutlet.Pressure);
            }

            return variables;
        }
    }

    // ECUACIÓN 2: Relación DeltaP (INDEPENDIENTE)
    public class ColumnPressureDeltaPEquation : ISolverEquation
    {
        private readonly SolverColumn _column;

        public ColumnPressureDeltaPEquation(SolverColumn column) => _column = column;

        public string Name => $"{EquationType} - DeltaP - {_column.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

        public List<double> GetResiduals()
        {
            var residuals = new List<double>();

            double pTope = _column.TopPressure.GetSolverValue();
            double pBottom = _column.BottomPressure.GetSolverValue();
            double deltaP = _column.DeltaP.GetSolverValue();

            residuals.Add(pBottom - (pTope + deltaP));

            return residuals;
        }

        private List<INewVariable> GetVariables()
        {
            var variables = new List<INewVariable>();

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

        public ColumnPressureBottomEquation(SolverColumn column) => _column = column;

        public string Name => $"{EquationType} - Bottom - {_column.Name}";
        public SolverEquationType EquationType => SolverEquationType.Pressure;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

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

        private List<INewVariable> GetVariables()
        {
            var variables = new List<INewVariable>();

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

        public ColumnEnergyBalanceEquation(SolverColumn column)
        {
            _column = column;
        }

        public string Name => $"{EquationType} - {_column.Name}";
        public SolverEquationType EquationType => SolverEquationType.MassEnergyBalance;
        public List<double> Residuals => GetResiduals();
        public List<INewVariable> Variables => GetVariables();

        public List<double> GetResiduals()
        {
            var residuals = new List<double>();



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

                for (int i = 0; i < _column.VaporOutlet.Composition.Components.Count - 1; i++)
                {
                    var compo = _column.VaporOutlet.Composition.Components[i];
                    massCompoOut += compo.MassFraction.GetSolverValue() * mVaporOutlet;
                }


            }

            if (_column.BottomOutlet != null)
            {
                double mBottomOutlet = _column.BottomOutlet.MassFlow.GetSolverValue();
                double HBottomOutlet = _column.BottomOutlet.MassEnthalpy.GetSolverValue();
                totalmassOut += mBottomOutlet;
                totalEnergyOut += mBottomOutlet * HBottomOutlet;
                for (int i = 0; i < _column.BottomOutlet.Composition.Components.Count - 1; i++)
                {
                    var compo = _column.BottomOutlet.Composition.Components[i];
                    massCompoOut += compo.MassFraction.GetSolverValue() * mBottomOutlet;
                }

            }
            if (_column.VaporInlet != null)
            {
                double mVaporInlet = _column.VaporInlet.MassFlow.GetSolverValue();
                double HVaporInlet = _column.VaporInlet.MassEnthalpy.GetSolverValue();
                totalmassIn += mVaporInlet;
                totalEnergyIn += mVaporInlet * HVaporInlet;

                for (int i = 0; i < _column.VaporInlet.Composition.Components.Count - 1; i++)
                {
                    var compo = _column.VaporInlet.Composition.Components[i];
                    massCompIn += compo.MassFraction.GetSolverValue() * mVaporInlet;
                }
            }

            if (_column.RefluxInlet != null)
            {
                double mRefluxInlet = _column.RefluxInlet.MassFlow.GetSolverValue();
                double HRefluxInlet = _column.RefluxInlet.MassEnthalpy.GetSolverValue();
                totalmassIn += mRefluxInlet;
                totalEnergyIn += mRefluxInlet * HRefluxInlet;

                for (int i = 0; i < _column.RefluxInlet.Composition.Components.Count - 1; i++)
                {
                    var compo = _column.RefluxInlet.Composition.Components[i];
                    massCompIn += compo.MassFraction.GetSolverValue() * mRefluxInlet;
                }
            }


            foreach (var sidedraw in _column.SideDraws)
            {
                double msidedraw = sidedraw.MassFlow.GetSolverValue();
                double Hsidedraw = sidedraw.MassEnthalpy.GetSolverValue();
                totalmassOut += msidedraw;
                totalEnergyOut += msidedraw * Hsidedraw;
                for (int i = 0; i < sidedraw.Composition.Components.Count - 1; i++)
                {
                    var compo = sidedraw.Composition.Components[i];
                    massCompoOut += compo.MassFraction.GetSolverValue() * msidedraw;
                }
            }
            foreach (var feed in _column.Feeds)
            {
                double mfeed = feed.MassFlow.GetSolverValue();
                double Hfeed = feed.MassEnthalpy.GetSolverValue();
                totalmassIn += mfeed;
                totalEnergyIn += mfeed * Hfeed;
                for (int i = 0; i < feed.Composition.Components.Count - 1; i++)
                {
                    var compo = feed.Composition.Components[i];
                    massCompIn += compo.MassFraction.GetSolverValue() * mfeed;
                }
            }


            // Sumar energía de entrada

            residuals.Add(massCompIn - massCompoOut);
            residuals.Add(totalmassIn - totalmassOut);
            residuals.Add(totalEnergyIn - totalEnergyOut);

            return residuals;
        }

        private List<INewVariable> GetVariables()
        {
            var variables = new List<INewVariable>();

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
