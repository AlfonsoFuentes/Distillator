using Shared.Thermodynamics.ControlledVariables;

namespace Shared.MatrixSolvers
{
   
    public class NewtonSolver
    {
        public double Tolerance { get; set; } = 1e-6;
        public int MaxIterations { get; set; } = 50;

        // 🔥 Validación interna del sistema (DOF)
        private MatrixResult<bool> ValidateSystem(EquationSystem eqs)
        {
            int nVars = eqs.Variables.Count;
            int nEqs = eqs.Equations.Count;

            if (nVars > nEqs)
                return MatrixResult<bool>.Fail(
                    $"Sistema subespecificado (DOF = {nVars - nEqs})");

            if (nVars < nEqs)
                return MatrixResult<bool>.Fail(
                    $"Sistema sobreespecificado (DOF = {nVars - nEqs})");

            return MatrixResult<bool>.Ok(true);
        }
        private double[,] NumericalJacobian(double[] x, Func<double[], double[]> F)
        {
            int n = x.Length;
            double h = 1e-6;

            var J = new double[n, n];
            var f0 = F(x);

            for (int j = 0; j < n; j++)
            {
                var xh = (double[])x.Clone();
                xh[j] += h;

                var fh = F(xh);

                for (int i = 0; i < n; i++)
                {
                    J[i, j] = (fh[i] - f0[i]) / h;
                }
            }

            return J;
        }


        public MatrixResult<double[]> SolveEquipment(EquationSystem eqs)
        {
            double[] x0 = new double[eqs.Variables.Count];
            // En NewtonSolver.Solve()



            for (int i = 0; i < eqs.Variables.Count; i++)
            {
                var variable = eqs.Variables[i];

                // 🔥 1. PRIORIDAD MÁXIMA: Si está especificado por el usuario, se respeta al 100%
                if (variable.IsDefined)
                {
                    x0[i] = variable.SolverValue;
                }
                else
                {
                    // 🔥 2. PRIORIDAD SECUNDARIA: Si está libre, usamos el InitValue que definiste en el constructor
                    // (Ej: Temp=298, Presión=101325, Fracción=0.5, etc.)
                    x0[i] = variable.InitValue;
                }
            }

            var result = SolveEquipment(
               eqs,
               eqs.Evaluate,
               x => NumericalJacobian(x, eqs.Evaluate),
               x0
           );
            return result;
        }
        private MatrixResult<double[]> SolveEquipment(EquationSystem eqs, Func<double[], double[]> F, Func<double[], double[,]> J, double[] x0, Action<double[]>? onIteration = null)
        {
            // 🔥 1. Validar DOF ANTES de empezar
            var dofCheck = ValidateSystem(eqs);
            if (!dofCheck.IsSuccess)
                return MatrixResult<double[]>.Fail(dofCheck.Error);

            var x = (double[])x0.Clone();

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // Sincronizar variables con valores actuales del solver
               

                var fx = F(x);
                var jx = J(x);

                var linearResult = LinearSolver.TrySolve(jx, VectorOps.Negate(fx));
                if (!linearResult.IsSuccess)
                    return MatrixResult<double[]>.Fail(linearResult.Error);

                var dx = linearResult.Value;

                // ====================================================================
                // >>> START ADAPTIVE DAMPING (Borrar este bloque si no funciona)
                // ====================================================================
                // Propósito: Evitar pasos de Newton demasiado grandes en ecuaciones no-lineales
                // Estrategia: Line search simple - reducir damping hasta que ||f|| disminuya

                double damping = 1.0;           // Paso completo de Newton por defecto
                const double minDamping = 1e-4; // Límite inferior para evitar estancamiento
                const double improvementFactor = 0.99; // Mejora mínima requerida (1%)

                double fxNorm = VectorOps.Norm(fx);
                double[] xNew = x; // Fallback: usar paso completo si line search falla

                // Intentar reducir el paso hasta encontrar una mejora en el residual
                for (int ls = 0; ls < 10; ls++)
                {
                    // Probar: x_trial = x + damping * dx
                    var scaledDx = VectorOps.Scale(dx, damping);
                    var addResult = VectorOps.Add(x, scaledDx);

                    if (!addResult.IsSuccess)
                    {
                        damping *= 0.5;
                        continue;
                    }

                    var xTrial = addResult.Value;
                    var fTrial = F(xTrial);
                    double fTrialNorm = VectorOps.Norm(fTrial);

                    // ¿El nuevo punto reduce el residual al menos un 1%?
                    if (fTrialNorm < fxNorm * improvementFactor)
                    {
                        xNew = xTrial; // ✅ Aceptar este punto
                        break;
                    }

                    // ❌ No mejoró: reducir paso y reintentar
                    damping *= 0.5;
                }

                // Si el damping es demasiado pequeño, posiblemente estamos en un mínimo local
                if (damping < minDamping)
                {
                    // Opción A: Fallar explícitamente (más seguro)
                    // return MatrixResult<double[]>.Fail("Line search failed: posible singularidad");

                    // Opción B: Fallback al paso completo de Newton (más permisivo) ← ELEGIMOS ESTA
                    xNew = VectorOps.Add(x, dx).Value;
                }

                x = xNew; // Actualizar con el punto aceptado
                          // <<< END ADAPTIVE DAMPING (Fin del bloque reversible)
                          // ====================================================================

                // 🔥 Si NO usás damping, descomentá esta línea y borrá el bloque de arriba:
                // var addResult = VectorOps.Add(x, dx);
                // if (!addResult.IsSuccess) return MatrixResult<double[]>.Fail(addResult.Error);
                // x = addResult.Value;

                // 🔥 Callback seguro (copia)
                onIteration?.Invoke((double[])x.Clone());

                var normDx = VectorOps.Norm(dx);
                var normFx = VectorOps.Norm(fx);
                for (int i = 0; i < eqs.Variables.Count; i++)
                {
                    if (eqs.Variables[i].IsToDefineByEquipmentSolver)
                        eqs.Variables[i].SetValueFromEquipmentSolver(x[i]);
                }
                if (normDx < Tolerance && normFx < Tolerance)
                {
                   
                    return MatrixResult<double[]>.Ok(x);
                }
            }

