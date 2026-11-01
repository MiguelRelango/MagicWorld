# Hearthbound — prólogo jugable (Unity 6)

Action-RPG 3D de aventuras con puzles al estilo *Alundra* y atmósfera cálida y estilizada.
Este repositorio contiene el **prólogo** descrito en el GDD (casa, aldea, primer combate, arco,
bombas y la cueva con el puzle de muros agrietados) y un **mundo abierto en una isla procedural**
que se genera por trozos mientras avanzas, como Minecraft.

El mundo y los personajes se generan por código. El repositorio incluye texturas pintadas,
follaje, flores y rocas procedurales, además de shaders URP para mezclar el terreno y animar el mar.

## Cómo probarlo

1. En Unity Hub crea un proyecto **3D** (Unity 6, plantilla **Universal 3D (URP)**; versión usada: **6000.3.15f1**).
2. Copia el **contenido** de `Assets/` de este repositorio dentro de `Assets/` de tu proyecto, conservando los archivos `.meta`.
3. Espera a que Unity compile. Pulsa **Play** en cualquier escena: el juego se inicia solo.
4. Opcional: menú **Hearthbound → Crear escena del prólogo** guarda una escena lista,
   y **Hearthbound → Comprobar configuración** muestra el pipeline y el sistema de input.

Funciona con el *Input System* nuevo y con el Input Manager clásico.

> Los scripts C# se comprobaron con el compilador y las bibliotecas de Unity 6000.3.15f1.
> Los shaders nuevos, el resultado visual y las mecánicas requieren comprobación en Play y en una build.
> Este repositorio contiene assets para importar; no incluye `Library/`, un ejecutable ni el proyecto completo de Unity.

## Controles

Todas las teclas de teclado y ratón se pueden cambiar en **Opciones → Controles** (dos asignaciones por acción).
Valores por defecto:

| Acción | Teclado y ratón | Mando |
| --- | --- | --- |
| Mover | WASD | Stick izquierdo |
| Cámara (girar) | Flechas, o mantener botón central del ratón y arrastrar (derecho si se activa en Opciones) | Stick derecho |
| Zoom de cámara | Rueda del ratón | — |
| Cambiar cámara (aérea / hombro) | V | Clic stick derecho |
| Saltar (bajo, ~0,7 m) | Espacio | A |
| Voltereta direccional | Ctrl izquierdo o derecho + WASD | Clic stick izquierdo + dirección |
| Correr (mantener) | Shift izquierdo o derecho | — |
| Espada (combo de 3) | J o clic izquierdo | X |
| Escudo / parry | K o clic derecho | LT |
| Fijar enemigo | Tab | RB |
| Arco (mantener y soltar) | F | Y |
| Bomba | B | B |
| Varita de fuego (gasta maná) | R | RT |
| Interactuar | E | LB |
| Ordenar a la pareja | C | Cruz abajo |
| Mapa grande | M | Cruz arriba |
| Tamaño del minimapa | N | Cruz izquierda |
| Inventario | I | Select |
| Panel de modo dev | F1 (solo con el modo dev activado) | — |
| Pausa | Esc | Start |

## Opciones

- **General:** volumen y modo desarrollador.
- **Cámara:** cámara actual, velocidad de giro, sensibilidad del ratón, suavizado del seguimiento, inversión de ejes, botón de giro y restablecer.
- **Controles:** reasignar cada acción (clic en la casilla, pulsa la tecla; Esc cancela, Supr borra).

## Mapa, inventario y modo dev

- **M** abre el mapa grande (arrastrar para mover, rueda para zoom de hasta x50, orientado al norte). En la isla el mapa se va
  descubriendo a medida que exploras (radio ~55 m), se guarda en disco por semilla y «Ver toda la isla» muestra el límite completo.
- **I** abre el inventario: equipo, materiales, descripción con tus teclas actuales y elección de armadura desbloqueada.
- **Modo dev** (Opciones → General): **F1** abre un panel con trucos (invulnerable, maná y munición infinitos, dar todo, curar, eliminar enemigos, armadura 1-4),
  aparición de objetos delante de ti (slimes, recogibles, enredaderas, cofres, rocas y muros agrietados, bombas, destello) y teletransportes.

