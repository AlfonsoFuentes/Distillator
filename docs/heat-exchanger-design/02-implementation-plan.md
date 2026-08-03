# 02 - Plan De Implementacion De Diseno De Intercambiador

Estado: Borrador

## Principio Rector

Primero se construye un backend de calculo excelente, puro y recalculable. La UI debe
consumir resultados, no contener ecuaciones ni reglas de Kern.

El alcance inicial debe ser el intercambiador de tubo y coraza. Rehervidor, pelicula y
forzada se conservan como conocimiento identificado, pero no entran hasta que el caso
principal este estable.

El modulo de diseno no reemplaza al solver de balances. `SolverHeatExchanger` conserva
su responsabilidad sobre presion, masa, composicion y energia. El nuevo backend de
diseno calcula configuraciones, coeficientes, area, caidas de presion y restricciones
de diseno; luego el diseno aplicado puede alimentar variables del equipo, como
`DeltaPHot`, `DeltaPCold` y resumen de dimensiones.

## Integracion Actual

Puntos existentes relevantes:

- `Shared/SolverConsecutive/Equipments/SolverHeatExchanger.cs`: facade/solver del
  equipo con `HotInlet`, `HotOutlet`, `ColdInlet`, `ColdOutlet`, `DeltaPHot`,
  `DeltaPCold` y `TransferHeat`.
- `Shared/ProcessFlowDiagram/HeatExchangers/HeatExchangerVisualElement.cs`: contrato
  visual y puertos tipados `TubeIn`, `TubeOut`, `ShellIn`, `CondensateOut`.
- `Client/Pages/UnitOperations/HeatExchangers/HeatExchangerDialog.razor`: dialogo
  actual del intercambiador.

La implementacion debe integrarse con esos contratos, no crear un segundo
intercambiador paralelo.

## Capas Propuestas

La separacion inicial debe ser:

- modelos compartidos de diseno: configuracion, restricciones, resultados y
  diagnosticos;
- servicios puros de calculo: Kern, geometria, termico, hidraulico y restricciones;
- servicio de aplicacion del diseno: toma un resultado valido y actualiza la intencion
  del equipo;
- persistencia: guarda lista de disenos e identificador aplicado;
- UI: muestra, edita y solicita recalculo.

Las reglas de calculo deben vivir fuera de componentes `.razor`.

## Fase 1 - Modelo De Dominio

Definir modelos de entrada y salida para diseno de tubo y coraza:

- datos de corrientes por lado;
- propiedades termofisicas requeridas;
- configuracion geometrica;
- restricciones;
- resultados termicos;
- resultados hidraulicos;
- diagnosticos;
- estado global del diseno.

Estos modelos deben representar intencion y resultados de forma explicita. No deben
depender de componentes Blazor ni de clases de persistencia.

Modelos minimos:

- `ShellAndTubeDesignInput`: snapshot de corrientes, configuracion y restricciones.
- `ShellAndTubeDesignConfiguration`: variables fijadas o propuestas.
- `ShellAndTubeDesignConstraints`: limites y preferencias.
- `ShellAndTubeDesignResult`: resultados calculados, diagnosticos y estado global.
- `ShellAndTubeDesignCandidate`: configuracion persistible mas metadatos.
- `AppliedHeatExchangerDesign`: referencia al candidato aplicado.

El input debe ser un snapshot. No debe contener referencias vivas a streams que el
servicio pueda mutar.

## Fase 1.1 - Extraccion De Datos Desde El Equipo

Crear un builder/adaptador que lea `HeatExchangerVisualElement` y
`SolverHeatExchanger` para construir el input de diseno.

Responsabilidades:

- mapear `TubeIn/TubeOut` al lado tubos;
- mapear `ShellIn/CondensateOut` al lado coraza;
- leer propiedades termofisicas disponibles;
- detectar datos faltantes;
- calcular o recuperar `Q` y `LMTD` cuando ya esten disponibles;
- clasificar servicio termico inicial.

Este adaptador es frontera entre el mundo mutable del flowsheet y el calculo puro.

## Fase 2 - Correlaciones Kern

Extraer las correlaciones de Kern como calculos aislados:

- factor `jH` lado tubos;
- factor `jH` lado coraza;
- factor de friccion lado tubos;
- factor de friccion lado coraza;
- coeficiente de condensacion en coraza;
- correlaciones especiales para agua, solucion acuosa y solucion organica.

