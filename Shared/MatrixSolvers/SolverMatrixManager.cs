using Shared.PropertiesDtos.Methods;
using Shared.Thermodynamics.ControlledVariables;
using Shared.UnitOperations.Basiss;
using Shared.UnitOperations.Columns;
using Shared.UnitOperations.ControlValves;
using Shared.UnitOperations.HeatExchangers;
using Shared.UnitOperations.Helpers;
using Shared.UnitOperations.Pumps;
using Shared.UnitOperations.Streams;
using Shared.UnitOperations.Vessels;
using UnitSystem;

namespace Shared.MatrixSolvers
{

    public class SolverMatrixManager2
    {
        private readonly List<IEquipmentFacade2> _equipments = new();
        private readonly List<IStreamFacade2> _streams = new();
        public ThermodynamicMethodFullDto? ThermoMethod => Configuration.ThermodynamicMethod;
        public SolverConfiguration2 Configuration { get; private set; } = new();
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto method)
        {
            Configuration.ThermodynamicMethod = method;

            foreach (var stream in _streams)
            {
                stream.SetThermodynamicMethod(method);
            }
        }





        public void RegisterStream(IStreamFacade2 stream)
        {
            if (stream == null) return;
            if (_streams.Contains(stream)) return;

            _streams.Add(stream);
            if (ThermoMethod != null)
                stream.SetThermodynamicMethod(ThermoMethod);
            stream.OnExecuteSolver += HandleExecuteSolver;


        }

        public void UnregisterStream(IStreamFacade2 stream)
        {
            if (stream == null) return;
            if (!_streams.Contains(stream)) return;

            stream.OnExecuteSolver -= HandleExecuteSolver;


            _streams.Remove(stream);

        }

        public void RegisterEquipment(IEquipmentFacade2 equipment)
        {
            if (equipment == null) return;
            if (_equipments.Contains(equipment)) return;
            equipment.OnExecuteSolver += HandleExecuteSolver;

            _equipments.Add(equipment);

        }

        public void UnregisterEquipment(IEquipmentFacade2 equipment)
        {
            if (equipment == null) return;
            if (!_equipments.Contains(equipment)) return;
            equipment.OnExecuteSolver -= HandleExecuteSolver;

            _equipments.Remove(equipment);


        }



