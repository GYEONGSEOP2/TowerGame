using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public enum GameSoundEffect
    {
        UiClick,
        UiError,
        TowerCreate,
        TowerFire,
        TowerMerge,
        TowerSell,
        EnemyHit,
        EnemyExplosion
    }

    /// <summary>Plays pooled gameplay sound effects and limits frequently repeated sounds.</summary>
    public sealed class GameAudioController : MonoBehaviour
    {
        private const string ResourceRoot = "Audio/AudioSfx/";
        private const int SourceCount = 8;

        private static GameAudioController instance;

        private readonly Dictionary<GameSoundEffect, AudioClip> clips = new();
        private readonly Dictionary<GameSoundEffect, float> nextPlayTimes = new();
        private AudioSource[] sources;
        private AudioSource musicSource;
        private int nextSourceIndex;

        public static GameAudioController EnsureInstance()
        {
            var controller = instance != null ? instance : FindAnyObjectByType<GameAudioController>();
            if (controller == null)
            {
                var controllerObject = new GameObject(nameof(GameAudioController));
                controller = controllerObject.AddComponent<GameAudioController>();
            }

            return controller;
        }

        public static void Play(GameSoundEffect effect)
        {
            EnsureInstance().PlayInternal(effect);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateSources();
            CreateMusicSource();
            LoadClips();
            StartMusic();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void PlayInternal(GameSoundEffect effect)
        {
            if (!clips.TryGetValue(effect, out var clip) || clip == null)
                return;

            var now = Time.unscaledTime;
            if (nextPlayTimes.TryGetValue(effect, out var nextPlayTime) && now < nextPlayTime)
                return;

            nextPlayTimes[effect] = now + GetMinimumInterval(effect);
            var source = sources[nextSourceIndex++ % sources.Length];
            source.pitch = effect == GameSoundEffect.TowerFire ? Random.Range(0.94f, 1.06f) : 1f;
            source.PlayOneShot(clip, GetVolume(effect));
        }

        private void CreateSources()
        {
            sources = new AudioSource[SourceCount];
            for (var index = 0; index < sources.Length; index++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                sources[index] = source;
            }
        }

        private void LoadClips()
        {
            clips[GameSoundEffect.UiClick] = Load("UI/Click");
            clips[GameSoundEffect.UiError] = Load("UI/Error");
            clips[GameSoundEffect.TowerCreate] = Load("UI/Confirm");
            clips[GameSoundEffect.TowerFire] = Load("Tower/Fire");
            clips[GameSoundEffect.TowerMerge] = Load("Tower/Merge");
            clips[GameSoundEffect.TowerSell] = Load("Tower/Sell");
            clips[GameSoundEffect.EnemyHit] = Load("Enemy/Hit");
            clips[GameSoundEffect.EnemyExplosion] = Load("Enemy/Explosion");
        }

        private void CreateMusicSource()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.12f;
        }

        private void StartMusic()
        {
            var music = Resources.Load<AudioClip>("Audio/Music/TowerDefenseTheme3");
            if (music == null)
                return;

            musicSource.clip = music;
            musicSource.Play();
        }

        private static AudioClip Load(string path)
        {
            return Resources.Load<AudioClip>(ResourceRoot + path);
        }

        private static float GetMinimumInterval(GameSoundEffect effect)
        {
            return effect switch
            {
                GameSoundEffect.TowerFire => 0.04f,
                GameSoundEffect.EnemyHit => 0.06f,
                GameSoundEffect.EnemyExplosion => 0.08f,
                _ => 0f
            };
        }

        private static float GetVolume(GameSoundEffect effect)
        {
            return effect switch
            {
                GameSoundEffect.TowerFire => 0.22f,
                GameSoundEffect.EnemyHit => 0.18f,
                GameSoundEffect.EnemyExplosion => 0.32f,
                _ => 0.5f
            };
        }
    }
}
