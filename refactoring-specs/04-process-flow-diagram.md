# Spec 04 — Shared/ProcessFlowDiagram: Connect/Disconnect + Código Muerto

## Estado
Pendiente

## Archivos afectados
- `Shared/ProcessFlowDiagram/IVisualElement.cs` — `VisualElementBase`
- `Shared/ProcessFlowDiagram/Vessels/FlashTankVisualElement.cs`
- `Shared/ProcessFlowDiagram/Streams/StreamVisualElement.cs`

---

## Contexto

`VisualElementBase` es la clase base de todos los elementos visuales del canvas. Está bien
diseñada en general, pero acumula versiones duplicadas de `Connect`/`CanConnect` y un
`Disconnect` incompleto que deja la responsabilidad de consistencia al llamador externo.

---

## Problemas a resolver

### 1. [ALTA] `CanConnect` y `CanConnect2` — lógica duplicada con diferencia sutil

`CanConnect` permite Equipo-Equipo; `CanConnect2` lo prohíbe (solo permite bipartito).
`Connect` usa `CanConnect` internamente. `Connect2` también usa `CanConnect` (no `CanConnect2`),
lo que hace que `Connect2` tenga la misma permisividad que `Connect`.

Resultado: la versión `2` no agrega valor real y crea confusión sobre cuál es el contrato
vigente del sistema.

### 2. [ALTA] `Disconnect` es incompleto — deja consistencia al llamador

```csharp
public void Disconnect(string myPortName)
{
    // ...
    myPort.ConnectedElementId = null;
    // Nota: El lienzo también deberá llamar al Disconnect del otro elemento
}
```

El otro extremo de la conexión no se limpia aquí. Si el lienzo no llama también al
`Disconnect` del otro elemento, los puertos quedan inconsistentes (un lado conectado,
otro libre). Este patrón es frágil.

### 3. [MEDIA] `FlashTankVisualElement.RefreshDynamicPorts()` — método vacío con 30 líneas comentadas

El método existe pero todo su cuerpo está comentado. Es código muerto que confunde.

### 4. [BAJA] `StreamVisualElement` tiene `GetToolTipLegend()` duplicando responsabilidad de tooltip

El stream tiene dos métodos de tooltip:
- `GetToolTipLegend()` — propio, bien implementado
- `GetToolTipData()` — de `IVisualElement`, retorna lista vacía (override del base que también
  retorna lista vacía)

Debería unificarse: o implementar `GetToolTipData()` correctamente o eliminar uno.

---

## Cambios requeridos

### Paso 1 — Decidir el contrato de conexión y eliminar la versión `2`

Revisar qué versión usa el `FlowsheetManager` / `EquipmentPortConnector` actualmente.
Determinar si el contrato vigente es bipartito (solo Equipo-Stream) o permisivo (permite
Equipo-Equipo).

Si el contrato es bipartito (`CanConnect2`):
- Renombrar `CanConnect2` → `CanConnect` (override del virtual base)
- Eliminar el `CanConnect` permisivo
- Eliminar `Connect2`

Si el contrato es permisivo (`CanConnect`):
- Eliminar `CanConnect2` y `Connect2`

En ambos casos, queda un único par `CanConnect`/`Connect`.

### Paso 2 — Hacer `Disconnect` atómicamente consistente

`Disconnect` debe limpiar ambos extremos de la conexión. Necesita una referencia al
elemento conectado para llamar al `Disconnect` del otro lado:

```csharp
public void Disconnect(string myPortName)
{
    var myPort = Ports.FirstOrDefault(p => p.Name == myPortName);
    if (myPort == null || myPort.ConnectedElementId == null) return;

    // Notificar al facade del equipo que la conexión se rompe
    if (this.Facade is IEquipmentFacade)
        DetachConnection(myPortName);

    // Liberar el puerto local
    myPort.ConnectedElementId = null;
}
```

Para limpiar el otro extremo sin acoplar `VisualElementBase` a una lista global,
el llamador (canvas / FSM) sigue siendo responsable de llamar `Disconnect` en ambos
elementos. Lo que se mejora es que el método quede **completo** de su lado y que el
comentario incompleto se elimine.

Alternativa más robusta: agregar un método `DisconnectBoth(IVisualElement other, string otherPortName)`
en la interfaz `IVisualElement` que encapsule la operación de ambos lados.

### Paso 3 — Limpiar `FlashTankVisualElement.RefreshDynamicPorts()`

Opciones:
- Si la funcionalidad de puertos dinámicos está planificada: dejar el método vacío con un
  comentario breve `// Puertos dinámicos no implementados en esta versión.`
- Si no está planificada: eliminar el método.

El cuerpo comentado de 30 líneas debe eliminarse en ambos casos.

### Paso 4 — Unificar tooltips en `StreamVisualElement`

```csharp
// Implementar GetToolTipData() correctamente y eliminar GetToolTipLegend():
public override List<ToolTipLegend> GetToolTipData()
{
    return new List<ToolTipLegend>
    {
        new("Temperature", LocalFacade.Temperature.ToUiString()),
        new("Pressure", LocalFacade.Pressure.ToUiString()),
        new("Mass Flow", LocalFacade.MassFlow.ToUiString()),
        new("Vapor Fraction", LocalFacade.VaporFraction.ToUiString()),
        new("Status", StatusText)
    };
}
```

Si algún componente Razor usa `GetToolTipLegend()` directamente, actualizarlo para usar
`GetToolTipData()`.

---

## Verificación

1. `dotnet build` sin errores.
2. `rg "CanConnect2\|Connect2"` no debe aparecer en producción.
3. Prueba manual en el canvas: conectar y desconectar un stream a una bomba — los puertos
   deben liberarse correctamente en ambos lados.
4. `rg "GetToolTipLegend"` no debe aparecer después del cambio de tooltip.

---

## Riesgo
**Medio.** El cambio en `CanConnect` depende de cuál versión esté activa. Verificar
en `FlowsheetManager`/`EquipmentPortConnector` cuál se llama antes de actuar.

---

## Dependencias previas
Ninguna. Esta spec es independiente.
