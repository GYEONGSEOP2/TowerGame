using Game.DOTS;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;ㅁ
using Unity.Transforms;
using UnityEngine;

namespace Game
{
    /// <summary>Finds the nearest enemy in range and creates projectile entities at a fixed interval.</summary>
    public sealed class TowerAttack : MonoBehaviour
    {
        [Min(0.1f)] public float attackRange = 3f;
        [Min(0.1f)] public float fireInterval = 0.5f;
        [Min(0f)] public float damage = 20f;
        [Min(0.1f)] public float projectileSpeed = 12f;
        [Min(1.01f)] public float minimumProjectileSpeedMultiplier = 1.25f;
        [Min(0.01f)] public float projectileHitRadius = 0.2f;
        public float RankDamageMultiplier => rankDamageMultiplier;
        public int ProjectileCount => definition != null && definition.towerType == TowerType.Purple
            ? Mathf.Max(1, definition.baseProjectileCount + (int)rank)
            : 1;

        private float baseDamage;
        private float baseFireInterval;
        private float baseAttackRange;
        private float rankDamageMultiplier = 1f;
        private TowerRank rank;
        private TowerDefinition definition;
        private float nextFireTime;
        private EntityQuery projectilePrefabQuery;
        private EntityManager entityManager;
        private EndSimulationEntityCommandBufferSystem endSimulationEcbSystem;
        private EnemySpatialGridSystem enemySpatialGridSystem;
        private World world;
        private readonly List<Entity> targets = new(6);
        private readonly List<float> targetDistanceSqs = new(6);

        private void Awake()
        {
            baseDamage = damage;
            baseFireInterval = fireInterval;
            baseAttackRange = attackRange;
            world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                enabled = false;
                return;
            }

            entityManager = world.EntityManager;
            endSimulationEcbSystem = world.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            enemySpatialGridSystem = world.GetExistingSystemManaged<EnemySpatialGridSystem>();
            projectilePrefabQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ProjectilePrefab>());
        }

        public void Configure(TowerDefinition towerDefinition)
        {
            definition = towerDefinition;
            RefreshStats();
        }

        public void SetRankDamageMultiplier(float multiplier)
        {
            rankDamageMultiplier = multiplier;
            RefreshStats();
        }

        public void SetRank(TowerRank towerRank)
        {
            rank = towerRank;
        }

        private void Update()
        {
            if (definition == null)
                return;

            ResolveDotsSystems();

            if (Time.time < nextFireTime)
                return;

            if (!TryFindClosestEnemies())
                return;

            if (endSimulationEcbSystem == null || !projectilePrefabQuery.TryGetSingleton(out ProjectilePrefab projectilePrefab))
                return;

            var ecb = endSimulationEcbSystem.CreateCommandBuffer();
            var canSetProjectileColor = entityManager.HasComponent<URPMaterialPropertyBaseColor>(projectilePrefab.Value);
            var color = definition.displayColor.linear;
            var spawnPosition = (float3)transform.position;
            spawnPosition.z = -0.7f;
            foreach (var target in targets)
            {
                var projectile = ecb.Instantiate(projectilePrefab.Value);
                ecb.SetComponent(projectile, new Projectile
                {
                    Target = target,
                    Position = spawnPosition,
                    Speed = GetProjectileSpeed(target),
                    Damage = damage,
                    HitRadius = projectileHitRadius,
                    ExplosionRadius = definition.explosionRadius,
                    ExplosionDamage = definition.explosionDamage * rankDamageMultiplier,
                    SlowDuration = definition.slowDuration,
                    SlowMultiplier = definition.slowMultiplier
                });
                ecb.SetComponent(projectile, LocalTransform.FromPosition(spawnPosition));
                if (canSetProjectileColor)
                {
                    ecb.SetComponent(projectile, new URPMaterialPropertyBaseColor
                    {
                        Value = new float4(color.r, color.g, color.b, color.a)
                    });
                }
            }
            GameAudioController.Play(GameSoundEffect.TowerFire);
            nextFireTime = Time.time + fireInterval;
        }

        private void RefreshStats()
        {
            if (definition == null)
                return;

            damage = baseDamage * rankDamageMultiplier * definition.damageMultiplier;
            fireInterval = baseFireInterval / definition.attackSpeedMultiplier;
            attackRange = baseAttackRange * definition.rangeMultiplier;
        }

        private bool TryFindClosestEnemies()
        {
            return enemySpatialGridSystem != null &&
                   enemySpatialGridSystem.FindClosestTargets(
                       (float3)transform.position,
                       attackRange,
                       ProjectileCount,
                       targets,
                       targetDistanceSqs) > 0;
        }

        private void ResolveDotsSystems()
        {
            if (world == null || !world.IsCreated)
                world = World.DefaultGameObjectInjectionWorld;

            if (world == null || !world.IsCreated)
                return;

            if (endSimulationEcbSystem == null)
                endSimulationEcbSystem = world.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();

            if (enemySpatialGridSystem == null)
                enemySpatialGridSystem = world.GetExistingSystemManaged<EnemySpatialGridSystem>();
        }

        private float GetProjectileSpeed(Entity target)
        {
            if (!entityManager.Exists(target) || !entityManager.HasComponent<Enemy>(target))
                return projectileSpeed;

            var targetSpeed = entityManager.GetComponentData<Enemy>(target).MoveSpeed;
            return Mathf.Max(projectileSpeed, targetSpeed * minimumProjectileSpeedMultiplier);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.15f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
