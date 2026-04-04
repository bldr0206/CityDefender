# Scene Workspace

Editor bootstrap (scenes, `ProjectContext`) is **not** run automatically on Unity load. Use **Unity MCP** or the menu when you intentionally want to (re)generate:

- `Tools/Color Charge TD/Generate Bootstrap Scenes` — creates missing scenes/prefab wiring; **overwrites** `ProjectContext.prefab` if you run it again.

Level layouts and waves are authored as normal assets under `Assets/_Game/Content` and `Assets/_Game/Prefabs/Layouts`.

---

Expected system scenes:
- `Boot`
- `MainMenu`
- `Meta`
- `Battle`

Recommended setup:
- `Boot` contains `ProjectContext`, `ProjectInstaller`, `BootSceneInstaller`, `BootSceneEntryPoint`.
- `MainMenu` contains `SceneContext`, `MainMenuSceneInstaller`, root menu screens, `SceneNavigationBridge`.
- `Meta` contains `SceneContext`, `MetaSceneInstaller`, result/upgrades/shop screens, `ResultScreenBridge`.
- `Battle` contains `SceneContext`, `BattleSceneInstaller`, `LevelSessionController`, HUD, and a layout spawn root.
