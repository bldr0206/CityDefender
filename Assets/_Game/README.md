# Color Charge TD Game Layer

This folder is the working area for gameplay, UI flow, and designer-authored content.

## Authoring Rules
- Put all reusable gameplay data in `ScriptableObject` assets, not in scene-only component values.
- Add new levels through `LevelDefinition + layout prefab + wave definition`, then register them in `LevelCatalogDefinition`.
- Keep combat logic inside runtime C# classes and use `MonoBehaviour` components only as Unity-facing adapters.
- Use `Tools/Color Charge TD/Validate Content` before committing content changes.

## Recommended Content Flow
1. Create or duplicate a layout prefab with `LevelLayoutAuthoring`.
2. Create a `WaveDefinition`.
3. Create a `LevelDefinition` with a unique `levelId`.
4. Add the level to `LevelCatalogDefinition`.
5. Validate content and test the level through the shared `Battle` scene flow.

## Scene Intent
- `Boot`: load profile and hand off to the main menu flow.
- `MainMenu`: root navigation and level selection entry.
- `Meta`: result, upgrades, and shop-facing shell.
- `Battle`: single shared gameplay scene that consumes `levelId`.
