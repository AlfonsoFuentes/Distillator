# 11 - Contratos De Equipos

Estado: Borrador

## Contexto

Cada elemento visual representa un facade de solver y un contrato de puertos. La UI,
la topologia, la persistencia y el solver deben usar el mismo contrato por tipo.

## Contrato Comun

Todo equipo soportado debe definir:

- tipo y prefijo de naming;
- facade concreto;
- puertos fijos y grupos dinamicos;
- reglas de conexion y desconexion;
- variables de input y variables calculadas;
- ecuaciones entregadas al solver;
- post-calculos;
- estado visual y diagnostico;
- informacion de intencion que debe persistirse.

## Matriz Inicial

| Equipo | Facade | Contrato principal de puertos |
|---|---|---|
| Material Stream | `IFacadeStream` | `Inlet`, `Outlet` |
| Pump | `SolverPump` | `Suction`, `Discharge` |
| Control Valve | `SolverValve` | `Inlet`, `Outlet` |
| Heat Exchanger | `SolverHeatExchanger` | Tube in/out, Shell in, Condensate out |
| Plate Exchanger | `SolverHeatExchanger` | Hot in/out, Cold in/out |
| Reboiler | `SolverHeatExchanger` | Tube in/out, Shell in, Condensate out |
| Flash Tank | `SolverDrum` | `Feed`, `Vapor`, `Liquid` |
| Splitter | `SolverSplitter` | Inlet fijo, outlets dinamicos |
| Mixer | `SolverStreamMixer` | Outlet fijo, inlets dinamicos |
| Vessel | `SolverVessel` | Inlets y outlets dinamicos |
| Column | `SolverColumn` | Overhead, Bottoms, Reflux, Reboiler Return y grupos dinamicos |
| Off-Page Connector | Sin equipo calculable propio | `Transfer` segun direccion |

Los nombres publicos finales se verificaran contra las constantes tipadas existentes.
La UI no buscara puertos fijos mediante texto repetido.

## Streams

- Son la frontera de variables termodinamicas entre equipos.
- Conservan inputs, composicion, unidades y auditoria.
- Conocen equipos aguas arriba y abajo mediante conexiones explicitas.
- Un stream se registra una sola vez en el solver.
- Un stream huerfano limpia resultados calculados sin borrar inputs validos.

## Equipos De Dos Puertos

Pump y Control Valve:

- un inlet y un outlet;
- conectar asigna las propiedades tipadas del facade;
- desconectar limpia exactamente el lado correspondiente;
- balances y ecuaciones solo se ofrecen cuando tienen datos estructurales necesarios.

## Intercambiadores

- Los cuatro lados deben mapearse consistentemente entre visual y facade.
- Hot y Cold no se intercambian por el orden de una lista.
- Cada lado conserva balances de masa, concentracion, presion y energia aplicables.
- Reboiler puede compartir facade, pero conserva semantica visual y de puertos propia.

## Flash Tank

Contrato fijo:

- `Feed`: inlet;
- `Vapor`: outlet;
- `Liquid`: outlet.

No tiene puertos extra dinamicos en el contrato actual. Conectar o desconectar debe
invocar `Set/UnSet` tipado correspondiente.

## Equipos Con Puertos Dinamicos

- Splitter agrega outlets sin cambiar el inlet fijo.
- Mixer agrega inlets sin cambiar el outlet fijo.
- Vessel administra inlets y outlets segun su contrato visual.
- Column administra feeds y side draws, manteniendo sus cuatro puertos principales.
- Agregar o retirar puertos actualiza layout y facade sin duplicar simulacion.

### SolverVessel

`SolverVessel` es el banco principal para equipos con numero dinamico de entradas y
salidas. Su contrato actual no asume un unico caso de balance; entrega varias
ecuaciones por intencion y cada una decide si sus grados de libertad cierran.

Ecuaciones vigentes:

- `MassFractionDistributorEquation`: para 1 entrada y una o mas salidas; propaga
  composicion cuando la fisica del caso es de divisor sin separacion.
- `GlobalMassBalanceEquation`: balance global de masa para resolver un flujo masico.
- `ComponentMassBalanceEquation`: balance por componentes para resolver fracciones
  masicas.
- `ComponentMassBalanceByMassFlowEquation`: balance por componentes para resolver
  flujos masicos con composiciones conocidas y rango suficiente.
- `ComponentMassBalanceMixedEquation`: caso mixto conservador para flujos y
  fracciones cuando el DOF cierra exactamente.
- `GlobalMassEnergyBalanceEquation`: balances de componentes + energia para resolver
  varios flujos masicos cuando composiciones y entalpias masicas estan disponibles.
- `GlobalEnergyBalanceByMassEnthalpyEquation`: balance de energia puro para resolver
  una entalpia masica faltante en cualquier corriente.

Validaciones manuales aprobadas:

- `1/1`: igualdad de composicion y balance global.
- `1/2`: divisor; composiciones iguales y flujos subdeterminados no se inventan.
- `2/1`: mezcla con composicion faltante en entrada o salida.
- `2/2`: resolucion de composiciones o flujos cuando matriz/rango lo permite.
- `3/1`: mezcla con entalpia de salida calculada por balance de energia.
- `3/2`: balances de componentes + energia resolviendo multiples flujos.

## Column

Los calculos de columna pueden incluir FUG, VLE, McCabe-Thiele y plate-by-plate. Cada
estrategia:

- recibe entradas validadas;
- acepta cancelacion cuando sea asincrona;
- devuelve resultado explicito;
- informa no convergencia sin dejar datos parciales como resultado vigente;
- no bloquea la finalizacion global indefinidamente.

## Invariantes

1. Tipo visual y tipo de facade son compatibles.
2. Cada puerto fijo tiene una propiedad o constante tipada unica.
3. Conectar y desconectar son operaciones inversas observables.
4. Listas `Inlets` y `Outlets` coinciden con propiedades tipadas.
5. Un equipo se registra una sola vez en el solver.
6. Ecuaciones no dependen del orden accidental de una lista de puertos.
7. Puertos dinamicos conectados no desaparecen sin limpieza.
8. Post-calculos producen un resultado final observable.
9. Inputs sobreviven a limpieza de resultados del solver.
10. El dialogo no duplica la simulacion ejecutada por el conector.

## Criterios De Aceptacion

Para cada equipo:

1. Se crea con facade, nombre y puertos esperados.
2. Cada puerto conecta y desconecta el lado correcto.
3. Guardar y recargar conserva inputs y topologia.
4. El solver recibe el equipo y streams una sola vez.
5. Balances convergen en un escenario nominal conocido.
6. Datos insuficientes producen pendiente o diagnostico, no excepcion arbitraria.
7. Borrar limpia registry, pipes, puertos y solver.
8. El dialogo Viewer es completamente read-only.

## Pruebas Requeridas

Se creara una matriz por equipo con:

- construccion y puertos;
- cada direccion de conexion;
- desconexion;
- inputs principales;
- ecuaciones generadas;
- balance nominal;
- flujo cero;
- limites fisicos;
- no convergencia;
- serializacion e hidratacion;
- eliminacion;
- dialogo y permisos.

## Objetivos De Refactor Posteriores

- Visual elements en `Shared/ProcessFlowDiagram`.
- Facades en `Shared/SolverConsecutive/Equipments`.
- Dialogos de `Client/Pages/UnitOperations`.
- Fabrica, conexion y registro en solver.

## Fuera De Alcance

- Equipos P&ID y electricos aun no implementados.
- Crear equipos nuevos durante este refactor.
- Redisenar ecuaciones sin prueba fisica de regresion.
