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
    /// TestRoom 메뉴 도구. 저장된 씬이 기준이며, 에디터 로드 시 자동으로 씬을 덮어쓰지 않는다.
    /// Cylinder 메뉴는 사용자가 직접 실행할 때만 동작한다.
    /// </summary>
    public static class TestRoomSetup
    {
        private const string ScenePath = "Assets/Scenes/TestRoom.unity";
        private const string SpritePath = "Assets/Sprites/WhiteSquare.png";
        private const string NoFrictionPath = "Assets/Physics/NoFriction.physicsMaterial2D";

        [MenuItem("Cylinder/Apply Vertical Course")]
        public static void ApplyVerticalCourseToOpenScene()
        {
            PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();
            int ground = LayerMask.NameToLayer("Ground");
            Sprite square = EnsureSquareSprite();
            Color wall = new Color(60f / 255f, 60f / 255f, 60f / 255f);
            Color platform = new Color(150f / 255f, 100f / 255f, 50f / 255f);

            SetNamedTransform("Wall_Left", new Vector3(-48.5f, 9.66f, 0f), new Vector3(1f, 24.32f, 1f));
            SetNamedTransform("Wall_Right", new Vector3(40.5f, 9.66f, 0f), new Vector3(1f, 24.32f, 1f));
            SetNamedTransform("Ceiling", new Vector3(-32f, 4.52f, 0f), new Vector3(28f, 1f, 1f));

            EnsureSolid("Ceiling_Top", new Vector3(-4f, 21.32f, 0f), new Vector3(88f, 1f, 1f), ground, square, wall);
            EnsureSolid("Floor_L1", new Vector3(20f, 2.5f, 0f), new Vector3(32f, 0.4f, 1f), ground, square, platform);
            EnsureSolid("Floor_L2", new Vector3(-16f, 6.7f, 0f), new Vector3(40f, 0.4f, 1f), ground, square, platform);
            EnsureSolid("Floor_L3", new Vector3(12f, 10.9f, 0f), new Vector3(40f, 0.4f, 1f), ground, square, platform);
            EnsureSolid("Floor_L4", new Vector3(-12f, 15.1f, 0f), new Vector3(40f, 0.4f, 1f), ground, square, platform);

            EnsureDummy("Dummy_5", new Vector3(18f, 2.7f, 0f), square);
            EnsureDummy("Dummy_6", new Vector3(-16f, 6.9f, 0f), square);
            EnsureDummy("Dummy_7", new Vector3(8f, 11.1f, 0f), square);
            EnsureDummy("Dummy_8", new Vector3(-8f, 15.3f, 0f), square);

            Collider2D[] colliders = Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col.gameObject.layer != ground)
                    continue;
                col.sharedMaterial = noFriction;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[TestRoomSetup] 세로 코스 배치 완료. Ground_Floor와 기존 Dummy_2/3/4는 유지.");
        }

        [MenuItem("Cylinder/Apply TestRoom Layout")]
        public static void ApplyLayoutToOpenScene()
        {
            PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();
            int ground = LayerMask.NameToLayer("Ground");
            Sprite square = EnsureSquareSprite();

            SetNamedTransform("Ground_Floor", new Vector3(-4f, -2f, 0f), new Vector3(88f, 1f, 1f));
            SetNamedTransform("Wall_Left", new Vector3(-48.5f, 1.26f, 0f), new Vector3(1f, 7.52f, 1f));
            SetNamedTransform("Wall_Right", new Vector3(40.5f, 1.26f, 0f), new Vector3(1f, 7.52f, 1f));
            SetNamedTransform("Ceiling", new Vector3(-4f, 4.52f, 0f), new Vector3(88f, 1f, 1f));
            SetNamedTransform("Platform_Step", new Vector3(16f, -1.05f, 0f), new Vector3(3f, 0.4f, 1f));
            SetNamedTransform("Platform_Air", new Vector3(-16f, 0.5f, 0f), new Vector3(3f, 0.4f, 1f));

            if (GameObject.Find("Ground_Island") == null)
            {
                Color floor = new Color(100f / 255f, 100f / 255f, 100f / 255f);
                GameObject island = CreateSolid("Ground_Island", new Vector3(45f, -2f, 0f), new Vector3(4f, 1f, 1f), ground, square, floor);
                EditorUtility.SetDirty(island);
            }
            else
            {
                SetNamedTransform("Ground_Island", new Vector3(45f, -2f, 0f), new Vector3(4f, 1f, 1f));
            }

            GameObject respawn = GameObject.Find("RespawnPoint");
            if (respawn != null)
            {
                respawn.transform.position = new Vector3(0f, -1.5f, 0f);
                EditorUtility.SetDirty(respawn);
            }

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(0f, -1.5f, 0f);
                ApplyUnitBody(player, true);
                EditorUtility.SetDirty(player);
            }

            SetDummy("Dummy_1", new Vector3(16f, -0.85f, 0f));
            SetDummy("Dummy_2", new Vector3(-40f, -1.5f, 0f));
            SetDummy("Dummy_3", new Vector3(-24f, -1.5f, 0f));
            SetDummy("Dummy_4", new Vector3(45f, -1.5f, 0f));
            SetDummy("Dummy_5", new Vector3(32f, -1.5f, 0f));
            SetDummy("Dummy_6", new Vector3(-16f, 0.7f, 0f));

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographicSize = 4.8f;
                cam.transform.position = new Vector3(0f, 1f, -10f);
                EditorUtility.SetDirty(cam.gameObject);
            }

            EnsureHelpUiOnCanvas();

            Collider2D[] colliders = Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                bool isGround = col.gameObject.layer == ground;
                bool isPlayer = col.GetComponent<PlayerController>() != null;
                if (!isGround && !isPlayer)
                    continue;
                col.sharedMaterial = noFriction;
                if (col.attachedRigidbody != null)
                    col.attachedRigidbody.sharedMaterial = noFriction;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[TestRoomSetup] TestRoom 레이아웃 갱신 완료");
        }

        private static void SetDummy(string name, Vector3 position)
        {
            GameObject dummy = GameObject.Find(name);
            if (dummy == null)
                return;

            dummy.transform.position = position;
            ApplyUnitBody(dummy, false);
            Rigidbody2D rb = dummy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
            }
            
            Dummy dummyComp = dummy.GetComponent<Dummy>();
            Transform front = dummy.transform.Find("FrontMarker");
            if (front == null)
            {
                Sprite square = EnsureSquareSprite();
                GameObject frontGo = CreateSpriteChild(dummy.transform, "FrontMarker", square,
                    new Color(1f, 0.86f, 0.12f, 1f), new Vector3(0.18f, 1.28f, 1f));
                frontGo.transform.localPosition = new Vector3(GameConstants.P_BODY_WIDTH * 0.5f, GameConstants.P_BODY_OFFSET_Y, 0f);
                frontGo.layer = dummy.layer;
                SpriteRenderer frontRenderer = frontGo.GetComponent<SpriteRenderer>();
                if (frontRenderer != null)
                    frontRenderer.sortingOrder = 2;
                front = frontGo.transform;
            }
            if (dummyComp != null && front != null)
                SetObjectRef(dummyComp, "_frontMarker", front);
            
            EditorUtility.SetDirty(dummy);
        }

        private static void ApplyUnitBody(GameObject unit, bool isCapsule)
        {
            if (isCapsule)
            {
                CapsuleCollider2D capsule = unit.GetComponent<CapsuleCollider2D>();
                if (capsule != null)
                {
                    capsule.size = new Vector2(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT);
                    capsule.offset = new Vector2(0f, GameConstants.P_BODY_OFFSET_Y);
                }
            }
            else
            {
                BoxCollider2D box = unit.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    box.size = new Vector2(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT);
                    box.offset = new Vector2(0f, GameConstants.P_BODY_OFFSET_Y);
                }
            }

            Transform visual = unit.transform.Find("Visual");
            if (visual != null)
            {
                visual.localScale = new Vector3(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT, 1f);
                visual.localPosition = new Vector3(0f, GameConstants.P_BODY_OFFSET_Y, 0f);
            }
        }

        private static void SetNamedTransform(string name, Vector3 position, Vector3 scale)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
                return;

            go.transform.position = position;
            go.transform.localScale = scale;
            EditorUtility.SetDirty(go);
        }

        [MenuItem("Cylinder/Setup TestRoom")]
        public static void BuildFromMenu()
        {
            Build();
        }

        [MenuItem("Cylinder/Apply No Friction To Scene")]
        public static void ApplyNoFrictionToSceneFromMenu()
        {
            ApplyNoFrictionToOpenScene();
        }

        public static void ApplyNoFrictionToOpenScene()
        {
            PhysicsMaterial2D material = EnsureNoFrictionMaterial();
            int ground = LayerMask.NameToLayer("Ground");
            Collider2D[] colliders = Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int applied = 0;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                bool isGround = col.gameObject.layer == ground;
                bool isPlayer = col.GetComponent<PlayerController>() != null;
                if (!isGround && !isPlayer)
                    continue;

                col.sharedMaterial = material;
                Rigidbody2D rb = col.attachedRigidbody;
                if (rb != null)
                    rb.sharedMaterial = material;

                EditorUtility.SetDirty(col.gameObject);
                applied++;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[TestRoomSetup] NoFriction 적용: " + applied);
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

            GameObject respawn = CreateEmpty("RespawnPoint", new Vector3(0f, -1.5f, 0f));
            GameObject player = CreatePlayer(square, respawn.transform);
            CreateTerrain(square);
            CreateDummies(square);
            CreateGaugeUi(player.GetComponent<PressureGauge>());
            EnsureHelpUiOnCanvas();

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
            camGo.transform.position = new Vector3(0f, 1f, -10f);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.8f;
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
            GameObject player = CreateEmpty("Player", new Vector3(0f, -1.5f, 0f));
            player.tag = "Player";
            player.layer = playerLayer;

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();
            rb.sharedMaterial = noFriction;

            CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = new Vector2(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT);
            capsule.offset = new Vector2(0f, GameConstants.P_BODY_OFFSET_Y);
            capsule.sharedMaterial = noFriction;

            player.AddComponent<PressureGauge>();
            PlayerController controller = player.AddComponent<PlayerController>();
            SetObjectRef(controller, "_respawnPoint", respawnPoint);

            GameObject visual = CreateSpriteChild(player.transform, "Visual", square,
                new Color(0f, 150f / 255f, 1f), new Vector3(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT, 1f));
            visual.transform.localPosition = new Vector3(0f, GameConstants.P_BODY_OFFSET_Y, 0f);
            visual.layer = playerLayer;
            return player;
        }

        private static void CreateTerrain(Sprite square)
        {
            int ground = LayerMask.NameToLayer("Ground");
            Color floor = new Color(100f / 255f, 100f / 255f, 100f / 255f);
            Color wall = new Color(60f / 255f, 60f / 255f, 60f / 255f);
            Color platform = new Color(150f / 255f, 100f / 255f, 50f / 255f);

            CreateSolid("Ground_Floor", new Vector3(-4f, -2f, 0f), new Vector3(88f, 1f, 1f), ground, square, floor);
            CreateSolid("Ground_Island", new Vector3(45f, -2f, 0f), new Vector3(4f, 1f, 1f), ground, square, floor);
            CreateSolid("Wall_Left", new Vector3(-48.5f, 1.26f, 0f), new Vector3(1f, 7.52f, 1f), ground, square, wall);
            CreateSolid("Wall_Right", new Vector3(40.5f, 1.26f, 0f), new Vector3(1f, 7.52f, 1f), ground, square, wall);
            CreateSolid("Ceiling", new Vector3(-4f, 4.52f, 0f), new Vector3(88f, 1f, 1f), ground, square, wall);
            CreateSolid("Platform_Step", new Vector3(16f, -1.05f, 0f), new Vector3(3f, 0.4f, 1f), ground, square, platform);
            CreateSolid("Platform_Air", new Vector3(-16f, 0.5f, 0f), new Vector3(3f, 0.4f, 1f), ground, square, platform);
        }

        private static void CreateDummies(Sprite square)
        {
            CreateDummy("Dummy_1", new Vector3(16f, -0.85f, 0f), square);
            CreateDummy("Dummy_2", new Vector3(-40f, -1.5f, 0f), square);
            CreateDummy("Dummy_3", new Vector3(-24f, -1.5f, 0f), square);
            CreateDummy("Dummy_4", new Vector3(45f, -1.5f, 0f), square);
            CreateDummy("Dummy_5", new Vector3(32f, -1.5f, 0f), square);
            CreateDummy("Dummy_6", new Vector3(-16f, 0.7f, 0f), square);
        }

        private static GameObject CreateDummy(string name, Vector3 position, Sprite square)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            GameObject dummy = CreateEmpty(name, position);
            dummy.tag = "Enemy";
            dummy.layer = enemyLayer;

            BoxCollider2D box = dummy.AddComponent<BoxCollider2D>();
            box.size = new Vector2(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT);
            box.offset = new Vector2(0f, GameConstants.P_BODY_OFFSET_Y);

            Rigidbody2D rb = dummy.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            Dummy dummyComp = dummy.AddComponent<Dummy>();
            GameObject visual = CreateSpriteChild(dummy.transform, "Visual", square,
                new Color(1f, 50f / 255f, 50f / 255f), new Vector3(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT, 1f));
            visual.transform.localPosition = new Vector3(0f, GameConstants.P_BODY_OFFSET_Y, 0f);
            visual.layer = enemyLayer;
            SetObjectRef(dummyComp, "_visualObject", visual);
            
            GameObject front = CreateSpriteChild(dummy.transform, "FrontMarker", square,
                new Color(1f, 0.86f, 0.12f, 1f), new Vector3(0.18f, 1.28f, 1f));
            front.transform.localPosition = new Vector3(GameConstants.P_BODY_WIDTH * 0.5f, GameConstants.P_BODY_OFFSET_Y, 0f);
            front.layer = enemyLayer;
            SpriteRenderer frontRenderer = front.GetComponent<SpriteRenderer>();
            if (frontRenderer != null)
                frontRenderer.sortingOrder = 2;
            SetObjectRef(dummyComp, "_frontMarker", front.transform);
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
            text.text = "4.0 / 5.0";
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

        private static void EnsureHelpUiOnCanvas()
        {
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
                return;

            HelpUI help = canvasGo.GetComponent<HelpUI>();
            if (help == null)
                help = canvasGo.AddComponent<HelpUI>();

            help.BuildIfMissing();
            EditorUtility.SetDirty(canvasGo);
        }

        private static void EnsureSolid(string name, Vector3 position, Vector3 scale, int layer, Sprite sprite, Color color)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                go = CreateSolid(name, position, scale, layer, sprite, color);
                EditorUtility.SetDirty(go);
                return;
            }

            go.transform.position = position;
            go.transform.localScale = scale;
            EditorUtility.SetDirty(go);
        }

        private static void EnsureDummy(string name, Vector3 position, Sprite square)
        {
            if (GameObject.Find(name) != null)
                return;

            GameObject dummy = CreateDummy(name, position, square);
            EditorUtility.SetDirty(dummy);
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
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.sharedMaterial = EnsureNoFrictionMaterial();
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

        private static PhysicsMaterial2D EnsureNoFrictionMaterial()
        {
            PhysicsMaterial2D existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
            if (existing != null)
            {
                existing.friction = 0f;
                existing.bounciness = 0f;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Physics"))
                AssetDatabase.CreateFolder("Assets", "Physics");

            PhysicsMaterial2D material = new PhysicsMaterial2D("NoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            AssetDatabase.CreateAsset(material, NoFrictionPath);
            return material;
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
