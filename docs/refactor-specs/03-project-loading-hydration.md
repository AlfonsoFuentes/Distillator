# 03 - Carga E Hidratacion De Proyecto

Estado: Borrador

## Contexto

El backend persiste configuracion, diagramas y la intencion necesaria para reconstruir
el dominio. Al abrir un proyecto, el cliente debe crear un modelo nuevo, restaurar
equipos y conexiones, aplicar unidades y metodo termodinamico, y recalcular resultados.

## Problema Actual

- Un unico servicio descarga, mapea, reconstruye, registra en solver y actualiza UI.
- La hidratacion dispara una simulacion en segundo plano y puede declararse terminada
  antes que el recalculo.
- Datos incompatibles se ignoran en varios puntos sin diagnostico estructurado.
- Una recarga reemplaza referencias completas del proyecto y suscripcion del solver.
- No existe un resultado de hidratacion que distinga carga parcial, dato invalido y
  fallo de simulacion.

## Comportamiento Deseado

La hidratacion es un proceso asincrono, determinista y cancelable que construye un
proyecto aislado antes de publicarlo como proyecto actual.

## Orden De Hidratacion

1. Obtener `ProjectDocument` autorizado desde HTTP.
2. Validar version, identificadores y configuracion basica.
3. Cargar el metodo termodinamico requerido.
4. Crear el proyecto y aplicar configuracion y unidades.
5. Crear diagramas en su orden persistido.
6. Crear equipos y restaurar solamente intencion persistida.
7. Registrar streams y equipos una sola vez en el solver del proyecto.
8. Restaurar pipes y conexiones de puertos locales.
9. Restaurar OPC y conexiones interdiagrama.
10. Restaurar formulas cuando todos sus simbolos esten disponibles.
11. Validar integridad del grafo reconstruido.
12. Ejecutar y esperar el recalculo inicial cuando corresponda.
13. Publicar atomically el proyecto hidratado a la sesion y la UI.

## Invariantes

1. El proyecto anterior permanece util hasta que el nuevo modelo este construido.
2. Un equipo aparece como maximo una vez en el registry y en el solver.
3. Un pipe referencia equipos y puertos existentes.
4. Una conexion interdiagrama solo existe si ambos extremos son validos.
5. Las formulas se restauran despues de registrar los streams que referencian.
6. Los valores calculados persistidos accidentalmente no reemplazan el recalculo.
7. La UI no observa un proyecto parcialmente hidratado.
8. Una carga cancelada no puede convertirse despues en proyecto actual.
9. Una respuesta atrasada de otro proyecto se descarta.
10. Finalizar hidratacion significa que termino el recalculo inicial requerido.

## Datos Incompatibles

- Un tipo de equipo desconocido produce diagnostico identificable.
- Un elemento aislado corrupto puede omitirse solo si la integridad restante es
  demostrable y el resultado se marca como carga parcial.
- Un pipe huerfano no se restaura.
- Una unidad desconocida usa fallback explicitamente registrado; no cambia en silencio
  el valor fisico.
- Una formula invalida se conserva como texto pendiente cuando sea posible, junto con
  su diagnostico.
- Un documento estructuralmente invalido no se publica como proyecto actual.

## Cambio De Proyecto

Al seleccionar otro proyecto:

- se resuelven primero guardados locales pendientes segun la spec de persistencia;
- se cancela o invalida la hidratacion anterior;
- se abandona el grupo realtime anterior;
- se hidrata el nuevo proyecto;
- solo entonces se actualizan canvas, presencia y diagrama activo.

## Criterios De Aceptacion

1. Abrir dos veces el mismo documento produce un dominio equivalente.
2. Inputs, formulas, unidades, nombres y topologia se restauran correctamente.
3. Los resultados del solver se recalculan y no se toman como verdad persistida.
4. La pantalla de carga permanece activa hasta finalizar el recalculo inicial.
5. Cambiar rapidamente A -> B no permite que A reaparezca por una respuesta atrasada.
6. Un pipe huerfano se diagnostica y no provoca excepcion de render.
7. Un proyecto fallido no reemplaza al proyecto valido que estaba activo.
8. El solver del proyecto hidratado contiene exactamente sus streams y equipos.

## Pruebas Requeridas

- Proyecto vacio y proyecto con un diagrama.
- Multiples diagramas y orden persistido.
- Todos los tipos de equipo soportados.
- Inputs, display units y auditoria.
- Formulas validas, pendientes e invalidas.
- Pipe huerfano, puerto inexistente y tipo desconocido.
- Conexion interdiagrama completa e incompleta.
- Cancelacion y respuestas fuera de orden.
- Recalculo convergente, no convergente y fallido.

## Objetivos De Refactor Posteriores

- Mapeo DTO-dominio dentro de `ProjectSessionService`.
- `FacadeStateSerializer`.
- Registro de equipos en solver.
- Restauracion de formulas y conexiones interdiagrama.
- Estado visible de hidratacion.

## Fuera De Alcance

- Migrar automaticamente cualquier formato historico no identificado.
- Recuperar datos fisicamente corruptos sin reglas conocidas.
- Persistir resultados calculados para evitar el recalculo.

