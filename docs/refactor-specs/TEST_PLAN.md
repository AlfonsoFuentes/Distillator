# Plan Incremental De Pruebas

Estado: Borrador activo

Las herramientas, estructura de proyectos, nombres y reglas deterministas se
especifican en [TEST_CONVENTIONS.md](TEST_CONVENTIONS.md). Este plan define que debe
probarse; ese documento define como se organiza y ejecuta la suite.

## Objetivo

Confirmar Distillator funcionalidad por funcionalidad. Cada porcion debe tener un
resultado esperado observable, pruebas proporcionales al riesgo y evidencia de que el
cambio funciona antes de continuar con la siguiente.

El objetivo no es acumular pruebas por cantidad. Cada prueba debe proteger una regla
funcional, un calculo importante o un bug que no debe reaparecer.

## Unidad De Trabajo

Una unidad de trabajo es una porcion vertical pequena que pueda demostrarse de
principio a fin. Ejemplos:

- ejecutar una simulacion y esperar su resultado real;
- editar una temperatura y conservar su auditoria;
- guardar un input valido despues de una no convergencia;
- mover un equipo sin ejecutar solver;
- conectar dos equipos y reconstruir la conexion al recargar.

Una unidad no debe mezclar varias decisiones funcionales independientes.

## Ciclo Obligatorio

### 1. Definir

- Identificar la spec y la regla concreta.
- Escribir entradas, precondiciones y resultado esperado.
- Aclarar que queda fuera del cambio.

### 2. Caracterizar

- Crear una prueba que describa el comportamiento correcto.
- Cuando sea necesario, registrar tambien el comportamiento actual incorrecto.
- Confirmar que la prueba falla por la causa que se quiere corregir.

### 3. Preservar Referencia

- Conservar temporalmente la implementacion anterior comentada.
- Marcarla como `LEGACY - TEMPORARY`, indicando spec, reemplazo y condicion de
  eliminacion.
- Confirmar que la ruta legacy ya no se ejecuta junto con la nueva.
- Conservar tambien el estado anterior en Git como referencia definitiva.

### 4. Implementar

- Aplicar el cambio minimo que satisface la regla.
- No incorporar refactors colaterales sin una spec relacionada.

### 5. Verificar

- Ejecutar primero las pruebas de la unidad modificada.
- Ejecutar luego las regresiones directamente relacionadas.
- Hacer prueba manual cuando intervengan UI, SignalR o varios usuarios.

### 6. Confirmar

- Comparar resultado obtenido contra resultado esperado.
- Registrar evidencia y limitaciones.
- Marcar la unidad como aceptada antes de comenzar otra.

## Puerta De Aceptacion

Una unidad esta terminada solo cuando:

1. El comportamiento esperado esta escrito y no es ambiguo.
2. La prueba principal pasa.
3. Las regresiones relacionadas pasan.
4. No aparecen warnings nuevos atribuibles al cambio.
5. La prueba manual requerida fue confirmada.
6. Los errores se presentan de la forma definida por la spec.
7. No queda trabajo necesario oculto bajo comentarios o excepciones ignoradas.
8. La evidencia permite repetir la comprobacion.
9. La implementacion legacy sigue disponible y marcada mientras la unidad aun no ha
   sido confirmada por Alfonso.

Si un punto no aplica, debe indicarse expresamente en el cierre de la unidad.

## Eliminacion De Legacy

La referencia temporal se elimina solamente cuando:

1. La prueba principal y las regresiones pasan.
2. La comprobacion manual requerida coincide con el resultado esperado.
3. Alfonso confirma la funcionalidad.
4. No existen consumidores de la ruta anterior.
5. Git contiene el estado previo recuperable.

La eliminacion se realiza como una unidad de limpieza separada. Esa limpieza no cambia
comportamiento y vuelve a ejecutar las pruebas de la funcionalidad.

## Niveles De Prueba