Cada correlacion debe documentar:

- unidades internas esperadas;
- rango conocido o supuesto;
- origen desde el C++ viejo;
- comportamiento fuera de rango;
- tolerancia de pruebas.

Las correlaciones deben conservar sus unidades internas originales cuando eso reduzca
riesgo de error. La conversion a esas unidades debe ocurrir en un borde claro del
servicio.

Primera tanda:

- `JhFig24` desde `DIntercambiador.cpp`;
- `JhFig28` desde `DIntercambiador.cpp`;
- `FriccionFig26` desde `DIntercambiador.cpp`;
- `FriccionFig29` desde `DIntercambiador.cpp`.

La documentacion de cada correlacion debe indicar que viene de ajustes hechos sobre
graficas de Kern.

## Fase 3 - Geometria

Crear servicios para calculos geometricos:

- diametro interno y externo de tubos;
- espesor por BWG o schedule;
- area de flujo por tubo;
- superficie por tubo;
- area instalada;
- pitch y claro entre tubos;
- numero requerido de tubos;
- numero real de tubos;
- diametro de coraza;
- espaciamiento de deflectores;
- diametro equivalente de coraza.

La seleccion tabulada equivalente a `BuscarIDCoraza` debe tratarse como un catalogo o
selector de geometria, no como una formula escondida dentro del orquestador.

Primer enfoque de bajo riesgo:

- portar la logica geometrica que no depende de base de datos externa;
- identificar de donde sale la tabla de `BuscarIDCoraza`;
- si la tabla existe en el proyecto viejo o en datos actuales, convertirla a catalogo;
- si no existe aun, encapsular la seleccion detras de una interfaz y comenzar con un
  conjunto pequeno de geometria validada.

## Fase 4 - Calculo Termico

Implementar calculos termicos puros:

- calor transferido;
- LMTD recibido desde el equipo o validado;
- area requerida;
- `Ud` inicial recomendado;
- coeficientes lado tubos y lado coraza;
- `Uc`;
- `Ud` calculado;
- factor de ensuciamiento;
- area requerida por `Rd` permitido.

El calculo debe devolver diagnosticos si falta alguna propiedad de corriente.

Separar dos tipos de resultado:

- resultados derivados de la configuracion actual;
- recomendaciones del modo automatico.

El modo manual no debe cambiar la configuracion para corregir un fallo. Solo debe
reportar el fallo.

## Fase 5 - Calculo Hidraulico

Implementar calculos hidraulicos:

- velocidad en tubos;
- velocidad en coraza;
- Reynolds;
- caida de presion en tubos;
- caida de presion de retorno;
- caida de presion en coraza.

Para el primer alcance, evitar mezclar hidraulica especial de rehervidor con el caso
general de tubo y coraza.

La caida de presion aplicada al solver debe pasar por una decision explicita:

- lado coraza alimenta `DeltaPHot`;
- lado tubos alimenta `DeltaPCold`;
- solo el diseno aplicado puede actualizar esas variables;
- candidatos no aplicados no deben afectar el solver.

## Fase 6 - Evaluador De Restricciones

Crear un evaluador que tome resultados y restricciones, y devuelva una lista clara de
cumplimientos.

Cada restriccion debe incluir:

- identificador estable;
- valor calculado;
- limite;
- unidad;
- estado;
- mensaje de usuario;
- severidad.

Estados recomendados:

- `Pass`;
- `Warning`;
- `Fail`;
- `NotEvaluated`;
- `OutOfRange`.

Los mensajes visibles deben estar en ingles. Los comentarios internos del codigo, si
se agregan, deben estar en espanol.

## Fase 7 - Orquestador De Diseno

Crear un orquestador con dos responsabilidades de alto nivel:

- generar candidatos automaticos;
- evaluar una configuracion manual.

El modo automatico puede iterar sobre configuraciones posibles. El modo manual debe
respetar lo que el usuario fijo y solo recalcular lo derivado.

El orquestador no debe contener detalles de UI ni persistencia.

Flujo automatico minimo:

1. construir configuracion semilla desde recomendaciones Kern;
2. calcular area requerida;
3. seleccionar geometria inicial;
4. evaluar resultados termicos e hidraulicos;
5. evaluar restricciones;
6. ajustar numero de pasos, tubos o coraza dentro de limites simples;
7. devolver el mejor candidato encontrado y diagnosticos.

