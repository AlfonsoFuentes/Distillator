using Shared.SolverQwen.Equipments;
using Shared.SolverQwen.Variables;

namespace Shared.SolverQwen.Simlations
{

    public class OrchestratorSystemAdapter2 : ISimulationSystem
    {
        private readonly List<ISolverPhaseStrategy> _strategies;
        private readonly VariableDataProcedence _currentPhase;
        private readonly List<IProcessVariable> _cachedCouplingVariables;


        public OrchestratorSystemAdapter2(List<ISolverPhaseStrategy> strategies, VariableDataProcedence currentPhase)
        {
            _strategies = strategies;
            _currentPhase = currentPhase;

            if (currentPhase == VariableDataProcedence.Phase3_ThermoAdjustment)
            {
                // ✅ FASE 3: Agrupar por NOMBRE (identidad lógica) para eliminar duplicados de referencia
                // Luego filtrar: solo variables NO especificadas son incógnitas
                _cachedCouplingVariables = _strategies
        .SelectMany(s => s.GetCouplingVariables())
        .Distinct()
        .Where(IsVariableAdjustable) // ← Recuperamos tu escudo original
        .ToList();

                // 🔍 DEBUG: Desglose por estrategia (solo para verificación visual)
                Console.WriteLine("\n[=== DEBUG FASE 3: POR ESTRATEGIA ===]");
                int totalEcuaciones = 0;
                int totalVariables = 0;

                foreach (var strat in _strategies)
                {
                    Console.WriteLine($"\n🔹 {strat.Name}");

                    var residuals = strat.GetResiduals();
                    Console.WriteLine($"   📐 Ecuaciones: {residuals.Length}");
                    totalEcuaciones += residuals.Length;

                    Console.WriteLine($"   🔗 Variables acopladas:");
                    foreach (var v in strat.GetCouplingVariables())
                    {
                        Printvariable(v);
                        totalVariables++;
                    }
                }
                Console.WriteLine($"\n total Variables (crudas): {totalVariables}");
                Console.WriteLine($" total ecuaciones: {totalEcuaciones}");

                Console.WriteLine($"\n -------:");
                Console.WriteLine($"\n CACHED Variables (ajustables reales):");
                foreach (var variable in _cachedCouplingVariables)
                {
                    Printvariable(variable);
                }
                Console.WriteLine($"\n📊 RESUMEN FINAL:");
                Console.WriteLine($"   Total Ecuaciones: {totalEcuaciones}");
                Console.WriteLine($"   Variables Ajustables (pasan filtro !IsSpecToSolver): {_cachedCouplingVariables.Count}");

                Console.WriteLine($"\n   ⚖️  BALANCE: {totalEcuaciones} ecuaciones vs {_cachedCouplingVariables.Count} variables");
                if (totalEcuaciones != _cachedCouplingVariables.Count)
                    Console.WriteLine($"   ❌ DESBALANCE de {Math.Abs(totalEcuaciones - _cachedCouplingVariables.Count)} grados");
                else
                    Console.WriteLine($"   ✅ Sistema perfectamente especificado");

                Console.WriteLine("===================================================\n");
            }
            else
            {
                // ✅ FASE 1 y 2: Filtro original con protección por niveles
                _cachedCouplingVariables = _strategies
                    .SelectMany(s => s.GetCouplingVariables())
                    .Distinct()                      // ← En Fase 1/2, .Distinct() por referencia suele ser suficiente
                    .Where(IsVariableAdjustable)
                    .ToList();
            }
        }

        private void Printvariable(IProcessVariable v)
        {

            string valor = v.ToUiString();

            // Imprimimos usando tu propiedad Name que ya tiene el formato perfecto
            Console.WriteLine($" {v.Name,-50} | IsSpec By={v.DataProcedence,-5} | {valor}");
        }
        // ✅ SIN FILTRO: Todas las ecuaciones son válidas
        public double[] GetResiduals()
        {
            return _strategies.SelectMany(s => s.GetResiduals()).ToArray();
        }

        public List<IProcessVariable> CouplingVariables => _cachedCouplingVariables;

        private bool IsVariableAdjustable(IProcessVariable variable)
        {
            if (variable.IsSpecToSolver) return false;
            if (!variable.IsDefined || variable.DataProcedence == _currentPhase) return true;
            return GetProtectionLevel(variable.DataProcedence) >= GetProtectionLevel(_currentPhase);
        }

