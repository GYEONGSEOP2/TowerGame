using Game.DOTS;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Displays the current difficulty wave and time remaining until the next wave.</summary>
    [RequireComponent(typeof(TowerPlacementController))]
    public sealed class DifficultyUGUI : MonoBehaviour
    {
        private const float RefreshInterval = 0.1f;

        private EntityQuery difficultyQuery;
        private EntityQuery waveStateQuery;
        private EntityQuery waveQuery;
        private EntityQuery waveSpawnQuery;
        private Text label;
        private float nextRefreshTime;
        private bool hasDifficultyQuery;

        private void Awake()
        {
            CreateLabel();
        }

        private void Update()
        {
            if (!TryInitializeQuery() ||
                Time.unscaledTime < nextRefreshTime)
                return;

            if (!waveStateQuery.IsEmptyIgnoreFilter && !waveQuery.IsEmptyIgnoreFilter && !waveSpawnQuery.IsEmptyIgnoreFilter)
            {
                var waveState = waveStateQuery.GetSingleton<EnemyWaveState>();
                var waves = waveQuery.GetSingletonBuffer<EnemyWave>(true);
                if (waves.Length > 0 && waveState.CurrentWaveIndex < waves.Length)
                {
                    var wave = waves[waveState.CurrentWaveIndex];
                    var waveNumber = waveState.CompletedWaveCount + 1;
                    var totalSpawnCount = 0;
                    var waveSpawns = waveSpawnQuery.GetSingletonBuffer<EnemyWaveSpawn>(true);
                    for (var index = wave.FirstSpawnIndex; index < wave.FirstSpawnIndex + wave.SpawnGroupCount; index++)
                        totalSpawnCount += waveSpawns[index].SpawnCount;
                    label.text = waveState.IsWaitingForNextWave
                        ? $"Wave: {waveNumber}  |  Next: {Mathf.CeilToInt(Mathf.Max(0f, waveState.TimeUntilNextWave))}s"
                        : $"Wave: {waveNumber}  |  Spawn: {waveState.SpawnedInWave}/{totalSpawnCount}";
                }

                nextRefreshTime = Time.unscaledTime + RefreshInterval;
                return;
            }

            if (difficultyQuery.IsEmptyIgnoreFilter)
                return;

            var difficulty = difficultyQuery.GetSingleton<EnemyDifficulty>();
            var stageDuration = Mathf.Max(0.01f, difficulty.StageDuration);
            var elapsedInStage = difficulty.ElapsedTime % stageDuration;
            var remaining = Mathf.Max(0f, stageDuration - elapsedInStage);
            label.text = $"Wave: {difficulty.CurrentStage + 1}  |  Next: {Mathf.CeilToInt(remaining)}s";
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
        }

        private bool TryInitializeQuery()
        {
            if (hasDifficultyQuery)
                return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            difficultyQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<EnemyDifficulty>());
            waveStateQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<EnemyWaveState>());
            waveQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<EnemyWave>());
            waveSpawnQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<EnemyWaveSpawn>());
            hasDifficultyQuery = true;
            return true;
        }

        private void CreateLabel()
        {
            var canvasTransform = GameUICanvas.GetOrCreate(transform);
            label = canvasTransform?.Find("Difficulty")?.GetComponent<Text>();
            if (label == null)
                Debug.LogError("DifficultyUGUI: Missing Difficulty in GameHUD prefab.", this);
        }
    }
}
