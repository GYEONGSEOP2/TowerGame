using System.IO;
using Game.DOTS;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    /// <summary>Creates static Entity Graphics prefabs for the explosion and slow-status rings.</summary>
    public static class CombatEffectBakedVisualGenerator
    {
        private const string OutputFolder = "Assets/Generated/CombatEffectRendering";
        private const string PrefabFolder = "Assets/Prefabs/Effects";
        private const string CombatPrefabFolder = "Assets/Prefabs/Combat";
        private const string ProjectilePrefabPath = "Assets/Prefabs/Projectiles/ProjectileVisual.prefab";
        private const string ExplosionPrefabPath = PrefabFolder + "/ExplosionVisual.prefab";
        private const string SlowEffectPrefabPath = PrefabFolder + "/SlowEffectVisual.prefab";
        private const string CombatVisualsPrefabPath = CombatPrefabFolder + "/TowerCombatVisuals.prefab";

        [MenuItem("Tools/DOTS/Generate Baked Combat Effect Visuals")]
        public static void Generate()
        {
            EnsureFolder(OutputFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(CombatPrefabFolder);

            CreateEffectPrefab<ExplosionAuthoring>(
                "ExplosionVisual",
                "ExplosionRing",
                new Color(1f, 0.25f, 0.1f),
                OutputFolder + "/ExplosionRing.asset",
                OutputFolder + "/ExplosionRing.mat",
                ExplosionPrefabPath);
            CreateEffectPrefab<SlowEffectAuthoring>(
                "SlowEffectVisual",
                "SlowEffectRing",
                new Color(0.2f, 0.75f, 1f),
                OutputFolder + "/SlowEffectRing.asset",
                OutputFolder + "/SlowEffectRing.mat",
                SlowEffectPrefabPath);

            CreateCombatVisualsPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateCombatVisualsPrefab()
        {
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            var explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);
            var slowEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlowEffectPrefabPath);
            if (projectilePrefab == null || explosionPrefab == null || slowEffectPrefab == null)
            {
                Debug.LogError("CombatEffectBakedVisualGenerator: Generate the baked projectile visual before generating combat visuals.");
                return;
            }

            var root = new GameObject("TowerCombatVisuals");
            try
            {
                var authoring = root.AddComponent<TowerCombatVisualsAuthoring>();
                authoring.projectilePrefab = projectilePrefab;
                authoring.explosionPrefab = explosionPrefab;
                authoring.slowEffectPrefab = slowEffectPrefab;
                PrefabUtility.SaveAsPrefabAsset(root, CombatVisualsPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateEffectPrefab<TAuthoring>(
            string objectName,
            string meshName,
            Color color,
            string meshPath,
            string materialPath,
            string prefabPath)
            where TAuthoring : Component
        {
            var mesh = GetOrCreateRingMesh(meshName, meshPath);
            var material = GetOrCreateMaterial(color, materialPath);
            var root = new GameObject(objectName);
            try
            {
                root.AddComponent<TAuthoring>();
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Mesh GetOrCreateRingMesh(string meshName, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
                return existing;

            const int segments = 24;
            const float innerRadius = 0.82f;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (var index = 0; index < segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices[index * 2] = direction;
                vertices[index * 2 + 1] = direction * innerRadius;
                var next = (index + 1) % segments;
                var triangleIndex = index * 6;
                triangles[triangleIndex] = index * 2;
                triangles[triangleIndex + 1] = index * 2 + 1;
                triangles[triangleIndex + 2] = next * 2;
                triangles[triangleIndex + 3] = index * 2 + 1;
                triangles[triangleIndex + 4] = next * 2 + 1;
                triangles[triangleIndex + 5] = next * 2;
            }

            var mesh = new Mesh { name = meshName, vertices = vertices, triangles = triangles };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material GetOrCreateMaterial(Color color, string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    throw new System.InvalidOperationException("URP Unlit shader is unavailable.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.enableInstancing = true;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
