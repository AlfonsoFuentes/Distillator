# MainSolver Remanufactured V1

## 1. Proposito

Construir un solver nuevo, paralelo al solver actual, que conserve las ecuaciones
fisicas y los comportamientos que ya funcionan, pero reemplace la orquestacion y
el resolvedor numerico por una implementacion sencilla, determinista, robusta y
facil de depurar.

El principio central es:

> Cada componente intenta una tarea concreta, responde "pude" o "no pude" y
> nunca deja resultados parciales cuando falla.

La primera version se implementara sin logs ni trazas. Sin embargo, la
arquitectura quedara preparada para conectar posteriormente un observador sin
mezclar diagnostico con calculo.

## 2. Principios de diseno

- Aplicar KISS, DRY, YAGNI y responsabilidad unica.
- Mantener metodos cortos, nombres explicitos y flujo legible de arriba hacia
  abajo.
- No agregar condiciones particulares para tags como `C-101`, `C-127` o
  `C-140`.
- No depender del orden de creacion de equipos, corrientes o diagramas.
- No reutilizar resultados calculados anteriores como semillas ocultas.
- No publicar valores provisionales generados durante una iteracion numerica.
- Retirar del listado pendiente solo ecuaciones realmente resueltas o
  satisfechas.
- Mantener el solver actual disponible hasta verificar el nuevo.
- Reutilizar las ecuaciones fisicas existentes siempre que su contrato sea
  compatible.

## 3. Alcance

La refactorizacion se divide en dos tareas principales:

1. Implementar `NewtonSolverRemanufactured`.
2. Implementar `MainSolverRemanufactured`.

La implementacion sera paralela. No se reemplazara ni eliminara el solver actual
durante esta primera aproximacion.

---

# Tarea 1: NewtonSolverRemanufactured

## 4. Responsabilidad

`NewtonSolverRemanufactured` resuelve exclusivamente un sistema numerico que ya
fue preparado y validado conceptualmente por el llamador.

Newton no debe:

- Seleccionar ecuaciones.
- Seleccionar incognitas.
- Construir clusters.
- Limpiar variables de la simulacion.
- Asignar `VariableDefinedBy`.
- Confirmar resultados en la simulacion.
- Ejecutar propagaciones de corrientes o equipos.
- Decidir prioridades entre ecuaciones y specifications.
- Conocer equipos, streams, diagramas o tags.
- Escribir logs o texto de diagnostico.

Newton puede escribir valores numericos temporales unicamente dentro de un
contexto de evaluacion silenciosa y siempre debe restaurarlos antes de retornar.

## 5. Contrato propuesto

```csharp
public interface INewtonSolverRemanufactured
{
    NewtonResult Solve(
        ISolverEquation equation,
        IReadOnlyList<IVariable> adjustableVariables,
        IReadOnlyList<double> initialValues);
}
```

El llamador entrega explicitamente:

- La ecuacion o cluster que se evaluara.
- La lista ordenada de incognitas.
- El vector inicial determinista.

Newton no debe volver a consultar `AdjustableVariables()` para cambiar la
seleccion realizada por MainSolver.

## 6. Resultado numerico

```csharp
public sealed record NewtonResult(
    bool Converged,
    IReadOnlyList<double> Solution,
    int Iterations,
    double FinalError,
    NewtonFailureReason FailureReason);
```

```csharp
public enum NewtonFailureReason
{
    None,
    EmptySystem,
    NonSquareSystem,
    InvalidResidual,
    SingularJacobian,
    NoImprovement,
    MaximumIterations,
    EvaluationFailed
}
```

El resultado describe el intento. No aplica la solucion ni modifica la
procedencia de las variables.

## 7. Comportamiento numerico

Newton debe ejecutar este flujo:

1. Validar que existan residuales e incognitas.
2. Validar que el sistema sea cuadrado.
3. Validar que el vector inicial tenga el tamano correcto.
4. Capturar los valores numericos originales.
5. Evaluar los residuales iniciales.
6. Rechazar residuales no finitos.
7. Comprobar convergencia inicial con tolerancia explicita.
8. Calcular el Jacobiano por diferencias finitas.
9. Resolver el sistema lineal.
10. Aplicar amortiguacion del paso.
11. Aceptar solamente pasos que mejoren el error.
12. Terminar por convergencia o por una razon de fallo explicita.
13. Restaurar siempre los valores originales antes de retornar.

La restauracion debe ejecutarse tambien cuando ocurra una excepcion inesperada.
La implementacion debe usar un bloque `try/finally` o un mecanismo equivalente.

## 8. Estado interno

Los datos de cada ejecucion deben vivir en variables locales. La clase no debe
conservar entre llamadas campos mutables como:

- Ecuacion actual.
- Variables ajustables actuales.
- Alpha actual.
- Iteracion actual.

Esto evita que una ejecucion afecte otra y facilita las pruebas unitarias.

## 9. Valores iniciales

Newton requiere un punto inicial, pero no debe reutilizar automaticamente el
ultimo resultado calculado.

La politica inicial sera:

- MainSolver obtiene el vector mediante `IInitialGuessProvider`.
- La implementacion V1 sera determinista.
- Para una variable indefinida se usara `1.0` en sus unidades internas de solver.
- No se usaran valores calculados persistidos ni resultados de una ejecucion
  anterior.
- Newton recibe el vector y no conoce su origen.

```csharp
public interface IInitialGuessProvider
{
    IReadOnlyList<double> CreateInitialValues(
        IReadOnlyList<IVariable> variables);
}
```

La interfaz permite mejorar las aproximaciones iniciales en el futuro sin
modificar Newton ni MainSolver.

## 10. Evaluacion silenciosa

El calculo del Jacobiano y los pasos amortiguados requieren escribir valores
temporales. Esas escrituras no pueden activar el sistema reactivo.

Se implementara un alcance equivalente a:

```csharp
using var scope = SolverEvaluationScope.Begin();
var result = newtonSolver.Solve(equation, variables, initialValues);
```

Mientras el alcance este activo:

- Se permiten valores numericos temporales.
- No cambia la procedencia de las variables.
- No se notifican observers.
- No se propagan calculos de streams.
- No se ejecutan calculos de equipos.
- No se dispara persistencia ni autosave.

Al terminar el alcance, los valores numericos deben estar restaurados. Esta
proteccion debe ser verificable mediante pruebas y no depender de convenciones
informales.

## 11. Configuracion numerica

Los parametros numericos estaran centralizados:

```csharp
public sealed class NewtonOptions
{
    public int MaxIterations { get; init; }
    public double ResidualTolerance { get; init; }
    public double SingularityTolerance { get; init; }
    public double PerturbationFactor { get; init; }
    public double MinimumPerturbation { get; init; }
    public double MinimumAlpha { get; init; }
    public int MaximumDampingAttempts { get; init; }
}
```

No se distribuiran numeros magicos entre los metodos.

## 12. Criterios de aceptacion de Newton

- Un sistema cuadrado sencillo converge dentro de la tolerancia.
- Un sistema no cuadrado falla con `NonSquareSystem`.
- Un sistema vacio falla con `EmptySystem`.
- Residuales no finitos fallan con `InvalidResidual`.
- Un Jacobiano singular falla de forma controlada.
- Una no convergencia restaura exactamente los valores originales.
- Una convergencia devuelve una propuesta sin modificar procedencias.
- No se notifican cambios durante los ensayos numericos.
- Dos ejecuciones con la misma entrada producen el mismo resultado.
- La clase no contiene referencias a equipos, streams, specifications ni tags.

---

# Tarea 2: MainSolverRemanufactured

## 13. Responsabilidad

`MainSolverRemanufactured` prepara, ordena y ejecuta los intentos de solucion. Es
el unico responsable de aceptar una solucion numerica y publicarla en la
simulacion.

MainSolver debe:

- Limpiar resultados calculados anteriores.
- Construir la lista inicial de ecuaciones pendientes.
- Proteger variables definidas por el usuario o por specifications activas.
- Intentar primero las ecuaciones sencillas.
- Intentar balances individuales.
- Construir e intentar clusters pendientes.
- Detectar progreso real.
- Terminar cuando una pasada completa no produzca progreso.
- Aplicar soluciones aceptadas de manera atomica.
- Devolver un resultado explicito de la simulacion.

## 14. Flujo principal

El metodo principal debe poder entenderse completo en una sola lectura:

```csharp
public async Task<SolveResult> SolveAsync()
{
    ClearCalculatedVariables();
    BuildPendingEquations();

    bool progress;
    int pass = 0;

    do
    {
        progress = false;
        pass++;

        progress |= await SolveSimpleEquationsAsync();
        progress |= await SolveMassAndEnergyBalancesAsync();
        progress |= await SolveClustersAsync();
    }
    while (progress && pass < Options.MaxPasses);

    return BuildResult(pass);
}
```

`MaxPasses` es una proteccion, no el mecanismo normal de terminacion.

## 15. Metodos principales esperados

La primera version debe intentar limitar su flujo a estos metodos:

```text
SolveAsync
ClearCalculatedVariables
BuildPendingEquations
SolveSimpleEquationsAsync
SolveMassAndEnergyBalancesAsync
SolveClustersAsync
TrySolveEquationAsync
ApplySolutionAtomically
BuildResult
```

Se pueden extraer ayudantes cuando eliminen duplicacion real, pero no deben
crearse capas o metodos que oculten el orden de resolucion.

## 16. Orden de ecuaciones sencillas

Las ecuaciones sencillas se intentaran en este orden:

```text
Pressure
Concentration
VaporFraction
Enthalpy
Specification
```

Despues se intentaran:

```text
MassBalance
MassEnergyBalance
Clusters pendientes
```

El orden debe definirse mediante una coleccion ordenada explicita. No puede
depender del orden de insercion de un diccionario.

## 17. Resultado de un intento

Cada intento interno devolvera un estado pequeno y explicito:

```csharp
public enum EquationSolveStatus
{
    Solved,
    Satisfied,
    NotReady,
    NotSquare,
    Failed
}
```

- `Solved`: se acepto una solucion y se retiro la ecuacion.
- `Satisfied`: los residuales ya cumplen la tolerancia y se retiro la ecuacion.
- `NotReady`: faltan dependencias necesarias para evaluar la ecuacion.
- `NotSquare`: la ecuacion puede participar posteriormente en un cluster.
- `Failed`: el intento numerico no convergio o no tiene variables ajustables.

Una ecuacion con cero incognitas y residuales fuera de tolerancia no esta
resuelta. Permanece pendiente como `Failed` con una razon interna explicita.

## 18. Definicion de progreso

Existe progreso solamente cuando:

- Una solucion fue aceptada.
- Se confirmo al menos una variable nueva.
- Una ecuacion satisfecha fue retirada del conjunto pendiente.

No se considera progreso:

- Ejecutar Newton.
- Evaluar residuales.
- Construir un cluster.
- Modificar temporalmente una aproximacion inicial.
- Repetir un intento fallido.

Si una pasada completa no produce progreso, la simulacion termina. No se repite
una ecuacion varias veces sobre un estado identico.

## 19. Ecuaciones sencillas

Una ecuacion sencilla se trata asi:

- Si no puede evaluar sus residuales, devuelve `NotReady`.
- Si tiene cero incognitas y cumple la tolerancia, devuelve `Satisfied`.
- Si tiene cero incognitas y no cumple la tolerancia, devuelve `Failed`.
- Si tiene una incognita y un residual, se intenta con Newton.
- Si tiene diferente cantidad de incognitas y residuales, devuelve `NotSquare`.
- Si converge, MainSolver confirma el resultado y devuelve `Solved`.

Este flujo es comun para todos los tipos sencillos. No se implementaran rutas
especiales por equipo.

## 20. Limpieza de variables

Se conservara la politica de limpieza que ya demostro funcionar:

