# SpiderStats

A statistics tracking mod for Spiderheck that monitors player performance and game events.


## Features

- **Extensive Player Statistics**: Tracks a wide range of player stats including kills, deaths, friendly kills, shield interactions, web swings, airborne time, kill streaks, and weapon-specific kills.
- **Enemy Tracking**: Counts enemies killed during survival gameplay
- **Player Titles**: At the end of each game, players are awarded fun titles based on their performance (e.g., "The Survivor", "Phantom Blade", "Lone Wolf")
- **In-Game Display**: 
  - Press **F1** to toggle the small stats display window
  - Press **F2** for the large stats overlay
  - Press **F3** to view player titles (only after the game ends)
- **Log File**: All the stats are saved to a local txt file when the game ends 
- **Configurable**: Using the Yaml file, various aspects of the mod can be changed
- **Updater**: The mod will automatically check for updates when the game starts
- **versus mode support**: The mod will track stats in versus mode, but as this was developed with the survival mode in mind, some features may not work as intended

### Small version:
<img width="2510" height="1393" alt="image" src="https://github.com/user-attachments/assets/70784ca7-bda0-478f-864d-ce39e5b6dae1" />

### Big version:
<img width="2520" height="1387" alt="image" src="https://github.com/user-attachments/assets/8e95019f-99c6-4e93-9c08-9b0f87a5fad4" />

## Installation

1. Install Silk Mod Loader: https://github.com/SilkModding/Silk
2. Download the mod dll file from [here](https://github.com/Dylan-Grinboju/spiderheck_stats_mod/releases) or from the sidebar in the Github page, or build the project yourself from this repo
3. Place the mod file in your Silk mods folder
4. Launch the game

## How to configure the Mod using the Yaml file:
The first time after launching the game with the mod installed, the following file will be created at this path: `...\Silk\Config\Mods\Stats_Mod.yaml`
```
display:
  showStatsWindow: true
  showPlayers: true
  showPlayTime: true
  showEnemyDeaths: true
  autoScale: true
  uiScale: 1
  bigUIOpacity: 100
  position:
    x: 10
    y: 10
tracking:
  enabled: true
  saveStatsToFile: true
updater:
  checkForUpdates: true
titles:
  enabled: true
  revealDelaySeconds: 2.0
```
After every change to the file, you need to relaunch the game for it to take effect. If you enter an incorrect value, for example 5 to a boolean field, the mod will throw a bunch of errors at you. You can change it back or just delete the file and it will be created again with the next game launch. 
Some explanation of the fields:

`showStatsWindow`: shows the mod UI. If false then pressing F1, F2, or F3 will do nothing. Note that the tracking will still work, it will just not show you anything.

`showPlayers`, `showPlayTime` and `showEnemyDeaths`: Exactly what this sounds. Will hide/show the corresponding parts of the UI.

`autoScale`: If true, the mod will automatically scale the UI based on your screen resolution.

`uiScale`: You can use this to manually scale the UI. 1 is normal size, 2 is double size, 0.5 is half size and so on. You can use this with conjunction with autoScale or by itself.

`bigUIOpacity`: Controls the transparency of the Big UI overlay (F2). Values range from 0 (fully transparent) to 100 (fully opaque). Default is 100. 

`position`: You can use this to manually set the position of the small UI. The coordinates are in pixels, with 0,0 being the top left corner of the screen. This value will be saved every time you click and drag the UI. If the UI doesn't show up on your screen, you can try setting the x and y values to 10 again.

`enabled`: If false, the mod will not track any stats. This is as if the mod is not installed at all.

`saveStatsToFile`: If true, the stats will be saved to a local txt file.

### Updater Configuration:

`checkForUpdates`: If true, the mod will automatically check for updates when the game starts.

### Titles Configuration:

`titles.enabled`: If true, player titles will be calculated and displayed at the end of each game. Press F3 to view them.

`titles.revealDelaySeconds`: Controls the delay between revealing each title card in the titles screen animation. Default is 2.0 seconds. Set to 0 for instant reveal.


## Notes and disclaimers
- Stats reset when starting a new game. After the game ending you can return to the lobby and the overlay will show the last game's stats
- By default, the mod saves the stats in a txt file in this path: `.../Silk/Logs/Spiderheck_stats_<Current_Date>.txt`
- There are some kills that won't register such as activating a weapon with another (hitting a grenade with a particle blade). This just complicates the logic and is up to interpretation of who gets the kill in difficult scenarios. Also, this is just very complicated to track, and I wasn't feeling like coding it
- This probably won't work with multiplayer, you are welcome to try and send me feedback, but I don't plan to support it anytime soon

## The Future
- I plan to continue expanding this mod with more fun titles and statistics
- I welcome any help or feedback! You can send me a message on Discord or Reddit, open a PR or try to send me a bug report using smoke signals. Some options are better than others