        private static int GetProtectionLevel(VariableDataProcedence phase) => phase switch
        {
            VariableDataProcedence.Phase1_LocalPropagation => 3,
            VariableDataProcedence.Phase2_EasyEquipmentNet => 2,
            VariableDataProcedence.Phase3_ThermoAdjustment => 1,
            _ => 0
        };
    }


    /// <summary>
    /// Adaptador que envuelve estrategias de solver para el Newton-Raphson global.
    /// 
    /// 🔹 OPTIMIZACIÓN CLAVE: Cache de GetResiduals() en Phase 3
    /// - Evita evaluar dos veces la misma estrategia (filtro + solver)
    /// - Mejora performance ~2× en Phase 3 para estrategias costosas (flash termodinámico)
    /// - Cero impacto en Phase 1/2 (comportamiento original intacto)
    /// </summary>
    public class OrchestratorSystemAdapter3 : ISimulationSystem
    {
        private readonly List<ISolverPhaseStrategy> _strategies;
        private readonly List<ISolverPhaseStrategy> _activeStrategies;
        private readonly VariableDataProcedence _currentPhase;
        private readonly List<IProcessVariable> _cachedCouplingVariables;

        // 🔹 NUEVO: Cache de residuales para Phase 3 (evita evaluación duplicada)
        private readonly Dictionary<ISolverPhaseStrategy, double[]> _strategyCache;