### Nivel 1 - Dominio Y Calculo

Pruebas rapidas sin UI, HTTP, PostgreSQL ni SignalR.

Protegen:

- reglas de dominio;
- clasificacion de cambios;
- estados y transiciones;
- unidades y validaciones;
- ecuaciones y tolerancias;
- convergencia y no convergencia;
- serializacion de intencion.

### Nivel 2 - Integracion En Memoria

Prueban la colaboracion entre servicios usando dependencias controladas.

Protegen:

- coordinador de simulacion y solver;
- coordinador de autosave y gateway HTTP falso;
- orden de operaciones;
- coalescencia y serializacion;
- manejo de fallos y reintentos;
- mapeo DTO-dominio y dominio-DTO.

### Nivel 3 - Integracion De Servidor

Prueban endpoints con autenticacion y una base de datos aislada.

Protegen:

- Owner, Editor y Viewer;
- validacion de DTOs;
- transacciones;
- versionado y conflictos;
- auditoria;
- persistencia atomica de uno o varios diagramas;
- emision de eventos solo despues de guardar.

### Nivel 4 - Componentes Blazor

Prueban componentes relevantes sin depender de una sesion manual completa.

Protegen:

- commit contra texto temporal;
- estado read-only;
- mensajes de validacion;
- eventos emitidos por la UI;
- ausencia de llamadas directas al solver y HTTP.

### Nivel 5 - Flujo Manual Multiusuario

Se reserva para comportamiento dificil de representar en una prueba aislada.

Protege:

- dos navegadores o sesiones A y B;
- SignalR y recarga HTTP;
- presencia;
- conflictos mientras hay cambios locales;
- conexiones interdiagrama visibles en ambos usuarios;
- comportamiento publicado sin depender de F5.

## Evidencia Por Unidad

Cada cierre debe registrar:

```text
Spec:
Unidad:
Precondiciones:
Resultado esperado:
Referencia legacy:
Pruebas ejecutadas:
Resultado obtenido:
Prueba manual:
Regresiones:
Confirmacion de Alfonso:
Legacy eliminado:
Pendientes:
```

La evidencia puede quedar inicialmente en el resumen de trabajo. Cuando exista una
suite estable, los nombres de las pruebas seran la evidencia principal repetible.

## Matriz Incremental

| Orden | Funcionalidad | Prueba principal | Confirmacion manual |
|---:|---|---|---|
| 1 | Finalizacion real del solver | Esperar una ejecucion hasta post-calculos | Estado visible termina en el momento correcto |
| 2 | Simulacion unica por proyecto | Varias solicitudes no se solapan | Ediciones rapidas no congelan ni mezclan resultados |
| 3 | Coalescencia | Diez cambios producen maximo una repeticion | Ultimo valor queda calculado |
| 4 | Input valido | Aplica valor, auditoria y revision dirty | Editar temperatura y confirmar valor |
| 5 | Input invalido | No muta, no simula, no guarda | Introducir texto o rango invalido |
| 6 | Autosave serializado | Respuestas no se aplican fuera de orden | Ediciones rapidas conservan ultimo input |
| 7 | Solver fallido | Conserva intencion y permite reintento | Provocar no convergencia controlada |
| 8 | Hidratacion | Reconstruye intencion y espera recalculo | Cerrar y abrir proyecto |
| 9 | Cambio visual | Guarda sin solver | Mover, rotar, pan y zoom |
| 10 | Topologia local | Conecta, simula, guarda y recarga | Crear y eliminar conexion |
| 11 | Conexion interdiagrama | Persiste ambos extremos atomicamente | Recargar A y B sin F5 |
| 12 | Eliminacion de diagrama | Limpia conexiones y diagramas sobrevivientes | Borrar diagrama conectado |
| 13 | Concurrencia | Detecta version atrasada | A y B editan el mismo diagrama |
| 14 | Realtime | Notifica despues de guardar | A cambia y B recibe version correcta |
| 15 | Permisos | Viewer no muta ni persiste | Probar Owner, Editor y Viewer |
| 16 | Configuracion | Aplica unidades y naming una vez | Cambiar configuracion y recargar |
| 17 | Equipos | Contrato por tipo de equipo | Matriz equipo por equipo |
| 18 | Catalogos y reportes | CRUD y exportacion enfocada | Verificar roles y archivo generado |