## Guía del prólogo

1. Habla con tu pareja y toma la espada y el escudo del perchero.
2. Defiende el huerto (oeste) de 3 slimes. Prueba el **fijado** y el **parry**.
3. El anciano de la aldea (norte) te da el **arco**. Toca la campana (este) con una flecha.
4. De la campana cae una bolsa de **bombas**. Ve a las ruinas (este).
5. Cueva: vuela el **muro agrietado**, cruza el foso lanzando una bomba al **interruptor**,
   derrota la emboscada y vuela la **roca** final. Abre el cofre.
6. Vuelve a casa y usa la **mesa de trabajo**: armadura de **nivel 2** (más vida, aspecto nuevo).

## Isla (mundo abierto)

Botón **Isla (mundo abierto)** del menú principal. El campo de al lado es la semilla: con la misma
semilla sale siempre la misma isla; vacío = al azar. Empiezas con todo el equipo en un campamento
con tu pareja y exploras:

- El terreno se genera en chunks de 32 m alrededor del jugador (radio de 3 chunks) y los lejanos se descartan.
- Bosques, flores, rocas, slimes salvajes (lejos del campamento), cofres y ruinas con guardianes.
- Las **rocas con grietas naranjas** se rompen con bombas y sueltan bombas y gemas.
- El mar profundo no se cruza a pie: si te metes demasiado, vuelves a tierra.
- Lo ya abierto o roto no reaparece en esa partida; los slimes sí se regeneran al recargar la zona.

Código: `Assets/Scripts/World/Island/` (terreno, chunks y contenido).

## Estructura

```
Assets/
  Editor/            Menú del editor
  Scripts/
    Core/            Estado, input, vida, utilidades, sonido sintetizado, arranque
    Player/          Cámara, modelo 3D, movimiento, combate, flecha, bomba
    Enemies/         Slime, pareja, objetos recogibles
    World/           Mundo, cueva, puzles, director de misiones
      Island/        Isla procedural: terreno, chunks y contenido
    UI/              HUD, menú 3D, creador de personajes
```

## Estado

Incluido: creador de personajes (cuerpo, peinado, colores), armaduras por niveles con vista
previa, cámara elevada con fijado tipo Zelda, combo, escudo con parry, arco, bombas físicas,
pareja con IA y reglas anti-frustración, menú 3D, pausa y opciones.

Novedades: minimapa cuadrado (arriba a la derecha, gira con la cámara), dos cámaras intercambiables en cualquier momento
(aérea y sobre el hombro, con mira), cámara orbitable con el ratón, destello de luz al romper muros agrietados y
**varita de fuego**: la bola de fuego (R, 18 de maná) daña slimes y prende las **enredaderas** que sellan un cofre al este
de la casa (prólogo) y en cofres de la isla; el fuego se contagia entre matas. El maná se recupera solo y con golpes de espada y parries.

Pendiente: archipiélago y barco, ciclo día/noche, multijugador cooperativo, libros de magia, hielo,
niveles de armadura 3 y 4 en juego, guardado de partida.

## Actualización visual y movimiento

- Bosque con corteza, hojas recortadas, hierba con volumen y enredaderas quemables.
- Flores con pétalos, rocas angulares, slimes brillantes y translúcidos.
- Ruinas de piedra; cofres de madera con bandas oscuras, cerradura y cavidad interior.
- Arena, mar animado y mezclas graduales entre arena, pasto, roca y nieve en la isla.
- Carrera mantenida y voltereta con dirección relativa a la cámara. Sin dirección, rueda hacia delante.
- La primera carga migra Shift/Ctrl a los controles nuevos; después siguen siendo reasignables.

Los archivos principales de apariencia son `ForestLook.cs`, `WorldLook.cs`, `IslandSurfaces.cs`
y `Assets/Resources/ForestLook/`. Los controles están en `GameInput.cs`; carrera y voltereta
en `PlayerController.cs` y `CharacterModel.cs`. Para trabajar en paralelo, actualizar desde GitHub
antes de editar y usar ramas separadas cuando se cambien los mismos archivos.
