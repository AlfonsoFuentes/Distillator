# Instrucciones De Trabajo

## Objetivo

Trabajar con Alfonso de forma precisa, ordenada y costo-beneficio: diagnosticar antes de tocar, resolver la causa raiz cuando sea razonable, evitar cambios innecesarios y explicar las decisiones importantes en lenguaje claro.

## Estilo De Colaboracion

- Responder en espanol por defecto.
- Ser directo, practico y claro.
- Si falta informacion, primero intentar inferirla leyendo el proyecto o el error disponible.
- Preguntar solo cuando una suposicion pueda causar perdida de tiempo, romper algo o cambiar el alcance.
- Mantener al usuario informado con avances cortos cuando el trabajo tome tiempo.

## Metodo Para Diagnosticar Proyectos

1. Entender el objetivo o falla visible.
2. Revisar estructura del proyecto, archivos de configuracion y comandos relevantes.
3. Identificar tecnologia, version, dependencias y punto de entrada.
4. Reproducir el error cuando sea seguro.
5. Separar sintomas de causa probable.
6. Proponer o aplicar el cambio minimo que resuelva el problema.
7. Verificar con build, test o prueba manual cuando aplique.

## Costo-Beneficio

- Priorizar cambios pequenos, reversibles y faciles de verificar.
- Evitar refactors grandes si no son necesarios para resolver la tarea.
- Antes de una solucion compleja, buscar si hay una configuracion, dependencia o error simple.
- Cuando haya varias opciones, comparar brevemente:
  - impacto;
  - riesgo;
  - tiempo estimado;
  - mantenibilidad.

## Reglas Para Codigo

- Seguir los patrones existentes del proyecto.
- No cambiar arquitectura, estilos o nombres sin necesidad clara.
- No tocar archivos no relacionados con la tarea.
- No borrar ni revertir cambios del usuario salvo autorizacion explicita.
- Agregar comentarios solo cuando aclaren una parte no obvia.
- Preferir pruebas enfocadas al area modificada.

## Blazor / .NET

- Revisar primero `.sln`, `.csproj`, `Program.cs`, configuracion de `Client`, `Server` y `Shared`.
- Distinguir entre Blazor WebAssembly standalone, hosted y Blazor Web App.
- Validar versiones de .NET, paquetes NuGet, rutas, DI, autenticacion, CORS, `HttpClient`, endpoints y configuracion de publish.
- Para errores de runtime, revisar consola del navegador, terminal del servidor y logs de ASP.NET Core.

## Reglas Del Proyecto Distillator

- El proyecto principal es `C:\Programas\Distillator\Distillator`.
- El usuario autorizo acceso de lectura a los archivos del proyecto Distillator para diagnostico; no pedir permiso archivo por archivo cuando solo se lea codigo del proyecto.
- La autorizacion de lectura no implica autorizacion para modificar archivos del proyecto.
- Distillator es un simulador de destilacion en Blazor WebAssembly ASP.NET Core hosted con PostgreSQL.
- El solver es reactivo.
- El codigo debe respetar patrones de diseno y principios SOLID, KISS, DRY y YAGNI.
- Seguir design patterns cuando aporten claridad, extensibilidad o mantenibilidad real.
- Reconocer que la mayoria del codigo del proyecto sigue buenas practicas; al modificar, mantener ese nivel y estilo.
- Priorizar soluciones simples, mantenibles y de buen costo-beneficio.
- No sobrearquitecturar ni introducir capas, patrones o abstracciones sin necesidad clara.
- El servidor usa PostgreSQL, EF Core e Identity; evitar repetir secretos o credenciales en respuestas.
- Antes de proponer cambios en seguridad/configuracion, considerar impacto en base de datos, autenticacion y despliegue.

## Arquitectura Y Mantenibilidad

- Mantener separacion clara: UI en `Client`, API/persistencia en `Server`, contratos compartidos en `Shared`, reglas de dominio en `Distillator.Domain`.
- Evitar que componentes `.razor` tengan logica pesada; mover calculo, orquestacion o reglas a servicios, dominio o clases dedicadas.
- No duplicar logica entre UI, dominio y solver; si una regla afecta simulacion, debe vivir fuera del componente visual.
- Usar nombres descriptivos en ingles para codigo publico, tipos, metodos y propiedades.
- Mantener metodos cortos cuando sea razonable.
- Eliminar codigo muerto solo cuando se confirme que no se usa y el usuario autorice el cambio.
- Antes de refactors grandes, identificar caso reproducible, riesgo y beneficio.

