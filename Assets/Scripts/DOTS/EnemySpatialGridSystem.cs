using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System.Collections.Generic;

namespace Game.DOTS
{
    /// <summary>
    /// Schedules Burst grid construction after movement and exposes allocation-free nearby-cell
    /// lookup to the current GameObject tower layer until tower logic moves to ECS.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    [BurstCompile]
    public partial class EnemySpatialGridSystem : SystemBase
    {
        private NativeParallelMultiHashMap<int2, Entity> enemyCells;
        private EntityQuery enemyQuery;
        private EntityQuery settingsQuery;
        private float cellSize;
        private bool isReady;

        public bool IsReady => isReady;

        protected override void OnCreate()
        {
            enemyCells = new NativeParallelMultiHashMap<int2, Entity>(128, Allocator.Persistent);
            enemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<Enemy>(),
                ComponentType.ReadOnly<LocalTransform>());
            settingsQuery = GetEntityQuery(ComponentType.ReadOnly<EnemySpatialGridSettings>());
            cellSize = 1f;
        }

        protected override void OnUpdate()
        {
            if (!settingsQuery.IsEmptyIgnoreFilter)
                cellSize = math.max(0.1f, settingsQuery.GetSingleton<EnemySpatialGridSettings>().CellSize);

            var enemyCount = enemyQuery.CalculateEntityCount();
            if (enemyCells.Capacity < enemyCount)
                enemyCells.Capacity = math.max(enemyCount, enemyCells.Capacity * 2);

            enemyCells.Clear();
            Dependency = new EnemySpatialGridBuildJob
            {
                CellSize = cellSize,
                EnemyCells = enemyCells.AsParallelWriter()
            }.ScheduleParallel(Dependency);

            // GameObject towers read this map during Update, so it must be complete before this frame ends.
            Dependency.Complete();
            isReady = true;
        }

        /// <summary>Returns the nearest indexed enemy inside a circular range without scanning all enemies.</summary>
        public bool TryFindClosest(float3 position, float range, out Entity closestEnemy)
        {
            closestEnemy = Entity.Null;
            if (!isReady || range <= 0f)
                return false;

            var rangeSq = range * range;
            var closestDistanceSq = rangeSq;
            var minCell = ToCell(position - new float3(range, range, 0f), cellSize);
            var maxCell = ToCell(position + new float3(range, range, 0f), cellSize);

            for (var y = minCell.y; y <= maxCell.y; y++)
            {
                for (var x = minCell.x; x <= maxCell.x; x++)
                {
                    var iterator = enemyCells.GetValuesForKey(new int2(x, y));
                    while (iterator.MoveNext())
                    {
                        var enemy = iterator.Current;
                        if (!EntityManager.Exists(enemy) || !EntityManager.HasComponent<LocalTransform>(enemy))
                            continue;

                        var enemyPosition = EntityManager.GetComponentData<LocalTransform>(enemy).Position;
                        var distanceSq = math.distancesq(position, enemyPosition);
                        if (distanceSq > closestDistanceSq)
                            continue;

                        closestDistanceSq = distanceSq;
                        closestEnemy = enemy;
                    }
                }
            }

            return closestEnemy != Entity.Null;
        }

