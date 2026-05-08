namespace Shared.MatrixSolvers
{
    //public static class PumpTest
    //{
    //    public static void Run()
    //    {
    //        // 🧱 Sistema global de ecuaciones
    //        var eqs = new EquationSystem();

    //        // 🔗 Streams
    //        var feed = new StreamMaterial(eqs, "Feed");
    //        var product = new StreamMaterial(eqs, "Product");

    //        // ⚙️ Equipo
    //        var pump = new Pump("Pump1", eqs)
    //        {
    //            Inlet = feed,
    //            Outlet = product
    //        };

    //        // 🧠 Ecuaciones del equipo
    //        pump.BuildEquations(eqs);

    //        // 🔧 Funciones del solver
    //        Func<double[], double[]> F = x => eqs.Evaluate(x);
    //        Func<double[], double[,]> J = x => JacobianBuilder.Numerical(F, x);

    //        var solver = new NewtonSolver();

    //        // 🎯 Initial guess
    //        double[] x0 = new double[eqs.Variables.Count];

    //        foreach (var v in eqs.Variables)
    //            x0[v.Index] = 1.0;

    //        // 💡 Mejora del guess
    //        x0[feed.MassFlow.Index] = 10;
    //        x0[feed.Pressure.Index] = 100;
    //        x0[product.MassFlow.Index] = 10;
    //        x0[product.Pressure.Index] = 150;

    //        // =========================================================
    //        // 🧪 CASO 1: Pin + ΔP → calcular Pout
    //        // =========================================================

    //        Console.WriteLine("\n=== CASO 1: Pin + ΔP → calcular Pout ===");

    //        eqs.RemoveSpecifications();

    //        eqs.FixVariable(feed.Pressure, 100);
    //        eqs.FixVariable(feed.MassFlow, 10);
    //        eqs.FixVariable(pump.DeltaP, 50);

    //        // 🔥 Solver ahora recibe eqs (DOF check interno)
    //        var result1 = solver.Solve(eqs, F, J, x0, xIter =>
    //        {
    //            eqs.UpdateVariables(xIter);

    //            Console.WriteLine($"Iterando... P_out ≈ {product.Pressure.UnitLessValue}");

    //            // 🔮 futuro:
    //            // Thermo.Calculate(product);
    //        });

    //        if (!result1.IsSuccess)
    //        {
    //            Console.WriteLine("❌ " + result1.Error);
    //            return;
    //        }

    //        // 🔥 aplicar solución final (opcional pero recomendado)
    //        eqs.UpdateVariables(result1.Value);

    //        eqs.ValidateSpecifications();

    //        Console.WriteLine("\n--- RESULTADOS CASO 1 ---");
    //        foreach (var v in eqs.Variables)
    //            Console.WriteLine($"{v.FullName} = {v.DeepValue}");

    //        // =========================================================
    //        // 🧪 CASO 2: Pin + Pout → calcular ΔP
    //        // =========================================================

    //        Console.WriteLine("\n=== CASO 2: Pin + Pout → calcular ΔP ===");

    //        // 🔥 usar solución anterior como guess
    //        x0 = eqs.Variables.Select(v => v.DeepValue).ToArray();

    //        eqs.RemoveSpecifications();

    //        eqs.FixVariable(feed.Pressure, 100);
    //        eqs.FixVariable(feed.MassFlow, 10);
    //        eqs.FixVariable(product.Pressure, 150);

    //        var result2 = solver.Solve(eqs, F, J, x0, xIter =>
    //        {
    //            eqs.UpdateVariables(xIter);

    //            Console.WriteLine($"Iterando... ΔP ≈ {pump.DeltaP.UnitLessValue}");
    //        });

    //        if (!result2.IsSuccess)
    //        {
    //            Console.WriteLine("❌ " + result2.Error);
    //            return;
    //        }

    //        eqs.UpdateVariables(result2.Value);

    //        eqs.ValidateSpecifications();

    //        Console.WriteLine("\n--- RESULTADOS CASO 2 ---");
    //        foreach (var v in eqs.Variables)
    //            Console.WriteLine($"{v.FullName} = {v.DeepValue}");

    //        // =========================================================
    //        // 📊 Diagnóstico final
    //        // =========================================================

    //        Console.WriteLine($"\nVars: {eqs.Variables.Count}, Eqs: {eqs.Equations.Count}");
    //    }
    //}
    //public static class SeriesPumpValveTest
    //{
    //    public static void Run()
    //    {
    //        // =========================================================
    //        // 🧱 1. Crear sistema global de ecuaciones
    //        // =========================================================
    //        var eqs = new EquationSystem();

    //        // =========================================================
    //        // 🔗 2. Crear corrientes
    //        // =========================================================
    //        var c1 = new StreamMaterial(eqs, "C1");
    //        var c2 = new StreamMaterial(eqs, "C2");
    //        var c3 = new StreamMaterial(eqs, "C3");

    //        // =========================================================
    //        // ⚙️ 3. Crear equipos
    //        // =========================================================
    //        var pump = new Pump("P1", eqs)
    //        {
    //            Inlet = c1,
    //            Outlet = c2
    //        };

    //        var valve = new ControlValve("V1", eqs)
    //        {
    //            Inlet = c2,
    //            Outlet = c3
    //        };

    //        // =========================================================
    //        // 🧠 4. Construir ecuaciones del modelo
    //        // =========================================================
    //        pump.BuildEquations(eqs);
    //        valve.BuildEquations(eqs);

    //        // =========================================================
    //        // 🔧 5. Definir funciones para el solver
    //        // =========================================================
    //        Func<double[], double[]> F = x => eqs.Evaluate(x);
    //        Func<double[], double[,]> J = x => JacobianBuilder.Numerical(F, x);

    //        var solver = new NewtonSolver();

    //        // =========================================================
    //        // 🎯 6. Initial guess (IMPORTANTE)
    //        // =========================================================
    //        double[] x0 = new double[eqs.Variables.Count];

    //        foreach (var v in eqs.Variables)
    //            x0[v.Index] = 1.0;

    //        // 💡 Buen guess acelera convergencia
    //        x0[c1.Pressure.Index] = 1;
    //        x0[c2.Pressure.Index] = 7;
    //        x0[c3.Pressure.Index] = 5;

    //        x0[c1.MassFlow.Index] = 10000;
    //        x0[c2.MassFlow.Index] = 10000;
    //        x0[c3.MassFlow.Index] = 10000;

    //        // =========================================================
    //        // 🧪 CASO 1: Usuario define Pin + ΔP + F3
    //        // =========================================================

    //        Console.WriteLine("\n=== CASO 1: Pin + ΔP + F3 ===");

    //        eqs.RemoveSpecifications();

    //        eqs.FixVariable(c1.Pressure, 1);     // entrada
    //        eqs.FixVariable(pump.DeltaP, 6);     // bomba
    //        eqs.FixVariable(valve.DeltaP, 2);    // válvula
    //        eqs.FixVariable(c3.MassFlow, 10000); // flujo salida

    //        var result1 = solver.Solve(eqs, F, J, x0, xIter =>
    //        {
    //            eqs.UpdateVariables(xIter);

    //            Console.WriteLine(
    //                $"Iterando... P3 ≈ {c3.Pressure.UnitLessValue}, F ≈ {c3.MassFlow.UnitLessValue}");
    //        });

    //        if (!result1.IsSuccess)
    //        {
    //            Console.WriteLine("❌ " + result1.Error);
    //            return;
    //        }

     
    //        eqs.ValidateSpecifications();

    //        Console.WriteLine("\n--- RESULTADOS CASO 1 ---");
    //        foreach (var v in eqs.Variables)
    //            Console.WriteLine($"{v.FullName} = {v.DeepValue}");

    //        // =========================================================
    //        // 🔁 CASO 2: Simular cambio de UI
    //        // =========================================================
    //        // Usuario ahora cambia:
    //        // - Ya NO define C1.P
    //        // - Define C3.P
    //        // - Cambia flujo a C2.F

    //        Console.WriteLine("\n=== CASO 2: usuario redefine datos (UI) ===");

    //        // 🔥 usar solución anterior como punto inicial
    //        x0 = eqs.Variables.Select(v => v.DeepValue).ToArray();

    //        // 🔥 limpiar specs anteriores
    //        eqs.RemoveSpecifications();

    //        // 🔥 nuevas especificaciones
    //        eqs.FixVariable(c3.Pressure, 5);     // ahora define salida
    //        eqs.FixVariable(pump.DeltaP, 6);
    //        eqs.FixVariable(valve.DeltaP, 2);
    //        eqs.FixVariable(c2.MassFlow, 5000);  // flujo intermedio

    //        var result2 = solver.Solve(eqs, F, J, x0, xIter =>
    //        {
    //            eqs.UpdateVariables(xIter);

    //            Console.WriteLine(
    //                $"Iterando... P1 ≈ {c1.Pressure.UnitLessValue}, P2 ≈ {c2.Pressure.UnitLessValue}, P3 ≈ {c3.Pressure.UnitLessValue}");
    //        });

    //        if (!result2.IsSuccess)
    //        {
    //            Console.WriteLine("❌ " + result2.Error);
    //            return;
    //        }

      
    //        eqs.ValidateSpecifications();

    //        Console.WriteLine("\n--- RESULTADOS CASO 2 ---");
    //        foreach (var v in eqs.Variables)
    //            Console.WriteLine($"{v.FullName} = {v.DeepValue}");

    //        // =========================================================
    //        // 📊 Diagnóstico final
    //        // =========================================================
    //        Console.WriteLine($"\nVars: {eqs.Variables.Count}, Eqs: {eqs.Equations.Count}");
    //    }
    //}
}
