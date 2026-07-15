# 05 - Conexiones Interdiagrama

Estado: Borrador

## Contexto

Una conexion entre diagramas enlaza un equipo de un flowsheet con un stream ubicado en
otro. Se representa visualmente mediante dos OPC y dos pipes, pero para el solver debe
comportarse como una conexion logica unica.

## Problema Actual

- La operacion crea artefactos en dos diagramas y varios registros en memoria.
- Algunos pasos conectan directamente facades y otros manipulan puertos visuales.
- Un fallo intermedio puede dejar solo un extremo.
- Borrar o desconectar debe localizar y limpiar artefactos distribuidos.
- La persistencia requiere guardar ambos diagramas como una sola intencion.

## Comportamiento Deseado

Crear, eliminar o restaurar una conexion interdiagrama es una operacion atomica sobre
el proyecto y sus dos diagramas afectados.

## Artefactos De Una Conexion

- conexion logica identificada;
- OPC local;
- OPC remoto;
- referencia visual de cada OPC;
- pipe local;
- pipe remoto;
- ocupacion de puertos en ambos extremos;
- conexion facade-stream usada por el solver.

Todos pertenecen al mismo ciclo de vida.

## Creacion

1. Validar permiso y que los diagramas pertenezcan al mismo proyecto.
2. Validar puertos, direccion y disponibilidad del stream remoto.
3. Crear una identidad de conexion.
4. Crear ambos OPC con referencias reciprocas.
5. Crear ambos pipes y ocupar puertos.
6. Crear una sola conexion logica para el solver.
7. Validar la integridad de ambos extremos.
8. Solicitar una simulacion.
9. Persistir ambos diagramas atomically bajo una version del proyecto.
10. Publicar realtime despues del guardado.

## Eliminacion

1. Identificar la conexion por cualquiera de sus extremos.
2. Desconectar el facade y limpiar resultados huerfanos necesarios.
3. Liberar puertos finales y puertos `Transfer`.
4. Retirar pipes, referencias y ambos OPC.
5. Retirar ambos OPC del registry y solver si estuvieran registrados.
6. Retirar la conexion logica.
7. Simular y persistir ambos diagramas sobrevivientes.

## Eliminacion De Diagrama

Antes de borrar un diagrama:

- identificar todas sus conexiones interdiagrama;
- ejecutar la eliminacion completa de cada conexion;
- persistir los diagramas sobrevivientes afectados;
- borrar el diagrama solamente cuando la limpieza sea reconstruible.

## Hidratacion

- Cada pareja reciproca de OPC produce una sola conexion logica.
- La restauracion es independiente del orden de los diagramas.
- Un extremo faltante se diagnostica y no crea una conexion parcial de solver.
- La conexion facade-stream se restaura despues de equipos, streams, pipes y puertos.
- La identidad persistida debe permitir distinguir conexiones multiples entre los
  mismos diagramas.

## Invariantes

1. Una conexion valida tiene exactamente dos OPC reciprocos.
2. Ambos OPC pertenecen a diagramas diferentes del mismo proyecto.
3. Ambos extremos se guardan bajo la misma revision logica.
4. No existe un OPC conectado cuyo gemelo no pueda resolverse.
5. El solver observa una sola conexion de proceso, no dos.
6. Borrar cualquier extremo elimina el ciclo completo o rechaza la operacion.
7. Una recarga no duplica conexiones logicas.
8. Un fallo de guardado no publica SignalR.

## Criterios De Aceptacion

1. Crear A -> B muestra OPC correctos en ambos diagramas.
2. El solver utiliza el stream remoto como conexion del equipo local.
3. Recargar reconstruye ambos extremos sin F5 adicional.
4. Desconectar desde A limpia A y B.
5. Desconectar desde B produce el mismo resultado.
6. Borrar A limpia los artefactos que permanecian en B.
7. Un fallo antes de confirmar no deja un unico OPC persistido.
8. Dos conexiones entre A y B conservan identidades independientes.
9. El autosave envia ambos diagramas en una operacion coordinada.

## Pruebas Requeridas

- Salida local hacia stream remoto.
- Entrada local desde stream remoto.
- Multiples conexiones entre dos diagramas.
- Diagramas seleccionados en orden inverso durante hidratacion.
- Gemelo, pipe o puerto faltante.
- Desconexion desde cada extremo.
- Eliminacion de cada diagrama participante.
- Fallo de persistencia del lote.
- Dos usuarios observando los diagramas distintos.

## Objetivos De Refactor Posteriores

- `InterFlowsheetConnectionService`.
- Restauracion dentro de `ProjectSessionService`.
- Eliminacion de flowsheet en `Project` y sesion.
- Persistencia por lote de diagramas.

## Fuera De Alcance

- Conexiones entre proyectos diferentes.
- Sincronizacion parcial de un solo OPC.
- Representar la conexion como dos streams fisicos independientes.

