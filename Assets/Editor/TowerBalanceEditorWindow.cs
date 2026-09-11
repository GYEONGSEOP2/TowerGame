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
        private Vector2 scrollPosition;
        private TowerRank previewRank = TowerRank.Triangle;
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
            RefreshDefinitions();
        }

        private void OnGUI()
        {
            DrawToolbar();
            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox("No TowerDefinition assets were found. Create one from Assets > Create > Game > Towers > Tower Definition.",
                    MessageType.Info);
                return;
            }

            previewRank = (TowerRank)EditorGUILayout.EnumPopup("Preview Rank", previewRank);
            EditorGUILayout.HelpBox(
                "Preview values use the same calculation as TowerAttack. DPS assumes every projectile hits one target. " +
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
                    RefreshDefinitions();

                if (GUILayout.Button("Save All", EditorStyles.toolbarButton))
                    SaveAll();

                GUILayout.FlexibleSpace();
                if (!string.IsNullOrEmpty(lastSaveMessage))
                    GUILayout.Label(lastSaveMessage, EditorStyles.miniLabel);
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
                    EditorGUILayout.LabelField($"{definition.towerType} Tower", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select", GUILayout.Width(56f)))
                    {
                        Selection.activeObject = definition;
                        EditorGUIUtility.PingObject(definition);
                    }
                }

                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("displayColor"), new GUIContent("Display Color"));
                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Base Combat", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("baseDamage"), new GUIContent("Damage"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("baseFireInterval"), new GUIContent("Fire Interval"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("baseAttackRange"), new GUIContent("Attack Range"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("projectileSpeed"), new GUIContent("Projectile Speed"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("projectileHitRadius"), new GUIContent("Hit Radius"));

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Type Modifiers", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("damageMultiplier"), new GUIContent("Damage Multiplier"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("attackSpeedMultiplier"), new GUIContent("Attack Speed Multiplier"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("rangeMultiplier"), new GUIContent("Range Multiplier"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("baseProjectileCount"), new GUIContent("Base Projectile Count"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("rankDamageMultiplier"), new GUIContent("Rank Damage Multiplier"));

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Special Effects", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("explosionRadius"), new GUIContent("Explosion Radius"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("explosionDamage"), new GUIContent("Explosion Damage"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("slowDuration"), new GUIContent("Slow Duration"));
                EditorGUILayout.PropertyField(serializedDefinition.FindProperty("slowMultiplier"), new GUIContent("Slow Multiplier"));

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
            var projectileCount = definition.towerType == TowerType.Purple
                ? Mathf.Max(1, definition.baseProjectileCount + (int)previewRank)
                : 1;
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
                EditorGUILayout.LabelField("Direct DPS", directDps.ToString("F1"));
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

            definitions.Sort((left, right) => left.towerType.CompareTo(right.towerType));
            Repaint();
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
