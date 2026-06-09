# Blast Game

A portrait 2D level-based blast game built in Unity.

## Gameplay

- Tap groups of 2+ same-color cubes to blast them
- Groups of 4+ create rockets that explode in a row or column
- Adjacent rockets combine into a powerful 3x3 combo
- Clear all obstacles (Boxes, Stones, Vases) within the move limit to win

## Features

- 10 handcrafted JSON-driven levels with increasing difficulty
- 4 cube colors (Red, Green, Blue, Yellow)
- 3 obstacle types with unique mechanics:
  - **Box** — 1 HP, damaged by adjacent blasts and rockets
  - **Stone** — 1 HP, damaged only by rockets
  - **Vase** — 2 HP, can fall, sprite changes when damaged
- Rocket creation from large groups, with directional explosion
- Rocket-Rocket combo (3x3 area explosion)
- Code-driven gravity and cube refill system
- Persistent level progression (PlayerPrefs)
- Win celebration with particles and lose popup with retry
- Dynamic grid scaling for portrait orientation (9:16)
- Editor tool to set current level number

## Tech Stack

- **Engine:** Unity 6000.0.59f2
- **Language:** C#
- **Renderer:** Unity Built-in Renderer
- **Tweening:** DOTween (free)
- **Orientation:** Portrait (9:16)

## Architecture

```
MainScene (UI)
  └── MainMenuUI → LevelButton → loads LevelScene

LevelScene (Gameplay)
  ├── LevelLoader       — JSON parsing
  ├── BoardManager      — Grid coordination & turn pipeline
  ├── ItemFactory       — Prefab instantiation
  ├── MatchFinder       — BFS flood-fill for groups
  ├── GravitySystem     — Column collapse logic
  ├── CubeSpawner       — Random cube refill
  └── InputHandler      — Touch/click to grid mapping

GameManager (Singleton)
  └── Scene transitions, level persistence
```

### Item Hierarchy

```
BoardItem (abstract base)
├── Cube      — Blastable, falls, shows rocket hint for groups ≥ 4
├── Rocket    — Falls, explodes directionally, supports combos
├── Box       — Static, 1 HP
├── Stone     — Static, 1 HP, rocket-only damage
└── Vase      — Falls, 2 HP, max 1 blast damage per turn
```

## Project Structure

```
Assets/
├── Art/            — Sprites (cubes, rockets, obstacles, UI)
├── Fonts/          — Noah Text Black SDF
├── Plugins/        — DOTween
├── Prefabs/        — Board, Items, UI prefabs
├── Resources/
│   └── Levels/     — level_01.json – level_10.json
├── Scenes/
│   ├── MainScene   — Main menu (build index 0)
│   └── LevelScene  — Gameplay (build index 1)
└── Scripts/
    ├── Core/       — GameManager, LevelLoader, PlayerPrefsManager
    ├── Data/       — LevelData, Enums
    ├── Board/      — BoardManager, Cell, MatchFinder, GravitySystem, CubeSpawner, InputHandler
    ├── Items/      — BoardItem, Cube, Rocket, Box, Stone, Vase, ItemFactory
    ├── UI/         — MainMenuUI, GameplayHUD, FailPopup, CelebrationManager
    └── Editor/     — LevelEditorMenu
```

## How to Run

1. Open the project in **Unity 6000.0.59f2**
2. If DOTween prompts setup, click **Tools → Demigiant → DOTween Utility Panel → Setup DOTween**
3. Open **Assets/Scenes/MainScene** in the Hierarchy
4. Press **Play** in MainScene

> **Note:** Both scenes are already configured in Build Settings. The Game view is automatically set to 9:16 portrait on first launch. No additional setup is needed.

## Level Overview

| Level | Grid | Moves | Obstacles |
|-------|------|-------|-----------|
| 1 | 9×10 | 20 | 27 Boxes |
| 2 | 10×7 | 15 | 20 Stones |
| 3 | 9×8 | 30 | Vases |
| 4 | 7×8 | 17 | Stones + Boxes |
| 5 | 9×9 | 23 | Vases + Pre-placed Rockets |
| 6 | 8×9 | 23 | 16 Vases |
| 7 | 9×8 | 24 | Stones + Boxes + Vases |
| 8 | 9×7 | 18 | Stones + Boxes + Vases |
| 9 | 6×9 | 20 | Vases |
| 10 | 10×8 | 10 | Boxes (only blue cubes) |