- Limpiar resultados con procedencia `Solver`.
- Limpiar resultados con procedencia `Specification`.
- Limpiar resultados con procedencia `Equipment`.
- Conservar `UserInput`.
- No limpiar indiscriminadamente `StreamCalculated` desde MainSolver.
- Permitir que cada stream invalide y reconstruya sus propiedades derivadas
  mediante su contrato termodinamico.

La limpieza no debe escribir semillas dependientes del resultado anterior. Las
aproximaciones iniciales pertenecen a `IInitialGuessProvider`.

## 21. Seleccion y proteccion de variables

La regla para seleccionar incognitas se centralizara:

```csharp
bool CanBeAdjusted(IVariable variable);
```

La regla debe rechazar:

- Variables definidas por `UserInput`.
- Objetivos de specifications activas.
- Variables ya confirmadas y protegidas durante la pasada actual.
- Variables que el contrato de la ecuacion declare no ajustables.

La regla no puede usar:

- Tags concretos.
- El diagrama visual.
- El orden de creacion.
- Una direccion global impuesta al proceso.

La informacion puede provenir de cualquier parte del grafo.

## 22. Specifications

Las specifications activas tienen prioridad sobre los balances que puedan
calcular su variable objetivo.

Reglas:

- Se intentan despues de `Enthalpy`.
- Solo se ejecutan cuando sus variables fuente estan calculadas y disponibles.
- No pueden usar valores semilla de una fuente como si fueran resultados.
- Una specification resuelta sale inmediatamente de pendientes.
- Una specification no preparada permanece pendiente.
- Su variable objetivo permanece protegida mientras la specification este
  activa.
- La procedencia `Specification` se asigna solamente durante el commit.

Cuando aplique, se conservaran tres niveles de intento:

1. Specification individual.
2. Specification con el equipo donde reside.
3. Specification con los equipos conectados inmediatos.

Los niveles segundo y tercero reutilizaran el mismo constructor de clusters. No
se implementaran como rutas completas y duplicadas dentro de MainSolver.

## 23. Balances individuales

Despues de las ecuaciones sencillas se intentan los balances pendientes:

```text
MassBalance
MassEnergyBalance
```

Reglas:

- Un balance evaluable y cuadrado se intenta individualmente.
- Un balance no preparado permanece pendiente.
- Un balance no cuadrado queda disponible para clustering.
- Un balance fallido no publica valores parciales.
- Un balance satisfecho sale de pendientes.

## 24. Construccion de clusters

Los clusters se construyen despues de intentar los balances individuales de la
pasada actual.

Algoritmo V1:

1. Tomar ecuaciones pendientes elegibles para clustering.
2. Obtener sus incognitas mediante la regla centralizada.
3. Construir un grafo bipartito ecuacion-variable.
4. Conectar ecuaciones que compartan al menos una incognita real.
5. Obtener los componentes conectados.
6. Crear un candidato por cada componente conectado.
7. Contar residuales e incognitas distintas.
8. Intentar solamente candidatos cuadrados.

Restricciones:

- No agrupar sistemas desconectados.
- No incorporar variables protegidas.
- No realizar busquedas combinatorias de subconjuntos en V1.
- No modificar `NewtonSolverRemanufactured` para aceptar sistemas no cuadrados.
- La construccion debe producir el mismo resultado para el mismo grafo,
  independientemente del orden de las colecciones.

`SolveClustersAsync` construye, valida e intenta los clusters. Construir un
cluster por si solo no cuenta como progreso.

## 25. Commit atomico

Cuando Newton converge, MainSolver debe publicar todas las variables de la
solucion como una sola operacion logica:

```csharp
ApplySolutionAtomically(solution, variables, procedence);
```

Durante el commit:

1. Suspender notificaciones y propagaciones intermedias.
2. Escribir todos los valores numericos.
3. Asignar la procedencia correcta.
4. Finalizar el lote.
5. Notificar las variables confirmadas.
6. Permitir la propagacion reactiva de streams y equipos.

Ningun consumidor debe observar una solucion aplicada parcialmente.

## 26. Procedencia de resultados

La procedencia se decide fuera de Newton:

- Una specification aceptada se publica como `Specification`.
- Una ecuacion general aceptada se publica como `Solver`.
- Un calculo propio de un equipo se publica como `Equipment` cuando corresponda
  a su contrato.
- Las propiedades derivadas por la corriente conservan `StreamCalculated`.

Newton no conoce ni recibe estos valores de procedencia.

## 27. Observabilidad preparada

No se implementaran logs ni trazas en V1. Se preparara una interfaz de
observacion que reciba eventos inmutables:

```csharp
public interface ISolverObserver
{
    void OnSolverEvent(SolverEvent solverEvent);
}
```

La implementacion inicial sera:

```csharp
public sealed class NullSolverObserver : ISolverObserver
{
    public void OnSolverEvent(SolverEvent solverEvent)
    {
    }
}
```

Reglas:

- MainSolver depende de la interfaz, no de Background Activity ni de un logger.
- Newton devuelve informacion numerica mediante `NewtonResult`.
- MainSolver puede emitir eventos de inicio, intento, commit y finalizacion.
- Los eventos no contienen texto formateado para UI.
- El observador nulo no realiza trabajo.
- Una futura implementacion podra convertir eventos en trace sin modificar la
  logica del solver.

No se agregara instrumentacion por iteracion de Newton en esta primera version.

## 28. Resultado de la simulacion

```csharp
public enum SimulationSolveStatus
{
    Converged,
    PartiallySolved,
    NotSolved,
    NumericalFailure
}
```

```csharp
public sealed record SolveResult(
    SimulationSolveStatus Status,
    int Passes,
    int SolvedEquations,
    IReadOnlyList<PendingEquationResult> PendingEquations);
```

Interpretacion:

- `Converged`: no quedan ecuaciones requeridas pendientes.
- `PartiallySolved`: hubo progreso, pero quedaron ecuaciones pendientes.
- `NotSolved`: no fue posible aceptar ninguna solucion.
- `NumericalFailure`: los sistemas preparados fallaron por razones numericas
  relevantes y no hubo una solucion aceptable.

Las ecuaciones pendientes forman parte del resultado aunque V1 no las muestre
en Background Activity.

## 29. Configuracion de MainSolver

```csharp
public sealed class SolverOptions
{
    public int MaxPasses { get; init; }
    public double SatisfactionTolerance { get; init; }
}
```

El orden de tipos de ecuacion sera una coleccion explicita e inmutable. No se
inferira desde diccionarios ni registros de equipos.

## 30. Consistencia entre carga y edicion

La carga de un proyecto y la edicion de una variable deben terminar llamando al
mismo punto de entrada de `MainSolverRemanufactured`.

Ambos escenarios deben usar:

- La misma politica de limpieza.
- El mismo orden de ecuaciones.
- El mismo proveedor de valores iniciales.
- El mismo constructor de clusters.
- La misma politica de commit.
- La misma configuracion numerica.

Solo cambia el motivo que origino el recalculo, no el algoritmo ejecutado.

## 31. PostSolve

`PostSolveAsync` no forma parte del nucleo V1.

Reglas futuras:

- Se ejecutara despues de finalizar balances y specifications.
- No podra modificar silenciosamente variables primarias del sistema resuelto.
- Si invalida el problema principal, debera solicitar un nuevo ciclo de solucion
  de forma explicita.
- El calculo McCabe-Thiele de columnas se revisara despues de estabilizar el
  solver principal.

---

# Verificacion e integracion

## 32. Pruebas de Newton

Agregar pruebas unitarias para:

- Sistema lineal de una ecuacion y una incognita.
- Sistema lineal de varias ecuaciones.
- Sistema no lineal sencillo.
- Sistema no cuadrado.
- Sistema vacio.
- Residuales `NaN` o infinitos.
- Jacobiano singular.
- Maximo de iteraciones.
- Restauracion despues de fallo.
- Restauracion despues de convergencia.
- Ausencia de cambios de procedencia.
- Ausencia de notificaciones durante evaluacion.
- Repetibilidad con entradas identicas.

## 33. Pruebas de MainSolver

