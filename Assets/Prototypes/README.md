# Prototype Assets

This folder contains scenes and scripts that are not part of the production build.
The `GameJam.Prototypes` assembly is Editor-only, so its scripts are excluded from player builds.

- `Scenes/Game.unity` is an empty scene retained only for reference.
- `Takamasa/Takamasa.unity` is an unintegrated UI prototype.
- `Takamasa/TimerSystem.cs` and `Takamasa/ScoreChanger.cs` belong only to that prototype.

Production uses `Assets/Scenes/Title.unity` and `Assets/Scenes/Rema.unity`. Match timing,
results, and HUD updates are owned by `MatchSession`, `GameManager`, and `MatchUIBootstrap`.
