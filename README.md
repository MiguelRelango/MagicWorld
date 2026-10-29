# Hearthbound — prólogo jugable (Unity 6)

Action-RPG 3D de aventuras con puzles al estilo *Alundra* y atmósfera cálida y estilizada.
Este repositorio contiene el **prólogo** descrito en el GDD: casa, aldea, primer combate, arco,
bombas y la cueva con el puzle de muros agrietados.

Todo el mundo, los personajes y los efectos se generan **por código con primitivas**:
no hay prefabs, modelos ni audio que importar.

## Cómo probarlo

1. En Unity Hub crea un proyecto **3D** (Unity 6, plantilla *Universal 3D* o *3D (Built-In)*).
2. Copia la carpeta `Assets/` de este repositorio dentro de la carpeta `Assets/` de tu proyecto.
3. Espera a que Unity compile. Pulsa **Play** en cualquier escena: el juego se inicia solo.
4. Opcional: menú **Hearthbound → Crear escena del prólogo** guarda una escena lista,
   y **Hearthbound → Comprobar configuración** muestra el pipeline y el sistema de input.

Funciona con el *Input System* nuevo y con el Input Manager clásico.

> Sin compilador de Unity en el entorno donde se escribió, el código se revisó a mano.
> Si Unity muestra algún error de compilación al importar, copia el mensaje y se corrige.

## Controles

| Acción | Teclado y ratón | Mando |
| --- | --- | --- |
| Mover | WASD | Stick izquierdo |
| Cámara | Flechas izquierda / derecha | Stick derecho |
| Espada (combo de 3) | J o clic izquierdo | X |
| Escudo / parry | K o clic derecho | LT |
| Esquivar | Espacio | A |
| Fijar enemigo | Tab | RB |
| Arco (mantener y soltar) | F | Y |
| Bomba | B | B |
| Interactuar | E | LB |
| Ordenar a la pareja | C | Cruz abajo |
| Pausa | Esc | Start |

## Guía del prólogo

1. Habla con tu pareja y toma la espada y el escudo del perchero.
2. Defiende el huerto (oeste) de 3 slimes. Prueba el **fijado** y el **parry**.
3. El anciano de la aldea (norte) te da el **arco**. Toca la campana (este) con una flecha.
4. De la campana cae una bolsa de **bombas**. Ve a las ruinas (este).
5. Cueva: vuela el **muro agrietado**, cruza el foso lanzando una bomba al **interruptor**,
   derrota la emboscada y vuela la **roca** final. Abre el cofre.
6. Vuelve a casa y usa la **mesa de trabajo**: armadura de **nivel 2** (más vida, aspecto nuevo).

## Estructura

```
Assets/
  Editor/            Menú del editor
  Scripts/
    Core/            Estado, input, vida, utilidades, sonido sintetizado, arranque
    Player/          Cámara, modelo 3D, movimiento, combate, flecha, bomba
    Enemies/         Slime, pareja, objetos recogibles
    World/           Mundo, cueva, puzles, director de misiones
    UI/              HUD, menú 3D, creador de personajes
```

## Estado

Incluido: creador de personajes (cuerpo, peinado, colores), armaduras por niveles con vista
previa, cámara elevada con fijado tipo Zelda, combo, escudo con parry, arco, bombas físicas,
pareja con IA y reglas anti-frustración, menú 3D, pausa y opciones.

Pendiente (fuera del prólogo): multijugador cooperativo, varitas y libros de magia, hielo y fuego,
niveles de armadura 3 y 4 en juego, guardado de partida.
