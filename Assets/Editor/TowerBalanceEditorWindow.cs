using System.Collections.Generic;
using Game;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Edits tower definitions and compares their effective combat values for one selected rank.</summary>
    public sealed class TowerBalanceEditorWindow : EditorWindow
    {
        private readonly List<TowerDefinition> definitions = new();
        private readonly List<TowerCatalogValidationIssue> catalogIssues = new();
        private Vector2 scrollPosition;
        private TowerRank previewRank = TowerRank.Triangle;
        private TowerDefinitionCatalog towerCatalog;
        private string lastSaveMessage;

        [MenuItem("Tools/Tower Game/Tower Balance Editor")]
        private static void Open()
        {
            var window = GetWindow<TowerBalanceEditorWindow>("Tower Balance");
            window.minSize = new Vector2(660f, 460f);
            window.Show();
        }

        private void OnEnable()
        {
            FindDefaultCatalog();
            RefreshDefinitions();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawCatalogValidation();
            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox("No TowerDefinition assets were found. Create one from Assets > Create > Game > Towers > Tower Definition.",
                    MessageType.Info);
                return;
            }

            previewRank = (TowerRank)EditorGUILayout.EnumPopup("Preview Rank", previewRank);
            EditorGUILayout.HelpBox(
                "Preview values use the same calculation as TowerAttack. Total Direct DPS assumes every projectile hits a target. " +
                "Explosion DPS is additional damage per enemy inside the blast radius.", MessageType.None);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var definition in definitions)
            {
                if (definition != null)
                    DrawDefinition(definition);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    RefreshDefinitions();
                    ValidateCatalog();
                }

                if (GUILayout.Button("Save All", EditorStyles.toolbarButton))
                    SaveAll();

                GUILayout.FlexibleSpace();
                EditorGUI.BeginChangeCheck();
                towerCatalog = (TowerDefinitionCatalog)EditorGUILayout.ObjectField(
                    towerCatalog, typeof(TowerDefinitionCatalog), false, GUILayout.Width(210f));
                if (EditorGUI.EndChangeCheck())
                    ValidateCatalog();

                if (!string.IsNullOrEmpty(lastSaveMessage))
                    GUILayout.Label(lastSaveMessage, EditorStyles.miniLabel);
            }
        }

        private void DrawCatalogValidation()
        {
            ValidateCatalog();
            if (towerCatalog != null && catalogIssues.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"Catalog validation passed: {towerCatalog.towerDefinitions.Count} entry/entries ready for random tower creation.",
                    MessageType.Info);
                return;
            }

            foreach (var issue in catalogIssues)
            {
                var messageType = issue.Severity == TowerCatalogValidationSeverity.Error
                    ? MessageType.Error
                    : MessageType.Warning;
                EditorGUILayout.HelpBox(issue.Message, messageType);
            }
        }

        private void DrawDefinition(TowerDefinition definition)
        {
            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.Update();

            var color = definition.displayColor;
            var previousColor = GUI.color;
            GUI.color = color;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUI.color = previousColor;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(definition.displayName, EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select", GUILayout.Width(56f)))
                    {
                        Selection.activeObject = definition;
                        EditorGUIUtility.PingObject(definition);
                    }
                }

                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("displayName"), new GUIContent("Display Name"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("description"), new GUIContent("Description"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("displayColor"), new GUIContent("Display Color"));
                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Base Combat", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("baseDamage"), new GUIContent("Damage"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("baseFireInterval"), new GUIContent("Fire Interval"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("baseAttackRange"), new GUIContent("Attack Range"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("projectileSpeed"), new GUIContent("Projectile Speed"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("projectileHitRadius"), new GUIContent("Hit Radius"));

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Combat Modifiers", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("damageMultiplier"), new GUIContent("Damage Multiplier"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("attackSpeedMultiplier"), new GUIContent("Attack Speed Multiplier"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("rangeMultiplier"), new GUIContent("Range Multiplier"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("baseProjectileCount"), new GUIContent("Base Projectile Count"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("projectileCountPerRank"), new GUIContent("Projectile Count per Rank"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("rankDamageMultiplier"), new GUIContent("Rank Damage Multiplier"));

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Special Effects", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("explosionRadius"), new GUIContent("Explosion Radius"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("explosionDamage"), new GUIContent("Explosion Damage"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("slowDuration"), new GUIContent("Slow Duration"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("slowMultiplier"), new GUIContent("Slow Multiplier"));
                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Poison Effect", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("poisonDamagePerTick"), new GUIContent("Damage per Tick"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("poisonDuration"), new GUIContent("Duration"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("poisonTickInterval"), new GUIContent("Tick Interval"));

                serializedDefinition.ApplyModifiedProperties();
                DrawEffectivePreview(definition);
            }
            GUI.color = previousColor;
            EditorGUILayout.Space(4f);
        }

        private void DrawEffectivePreview(TowerDefinition definition)
        {
            var rankMultiplier = Mathf.Pow(definition.rankDamageMultiplier, (int)previewRank);
            var fireRate = definition.attackSpeedMultiplier / Mathf.Max(0.01f, definition.baseFireInterval);
            var projectileCount = Mathf.Max(1,
                definition.baseProjectileCount + definition.projectileCountPerRank * (int)previewRank);
            var damage = definition.baseDamage * definition.damageMultiplier * rankMultiplier;
            var range = definition.baseAttackRange * definition.rangeMultiplier;
            var directDps = damage * projectileCount * fireRate;
            var explosionDamage = definition.explosionDamage * rankMultiplier;
            var explosionDps = explosionDamage * projectileCount * fireRate;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Effective Preview — {previewRank}", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("Direct Damage", damage.ToString("F1"));
                EditorGUILayout.LabelField("Fire Rate", $"{fireRate:F2} / sec");
                EditorGUILayout.LabelField("Targets per Shot", projectileCount.ToString());
                EditorGUILayout.LabelField("Total Direct DPS", directDps.ToString("F1"));
                EditorGUILayout.LabelField("Range", range.ToString("F2"));

                if (definition.explosionRadius > 0f && definition.explosionDamage > 0f)
                {
                    EditorGUILayout.LabelField("Explosion", $"{explosionDamage:F1} dmg / {definition.explosionRadius:F2} radius");
                    EditorGUILayout.LabelField("Explosion DPS / Enemy", explosionDps.ToString("F1"));
                }

                if (definition.slowDuration > 0f)
                {
                    var speedReduction = (1f - definition.slowMultiplier) * 100f;
                    EditorGUILayout.LabelField("Slow", $"{speedReduction:F0}% for {definition.slowDuration:F2}s");
                }

                if (definition.poisonDuration > 0f && definition.poisonDamagePerTick > 0f)
                {
                    var poisonDps = definition.poisonDamagePerTick * rankMultiplier /
                                    Mathf.Max(0.01f, definition.poisonTickInterval);
                    EditorGUILayout.LabelField("Poison", $"{definition.poisonDamagePerTick * rankMultiplier:F1} dmg / {definition.poisonTickInterval:F2}s for {definition.poisonDuration:F1}s");
                    EditorGUILayout.LabelField("Poison DPS / Enemy", poisonDps.ToString("F1"));
                }
            }
        }

        private void RefreshDefinitions()
        {
            definitions.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:TowerDefinition"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<TowerDefinition>(path);
                if (definition != null)
                    definitions.Add(definition);
            }

            definitions.Sort((left, right) => string.Compare(left.displayName, right.displayName, System.StringComparison.Ordinal));
            Repaint();
        }

        private void FindDefaultCatalog()
        {
            var catalogGuids = AssetDatabase.FindAssets("t:TowerDefinitionCatalog");
            if (catalogGuids.Length == 0)
                return;

            var catalogPath = AssetDatabase.GUIDToAssetPath(catalogGuids[0]);
            towerCatalog = AssetDatabase.LoadAssetAtPath<TowerDefinitionCatalog>(catalogPath);
        }

        private void ValidateCatalog()
        {
            TowerCatalogValidator.Validate(towerCatalog, catalogIssues);
        }

        private void SaveAll()
        {
            foreach (var definition in definitions)
            {
                if (definition != null)
                    EditorUtility.SetDirty(definition);
            }

            AssetDatabase.SaveAssets();
            lastSaveMessage = "Saved all tower definitions";
            Repaint();
        }
    }
}
