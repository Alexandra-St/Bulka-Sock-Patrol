using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BulkoGame.Audio;
using BulkoGame.Collectibles;
using BulkoGame.Defense;
using BulkoGame.GameFlow;
using BulkoGame.Levels;
using BulkoGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BulkoGame.Editor
{
    public sealed class LevelAuthoringCommands : EditorWindow
    {
        private const string LevelsPath = "Assets/Levels";
        private const string SharedDefaultsPath = "Assets/Levels/Level_01/Level_01Definition.asset";
        private const string CreateMenu = "BulkoGame/Levels/Create New Level";
        private const string ConfigureMenu = "BulkoGame/Levels/Configure Shared Setup";

        private string levelId = "Level_02";
        private string displayName = "Level 02";
        private string status;

        [MenuItem(CreateMenu, false, 10)]
        private static void OpenCreateWindow()
        {
            var window = GetWindow<LevelAuthoringCommands>(true, "Create New Level", true);
            window.minSize = new Vector2(380f, 150f);
            window.maxSize = new Vector2(520f, 170f);
            window.ShowUtility();
        }

        [MenuItem(ConfigureMenu, false, 11)]
        private static void ConfigureCurrentGameplayScene()
        {
            if (!TryConfigureScene(SceneManager.GetActiveScene(), out string message))
            {
                EditorUtility.DisplayDialog("Configure Shared Setup", message, "OK");
                Debug.LogError("Configure Shared Setup: " + message);
                return;
            }

            Debug.Log("Configure Shared Setup: " + message);
        }

        [MenuItem(ConfigureMenu, true)]
        private static bool ValidateConfigureMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && SceneManager.GetActiveScene().IsValid()
                && SceneManager.GetActiveScene().isLoaded;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create a reusable gameplay scene scaffold.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8f);
            levelId = EditorGUILayout.TextField("Level ID", levelId);
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            if (!string.IsNullOrEmpty(status))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(status, MessageType.Error);
            }
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(90f))) Close();
                if (GUILayout.Button("Create", GUILayout.Width(90f)))
                {
                    if (TryCreateNewLevel(levelId, displayName, out string message)) Close();
                    else status = message;
                }
            }
        }

        internal static bool TryCreateNewLevel(string requestedId, string requestedDisplayName, out string message)
        {
            message = null;
            string id = requestedId != null ? requestedId.Trim() : string.Empty;
            string title = requestedDisplayName != null ? requestedDisplayName.Trim() : string.Empty;
            if (!IsValidLevelId(id))
            {
                message = "Level ID must start with a letter and contain only letters, numbers, '_' or '-'.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                message = "Enter a display name.";
                return false;
            }
            if (!AssetDatabase.IsValidFolder(LevelsPath))
            {
                message = $"Required folder '{LevelsPath}' does not exist.";
                return false;
            }

            string folderPath = $"{LevelsPath}/{id}";
            string definitionPath = $"{folderPath}/{id}Definition.asset";
            string scenePath = $"{folderPath}/{id}.unity";
            if (AssetDatabase.GetSubFolders(LevelsPath)
                    .Select(path => Path.GetFileName(path))
                    .Any(folderName => string.Equals(folderName, id, StringComparison.OrdinalIgnoreCase))
                || File.Exists(scenePath) || File.Exists(definitionPath))
            {
                message = $"Level '{id}' already exists. Choose a different ID; existing files will not be overwritten.";
                return false;
            }

            var defaults = AssetDatabase.LoadAssetAtPath<LevelDefinition>(SharedDefaultsPath);
            if (defaults == null || defaults.balance == null || defaults.audio == null
                || defaults.art == null || defaults.environmentArt == null)
            {
                message = $"Shared defaults are incomplete. Check '{SharedDefaultsPath}' and its balance/audio/art references.";
                return false;
            }

            string guid = AssetDatabase.CreateFolder(LevelsPath, id);
            if (string.IsNullOrEmpty(guid))
            {
                message = $"Could not create '{folderPath}'.";
                return false;
            }

            Scene createdScene = default;
            bool sceneWasCreated = false;
            try
            {
                var definition = CreateInstance<LevelDefinition>();
                definition.levelId = id;
                definition.displayName = title;
                definition.balance = defaults.balance;
                definition.audio = defaults.audio;
                definition.art = defaults.art;
                definition.environmentArt = defaults.environmentArt;
                definition.sockCount = 10;
                definition.minimumSocksPerZone = 1;
                definition.maximumSocksPerZone = 3;
                definition.catRouteSequence = Array.Empty<string>();
                AssetDatabase.CreateAsset(definition, definitionPath);

                // Persist/import the definition first so scene components receive the
                // canonical persistent asset object, not the just-created transient reference.
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(definitionPath, ImportAssetOptions.ForceSynchronousImport);
                var persistentDefinition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(definitionPath);
                if (persistentDefinition == null || !EditorUtility.IsPersistent(persistentDefinition))
                    throw new InvalidOperationException($"LevelDefinition '{definitionPath}' was not imported as a persistent asset.");

                createdScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                sceneWasCreated = true;
                BuildNewLevelScaffold(id, persistentDefinition);
                EditorSceneManager.MarkSceneDirty(createdScene);
                if (!EditorSceneManager.SaveScene(createdScene, scenePath))
                    throw new InvalidOperationException($"Unity could not save '{scenePath}'.");

                AssetDatabase.SaveAssets();
                createdScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var savedBootstrap = FindInScene<LevelBootstrap>(createdScene).SingleOrDefault();
                var savedBindings = FindInScene<LevelSceneBindings>(createdScene).SingleOrDefault();
                if (savedBootstrap == null || savedBindings == null
                    || savedBootstrap.definition != persistentDefinition
                    || savedBindings.definition != persistentDefinition
                    || savedBootstrap.sceneBindings != savedBindings)
                    throw new InvalidOperationException("The saved level scene did not retain its LevelDefinition and scene bindings references.");

                if (SceneManager.GetActiveScene() != createdScene)
                    throw new InvalidOperationException("Unity did not make the newly opened level scene active.");
                message = $"Created '{folderPath}' with a LevelDefinition and an empty gameplay scaffold. Add layout and authoring data, then run Validate Current Level.";
                Debug.Log("Create New Level: " + message);
                return true;
            }
            catch (Exception exception)
            {
                if (sceneWasCreated && createdScene.IsValid() && createdScene.isLoaded)
                    EditorSceneManager.CloseScene(createdScene, true);
                AssetDatabase.DeleteAsset(folderPath);
                AssetDatabase.Refresh();
                message = "Could not create the level: " + exception.Message;
                return false;
            }
        }

        private static void BuildNewLevelScaffold(string id, LevelDefinition definition)
        {
            var levelRoot = CreateObject(id, null);
            var environment = CreateObject("Environment", levelRoot.transform);
            CreateObject("Floors", environment.transform);
            CreateObject("Walls", environment.transform);
            CreateObject("Furniture", environment.transform);
            CreateObject("Decor", environment.transform);

            var authoring = CreateObject("Authoring", levelRoot.transform);
            var playerSpawn = CreateObject("PlayerSpawn", authoring.transform);
            var basketAnchor = CreateObject("BasketAnchor", authoring.transform);
            CreateObject("SockSpawnPoints", authoring.transform);
            CreateObject("CatRoutes", authoring.transform);
            CreateObject("TrapZones", authoring.transform);

            var systems = CreateObject("Systems", levelRoot.transform);
            var bootstrap = Undo.AddComponent<LevelBootstrap>(systems);
            var bindings = Undo.AddComponent<LevelSceneBindings>(systems);
            Camera gameplayCamera = CreateDefaultCamera(systems.transform);

            // Complete Undo-based object/component creation before writing serialized
            // references; subsequent RegisterCreatedObjectUndo calls can flush edits.
            bootstrap.definition = definition;
            bootstrap.sceneBindings = bindings;
            bindings.definition = definition;
            bindings.playerSpawn = playerSpawn.transform;
            bindings.basketRoot = basketAnchor.transform;
            bindings.routes = Array.Empty<CatRoute>();
            bindings.trapZones = Array.Empty<TrapPlacementZone>();
            bindings.sockSpawnPoints = Array.Empty<SockSpawnPoint>();
            bindings.gameplayCamera = gameplayCamera;
            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(bindings);
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create level scaffold object");
            if (parent != null) gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Camera CreateDefaultCamera(Transform parent, bool addAudioListener = true)
        {
            var cameraObject = CreateObject("Gameplay Camera", parent);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -20f);
            cameraObject.transform.localRotation = Quaternion.identity;

            var camera = Undo.AddComponent<Camera>(cameraObject);
            camera.orthographic = true;
            camera.orthographicSize = 9f;
            Undo.AddComponent<AspectFitCamera>(cameraObject);
            if (addAudioListener) Undo.AddComponent<AudioListener>(cameraObject);
            return camera;
        }

        internal static bool TryConfigureScene(Scene scene, out string message)
        {
            message = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                message = "Stop Play Mode before configuring a gameplay scene.";
                return false;
            }
            if (!scene.IsValid() || !scene.isLoaded)
            {
                message = "Open a gameplay scene before running Configure Shared Setup.";
                return false;
            }

            var bootstraps = FindInScene<LevelBootstrap>(scene);
            var bindingsList = FindInScene<LevelSceneBindings>(scene);
            if (bootstraps.Length > 1 || bindingsList.Length > 1)
            {
                message = $"Expected at most one LevelBootstrap and one LevelSceneBindings; found {bootstraps.Length} and {bindingsList.Length}. Resolve duplicates manually; the tool will not delete objects.";
                return false;
            }

            LevelBootstrap bootstrap = bootstraps.FirstOrDefault();
            LevelSceneBindings bindings = bindingsList.FirstOrDefault();
            LevelDefinition definition = bootstrap != null ? bootstrap.definition : null;
            if (definition != null && bindings != null && bindings.definition != null && bindings.definition != definition)
            {
                message = "LevelBootstrap and LevelSceneBindings reference different LevelDefinition assets. Choose the intended definition and reconnect them manually.";
                return false;
            }
            if (definition == null && bindings != null) definition = bindings.definition;
            if (definition == null) definition = TryLoadDefinitionForScene(scene);
            if (definition == null)
            {
                message = $"No LevelDefinition is assigned (bootstraps={bootstraps.Length}, bindings={bindingsList.Length}, bootstrapDefinition={(bootstrap != null && bootstrap.definition != null)}, bindingsDefinition={(bindings != null && bindings.definition != null)}). Assign the intended LevelDefinition to either component, or place '{scene.name}Definition.asset' beside this scene.";
                return false;
            }
            var sharedDefaults = AssetDatabase.LoadAssetAtPath<LevelDefinition>(SharedDefaultsPath);
            if (sharedDefaults == null || sharedDefaults.balance == null || sharedDefaults.audio == null
                || sharedDefaults.art == null || sharedDefaults.environmentArt == null)
            {
                message = $"Shared defaults are incomplete. Check '{SharedDefaultsPath}' and its shared references.";
                return false;
            }

            Transform playerSpawn = ResolveAuthorAnchor(scene, bindings != null ? bindings.playerSpawn : null, "PlayerSpawn", out string anchorError);
            if (playerSpawn == null) { message = anchorError; return false; }
            Transform basketAnchor = ResolveAuthorAnchor(scene, bindings != null ? bindings.basketRoot : null, "BasketAnchor", out anchorError);
            if (basketAnchor == null) { message = anchorError; return false; }

            var systemsObjects = FindNamedObjects(scene, "Systems");
            if (bootstrap == null && bindings == null && systemsObjects.Length > 1)
            {
                message = "Multiple objects named 'Systems' were found. Keep or select one technical root before configuring.";
                return false;
            }

            Camera camera = bindings != null && IsInScene(bindings.gameplayCamera, scene)
                ? bindings.gameplayCamera : null;
            if (camera != null && !camera.isActiveAndEnabled)
            {
                message = "The assigned gameplay camera is inactive or disabled. Enable it manually; Configure will not change its authored state.";
                return false;
            }
            if (camera == null)
            {
                var activeCameras = FindInScene<Camera>(scene).Where(item => item.isActiveAndEnabled).ToArray();
                if (activeCameras.Length == 1) camera = activeCameras[0];
                else if (activeCameras.Length > 1)
                {
                    var tagged = activeCameras.Where(item => item.CompareTag("MainCamera")).ToArray();
                    if (tagged.Length == 1) camera = tagged[0];
                    else
                    {
                        message = "Several active cameras exist and no unique MainCamera can be selected. Assign gameplayCamera manually.";
                        return false;
                    }
                }
                else if (FindInScene<Camera>(scene).Length > 0)
                {
                    message = "Camera components exist but none are active and enabled. Enable the intended camera manually.";
                    return false;
                }
            }

            int activeListeners = FindInScene<AudioListener>(scene)
                .Count(listener => listener.enabled && listener.gameObject.activeInHierarchy);
            if (activeListeners > 1)
            {
                message = $"Found {activeListeners} active AudioListeners. Keep one enabled before configuring; the tool will not remove authored components.";
                return false;
            }

            if (!TryFindAuthoredComponents(scene, "CatRoutes", out CatRoute[] discoveredRoutes, out string collectionError)
                || !TryFindAuthoredComponents(scene, "TrapZones", out TrapPlacementZone[] discoveredTraps, out collectionError)
                || !TryFindAuthoredComponents(scene, "SockSpawnPoints", out SockSpawnPoint[] discoveredSockPoints, out collectionError))
            {
                message = collectionError;
                return false;
            }

            GameObject systemsRoot = null;
            if (bootstrap == null || bindings == null || camera == null || activeListeners == 0)
            {
                systemsRoot = bootstrap != null ? bootstrap.gameObject
                    : bindings != null ? bindings.gameObject
                    : systemsObjects.FirstOrDefault();
                if (systemsRoot == null)
                {
                    systemsRoot = new GameObject("Systems");
                    Undo.RegisterCreatedObjectUndo(systemsRoot, "Create level Systems root");
                    SceneManager.MoveGameObjectToScene(systemsRoot, scene);
                }
            }

            if (bootstrap == null)
            {
                bootstrap = Undo.AddComponent<LevelBootstrap>(systemsRoot);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            if (bindings == null)
            {
                bindings = Undo.AddComponent<LevelSceneBindings>(systemsRoot);
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Undo.RecordObjects(new UnityEngine.Object[] { bootstrap, bindings }, "Configure shared level setup");
            Undo.RecordObject(definition, "Restore missing shared LevelDefinition references");
            bootstrap.definition = definition;
            bootstrap.sceneBindings = bindings;
            bindings.definition = definition;
            bindings.playerSpawn = playerSpawn;
            bindings.basketRoot = basketAnchor;

            bool definitionChanged = FillMissingSharedReferences(definition, out string defaultsError);
            if (definitionChanged) EditorUtility.SetDirty(definition);
            if (!string.IsNullOrEmpty(defaultsError))
            {
                message = defaultsError;
                return false;
            }

            if (camera == null)
            {
                camera = CreateDefaultCamera(systemsRoot.transform, activeListeners == 0);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            bindings.gameplayCamera = camera;

            if (activeListeners == 0)
            {
                var listener = camera.GetComponent<AudioListener>();
                if (listener == null) listener = Undo.AddComponent<AudioListener>(camera.gameObject);
                else Undo.RecordObject(listener, "Enable gameplay AudioListener");
                if (!listener.enabled) listener.enabled = true;
                EditorUtility.SetDirty(listener);
                EditorSceneManager.MarkSceneDirty(scene);
            }

            bindings.routes = MergeSceneReferences(bindings.routes, discoveredRoutes, scene);
            bindings.trapZones = MergeSceneReferences(bindings.trapZones, discoveredTraps, scene);
            bindings.sockSpawnPoints = MergeSceneReferences(bindings.sockSpawnPoints, discoveredSockPoints, scene);
            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(bindings);
            EditorSceneManager.MarkSceneDirty(scene);

            message = $"Connected LevelDefinition '{definition.levelId}', Bootstrap, bindings and camera; collected {bindings.routes.Length} routes, {bindings.trapZones.Length} trap zones and {bindings.sockSpawnPoints.Length} sock points. Existing authored transforms and populated definition values were preserved. Scene changes are unsaved.";
            return true;
        }

        private static Transform ResolveAuthorAnchor(Scene scene, Transform assigned, string objectName, out string error)
        {
            error = null;
            if (assigned != null)
            {
                if (assigned.gameObject.scene == scene) return assigned;
                error = $"Assigned {objectName} belongs to another scene. Reconnect it manually.";
                return null;
            }

            var matches = FindNamedObjects(scene, objectName);
            if (matches.Length == 1) return matches[0].transform;
            error = matches.Length == 0
                ? $"Author-owned anchor '{objectName}' is missing. Add it at the intended level position, then configure again."
                : $"Multiple author-owned anchors named '{objectName}' exist. Resolve them manually.";
            return null;
        }

        private static bool FillMissingSharedReferences(LevelDefinition definition, out string error)
        {
            error = null;
            var defaults = AssetDatabase.LoadAssetAtPath<LevelDefinition>(SharedDefaultsPath);
            if (defaults == null)
            {
                error = $"Could not load shared defaults from '{SharedDefaultsPath}'.";
                return false;
            }

            bool changed = false;
            if (definition.balance == null && defaults.balance != null) { definition.balance = defaults.balance; changed = true; }
            if (definition.audio == null && defaults.audio != null) { definition.audio = defaults.audio; changed = true; }
            if (definition.art == null && defaults.art != null) { definition.art = defaults.art; changed = true; }
            if (definition.environmentArt == null && defaults.environmentArt != null) { definition.environmentArt = defaults.environmentArt; changed = true; }

            if (definition.balance == null || definition.audio == null || definition.art == null || definition.environmentArt == null)
                error = $"LevelDefinition '{definition.levelId}' is missing one or more shared references and the defaults asset is incomplete. Existing non-empty values were preserved.";
            return changed;
        }

        private static LevelDefinition TryLoadDefinitionForScene(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.path)) return null;

            string sceneFolder = Path.GetDirectoryName(scene.path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(sceneFolder)) return null;

            // Recovery is deliberately limited to the scene's own sibling definition
            // asset; never search arbitrary project assets by name or display label.
            string definitionPath = $"{sceneFolder}/{Path.GetFileNameWithoutExtension(scene.path)}Definition.asset";
            var definition = AssetDatabase.LoadAssetAtPath<LevelDefinition>(definitionPath);
            if (definition == null) return null;

            string sceneId = Path.GetFileNameWithoutExtension(scene.path);
            return string.Equals(definition.levelId, sceneId, StringComparison.OrdinalIgnoreCase)
                ? definition
                : null;
        }

        private static T[] MergeSceneReferences<T>(T[] existing, T[] discovered, Scene scene) where T : Component
        {
            var merged = new List<T>();
            var seen = new HashSet<T>();
            if (existing != null)
            {
                foreach (var item in existing)
                    if (item != null && item.gameObject.scene == scene && seen.Add(item)) merged.Add(item);
            }

            foreach (var item in discovered
                         .Where(item => item != null && item.gameObject.scene == scene && !seen.Contains(item))
                         .OrderBy(item => HierarchyPath(item.transform), StringComparer.Ordinal))
            {
                if (seen.Add(item)) merged.Add(item);
            }
            return merged.ToArray();
        }

        private static bool TryFindAuthoredComponents<T>(Scene scene, string authoringChildName, out T[] components, out string error)
            where T : Component
        {
            error = null;
            var authoringRoots = FindNamedObjects(scene, "Authoring");
            if (authoringRoots.Length > 1)
            {
                components = Array.Empty<T>();
                error = "Multiple 'Authoring' roots exist. Keep one authoring root or assign the scene arrays manually.";
                return false;
            }

            if (authoringRoots.Length == 1)
            {
                Transform container = authoringRoots[0].transform.Find(authoringChildName);
                components = container == null
                    ? Array.Empty<T>()
                    : container.GetComponentsInChildren<T>(true)
                        .Where(item => item != null && item.gameObject.scene == scene)
                        .Distinct()
                        .ToArray();
                return true;
            }

            // Level_01 predates the Authoring folder and keeps these components flat
            // across the scene. Preserve that authored layout until it is migrated.
            components = FindInScene<T>(scene);
            return true;
        }

        private static string HierarchyPath(Transform transform)
        {
            var segments = new Stack<string>();
            while (transform != null)
            {
                segments.Push(transform.name);
                transform = transform.parent;
            }
            return string.Join("/", segments);
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded) return Array.Empty<T>();
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Where(item => item != null && item.gameObject.scene == scene)
                .Distinct()
                .ToArray();
        }

        private static GameObject[] FindNamedObjects(Scene scene, string objectName)
        {
            if (!scene.IsValid() || !scene.isLoaded) return Array.Empty<GameObject>();
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform != null && transform.name == objectName && transform.gameObject.scene == scene)
                .Select(transform => transform.gameObject)
                .Distinct()
                .ToArray();
        }

        private static bool IsInScene(Component component, Scene scene)
        {
            return component != null && component.gameObject.scene == scene;
        }

        private static bool IsValidLevelId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !char.IsLetter(id[0])) return false;
            return id.All(character => char.IsLetterOrDigit(character) || character == '_' || character == '-');
        }

    }
}
