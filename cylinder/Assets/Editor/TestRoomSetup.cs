using System.Collections.Generic;
using System.IO;
using Cylinder.CameraSystem;
using Cylinder.Core;
using Cylinder.Enemy;
using Cylinder.Player;
using Cylinder.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cylinder.EditorTools
{
    /// <summary>
    /// docs/씬설정가이드.md 기준으로 TestRoom 씬을 구성한다.
    /// </summary>
    public static class TestRoomSetup
    {
        private const string ScenePath = "Assets/Scenes/TestRoom.unity";
        private const string SpritePath = "Assets/Sprites/WhiteSquare.png";

        [InitializeOnLoadMethod]
        private static void BuildIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                    return;

                if (File.Exists(Path.Combine(Application.dataPath, "Scenes/TestRoom.unity")))
                    return;

                Debug.Log("[TestRoomSetup] TestRoom 씬이 없어 가이드 기준으로 생성합니다.");
                Build();
            };
        }

        [MenuItem("Cylinder/Setup TestRoom")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void Build()
        {
            EnsureTagsAndLayers();
            Physics2D.gravity = new Vector2(0f, -20f);
            IgnorePlayerEnemyCollision();

            Sprite square = EnsureSquareSprite();

            if (!Directory.Exists(Path.Combine(Application.dataPath, "Scenes")))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera mainCamera = CreateCamera();
            CreateGlobalLight();

            GameObject respawn = CreateEmpty("RespawnPoint", Vector3.zero);
            GameObject player = CreatePlayer(square, respawn.transform);
            CreateTerrain(square);
            CreateDummies(square);
            CreateGaugeUi(player.GetComponent<PressureGauge>());

            SmoothFollowCamera follow = mainCamera.GetComponent<SmoothFollowCamera>();
            SetObjectRef(follow, "_target", player.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TestRoomSetup] TestRoom 씬 생성 완료: " + ScenePath);
        }

        private static void EnsureTagsAndLayers()
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty tags = tagManager.FindProperty("tags");
            bool hasEnemyTag = false;
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == "Enemy")
                    hasEnemyTag = true;
            }

            if (!hasEnemyTag)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Enemy";
            }

            SerializedProperty layers = tagManager.FindProperty("layers");
            layers.GetArrayElementAtIndex(6).stringValue = "Ground";
            layers.GetArrayElementAtIndex(7).stringValue = "Enemy";
            layers.GetArrayElementAtIndex(8).stringValue = "Player";
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void IgnorePlayerEnemyCollision()
        {
            int player = LayerMask.NameToLayer("Player");
            int enemy = LayerMask.NameToLayer("Enemy");
            if (player >= 0 && enemy >= 0)
                Physics2D.IgnoreLayerCollision(player, enemy, true);
        }

        private static Sprite EnsureSquareSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
                AssetDatabase.CreateFolder("Assets", "Sprites");

            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(SpritePath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static Camera CreateCamera()
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            cam.depth = -1;

            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<SmoothFollowCamera>();
            return cam;
        }

        private static void CreateGlobalLight()
        {
            GameObject lightGo = new GameObject("Global Light 2D");
            Light2D light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
        }

        private static GameObject CreatePlayer(Sprite square, Transform respawnPoint)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            GameObject player = CreateEmpty("Player", Vector3.zero);
            player.tag = "Player";
            player.layer = playerLayer;

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(0.5f, 1.8f);
            capsule.offset = new Vector2(0f, 0.9f);

            player.AddComponent<PressureGauge>();
            PlayerController controller = player.AddComponent<PlayerController>();
            SetObjectRef(controller, "_respawnPoint", respawnPoint);

            GameObject visual = CreateSpriteChild(player.transform, "Visual", square,
                new Color(0f, 150f / 255f, 1f), new Vector3(0.5f, 1.8f, 1f));
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.layer = playerLayer;
            return player;
        }

        private static void CreateTerrain(Sprite square)
        {
            int ground = LayerMask.NameToLayer("Ground");
            Color floor = new Color(100f / 255f, 100f / 255f, 100f / 255f);
            Color wall = new Color(60f / 255f, 60f / 255f, 60f / 255f);
            Color platform = new Color(150f / 255f, 100f / 255f, 50f / 255f);

            CreateSolid("Ground_Floor", new Vector3(0f, -2f, 0f), new Vector3(20f, 1f, 1f), ground, square, floor);
            CreateSolid("Wall_Left", new Vector3(-10f, 3f, 0f), new Vector3(1f, 10f, 1f), ground, square, wall);
            CreateSolid("Wall_Right", new Vector3(10f, 3f, 0f), new Vector3(1f, 10f, 1f), ground, square, wall);
            CreateSolid("Ceiling", new Vector3(0f, 8f, 0f), new Vector3(10f, 1f, 1f), ground, square, wall);
            CreateSolid("Platform_Step", new Vector3(3f, -1f, 0f), new Vector3(3f, 0.5f, 1f), ground, square, platform);
            CreateSolid("Platform_Air", new Vector3(-3f, 2f, 0f), new Vector3(2f, 0.5f, 1f), ground, square, platform);
        }

        private static void CreateDummies(Sprite square)
        {
            CreateDummy("Dummy_1", new Vector3(5f, -1f, 0f), square);
            CreateDummy("Dummy_2", new Vector3(-5f, -1f, 0f), square);
            CreateDummy("Dummy_3", new Vector3(-6f, -1f, 0f), square);
            CreateDummy("Dummy_4", new Vector3(-7f, -1f, 0f), square);
            CreateDummy("Dummy_5", new Vector3(8f, -1f, 0f), square);
            CreateDummy("Dummy_6", new Vector3(-3f, 3f, 0f), square);
        }

        private static GameObject CreateDummy(string name, Vector3 position, Sprite square)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            GameObject dummy = CreateEmpty(name, position);
            dummy.tag = "Enemy";
            dummy.layer = enemyLayer;

            BoxCollider2D box = dummy.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.5f, 1.8f);
            box.offset = new Vector2(0f, 0.9f);

            Rigidbody2D rb = dummy.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            Dummy dummyComp = dummy.AddComponent<Dummy>();
            GameObject visual = CreateSpriteChild(dummy.transform, "Visual", square,
                new Color(1f, 50f / 255f, 50f / 255f), new Vector3(0.5f, 1.8f, 1f));
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.layer = enemyLayer;
            SetObjectRef(dummyComp, "_visualObject", visual);
            return dummy;
        }

        private static void CreateGaugeUi(PressureGauge gauge)
        {
            GameObject canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            GameObject panel = new GameObject("GaugePanel");
            panel.transform.SetParent(canvasGo.transform, false);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 150f / 255f);
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = new Vector2(150f, -50f);
            panelRt.sizeDelta = new Vector2(250f, 80f);

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(panel.transform, false);
            Image fill = fillGo.AddComponent<Image>();
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            fill.color = new Color(1f, 150f / 255f, 0f, 1f);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(10f, 10f);
            fillRt.offsetMax = new Vector2(-10f, -10f);

            GameObject textGo = new GameObject("GaugeText");
            textGo.transform.SetParent(panel.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = "2.0 / 4.0";
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 0.5f);
            textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = new Vector2(230f, 60f);

            GaugeUI gaugeUi = panel.AddComponent<GaugeUI>();
            SetObjectRef(gaugeUi, "_gauge", gauge);
            SetObjectRef(gaugeUi, "_fillImage", fill);
            SetObjectRef(gaugeUi, "_gaugeText", text);
        }

        private static GameObject CreateSolid(string name, Vector3 position, Vector3 scale, int layer, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name);
            go.layer = layer;
            go.transform.position = position;
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            go.AddComponent<BoxCollider2D>();
            return go;
        }

        private static GameObject CreateSpriteChild(Transform parent, string name, Sprite sprite, Color color, Vector3 scale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 1;
            return go;
        }

        private static GameObject CreateEmpty(string name, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            return go;
        }

        private static void SetObjectRef(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError("[TestRoomSetup] 필드를 찾지 못함: " + field + " on " + target);
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == ScenePath)
                    return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
