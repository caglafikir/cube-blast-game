# Dream Games – Software Engineering Case Study

A level-based cube-blast puzzle game (Unity 6000.3.10f1, C#, built-in renderer),
implemented per the case study brief.

## 1. First-time setup (do this before pressing Play)

1. Open this folder as a project in **Unity Hub** with editor version **6000.3.10f1**.
2. Once the project finishes importing, open the menu **Dream Games → Setup Project
   (Build Scenes and Prefabs)**.
   - No art assets were supplied with the case study, so this script generates simple
     placeholder square sprites (`Assets/GeneratedArt`), builds the item prefabs
     (`Assets/Prefabs`), and builds **MainScene** and **LevelScene** from scratch:
     canvases, camera, LevelButton, HUD, fail popup, win celebration, and every
     script reference wired up automatically. It also registers both scenes in
     Build Settings.
   - It is safe to re-run at any time; it overwrites the generated art/prefabs/scenes.
3. Open `Assets/Scenes/MainScene.unity` and press **Play**.

Swap the placeholder sprites for real art any time by replacing the files in
`Assets/GeneratedArt` (keep the same file names) or by editing the prefabs directly.

## 2. Editor menu items

- **Dream Games → Setup Project (Build Scenes and Prefabs)** — one-click scene/prefab
  bootstrapper described above.
- **Dream Games → Set Last Played Level...** — the required editor tool for setting the
  persisted last-played level number. Works whether or not the game is in Play Mode
  (it writes straight to the same `PlayerPrefs` key the game reads).

## 3. Project structure

```
Assets/
  Scripts/
    Core/          CubeColor, SpecialItemType, DamageContext, SceneNames, InputController
    Grid/          GridManager, MatchFinder (flood-fill), FallController (hand-coded gravity),
                   BlastController (tap → blast/special/combo resolution), ComboShapes,
                   ItemPrefabRegistry
    Items/         GridItem (base), IDamageable, Cube
      Obstacles/   Obstacle (base), Vase, Stone, ChaliceBoxPart + ChaliceBoxState
      SpecialItems/ SpecialItem (base), Rocket, Tnt
    Level/         LevelData, LevelTokens, LevelLoader, LevelManager
    UI/            MainSceneController, LevelHudController, FailPopupController,
                   WinCelebrationController
    Persistence/   GameProgress (PlayerPrefs wrapper: last played level)
    Utils/         TweenUtil (coroutine-based move/scale/punch tweening)
  Editor/          SetLastPlayedLevelWindow, ProjectSceneBootstrapper
  StreamingAssets/Levels/  level_01.json … level_10.json
```

The code follows OOP throughout: `GridItem` is the shared base for everything that can
sit in a grid cell; `Obstacle` and `SpecialItem` are abstract bases with polymorphic
`TakeDamage` / `Explode` behaviour so new obstacle or special-item types can be added
without touching `BlastController`; `IDamageable` decouples "who can be damaged" from
"what damages them". Movement/animation goes through a single `TweenUtil` seam, so a
third-party tween library could be dropped in later without touching gameplay code.

## 4. Gameplay implementation notes

- **Matching**: `MatchFinder` flood-fills 4-directionally connected same-color cubes.
  Groups of 2–3 blast normally; 4–5 spawn a random-orientation Rocket; 6+ spawn a TNT,
  both created on the tapped cell as described in the brief.
- **Falling/refill**: implemented entirely in code (`FallController`), not Physics or
  the Animation system. Each column compacts downward; fixed obstacles (`CanFall ==
  false`, i.e. Stone / Chalice Box) split a column into segments that items can't fall
  through, matching "cubes cannot fall through other cubes, special items, or
  obstacles." New cubes spawn above the topmost segment and fall in.
- **Special items**: Rockets damage their full row/column; TNT damages a 5×5 area.
  Adjacent special items combo instead of exploding individually (handled in
  `BlastController.ProcessSpecialTap` + `ComboShapes`).
- **Obstacles**: `Vase` (2 HP, damaged by blast or explosion, falls), `Stone` (1 HP,
  only special-item explosions, doesn't fall), `ChaliceBox` (2×2, door phase → chalice
  phase, goal 10 chalices) are all implemented per the brief.

## 5. Assumptions made where the brief left room for interpretation

- **TNT-Rocket combo** ("3×3 array of exploding rockets in a + shape"): implemented as
  3 full rows (center row ±1) and 3 full columns (center column ±1) — a thick cross of
  9 line-rockets. See `ComboShapes.TntRocket`.
- **Rocket-Rocket combo**: one full row + one full column through the tapped cell.
- **TNT-TNT combo**: 7×7 area centered on the tapped cell.
- **Chalice Box door phase**: a single hit destroys the door (`doorHealth = 1`),
  since the brief doesn't specify a door HP value; this is configurable in
  `ChaliceBoxState`'s constructor.
- **Special item hint icon**: for a same-color group ≥ 4, a hint icon is shown (rocket
  or TNT depending on eventual group size) but the concrete created type is still only
  decided at blast time, matching "eligible to create one."
- Cells left empty under a **Stone**/**Chalice Box** that can no longer be reached from
  above the grid stay empty (correct physical behaviour, and levels are built so this
  doesn't happen in practice).

## 6. Persistence

`GameProgress` (plain C# static class over `PlayerPrefs`) stores the last-played level
(1–10, or 11 meaning "all levels finished"). `MainSceneController` reads it to show
either `Level N` or the finished text on the `LevelButton`.

## 7. Submission

Per the brief: push this project to a **private GitHub repository** and share it with
`software.engineering.study@dreamgames.com`.
