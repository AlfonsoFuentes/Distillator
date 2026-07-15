# 10 - Formulas Y Specifications

Estado: Borrador

## Contexto

Las specifications activas se expresan mediante formulas que relacionan variables de
streams. Deben persistirse de forma generica, conservar autoria y participar en los
tres niveles de intento definidos por el solver.

## Ciclo De Vida

Una formula puede estar:

- Editing: texto temporal de UI;
- Invalid: sintaxis o simbolos no validos;
- Pending: formula valida, pero faltan valores para evaluar;
- Active: formula valida incluida en el plan del solver;
- Solved: satisfecha dentro de tolerancia en una ejecucion concreta;
- Failed: evaluacion o cluster no convergente en una ejecucion concreta.

`Solved` y `Failed` son resultados transitorios; formula, identidad y autoria son
intencion persistida.

## Creacion Y Edicion

1. La UI conserva texto temporal sin mutar la coleccion activa.
2. El parser valida sintaxis, operadores, streams, componentes y variables.
3. Una formula valida crea o reemplaza una specification con identidad estable.
4. Se registra autor y fecha del commit.
5. Se solicita simulacion una sola vez.
6. Se marca la revision para autosave.

Editar conserva el identificador de la specification. Cancelar conserva la formula
anterior sin cambios.

## Eliminacion

- Retira la specification por identidad.
- Invalida resultados solver que dependan de ella.
- Solicita simulacion.
- Persiste la nueva coleccion.
- No deja ecuaciones o suscripciones huerfanas.

## Resolucion De Simbolos

- Los streams se resuelven por identidad estable cuando el formato lo permita.
- El nombre es presentacion y puede cambiar por naming.
- Mientras el formato actual use nombres, un renombrado debe actualizar formulas
  atomically o impedir una referencia rota.
- Variables y componentes se validan contra contratos conocidos.
- La cultura numerica del parser es determinista.

## Estrategia Del Solver

Se conservan tres niveles de intento cuando aplique:

1. specification suelta;
2. specification mas equipo donde reside;
3. specification mas equipos conectados inmediatos.

Una ecuacion o cluster se retira del trabajo pendiente solo cuando converge segun su
contrato. Un residual casualmente bajo no autoriza retirar ecuaciones sin validar
variables, incognitas y tolerancias.

## Persistencia

Se guarda:

- identificador;
- expresion original normalizada solo cuando sea seguro;
- propietario o equipo donde reside;
- autor y fecha;
- version de formato si fuera necesaria.

No se guardan como verdad:

- residual actual;
- cluster construido;
- estado `Solved`;
- valores calculados resultantes.

## Invariantes

1. Texto temporal no modifica specifications activas.
2. Una formula invalida no ejecuta solver ni autosave.
3. Editar conserva identidad; crear genera una nueva.
4. Cada specification activa aparece una sola vez en el plan correspondiente.
5. El parser no depende del orden de render de componentes.
6. Renombrar no deja formulas silenciosamente rotas.
7. Autoria y fecha sobreviven guardado y recarga.
8. Una formula pendiente no se trata como error inesperado.
9. Resultados de una ejecucion no se persisten como definicion.
10. El componente antiguo no compite con la UI activa de formulas.

## Criterios De Aceptacion

1. Crear una formula valida la agrega, simula y guarda una vez.
2. Una formula invalida conserva el editor y explica la causa.
3. Una formula con datos faltantes queda pendiente y se activa cuando aparecen.
4. Editar conserva ID y actualiza autoria y fecha segun la regla aprobada.
5. Eliminar retira la formula despues de recargar.
6. Renombrar un stream mantiene o migra referencias sin perdida.
7. Los tres niveles de intento se ejecutan en el orden definido.
8. Un cluster no cuadrado se diagnostica sin modificar arbitrariamente NewtonSolver.
9. Dos formulas no se duplican durante hidratacion.

## Pruebas Requeridas

- Sintaxis valida e invalida.
- Constantes, operadores y precedencia.
- Stream o variable inexistente.
- Componente valido e inexistente.
- Datos suficientes e insuficientes.
- Crear, editar, cancelar y eliminar.
- Renombrar streams y equipos.
- Persistencia, auditoria e hidratacion.
- Tres niveles de intento.
- Cluster cuadrado, no cuadrado, convergente y no convergente.

## Objetivos De Refactor Posteriores

- `FormulaParser` y `FormulaSpecification`.
- `EquipmentBaseFormulaSpecifications`.
- Construccion del plan en `MainSolver`.
- Serializacion y restauracion de formulas.

## Fuera De Alcance

- Lenguaje general de scripting.
- Funciones personalizadas ejecutables desde servidor.
- Persistir clusters construidos por el solver.

