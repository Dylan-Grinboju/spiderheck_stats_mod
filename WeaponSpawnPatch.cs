using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class WeaponSpawnPatchInput : MonoBehaviour
{
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        // Toggle patch on F3 key press
        if (keyboard != null && keyboard.f3Key.wasPressedThisFrame)
        {
            if (WeaponSpawnPatch.IsPatchApplied())
            {
                WeaponSpawnPatch.RemovePatch();
            }
            else
            {
                WeaponSpawnPatch.ApplyPatch();
            }
        }

        // // Cycle to next weapon with Shift+F3
        // if (Input.GetKeyDown(KeyCode.F3) && Input.GetKey(KeyCode.LeftShift))
        // {
        //     WeaponSpawnPatch.CycleToNextWeapon();
        // }
    }
}

public static class WeaponSpawnPatch
{
    // Set this to the weapon name you want to force spawn at all weapon spawners
    // Example: "ParticleBlade", "Railvolver", "Shotgun", etc.
    public static string ForceWeaponName = "ParticleBlade";

    private static bool _patchApplied = false;
    private static GameObject _cachedWeaponPrefab = null;
    private static WeaponSpawnPatchInput _inputHandler = null;
    private static int _currentWeaponIndex = 0;

    /// <summary>
    /// Initialize the F3 key handler. Call this once when your mod starts.
    /// </summary>
    public static void Initialize()
    {
        if (_inputHandler == null)
        {
            // Create a persistent GameObject to handle input
            GameObject inputObject = new GameObject("WeaponSpawnPatchInput");
            _inputHandler = inputObject.AddComponent<WeaponSpawnPatchInput>();
            UnityEngine.Object.DontDestroyOnLoad(inputObject);

            // Initialize weapon index based on current weapon name
            string[] weaponNames = GetAvailableWeaponNames();
            for (int i = 0; i < weaponNames.Length; i++)
            {
                if (weaponNames[i] == ForceWeaponName)
                {
                    _currentWeaponIndex = i;
                    break;
                }
            }

            Debug.Log("WeaponSpawnPatch: F3 key handler initialized. Press F3 to toggle weapon override, Shift+F3 to cycle weapons.");
        }
    }


    public static void ApplyPatch()
    {
        if (_patchApplied)
        {
            Debug.Log("WeaponSpawnPatch: Patch already applied");
            return;
        }

        try
        {
            GameObject weaponPrefab = GetWeaponPrefabByName(ForceWeaponName);
            if (weaponPrefab == null)
            {
                Debug.LogError($"WeaponSpawnPatch: Could not find weapon with name '{ForceWeaponName}'");
                return;
            }

            _cachedWeaponPrefab = weaponPrefab;

            // Find all weapon spawners and override their weapon
            WeaponSpawner[] weaponSpawners = UnityEngine.Object.FindObjectsOfType<WeaponSpawner>();

            if (weaponSpawners.Length == 0)
            {
                Debug.LogWarning("WeaponSpawnPatch: No weapon spawners found in the scene");
                return;
            }

            foreach (WeaponSpawner spawner in weaponSpawners)
            {
                spawner.overrideWeapon = weaponPrefab;
                // Reset the spawner to apply the new weapon immediately
                spawner.ResetWeaponSpawn();
            }

            _patchApplied = true;
            Debug.Log($"WeaponSpawnPatch: Successfully applied patch to {weaponSpawners.Length} weapon spawners. All will now spawn '{ForceWeaponName}'");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WeaponSpawnPatch: Error applying patch - {ex.Message}");
        }
    }

    public static void RemovePatch()
    {
        if (!_patchApplied)
        {
            Debug.Log("WeaponSpawnPatch: No patch to remove");
            return;
        }

        try
        {
            WeaponSpawner[] weaponSpawners = UnityEngine.Object.FindObjectsOfType<WeaponSpawner>();

            foreach (WeaponSpawner spawner in weaponSpawners)
            {
                spawner.overrideWeapon = null;
                spawner.ResetWeaponSpawn();
            }

            _patchApplied = false;
            _cachedWeaponPrefab = null;
            Debug.Log($"WeaponSpawnPatch: Successfully removed patch from {weaponSpawners.Length} weapon spawners. Normal spawning restored.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"WeaponSpawnPatch: Error removing patch - {ex.Message}");
        }
    }


    public static void ChangeWeapon(string weaponName)
    {
        ForceWeaponName = weaponName;

        // Update the weapon index to match the new weapon name
        string[] weaponNames = GetAvailableWeaponNames();
        for (int i = 0; i < weaponNames.Length; i++)
        {
            if (weaponNames[i] == weaponName)
            {
                _currentWeaponIndex = i;
                break;
            }
        }

        if (_patchApplied)
        {
            RemovePatch();
            ApplyPatch();
        }
        else
        {
            Debug.Log($"WeaponSpawnPatch: Weapon name changed to '{weaponName}'. Apply patch to use it.");
        }
    }

    /// <summary>
    /// Cycle to the next weapon in the list and apply it if patch is active
    /// </summary>
    public static void CycleToNextWeapon()
    {
        string[] weaponNames = GetAvailableWeaponNames();
        if (weaponNames.Length == 0) return;

        _currentWeaponIndex = (_currentWeaponIndex + 1) % weaponNames.Length;
        string newWeaponName = weaponNames[_currentWeaponIndex];

        ForceWeaponName = newWeaponName;
        Debug.Log($"WeaponSpawnPatch: Cycled to weapon '{newWeaponName}' ({_currentWeaponIndex + 1}/{weaponNames.Length})");

        if (_patchApplied)
        {
            RemovePatch();
            ApplyPatch();
        }
    }

    private static GameObject GetWeaponPrefabByName(string weaponName)
    {
        // Try to parse the weapon name to SerializationWeaponName enum
        if (!Enum.TryParse<SerializationWeaponName>(weaponName, out SerializationWeaponName targetWeaponName))
        {
            Debug.LogError($"WeaponSpawnPatch: '{weaponName}' is not a valid weapon name. Valid names are: {string.Join(", ", Enum.GetNames(typeof(SerializationWeaponName)))}");
            return null;
        }

        // Check VersusMode weapons list
        if (VersusMode.instance != null && VersusMode.instance.weapons != null)
        {
            foreach (var spawnableWeapon in VersusMode.instance.weapons)
            {
                if (spawnableWeapon.weaponObject != null)
                {
                    Weapon weaponComponent = spawnableWeapon.weaponObject.GetComponent<Weapon>();
                    if (weaponComponent != null && weaponComponent.serializationWeaponName == targetWeaponName)
                    {
                        return spawnableWeapon.weaponObject;
                    }
                }
            }
        }

        Debug.LogError($"WeaponSpawnPatch: Could not find weapon prefab for '{weaponName}' in VersusMode weapons list");
        return null;
    }

    public static string[] GetAvailableWeaponNames()
    {
        return Enum.GetNames(typeof(SerializationWeaponName));
    }


    public static bool IsPatchApplied()
    {
        return _patchApplied;
    }


    public static string GetCurrentWeaponName()
    {
        return ForceWeaponName;
    }
}