using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Owns the single UGUI canvas shared by all runtime HUD and selection elements.</summary>
    public sealed class GameUICanvas : MonoBehaviour
    {
        public static Transform GetOrCreate(Transform owner)
        {
            var existing = FindAnyObjectByType<GameUICanvas>(FindObjectsInactive.Include);
            if (existing != null)
                return existing.transform;
            Debug.LogError("GameUICanvas: Place the GameHUD prefab in the active scene.", owner);
            return null;
        }
    }
}
