# StatsManager Implementation Summary

## Overview
Created a centralized `StatsManager` class that consolidates all statistics tracking functionality for the Spider Heck stats mod. This class acts as the single source of truth for all game statistics and provides a clean interface for the DisplayStats class.

## Key Features

### 1. Centralized Stats Management
- **StatsManager.cs**: New singleton class that manages all statistics
- Coordinates PlayerTracker and EnemiesTracker instances
- Handles survival session timing and game flow
- Provides unified data access through `GameStatsSnapshot`

### 2. Statistics Tracked
- **Survival Session Data**:
  - Current session timer (if active)
  - Last game duration
  - Total games played counter
  - Session start/stop management

- **Player Statistics** (via PlayerTracker):
  - Individual player deaths and kills
  - Player registration/unregistration
  - Total deaths and kills across all players

- **Enemy Statistics** (via EnemiesTracker):
  - Total enemies killed count

### 3. Data Snapshot System
The `GameStatsSnapshot` class provides a point-in-time view of all statistics:
```csharp
public class GameStatsSnapshot
{
    public bool IsSurvivalActive { get; set; }
    public TimeSpan CurrentSessionTime { get; set; }
    public TimeSpan LastGameDuration { get; set; }
    public int TotalGamesPlayed { get; set; }
    public Dictionary<PlayerInput, PlayerTracker.PlayerData> ActivePlayers { get; set; }
    public int TotalPlayerDeaths { get; set; }
    public int TotalPlayerKills { get; set; }
    public int EnemiesKilled { get; set; }
}
```

## Code Changes

### 1. New Files
- **StatsManager.cs**: Main statistics management class

### 2. Updated Files

#### PlayerTracker.cs
- Added new methods for StatsManager integration:
  - `GetActivePlayers()`: Returns copy of active players dictionary
  - `GetTotalPlayerDeaths()`: Aggregates all player deaths
  - `GetTotalPlayerKills()`: Aggregates all player kills
  - `IncrementPlayerDeath()`: Direct death increment method
  - `IncrementPlayerKill()`: Direct kill increment method

#### DisplayStats.cs
- Removed internal timer management fields (`isSurvivalActive`, `survivalStartTime`, `lastGameDuration`)
- Removed `StartSurvivalTimer()` and `StopSurvivalTimer()` methods
- Updated UI rendering to use `StatsManager.Instance.GetStatsSnapshot()`
- Simplified player and enemy statistics display logic

#### SurvivalModePatches.cs
- Updated patches to use `StatsManager` instead of calling individual trackers
- `StartGame` patch now calls `StatsManager.Instance.StartSurvivalSession()`
- `StopGameMode` patch now calls `StatsManager.Instance.StopSurvivalSession()`
- Cleaner, more centralized session management

#### EnemyDeathTypes.cs
- Updated enemy death tracking to use `StatsManager.Instance.IncrementEnemyKilled()`
- Updated player kill tracking to use `StatsManager.Instance.IncrementPlayerKill()`

## Benefits

### 1. Separation of Concerns
- **StatsManager**: Centralized data management and business logic
- **DisplayStats**: Pure UI presentation layer
- **Individual Trackers**: Focused on their specific tracking responsibilities

### 2. Data Consistency
- Single source of truth for all statistics
- Atomic operations for session management
- Consistent state across all components

### 3. Maintainability
- Clear data flow: Game Events → StatsManager → DisplayStats
- Easier to add new statistics (just extend StatsManager)
- Unified interface reduces coupling between components

### 4. Extensibility
- Easy to add new statistics types (wave numbers, special achievements, etc.)
- Simple to implement data persistence in the future
- Clean API for potential future features (leaderboards, analytics, etc.)

## Usage Example

The DisplayStats class now simply gets a snapshot and displays it:

```csharp
var statsSnapshot = StatsManager.Instance.GetStatsSnapshot();

// Display current/last game time
if (statsSnapshot.IsSurvivalActive)
{
    GUILayout.Label(FormatTimeSpan(statsSnapshot.CurrentSessionTime), timerStyle);
}
else
{
    GUILayout.Label(FormatTimeSpan(statsSnapshot.LastGameDuration), statusStyle);
}

// Display enemy kills
GUILayout.Label(statsSnapshot.EnemiesKilled.ToString(), killsStyle);

// Display player stats
foreach (var player in statsSnapshot.ActivePlayers)
{
    // Render player data...
}
```

This architecture provides a solid foundation for future enhancements while maintaining clean separation between data management and presentation.