        /// <summary>Writes distinct nearby enemies and their distance squares in nearest-first order, up to maxTargets.</summary>
        public int FindClosestTargets(float3 position, float range, int maxTargets, List<Entity> targets, List<float> targetDistanceSqs)
        {
            targets.Clear();
            targetDistanceSqs.Clear();
            if (!isReady || range <= 0f || maxTargets <= 0)
                return 0;

            var rangeSq = range * range;
            var minCell = ToCell(position - new float3(range, range, 0f), cellSize);
            var maxCell = ToCell(position + new float3(range, range, 0f), cellSize);

            for (var y = minCell.y; y <= maxCell.y; y++)
            {
                for (var x = minCell.x; x <= maxCell.x; x++)
                {
                    var iterator = enemyCells.GetValuesForKey(new int2(x, y));
                    while (iterator.MoveNext())
                    {
                        var enemy = iterator.Current;
                        if (!EntityManager.Exists(enemy) ||
                            !EntityManager.HasComponent<LocalTransform>(enemy) ||
                            !EntityManager.HasComponent<EnemyHealth>(enemy) ||
                            EntityManager.GetComponentData<EnemyHealth>(enemy).Current <= 0f)
                            continue;

                        var enemyPosition = EntityManager.GetComponentData<LocalTransform>(enemy).Position;
                        var distanceSq = math.distancesq(position, enemyPosition);
                        if (distanceSq > rangeSq)
                            continue;

                        var insertIndex = targets.Count;
                        for (var i = 0; i < targets.Count; i++)
                        {
                            if (distanceSq < targetDistanceSqs[i])
                            {
                                insertIndex = i;
                                break;
                            }
                        }

                        if (insertIndex >= maxTargets)
                            continue;

                        targets.Insert(insertIndex, enemy);
                        targetDistanceSqs.Insert(insertIndex, distanceSq);
                        if (targets.Count > maxTargets)
                        {
                            targets.RemoveAt(targets.Count - 1);
                            targetDistanceSqs.RemoveAt(targetDistanceSqs.Count - 1);
                        }
                    }
                }
            }

            return targets.Count;
        }

        /// <summary>Applies one circular hit to indexed enemies and reserves newly killed enemies for destruction.</summary>
        public void ApplyDamageInRadius(float3 position, float radius, float damage, out int earnedReward, out int killCount)
        {
            earnedReward = 0;
            killCount = 0;
            if (!isReady || radius <= 0f || damage <= 0f)
                return;

            var radiusSq = radius * radius;
            var minCell = ToCell(position - new float3(radius, radius, 0f), cellSize);
            var maxCell = ToCell(position + new float3(radius, radius, 0f), cellSize);

            for (var y = minCell.y; y <= maxCell.y; y++)
            {
                for (var x = minCell.x; x <= maxCell.x; x++)
                {
                    var iterator = enemyCells.GetValuesForKey(new int2(x, y));
                    while (iterator.MoveNext())
                    {
                        var enemy = iterator.Current;
                        if (!EntityManager.Exists(enemy) ||
                            !EntityManager.HasComponent<LocalTransform>(enemy) ||
                            !EntityManager.HasComponent<EnemyHealth>(enemy) ||
                            !EntityManager.HasComponent<EnemyDeadTag>(enemy) ||
                            EntityManager.IsComponentEnabled<EnemyDeadTag>(enemy))
                            continue;

                        var enemyPosition = EntityManager.GetComponentData<LocalTransform>(enemy).Position;
                        if (math.distancesq(position, enemyPosition) > radiusSq)
                            continue;

                        var health = EntityManager.GetComponentData<EnemyHealth>(enemy);
                        if (health.Current <= 0f)
                            continue;

                        health.Current -= damage;
                        EntityManager.SetComponentData(enemy, health);
                        if (health.Current > 0f)
                            continue;

                        EntityManager.SetComponentEnabled<EnemyDeadTag>(enemy, true);
                        killCount++;
                        if (EntityManager.HasComponent<Enemy>(enemy))
                            earnedReward += EntityManager.GetComponentData<Enemy>(enemy).KillReward;
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
            if (enemyCells.IsCreated)
                enemyCells.Dispose();
        }

        /// <summary>Writes current enemy positions to the shared spatial index in parallel.</summary>
        [BurstCompile]
        private partial struct EnemySpatialGridBuildJob : IJobEntity
        {
            public float CellSize;
            public NativeParallelMultiHashMap<int2, Entity>.ParallelWriter EnemyCells;

            [BurstCompile]
            private void Execute(Entity entity, in Enemy enemy, in LocalTransform transform)
            {
                var cell = (int2)math.floor(transform.Position.xy / CellSize);
                EnemyCells.Add(cell, entity);
            }
        }

        private static int2 ToCell(float3 position, float size)
        {
            return (int2)math.floor(position.xy / size);
        }
    }
}