            return MatrixResult<double[]>.Fail("Newton no convergió");
        }

        public MatrixResult<double[]> SolveGeneral(EquationSystem eqs)
        {
            double[] x0 = new double[eqs.Variables.Count];
            // En NewtonSolver.Solve()



            for (int i = 0; i < eqs.Variables.Count; i++)
            {
                var variable = eqs.Variables[i];

                // 🔥 1. PRIORIDAD MÁXIMA: Si está especificado por el usuario, se respeta al 100%
                if (variable.IsDefined)
                {
                    x0[i] = variable.SolverValue;
                }
                else
                {
                    // 🔥 2. PRIORIDAD SECUNDARIA: Si está libre, usamos el InitValue que definiste en el constructor
                    // (Ej: Temp=298, Presión=101325, Fracción=0.5, etc.)
                    x0[i] = variable.InitValue;
                }
            }

            var result = SolveGeneral(
               eqs,
               eqs.Evaluate,
               x => NumericalJacobian(x, eqs.Evaluate),
               x0
           );
            return result;
        }
        private MatrixResult<double[]> SolveGeneral(EquationSystem eqs, Func<double[], double[]> F, Func<double[], double[,]> J, double[] x0, Action<double[]>? onIteration = null)
        {
            // 🔥 1. Validar DOF ANTES de empezar
            var dofCheck = ValidateSystem(eqs);
            if (!dofCheck.IsSuccess)
                return MatrixResult<double[]>.Fail(dofCheck.Error);

            var x = (double[])x0.Clone();

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // Sincronizar variables con valores actuales del solver


                var fx = F(x);
                var jx = J(x);

                var linearResult = LinearSolver.TrySolve(jx, VectorOps.Negate(fx));
                if (!linearResult.IsSuccess)
                    return MatrixResult<double[]>.Fail(linearResult.Error);

                var dx = linearResult.Value;

                // ====================================================================
                // >>> START ADAPTIVE DAMPING (Borrar este bloque si no funciona)
                // ====================================================================
                // Propósito: Evitar pasos de Newton demasiado grandes en ecuaciones no-lineales
                // Estrategia: Line search simple - reducir damping hasta que ||f|| disminuya

                double damping = 1.0;           // Paso completo de Newton por defecto
                const double minDamping = 1e-4; // Límite inferior para evitar estancamiento
                const double improvementFactor = 0.99; // Mejora mínima requerida (1%)

                double fxNorm = VectorOps.Norm(fx);
                double[] xNew = x; // Fallback: usar paso completo si line search falla

                // Intentar reducir el paso hasta encontrar una mejora en el residual
                for (int ls = 0; ls < 10; ls++)
                {
                    // Probar: x_trial = x + damping * dx
                    var scaledDx = VectorOps.Scale(dx, damping);
                    var addResult = VectorOps.Add(x, scaledDx);

                    if (!addResult.IsSuccess)
                    {
                        damping *= 0.5;
                        continue;
                    }

                    var xTrial = addResult.Value;
                    var fTrial = F(xTrial);
                    double fTrialNorm = VectorOps.Norm(fTrial);

                    // ¿El nuevo punto reduce el residual al menos un 1%?
                    if (fTrialNorm < fxNorm * improvementFactor)
                    {
                        xNew = xTrial; // ✅ Aceptar este punto
                        break;
                    }

                    // ❌ No mejoró: reducir paso y reintentar
                    damping *= 0.5;
                }

                // Si el damping es demasiado pequeño, posiblemente estamos en un mínimo local
                if (damping < minDamping)
                {
                    // Opción A: Fallar explícitamente (más seguro)
                    // return MatrixResult<double[]>.Fail("Line search failed: posible singularidad");

                    // Opción B: Fallback al paso completo de Newton (más permisivo) ← ELEGIMOS ESTA
                    xNew = VectorOps.Add(x, dx).Value;
                }

                x = xNew; // Actualizar con el punto aceptado
                          // <<< END ADAPTIVE DAMPING (Fin del bloque reversible)
                          // ====================================================================

                // 🔥 Si NO usás damping, descomentá esta línea y borrá el bloque de arriba:
                // var addResult = VectorOps.Add(x, dx);
                // if (!addResult.IsSuccess) return MatrixResult<double[]>.Fail(addResult.Error);
                // x = addResult.Value;

                // 🔥 Callback seguro (copia)
                onIteration?.Invoke((double[])x.Clone());

                var normDx = VectorOps.Norm(dx);
                var normFx = VectorOps.Norm(fx);
                for (int i = 0; i < eqs.Variables.Count; i++)
                {
                    if (eqs.Variables[i].IsToDefineByGeneralSolver)
                        eqs.Variables[i].SetValueFromGeneralSolver(x[i]);
                }
                if (normDx < Tolerance && normFx < Tolerance)
                {

                    return MatrixResult<double[]>.Ok(x);
                }
            }

            return MatrixResult<double[]>.Fail("Newton no convergió");
        }

    }
}
