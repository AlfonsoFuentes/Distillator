# 09 - Configuracion, Unidades Y Naming

Estado: Borrador

## Contexto

La configuracion de proyecto incluye metodo termodinamico, elevacion, sistemas de
unidades, camara, naming, reportes y parametros de diseno. Algunos cambios solo afectan
presentacion; otros invalidan resultados y requieren recalculo.

## Propiedad De La Configuracion

- Existe una configuracion activa por proyecto.
- La edicion usa una copia temporal.
- Cancelar no muta el proyecto.
- Guardar valida la configuracion completa antes de aplicarla.
- La configuracion aceptada se aplica una vez y se persiste como una revision.

## Clasificacion De Cambios

| Cambio | Requiere solver | Requiere actualizar equipos |
|---|---:|---:|
| Metodo termodinamico | Si | Streams y solver |
| Elevacion | Si | Presion atmosferica del solver |
| Unidad interna fisica | Depende | Variables afectadas |
| Display unit por defecto | No | Presentacion sin override |
| Camara por defecto | No | Nuevos/reset de diagramas |
| Naming | No | Solo si se aprueba migracion |
| Reportes | No | Configuracion de exportacion |
| Diseno | Depende del consumidor | Equipos aplicables |

La clasificacion exacta de cada campo debe quedar codificada en un unico lugar.

## Unidades

- El valor fisico se conserva al cambiar display unit.
- Cada `Variable<T>` tiene unidad interna compatible con su magnitud.
- Un override individual prevalece sobre el default del proyecto.
- Cambiar default actualiza variables sin override.
- La persistencia guarda valor, unidad de valor, display unit y si existe override.
- Una unidad desconocida durante hidratacion produce fallback diagnosticado.
- Conversiones invalidas se rechazan antes de mutar.

## Metodo Termodinamico

1. Seleccionar un metodo disponible y completo.
2. Validar compatibilidad minima con componentes del proyecto.
3. Aplicarlo al proyecto, solver y todos los streams.
4. Invalidar resultados derivados necesarios.
5. Solicitar una simulacion coordinada.
6. Persistir identificador y configuracion de usuario, no resultados.

No puede configurarse un solver de DI distinto del solver real del proyecto.

## Elevacion Y Presion Atmosferica

- La elevacion se persiste con unidad explicita.
- La presion atmosferica se deriva de la elevacion mediante una unica regla.
- Cambiar elevacion actualiza el solver del proyecto activo.
- La presion derivada no se trata como input independiente salvo que una spec futura lo
  permita.

## Naming

- Los nombres son unicos dentro del alcance configurado.
- El generador usa tipo de equipo, proyecto, flowsheet y numero de diagrama segun modo.
- Un cambio de reglas no renombra existentes sin decision explicita.
- Si la nueva regla requiere numero de diagrama, todos los numeros se validan antes de
  aplicar.
- La migracion de nombres y numeros es atomica con la configuracion.
- Registry e indices de nombre se actualizan cuando cambia un nombre.

## Configuracion De Diagrama

- Nombre y numero se validan en el proyecto.
- Orden, dimensiones, escala, grid y camara se persisten.
- Reordenar diagramas produce persistencia sin ejecutar solver.
- El numero de diagrama puede ser opcional o requerido por naming.
- Los valores visuales fuera de rango se rechazan o normalizan explicitamente.

## Invariantes

1. Cancelar un dialogo de configuracion no cambia dominio.
2. Guardar aplica exactamente una configuracion completa.
3. Un cambio de display unit no altera el valor fisico.
4. Un override individual sobrevive a cambios de defaults.
5. El metodo termodinamico del proyecto, streams y solver es consistente.
6. Una migracion de naming es completa o no se aplica.
7. Nombres e identificadores fisicos no dependen de texto visual transitorio.
8. Cambios visuales de diagrama no ejecutan solver.
9. Una persistencia fallida deja configuracion `Dirty` o revierte de forma explicita.

## Criterios De Aceptacion

1. Editar y cancelar deja la configuracion original intacta.
2. Cambiar unidad conserva el mismo valor convertido.
3. Defaults y overrides se restauran al recargar.
4. Cambiar metodo actualiza todos los streams y ejecuta una simulacion.
5. Cambiar elevacion actualiza la presion atmosferica esperada.
6. Naming genera nombres unicos en cada alcance configurado.
7. Una migracion invalida no renombra parcialmente.
8. Reordenar, cambiar camara y dimensiones persiste sin solver.
9. Recargar reproduce configuracion y nombres.

## Pruebas Requeridas

- Cancelar y guardar cada grupo de configuracion.
- Conversiones de temperatura, presion, flujo y energia.
- Unidad incompatible y unidad persistida desconocida.
- Default sin override y con override.
- Cambio de metodo con varios streams.
- Elevacion cero, positiva y limite valido.
- Modos y alcances de naming.
- Numeros de diagrama faltantes y duplicados.
- Renombrar existentes aceptado y cancelado.
- Reordenar diagramas y recargar.

## Objetivos De Refactor Posteriores

- `ProjectFormDialog` y tabs.
- `ProjectConfiguration`, `UnitConfiguration` y applier.
- `EquipmentNamingService`.
- `ProjectSessionService` para configuracion y DTO mapping.
- Configuracion del solver por proyecto.

## Fuera De Alcance

- Crear un nuevo sistema de unidades.
- Renombrado automatico continuo de equipos existentes.
- Configuracion por usuario de valores compartidos del proyecto.

