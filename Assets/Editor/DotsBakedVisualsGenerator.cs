using UnityEditor;

namespace Game.Editor
{
    /// <summary>Generates every baked Entity Graphics visual in dependency order.</summary>
    public static class DotsBakedVisualsGenerator
    {
        [MenuItem("Tools/DOTS/Generate All Baked Visuals")]
        private static void GenerateAll()
        {
            EnemyBakedVisualGenerator.Generate();
            ProjectileBakedVisualGenerator.Generate();
            CombatEffectBakedVisualGenerator.Generate();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
