# Script Architecture

The runtime code is organized by responsibility instead of by scene.

## Core

- `Core/Match`: Match rules and state. `MatchSession` is plain C# and owns phase transitions, timers, rounds, results, and launch budgets.
- `GameManager` is the Unity-facing facade. Existing callers can keep using its public API and events.

## Player

- `Player/Input/PlayerController`: Input message receiver and compatibility facade only.
- `Player/Input/PlayerIdentity`: Side assignment and local input device pairing.
- `Player/Movement/PlayerContactSensor2D`: Ground and wall probes.
- `Player/Movement/PlayerMotor2D`: Horizontal movement, jumping, and input locking.
- `Player/Abilities/PlayerDash`: Dash state and physics override.
- `Player/Abilities/PlayerStun`: Stun reaction.
- `Player/Abilities/PlayerCannon`: Cannon selection and projectile launching.
- `Player/Abilities/PlayerDrawing`: Drawing cursor, shape selection, and placement input.

Add or remove player abilities as components. Avoid adding gameplay logic back into `PlayerController`.

## Gameplay

- `Gameplay/Projectiles`: Projectile behavior.
- `Gameplay/Drawing`: Drawing surfaces and generated collision stamps.
- `Gameplay/Goals`: Goal detection.
- `Gameplay/Stage`: Runtime stage construction, cannon mounts, and player spawning.

Scene builders create and connect components. Gameplay components should not depend on object names when a component reference can represent the relationship.

## Infrastructure And Presentation

- `Infrastructure/Input`: Player registration and shared input operations.
- `Infrastructure/Audio`: Persistent audio service entry point.
- `Presentation/HUD`: HUD creation, view rendering, and match-event binding.
- `SceneManagement`: Scene navigation commands.

## Dependency Direction

`Presentation / SceneManagement -> Core`

`Player / Gameplay -> Core`

`Bootstrap and factories -> concrete runtime components`

Keep `Core/Match/MatchSession` independent from Unity scene objects so match rules remain testable without loading a scene.
