# 13 - Catalogos, Administracion Y Reportes

Estado: Borrador

## Contexto

Distillator administra componentes quimicos, metodos termodinamicos, usuarios y
exportaciones. Estas funciones soportan la simulacion, pero tienen ciclos de vida y
permisos diferentes a la edicion de proyectos.

## Componentes Quimicos

Operaciones:

- listar resumen;
- obtener detalle completo;
- crear;
- editar;
- eliminar;
- validar identificadores, correlaciones y coeficientes;
- exportar catalogo.

Reglas:

- nombres e identificadores requeridos son unicos segun la politica vigente;
- correlaciones validan unidades, coeficientes y rango;
- eliminar un componente referenciado no debe romper silenciosamente metodos o
  proyectos;
- DTOs se validan en servidor aunque la UI valide;
- solo roles autorizados modifican el catalogo.

## Metodos Termodinamicos

Operaciones:

- listar;
- obtener configuracion completa;
- crear, editar y eliminar;
- asociar componentes;
- administrar parametros binarios;
- exportar catalogo.

Reglas:

- componentes asociados deben existir;
- cada pareja binaria se normaliza para evitar duplicados por orden inverso;
- parametros requeridos deben ser finitos y validos para el modelo;
- un metodo usado por proyectos no se elimina sin politica explicita;
- cargar el metodo completo produce un DTO determinista.

## Usuarios Y Roles Globales

- Registro publico crea solamente el rol permitido por la politica.
- Crear usuarios administrativamente requiere Administrator.
- Listar usuarios requiere Developer o Administrator segun endpoint.
- Activar o desactivar respeta protecciones de roles privilegiados.
- Cambiar password inicial valida identidad, password actual y politica.
- Roles globales no sustituyen roles de colaboracion del proyecto.
- Passwords temporales no se codifican como constantes permanentes ni se exponen en
  logs.

## Reportes Y Exportacion

- La exportacion usa datos ya autorizados para el usuario.
- Nombre, formato, columnas y unidades son deterministas.
- Un archivo se genera completamente o se informa fallo.
- La exportacion no muta proyecto, catalogos ni solver.
- `AutoExportOnSimulation` no se ejecuta hasta que exista una spec de producto que
  defina disparador, destino y tratamiento de errores.
- Archivos no incluyen secretos ni campos internos no aprobados.

## Phase Envelope Y Graficas

- Se ejecutan bajo solicitud explicita o politica definida.
- Presentan loading, resultado o error real.
- Cancelar o cerrar evita publicar un resultado atrasado sobre otro stream.
- Las graficas consumen resultados y no alteran inputs.
- Datos vacios, no finitos o fuera de rango producen estado controlado.

## Errores

- Un CRUD solo navega o cierra como exitoso cuando el servidor confirma.
- Errores de validacion se asocian al campo o entidad correspondiente.
- Errores de base de datos no se devuelven literalmente al usuario.
- Eliminar es idempotente cuando sea seguro o informa que el recurso ya no existe.
- Exportacion fallida no produce archivo parcial presentado como valido.

## Invariantes

1. Toda mutacion de catalogo esta autorizada y validada en servidor.
2. Relaciones entre metodo, componentes y parametros permanecen integras.
3. Un CRUD fallido no se muestra como exitoso.
4. Roles globales y roles de proyecto no se confunden.
5. Exportar y graficar son operaciones de lectura respecto al proyecto.
6. No se exponen passwords, connection strings ni detalles internos.
7. Datos numericos exportados incluyen unidad o contexto suficiente.
8. Un resultado asincrono atrasado no reemplaza la seleccion actual.

## Criterios De Aceptacion

1. CRUD de componentes conserva correlaciones y valida rangos.
2. CRUD de metodos conserva componentes y parametros binarios.
3. Eliminar recursos referenciados sigue una politica explicita.
4. Cada endpoint rechaza roles no autorizados.
5. Registro, creacion administrativa, activacion y cambio de password funcionan segun
   politica.
6. Exportar componentes y metodos genera archivos reproducibles.
7. Una exportacion fallida informa error sin descarga invalida.
8. Phase envelope muestra resultado del stream solicitado y maneja cancelacion.
9. Graficas no mutan variables ni ejecutan autosave.

## Pruebas Requeridas

- CRUD valido e invalido de componentes.
- Correlaciones incompletas, fuera de rango y con coeficientes no finitos.
- CRUD de metodos y relaciones.
- Parametro binario A-B y B-A.
- Recurso referenciado durante eliminacion.
- Matriz de roles por endpoint.
- Registro, login, password inicial, activar y desactivar.
- Exportacion vacia, nominal y fallida.
- Verificacion basica de columnas, unidades y nombre de archivo.
- Phase envelope nominal, no convergente, cancelado y seleccion cambiada.
- Graficas con datos vacios y no finitos.

## Objetivos De Refactor Posteriores

- Endpoints y paginas de componentes.
- Endpoints y paginas de metodos termodinamicos.
- Endpoints y paginas de usuarios.
- Servicios Excel y phase envelope.

## Fuera De Alcance

- Redisenar el catalogo cientifico durante el refactor de coordinacion.
- Programar exportaciones automaticas sin destino y politica de producto.
- Cambiar el proveedor de archivos o identidad sin necesidad demostrada.

