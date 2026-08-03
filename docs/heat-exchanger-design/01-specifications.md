# 01 - Especificaciones De Diseno De Intercambiador

Estado: Borrador

## Contexto

El intercambiador de tubo y coraza de Distillator debe evolucionar desde el
conocimiento del programa C++ viejo, especialmente `DIntercambiador.cpp`, donde varias
graficas y tablas del libro de Kern fueron transformadas en ecuaciones.

El objetivo no es copiar la implementacion antigua, sino conservar su conocimiento de
ingenieria y expresarlo como funcionalidad clara, recalculable y mantenible dentro de
Distillator.

El primer alcance funcional es el diseno de intercambiador de tubo y coraza normal. El
C++ viejo tambien contiene conocimiento para rehervidor, circulacion forzada y pelicula,
pero esos casos no deben mezclarse con el primer contrato salvo que una decision de
alcance los active.

## Fuentes De Conocimiento

Las especificaciones se apoyan en tres fuentes:

- el flujo deseado por el usuario en Distillator;
- las ecuaciones y decisiones de `DIntercambiador.cpp`;
- el libro de Kern, especialmente las secciones de intercambiadores tubulares,
  transferencia lado tubos, transferencia lado coraza, LMTD, condensacion y caidas de
  presion.

Mapa inicial entre el C++ viejo y Kern:

| Funcion vieja | Rol fisico | Referencia conceptual |
|---|---|---|
| `JhFig24` | factor `jH` para transferencia en tubos | grafica de Kern para lado tubos |
| `FriccionFig26` | factor de friccion para tubos | grafica de Kern para caida de presion en tubos |
| `JhFig28` | factor `jH` para lado coraza | grafica de Kern para flujo lado coraza |
| `FriccionFig29` | factor de friccion para coraza | grafica de Kern para caida de presion en coraza |
| `CalcularCoeficienteCondensacion` | coeficiente de condensacion en coraza | metodologia de condensacion de vapores |
| `CalculoNoTubosReales` | seleccion de coraza y tubos reales | tablas/catalogos geometricos |
| `CalcularDeltaPTubosAgua` | caida de presion lado tubos | ecuaciones de presion de Kern |
| `CalcularDeltaPCorazaLiquido` | caida de presion lado coraza | ecuaciones de presion de Kern |

## Experiencia Principal

El usuario abre el dialogo del intercambiador y presiona `Create Design`.

Distillator toma las corrientes conectadas a los puertos, sus propiedades calculadas,
el servicio termico, las restricciones disponibles y las recomendaciones de Kern para
proponer una configuracion inicial valida o diagnosticar por que no puede hacerlo.

El usuario puede:

- crear varios disenos candidatos;
- comparar disenos;
- editar manualmente un diseno;
- recalcular el diseno despues de cada cambio;
- aplicar el diseno que prefiera al intercambiador;
- conservar la lista de disenos en el proyecto.

La experiencia debe sentirse asistida: el sistema propone, explica y valida. El usuario
mantiene control final sobre que diseno se aplica.

## Flujo `Create Design`

Al presionar `Create Design`, Distillator debe:

1. leer el estado actual de los puertos del intercambiador;
2. verificar que existan corrientes suficientes en lado tubos y lado coraza;
3. solicitar o usar propiedades ya calculadas de las corrientes;
4. clasificar el servicio termico;
5. validar LMTD y temperaturas cruzadas;
6. escoger un `Ud` inicial recomendado por Kern segun servicio y fluidos;
7. generar una configuracion geometrica inicial;
8. iterar configuraciones hasta encontrar una candidata que cumpla restricciones;
9. devolver el mejor candidato o un diagnostico claro;
10. agregar el candidato a la lista persistente de disenos.

La accion no aplica automaticamente el diseno al intercambiador, salvo que exista una
decision explicita de UI para hacerlo.

## Entradas Del Diseno

El backend debe construir entradas limpias a partir del estado del equipo:

- corrientes de entrada y salida del lado tubos;
- corrientes de entrada y salida del lado coraza;
- fases y tipo de servicio;
- flujo masico, flujo volumetrico y entalpia;
- temperatura, presion y composicion;
- densidad, viscosidad, Cp y conductividad termica;
- calor transferido;
- LMTD calculado por el equipo;
- restricciones definidas por el usuario;
- configuracion manual fijada por el usuario, si existe.

La UI no debe calcular estas propiedades por su cuenta.

## Servicio Termico

El backend debe clasificar el servicio antes de escoger correlaciones y valores
iniciales.

Clasificaciones iniciales:

- liquido en tubos contra liquido en coraza;
- vapor o mezcla condensando en coraza;
- vapor o mezcla condensando en tubos;
- vapor sobrecalentado sensible;
- corriente acuosa;
- corriente organica;
- agua pura o casi pura.

