using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.DOTS
{
    /// <summary>Configures Entity Graphics once on the baked enemy prefab before it is instantiated.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemySpawnSystem))]
    public partial class EnemyEntityRenderSystem : SystemBase
    {
        private EntityQuery spawnerQuery;
        private EntityQuery waveSpawnerQuery;
        private Mesh[] meshes;
        private Material material;
        private RenderMeshArray renderMeshArray;
        private RenderMeshDescription renderDescription;

        protected override void OnCreate()
        {
            spawnerQuery = GetEntityQuery(ComponentType.ReadOnly<EnemySpawner>());
            waveSpawnerQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemySpawner>(),
                ComponentType.ReadOnly<EnemyWaveSpawn>());

            meshes = new[]
            {
                CreateRegularPolygonMesh("EnemyTriangle", 3),
                CreateRegularPolygonMesh("EnemySquare", 4),
                CreateRegularPolygonMesh("EnemyPentagon", 5),
                CreateRegularPolygonMesh("EnemyHexagon", 6),
                CreateRegularPolygonMesh("EnemyCircle", 24)
            };
            var shader = Resources.Load<Shader>("EnemyHealthFill") ??
                         Shader.Find("Game/Enemy Health Fill") ??
                         Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color");
            material = new Material(shader);
            material.SetColor("_BaseColor", new Color(0.9f, 0.22f, 0.18f));
            material.enableInstancing = true;
            renderMeshArray = new RenderMeshArray(new[] { material }, meshes);
            renderDescription = new RenderMeshDescription(ShadowCastingMode.Off, receiveShadows: false);
        }

        protected override void OnUpdate()
        {
            if (spawnerQuery.IsEmptyIgnoreFilter)
                return;

            using var enemyPrefabs = new NativeList<Entity>(Allocator.Temp);
            enemyPrefabs.Add(spawnerQuery.GetSingleton<EnemySpawner>().EnemyPrefab);

            if (!waveSpawnerQuery.IsEmptyIgnoreFilter)
            {
                var waveSpawns = waveSpawnerQuery.GetSingletonBuffer<EnemyWaveSpawn>(true);
                foreach (var waveSpawn in waveSpawns)
                    enemyPrefabs.Add(waveSpawn.EnemyPrefab);
            }

            foreach (var enemyPrefab in enemyPrefabs)
                ConfigureEnemyPrefab(enemyPrefab);
        }

        private void ConfigureEnemyPrefab(Entity enemyPrefab)
        {
            if (enemyPrefab == Entity.Null)
                return;

            var visual = EntityManager.HasComponent<EnemyVisual>(enemyPrefab)
                ? EntityManager.GetComponentData<EnemyVisual>(enemyPrefab)
                : new EnemyVisual { MeshIndex = 0, Scale = 1f, TypeColor = new float4(1f) };
            var meshIndex = math.clamp(visual.MeshIndex, 0, meshes.Length - 1);

            if (!EntityManager.HasComponent<MaterialMeshInfo>(enemyPrefab))
            {
                RenderMeshUtility.AddComponents(
                    enemyPrefab,
                    EntityManager,
                    renderDescription,
                    renderMeshArray,
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, meshIndex));
            }

            if (!EntityManager.HasComponent<URPMaterialPropertyBaseColor>(enemyPrefab))
            {
                var healthBlue = new float4(0f, 0f, 1f, 1f);
                EntityManager.AddComponentData(enemyPrefab, new URPMaterialPropertyBaseColor
                {
                    Value = math.lerp(healthBlue, visual.TypeColor, 0.38f)
                });
            }

            if (!EntityManager.HasComponent<EnemyHealthFillProperty>(enemyPrefab))
            {
                EntityManager.AddComponentData(enemyPrefab, new EnemyHealthFillProperty
                {
                    Value = 1f
                });
            }
        }

        protected override void OnDestroy()
        {
            if (meshes != null)
            {
                foreach (var mesh in meshes)
                {
                    if (mesh != null)
                        Object.Destroy(mesh);
                }
            }
            if (material != null)
                Object.Destroy(material);
        }

        private static Mesh CreateRegularPolygonMesh(string meshName, int segments)
        {
            const float radius = 0.3f;
            var vertices = new Vector3[segments + 1];
            var uv = new Vector2[segments + 1];
            var triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (var index = 0; index < segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var x = Mathf.Cos(angle);
                var y = Mathf.Sin(angle);
                vertices[index + 1] = new Vector3(x * radius, y * radius, 0f);
                uv[index + 1] = new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f);

                var nextIndex = (index + 1) % segments;
                var triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = nextIndex + 1;
                triangles[triangleIndex + 2] = index + 1;
            }

            var result = new Mesh { name = meshName };
            result.vertices = vertices;
            result.uv = uv;
            result.triangles = triangles;
            result.RecalculateBounds();
            return result;
        }
    }
}
