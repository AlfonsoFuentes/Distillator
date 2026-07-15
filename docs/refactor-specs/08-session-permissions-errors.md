# 08 - Sesion, Permisos Y Manejo De Errores

Estado: Borrador

## Contexto

La aplicacion usa Identity con cookies, roles globales y roles por proyecto. Tambien
conserva preferencias personales de workspace y la seleccion de proyecto/diagrama.

## Conceptos Separados

### Authentication Session

Identidad autenticada, claims y roles globales como Administrator o Developer.

### Project Access

Rol dentro de un proyecto:

- Owner: editar, compartir y eliminar;
- Editor: editar contenido, no administrar propiedad;
- Viewer: leer y colaborar por presencia sin mutar.

### Workspace Session

Preferencias personales:

- ultimo proyecto;
- ultimo diagrama;
- paneles colapsados;
- tipos de diagrama expandidos.

No pertenece al documento compartido del proyecto.

## Inicio De Sesion

1. Autenticar mediante servidor.
2. Cargar identidad y roles una sola vez por ciclo de autenticacion.
3. Cargar workspace state del usuario.
4. Obtener resumen de proyectos accesibles.
5. Seleccionar el ultimo proyecto aun autorizado o un fallback determinista.
6. Hidratar el proyecto seleccionado.
7. Unir realtime despues de validar acceso.

## Cambio De Proyecto Y Diagrama

- Seleccionar proyecto es un comando explicito de sesion, no un efecto de render.
- Antes de abandonar un proyecto se resuelven cambios locales pendientes.
- El diagrama activo debe pertenecer al proyecto actual.
- La seleccion se guarda como workspace state sin cambiar version del proyecto.
- La UI se actualiza despues de aceptar el cambio de sesion.

## Cambio De Permisos

- Si un Editor pasa a Viewer, se detienen nuevos comandos de escritura.
- Guardados ya enviados se resuelven segun la autoridad vigente en servidor.
- Si se retira acceso, el proyecto deja de estar activo y se limpia informacion
  compartida de la UI.
- Owner no puede retirarse accidentalmente mediante la lista de colaboradores.
- La UI orienta, pero el servidor siempre vuelve a autorizar.

## Manejo De Errores

Los errores se clasifican:

| Categoria | Ejemplo | Comportamiento |
|---|---|---|
| Validation | valor o DTO invalido | No mutar; mensaje util |
| Authorization | Viewer intenta guardar | Rechazar; refrescar acceso si cambio |
| Conflict | version atrasada | Conservar intencion; reconciliar |
| Connectivity | timeout o sin red | Mantener pendiente; reintento limitado |
| Simulation | no convergencia | Conservar intencion; mostrar diagnostico |
| DataIntegrity | pipe huerfano | No publicar modelo invalido |
| Unexpected | excepcion no prevista | Registrar contexto seguro; recuperar estado |

No se usan excepciones para validaciones esperadas. Una excepcion capturada no se
considera resuelta solo por escribir en consola.

## Seguridad

- DTOs se validan en servidor.
- Endpoints administrativos requieren roles de servidor.
- No se codifican listas de cuentas privilegiadas en componentes.
- Passwords temporales y secretos no se registran ni devuelven fuera del flujo
  estrictamente requerido.
- Mensajes al cliente no exponen detalles internos de base de datos.
- Tenant y acceso se derivan de la identidad autenticada, no del payload del cliente.

## Invariantes

1. Un componente no selecciona proyecto como efecto secundario de render.
2. Workspace state pertenece a un usuario y no incrementa version del proyecto.
3. Viewer no modifica dominio compartido, solver ni persistencia.
4. Owner y Editor se validan nuevamente en servidor.
5. Perder acceso invalida sesion del proyecto de forma controlada.
6. Un error esperado produce resultado explicito.
7. Un error de persistencia no se oculta bajo estado local exitoso.
8. Logout limpia identidad, proyecto activo, presencia y estado sensible en memoria.
9. No se exponen secretos en UI, logs, auditoria o respuestas.

## Criterios De Aceptacion

1. Login carga identidad una vez y selecciona proyecto deterministamente.
2. Cambiar proyecto no depende de `OnParametersSet` para efectos de sesion.
3. Viewer no puede editar por canvas, dialogos ni llamadas directas al endpoint.
4. Editor puede modificar contenido pero no sharing ni eliminar proyecto.
5. Owner puede administrar sharing y eliminar.
6. Retirar acceso remoto saca al usuario del proyecto sin perder otro trabajo local
   ajeno.
7. Logout elimina presencia y evita usar objetos de la sesion anterior.
8. Timeout, conflicto y permiso insuficiente se distinguen en el resultado.

## Pruebas Requeridas

- Login correcto, incorrecto y cambio inicial de password.
- Logout y sesion expirada.
- Seleccion con cero, uno y multiples proyectos.
- Ultimo proyecto ya no autorizado.
- Owner, Editor y Viewer en UI y endpoints.
- Cambio de rol durante una sesion activa.
- Retiro de acceso y eliminacion del proyecto.
- Timeout, 401, 403, conflicto y error inesperado.
- Ausencia de secretos en mensajes de error.

## Objetivos De Refactor Posteriores

- `CustomAuthenticationStateProvider`.
- Responsabilidades de sesion en `ProjectSessionService`.
- `ProjectDiagram` y seleccion de proyecto.
- Endpoints de usuarios y proyectos.
- Mensajes y resultados HTTP.

## Fuera De Alcance

- Cambiar proveedor de Identity.
- Single sign-on sin requisito funcional.
- Permisos por variable o equipo.

