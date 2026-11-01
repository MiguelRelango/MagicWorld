# Personaje articulado

Implementado en el proyecto local MagicWorld:

- Huesos de caderas, columna, pecho, cuello, cabeza, hombros, brazos, antebrazos, manos, muslos, pantorrillas, pies y dedos de los pies.
- Extremidades separadas con articulaciones visibles y animación provisional suavizada al caminar, correr, saltar y rodar.
- Armas y escudo sujetos a las manos. Poses de combate conservan el sistema de daño existente.
- Cápsula de colisión ajustada al tipo de cuerpo; las piedras pequeñas ahora tienen colisión con su malla.
- Preparación de un Avatar Humanoid; si no resulta válido, continúa la animación provisional y Unity muestra una advertencia.

No es un ragdoll: las articulaciones animan el modelo y la cápsula controla el movimiento físico. El modelo sigue formado por piezas rígidas; no incluye una piel continua deformable.

## Añadir Human Basic Motions FREE

1. Añadir el paquete a la cuenta desde https://assetstore.unity.com/packages/3d/animations/human-basic-motions-free-154271.
2. En Unity, abrir Package Manager → My Assets, buscar el paquete, descargarlo e importarlo.
3. Abrir **Hearthbound → Configurar animaciones Humanoid**.
4. Asignar clips Humanoid de Idle, Walk y Run; Jump es opcional. Usar variantes in-place y activar Loop Time para los tres primeros.
5. Pulsar **Crear controlador de locomoción** y reiniciar Play. El controlador se guarda en `Assets/Resources/CharacterMotion/HumanLocomotion.controller`.
6. Si el controlador ya existe, la herramienta lo selecciona sin sobrescribirlo; se puede editar desde Animator.

El desplazamiento queda a cargo de PlayerController (root motion desactivado). La voltereta y las poses de combate permanecen procedurales. Si no hay clip de salto, se usa la pose provisional en el aire.

## Verificación

Compilación C# con las bibliotecas de Unity 6000.3.15f1: sin errores; cuatro advertencias preexistentes de variables sin uso.
Pendiente comprobar en Play: validez del Avatar Humanoid, proporciones, agarre de armas, colisión con rocas y aspecto de las animaciones. El paquete externo todavía no está importado ni sus clips han sido probados.

Respaldo de los cuatro scripts anteriores: `Respaldo-antes-de-articulaciones.zip`.

Archivos principales: `CharacterJoints.cs`, `CharacterModel.cs`, `PlayerController.cs`, `PlayerCombat.cs`, `WorldLook.cs` y `HumanMotionSetup.cs`.