Flujo manual minimo:

1. recibir configuracion fijada;
2. calcular derivados;
3. evaluar restricciones;
4. devolver resultado sin cambiar la intencion manual.

No introducir optimizacion compleja hasta tener pruebas de caracterizacion.

## Fase 8 - Persistencia

Persistir por intercambiador:

- lista de disenos candidatos;
- configuracion de cada diseno;
- restricciones fijadas;
- identificador del diseno aplicado;
- metadatos ligeros de autoria y fecha cuando aplique.

No persistir resultados calculados como verdad principal. Se pueden cachear para UI si
se invalidan y recalculan al cargar.

Opciones de ubicacion:

- si el proyecto ya persiste estado de visual elements, agregar la lista al estado del
  `HeatExchangerVisualElement`;
- si existe un DTO generico de persistencia de facade, guardar configuracion como
  intencion extendida del equipo;
- evitar migraciones de base de datos por cada variable de diseno.

El diseno aplicado debe persistirse por identificador estable del candidato, no por
indice de lista.

## Fase 9 - UI

Integrar en el dialogo del intercambiador:

- boton `Create Design`;
- lista de disenos;
- detalle editable del diseno seleccionado;
- comparacion de resultados;
- indicadores de restricciones;
- accion `Apply Design`;
- SVG tecnico dinamico.

Cada cambio manual debe disparar reevaluacion del diseno seleccionado.

Pantallas/zonas sugeridas:

- lista compacta de disenos candidatos;
- boton `Create Design`;
- accion `Apply Design`;
- editor de configuracion;
- resultados principales;
- tabla de restricciones;
- SVG tecnico dinamico.

El componente SVG debe recibir un modelo visual derivado. No debe leer directamente
`SolverHeatExchanger` ni ejecutar formulas.

La primera UI puede ser funcional y sobria. Comparacion avanzada de muchos candidatos
puede quedar para una iteracion posterior.

## Fase 10 - Verificacion

Verificar con pruebas enfocadas:

- correlaciones Kern contra valores del C++ viejo;
- geometria y seleccion de tubos/coraza;
- coeficientes `h_i`, `h_o`, `Uc`, `Ud`, `Rd`;
- caidas de presion;
- evaluacion de restricciones;
- recarga de proyecto con recalculo de resultados;
- UI manual: cambiar una variable recalcula y actualiza estados.

Tambien verificar:

- candidatos no aplicados no cambian `DeltaPHot` ni `DeltaPCold`;
- aplicar diseno actualiza solo la intencion esperada del intercambiador;
- al cargar proyecto se recalculan resultados;
- datos insuficientes generan diagnostico, no excepcion inesperada.

## Fase 11 - Caracterizacion Del C++ Viejo

Antes de portar formulas grandes, construir casos de referencia desde
`DIntercambiador.cpp`.

Casos iniciales:

- una configuracion liquido-liquido con datos completos;
- una configuracion con vapor condensando en coraza;
- una configuracion manual invalida por baja velocidad en tubos;
- una configuracion invalida por coraza insuficiente;
- una configuracion con LMTD invalido.

Cada caso debe registrar inputs, unidades y outputs esperados. La meta no es validar
que el C++ viejo sea perfecto, sino tener una base para detectar cambios accidentales.

## Orden Recomendado

1. Crear casos de caracterizacion desde C++ viejo y Kern.
2. Crear modelos de entrada/salida.
3. Crear builder de input desde `SolverHeatExchanger`.
4. Portar correlaciones Kern aisladas.
5. Implementar geometria base.
6. Implementar calculo termico del caso tubo y coraza.
7. Implementar calculo hidraulico del caso tubo y coraza.
8. Implementar evaluador de restricciones.
9. Implementar evaluacion manual.
10. Implementar generacion automatica simple.
11. Persistir lista de disenos y diseno aplicado.
12. Integrar UI basica.
13. Integrar SVG dinamico.
14. Ampliar comparacion y calidad visual.

## Criterio Para Pasar A Codigo

Antes de implementar se debe tener:

- alcance confirmado: tubo y coraza normal;
- nombres de modelos acordados o aceptados;
- ubicacion de carpetas definida;
- decision de persistencia para lista de disenos;
- al menos un caso numerico de referencia;
- confirmacion de que la app esta apagada.
