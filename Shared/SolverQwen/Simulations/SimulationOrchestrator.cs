using Shared.MatrixSolvers;
using Shared.SolverQwen.Equipments;
using Shared.SolverQwen.Variables;
using System.Diagnostics;

namespace Shared.SolverQwen.Simlations
{   // Un contenedor simple para nuestras tareas

    public interface ISimulationSystem
    {


        double[] GetResiduals();
        List<IProcessVariable> CouplingVariables { get; }
    }


    public class SimulationOrchestrator2
    {
        private readonly List<IEquipment> _equipments = new();
        private readonly NewtonRaphsonSolver _globalSolver = new();

        public void AddEquipment(IEquipment equipment)
        {
            if (!_equipments.Contains(equipment))
                _equipments.Add(equipment);
        }

        /// <summary>
        /// Contenedor ligero para manejar las estrategias dentro de la Cola Dinámica.
        /// </summary>
        private class PipelineTask
        {
            public List<ISolverPhaseStrategy> Subsystem { get; set; } = new();
            public VariableDataProcedence Procedence { get; set; }
            public StrategyType Type { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        /// <summary>
        /// Ejecuta la simulación unificada usando un algoritmo de Cola Dinámica (Active Set).
        /// </summary>
        public void RunSimulation()
        {
            var swTotal = Stopwatch.StartNew();
            Console.WriteLine($"\n[Orch] >>> INICIANDO RunSimulation");

            // 1. FASE 0 Y 1: Limpieza y Propagación Local
            var swPhase1 = Stopwatch.StartNew();
            ResetSimulationState();
            RunPhase1LocalPropagation();
            swPhase1.Stop();
            Console.WriteLine($"[Orch] Reset + Phase1: {swPhase1.ElapsedMilliseconds} ms");

            // 2. CONSTRUIR LA COLA DE TAREAS (Orden estricto de ejecución)
            var pendingTasks = new List<PipelineTask>();
            var swBuild = Stopwatch.StartNew();

            // Orden 1: Presiones (Fase 2)
            var swPress = Stopwatch.StartNew();
            pendingTasks.AddRange(BuildTasks(StrategyType.Pressure, VariableDataProcedence.Phase2_EasyEquipmentNet));
            swPress.Stop();
            Console.WriteLine($"[Orch] └─ Build Pressure: {swPress.ElapsedMilliseconds} ms");

            // Orden 2: Concentraciones (Fase 2)
            var swConc = Stopwatch.StartNew();
            pendingTasks.AddRange(BuildTasks(StrategyType.Concentration, VariableDataProcedence.Phase2_EasyEquipmentNet));
            swConc.Stop();
            Console.WriteLine($"[Orch] └─ Build Concentration: {swConc.ElapsedMilliseconds} ms");

            // Orden 3: Entalpías y Flash (Fase 2)
            var swEnth = Stopwatch.StartNew();
            pendingTasks.AddRange(BuildTasks(StrategyType.Enthalpy, VariableDataProcedence.Phase2_EasyEquipmentNet));
            swEnth.Stop();
            Console.WriteLine($"[Orch] └─ Build Enthalpy: {swEnth.ElapsedMilliseconds} ms");

            // Orden 4: Masas de Equipos Fáciles (Fase 2)
            var swMass = Stopwatch.StartNew();
            pendingTasks.AddRange(BuildTasks(StrategyType.MassBalance, VariableDataProcedence.Phase2_EasyEquipmentNet));
            swMass.Stop();
            Console.WriteLine($"[Orch] └─ Build MassBalance: {swMass.ElapsedMilliseconds} ms");

            // Orden 5: Matriz Global de Masa y Energía (Fase 3)
            var swPhase3 = Stopwatch.StartNew();
            pendingTasks.AddRange(BuildTasks(StrategyType.MassEnergyBalance, VariableDataProcedence.Phase3_ThermoAdjustment));
            swPhase3.Stop();
            Console.WriteLine($"[Orch] └─ Build MassEnergyBalance: {swPhase3.ElapsedMilliseconds} ms");

            swBuild.Stop();
            Console.WriteLine($"[Orch] TOTAL BuildTasks: {swBuild.ElapsedMilliseconds} ms | {pendingTasks.Count} tareas en cola");

            int maxIter = 30;
            int iter = 0;
            long totalSolverTime = 0;
            int totalSolverCalls = 0;

            // 3. EL BUCLE UNIFICADO ENCOGIBLE
            var swLoop = Stopwatch.StartNew();
            while (pendingTasks.Count > 0 && iter < maxIter)
            {
                var swIterStart = Stopwatch.StartNew();
                bool anyMovementInThisIter = false;
                int tasksProcessed = 0;

                // REGLA 1: Usar 'for' clásico en lugar de 'foreach' para poder modificar la lista en vivo.
                for (int i = 0; i < pendingTasks.Count;)
                {
                    var task = pendingTasks[i];

                    // Ejecutamos la tarea y leemos su diagnóstico
                    var swTask = Stopwatch.StartNew();
                    var (isConverged, numbersMoved) = ExecuteAndCheckTask(task);
                    swTask.Stop();

                    totalSolverTime += swTask.ElapsedMilliseconds;
                    totalSolverCalls++;
                    tasksProcessed++;

                    if (numbersMoved)
                    {
                        anyMovementInThisIter = true;
                    }

                    // REGLA 3: Extremar medidas. Si la tarea se resolvió (convergió y ya no mueve números),
                    // la SACAMOS de la lista para no volver a iterar sobre ella inútilmente.
                    if (isConverged && !numbersMoved)
                    {
                        pendingTasks.RemoveAt(i);
                        // NOTA: Al hacer RemoveAt, el elemento que estaba en i+1 rueda a la posición 'i'.
                        // Por lo tanto, NO incrementamos 'i' para no saltarnos el nuevo elemento.
                    }
                    else
                    {
                        // Si no ha convergido o sus números aún se están moviendo, la dejamos en la cola.
                        // Avanzamos al siguiente elemento.
                        i++;
                    }
                }

                swIterStart.Stop();
                Console.WriteLine($"[Orch] Iter {iter}: {tasksProcessed} tareas, {pendingTasks.Count} restantes, {(anyMovementInThisIter ? "movimiento" : "estable")}, {swIterStart.ElapsedMilliseconds} ms");

                // CORTOCIRCUITO: Si recorrimos toda la cola sobrante y nada se movió,
                // estamos en un punto muerto por falta de grados de libertad (datos de UI).
                if (!anyMovementInThisIter && pendingTasks.Count > 0)
                {
                    Console.WriteLine($"⚠️ [Orch] Bucle detenido temprano (Iter {iter}). La planta está en espera de datos de entrada.");
                    break;
                }

                iter++;
            }
            swLoop.Stop();

            swTotal.Stop();

            // Reporte final de rendimiento
            Console.WriteLine($"\n[Orch] <<< RunSimulation COMPLETADA");
            Console.WriteLine($"[Orch] Tiempo total: {swTotal.ElapsedMilliseconds} ms");
            Console.WriteLine($"[Orch] Iteraciones del bucle: {iter}");
            Console.WriteLine($"[Orch] Tiempo en bucle principal: {swLoop.ElapsedMilliseconds} ms");
            Console.WriteLine($"[Orch] Llamadas a ExecuteAndCheckTask: {totalSolverCalls}");
            Console.WriteLine($"[Orch] Tiempo total en solver (acumulado): {totalSolverTime} ms");
            if (totalSolverCalls > 0)
                Console.WriteLine($"[Orch] Promedio por llamada a solver: {totalSolverTime / (double)totalSolverCalls:F2} ms");

            if (pendingTasks.Count == 0)
                Console.WriteLine($"✅ CONVERGENCIA TOTAL. Todas las estrategias estabilizadas en {iter} iteraciones.");
            else if (iter >= maxIter)
                Console.WriteLine($"❌ ERROR: Límite de {maxIter} iteraciones alcanzado. {pendingTasks.Count} tareas quedaron sin resolver.");
        }

        // ─────────────────────────────────────────────────────────
        // MÉTODOS DE EJECUCIÓN Y AUDITORÍA DE TAREAS
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Evalúa la tarea y retorna si convergió matemáticamente y si causó algún cambio real en los números.
        /// </summary>
        private (bool isConverged, bool numbersMoved) ExecuteAndCheckTask(PipelineTask task)
        {
            var sw = Stopwatch.StartNew();

            var activeVars = task.Subsystem.SelectMany(s => s.GetCouplingVariables()).Distinct().ToList();
            var oldValues = activeVars.Select(v => v.GetSolverValue()).ToArray();

            var swAdapter = Stopwatch.StartNew();
            var systemWrapper = new OrchestratorSystemAdapter(task.Subsystem, task.Procedence);
            swAdapter.Stop();

            // Si el adaptador no encuentra variables activas, lo damos por resuelto sin cambios.
            if (!systemWrapper.CouplingVariables.Any())
            {
                sw.Stop();
                return (true, false);
            }

            var swSolve = Stopwatch.StartNew();
            var result = _globalSolver.Solve(systemWrapper, task.Procedence);
            swSolve.Stop();

            // Si falló por falta de datos o matriz singular
            if (!result.Converged)
            {
                sw.Stop();
                return (false, false);
            }

            var newValues = activeVars.Select(v => v.GetSolverValue()).ToArray();
            for (int i = 0; i < oldValues.Length; i++)
            {
                if (Math.Abs(oldValues[i] - newValues[i]) > 1e-8)
                {
                    // Convergió, pero los números sufrieron un cambio. Requerirá otra revisión.
                    sw.Stop();
                    return (true, true);
                }
            }

            sw.Stop();
            // Descomenta la siguiente línea si quieres ver timing de cada tarea individual (puede ser muy verboso)
            // Console.WriteLine($"[Task] {task.Name}: {sw.ElapsedMilliseconds} ms (adapter:{swAdapter.ElapsedMilliseconds}, solve:{swSolve.ElapsedMilliseconds})");

            // Convergió perfectamente y los números están en estado estacionario.
            return (true, false);
        }



        // ─────────────────────────────────────────────────────────
        // FASE 0 & 1: PREPARACIÓN Y PROPAGACIÓN LINEAL
        // ─────────────────────────────────────────────────────────
        private void ResetSimulationState()
        {
            var sw = Stopwatch.StartNew();

            var allVariables = _equipments
                .SelectMany(e => e.GetStrategies())
                .SelectMany(s => s.GetCouplingVariables())
                .Distinct()
                .ToList();

            foreach (var variable in allVariables)
            {
                if (variable.DataProcedence != VariableDataProcedence.UserInput &&
                    variable.DataProcedence != VariableDataProcedence.StreamCalculated)
                {
                    variable.ResetProcedence();
                }
            }
            sw.Stop();
            Console.WriteLine($"[Orch] ResetSimulationState: {sw.ElapsedMilliseconds} ms");
        }

        private void RunPhase1LocalPropagation()
        {
            var swTotal = Stopwatch.StartNew();
            int strategiesProcessed = 0;

            foreach (var eq in _equipments)
            {
                var phase1Strategies = eq.GetStrategies()
                    .Where(s => s.Procedence == VariableDataProcedence.Phase1_LocalPropagation)
                    .ToList();

                foreach (var strategy in phase1Strategies)
                {
                    var localAdapter = new OrchestratorSystemAdapter(
                        new List<ISolverPhaseStrategy> { strategy },
                        strategy.Procedence
                    );

                    if (localAdapter.CouplingVariables.Any())
                    {
                        var localSolver = new NewtonRaphsonSolver { MaxIterations = 10 };
                        localSolver.Solve(localAdapter, strategy.Procedence);
                        strategiesProcessed++;
                    }
                }
            }
            swTotal.Stop();
            Console.WriteLine($"[Orch] Phase1LocalPropagation: {swTotal.ElapsedMilliseconds} ms, {strategiesProcessed} estrategias ejecutadas");
        }

        // ─────────────────────────────────────────────────────────
        // TOPOLOGÍA (BFS)
        // ─────────────────────────────────────────────────────────
        private List<List<ISolverPhaseStrategy>> BuildSubsystemsByType(StrategyType type, VariableDataProcedence phase)
        {
            var swTotal = Stopwatch.StartNew();

            var subsystems = new List<List<ISolverPhaseStrategy>>();

            var swCollect = Stopwatch.StartNew();
            var allStrategies = _equipments
                .SelectMany(e => e.GetStrategies())
                .Where(s => s.Type == type && s.Procedence == phase)
                .ToList();
            swCollect.Stop();

            var visitedStrategies = new HashSet<ISolverPhaseStrategy>();
            int bfsIterations = 0;
            int strategiesGrouped = 0;

            foreach (var startStrategy in allStrategies)
            {
                if (visitedStrategies.Contains(startStrategy)) continue;

                var currentSubsystem = new List<ISolverPhaseStrategy>();
                var queue = new Queue<ISolverPhaseStrategy>();

                visitedStrategies.Add(startStrategy);
                currentSubsystem.Add(startStrategy);
                queue.Enqueue(startStrategy);

                while (queue.Count > 0)
                {
                    bfsIterations++;
                    var currentStrategy = queue.Dequeue();
                    var currentVars = new HashSet<IProcessVariable>(currentStrategy.GetCouplingVariables());

                    foreach (var candidate in allStrategies)
                    {
                        if (visitedStrategies.Contains(candidate)) continue;

                        var candidateVars = candidate.GetCouplingVariables();

                        if (candidateVars.Any(v => currentVars.Contains(v)))
                        {
                            visitedStrategies.Add(candidate);
                            currentSubsystem.Add(candidate);
                            queue.Enqueue(candidate);
                            strategiesGrouped++;
                        }
                    }
                }

                subsystems.Add(currentSubsystem);
            }

            swTotal.Stop();
            Console.WriteLine($"[BFS] {type}_{phase}: {swTotal.ElapsedMilliseconds} ms | estrategias:{allStrategies.Count} | grupos:{subsystems.Count} | iteraciones BFS:{bfsIterations}");

            return subsystems;
        }
        // ─────────────────────────────────────────────────────────
        // 🔹 FASE 3: CONSTRUCCIÓN DE SUBSISTEMAS POR RAMAL (NUEVO)
        // ─────────────────────────────────────────────────────────
        /// <summary>
        /// Construye subsistemas para Phase 3 usando estrategia "por ramal".
        /// 
        /// 🔹 Diferencia clave con Phase 2:
        /// - NO usa BFS para agrupar estrategias por variables compartidas
        /// - Cada estrategia es su propio subsistema (resolución independiente)
        /// - Permite que equipos se resuelvan parcialmente (ej: solo lado caliente de HEX)
        /// - El OrchestratorSystemAdapter filtrará estrategias vacías (sin specs)
        /// </summary>
        private List<List<ISolverPhaseStrategy>> BuildPhase3SubsystemsByRamal(StrategyType type, VariableDataProcedence phase)
        {
            var swTotal = Stopwatch.StartNew();

            // Recolectar estrategias de Phase 3 del tipo solicitado
            var allStrategies = _equipments
                .SelectMany(e => e.GetStrategies())
                .Where(s => s.Type == type && s.Procedence == phase)
                .ToList();

            // 🔹 ESTRATEGIA "POR RAMAL": Cada estrategia es su propio subsistema
            // Esto permite resolución independiente y evita acoplamientos prematuros
            var subsystems = allStrategies
                .Select(s => new List<ISolverPhaseStrategy> { s })
                .ToList();

            swTotal.Stop();

            // Logging mínimo para auditoría (sin verbosidad excesiva)
            Console.WriteLine($"[Phase3-Ramal] {type}_{phase}: {swTotal.ElapsedMilliseconds} ms | estrategias:{allStrategies.Count} | ramales:{subsystems.Count}");

            return subsystems;
        }
        private List<PipelineTask> BuildTasks(StrategyType type, VariableDataProcedence procedence)
        {
            var sw = Stopwatch.StartNew();

            // 🔹 DISPATCHER: Usar builder específico según la fase
            var subsystems = procedence == VariableDataProcedence.Phase3_ThermoAdjustment
                ? BuildPhase3SubsystemsByRamal(type, procedence)  // ← NUEVO: Path exclusivo Phase 3
                : BuildSubsystemsByType(type, procedence);         // ← ORIGINAL: Path Phase 1/2 (intacto)

            sw.Stop();

            var result = subsystems.Select(sub => new PipelineTask
            {
                Subsystem = sub,
                Procedence = procedence,
                Type = type,
                Name = $"{type}_{procedence}"
            }).ToList();

            Console.WriteLine($"[Build] {type}_{procedence}: {sw.ElapsedMilliseconds} ms, {subsystems.Count} tareas");
            return result;
        }
        //private List<PipelineTask> BuildTasks(StrategyType type, VariableDataProcedence procedence)
        //{
        //    var sw = Stopwatch.StartNew();
        //    var subsystems = BuildSubsystemsByType(type, procedence);
        //    sw.Stop();

        //    var result = subsystems.Select(sub => new PipelineTask
        //    {
        //        Subsystem = sub,
        //        Procedence = procedence,
        //        Type = type,
        //        Name = $"{type}_{procedence}"
        //    }).ToList();

        //    Console.WriteLine($"[Build] {type}_{procedence}: {sw.ElapsedMilliseconds} ms, {subsystems.Count} subsistemas");
        //    return result;
        //}
    }