La clasificacion debe quedar disponible como diagnostico para que la UI pueda mostrar
por que se escogio determinada recomendacion.

## Variables Editables

El usuario debe poder editar, cuando aplique:

- nombre del diseno;
- diametro nominal de tubos;
- BWG o schedule;
- longitud de tubos;
- pitch;
- numero de pasos por tubos;
- numero de pasos por coraza;
- numero de tubos;
- diametro interno de coraza;
- espaciamiento de deflectores;
- orientacion;
- `Ud` supuesto;
- `Rd` permitido;
- caida de presion maxima en tubos;
- caida de presion maxima en coraza;
- velocidad minima en tubos;
- velocidad maxima en tubos.

Cada valor fijado manualmente debe conservarse como intencion del usuario. El
recalculo puede marcarlo invalido, pero no debe reemplazarlo silenciosamente.

## Disenos Candidatos

Cada intercambiador puede tener una lista persistente de disenos candidatos.

Cada diseno debe conservar principalmente intencion y configuracion:

- nombre editable;
- tipo de creacion: automatico o manual;
- diametro nominal de tubos;
- BWG o schedule;
- diametro interno y externo derivado;
- longitud de tubos;
- pitch;
- numero de pasos por tubos;
- numero de pasos por coraza;
- numero de tubos;
- diametro interno de coraza;
- espaciamiento de deflectores;
- orientacion;
- restricciones fijadas por el usuario;
- indicador de si esta aplicado al intercambiador.

Los resultados calculados no deben ser la verdad principal persistida. Al cargar el
proyecto, los resultados deben recalcularse desde configuracion, corrientes y
restricciones.

Cada diseno candidato debe poder existir aunque no sea valido. Esto permite que el
usuario vea una configuracion fallida, entienda por que falla y la corrija manualmente.

## Diseno Aplicado

Debe distinguirse entre:

- diseno seleccionado en la UI;
- diseno candidato guardado;
- diseno aplicado al intercambiador.

Solo el diseno aplicado alimenta el estado operativo del intercambiador, por ejemplo
dimensiones visibles, caidas de presion usadas por el equipo y resumen de resultados.

El usuario puede inspeccionar o editar candidatos sin cambiar el diseno aplicado hasta
confirmar `Apply Design`.

## Modo Automatico

En modo automatico, Distillator debe buscar una configuracion candidata segun:

- tipo de servicio termico;
- `Ud` inicial recomendado por Kern;
- area requerida;
- geometria comercial disponible;
- velocidades minimas o maximas;
- caida de presion permitida;
- factor de ensuciamiento requerido o permitido;
- margen entre area instalada y area requerida;
- restricciones manuales ya fijadas.

El resultado ideal es una propuesta valida. Si existen varias configuraciones validas,
el sistema puede conservar varios candidatos ordenados por criterio practico.

El criterio inicial de mejor diseno debe favorecer:

- restricciones principales cumplidas;
- area instalada suficiente con sobredimensionamiento razonable;
- caidas de presion dentro del limite;
- velocidades dentro del rango practico;
- geometria comercial disponible;
- menor numero de advertencias.

No se requiere una optimizacion multiobjetivo avanzada en el primer alcance.

## Modo Manual

En modo manual, el usuario fija una o mas variables del diseno.

Distillator debe respetar esas decisiones, recalcular todo lo derivado y mostrar si la
configuracion cumple o no cumple. El sistema no debe reemplazar silenciosamente una
decision manual por una recomendacion automatica.

El modo manual y el modo automatico deben compartir los mismos calculos base y el mismo
evaluador de restricciones. La diferencia es la intencion: automatico explora,
manual evalua una configuracion fijada.

## Resultados Esperados

Un calculo de diseno debe devolver, cuando existan datos suficientes:

- calor transferido;
- LMTD;
- area requerida;
- area instalada;
- superficie por tubo;
- area de flujo por tubo;
- area de flujo real en tubos;
- area de flujo en coraza;
- diametro equivalente de coraza;
- velocidad en tubos;
- velocidad en coraza;
- Reynolds de tubos;
- Reynolds de coraza;
- coeficiente lado tubos;
- coeficiente lado coraza;
- coeficiente limpio `Uc`;
- `Ud` calculado;
- factor de ensuciamiento calculado;
- caida de presion en tubos;
- caida de presion en coraza;
- advertencias y diagnosticos.

Los resultados deben indicar tambien si fueron calculados, asumidos, fijados por el
usuario o no disponibles.

## Restricciones

El evaluador debe informar el estado de cada restriccion:

