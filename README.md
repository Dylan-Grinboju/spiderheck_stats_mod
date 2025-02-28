# Stats Mod for Spider Heck

A simple mod that tracks and displays how many enemies you've killed during your gameplay.

## Features

- Counts every enemy that is killed via the Disintegrate method
- Displays kill counter in the top-left corner of the screen
- Logs kill count to the console

## Installation

1. Install Silk Mod Loader
2. Place the mod files in your Spider Heck mods folder
3. Launch the game

## How it Works

This mod uses Harmony to patch the `Disintegrate` method of the `EnemyHealthSystem` class to count each enemy death.
