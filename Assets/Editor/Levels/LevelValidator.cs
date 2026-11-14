using System;
using System.Collections.Generic;
using System.Linq;
using BulkoGame.Collectibles;
using BulkoGame.GameFlow;
using BulkoGame.Levels;
using BulkoGame.Defense;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BulkoGame.Editor
{
    public static class LevelValidator
    {
        private const float SockClearanceRadius = .2f;

        [MenuItem("Bulka: Sock Patrol/Levels/Validate Current Level")]
        public static void ValidateCurrentLevel()
        {
            ValidateScene(SceneManager.GetActiveScene());
        }

        internal static ValidationResult ValidateScene(Scene scene)
        {
            var result = new ValidationResult();
            var bindings = scene.IsValid()
                ? UnityEngine.Object.FindObjectsByType<LevelSceneBindings>(FindObjectsInactive.Include)
                    .Where(x => x != null && x.gameObject.scene == scene).ToArray()
                : Array.Empty<LevelSceneBindings>();
            var bootstraps = scene.IsValid()
                ? UnityEngine.Object.FindObjectsByType<LevelBootstrap>(FindObjectsInactive.Include)
                    .Where(x => x != null && x.gameObject.scene == scene).ToArray()
                : Array.Empty<LevelBootstrap>();

            LevelDefinition definition = bindings.Length == 1 ? bindings[0].definition : null;
            string label = definition != null && !string.IsNullOrWhiteSpace(definition.levelId)
                ? definition.levelId : scene.IsValid() ? scene.name : "<no active scene>";
            Debug.Log($"LEVEL VALIDATION: {label}");

            if (!scene.IsValid())
                result.Error("No valid active scene is open.");
            if (bindings.Length != 1)
                result.Error($"Expected exactly one LevelSceneBindings in the current scene; found {bindings.Length}.",
                    bindings.FirstOrDefault());

            if (bindings.Length == 0)
            {
                result.Error("LevelSceneBindings is missing; no LevelDefinition or scene references can be validated.");
            }
            else
            {
                if (bindings.Length > 1)
                    foreach (var duplicate in bindings) result.Error("Extra LevelSceneBindings component found.", duplicate);
                foreach (var binding in bindings) ValidateBindings(scene, binding, result);
            }

            if (bootstraps.Length != 1)
                result.Error($"Expected exactly one LevelBootstrap in the current level; found {bootstraps.Length}.", bootstraps.FirstOrDefault());
            foreach (var bootstrap in bootstraps)
            {
                if (bindings.Length == 1 && bootstrap.SceneBindings != bindings[0])
                    result.Error("LevelBootstrap.sceneBindings must reference the scene's LevelSceneBindings.", bootstrap);
                if (definition != null && bootstrap.Definition != definition)
                    result.Error("LevelBootstrap.definition must reference the LevelDefinition assigned to LevelSceneBindings.", bootstrap);
            }

            ValidateAudioListeners(scene, result);
            Debug.Log($"Errors: {result.Errors}\nWarnings: {result.Warnings}");
            if (result.Errors == 0) Debug.Log("VALIDATION PASSED");
            else Debug.LogError("VALIDATION FAILED");
            return result;
        }

        private static void ValidateBindings(Scene scene, LevelSceneBindings bindings, ValidationResult result)
        {
            LevelDefinition definition = bindings.definition;
            if (definition == null)
            {
                result.Error("LevelDefinition reference is missing.", bindings);
            }
            else
            {
                ValidateDefinition(definition, result);
            }

            ValidateSceneTransform(bindings.playerSpawn, "Player spawn reference", scene, bindings, result);
            ValidateSceneTransform(bindings.basketRoot, "Basket root/reference", scene, bindings, result);
            if (bindings.gameplayCamera == null)
                result.Error("Gameplay camera reference is missing.", bindings);
            else if (bindings.gameplayCamera.gameObject.scene != scene)
                result.Error("Gameplay camera must reference an object in the current scene.", bindings.gameplayCamera);
            else if (!bindings.gameplayCamera.isActiveAndEnabled)
                result.Error("Gameplay camera must be active and enabled.", bindings.gameplayCamera);

            var routes = ValidateArray(bindings.routes, "CatRoutes", bindings, result);
            var traps = ValidateArray(bindings.trapZones, "TrapPlacementZones", bindings, result);
            var sockPoints = ValidateArray(bindings.sockSpawnPoints, "SockSpawnPoints", bindings, result);

            ValidateRoutes(routes, definition, result);
            ValidateTrapZones(traps, scene, result);
            ValidateSockPoints(sockPoints, scene, definition, result);
            ValidateShortcutApproaches(scene, result);
        }

        private static void ValidateDefinition(LevelDefinition definition, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(definition.levelId)) result.Error("LevelDefinition levelId is empty.", definition);
            if (string.IsNullOrWhiteSpace(definition.displayName)) result.Error("LevelDefinition displayName is empty.", definition);
            if (definition.balance == null) result.Error("GameBalanceConfig reference is missing.", definition);
            if (definition.audio == null) result.Error("AudioConfig reference is missing.", definition);
            if (definition.art == null) result.Error("ApartmentArtSet reference is missing.", definition);
            else
            {
                if (definition.art.bulkaPrefab == null) result.Error("ApartmentArtSet bulkaPrefab reference is missing.", definition.art);
                if (definition.art.sockPrefab == null) result.Error("ApartmentArtSet sockPrefab reference is missing.", definition.art);
                if (definition.art.basketPrefab == null) result.Error("ApartmentArtSet basketPrefab reference is missing.", definition.art);
                if (definition.art.orangeCatPrefab == null) result.Error("ApartmentArtSet orangeCatPrefab reference is missing.", definition.art);
                if (definition.art.slowTrapPrefab == null) result.Error("ApartmentArtSet slowTrapPrefab reference is missing.", definition.art);
                if (definition.art.sockPink == null) result.Error("ApartmentArtSet base sock sprite is missing.", definition.art);
                if (definition.art.hudCard == null || definition.art.finishCard == null)
                    result.Error("ApartmentArtSet hudCard and finishCard are required.", definition.art);
                if (definition.art.trapZoneMarker == null) result.Error("ApartmentArtSet trapZoneMarker is missing.", definition.art);
            }
            if (definition.environmentArt == null) result.Error("EnvironmentArtSet reference is missing.", definition);
            else
            {
                if (definition.environmentArt.sockCreamStripe == null || definition.environmentArt.sockGreen == null
                    || definition.environmentArt.sockLeaf == null || definition.environmentArt.sockOrangeDots == null
                    || definition.environmentArt.sockGreenCheck == null || definition.environmentArt.sockHeart == null)
                    result.Error("EnvironmentArtSet requires all six sock visual variants.", definition.environmentArt);
                if (definition.environmentArt.slowTrapArmed == null || definition.environmentArt.slowTrapTriggered == null
                    || definition.environmentArt.slowTrapUsed == null)
                    result.Error("EnvironmentArtSet requires all three Slow Trap sprites.", definition.environmentArt);
            }

            if (definition.sockCount <= 0) result.Error("sockCount must be greater than zero.", definition);
            if (definition.minimumSocksPerZone < 0) result.Error("minimumSocksPerZone cannot be negative.", definition);
            if (definition.maximumSocksPerZone < definition.minimumSocksPerZone)
                result.Error("maximumSocksPerZone must be greater than or equal to minimumSocksPerZone.", definition);
            if (definition.catRouteSequence == null || definition.catRouteSequence.Length == 0)
                result.Error("catRouteSequence must contain at least one route ID.", definition);
            if (!string.IsNullOrWhiteSpace(definition.nextLevelSceneName))
            {
                var matchingScenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled
                        && string.Equals(System.IO.Path.GetFileNameWithoutExtension(scene.path),
                            definition.nextLevelSceneName.Trim(), StringComparison.Ordinal)
                        && AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) != null)
                    .ToArray();
                if (matchingScenes.Length != 1)
                    result.Error($"nextLevelSceneName '{definition.nextLevelSceneName}' must match exactly one enabled scene in Build Settings; found {matchingScenes.Length}.", definition);
            }
        }

        private static void ValidateSceneTransform(Transform value, string field, Scene scene,
            LevelSceneBindings context, ValidationResult result)
        {
            if (value == null) result.Error(field + " is missing.", context);
            else if (value.gameObject.scene != scene) result.Error(field + " must reference an object in the current scene.", value);
        }

        private static T[] ValidateArray<T>(T[] values, string label, LevelSceneBindings context,
            ValidationResult result) where T : Component
        {
            if (values == null)
            {
                result.Error(label + " array is null.", context);
                return Array.Empty<T>();
            }

            var seen = new HashSet<T>();
            var valid = new List<T>();
            for (int i = 0; i < values.Length; i++)
            {
                T value = values[i];
                if (value == null)
                {
                    result.Error($"{label}[{i}] is null.", context);
                    continue;
                }
                if (!seen.Add(value))
                {
                    result.Error($"{label} contains a duplicate reference to {value.name}.", value);
                    continue;
                }
                valid.Add(value);
            }
            return valid.ToArray();
        }

        private static void ValidateRoutes(CatRoute[] routes, LevelDefinition definition, ValidationResult result)
        {
            if (routes.Length == 0) result.Error("At least one CatRoute is required.");
            var routeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CatRoute route in routes)
            {
                string routeId = route.RouteId == null ? null : route.RouteId.Trim();
                if (string.IsNullOrWhiteSpace(routeId))
                    result.Error("CatRoute has no routeId. Assign a non-empty route ID in the Inspector.", route);
                else if (!routeIds.Add(routeId))
                    result.Error($"CatRoute route ID '{routeId}' is duplicated.", route);

                var serializedRoute = new SerializedObject(route);
                SerializedProperty waypoints = serializedRoute.FindProperty("waypoints");
                if (waypoints == null || !waypoints.isArray)
                {
                    result.Error("CatRoute waypoint data is unavailable.", route);
                    continue;
                }
                if (waypoints.arraySize < 2)
                    result.Error("CatRoute requires at least two waypoints (entrance and basket approach).", route);

                var points = new Transform[waypoints.arraySize];
                for (int i = 0; i < waypoints.arraySize; i++)
                {
                    var waypoint = waypoints.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    points[i] = waypoint;
                    if (waypoint == null)
                        result.Error($"CatRoute waypoint {i} is missing.", route);
                    else if (waypoint.gameObject.scene != route.gameObject.scene)
                        result.Error($"CatRoute waypoint {i} is not in the route's scene.", waypoint);
                }
                var routeColliders = GetSolidGeometryColliders(route.gameObject.scene, ignoreShortcutBlockers: true);
                for (int i = 0; i + 1 < points.Length; i++)
                {
                    if (points[i] == null || points[i + 1] == null) continue;
                    var hit = Physics2D.LinecastAll(points[i].position, points[i + 1].position)
                        .Select(h => h.collider)
                        .FirstOrDefault(c => c != null && routeColliders.Contains(c));
                    if (hit != null)
                        result.Error($"CatRoute '{routeId ?? "<unnamed>"}' segment {i} ('{points[i].name}' to '{points[i + 1].name}') is blocked by '{hit.name}'.", route);
                }
                if (waypoints.arraySize > 0 && waypoints.GetArrayElementAtIndex(0).objectReferenceValue == null)
                    result.Error("CatRoute entrance/exit endpoint is missing.", route);
                if (waypoints.arraySize > 1 && waypoints.GetArrayElementAtIndex(waypoints.arraySize - 1).objectReferenceValue == null)
                    result.Error("CatRoute basket-side endpoint is missing.", route);
            }

            if (definition == null || definition.catRouteSequence == null || definition.catRouteSequence.Length == 0) return;
            foreach (string routeId in definition.catRouteSequence)
            {
                if (string.IsNullOrWhiteSpace(routeId))
                    result.Error("catRouteSequence contains an empty route ID.", definition);
                else if (!routeIds.Contains(routeId.Trim()))
                    result.Error($"catRouteSequence references unknown route ID '{routeId}'.", definition);
            }

            if (definition.balance == null) return;
            if (definition.catRouteSequence.Length < definition.balance.waveSize)
                result.Error($"catRouteSequence has {definition.catRouteSequence.Length} entries but waveSize is {definition.balance.waveSize}; the sequence cannot serve the full wave.", definition);
            else if (definition.catRouteSequence.Length > definition.balance.waveSize)
                result.Warning($"catRouteSequence has {definition.catRouteSequence.Length - definition.balance.waveSize} unused entries beyond waveSize {definition.balance.waveSize}.", definition);
        }

        private static void ValidateTrapZones(TrapPlacementZone[] zones, Scene scene, ValidationResult result)
        {
            if (zones.Length == 0) result.Error("At least one TrapPlacementZone is required.");
            foreach (TrapPlacementZone zone in zones)
            {
                if (zone.gameObject.scene != scene)
                    result.Error("TrapPlacementZone must belong to the current scene.", zone);
                if (!zone.enabled || !zone.gameObject.activeInHierarchy)
                    result.Error("TrapPlacementZone component and GameObject must be active.", zone);
                foreach (var collider in zone.GetComponentsInChildren<Collider2D>(true))
                    if (collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger)
                        result.Error("TrapPlacementZone has a solid collider; placement zones should not block movement.", collider);

                var blocker = FindGeometryOverlap(zone.transform.position, .21f,
                    GetSolidGeometryColliders(scene, ignoreShortcutBlockers: true), zone.transform);
                if (blocker != null)
                    result.Error($"TrapPlacementZone '{zone.name}' is inside solid geometry '{blocker.name}'.", zone);
            }
        }

        private static void ValidateSockPoints(SockSpawnPoint[] points, Scene scene,
            LevelDefinition definition, ValidationResult result)
        {
            if (points.Length == 0) result.Error("At least one SockSpawnPoint is required.");
            var roomCounts = Enum.GetValues(typeof(SockSpawnRoom)).Cast<SockSpawnRoom>()
                .ToDictionary(room => room, _ => 0);
            var seenTransforms = new HashSet<Transform>();
            var validPoints = new List<SockSpawnPoint>();

            foreach (SockSpawnPoint point in points)
            {
                if (point.gameObject.scene != scene)
                    result.Error("SockSpawnPoint must belong to the current scene.", point);
                if (point.transform == null || !seenTransforms.Add(point.transform))
                {
                    result.Error("SockSpawnPoint transform is missing or duplicated.", point);
                    continue;
                }
                if (!Enum.IsDefined(typeof(SockSpawnRoom), point.Room))
                {
                    result.Error("SockSpawnPoint has an invalid room/zone assignment.", point);
                    continue;
                }
                roomCounts[point.Room]++;
                validPoints.Add(point);
            }

            if (definition != null && definition.sockCount > 0)
            {
                if (validPoints.Count < definition.sockCount)
                    result.Error($"Only {validPoints.Count} unique usable sock points exist for {definition.sockCount} requested socks.", definition);

                var zones = roomCounts.Where(pair => pair.Value > 0 || definition.minimumSocksPerZone > 0).ToArray();
                int zoneCount = zones.Length;
                int minimumTotal = zoneCount * definition.minimumSocksPerZone;
                int maximumCapacity = zones.Sum(pair => Math.Min(pair.Value, definition.maximumSocksPerZone));
                if (minimumTotal > definition.sockCount)
                    result.Error($"Sock distribution impossible: {zoneCount} zones × minimum {definition.minimumSocksPerZone} = {minimumTotal}, greater than {definition.sockCount} socks.", definition);
                if (maximumCapacity < definition.sockCount)
                    result.Error($"Sock distribution impossible: {definition.sockCount} socks requested, max capacity across {zoneCount} current zones is {maximumCapacity}.", definition);
                foreach (var zone in zones)
                    if (zone.Value < definition.minimumSocksPerZone)
                        result.Error($"Sock zone {zone.Key} contains {zone.Value} points; minimum required is {definition.minimumSocksPerZone}.", definition);
            }

            var colliders = GetSolidGeometryColliders(scene, ignoreShortcutBlockers: true);

            if (colliders.Length == 0)
            {
                result.Warning("No authored solid environment colliders are available for sock clearance; runtime-generated colliders cannot be checked in the saved scene.");
                return;
            }

            foreach (SockSpawnPoint point in validPoints)
            {
                var blocker = FindGeometryOverlap(point.transform.position, SockClearanceRadius, colliders, point.transform);
                if (blocker != null)
                    result.Error($"SockSpawnPoint '{point.name}' lacks {SockClearanceRadius:0.0} clearance from solid collider '{blocker.name}'.", point);
            }
        }

        private static Collider2D[] GetSolidGeometryColliders(Scene scene, bool ignoreShortcutBlockers)
        {
            if (!scene.IsValid()) return Array.Empty<Collider2D>();
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Collider2D>(true))
                .Where(c => c != null && c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger
                    && (c.attachedRigidbody == null || c.attachedRigidbody.bodyType == RigidbodyType2D.Static)
                    && c.GetComponentInParent<TrapPlacementZone>() == null
                    && (!ignoreShortcutBlockers || c.GetComponentInParent<CatShortcutBlocker>() == null))
                .Distinct().ToArray();
        }

        private static Collider2D FindGeometryOverlap(Vector2 center, float radius,
            Collider2D[] colliders, Transform ignoredHierarchy)
        {
            foreach (var collider in colliders)
            {
                if (ignoredHierarchy != null && collider.transform.IsChildOf(ignoredHierarchy)) continue;
                Vector2 closest = collider.ClosestPoint(center);
                if ((closest - center).sqrMagnitude <= radius * radius) return collider;
            }
            return null;
        }

        private static void ValidateShortcutApproaches(Scene scene, ValidationResult result)
        {
            var colliders = GetSolidGeometryColliders(scene, ignoreShortcutBlockers: true);
            var shortcuts = scene.IsValid()
                ? scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CatShortcutBlocker>(true))
                    .Where(s => s != null && s.enabled && s.gameObject.activeInHierarchy).ToArray()
                : Array.Empty<CatShortcutBlocker>();
            foreach (var shortcut in shortcuts)
            {
                var box = shortcut.GetComponent<BoxCollider2D>();
                if (box == null) continue;
                Vector2 center = box.transform.TransformPoint(box.offset);
                Vector2 worldX = box.transform.TransformVector(Vector3.right * box.size.x);
                Vector2 worldY = box.transform.TransformVector(Vector3.up * box.size.y);
                bool travelAlongX = worldX.magnitude < worldY.magnitude;
                Vector2 axis = travelAlongX ? worldX.normalized : worldY.normalized;
                float halfDepth = (travelAlongX ? worldX.magnitude : worldY.magnitude) * .5f;
                float approachDistance = halfDepth + .3f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 approach = center + axis * approachDistance * side;
                    var blocker = FindGeometryOverlap(approach, .2f, colliders, shortcut.transform);
                    if (blocker != null)
                        result.Error($"CatShortcut '{shortcut.name}' side {(side < 0 ? "A" : "B")} has no clear approach; blocked by '{blocker.name}'.", shortcut);
                }
                var crossing = Physics2D.LinecastAll(center - axis * approachDistance, center + axis * approachDistance)
                    .Select(h => h.collider).FirstOrDefault(c => c != null && colliders.Contains(c));
                if (crossing != null)
                    result.Error($"CatShortcut '{shortcut.name}' opening is obstructed by '{crossing.name}'.", shortcut);
            }
        }

        private static void ValidateAudioListeners(Scene scene, ValidationResult result)
        {
            int count = scene.IsValid()
                ? scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AudioListener>(true))
                    .Count(listener => listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
                : 0;
            if (count == 0) result.Error("Current gameplay scene has no active AudioListener; one active listener is expected.");
            else if (count > 1) result.Error($"Current gameplay scene has {count} active AudioListeners; exactly one is expected.");
        }

        internal sealed class ValidationResult
        {
            public int Errors { get; private set; }
            public int Warnings { get; private set; }

            public void Error(string message, UnityEngine.Object context = null)
            {
                Errors++;
                Debug.LogError("ERROR: " + message, context);
            }

            public void Warning(string message, UnityEngine.Object context = null)
            {
                Warnings++;
                Debug.LogWarning("WARNING: " + message, context);
            }
        }
    }
}
