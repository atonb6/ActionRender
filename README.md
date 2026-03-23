# ActionRender

Primera base para retomar el demo en Unity 2017.3.1f1.

## Qué quedó implementado

- Un controlador de ciclo día/noche que mueve el sol durante el día y activa una luz de luna durante la noche.
- Un HUD táctil generado en runtime para una pantalla touch:
  - slider de 24 horas,
  - botones `-15m` y `+15m`,
  - presets `08:00`, `12:00` y `18:30`,
  - modo automático para dejar correr el día completo.
- Ajuste de iluminación ambiente y exposición del skybox según la hora.

## Cómo probarlo en Unity

1. Abrir el proyecto con **Unity 2017.3.1f1** o una versión compatible.
2. Abrir la escena `Assets/Scenes/Action.unity`.
3. Ejecutar Play.
4. En la parte inferior aparecerá el panel táctil para mover la hora y revisar cómo cambia la luz sobre el edificio.

## Ajustes recomendados

El componente `DayNightCycleController` se agrega automáticamente a la luz direccional principal. Desde el Inspector puedes ajustar:

- `sunriseHour` y `sunsetHour`,
- `sunriseAzimuth` y `sunsetAzimuth`,
- `maxSunElevation`,
- `dayDurationInSeconds`.

Con eso puedes acercar la trayectoria solar al comportamiento del proyecto inmobiliario que quieras mostrar.

## Próximo paso sugerido

Para una segunda iteración conviene conectar la trayectoria solar a una ubicación real (latitud/longitud) y una fecha específica, así la simulación se acerca más a la incidencia solar real de cada departamento.