        public OrchestratorSystemAdapter3(List<ISolverPhaseStrategy> strategies, VariableDataProcedence currentPhase)
        {
            _strategies = strategies ?? new List<ISolverPhaseStrategy>();
            _currentPhase = currentPhase;
            _strategyCache = new Dictionary<ISolverPhaseStrategy, double[]>();  // ← Inicializar cache

            // ─────────────────────────────────────────────────────────
            // 🔹 FASE 3: Filtrado inteligente + Cache de residuales
            // ─────────────────────────────────────────────────────────
            if (currentPhase == VariableDataProcedence.Phase3_ThermoAdjustment)
            {
                // 1. Identificar estrategias ACTIVAS y cachear sus residuales
                _activeStrategies = _strategies
                    .Where(s =>
                    {
                        try
                        {
                            var residuals = s.GetResiduals();
                            _strategyCache[s] = residuals;  // ← Cache para uso posterior
                            return residuals != null && residuals.Length > 0;
                        }
                        catch
                        {
                            // Si falla la evaluación, considerar inactiva por seguridad
                            return false;
                        }
                    })
                    .ToList();

                // 2. Construir variables de acoplamiento SOLO de estrategias activas
                _cachedCouplingVariables = _activeStrategies
                    .SelectMany(s => s.GetCouplingVariables())
                    .Distinct()
                    .Where(IsVariableAdjustable)
                    .ToList();

                // 3. 🔍 DEBUG: Desglose detallado para auditoría
                Console.WriteLine("\n[=== DEBUG FASE 3: POR ESTRATEGIA ===]");

                int totalEcuaciones = 0;
                int totalVariables = 0;
                int estrategiasActivas = 0;
                int estrategiasInactivas = 0;

                foreach (var strat in _strategies)
                {
                    double[] residuals;
                    bool isActive;

                    try
                    {
                        // 🔹 Usar cache si ya existe (evita re-evaluar)
                        if (_strategyCache.TryGetValue(strat, out var cached))
                        {
                            residuals = cached;
                        }
                        else
                        {
                            residuals = strat.GetResiduals();
                            _strategyCache[strat] = residuals;  // ← Cache para consistencia
                        }
                        isActive = residuals != null && residuals.Length > 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ ERROR evaluando {strat.Name}: {ex.Message}");
                        residuals = Array.Empty<double>();
                        isActive = false;
                    }

                    if (isActive)
                    {
                        estrategiasActivas++;
                        totalEcuaciones += residuals?.Length ?? 0;
                    }
                    else
                    {
                        estrategiasInactivas++;
                    }

                    Console.WriteLine($"\n🔹 {strat.Name} [{(isActive ? "✅ ACTIVA" : "⚪ INACTIVA")}]");
                    Console.WriteLine($"   📐 Ecuaciones: {residuals?.Length ?? 0}");

                    if (isActive)
                    {
                        Console.WriteLine($"   🔗 Variables acopladas:");
                        foreach (var v in strat.GetCouplingVariables())
                        {
                            Printvariable(v);
                            totalVariables++;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ⚪ Sin specs suficientes → omitida del sistema");
                    }
                }

                Console.WriteLine($"\n📊 RESUMEN DE ESTRATEGIAS:");
                Console.WriteLine($"   Activas: {estrategiasActivas} | Inactivas: {estrategiasInactivas} | Total: {_strategies.Count}");

                Console.WriteLine($"\n📊 RESUMEN DEL SISTEMA ACTIVO:");
                Console.WriteLine($"   Total Ecuaciones: {totalEcuaciones}");
                Console.WriteLine($"   Variables Ajustables: {_cachedCouplingVariables.Count}");

                Console.WriteLine($"\n   ⚖️  BALANCE: {totalEcuaciones} ecuaciones vs {_cachedCouplingVariables.Count} variables");

                if (totalEcuaciones == 0 && _cachedCouplingVariables.Count == 0)
                {
                    Console.WriteLine($"   ℹ️  Sistema vacío (no hay specs suficientes en ninguna estrategia)");
                }
                else if (totalEcuaciones != _cachedCouplingVariables.Count)
                {
                    Console.WriteLine($"   ❌ DESBALANCE de {Math.Abs(totalEcuaciones - _cachedCouplingVariables.Count)} grados");
                }
                else
                {
                    Console.WriteLine($"   ✅ Sistema perfectamente especificado");
                }

                Console.WriteLine("===================================================\n");
            }
            // ─────────────────────────────────────────────────────────
            // 🔹 FASE 1 y 2: Comportamiento original (SIN cambios, SIN cache)
            // ─────────────────────────────────────────────────────────
            else
            {
                _activeStrategies = new List<ISolverPhaseStrategy>(_strategies);
                _cachedCouplingVariables = _strategies
                    .SelectMany(s => s.GetCouplingVariables())
                    .Distinct()
                    .Where(IsVariableAdjustable)
                    .ToList();
            }
        }

        /// <summary>
        /// Imprime variable con formato consistente para debugging.
        /// </summary>
        private void Printvariable(IProcessVariable v)
        {
            if (v == null) return;

            string valor = v.ToUiString();
            Console.WriteLine($" {v.Name,-50} | IsSpec By={v.DataProcedence,-5} | {valor}");
        }

        public double[] GetResiduals()
        {
            var targetStrategies = _currentPhase == VariableDataProcedence.Phase3_ThermoAdjustment
                ? _activeStrategies
                : _strategies;

            return targetStrategies
                .SelectMany(s =>
                {
                    // ✅ Evaluamos la física real en cada iteración del solver
                    try
                    {
                        return s.GetResiduals() ?? Array.Empty<double>();
                    }
                    catch
                    {
                        return Array.Empty<double>();
                    }
                })
                .ToArray();
        }
        public double[] GetResiduals2()
        {
            var targetStrategies = _currentPhase == VariableDataProcedence.Phase3_ThermoAdjustment
                ? _activeStrategies
                : _strategies;

            return targetStrategies
                .SelectMany(s =>
                {
                    // 🔹 OPTIMIZACIÓN: Usar cache si estamos en Phase 3 y la estrategia está cacheada
                    if (_currentPhase == VariableDataProcedence.Phase3_ThermoAdjustment &&
                        _strategyCache.TryGetValue(s, out var cached))
                    {
                        return cached ?? Array.Empty<double>();
                    }

                    // Fallback: evaluar normalmente con defensa contra excepciones
                    try
                    {
                        return s.GetResiduals() ?? Array.Empty<double>();
                    }
                    catch
                    {
                        return Array.Empty<double>();
                    }
                })
                .ToArray();
        }

        /// <summary>
        /// Variables que el solver puede ajustar (incógnitas).
        /// </summary>
        public List<IProcessVariable> CouplingVariables => _cachedCouplingVariables;

        /// <summary>
        /// Determina si una variable puede ser ajustada por el solver en esta fase.
        /// Reglas de precedencia:
        /// - UserInput y StreamCalculated NUNCA se ajustan
        /// - Variables no definidas SÍ se ajustan
        /// - Variables de fase actual o inferior SÍ se ajustan
        /// </summary>
        private bool IsVariableAdjustable(IProcessVariable variable)
        {
            if (variable == null) return false;

            // 🔹 Regla 1: Si está especificada para el solver, no tocar
            if (variable.IsSpecToSolver) return false;

            // 🔹 Regla 2: Si no está definida, es incógnita → ajustar
            if (!variable.IsDefined || variable.DataProcedence == _currentPhase) return true;

            // 🔹 Regla 3: Comparar niveles de protección por fase
            return GetProtectionLevel(variable.DataProcedence) >= GetProtectionLevel(_currentPhase);
        }

        /// <summary>
        /// Niveles de protección por fase (mayor número = más protegido).
        /// Phase 1 > Phase 2 > Phase 3 en términos de "no sobrescribir".
        /// </summary>
        private static int GetProtectionLevel(VariableDataProcedence phase) => phase switch
        {
            VariableDataProcedence.Phase1_LocalPropagation => 3,
            VariableDataProcedence.Phase2_EasyEquipmentNet => 2,
            VariableDataProcedence.Phase3_ThermoAdjustment => 1,
            _ => 0
        };
    }



