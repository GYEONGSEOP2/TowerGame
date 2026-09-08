using Game.DOTS;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Dedicated authoring window for the repeating ECS wave definition asset.
    /// The window edits the existing ScriptableObject directly, so the baker remains
    /// the single path that converts the data into EnemyWave buffers at build time.
    /// </summary>
    public sealed class EnemyWaveEditorWindow : EditorWindow
    {
        private const string DefaultAssetDirectory = "Assets/Waves";

        private EnemyWaveDefinition waveDefinition;
        private SerializedObject serializedDefinition;
        private Vector2 scrollPosition;
        private string lastSaveMessage;

        [MenuItem("Tools/Tower Game/Wave Editor")]
        private static void Open()
        {
            var window = GetWindow<EnemyWaveEditorWindow>("Wave Editor");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            SelectWaveDefinitionFromSelection();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUI.BeginChangeCheck();
            var selectedDefinition = (EnemyWaveDefinition)EditorGUILayout.ObjectField(
                "Wave Definition", waveDefinition, typeof(EnemyWaveDefinition), false);
            if (EditorGUI.EndChangeCheck())
                SetWaveDefinition(selectedDefinition);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create New Wave Definition"))
                    CreateWaveDefinitionAsset();

                using (new EditorGUI.DisabledScope(waveDefinition == null))
                {
                    if (GUILayout.Button("Save Asset"))
                        SaveCurrentAsset();

                    if (GUILayout.Button("Select Asset"))
                    {
                        Selection.activeObject = waveDefinition;
                        EditorGUIUtility.PingObject(waveDefinition);
                    }
                }
            }

            if (!string.IsNullOrEmpty(lastSaveMessage))
                EditorGUILayout.HelpBox(lastSaveMessage, MessageType.Info);

            if (waveDefinition == null)
            {
                EditorGUILayout.HelpBox(
                    "Edit an EnemyWaveDefinition asset here. Assign the asset to EnemySpawnerAuthoring in the SubScene.",
                    MessageType.Info);
                return;
            }

            EnsureSerializedDefinition();
            serializedDefinition.Update();
            var waves = serializedDefinition.FindProperty("waves");

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawLoopSettings();
            EditorGUILayout.Space(8f);
            DrawSummary(waves);
            EditorGUILayout.Space(8f);
            DrawWaves(waves);
            EditorGUILayout.EndScrollView();

            serializedDefinition.ApplyModifiedProperties();
        }

        private void DrawLoopSettings()
        {
            EditorGUILayout.LabelField("Loop Difficulty", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty("healthMultiplierPerLoop"),
                new GUIContent("Health Multiplier per Loop"));
            EditorGUILayout.PropertyField(serializedDefinition.FindProperty("speedMultiplierPerLoop"),
                new GUIContent("Speed Multiplier per Loop"));
            EditorGUI.indentLevel--;
        }

        private void DrawSummary(SerializedProperty waves)
        {
            var totalEnemies = 0;
            var totalReward = 0;
            var totalBaseHealth = 0f;
            var invalidGroups = 0;

            for (var waveIndex = 0; waveIndex < waves.arraySize; waveIndex++)
            {
                var wave = waves.GetArrayElementAtIndex(waveIndex);
                var healthMultiplier = wave.FindPropertyRelative("healthMultiplier").floatValue;
                var rewardMultiplier = wave.FindPropertyRelative("killRewardMultiplier").floatValue;
                var groups = wave.FindPropertyRelative("spawns");

                if (groups.arraySize == 0)
                {
                    AddPreviewValues(wave.FindPropertyRelative("enemyPrefab").objectReferenceValue as GameObject,
                        wave.FindPropertyRelative("spawnCount").intValue, healthMultiplier, rewardMultiplier,
                        ref totalEnemies, ref totalBaseHealth, ref totalReward, ref invalidGroups);
                    continue;
                }

                for (var groupIndex = 0; groupIndex < groups.arraySize; groupIndex++)
                {
                    var group = groups.GetArrayElementAtIndex(groupIndex);
                    AddPreviewValues(group.FindPropertyRelative("enemyPrefab").objectReferenceValue as GameObject,
                        group.FindPropertyRelative("spawnCount").intValue, healthMultiplier, rewardMultiplier,
                        ref totalEnemies, ref totalBaseHealth, ref totalReward, ref invalidGroups);
                }
            }

            EditorGUILayout.LabelField("Definition Preview", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Waves", waves.arraySize.ToString());
                EditorGUILayout.LabelField("Total Enemies per Loop", totalEnemies.ToString());
                EditorGUILayout.LabelField("Estimated Total Health", totalBaseHealth.ToString("N0"));
                EditorGUILayout.LabelField("Estimated Total Reward", totalReward.ToString());
                if (invalidGroups > 0)
                    EditorGUILayout.HelpBox($"{invalidGroups} spawn group(s) have no EnemyAuthoring prefab. " +
                                            "They will use the spawner's default enemy at runtime.", MessageType.Warning);
            }
        }

        private static void AddPreviewValues(GameObject prefab, int count, float healthMultiplier, float rewardMultiplier,
            ref int totalEnemies, ref float totalBaseHealth, ref int totalReward, ref int invalidGroups)
        {
            count = Mathf.Max(0, count);
            totalEnemies += count;

            if (prefab == null || !prefab.TryGetComponent<EnemyAuthoring>(out var authoring))
            {
                invalidGroups++;
                return;
            }

            var health = authoring.definition == null ? authoring.maxHealth : authoring.definition.maxHealth;
            var reward = authoring.definition == null ? authoring.killReward : authoring.definition.killReward;
            totalBaseHealth += health * Mathf.Max(0f, healthMultiplier) * count;
            totalReward += Mathf.RoundToInt(reward * Mathf.Max(0f, rewardMultiplier)) * count;
        }

        private void DrawWaves(SerializedProperty waves)
        {
            EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel);
            for (var waveIndex = 0; waveIndex < waves.arraySize; waveIndex++)
            {
                var wave = waves.GetArrayElementAtIndex(waveIndex);
                DrawWave(waves, wave, waveIndex);
                EditorGUILayout.Space(5f);
            }

            if (GUILayout.Button("Add Wave"))
            {
                waves.InsertArrayElementAtIndex(waves.arraySize);
                InitializeWave(waves.GetArrayElementAtIndex(waves.arraySize - 1));
            }
        }

        private void DrawWave(SerializedProperty waves, SerializedProperty wave, int waveIndex)
        {
            var groups = wave.FindPropertyRelative("spawns");
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Wave {waveIndex + 1}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(waveIndex == 0))
                    {
                        if (GUILayout.Button("▲", GUILayout.Width(28f)))
                            waves.MoveArrayElement(waveIndex, waveIndex - 1);
                    }
                    using (new EditorGUI.DisabledScope(waveIndex == waves.arraySize - 1))
                    {
                        if (GUILayout.Button("▼", GUILayout.Width(28f)))
                            waves.MoveArrayElement(waveIndex, waveIndex + 1);
                    }
                    if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                    {
                        waves.DeleteArrayElementAtIndex(waveIndex);
                        return;
                    }
                }

                EditorGUILayout.PropertyField(wave.FindPropertyRelative("nextWaveDelay"), new GUIContent("Next Wave Delay"));
                EditorGUILayout.PropertyField(wave.FindPropertyRelative("healthMultiplier"), new GUIContent("Health Multiplier"));
                EditorGUILayout.PropertyField(wave.FindPropertyRelative("speedMultiplier"), new GUIContent("Speed Multiplier"));
                EditorGUILayout.PropertyField(wave.FindPropertyRelative("killRewardMultiplier"), new GUIContent("Kill Reward Multiplier"));

                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("Spawn Groups", EditorStyles.miniBoldLabel);
                if (groups.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("This wave uses legacy single-spawn fields. Add a group to use the current format.",
                        MessageType.None);
                    EditorGUILayout.PropertyField(wave.FindPropertyRelative("enemyPrefab"), new GUIContent("Legacy Enemy"));
                    EditorGUILayout.PropertyField(wave.FindPropertyRelative("spawnCount"), new GUIContent("Legacy Count"));
                    EditorGUILayout.PropertyField(wave.FindPropertyRelative("spawnInterval"), new GUIContent("Legacy Interval"));
                }

                for (var groupIndex = 0; groupIndex < groups.arraySize; groupIndex++)
                    DrawSpawnGroup(groups, groupIndex);

                if (GUILayout.Button("Add Spawn Group"))
                {
                    groups.InsertArrayElementAtIndex(groups.arraySize);
                    InitializeSpawnGroup(groups.GetArrayElementAtIndex(groups.arraySize - 1));
                }
            }
        }

        private static void DrawSpawnGroup(SerializedProperty groups, int groupIndex)
        {
            var group = groups.GetArrayElementAtIndex(groupIndex);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Group {groupIndex + 1}", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Remove", GUILayout.Width(62f)))
                    {
                        groups.DeleteArrayElementAtIndex(groupIndex);
                        return;
                    }
                }

                EditorGUILayout.PropertyField(group.FindPropertyRelative("enemyPrefab"), new GUIContent("Enemy Prefab"));
                EditorGUILayout.PropertyField(group.FindPropertyRelative("spawnCount"), new GUIContent("Count"));
                EditorGUILayout.PropertyField(group.FindPropertyRelative("spawnInterval"), new GUIContent("Interval"));
            }
        }

        private static void InitializeWave(SerializedProperty wave)
        {
            wave.FindPropertyRelative("nextWaveDelay").floatValue = 3f;
            wave.FindPropertyRelative("healthMultiplier").floatValue = 1f;
            wave.FindPropertyRelative("speedMultiplier").floatValue = 1f;
            wave.FindPropertyRelative("killRewardMultiplier").floatValue = 1f;
            wave.FindPropertyRelative("spawns").arraySize = 0;
        }

        private static void InitializeSpawnGroup(SerializedProperty group)
        {
            group.FindPropertyRelative("enemyPrefab").objectReferenceValue = null;
            group.FindPropertyRelative("spawnCount").intValue = 10;
            group.FindPropertyRelative("spawnInterval").floatValue = 0.75f;
        }

        private void SelectWaveDefinitionFromSelection()
        {
            if (Selection.activeObject is EnemyWaveDefinition definition)
                SetWaveDefinition(definition);
        }

        private void OnSelectionChanged()
        {
            SelectWaveDefinitionFromSelection();
            Repaint();
        }

        private void SetWaveDefinition(EnemyWaveDefinition definition)
        {
            if (waveDefinition == definition)
                return;

            waveDefinition = definition;
            serializedDefinition = waveDefinition == null ? null : new SerializedObject(waveDefinition);
            Repaint();
        }

        private void EnsureSerializedDefinition()
        {
            if (serializedDefinition == null || serializedDefinition.targetObject != waveDefinition)
                serializedDefinition = new SerializedObject(waveDefinition);
        }

        private void CreateWaveDefinitionAsset()
        {
            if (!AssetDatabase.IsValidFolder(DefaultAssetDirectory))
                AssetDatabase.CreateFolder("Assets", "Waves");

            var asset = CreateInstance<EnemyWaveDefinition>();
            var path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultAssetDirectory}/NewWaveDefinition.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            SetWaveDefinition(asset);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void SaveCurrentAsset()
        {
            serializedDefinition?.ApplyModifiedProperties();
            EditorUtility.SetDirty(waveDefinition);
            AssetDatabase.SaveAssets();
            lastSaveMessage = $"Saved: {AssetDatabase.GetAssetPath(waveDefinition)}";
            Repaint();
        }
    }
}