El orden puede ajustarse cuando una dependencia tecnica lo exija, pero una unidad no
se declara aceptada por pruebas pertenecientes a otra.

## Primer Paquete - Ciclo De Simulacion

### S1 - Finalizacion Real

Precondicion:

- proyecto con solver controlado;
- el post-calculo permanece bloqueado hasta que la prueba lo libere.

Accion:

- solicitar una simulacion;
- comprobar el estado antes y despues de liberar el post-calculo.

Resultado esperado:

- la tarea no termina mientras el post-calculo siga activo;
- el estado permanece `Running`;
- al liberar el post-calculo se obtiene un unico resultado final;
- el estado deja de ser `Running`.

### S2 - No Solapamiento

Precondicion:

- primera ejecucion bloqueada de forma controlada.

Accion:

- solicitar una segunda simulacion mientras la primera esta activa.

Resultado esperado:

- solo existe una entrada concurrente al solver;
- la segunda solicitud queda pendiente;
- al terminar la primera se ejecuta la revision mas reciente.

### S3 - Coalescencia

Accion:

- enviar diez solicitudes con revisiones crecientes durante la primera ejecucion.

Resultado esperado:

- se ejecutan dos simulaciones en total;
- la segunda corresponde a la revision numero diez;
- las revisiones intermedias quedan superadas.

### S4 - No Convergencia

Accion:

- usar un sistema valido que termine sin converger.

Resultado esperado:

- la ejecucion termina normalmente;
- `Completed` y `Converged = false` se distinguen;
- se conservan diagnosticos y se permite otra ejecucion.

### S5 - Excepcion

Accion:

- provocar una excepcion controlada durante ecuaciones y otra durante post-calculos.

Resultado esperado:

- cada ejecucion produce exactamente un resultado final;
- el resultado contiene diagnostico sin dejar el coordinador bloqueado;
- una ejecucion posterior puede comenzar.

### S6 - Revision Atrasada

Accion:

- completar una simulacion de revision anterior despues de que exista una revision
  local mas reciente.

Resultado esperado:

- el resultado anterior puede registrarse como historial;
- no marca la revision nueva como calculada ni guardada;
- se procesa la revision mas reciente.

## Regresion Del Solver

Cada cambio que alcance ecuaciones o equipos debe agregar casos con tolerancias
explicitas. Como minimo, segun el equipo afectado:

- balance de masa;
- balance de energia;
- flujo cero;
- composicion invalida;
- presion o temperatura fuera de rango;
- sistema no cuadrado;
- convergencia;
- no convergencia.

No se modificara `NewtonSolver` para hacer pasar una prueba que en realidad evidencia
un armado incorrecto de ecuaciones o variables.

## Pruebas Manuales Y Datos

- Los escenarios manuales deben usar proyectos de prueba identificables.
- No se probaran refactors contra datos de produccion.
- Cada prueba multiusuario indicara usuario, rol, proyecto, diagrama y accion.
- Los escenarios numericos deben fijar entradas, unidades y tolerancias esperadas.
- La limpieza de datos de prueba debe ser explicita y limitada al entorno de prueba.

## Condicion Para Empezar Implementacion

Antes de modificar el ciclo de simulacion deben acordarse:

1. La diferencia entre ejecucion completada y solucion convergente.
2. La politica de coalescencia de solicitudes.
3. El comportamiento del autosave cuando la simulacion falla.
4. La forma minima de identificar revision y ejecucion.