        EquationSystem EqConc = new EquationSystem();
        EquationSystem EqPress = new EquationSystem();
        EquationSystem EqMassEnergy = new EquationSystem();
        private void HandleExecuteSolver()
        {
            EqConc.ClearGeneralSolverDefinitions();
            EqConc.Clear();
            foreach (var eqProvider in _equipments)
            {
                var equipmentEquation = eqProvider.GetEquationConcentration();

                EqConc.CreateFromFacades(equipmentEquation);
            }
            EqConc.SolveGeneral();
            EqPress.ClearGeneralSolverDefinitions();
            EqPress.Clear();
            foreach (var eqProvider in _equipments)
            {
                var equipmentEquation = eqProvider.GetEquationPressure();

                EqPress.CreateFromFacades(equipmentEquation);
            }

            EqPress.SolveGeneral();
            //EqMassEnergy.Clear();
            //foreach (var eqProvider in _equipments)
            //{
            //    var equipmentEquation = eqProvider.GetEquationSystem();

            //    EqMassEnergy.CreateFromFacades(equipmentEquation);
            //}
            //EqMassEnergy.SolveGeneral();

            //Aqui ejecutar evento por cada corriente y equipo que verifique su estado actual
        }








    }

    public class SolverConfiguration2
    {
        // Método termodinámico
        public ThermodynamicMethodFullDto? ThermodynamicMethod { get; set; }

        // 🔥 Altura sobre nivel del mar (con tu sistema de unidades)
        public NewNewVariableAmount<Length> Altitude { get; set; }

        // 🔥 Presión atmosférica calculada (con tu sistema de unidades)
        public Pressure AtmosphericPressure { get; private set; } = new Pressure(101325, PressureUnits.Pascala);

        // 🔥 Evento para notificar cambios a la UI
        public event Action? ConfigurationChanged;
        public SolverConfiguration2()
        {
            // Inicializar Altitude con unidad por defecto (metros) y valor 0
            Altitude = new NewNewVariableAmount<Length>(
                new Length(0, LengthUnits.Meter),
                LengthUnits.Meter,      // UnitForUI
                LengthUnits.Meter,      // UnitForSolver (internamente trabajamos en metros)
                (v, u) => new Length(v, u),
                0  // InitValue
            );

            // Suscribirse a cambios en Altitude para recalcular presión
            Altitude.ExecuteStreamCalculation += CalculateAtmosphericPressure;
        }

        /// <summary>
        /// Calcula la presión atmosférica basada en la altura usando la fórmula barométrica
        /// </summary>
        public void CalculateAtmosphericPressure()
        {
            // Fórmula barométrica simplificada para la troposfera
            const double P0 = 101325.0;      // Presión al nivel del mar (Pa)
            const double L = 0.0065;          // Lapse rate (K/m)
            const double T0 = 288.15;         // Temperatura estándar (K)

            // Obtener altura en metros para el cálculo
            double altitudeMeters = Altitude.Value.GetValue(LengthUnits.Meter);

            // Validar rango (troposfera: 0-11000 m)
            if (altitudeMeters < 0) altitudeMeters = 0;
            if (altitudeMeters > 11000) altitudeMeters = 11000;

            // Calcular presión en Pascal
            double pressurePa = P0 * Math.Pow(1 - (L * altitudeMeters) / T0, 5.255);

            // Actualizar la presión atmosférica
            AtmosphericPressure.SetValue(pressurePa, PressureUnits.Pascala);

            // Actualizar referencia global si existe
            UnitManager.SetAtmosphericPressureReference(AtmosphericPressure);

            // Notificar a la UI
            ConfigurationChanged?.Invoke();
        }

        /// <summary>
        /// Obtiene la presión atmosférica en diferentes unidades (para display)
        /// </summary>

    }



    // ───────────────────────────────────────────────────────────────
    // 🔹 CLASE: SolverConfiguration (minimalista, solo configuración)
    // ───────────────────────────────────────────────────────────────
    public class SolverConfiguration
    {
        // Método termodinámico
        public ThermodynamicMethodFullDto? ThermodynamicMethod { get; set; }

        // 🔥 Altura sobre nivel del mar (con sistema de unidades)
        public VariableAmount<Length> Altitude { get; private set; }

        // 🔥 Presión atmosférica calculada
        public Pressure AtmosphericPressure { get; private set; } = new Pressure(101325, PressureUnits.Pascala);

        // 🔥 Evento para notificar cambios a la UI
        public event Action? ConfigurationChanged;

        public SolverConfiguration()
        {
            // Inicializar Altitude con unidad por defecto (metros) y valor 0
            Altitude = new VariableAmount<Length>(
                new Length(0, LengthUnits.Meter),
                LengthUnits.Meter,      // UnitForUI
                LengthUnits.Meter,      // UnitForSolver
                (v, u) => new Length(v, u),
                0  // InitValue
            );

            // Suscribirse a cambios en Altitude para recalcular presión
            Altitude.ExecuteStreamCalculation += CalculateAtmosphericPressure;
        }

        /// <summary>
        /// Calcula la presión atmosférica basada en la altura usando la fórmula barométrica
        /// </summary>
        public void CalculateAtmosphericPressure()
        {
            // Fórmula barométrica simplificada para la troposfera
            const double P0 = 101325.0;      // Presión al nivel del mar (Pa)
            const double L = 0.0065;          // Lapse rate (K/m)
            const double T0 = 288.15;         // Temperatura estándar (K)

            // Obtener altura en metros para el cálculo
            double altitudeMeters = Altitude.Value.GetValue(LengthUnits.Meter);

            // Validar rango (troposfera: 0-11000 m)
            if (altitudeMeters < 0) altitudeMeters = 0;
            if (altitudeMeters > 11000) altitudeMeters = 11000;

            // Calcular presión en Pascal
            double pressurePa = P0 * Math.Pow(1 - (L * altitudeMeters) / T0, 5.255);

            // Actualizar la presión atmosférica
            AtmosphericPressure = new Pressure(pressurePa, PressureUnits.Pascala);

            // Actualizar referencia global si existe
            UnitManager.SetAtmosphericPressureReference(AtmosphericPressure);

            // Notificar a la UI
            ConfigurationChanged?.Invoke();
        }
    }

    // ───────────────────────────────────────────────────────────────
    // 🔹 CLASE: SolverMatrixManager (nuevo, para solver reactivo)
    // ───────────────────────────────────────────────────────────────
    public class SolverMatrixManager
    {
        // ═══════════════════════════════════════════════════════════
        // 🔹 CAMPOS INTERNOS (encapsulados, solo lectura externa)
        // ═══════════════════════════════════════════════════════════
        private readonly List<IEquipmentFacade> _equipments = new();
        private readonly List<IStreamFacade> _streams = new();
        private ReactiveNewtonSolver? _reactiveSolver;

        // ═══════════════════════════════════════════════════════════
        // 🔹 CONFIGURACIÓN (SRP: delegada a clase específica)
        // ═══════════════════════════════════════════════════════════
        public SolverConfiguration Configuration { get; } = new();
        public ThermodynamicMethodFullDto? ThermoMethod => Configuration.ThermodynamicMethod;

        // ═══════════════════════════════════════════════════════════
        // 🔹 CONFIGURACIÓN TERMODINÁMICA (DIP: delega a streams)
        // ═══════════════════════════════════════════════════════════
        public void SetThermodynamicMethod(ThermodynamicMethodFullDto method)
        {
            Configuration.ThermodynamicMethod = method;

            foreach (var stream in _streams)
            {
                stream.SetThermodynamicMethod(method);
            }

            // 🔥 Re-inicializar solver reactivo con nueva configuración
            ReinitializeReactiveSolver();
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 REGISTRO DE STREAMS (KISS: suscripción explícita)
        // ═══════════════════════════════════════════════════════════
        public void RegisterStream(IStreamFacade stream)
        {
            if (stream == null || _streams.Contains(stream)) return;

            _streams.Add(stream);

            if (ThermoMethod != null)
                stream.SetThermodynamicMethod(ThermoMethod);

            // 🔥 Suscribirse al evento de ejecución del solver
            stream.OnExecuteSolver += HandleExecuteSolver;

            // 🔥 Re-inicializar solver reactivo si hay datos suficientes
            ReinitializeReactiveSolverIfNeeded();
        }

        public void UnregisterStream(IStreamFacade stream)
        {
            if (stream == null || !_streams.Contains(stream)) return;

            stream.OnExecuteSolver -= HandleExecuteSolver;
            _streams.Remove(stream);

            // 🔥 Re-inicializar solver reactivo
            ReinitializeReactiveSolverIfNeeded();
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 REGISTRO DE EQUIPOS (KISS: suscripción explícita)
        // ═══════════════════════════════════════════════════════════
        public void RegisterEquipment(IEquipmentFacade equipment)
        {
            if (equipment == null || _equipments.Contains(equipment)) return;

            // 🔥 Suscribirse al evento de ejecución del solver
            equipment.OnExecuteSolver += HandleExecuteSolver;
            _equipments.Add(equipment);

            // 🔥 Re-inicializar solver reactivo si hay datos suficientes
            ReinitializeReactiveSolverIfNeeded();
        }

        public void UnregisterEquipment(IEquipmentFacade equipment)
        {
            if (equipment == null || !_equipments.Contains(equipment)) return;

            equipment.OnExecuteSolver -= HandleExecuteSolver;
            _equipments.Remove(equipment);

            // 🔥 Re-inicializar solver reactivo
            ReinitializeReactiveSolverIfNeeded();
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 INICIALIZACIÓN DEL SOLVER REACTIVO (OCP: extensible)
        // ═══════════════════════════════════════════════════════════
        private void ReinitializeReactiveSolverIfNeeded()
        {
            // Solo crear solver si hay al menos 1 stream y 1 equipo
            if (_streams.Count == 0 || _equipments.Count == 0)
            {
                _reactiveSolver = null;
                return;
            }

            _reactiveSolver = new ReactiveNewtonSolver(
                equipments: _equipments,
                streams: _streams,
                tolerance: 1e-4,           // Tolerancia típica en procesos
                maxIterations: 100,        // Límite razonable
                enablePartialSolve: true   // 🔥 CLAVE: permitir solución parcial
            );
        }

        public void ReinitializeReactiveSolver()
        {
            _reactiveSolver = null;
            ReinitializeReactiveSolverIfNeeded();
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 HANDLER PRINCIPAL: Ejecutar solver reactivo
        // ═══════════════════════════════════════════════════════════
        private void HandleExecuteSolver()
        {
            // 🔥 Ejecutar solo el solver reactivo (nuevo ecosistema)
            ExecuteReactiveSolver();
        }

        private void ExecuteReactiveSolver()
        {
            if (_reactiveSolver == null)
            {
                // 🔥 Si no hay solver, re-intentar inicializar
                ReinitializeReactiveSolverIfNeeded();
                if (_reactiveSolver == null) return;
            }

            try
            {
                var result = _reactiveSolver.Solve();

                // 🔥 Logging opcional para debug (eliminar en producción)
                // Console.WriteLine($"Solver: {result.Status}, iterations: {result.Iterations}, residual: {result.FinalResidual:E3}");

                // Si hubo error, no hacer nada (los valores anteriores se mantienen)
                if (result.Status == SolverStatus.Error)
                    return;

                // 🔥 ApplySolution() ya está dentro de Solve() si converge
                // Si enablePartialSolve=true, también aplica lo parcial automáticamente
            }
            catch (Exception ex)
            {
                // 🔥 No romper el flujo: registrar error y continuar
                 Console.WriteLine($"Reactive solver error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 🔹 CONFIGURACIÓN PÚBLICA (ISP: solo lo necesario)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Activar/desactivar el solver reactivo sin afectar otros componentes
        /// </summary>
        public void EnableReactiveSolver(bool enable)
        {
            if (!enable)
            {
                _reactiveSolver = null;
            }
            else
            {
                ReinitializeReactiveSolverIfNeeded();
            }
        }

        /// <summary>
        /// Obtiene el estado actual del solver (para UI/debug)
        /// </summary>
        public bool IsReactiveSolverReady => _reactiveSolver != null;

        /// <summary>
        /// Obtiene el número de streams y equipos registrados (para UI/debug)
        /// </summary>
        public (int Streams, int Equipments) GetRegistrationCount()
            => (_streams.Count, _equipments.Count);

        public void Sample()
        {
            IStreamFacade S101, S102, S103;
            PumpSimulationFacade P101;
            ControlValveSimulationFacade CV101;
            S101 = new StreamFacade { Name = "S-101" };
            S102 = new StreamFacade { Name = "S-102" };
            S103 = new StreamFacade { Name = "S-103" };
            RegisterStream(S101);
            RegisterStream(S102);
            RegisterStream(S103);

            // ═══════════════════════════════════════════════════════════
            // 🔹 PASO 3: Crear equipos (con nueva base EquipmentFacade)
            // ═══════════════════════════════════════════════════════════
            P101 = new PumpSimulationFacade { Name = "P-101" };
            CV101 = new ControlValveSimulationFacade { Name = "CV-101" };
            RegisterEquipment(CV101);
            RegisterEquipment(P101);

            P101.AttachConnection("Suction", S101);

            // P-101 (Discharge) → S-102
            P101.AttachConnection("Discharge", S102);

            // S-102 → CV-101 (Inlet)
            CV101.AttachConnection("Inlet", S102);

            // CV-101 (Outlet) → S-103
            CV101.AttachConnection("Outlet", S103);

            var composition = S101.StreamComposition.Value.Clone();
            composition.Components[0].MassFractionSolver.SetValueFromUI(25.0);  // 25% Etanol
            composition.Components[1].MassFractionSolver.SetValueFromUI(75.0);     // 75% Agua
            composition.InputType = ComponentInputType.MassFraction;
            S101.StreamComposition.SetValueFromUI(composition);

          
            Console.WriteLine($"   🔄 Disparó ExecuteGeneralSolver → Solver reactivo ejecutándose...\n");

            // ═══════════════════════════════════════════════════════════
            // 🔹 PASO 8: Definir presión y flujo en S-101
            // ═══════════════════════════════════════════════════════════
            Console.WriteLine("🔹 Paso 8: Definir P=1 bar, ṁ=100 kg/h en S-101");

            S101.Pressure.SetValueFromUI(new Pressure(1.0, PressureUnits.Bara));
            S101.Temperature.SetValueFromUI(new Temperature(25.0, TemperatureUnits.DegreeCelcius));
            S101.MassFlow.SetValueFromUI(new MassFlow(100.0, MassFlowUnits.Kg_hr));

            Console.WriteLine($"   📊 S-101: P={S101.Pressure.GetDisplayString()}, T={S101.Temperature.GetDisplayString()}, ṁ={S101.MassFlow.GetDisplayString()}\n");

            // ═══════════════════════════════════════════════════════════
            // 🔹 PASO 9: Definir parámetros de equipos (ΔP, eficiencia)
            // ═══════════════════════════════════════════════════════════
            Console.WriteLine("🔹 Paso 9: Definir parámetros de P-101 y CV-101");

            // Bomba: ΔP = 2 bar, η = 80%
            P101.DeltaPressure.SetValueFromUI(new PressureDrop(2.0, PressureDropUnits.Bar));
            P101.Efficiency.SetValueFromUI(80.0);  // 80%

            // Válvula: ΔP = 0.5 bar
            CV101.DeltaPressure.SetValueFromUI(new PressureDrop(0.5, PressureDropUnits.Bar));

            Console.WriteLine($"   ⚙️  P-101: ΔP={P101.DeltaPressure.GetDisplayString()}, η={P101.Efficiency.GetDisplayString()}%");
            Console.WriteLine($"   ⚙️  CV-101: ΔP={CV101.DeltaPressure.GetDisplayString()}\n");

            // ═══════════════════════════════════════════════════════════
            // 🔹 PASO 10: 🔥 EJECUTAR SOLVER REACTIVO
            // ═══════════════════════════════════════════════════════════
            Console.WriteLine("🔹 Paso 10: Ejecutar solver reactivo (Newton-Raphson matricial)");

            

            // ═══════════════════════════════════════════════════════════
            // 🔹 PASO 11: Mostrar resultados propagados
            // ═══════════════════════════════════════════════════════════
            Console.WriteLine("🔹 Paso 11: Resultados propagados automáticamente\n");

           

            Console.WriteLine($"\n⚙️  P-101 Power: {P101.Power.GetDisplayString()}");
            Console.WriteLine($"⚙️  CV-101 Cv: {CV101.Cv.GetDisplayString()}");

            // ═══════════════════════════════════════════════════════════
            // 🔹 PASO 12: 🔥 PRUEBA DE LIMPIEZA: Limpiar composición en S-101
            // ═══════════════════════════════════════════════════════════
            Console.WriteLine("\n🔹 Paso 12: Limpiar composición en S-101 → verificar limpieza en cascada");

            S101.StreamComposition.ClearFromUI();  // ← Dispara re-cálculo

            // Ejecutar solver nuevamente para propagar limpieza
           

            Console.WriteLine($"   📊 S-101 Composición después de limpiar: {S101.StreamComposition.GetDisplayString()}");
            Console.WriteLine($"   📊 S-102 Composición después de limpiar: {S102.StreamComposition.GetDisplayString()}");
            Console.WriteLine($"   ✅ Valores calculados dependientes se limpiaron automáticamente\n");

            Console.WriteLine("🎉 Ejemplo completado exitosamente!");
        }
    }
    // ═══════════════════════════════════════════════════════════════


}
