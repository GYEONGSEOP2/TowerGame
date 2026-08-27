using System.IO;
using Game.DOTS;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    /// <summary>Creates the static projectile visual prefab used by the baked projectile entity.</summary>
    public static class ProjectileBakedVisualGenerator
    {
        private const string OutputFolder = "Assets/Generated/ProjectileRendering";
        private const string MeshPath = OutputFolder + "/ProjectileMesh.asset";
        private const string MaterialPath = OutputFolder + "/ProjectileHealthFill.mat";
        private const string PrefabFolder = "Assets/Prefabs/Projectiles";
        private const string PrefabPath = PrefabFolder + "/ProjectileVisual.prefab";
        private const string ShaderPath = "Assets/Resources/EnemyHealthFill.shader";

        [MenuItem("Tools/DOTS/Generate Baked Projectile Visual")]
        public static void Generate()
        {
            EnsureFolder(OutputFolder);
            EnsureFolder(PrefabFolder);

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                Debug.LogError($"ProjectileBakedVisualGenerator: Missing shader at {ShaderPath}.");
                return;
            }

            var mesh = GetOrCreateMesh();
            var material = GetOrCreateMaterial(shader);
            var root = new GameObject("ProjectileVisual");
            try
            {
                root.AddComponent<ProjectileAuthoring>();
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Mesh GetOrCreateMesh()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (existing != null)
                return existing;

            var mesh = new Mesh { name = "ProjectileMesh" };
            mesh.vertices = new[]
            {
                new Vector3(-0.16f, -0.06f, 0f), new Vector3(0.16f, -0.06f, 0f),
                new Vector3(-0.16f, 0.06f, 0f), new Vector3(0.16f, 0.06f, 0f)
            };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, MeshPath);
            return mesh;
        }

        private static Material GetOrCreateMaterial(Shader shader)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                ConfigureMaterial(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = new Material(shader) { name = "ProjectileHealthFill" };
            ConfigureMaterial(material);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void ConfigureMaterial(Material material)
        {
            material.enableInstancing = true;
            // SpriteRenderer tiles use the transparent queue with a higher sorting order.
            // Rendering this material in Overlay keeps projectile entities visible above tiles.
            material.renderQueue = (int)RenderQueue.Overlay;
            material.SetColor("_BaseColor", new Color(1f, 0.82f, 0.16f));
            material.SetFloat("_HealthPercent", 1f);
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