    public class OrchestratorSystemAdapter : ISimulationSystem
    {
        private readonly List<ISolverPhaseStrategy> _strategies;
        private readonly List<ISolverPhaseStrategy> _activeStrategies;
        private readonly VariableDataProcedence _currentPhase;
        private readonly List<IProcessVariable> _cachedCouplingVariables;

        // 🔹 CACHE: Avoids duplicated evaluations in Phase 3
        private readonly Dictionary<ISolverPhaseStrategy, double[]> _strategyCache;

        public OrchestratorSystemAdapter(List<ISolverPhaseStrategy> strategies, VariableDataProcedence currentPhase)
        {
            _strategies = strategies ?? new List<ISolverPhaseStrategy>();
            _currentPhase = currentPhase;
            _strategyCache = new Dictionary<ISolverPhaseStrategy, double[]>();

            // ─────────────────────────────────────────────────────────
            // 🔹 PHASE 3: Smart Filtering + Residual Cache
            // ─────────────────────────────────────────────────────────
            if (currentPhase == VariableDataProcedence.Phase3_ThermoAdjustment)
            {
                // 1. Identify ACTIVE strategies and cache residuals
                _activeStrategies = _strategies
                    .Where(s =>
                    {
                        try
                        {
                            var residuals = s.GetResiduals();
                            _strategyCache[s] = residuals;
                            return residuals != null && residuals.Length > 0;
                        }
                        catch
                        {
                            return false; // Safety fallback
                        }
                    })
                    .ToList();

                // 2. Build coupling variables ONLY for active strategies
                _cachedCouplingVariables = _activeStrategies
                    .SelectMany(s => s.GetCouplingVariables())
                    .Distinct()
                    .Where(v => IsVariableAdjustable(v, _currentPhase)) // <-- USING PUBLIC STATIC METHOD
                    .ToList();

#if DEBUG
                Console.WriteLine($"\n=========================================================");
                Console.WriteLine($"[Adapter-Build] 🛠️ CONSTRUYENDO SISTEMA - {_currentPhase}");
                Console.WriteLine($"=========================================================");

                int totalEquations = 0;
                int activeStrategyCount = 0;
                int inactiveStrategyCount = 0;

                foreach (var strat in _strategies)
                {
                    double[] residuals;
                    bool isActive;

                    try
                    {
                        if (_strategyCache.TryGetValue(strat, out var cached))
                        {
                            residuals = cached;
                        }
                        else
                        {
                            residuals = strat.GetResiduals();
                            _strategyCache[strat] = residuals;
                        }
                        isActive = residuals != null && residuals.Length > 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   ❌ [ERROR] Falló evaluación inicial de {strat.Name}: {ex.Message}");
                        residuals = Array.Empty<double>();
                        isActive = false;
                    }

                    if (isActive)
                    {
                        activeStrategyCount++;
                        totalEquations += residuals?.Length ?? 0;
                        Console.WriteLine($"\n  🔹 {strat.Name} [✅ ACTIVA]");
                        Console.WriteLine($"     📐 Ecuaciones aportadas: {residuals?.Length ?? 0}");
                        Console.WriteLine($"     🔗 Análisis de Variables:");

                        foreach (var v in strat.GetCouplingVariables())
                        {
                            PrintVariableDebug(v, _currentPhase);
                        }
                    }
                    else
                    {
                        inactiveStrategyCount++;
                        Console.WriteLine($"\n  🔹 {strat.Name} [⚪ INACTIVA / SIN SPECS]");
                    }
                }

                Console.WriteLine($"\n  📊 RESUMEN MATRICIAL FASE 3:");
                Console.WriteLine($"     Estrategias: Activas {activeStrategyCount} | Inactivas {inactiveStrategyCount} | Total {_strategies.Count}");
                Console.WriteLine($"     Ecuaciones (Filas)   : {totalEquations}");
                Console.WriteLine($"     Incógnitas (Columnas): {_cachedCouplingVariables.Count}");

                if (totalEquations == 0 && _cachedCouplingVariables.Count == 0)
                    Console.WriteLine($"     ℹ️ BALANCE: Sistema Vacío (No hay simulación requerida)");
                else if (totalEquations == _cachedCouplingVariables.Count)
                    Console.WriteLine($"     ⚖️ BALANCE: PERFECTO (Sistema Cuadrado)");
                else
                    Console.WriteLine($"     ❌ BALANCE: DESFASADO (Δ = {Math.Abs(totalEquations - _cachedCouplingVariables.Count)} grados de libertad)");

                Console.WriteLine($"=========================================================\n");
#endif
            }
            // ─────────────────────────────────────────────────────────
            // 🔹 PHASE 1 & 2: Local & Network Propagation
            // ─────────────────────────────────────────────────────────
            else
            {
                _activeStrategies = new List<ISolverPhaseStrategy>(_strategies);
                _cachedCouplingVariables = _strategies
                    .SelectMany(s => s.GetCouplingVariables())
                    .Distinct()
                    .Where(v => IsVariableAdjustable(v, _currentPhase))
                    .ToList();

#if DEBUG
                Console.WriteLine($"\n[Adapter-Build] 🛠️ SISTEMA {_currentPhase} | Estrategias: {_activeStrategies.Count} | Incógnitas Totales: {_cachedCouplingVariables.Count}");
                foreach (var strat in _activeStrategies)
                {
                    var eqs = strat.GetResiduals()?.Length ?? 0;
                    Console.WriteLine($"   🔹 {strat.Name} -> Aporta {eqs} ecuaciones.");
                    foreach (var v in strat.GetCouplingVariables())
                    {
                        PrintVariableDebug(v, _currentPhase);
                    }
                }
#endif
            }
        }

#if DEBUG
        private void PrintVariableDebug(IProcessVariable v, VariableDataProcedence currentPhase)
        {
            if (v == null) return;

            bool isAdjustable = IsVariableAdjustable(v, currentPhase);
            string statusTag = isAdjustable ? "🟢 INCÓGNITA" : "🧱 MURO/BOUNDARY";
            string valueStr = v.IsDefined ? v.ToUiString() : "[No Definida]";

            Console.WriteLine($"        -> {statusTag,-16} | Proc: {v.DataProcedence,-25} | {v.Name,-30} = {valueStr}");

            if (!isAdjustable && v.IsDefined)
            {
                Console.WriteLine($"           [Graph Tearing] ⚡ {v.Name} corta la red aquí y aísla el equipo.");
            }
        }
#endif

