using HarmonyLib;
using UnityEngine;

namespace OffScreenIndicatorFix
{
    // The enemy's off-screen arrow is destroyed when the enemy dies, but Update keeps calling FollowTarget on it.
    // Enemies that die in lava stay active 0.2s longer, which threw a NullReferenceException every frame.
    [HarmonyPatch(typeof(OffScreenIndicator), "FollowTarget")]
    public class OffScreenIndicatorFollowTargetPatch
    {
        static bool Prefix(SpriteRenderer ____indicatorSprite)
        {
            return ____indicatorSprite != null;
        }
    }
}
