# Silk 0.6 YAML Configuration Guide

## Overview

With Silk 0.6, your Stats Mod now supports user-configurable settings through YAML files. This allows users to customize the mod's behavior without needing to modify code or recompile.

## How It Works

### 1. **Automatic Configuration File Creation**

When your mod runs for the first time with Silk 0.6, it automatically creates a configuration file at:
```
Spiderheck/Silk/Config/Mods/Stats_Mod.yaml
```

### 2. **Default Values Setup**

In your mod's `Initialize()` method, you define default configuration values:

```csharp
private void SetupConfiguration()
{
    var defaultConfig = new Dictionary<string, object>
    {
        { "display", new Dictionary<string, object>
            {
                { "showStats", true },
                { "showKillCount", true },
                { "showDeathCount", true },
                { "position", new Dictionary<string, object>
                    {
                        { "x", 10 },
                        { "y", 10 }
                    }
                }
            }
        },
        { "tracking", new Dictionary<string, object>
            {
                { "enabled", true },
                { "saveStatsToFile", true }
            }
        },
        { "keybinds", new Dictionary<string, object>
            {
                { "toggleStats", "F1" },
                { "resetStats", "F2" }
            }
        }
    };

    Config.LoadModConfig(ModId, defaultConfig);
}
```

### 3. **Configuration Helper Class**

The `ModConfig.cs` file provides easy access to configuration values:

```csharp
public static class ModConfig
{
    // Easy property access
    public static bool ShowStats => Config.GetModConfigValue<bool>(ModId, "display.showStats", true);
    public static int DisplayPositionX => Config.GetModConfigValue<int>(ModId, "display.position.x", 10);
    
    // Methods to update values at runtime
    public static void SetShowStats(bool value)
    {
        Config.SetModConfigValue(ModId, "display.showStats", value);
    }
}
```

### 4. **Key Features**

#### **Nested Configuration Structure**
- Use dot notation: `"display.position.x"`
- Supports deep nesting for organized settings

#### **Type Safety**  
- Automatic type conversion: `GetModConfigValue<bool>()`, `GetModConfigValue<int>()`
- Default values if configuration is missing

#### **Runtime Updates**
- Changes save immediately to the YAML file
- Example: Window position saves when user drags the stats display

#### **Merge Strategy**
- User changes are preserved
- New default values are added automatically
- Missing user settings fall back to defaults

## User Experience

### For Users:
1. **First Run**: Configuration file is created with sensible defaults
2. **Customization**: Users edit the YAML file with any text editor
3. **Live Updates**: Some settings (like window position) update automatically
4. **Preserved Settings**: User customizations survive mod updates

### Example User Workflow:
1. Install and run the Stats Mod
2. Find the generated YAML file at `Spiderheck/Silk/Config/Mods/Stats_Mod.yaml`
3. Edit settings:
   ```yaml
   display:
     showStats: true
     showKillCount: false    # Hide enemy kill count
     position:
       x: 100               # Move to different position
       y: 50
   
   keybinds:
     toggleStats: "TAB"      # Use TAB instead of F1
   ```
4. Restart the game or use in-game reload (if implemented)

## Benefits of This Approach

### **For Mod Developers:**
- **Easy Implementation**: Just a few lines of code to add configurability  
- **Automatic File Management**: Silk handles YAML serialization/deserialization
- **Type Safety**: Strong typing prevents configuration errors
- **Maintainable**: Clean separation between code and configuration

### **For Users:**
- **No Code Changes**: Modify behavior without programming
- **Human Readable**: YAML is easy to understand and edit
- **Backup Friendly**: Configuration files can be easily shared or backed up
- **Flexible**: Support for complex nested configurations

### **For the Community:**
- **Standardized**: All Silk mods use the same configuration system
- **Shareable**: Users can share configuration presets
- **Tool Support**: Third-party configuration managers could be built

## Migration from Silk 0.5 and Earlier

If you're upgrading from an older version of Silk:

1. **Add Configuration Setup**: Include `SetupConfiguration()` in your mod's `Initialize()` method
2. **Create Helper Class**: Add `ModConfig.cs` for easy access to settings  
3. **Update Code**: Replace hardcoded values with configuration calls
4. **Test**: Verify that configuration changes work as expected

## Advanced Features

### **Dynamic Configuration**
Settings can be changed at runtime and immediately saved:
```csharp
// When user drags window, save new position
ModConfig.SetDisplayPosition((int)newPosition.x, (int)newPosition.y);
```

### **Validation**
You can add validation in your helper methods:
```csharp
public static void SetDisplayPosition(int x, int y)
{
    // Clamp to screen bounds
    x = Mathf.Clamp(x, 0, Screen.width - 300);
    y = Mathf.Clamp(y, 0, Screen.height - 200);
    
    Config.SetModConfigValue(ModId, "display.position.x", x);
    Config.SetModConfigValue(ModId, "display.position.y", y);
}
```

### **Complex Data Types**
The system supports complex nested structures for advanced configuration needs.

## Summary

The Silk 0.6 YAML configuration system makes your mods more user-friendly and maintainable. With just a few lines of code, you can give users powerful customization options while keeping your mod's code clean and organized.
