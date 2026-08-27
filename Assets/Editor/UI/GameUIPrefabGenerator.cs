using Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Creates editable UGUI prefab assets for the gameplay HUD when they do not exist yet.</summary>
    public static class GameUIPrefabGenerator
    {
        private const string RootPath = "Assets/Prefabs/UI";
        private const string HudPath = RootPath + "/GameHUD.prefab";
        private const string SelectionPath = RootPath + "/TowerSelectionPanel.prefab";
        private const string MergePath = RootPath + "/TowerMergePreview.prefab";

        [InitializeOnLoadMethod]
        private static void CreateMissingPrefabsAfterReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) == null)
                    CreatePrefabs();
            };
        }

        [MenuItem("Tools/Game/Create UI Prefabs")]
        public static void CreatePrefabs()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(RootPath))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            CreateSelectionPrefab();
            CreateMergePrefab();
            CreateHudPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Game/Install Game HUD In Main Scene")]
        public static void InstallInMainScene()
        {
            CreatePrefabs();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<GameUICanvas>(FindObjectsInactive.Include) != null)
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            PrefabUtility.InstantiatePrefab(prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateHudPrefab()
        {
            var root = new GameObject("Game UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameUICanvas));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateEventSystem(root.transform);
            CreateButton(root.transform, "Create Tower Button", new Vector2(0f, 24f), new Vector2(210f, 56f), new Color(0.42f, 0.28f, 0.62f), "Create Tower", 24, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            CreateLabel(root.transform, "Kill Count", new Vector2(24f, -24f), new Vector2(360f, 48f), 28, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            CreateLabel(root.transform, "Enemy Count", new Vector2(24f, -72f), new Vector2(360f, 48f), 28, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            CreateLabel(root.transform, "Difficulty", new Vector2(0f, -24f), new Vector2(480f, 48f), 28, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            CreateLabel(root.transform, "Money Count", new Vector2(-24f, -24f), new Vector2(360f, 48f), 28, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.2f), Vector2.one, Vector2.one, Vector2.one);
            CreateLabel(root.transform, "Frame Rate", new Vector2(-24f, -72f), new Vector2(240f, 48f), 24, TextAnchor.MiddleRight, Color.white, Vector2.one, Vector2.one, Vector2.one);
            AddNestedPrefab(root.transform, SelectionPath, "Tower Selection Panel");
            AddNestedPrefab(root.transform, MergePath, "Tower Merge Preview");
            SavePrefab(root, HudPath);
        }

        private static void CreateSelectionPrefab()
        {
            var root = new GameObject("Tower Selection Panel", typeof(Image));
            var rect = root.GetComponent<RectTransform>();
            SetRect(rect, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24f, 24f), new Vector2(360f, 330f));
            root.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.1f, 0.88f);
            var icon = CreateImage(root.transform, "Icon", new Vector2(18f, -18f), new Vector2(52f, 52f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            icon.preserveAspect = true;
            CreateLabel(root.transform, "Title", new Vector2(82f, -12f), new Vector2(258f, 42f), 24, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            CreateLabel(root.transform, "Stats", new Vector2(18f, -78f), new Vector2(324f, 100f), 22, TextAnchor.UpperLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            CreateLabel(root.transform, "Special", new Vector2(18f, -184f), new Vector2(324f, 42f), 20, TextAnchor.UpperLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            CreateLabel(root.transform, "Sell Value", new Vector2(18f, -232f), new Vector2(324f, 28f), 20, TextAnchor.MiddleLeft, new Color(0.75f, 1f, 0.55f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            CreateButton(root.transform, "Sell Button", new Vector2(0f, 14f), new Vector2(324f, 42f), new Color(0.66f, 0.2f, 0.18f), "Sell Tower", 21, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            SavePrefab(root, SelectionPath);
        }

        private static void CreateMergePrefab()
        {
            var root = new GameObject("Tower Merge Preview", typeof(Image));
            var rect = root.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 142f));
            root.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.1f, 0.94f);
            CreateLabel(root.transform, "Title", new Vector2(14f, -10f), new Vector2(312f, 34f), 21, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            CreateLabel(root.transform, "Details", new Vector2(14f, -46f), new Vector2(312f, 88f), 18, TextAnchor.UpperLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            SavePrefab(root, MergePath);
        }

        private static void CreateEventSystem(Transform parent)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(parent, false);
        }

        private static void AddNestedPrefab(Transform parent, string path, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            instance.name = name;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var item = new GameObject(name, typeof(Image));
            item.transform.SetParent(parent, false);
            SetRect(item.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, position, size);
            return item.GetComponent<Image>();
        }

        private static void CreateButton(Transform parent, string name, Vector2 position, Vector2 size, Color color, string labelText, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var item = new GameObject(name, typeof(Image), typeof(Button));
            item.transform.SetParent(parent, false);
            SetRect(item.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, position, size);
            var image = item.GetComponent<Image>();
            image.color = color;
            item.GetComponent<Button>().targetGraphic = image;
            var label = CreateLabel(item.transform, "Label", Vector2.zero, Vector2.zero, fontSize, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.text = labelText;
        }

        private static Text CreateLabel(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var item = new GameObject(name, typeof(Text));
            item.transform.SetParent(parent, false);
            SetRect(item.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, position, size);
            var text = item.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
