using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.DOTS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemySpawnSystem))]
    [BurstCompile]
    public partial struct EnemyMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyWaypoint>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var route = SystemAPI.GetSingletonBuffer<EnemyWaypoint>(true);
            if (route.Length == 0)
                return;

            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, enemy, segment) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<Enemy>, RefRW<EnemyMovementSegment>>())
            {
                if (enemy.ValueRO.CurrentWaypoint >= route.Length)
                {
                    enemy.ValueRW.CurrentWaypoint = 0;
                    segment.ValueRW.RemainingDistance = 0f;
                }

                var position = transform.ValueRO.Position;
                var isSlowed = enemy.ValueRO.SlowRemainingTime > 0f;
                var step = enemy.ValueRO.MoveSpeed * (isSlowed ? enemy.ValueRO.SlowMultiplier : 1f) * deltaTime;

                if (isSlowed)
                {
                    enemy.ValueRW.SlowRemainingTime = math.max(0f, enemy.ValueRO.SlowRemainingTime - deltaTime);
                    if (enemy.ValueRW.SlowRemainingTime <= 0f)
                        enemy.ValueRW.SlowMultiplier = 1f;
                }

                if (segment.ValueRO.RemainingDistance <= 0.001f)
                {
                    var target = route[enemy.ValueRO.CurrentWaypoint].Position;
                    target.z = position.z;
                    var offset = target - position;
                    var distance = math.length(offset);

                    if (distance <= 0.001f)
                    {
                        transform.ValueRW.Position = target;
                        enemy.ValueRW.CurrentWaypoint = (enemy.ValueRO.CurrentWaypoint + 1) % route.Length;
                        segment.ValueRW.RemainingDistance = 0f;
                        continue;
                    }

                    segment.ValueRW.Direction = offset / distance;
                    segment.ValueRW.RemainingDistance = distance;
                }

                if (segment.ValueRO.RemainingDistance <= step)
                {
                    var target = route[enemy.ValueRO.CurrentWaypoint].Position;
                    target.z = position.z;
                    transform.ValueRW.Position = target;
                    enemy.ValueRW.CurrentWaypoint = (enemy.ValueRO.CurrentWaypoint + 1) % route.Length;
                    segment.ValueRW.RemainingDistance = 0f;
                    continue;
                }

                transform.ValueRW.Position = position + segment.ValueRO.Direction * step;
                segment.ValueRW.RemainingDistance -= step;
            }
        }
    }
}
