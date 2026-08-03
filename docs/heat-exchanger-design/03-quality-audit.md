# 03 - Auditoria De Calidad De Diseno De Intercambiador

Estado: Borrador

## Objetivo

Esta auditoria define como revisar que el modulo de diseno de intercambiador cumple
con los principios de Distillator: SOLID, KISS, DRY, YAGNI, calculo determinista,
persistencia de intencion y experiencia interactiva confiable.

## SOLID

Verificar:

- las correlaciones de Kern no dependen de UI;
- el orquestador no contiene detalles de SVG ni persistencia;
- la persistencia no ejecuta formulas;
- la UI no calcula propiedades ni coeficientes;
- las restricciones se evaluan en un componente separado;
- la geometria se calcula o selecciona fuera del calculo termico;
- cada servicio tiene una responsabilidad clara y pequena.

Senales de alerta:

- una clase que calcula todo;
- metodos que mutan varias corrientes directamente;
- formulas duplicadas entre UI y dominio;
- estados globales o flags ocultos que cambian el resultado.

## KISS

Verificar:

- el flujo principal puede explicarse de entrada a resultado sin saltos ocultos;
- las funciones tienen nombres fisicos claros;
- las unidades internas de cada correlacion estan documentadas;
- los modos automatico y manual estan separados;
- los errores esperados vuelven como diagnosticos.

Evitar:

- frameworks internos innecesarios;
- jerarquias profundas;
- abstracciones creadas antes de tener variantes reales;
- configuraciones magicas sin origen documentado.

## YAGNI

Alcance inicial:

- tubo y coraza normal;
- diseno candidato;
- evaluacion manual;
- restricciones principales;
- persistencia de lista de disenos;
- SVG tecnico basico pero dinamico.

Fuera del primer alcance salvo decision explicita:

- rehervidor;
- pelicula;
- circulacion forzada;
- optimizacion multiobjetivo avanzada;
- Bell-Delaware;
- reportes formales de diseno;
- exportacion CAD.

## DRY

Verificar:

- cada correlacion Kern vive en un unico lugar;
- conversiones de unidad no se repiten en componentes;
- el calculo automatico y manual comparten el evaluador base;
- la UI usa el mismo resultado que las pruebas;
- la seleccion de geometria no se replica en varios dialogos.

## Determinismo Y Recalculo

Cada calculo debe cumplir:

- mismos inputs producen mismos outputs;
- no depende de resultados viejos guardados;
- no usa estado mutable compartido entre ejecuciones;
- recalcula todo lo derivado cuando cambia una variable;
- tolera datos incompletos con diagnosticos, no con excepciones inesperadas.

Escenarios obligatorios:

- cambiar numero de tubos recalcula area, velocidad, Reynolds, coeficientes y
  restricciones;
- cambiar longitud recalcula superficie, area instalada, caidas de presion y SVG;
- cambiar pitch recalcula claro, area de coraza, diametro equivalente y restricciones;
- cambiar `Ud` supuesto recalcula area requerida y seleccion automatica;
- cargar proyecto recalcula resultados desde disenos persistidos.

## Persistencia

Debe persistirse:

- configuracion del diseno;
- restricciones definidas;
- lista de disenos;
- diseno aplicado;
- auditoria ligera de intencion de usuario cuando aplique.

No debe persistirse como verdad principal:

- coeficientes calculados;
- velocidades;
- Reynolds;
- caidas de presion;
- area requerida;
- estado de cumplimiento.

Estos resultados deben recalcularse al cargar o al cambiar entradas.

## Pruebas

Pruebas minimas:

- correlaciones `jH` y friccion con valores de referencia del C++ viejo;
- calculo de area requerida;
- calculo de numero requerido de tubos;
- calculo de area instalada;
- calculo de velocidad en tubos;
- calculo de coeficientes lado tubos y coraza;
- calculo de `Uc`, `Ud` y `Rd`;
- caida de presion de tubos y coraza;
- evaluacion de restricciones;
- candidato automatico valido para un caso base;
- candidato manual invalido con diagnostico correcto.

Las pruebas numericas deben usar tolerancias explicitas.

## UI Y UX

Verificar:

- `Create Design` genera o diagnostica sin bloquear la experiencia;
- la lista de disenos conserva seleccion y aplicado;
- editar un parametro recalcula todo;
- las restricciones fallidas son visibles y comprensibles;
- el usuario puede aplicar un diseno con advertencias solo de forma consciente;
- el SVG refleja cambios de dimensiones sin ser la fuente de calculo;
- los textos visibles estan en ingles.

## Criterio De Aceptacion

El modulo pasa la auditoria cuando:

- el caso tubo y coraza puede crear, editar, recalcular, comparar y aplicar disenos;
- las formulas clave estan aisladas y probadas;
- la persistencia guarda intencion y recalcula resultados;
- la UI no contiene formulas de Kern;
- cada restriccion importante reporta estado claro;
- los resultados coinciden con el C++ viejo dentro de tolerancias acordadas o queda
  documentada una diferencia intencional.