    /// <summary>
    /// Orquestador de simulación que ejecuta estrategias de equipos en fases ordenadas.
    /// 
    /// Arquitectura refactorizada:
    /// - Phase 1: Propagación local (determinista)
    /// - Phase 2: Redes acopladas (BFS por variables compartidas)
    /// - Phase 3: Ajuste termodinámico (por ramal, estrategias independientes)
    /// </summary>
    public class SimulationOrchestrator
    {
        private readonly List<IEquipment> _equipments = new();
        private readonly NewtonRaphsonSolver _globalSolver = new();

        public void AddEquipment(IEquipment equipment)
        {
            if (!_equipments.Contains(equipment))
                _equipments.Add(equipment);
        }

        /// <summary>
        /// Contenedor ligero para manejar las estrategias dentro de la Cola Dinámica.
        /// </summary>
        private class PipelineTask
        {
            public List<ISolverPhaseStrategy> Subsystem { get; set; } = new();
            public VariableDataProcedence Procedence { get; set; }
            public StrategyType Type { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        /// <summary>
        /// Resultado de ejecución de una fase.
        /// </summary>
        private class PhaseResult
        {
            public string PhaseName { get; set; } = string.Empty;
            public bool Converged { get; set; }
            public int PendingTasks { get; set; }
            public int Iterations { get; set; }
            public long ElapsedMs { get; set; }
            public long LoopTimeMs { get; set; }
            public long SolverTimeMs { get; set; }
            public int SolverCalls { get; set; }
            public bool LastMovement { get; set; }
        }

        /// <summary>
        /// Ejecuta la simulación unificada orquestando las fases en orden.
        /// </summary>
        public void RunSimulation()
        {
            var swTotal = Stopwatch.StartNew();
            Console.WriteLine($"\n[Orch] >>> INICIANDO RunSimulation");

            // ─────────────────────────────────────────────────────────
            // FASE 1: Limpieza y Propagación Local
            // ─────────────────────────────────────────────────────────
            var phase1Result = RunPhase1WithPropagation();

            // ─────────────────────────────────────────────────────────
            // FASE 2: Propagación de Redes (Presión, Masa, Composición, Entalpía)
            // ─────────────────────────────────────────────────────────
            var phase2Result = RunPhase2Propagation();

            // ─────────────────────────────────────────────────────────
            // FASE 3: Ajuste Termodinámico Global (Balances de Masa/Energía)
            // ─────────────────────────────────────────────────────────
            var phase3Result = RunPhase3ThermoAdjustment();

            // ─────────────────────────────────────────────────────────
            // REPORTE FINAL CONSOLIDADO
            // ─────────────────────────────────────────────────────────
            swTotal.Stop();
            PrintFinalReport(phase1Result, phase2Result, phase3Result, swTotal.ElapsedMilliseconds);
        }

        /// <summary>
        /// Ejecuta Fase 1: Reset de estado + propagación local.
        /// </summary>
        private PhaseResult RunPhase1WithPropagation()
        {
            var sw = Stopwatch.StartNew();

            ResetSimulationState();
            RunPhase1LocalPropagation();

            sw.Stop();

            Console.WriteLine($"[Orch-Phase1] Completado: {sw.ElapsedMilliseconds} ms");

            return new PhaseResult
            {
                PhaseName = "Phase1",
                ElapsedMs = sw.ElapsedMilliseconds,
                Converged = true,
                PendingTasks = 0
            };
        }

        /// <summary>
        /// Ejecuta Fase 2: Propagación de redes acopladas.
        /// Usa BFS para agrupar estrategias por variables compartidas.
        /// </summary>
        private PhaseResult RunPhase2Propagation()
        {
            Console.WriteLine($"\n[Orch] >>> EJECUTANDO FASE 2: Propagación de Redes");

            var phase2Tasks = new List<PipelineTask>();
            var swBuild = Stopwatch.StartNew();

            phase2Tasks.AddRange(BuildTasks(StrategyType.Pressure, VariableDataProcedence.Phase2_EasyEquipmentNet));
            phase2Tasks.AddRange(BuildTasks(StrategyType.Concentration, VariableDataProcedence.Phase2_EasyEquipmentNet));
            phase2Tasks.AddRange(BuildTasks(StrategyType.Enthalpy, VariableDataProcedence.Phase2_EasyEquipmentNet));
            phase2Tasks.AddRange(BuildTasks(StrategyType.MassBalance, VariableDataProcedence.Phase2_EasyEquipmentNet));

            swBuild.Stop();
            Console.WriteLine($"[Orch-Phase2] Build: {swBuild.ElapsedMilliseconds} ms | {phase2Tasks.Count} tareas");

            var loopResult = ExecutePhaseLoop(
                tasks: phase2Tasks,
                phaseName: "Phase2",
                maxIterations: 10,
                logPrefix: "[Orch-Phase2]"
            );

            Console.WriteLine($"\n[Orch] <<< FASE 2 COMPLETADA");
            Console.WriteLine($"[Orch-Phase2] Iteraciones: {loopResult.Iterations} | Tiempo bucle: {loopResult.LoopTimeMs} ms");

            if (loopResult.Converged)
                Console.WriteLine($"✅ FASE 2: Convergencia total de propagación de redes.");
            else
                Console.WriteLine($"❌ FASE 2: {loopResult.PendingTasks} tareas pendientes tras {loopResult.Iterations} iteraciones.");

            return loopResult;
        }

        /// <summary>
        /// Ejecuta Fase 3: Ajuste termodinámico global.
        /// Usa estrategia "por ramal": cada estrategia es su propio subsistema.
        /// </summary>
        private PhaseResult RunPhase3ThermoAdjustment()
        {
            Console.WriteLine($"\n[Orch] >>> EJECUTANDO FASE 3: Ajuste Termodinámico");

            var phase3Tasks = new List<PipelineTask>();
            var swBuild = Stopwatch.StartNew();

            phase3Tasks.AddRange(BuildTasks(StrategyType.MassEnergyBalance, VariableDataProcedence.Phase3_ThermoAdjustment));

            swBuild.Stop();
            Console.WriteLine($"[Orch-Phase3] Build: {swBuild.ElapsedMilliseconds} ms | {phase3Tasks.Count} tareas");

            var loopResult = ExecutePhaseLoop(
                tasks: phase3Tasks,
                phaseName: "Phase3",
                maxIterations: 30,
                logPrefix: "[Orch-Phase3]"
            );

            Console.WriteLine($"\n[Orch] <<< FASE 3 COMPLETADA");
            Console.WriteLine($"[Orch-Phase3] Iteraciones: {loopResult.Iterations} | Tiempo bucle: {loopResult.LoopTimeMs} ms");

            if (loopResult.Converged)
                Console.WriteLine($"✅ FASE 3: Convergencia total de ajuste termodinámico.");
            else
                Console.WriteLine($"❌ FASE 3: {loopResult.PendingTasks} tareas pendientes tras {loopResult.Iterations} iteraciones.");

            return loopResult;
        }

        /// <summary>
        /// Ejecuta el bucle de convergencia genérico para una fase dada.
        /// </summary>
        private PhaseResult ExecutePhaseLoop(
            List<PipelineTask> tasks,
            string phaseName,
            int maxIterations,
            string logPrefix)
        {
            if (tasks.Count == 0)
            {
                Console.WriteLine($"[{logPrefix}] ℹ️  Sin tareas para resolver.");
                return new PhaseResult { PhaseName = phaseName, Converged = true, PendingTasks = 0 };
            }

            var swLoop = Stopwatch.StartNew();
            int iter = 0;
            long totalSolverTime = 0;
            int totalSolverCalls = 0;
            bool anyMovementInLastIter = false;

            while (tasks.Count > 0 && iter < maxIterations)
            {
                var swIterStart = Stopwatch.StartNew();
                bool anyMovementInThisIter = false;
                int tasksProcessed = 0;

                for (int i = 0; i < tasks.Count;)
                {
                    var task = tasks[i];
                    var swTask = Stopwatch.StartNew();

                    var (isConverged, numbersMoved) = ExecuteAndCheckTask(task);

                    swTask.Stop();
                    totalSolverTime += swTask.ElapsedMilliseconds;
                    totalSolverCalls++;
                    tasksProcessed++;

                    if (numbersMoved) anyMovementInThisIter = true;

                    if (isConverged && !numbersMoved)
                    {
                        tasks.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                swIterStart.Stop();
                anyMovementInLastIter = anyMovementInThisIter;

                Console.WriteLine($"{logPrefix} Iter {iter}: {tasksProcessed} tareas, {tasks.Count} restantes, {(anyMovementInThisIter ? "movimiento" : "estable")}, {swIterStart.ElapsedMilliseconds} ms");

                if (!anyMovementInThisIter && tasks.Count > 0)
                {
                    Console.WriteLine($"⚠️ {logPrefix} Bucle detenido temprano (Iter {iter}). En espera de datos de entrada.");
                    break;
                }

                iter++;
            }
            swLoop.Stop();

            return new PhaseResult
            {
                PhaseName = phaseName,
                Converged = tasks.Count == 0,
                PendingTasks = tasks.Count,
                Iterations = iter,
                LoopTimeMs = swLoop.ElapsedMilliseconds,
                SolverTimeMs = totalSolverTime,
                SolverCalls = totalSolverCalls,
                LastMovement = anyMovementInLastIter
            };
        }

        /// <summary>
        /// Imprime el reporte final consolidado de todas las fases.
        /// </summary>
        private void PrintFinalReport(PhaseResult phase1, PhaseResult phase2, PhaseResult phase3, long totalMs)
        {
            Console.WriteLine($"\n[Orch] <<< RunSimulation COMPLETADA");
            Console.WriteLine($"[Orch] Tiempo total: {totalMs} ms");
            Console.WriteLine($"[Orch] Fase 1: {phase1.ElapsedMs} ms {(phase1.Converged ? "✅" : "❌")}");
            Console.WriteLine($"[Orch] Fase 2: {phase2.LoopTimeMs} ms, {phase2.Iterations} iters {(phase2.Converged ? "✅" : "❌")}");
            Console.WriteLine($"[Orch] Fase 3: {phase3.LoopTimeMs} ms, {phase3.Iterations} iters {(phase3.Converged ? "✅" : "❌")}");

            if (phase1.Converged && phase2.Converged && phase3.Converged)
                Console.WriteLine($"✅ CONVERGENCIA TOTAL: Todas las fases estabilizadas.");
            else if (!phase2.Converged && !phase3.Converged)
                Console.WriteLine($"❌ SIN CONVERGENCIA: Tareas pendientes en Fase 2 ({phase2.PendingTasks}) y Fase 3 ({phase3.PendingTasks}).");
            else if (!phase2.Converged)
                Console.WriteLine($"⚠️ CONVERGENCIA PARCIAL: Fase 2 pendiente ({phase2.PendingTasks} tareas), Fase 3 OK.");
            else
                Console.WriteLine($"⚠️ CONVERGENCIA PARCIAL: Fase 2 OK, Fase 3 pendiente ({phase3.PendingTasks} tareas).");
        }

        /// <summary>
        /// Evalúa una tarea y retorna si convergió y si causó movimiento en los números.
        /// </summary>
        private (bool isConverged, bool numbersMoved) ExecuteAndCheckTask(PipelineTask task)
        {
            var sw = Stopwatch.StartNew();

            var activeVars = task.Subsystem.SelectMany(s => s.GetCouplingVariables()).Distinct().ToList();
            var oldValues = activeVars.Select(v => v.GetSolverValue()).ToArray();

            var swAdapter = Stopwatch.StartNew();
            var systemWrapper = new OrchestratorSystemAdapter(task.Subsystem, task.Procedence);
            swAdapter.Stop();

            if (!systemWrapper.CouplingVariables.Any())
            {
                sw.Stop();
                return (true, false);
            }

            var swSolve = Stopwatch.StartNew();
            var result = _globalSolver.Solve(systemWrapper, task.Procedence);
            swSolve.Stop();

            if (!result.Converged)
            {
                sw.Stop();
                return (false, false);
            }

            var newValues = activeVars.Select(v => v.GetSolverValue()).ToArray();
            for (int i = 0; i < oldValues.Length; i++)
            {
                if (Math.Abs(oldValues[i] - newValues[i]) > 1e-8)
                {
                    sw.Stop();
                    return (true, true);
                }
            }

            sw.Stop();
            return (true, false);
        }

        /// <summary>
        /// Construye tareas para una fase dada, usando el builder adecuado según la fase.
        /// </summary>
        private List<PipelineTask> BuildTasks(StrategyType type, VariableDataProcedence procedence)
        {
            var sw = Stopwatch.StartNew();

            var subsystems = procedence == VariableDataProcedence.Phase3_ThermoAdjustment
                ? BuildPhase3SubsystemsByRamal(type, procedence)
                : BuildSubsystemsByType(type, procedence);

            sw.Stop();

            var result = subsystems.Select(sub => new PipelineTask
            {
                Subsystem = sub,
                Procedence = procedence,
                Type = type,
                Name = $"{type}_{procedence}"
            }).ToList();

            Console.WriteLine($"[Build] {type}_{procedence}: {sw.ElapsedMilliseconds} ms, {subsystems.Count} tareas");
            return result;
        }

        /// <summary>
        /// Reset de estado: limpia variables no especificadas por UI.
        /// </summary>
        private void ResetSimulationState()
        {
            var sw = Stopwatch.StartNew();

            var allVariables = _equipments
                .SelectMany(e => e.GetStrategies())
                .SelectMany(s => s.GetCouplingVariables())
                .Distinct()
                .ToList();

            foreach (var variable in allVariables)
            {
                if (variable.DataProcedence != VariableDataProcedence.UserInput &&
                    variable.DataProcedence != VariableDataProcedence.StreamCalculated)
                {
                    variable.ResetProcedence();
                }
            }
            sw.Stop();
            Console.WriteLine($"[Orch] ResetSimulationState: {sw.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// Ejecuta propagación local (Phase 1) para cada equipo.
        /// </summary>
        private void RunPhase1LocalPropagation()
        {
            var swTotal = Stopwatch.StartNew();
            int strategiesProcessed = 0;

            foreach (var eq in _equipments)
            {
                var phase1Strategies = eq.GetStrategies()
                    .Where(s => s.Procedence == VariableDataProcedence.Phase1_LocalPropagation)
                    .ToList();

                foreach (var strategy in phase1Strategies)
                {
                    var localAdapter = new OrchestratorSystemAdapter(
                        new List<ISolverPhaseStrategy> { strategy },
                        strategy.Procedence
                    );

                    if (localAdapter.CouplingVariables.Any())
                    {
                        var localSolver = new NewtonRaphsonSolver { MaxIterations = 10 };
                        localSolver.Solve(localAdapter, strategy.Procedence);
                        strategiesProcessed++;
                    }
                }
            }
            swTotal.Stop();
            Console.WriteLine($"[Orch] Phase1LocalPropagation: {swTotal.ElapsedMilliseconds} ms, {strategiesProcessed} estrategias ejecutadas");
        }

        /// <summary>
        /// Construye subsistemas para Phase 1/2 usando BFS por variables compartidas.
        /// </summary>
        private List<List<ISolverPhaseStrategy>> BuildSubsystemsByType(StrategyType type, VariableDataProcedence phase)
        {
            var swTotal = Stopwatch.StartNew();
            var subsystems = new List<List<ISolverPhaseStrategy>>();

            var swCollect = Stopwatch.StartNew();
            var allStrategies = _equipments
                .SelectMany(e => e.GetStrategies())
                .Where(s => s.Type == type && s.Procedence == phase)
                .ToList();
            swCollect.Stop();

            var visitedStrategies = new HashSet<ISolverPhaseStrategy>();
            int bfsIterations = 0;
            int strategiesGrouped = 0;

            foreach (var startStrategy in allStrategies)
            {
                if (visitedStrategies.Contains(startStrategy)) continue;

                var currentSubsystem = new List<ISolverPhaseStrategy>();
                var queue = new Queue<ISolverPhaseStrategy>();

                visitedStrategies.Add(startStrategy);
                currentSubsystem.Add(startStrategy);
                queue.Enqueue(startStrategy);

                while (queue.Count > 0)
                {
                    bfsIterations++;
                    var currentStrategy = queue.Dequeue();
                    var currentVars = new HashSet<IProcessVariable>(currentStrategy.GetCouplingVariables());

                    foreach (var candidate in allStrategies)
                    {
                        if (visitedStrategies.Contains(candidate)) continue;

                        var candidateVars = candidate.GetCouplingVariables();

                        if (candidateVars.Any(v => currentVars.Contains(v)))
                        {
                            visitedStrategies.Add(candidate);
                            currentSubsystem.Add(candidate);
                            queue.Enqueue(candidate);
                            strategiesGrouped++;
                        }
                    }
                }

                subsystems.Add(currentSubsystem);
            }

            swTotal.Stop();
            Console.WriteLine($"[BFS] {type}_{phase}: {swTotal.ElapsedMilliseconds} ms | estrategias:{allStrategies.Count} | grupos:{subsystems.Count} | iteraciones BFS:{bfsIterations}");

            return subsystems;
        }

        /// <summary>
        /// Construye subsistemas para Phase 3 usando estrategia "por ramal".
        /// Cada estrategia es su propio subsistema (resolución independiente).
        /// </summary>
        private List<List<ISolverPhaseStrategy>> BuildPhase3SubsystemsByRamal(StrategyType type, VariableDataProcedence phase)
        {
            var swTotal = Stopwatch.StartNew();

            var allStrategies = _equipments
                .SelectMany(e => e.GetStrategies())
                .Where(s => s.Type == type && s.Procedence == phase)
                .ToList();

            var subsystems = allStrategies
                .Select(s => new List<ISolverPhaseStrategy> { s })
                .ToList();

            swTotal.Stop();
            Console.WriteLine($"[Phase3-Ramal] {type}_{phase}: {swTotal.ElapsedMilliseconds} ms | estrategias:{allStrategies.Count} | ramales:{subsystems.Count}");

            return subsystems;
        }
    }

}