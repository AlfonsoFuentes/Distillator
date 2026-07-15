# 04 - Edicion De Flowsheet Y Topologia Local

Estado: Borrador

## Contexto

Un flowsheet contiene equipos, referencias visuales, pipes y conexiones logicas con el
solver. Las acciones del canvas y de los dialogos deben producir el mismo modelo
consistente independientemente del punto de entrada de la UI.

## Tipos De Cambio

### Visual

- seleccionar;
- mover;
- rotar;
- invertir;
- pan, zoom y dimensiones;
- z-index y visibilidad de etiqueta.

No requiere solver.

### Topologico

- crear o borrar equipo;
- crear stream;
- conectar o desconectar puerto;
- crear o borrar pipe;
- agregar o retirar puertos dinamicos.

Requiere actualizar dominio, registry y solver; normalmente requiere recalculo.

## Comportamiento Deseado

Cada comando de edicion es una operacion de dominio atomica. Si una validacion o paso
obligatorio falla, no deja artefactos parciales en puertos, pipes, registry o solver.

## Creacion De Equipo

1. Validar permiso, tipo de diagrama y tipo permitido.
2. Crear el elemento mediante la fabrica del tipo de flowsheet.
3. Asignar nombre unico segun configuracion.
4. Resolver posicion y limites del canvas.
5. Registrar elemento y facade una sola vez.
6. Crear referencia visual.
7. Solicitar simulacion solo si el cambio afecta el modelo calculable.
8. Marcar el diagrama para autosave.

## Conexion Local

1. Validar existencia y disponibilidad de ambos puertos.
2. Validar direccion, reglas del flowsheet y compatibilidad.
3. Determinar si se necesita stream intermedio.
4. Crear todos los objetos necesarios en memoria aislada o con rollback definido.
5. Conectar ambos extremos logicos.
6. Registrar stream nuevo en registry y solver, si aplica.
7. Crear pipes visuales.
8. Confirmar la operacion y solicitar una sola simulacion.
9. Guardar el diagrama resultante.

## Desconexion Y Eliminacion

- Desconectar un puerto libera ambos extremos.
- Retirar un pipe actualiza tambien la conexion del facade.
- Un stream sin conexiones se retira o conserva segun la accion explicita que lo
  origino; la regla no se infiere accidentalmente.
- Borrar un equipo desconecta primero todos sus pipes.
- Despues se retira de referencia visual, registry y solver.
- Una eliminacion produce una sola revision logica aunque tenga varios pasos internos.

## Puertos Dinamicos

- Mixer, splitter, vessel y column pueden modificar grupos de puertos.
- La coleccion visible y el facade deben mantenerse sincronizados.
- Un puerto conectado no puede desaparecer sin desconectar correctamente su extremo.
- `OnConnChanged` refresca topologia dinamica; no duplica simulacion ni persistencia.
- Los puertos fijos se consumen mediante propiedades tipadas.

## Invariantes

1. Cada elemento del flowsheet existe en el registry del proyecto.
2. Cada pipe conecta dos elementos y dos puertos existentes.
3. Un puerto tiene como maximo una conexion cuando su contrato asi lo exige.
4. Registry, referencias, pipes y solver describen la misma topologia.
5. Una accion topologica solicita como maximo una simulacion logica.
6. Un cambio visual nunca ejecuta solver.
7. Una operacion fallida no deja streams, pipes o puertos huerfanos.
8. La UI no implementa reglas topologicas propias.
9. Viewer no inicia comandos de edicion.
10. Todo cambio aceptado queda marcado para persistencia.

## Criterios De Aceptacion

1. Crear cada equipo permitido produce registry, referencia y solver consistentes.
2. Mover, rotar, invertir, pan y zoom guardan sin ejecutar solver.
3. Conectar equipo-stream produce exactamente un pipe y una conexion logica.
4. Conectar equipo-equipo crea el stream intermedio correcto sin duplicados.
5. Desconectar libera ambos puertos y limpia resultados huerfanos necesarios.
6. Borrar un equipo conectado no deja referencias en pipes ni solver.
7. Una conexion invalida no modifica ningun estado.
8. Puertos dinamicos mantienen nombres, orden y conexiones al cambiar cantidad.
9. Guardar y recargar reproduce la topologia aceptada.

## Pruebas Requeridas

- Crear, mover, rotar, invertir y borrar.
- Pan, zoom, zoom-to-fit y cambio de dimensiones.
- Conexion directa, stream intermedio y espacio vacio.
- Puertos ocupados, misma direccion y tipo incompatible.
- Desconexion desde canvas y desde dialogo.
- Mixer y splitter con multiples puertos.
- Vessel y column con puertos dinamicos.
- Fallo durante una operacion compuesta y rollback.
- Recarga posterior de cada escenario.

## Objetivos De Refactor Posteriores

- `FlowsheetManager`.
- `ConnectionService`.
- `EquipmentPortConnector`.
- Fabrica y registry de equipos.
- Wrappers y canvas que disparan comandos.

## Fuera De Alcance

- Edicion P&ID o electrica aun no implementada.
- Undo/redo hasta definir una spec propia.
- Enrutamiento visual avanzado de pipes.