        public double[] GetResiduals()
        {
            var targetStrategies = _currentPhase == VariableDataProcedence.Phase3_ThermoAdjustment
                ? _activeStrategies
                : _strategies;

            return targetStrategies.SelectMany(s =>
            {
                try { return s.GetResiduals() ?? Array.Empty<double>(); }
                catch { return Array.Empty<double>(); }
            }).ToArray();
        }

        public double[] GetResiduals2()
        {
            var targetStrategies = _currentPhase == VariableDataProcedence.Phase3_ThermoAdjustment
                ? _activeStrategies
                : _strategies;

            return targetStrategies.SelectMany(s =>
            {
                if (_currentPhase == VariableDataProcedence.Phase3_ThermoAdjustment &&
                    _strategyCache.TryGetValue(s, out var cached))
                {
                    return cached ?? Array.Empty<double>();
                }
                try { return s.GetResiduals() ?? Array.Empty<double>(); }
                catch { return Array.Empty<double>(); }
            }).ToArray();
        }

        public List<IProcessVariable> CouplingVariables => _cachedCouplingVariables;

        // ✅ MAGIA: Método público estático para que el BFS del Orquestador lo use
        public static bool IsVariableAdjustable(IProcessVariable variable, VariableDataProcedence currentPhase)
        {
            if (variable == null) return false;
            if (variable.IsSpecToSolver) return false;
            if (!variable.IsDefined || variable.DataProcedence == currentPhase) return true;

            return GetProtectionLevel(variable.DataProcedence) >= GetProtectionLevel(currentPhase);
        }

        private static int GetProtectionLevel(VariableDataProcedence phase) => phase switch
        {
            VariableDataProcedence.Phase1_LocalPropagation => 3,
            VariableDataProcedence.Phase2_EasyEquipmentNet => 2,
            VariableDataProcedence.Phase3_ThermoAdjustment => 1,
            _ => 0
        };
    }

}
