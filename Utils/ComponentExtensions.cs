using UnityEngine;

namespace StatsMod
{
    public static class ComponentExtensions
    {
        public static T GetComponentOrParent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null) return null;
            return gameObject.GetComponent<T>() ?? gameObject.GetComponentInParent<T>();
        }

        public static T GetComponentOrParent<T>(this Component component) where T : Component
        {
            if (component == null) return null;
            return component.GetComponent<T>() ?? component.GetComponentInParent<T>();
        }
    }
}
