using System;
using System.Collections.Generic;
using System.Text;

namespace Numerics
{
    //public static class NumericsTest
    //{
    //    public static void Run()
    //    {
    //        // Función F(x)
    //        Func<double[], double[]> F = (x) =>
    //        {
    //            double x1 = x[0];
    //            double x2 = x[1];

    //            return new double[]
    //            {
    //            x1 * x1 + x2 * x2 - 5,   // ecuación 1
    //            x1 - x2 - 1              // ecuación 2
    //            };
    //        };

    //        // Jacobiano numérico
    //        Func<double[], double[,]> J = (x) =>
    //        {
    //            return JacobianBuilder.Numerical(F, x);
    //        };

    //        // Initial guess (IMPORTANTE)
    //        double[] x0 = new double[] { 1.0, 1.0 };

    //        var solver = new NewtonSolver();

    //        var result = solver.Solve(F, J, x0);

    //        if (result.IsSuccess)
    //        {
    //            var sol = result.Value;

    //            Console.WriteLine("Convergió:");
    //            Console.WriteLine($"x = {sol[0]}");
    //            Console.WriteLine($"y = {sol[1]}");
    //        }
    //        else
    //        {
    //            Console.WriteLine("Error:");
    //            Console.WriteLine(result.Error);
    //        }
    //    }
    //}
}
