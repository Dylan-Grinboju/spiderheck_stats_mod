# Stats Mod for Spider Heck

A comprehensive statistics tracking mod for Spider Heck that monitors player performance and game events.

## Features

- **Enemy Tracking**: Counts enemies killed during gameplay
- **Player Statistics**: Monitors player deaths and active player counts
- **Survival Mode Timer**: Tracks session duration in survival mode
- **In-Game Display**: Press F1 to toggle stats display window
- **Session History**: Records your last survival session time

## Installation

1. Install Silk Mod Loader
2. Place the mod files in your Spider Heck mods folder
3. Launch the game

## Usage

- Press F1 to show/hide the statistics window
- All stats are tracked automatically during gameplay
- Stats reset when starting a new survival mode session

## Technical Details

This mod uses Harmony to patch various game methods including `EnemyHealthSystem.Explode`, `SurvivalMode.StartGame`, and `SpiderHealthSystem.ExplodeInDirection` to collect gameplay statistics.
