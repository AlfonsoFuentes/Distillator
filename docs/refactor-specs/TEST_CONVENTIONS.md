# Convenciones E Infraestructura De Pruebas

Estado: En revision M02

## Objetivo

Definir una estructura de pruebas incremental para Distillator antes de crearla en
M03. La suite debe proteger comportamiento funcional y numerico sin obligar a crear
todos los proyectos de pruebas desde el inicio.

## Inventario Actual

- La solucion usa .NET 10 y no contiene proyectos de pruebas.
- No existen referencias a frameworks, runners, mocks ni librerias de aserciones.
- `Shared/SolverQwen/StreamMixerBalanceRegressionTest.cs` contiene cuatro escenarios
  con aserciones manuales y espera mediante `Thread.Sleep(250)`.
- `Shared/SolverQwen/StreamIntegrationTest.cs` es un banco exploratorio grande que
  ejecuta escenarios y escribe resultados en consola.
- Ambos archivos forman parte del ensamblado de produccion `Shared`; no son
  descubiertos por `dotnet test`.

Estos archivos se conservan como referencia legacy. Sus escenarios se migraran de
forma gradual y no se borraran en M03.

## Decisiones Propuestas M02

### Framework Base

- Usar xUnit como framework principal.
- Ejecutar la suite mediante `dotnet test`.
- Mantener `Nullable` e `ImplicitUsings` habilitados y usar `net10.0`.
- Fijar versiones exactas de paquetes en M03, al crear y restaurar el primer proyecto.
- No agregar inicialmente una libreria de mocks ni una libreria de aserciones.
  Preferir fakes pequenos y `Assert` de xUnit hasta que exista una necesidad repetida.

### Proyectos Incrementales

M03 crea solamente:

```text
tests/
  Distillator.Core.Tests/
```

`Distillator.Core.Tests` cubre dominio, unidades, solver, serializacion de intencion y
coordinadores que puedan probarse en memoria. Sus referencias iniciales son:

```text
UnitSystem
Shared
Distillator.Domain
```

Los proyectos siguientes se crean solo al llegar a la primera unidad que los exige:

```text
tests/Distillator.Client.ComponentTests
tests/Distillator.Server.IntegrationTests
```

- `Client.ComponentTests` usara bUnit para componentes Blazor.
- `Server.IntegrationTests` usara la infraestructura oficial de ASP.NET Core para
  alojar la API durante pruebas.
- Las pruebas de PostgreSQL usaran una base aislada con comportamiento PostgreSQL
  real. No se sustituira por EF Core InMemory para probar transacciones, JSONB,
  concurrencia o restricciones.
- El mecanismo concreto de base aislada se decide antes de la primera prueba de
  servidor, segun disponibilidad controlada de PostgreSQL o contenedores.

### Estructura Interna

Las carpetas siguen la capacidad protegida, no el tipo de helper:

```text
Distillator.Core.Tests/
  Simulation/
  Solver/
  Thermodynamics/
  Variables/
  Persistence/
  TestSupport/
```

`TestSupport` contiene builders, fakes y datos comunes solo cuando se reutilizan. Un
helper usado por una sola clase permanece junto a esa clase.

### Nombres

- Clases: `<Subject>Tests`.
- Metodos: `<Operation>_When<Condition>_Should<ExpectedResult>`.
- Nombres de codigo y pruebas en ingles.
- Cada prueba protege un comportamiento principal.
- Relacionar pruebas con specs mediante traits cuando aporte trazabilidad:

```csharp
[Trait("Spec", "01")]
[Trait("Level", "Unit")]
```

### Forma De Las Pruebas

- Organizar claramente Arrange, Act y Assert sin comentarios obvios.
- No depender del orden de ejecucion ni de estado compartido mutable.
- No usar hora local, GUID aleatorio o datos externos cuando afecten el resultado.
- Inyectar reloj, IDs o dependencias controlables cuando el caso lo necesite.
- No usar `Thread.Sleep` para sincronizacion.
- Para concurrencia asincrona usar barreras deterministas, por ejemplo
  `TaskCompletionSource`, y timeouts solamente como proteccion contra bloqueos.
- Una prueba de excepcion debe verificar tambien el estado final observable.

### Calculos Numericos

- Toda comparacion numerica declara una tolerancia con unidad y motivo fisico.
- No usar una tolerancia global para magnitudes diferentes.
- Verificar, segun aplique, balance de masa, balance de energia, composicion, limites
  fisicos, convergencia y no convergencia.
- Separar precision numerica de convergencia: un sistema puede terminar sin converger
  sin lanzar una excepcion.
- Usar datos pequenos y explicitos; evitar snapshots opacos de cientos de valores.

### Dobles De Prueba

- Preferir fakes escritos contra interfaces estrechas para solver, persistencia y
  reloj.
- Un fake registra llamadas y permite controlar finalizacion, fallo y respuesta.
- No replicar dentro del fake la logica de produccion que la prueba intenta validar.
- Introducir una libreria de mocks solo si los fakes repetidos demuestran un beneficio
  claro.

### Datos Y Entornos

- Las pruebas unitarias no usan red, navegador, PostgreSQL ni secretos.
- Las pruebas de servidor no usan datos de produccion.
- Cada fixture crea y elimina unicamente sus propios datos identificables.
- Las pruebas manuales registran usuario, rol, proyecto, diagrama, entradas y resultado
  esperado.
- Ningun secreto se incorpora al repositorio para ejecutar pruebas.

## Comandos Normalizados

Despues de crear la infraestructura en M03, los comandos base seran:

```text
dotnet test tests/Distillator.Core.Tests/Distillator.Core.Tests.csproj
dotnet test Distillator.slnx
```

Cada unidad ejecuta primero su filtro enfocado y despues las regresiones relacionadas.
El comando exacto y su resultado se registran como evidencia en el plan maestro.

## Alcance De M03

1. Crear `Distillator.Core.Tests` y agregarlo a `Distillator.slnx`.
2. Instalar solamente los paquetes necesarios para xUnit y `dotnet test`.
3. Crear una prueba minima que confirme descubrimiento y ejecucion.
4. Migrar un escenario pequeno de regresion existente como caracterizacion, sin
   borrar ni alterar la referencia legacy.
5. Sustituir la espera temporal del escenario migrado por control determinista cuando
   el contrato actual lo permita.
6. Registrar build, descubrimiento, ejecucion y resultado de linea base.

M03 no crea todavia proyectos de componentes o servidor y no refactoriza el solver.

## Criterios De Aceptacion De M02

1. Framework, estructura inicial y crecimiento posterior estan definidos.
2. Las pruebas asincronas no dependen de esperas arbitrarias.
3. Las reglas numericas exigen tolerancias explicitas.
4. Los escenarios legacy tienen una estrategia de migracion sin borrado prematuro.
5. La infraestructura inicial respeta KISS y YAGNI.
6. Alfonso aprueba estas convenciones antes de iniciar M03.

## Fuera De Alcance

- Crear o restaurar paquetes.
- Ejecutar pruebas que aun no existen.
- Modificar los dos bancos de prueba embebidos en `Shared`.
- Elegir ahora infraestructura de contenedores o CI.
- Definir cobertura porcentual como objetivo.