- cumple;
- no cumple;
- cerca del limite;
- no evaluable por datos insuficientes;
- fuera del dominio de la correlacion.

Restricciones iniciales:

- area instalada mayor o igual al area requerida;
- velocidad minima en tubos;
- velocidad maxima opcional en tubos;
- caida de presion maxima en tubos;
- caida de presion maxima en coraza;
- factor de ensuciamiento disponible;
- numero de tubos compatible con la coraza;
- geometria comercial valida;
- LMTD positivo y sin temperaturas cruzadas.

Las restricciones deben ser visibles individualmente. Un estado global `Invalid` no es
suficiente; el usuario necesita saber que variable debe corregir.

## Recalculo

Cada cambio de una variable editable debe recalcular el diseno completo desde entradas,
configuracion y restricciones.

Reglas:

- no depender de resultados calculados anteriormente como entrada implicita;
- no mutar corrientes desde el servicio de diseno;
- no recalcular en la UI con formulas duplicadas;
- devolver diagnosticos si faltan propiedades de corriente;
- mantener la seleccion y el diseno aplicado aunque el candidato editado falle;
- invalidar resultados cuando cambian corrientes, metodo termodinamico o conexiones.

Ejemplos:

- cambiar longitud de tubos recalcula superficie, area instalada, `Ud`, caidas de
  presion y SVG;
- cambiar numero de pasos recalcula area de flujo, velocidad, Reynolds y caida de
  presion;
- cambiar pitch recalcula claro entre tubos, area de flujo de coraza, diametro
  equivalente y restricciones geometricas;
- cambiar `Rd` permitido recalcula area requerida y cumplimiento de ensuciamiento.

## Visualizacion SVG

El dialogo debe incluir una visualizacion SVG tecnica y dinamica del intercambiador.

La imagen debe responder a variables del diseno:

- longitud de tubos;
- diametro de coraza;
- numero de tubos;
- numero de pasos;
- pitch;
- espaciamiento de deflectores;
- orientacion;
- boquillas de tubos y coraza.

El SVG no debe calcular. Debe renderizar un modelo visual derivado de los resultados
del backend.

Elementos visuales minimos:

- envolvente de coraza;
- haz de tubos representativo;
- cabezales;
- boquillas lado tubos;
- boquillas lado coraza;
- deflectores;
- flechas de flujo;
- etiquetas compactas de dimensiones principales.

La escala puede ser normalizada para visualizacion. No necesita ser un plano mecanico,
pero si debe comunicar aumento o disminucion relativa de dimensiones.

## Estados Del Diseno

Un diseno debe exponer un estado global:

- `Valid`: calculo completo y restricciones principales cumplidas.
- `Warning`: calculo completo con advertencias o margenes pobres.
- `Invalid`: calculo completo con restricciones fallidas.
- `Incomplete`: faltan entradas o propiedades necesarias.
- `Failed`: no convergio o salio del dominio de las correlaciones.

## Mensajes Y Diagnosticos

Los mensajes visibles al usuario deben estar en ingles.

Diagnosticos iniciales:

- faltan corrientes conectadas;
- falta una propiedad termofisica requerida;
- LMTD invalido;
- temperaturas cruzadas;
- no se encontro geometria comercial compatible;
- numero de tubos insuficiente para el area requerida;
- diametro de coraza no contiene los tubos escogidos;
- velocidad en tubos menor que el minimo;
- caida de presion en tubos excede el limite;
- caida de presion en coraza excede el limite;
- factor de ensuciamiento disponible menor que el requerido;
- correlacion fuera de rango conocido.

## Persistencia Y Realtime

La lista de disenos debe persistir con el proyecto y sincronizarse con la experiencia
multiusuario.

Debe persistirse:

- configuracion de cada diseno;
- restricciones fijadas;
- identificador del diseno aplicado;
- autoria y fecha de cambios manuales cuando aplique.

No debe persistirse como verdad principal:

- coeficientes calculados;
- velocidades;
- Reynolds;
- areas calculadas;
- caidas de presion;
- estado de cumplimiento.

Al cargar el proyecto, Distillator debe recalcular resultados de los disenos desde la
intencion persistida y el estado actual de las corrientes.

## Limites Del Primer Alcance

Incluido:

- tubo y coraza normal;
- lista de disenos candidatos;
- diseno aplicado;
- modo automatico inicial;
- modo manual con recalculo completo;
- restricciones principales;
- SVG tecnico dinamico;
- trazabilidad conceptual a Kern y al C++ viejo.

Excluido por ahora:

- rehervidor;
- circulacion forzada;
- evaporador de pelicula;
- reportes mecanicos completos;
- diseno de boquillas avanzado;
- optimizacion economica;
- exportacion CAD.
