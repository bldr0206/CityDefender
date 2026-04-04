# Scene Workspace

Editor bootstrap (scenes, `ProjectContext`, MVP level assets) is **not** run automatically on Unity load. Use **Unity MCP** or the menu when you intentionally want to (re)generate:

- `Tools/Color Charge TD/Generate Bootstrap Scenes` — creates missing scenes/prefab wiring; **overwrites** `ProjectContext.prefab` if you run it again.
- `Tools/Color Charge TD/Generate MVP Level 1` — full MVP content + layout prefab; **overwrites** those assets if you run it again.

After the first generation, edit assets in the Editor; avoid re-running these menus unless you mean to reset.

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