Agregar pruebas enfocadas para:

- Ecuacion sencilla resuelta y retirada.
- Ecuacion no preparada que permanece pendiente.
- Specification cuya fuente no esta calculada.
- Specification que se habilita en una pasada posterior.
- Proteccion del objetivo de una specification.
- Balance individual cuadrado.
- Balance no cuadrado enviado a clustering.
- Dos equipos conectados que forman un cluster cuadrado.
- Sistemas desconectados que no se agrupan.
- Pasada sin progreso que termina inmediatamente.
- Commit atomico de varias variables.
- Newton fallido sin contaminacion del modelo.
- Calculo repetido con el mismo resultado.
- Resultado identico entre edicion y carga del proyecto.

## 34. Escenarios de regresion del proyecto

Antes de activar el nuevo solver se verificaran progresivamente:

1. Un equipo con una ecuacion sencilla.
2. Bomba.
3. Splitter.
4. Intercambiador.
5. Columna C-101.
6. Columna C-127.
7. Columna C-140.
8. Specifications interdiagrama.
9. Cierre y reapertura repetida del proyecto.
10. Cambio de una variable no relacionada sin alterar subsistemas ya resueltos.

Los tags anteriores identifican escenarios de prueba. No autorizan condiciones
especiales dentro del codigo.

## 35. Estrategia de implementacion

1. Crear contratos y pruebas de `NewtonSolverRemanufactured`.
2. Implementar Newton hasta completar sus criterios de aceptacion.
3. Crear los contratos minimos de MainSolver.
4. Implementar limpieza y construccion de pendientes.
5. Implementar ecuaciones sencillas.
6. Implementar balances individuales.
7. Implementar el constructor de clusters.
8. Implementar commit atomico.
9. Agregar el observador nulo.
10. Ejecutar pruebas pequenas de equipos.
11. Ejecutar escenarios de columnas y proyectos interdiagrama.
12. Habilitar seleccion controlada entre solver actual y remanufacturado.

## 36. Activacion controlada

Los dos solvers conviviran durante la validacion. La seleccion se realizara por
inyeccion de dependencias o configuracion interna controlada, sin duplicar logica
de UI.

No se eliminara el solver actual hasta que:

- Newton nuevo supere sus pruebas unitarias.
- MainSolver nuevo supere sus pruebas de integracion.
- La carga y el recalculo produzcan resultados repetibles.
- Los balances de masa y energia usados como regresion cierren dentro de la
  tolerancia acordada.

## 37. Fuera de alcance de V1

- Reescribir ecuaciones fisicas existentes.
- Modificar balances de columnas sin un caso de regresion aislado.
- Optimizar McCabe-Thiele.
- Reactivar o redisenar `PostSolveAsync` de columnas.
- Implementar logs o trazas visuales.
- Crear una UI de diagnostico.
- Buscar combinaciones arbitrarias de subconjuntos para formar clusters.
- Permitir sistemas no cuadrados dentro de Newton.
- Implementar semillas inteligentes basadas en resultados anteriores.
- Eliminar el solver actual.

## 38. Definicion de terminado para V1

La primera version estara terminada cuando:

- El flujo principal pueda comprenderse leyendo `SolveAsync` y sus tres etapas.
- Newton solo proponga soluciones y nunca publique resultados.
- MainSolver publique soluciones completas mediante commit atomico.
- Los ensayos numericos no activen el sistema reactivo.
- Una pasada sin progreso termine el proceso sin ciclos innecesarios.
- Las ecuaciones resueltas salgan del listado pendiente.
- Las ecuaciones no resueltas permanezcan identificables en `SolveResult`.
- Los mismos inputs produzcan el mismo resultado al editar y al cargar.
- No exista logica especial para equipos concretos.
- El solver actual permanezca disponible como respaldo durante la validacion.

## 39. Regla arquitectonica final

> `NewtonSolverRemanufactured` propone una solucion numerica.
> `MainSolverRemanufactured` decide si la acepta y la publica.

Esta frontera no debe romperse durante la implementacion.
