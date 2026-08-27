using System.IO;
using Game.DOTS;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    /// <summary>Creates static meshes and renderers for enemy prefabs so Entities Graphics can bake them.</summary>
    public static class EnemyBakedVisualGenerator
    {
        private const string OutputFolder = "Assets/Generated/EnemyRendering";
        private const string MaterialPath = OutputFolder + "/EnemyDotsUnlit.mat";
        private const string ShaderPath = "Assets/Resources/EnemyHealthFill.shader";

        [MenuItem("Tools/DOTS/Generate Baked Enemy Visuals")]
        public static void Generate()
        {
            EnsureFolder(OutputFolder);
            var material = GetOrCreateMaterial();
            if (material == null)
            {
                Debug.LogError($"EnemyBakedVisualGenerator: Missing shader at {ShaderPath}.");
                return;
            }

            foreach (var prefabGuid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" }))
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    var authoring = root.GetComponentInChildren<EnemyAuthoring>(true);
                    if (authoring == null)
                        continue;

                    var mesh = GetOrCreateMesh(authoring.definition == null ? EnemyShape.Circle : authoring.definition.shape);
                    var filter = root.GetComponent<MeshFilter>();
                    if (!filter)
                    {
                        root.AddComponent<MeshFilter>();
                        filter = root.GetComponent<MeshFilter>();
                    }

                    var renderer = root.GetComponent<MeshRenderer>();
                    if (!renderer)
                    {
                        root.AddComponent<MeshRenderer>();
                        renderer = root.GetComponent<MeshRenderer>();
                    }

                    if (!filter || !renderer)
                    {
                        Debug.LogError($"EnemyBakedVisualGenerator: Failed to configure '{prefabPath}'.");
                        continue;
                    }

                    filter.sharedMesh = mesh;
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Material GetOrCreateMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
                return null;

            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                ConfigureMaterial(existing, shader);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = new Material(shader) { name = "EnemyHealthFill" };
            ConfigureMaterial(material, shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void ConfigureMaterial(Material material, Shader shader)
        {
            material.shader = shader;
            material.enableInstancing = true;
            material.SetColor("_BaseColor", Color.blue);
            material.SetFloat("_HealthPercent", 1f);
        }

        private static Mesh GetOrCreateMesh(EnemyShape shape)
        {
            var path = $"{OutputFolder}/{shape}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                ApplyHealthFillUvs(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var segments = shape switch
            {
                EnemyShape.Triangle => 3,
                EnemyShape.Square => 4,
                EnemyShape.Pentagon => 5,
                EnemyShape.Hexagon => 6,
                _ => 24
            };
            var mesh = new Mesh { name = shape.ToString() };
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            for (var index = 0; index < segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle) * 0.3f, Mathf.Sin(angle) * 0.3f, 0f);
                var triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = (index + 1) % segments + 1;
                triangles[triangleIndex + 2] = index + 1;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            ApplyHealthFillUvs(mesh);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void ApplyHealthFillUvs(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var uvs = new Vector2[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var position = vertices[index];
                uvs[index] = new Vector2(position.x / 0.6f + 0.5f, position.y / 0.6f + 0.5f);
            }
            mesh.uv = uvs;
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