## Solver Y Dominio

- Cada equipo, unidad u operacion debe tener responsabilidad clara: validar entradas, resolver y reportar resultado.
- Usar resultados explicitos tipo `Result<T>` para errores esperados cuando aplique; no usar excepciones para flujo normal.
- Validar unidades, composiciones y rangos fisicos cerca del punto donde entran los datos.
- Los calculos numericos deben manejar tolerancias explicitas y estados de no convergencia.
- El solver se trabaja bajo la filosofia de intencion: limpiar resultados calculados, intentar ecuaciones ordenadamente y retirar del listado pendiente solo lo que realmente converge.
- Las specifications deben conservar tres niveles de intento cuando aplique: specification suelta, specification + equipo donde reside, y specification + equipos conectados inmediatos.
- No modificar `NewtonSolver` para aceptar sistemas no cuadrados o retirar ecuaciones por residual casualmente bajo sin evaluar el conteo de variables/incognitas; preferir corregir el armado de ecuaciones o clusters.
- Priorizar pruebas para balances de masa, balances de energia, limites fisicos, convergencia y no convergencia.
- Crear pruebas de regresion para bugs del solver o calculos corregidos.

## Blazor Y Estado

- Componentes pequenos y enfocados.
- Evitar bloques `@code` gigantes; usar partial class `.razor.cs` cuando el componente crezca.
- Usar `EventCallback` para comunicacion padre-hijo.
- Evitar mutar estado compartido desde muchos componentes; pasar por un servicio u orquestador.
- Cuidar renders: usar `@key` cuando ayude, evitar recalcular listas pesadas en markup y llamar `StateHasChanged` solo cuando haga falta.

## Reglas UI De Distillator

- Implementar componentes Blazor principalmente con HTML y CSS scoped.
- MudBlazor esta instalado, pero solo debe usarse cuando mejore claramente la experiencia de usuario.
- Los textos visibles, labels, botones, mensajes y leyendas de UI deben estar en ingles.
- Los comentarios de codigo deben estar en espanol.
- Los estilos CSS deben ser minimalistas.
- Mantener paletas de colores pasteles y coherentes con los CSS existentes.
- Antes de modificar estilos, revisar archivos `.razor.css` relacionados y conservar el lenguaje visual actual.
- Cada componente visual debe preferir su propio `.razor.css` scoped.
- Evitar estilos inline salvo casos minimos y justificados.
- Mantener accesibilidad basica: labels claros, botones comprensibles y contraste suficiente.

## Datos, Seguridad Y Configuracion

- No guardar nuevos secretos en `appsettings.json`; preferir User Secrets, variables de entorno o configuracion segura.
- No repetir connection strings, passwords ni secretos en respuestas, commits o documentacion publica.
- Validar DTOs en servidor aunque el cliente ya valide.
- Mantener endpoints protegidos por autorizacion y roles cuando corresponda.

## Testing

- Priorizar pruebas unitarias para dominio, solver, reglas de negocio y calculos.
- Agregar pruebas de regresion para bugs corregidos.
- En calculos numericos, usar tolerancias explicitas y nombres de tests que indiquen el escenario fisico.
- Incluir casos edge cuando aplique: flujo cero, composicion invalida, presion/temperatura fuera de rango y no convergencia.

## Regla De Modificaciones

- Antes de cualquier modificacion de archivos del proyecto Distillator, preguntar primero y confirmar que la app no este corriendo.
- Si solo se esta leyendo o diagnosticando, indicar que no se modificara nada.
- No tocar `C:\Programas\Distillator\Distillator` sin autorizacion explicita para esa accion.

## Seguridad Y Permisos

- Para carpetas fuera del workspace, pedir permiso antes de leer o ejecutar comandos.
- Para comandos que modifiquen, instalen dependencias, ejecuten servicios o usen red, explicar por que son necesarios.
- No ejecutar acciones destructivas sin autorizacion clara.

## Entregables

- Al finalizar, resumir:
  - que se encontro;
  - que se cambio;
  - como se verifico;
  - que queda pendiente o recomendado.
