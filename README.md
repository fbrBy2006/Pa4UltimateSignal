# BlueBox PA4 - La última señal

Videojuego 3D de supervivencia y plataformas creado en Unity 6.6 con C#. Explora un laboratorio dañado, recoge las siete celdas de energía, evita los peligros, activa el núcleo y cruza la salida. Una sentinela patrulla el corredor y persigue al jugador cuando lo detecta.

## Ejecutar el juego

En Windows, abre `Builds/Windows/BlueBoxPA4.exe`. Conserva junto al ejecutable la carpeta completa del build: `BlueBoxPA4_Data`, `MonoBleedingEdge`, `UnityPlayer.dll`, `UnityCrashHandler64.exe` y las demás dependencias.

## Controles

- **WASD o flechas:** moverse.
- **Ratón:** mirar.
- **Shift izquierdo:** correr.
- **Espacio:** saltar.
- **E:** activar el núcleo cuando hayas reunido las siete celdas.
- **Escape:** liberar el cursor; clic izquierdo para volver a capturarlo.
- **R:** reiniciar tras perder las tres vidas.

## Sistemas incluidos

- Nivel corto con inicio, objetivo, recolección, peligros, punto de control, condición de victoria y condición de derrota.
- HUD con objetivo, controles, celdas, vidas, cronómetro y estado de la sentinela.
- IA propia con patrulla, detección por distancia y línea de visión, persecución, búsqueda breve y ataque por contacto.
- Música ambiental, tono de alerta, iluminación, materiales emisivos y partículas de vapor.
- Optimización de luces: las luces puntuales decorativas en tiempo real no proyectan sombras. La IA comprueba distancia y línea de visión cada 0.2 segundos (5 veces por segundo) mientras suaviza el movimiento cada frame.
- Código separado por jugador, interacción, IA y generación de nivel.

La escena crea la sentinela y muestra las instrucciones al iniciar si todavía no están guardadas en ella. Desde el editor, **Blue Box > Prepare PA4 Scene** también guarda estos objetos en la escena. Así el objetivo y la IA funcionan al abrir la escena incluida, sin depender de ejecutar el generador.

## Abrir y reconstruir

Abre este directorio como proyecto con Unity **6000.6.0f1** y deja que termine la importación. La escena de entrega es `Assets/_Project/Scenes/BlueBoxLab.unity`; ya está incluida en Build Settings. Para regenerarla desde el editor, usa **Blue Box > Build PA4 Game**. El constructor también inicia el bake de iluminación estática. Para reconstruir el ejecutable, usa **Blue Box > Build Windows EXE** y entrega siempre la carpeta completa `Builds/Windows`.

El proyecto incluye la escena y los recursos fuente. `Library`, cachés y archivos locales del editor no forman parte del proyecto compartible.
