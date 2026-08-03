# 12 - Termodinamica Y Comportamiento Numerico

Estado: Borrador

## Contexto

El solver de proceso depende de calculos termodinamicos, propiedades puras, equilibrio,
balances y solucion Newton. Un refactor de coordinacion no debe cambiar resultados
fisicos sin evidencia y tolerancias explicitas.

## Resultado Numerico Explicito

Todo calculo relevante debe poder distinguir:

- resultado valido y convergente;
- resultado valido en limite fisico;
- datos insuficientes;
- input fuera de dominio;
- sistema estructuralmente invalido;
- no convergencia;
- cancelacion;
- error inesperado.

No convergencia no se representa como exito silencioso ni necesariamente como
excepcion.

## Unidades Y Normalizacion

- Las ecuaciones operan en unidades internas conocidas.
- Cada variable declara escala de normalizacion no nula.
- Entradas UI se convierten antes de entrar al calculo.
- Residuales y tolerancias se interpretan con escala documentada.
- Comparaciones fisicas no dependen de display units.

## Armado De Sistemas

Antes de Newton se valida:

- numero de ecuaciones;
- numero de incognitas ajustables;
- grados de libertad de la ecuacion o cluster;
- independencia/rango de las ecuaciones cuando los residuales pueden ser dependientes;
- variables duplicadas;
- ecuaciones sin residual;
- valores iniciales finitos;
- escalas validas;
- dependencias del cluster.

`NewtonSolver` no se modifica para aceptar sistemas no cuadrados ni para retirar
ecuaciones por residual casualmente bajo. Se corrige el armado o se devuelve un
diagnostico estructural.

## Grados De Libertad Por Ecuacion

El solver opera por intencion: un equipo puede entregar varias ecuaciones candidatas y
solo converge la que este estructuralmente lista en esa ronda. Cada ecuacion debe
decidir si representa un caso resoluble antes de exponer residuales y variables.

Reglas vigentes para balances de equipos:

- Una ecuacion no debe intentar ser universal; debe representar un modo resolutivo
  concreto.
- Si faltan datos requeridos para ese modo, `Residuals` y `Variables` devuelven listas
  vacias.
- `Variables` puede exponer el universo de variables de esa ecuacion; `NewtonSolver`
  filtra las ajustables con `!IsDefined`.
- La ecuacion si puede usar `IsDefined` para decidir sus datos requeridos y sus grados
  de libertad.
- `CanSolve` debe equivaler a: incognitas candidatas igual a ecuaciones
  independientes.
- En balances de componente, `ncomp` no garantiza independencia; se valida rango.

Ejemplos aprobados:

- `ComponentMassBalanceEquation`: resuelve fracciones masicas cuando todos los flujos
  masicos estan definidos y las fracciones faltantes cierran DOF.
- `ComponentMassBalanceByMassFlowEquation`: resuelve flujos masicos cuando las
  composiciones estan definidas y la matriz de composiciones de los flujos
  desconocidos tiene rango completo.
- `ComponentMassBalanceMixedEquation`: resuelve casos mixtos solo si el total de
  flujos/fracciones faltantes iguala el rango independiente del balance.
- `GlobalMassEnergyBalanceEquation`: resuelve flujos masicos con balances de
  componentes + energia; la matriz de DOF usa filas de fracciones masicas y entalpia
  masica.
- `GlobalEnergyBalanceByMassEnthalpyEquation`: resuelve una unica entalpia masica
  faltante en cualquier corriente cuando todos los flujos masicos estan definidos y el
  flujo de esa corriente no es cero.

Casos degenerados son resultados esperados, no errores de Newton. Por ejemplo, dos
salidas con igual composicion y dos flujos desconocidos producen rango uno aunque haya
dos componentes; el solver no debe inventar una solucion con flujo negativo.

## Convergencia

- Cada algoritmo define tolerancia residual, tolerancia de paso y maximo de
  iteraciones cuando aplique.
- Convergencia inmediata se valida contra el mismo contrato estructural.
- NaN, infinito y singularidad producen diagnostico.
- Damping o fallback no ocultan un sistema invalido.
- El resultado informa iteraciones y error final.

## Limites Fisicos

Segun la variable y el modelo:

- flujos no negativos;
- composiciones entre limites validos y suma dentro de tolerancia;
- presiones positivas donde corresponda;
- temperaturas dentro del dominio de correlaciones;
- propiedades finitas;
- fraccion de vapor en rango;
- balances de masa y energia dentro de tolerancia.

Un algoritmo puede usar valores temporales fuera de rango durante iteracion solo si la
estrategia lo controla y el resultado final se valida.

## Termodinamica

- El metodo termodinamico es consistente para todos los streams del proyecto.
- Componentes requeridos deben existir en el metodo.
- Correlaciones validan rango y coeficientes.
- Estrategias PT, PH, PS y con fraccion de vapor devuelven estado explicito.
- Phase envelope y calculos de columna no cambian inputs del proyecto como efecto
  secundario no documentado.

## Determinismo Y Concurrencia

- Mismas entradas, configuracion y tolerancias producen resultados equivalentes.
- Dos simulaciones no mutan simultaneamente las mismas variables.
- Calculos paralelos usan objetos independientes o sincronizacion demostrable.
- Cancelar un calculo no publica resultados parciales como vigentes.

## Invariantes

1. Valores no finitos nunca se publican como resultados validos.
2. Un sistema no cuadrado se rechaza antes de resolver.
3. Solo ecuaciones realmente convergentes salen del trabajo pendiente.
4. Las tolerancias son explicitas en pruebas y codigo numerico.
5. Inputs del usuario no se limpian al reiniciar resultados calculados.
6. El metodo termodinamico es unico por proyecto activo.
7. Post-calculos fallidos se reflejan en el resultado global.
8. Resultados cancelados o atrasados no reemplazan resultados actuales.

## Criterios De Aceptacion

1. Escenarios nominales conservan resultados dentro de tolerancias aprobadas.
2. Flujo cero no produce NaN ni division no controlada.
3. Composicion invalida se rechaza antes del equilibrio.
4. Correlacion fuera de rango produce diagnostico identificable.
5. Sistema no cuadrado no entra a Newton.
6. No convergencia informa iteraciones y residual final.
7. Cancelacion no deja el solver en estado activo.
8. Repetir el mismo caso produce resultado equivalente.

## Pruebas Requeridas

- Conversiones y normalizacion por magnitud.
- Balances de masa y energia.
- Componente puro y mezclas conocidas.
- Flujo cero y valores limite.
- Composicion menor, igual y mayor que la tolerancia de suma.
- Presion y temperatura fuera de dominio.
- Jacobiano singular y valores iniciales invalidos.
- Sistemas cuadrados y no cuadrados.
- Convergencia inmediata, normal y no convergencia.
- Estrategias de equilibrio soportadas.
- Phase envelope cancelado y completo.
- Regresiones numericas de cada bug corregido.

## Objetivos De Refactor Posteriores

- `MainSolver`, `SolverNewtonSolver` y clusters.
- Variables y normalizacion.
- Estrategias de equilibrio y flujo.
- Facades y post-calculos.
- Orquestacion de columna y phase envelope.

## Fuera De Alcance

- Cambiar modelos termodinamicos sin validacion independiente.
- Optimizar rendimiento antes de tener resultados de referencia.
- Aceptar sistemas estructuralmente invalidos para evitar un fallo visible.
