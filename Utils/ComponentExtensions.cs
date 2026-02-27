using UnityEngine;

namespace StatsMod;

public static class ComponentExtensions
{
    public static T GetComponentOrParent<T>(this GameObject gameObject) where T : Component
    {
        if (gameObject is null) return null;
        return gameObject.GetComponent<T>() ?? gameObject.GetComponentInParent<T>();
    }

    public static T GetComponentOrParent<T>(this Component component) where T : Component
    {
        if (component is null) return null;
        return component.GetComponent<T>() ?? component.GetComponentInParent<T>();
    }
}
