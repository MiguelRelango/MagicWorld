# Crafteo: primera versión

Hay un banco de trabajo junto a la casa de la aldea (8, 0, -7) y otro en el campamento de la isla. Acercarse y pulsar la tecla de interacción (E por defecto).

Recetas:

| Resultado | Coste | Requisito |
| --- | --- | --- |
| 6 flechas | 3 madera | Arco desbloqueado y espacio hasta el límite de 30 |
| 2 bombas | 4 madera + 2 gemas | Bombas desbloqueadas y espacio hasta el límite de 20 |

Junto al banco hay madera: permite recoger 2 unidades cada 5 segundos. Las gemas proceden del botín existente.
Cada fabricación tarda 3 segundos. Los materiales se descuentan solo al completar. Cancelar o recibir daño interrumpe sin gastar materiales. Pausar detiene el progreso. Durante el trabajo se bloquean movimiento, combate e interacciones y se ocultan las armas.
El inventario sigue usando el estado de partida existente; este cambio no añade guardado persistente de inventario entre sesiones.

## Animaciones del paquete

Human Crafting Animations FREE todavía está únicamente en My Assets y no está importado en este proyecto. Se usa una animación articulada provisional.

1. Abrir Package Manager → My Assets, seleccionar Human Crafting Animations FREE y pulsar Download e Import.
2. Abrir Hearthbound → Configurar animación de crafteo.
3. Arrastrar un clip Humanoid de trabajo de pie, apropiado para banco de trabajo y preferiblemente in-place.
4. Pulsar Asignar al banco de trabajo y reiniciar Play.

La configuración se guarda en Assets/Resources/CharacterMotion/CraftingMotion.asset. El clip se reproduce mediante Playables sobre el Avatar Humanoid; si no hay clip o el avatar no es válido, se mantiene la animación provisional. La posición de las manos respecto al banco requiere ajuste visual con el clip elegido.

## Validación

Compilación C# con bibliotecas Unity 6000.3.15f1: sin errores. Prueba ejecutable de las funciones reales CanCraft/TryComplete: 3696 combinaciones de recursos, desbloqueos y capacidad pasaron; también se comprobó rechazo de recetas inválidas y de una segunda fabricación sin materiales.

Pendientes en Unity: funcionamiento del menú, interacción, pausa, interrupción por daño y reproducción/retargeting del clip importado. No se ha ejecutado una prueba de juego ni se han importado o probado los archivos del paquete.

Respaldo: Respaldo-antes-de-crafteo.zip.
