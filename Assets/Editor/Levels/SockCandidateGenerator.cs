using System;
using System.Collections.Generic;
using System.Linq;
using BulkoGame.Collectibles;
using BulkoGame.Defense;
using BulkoGame.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BulkoGame.Editor
{
    /// <summary>
    /// Bakes the deliberately authored Level_02 sock pool from named floor regions.
    /// The output is ordinary SockSpawnPoint scene data and is never used at runtime.
    /// </summary>
    public static class SockCandidateGenerator
    {
        private const string MenuPath = "Bulka: Sock Patrol/Levels/Generate Sock Candidates";
        private const string Level02Id = "Level_02";
        private const float MinimumPointSpacing = 1.25f;
        private const float AdditionalClearance = .12f;

        [MenuItem(MenuPath, false, 12)]
        private static void GenerateForActiveScene()
        {
            if (!TryGenerate(SceneManager.GetActiveScene(), out string message))
            {
                Debug.LogError("Generate Sock Candidates: " + message);
                EditorUtility.DisplayDialog("Generate Sock Candidates", message, "OK");
                return;
            }

            Debug.Log("Generate Sock Candidates: " + message);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateGenerateMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && SceneManager.GetActiveScene().IsValid()
                && SceneManager.GetActiveScene().isLoaded;
        }

        internal static bool TryGenerate(Scene scene, out string message)
        {
            message = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                message = "Stop Play Mode before generating authored sock points.";
                return false;
            }
            if (!scene.IsValid() || !scene.isLoaded)
            {
                message = "Open Level_02 before generating sock candidates.";
                return false;
            }

            LevelSceneBindings bindings = FindInScene<LevelSceneBindings>(scene).SingleOrDefault();
            if (bindings == null || bindings.definition == null || bindings.definition.levelId != Level02Id)
            {
                message = "This first baking pass is intentionally authored for Level_02. Open the Level_02 scene with its Level_02Definition assigned.";
                return false;
            }

            Transform authoring = FindRootByName(scene, "Authoring");
            Transform pointRoot = authoring != null ? authoring.Find("SockSpawnPoints") : null;
            if (pointRoot == null)
            {
                message = "Level_02 is missing Authoring/SockSpawnPoints.";
                return false;
            }
            if (pointRoot.GetComponentsInChildren<SockSpawnPoint>(true).Length > 0)
            {
                message = "SockSpawnPoints already contains authored points. This command does not replace manual work; adjust or remove them first if a new bake is intended.";
                return false;
            }

            if (!TryGetClearanceRadius(bindings, out float clearanceRadius, out message)) return false;
            Physics2D.SyncTransforms();
            var solids = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Collider2D>(true))
                .Where(collider => collider != null && collider.enabled && collider.gameObject.activeInHierarchy
                    && !collider.isTrigger && collider.GetComponentInParent<TrapPlacementZone>() == null)
                .ToArray();

            var regions = BuildRoomSpecs(scene);
            if (regions == null)
            {
                message = "Level_02 is missing one or more named authored floor regions needed by this command.";
                return false;
            }

            var accepted = new List<SpawnCandidate>();
            foreach (RoomSpec spec in regions)
            {
                foreach (CandidateProposal proposal in spec.GetCandidates())
                {
                    if (!HasRoomClearance(proposal.Position, proposal.FloorBounds, clearanceRadius, solids)) continue;
                    if (accepted.Any(existing => Vector2.Distance(existing.Position, proposal.Position) < MinimumPointSpacing)) continue;
                    accepted.Add(new SpawnCandidate(spec.Room, proposal.Position));
                    if (accepted.Count(candidate => candidate.Room == spec.Room) == spec.TargetCount) break;
                }

                int selected = accepted.Count(candidate => candidate.Room == spec.Room);
                if (selected != spec.TargetCount)
                {
                    message = $"{spec.Room} produced {selected} safe candidates of {spec.TargetCount} requested. Adjust the authored floor/furniture layout or the Level_02 candidate pattern before baking.";
                    return false;
                }
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generate Level_02 sock candidates");
            try
            {
                var generated = new List<SockSpawnPoint>(accepted.Count);
                foreach (var candidate in accepted)
                {
                    string roomLabel = candidate.Room.ToString();
                    var pointObject = new GameObject($"{roomLabel} Sock Spawn {generated.Count(x => x.Room == candidate.Room) + 1:00}");
                    Undo.RegisterCreatedObjectUndo(pointObject, "Create sock spawn point");
                    pointObject.transform.SetParent(pointRoot, false);
                    pointObject.transform.position = new Vector3(candidate.Position.x, candidate.Position.y, 0f);
                    var spawnPoint = Undo.AddComponent<SockSpawnPoint>(pointObject);
                    var serializedPoint = new SerializedObject(spawnPoint);
                    serializedPoint.FindProperty("room").enumValueIndex = (int)candidate.Room;
                    serializedPoint.ApplyModifiedPropertiesWithoutUndo();
                    generated.Add(spawnPoint);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                message = $"Baked {generated.Count} editable Level_02 SockSpawnPoints with {clearanceRadius:F2}u obstacle clearance: "
                    + string.Join(", ", regions.Select(spec => $"{spec.Room} {spec.TargetCount}")) + ". Run Configure Shared Setup to bind them.";
                return true;
            }
            catch (Exception exception)
            {
                message = "Could not bake sock candidates: " + exception.Message;
                return false;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static bool TryGetClearanceRadius(LevelSceneBindings bindings, out float radius, out string message)
        {
            radius = 0f;
            message = null;
            GameObject bulkaPrefab = bindings.definition.art != null ? bindings.definition.art.bulkaPrefab : null;
            if (bulkaPrefab == null)
            {
                message = "LevelDefinition has no Bulka prefab; clearance cannot be calculated.";
                return false;
            }

            Collider2D bulkaCollider = bulkaPrefab.GetComponentInChildren<Collider2D>(true);
            if (bulkaCollider == null)
            {
                message = "Bulka prefab has no Collider2D; clearance cannot be calculated.";
                return false;
            }

            Bounds bounds = bulkaCollider.bounds;
            radius = Mathf.Max(.2f, Mathf.Min(bounds.extents.x, bounds.extents.y)) + AdditionalClearance;
            return true;
        }

        private static bool HasRoomClearance(Vector2 point, Bounds floorBounds, float radius, Collider2D[] solids)
        {
            if (point.x - radius < floorBounds.min.x || point.x + radius > floorBounds.max.x
                || point.y - radius < floorBounds.min.y || point.y + radius > floorBounds.max.y)
                return false;

            foreach (Collider2D solid in solids)
            {
                if (solid.OverlapPoint(point)) return false;
                Vector2 closest = solid.ClosestPoint(point);
                if (closest != point && Vector2.Distance(closest, point) < radius) return false;
            }
            return true;
        }

        private static RoomSpec[] BuildRoomSpecs(Scene scene)
        {
            if (!TryGetFloorBounds(scene, "Bedroom oak", out Bounds bedroom)
                || !TryGetFloorBounds(scene, "Kitchen tile", out Bounds kitchen)
                || !TryGetFloorBounds(scene, "Central lounge oak", out Bounds lounge)
                || !TryGetFloorBounds(scene, "Entry reading nook oak", out Bounds entry)
                || !TryGetFloorBounds(scene, "Dining oak", out Bounds dining)
                || !TryGetFloorBounds(scene, "Bathroom tile", out Bounds bathroom)) return null;

            return new[]
            {
                new RoomSpec(SockSpawnRoom.Bedroom, 7, new[] { bedroom }, new[]
                {
                    new Vector2(.14f, .16f), new Vector2(.28f, .41f), new Vector2(.48f, .30f),
                    new Vector2(.72f, .14f), new Vector2(.67f, .45f), new Vector2(.18f, .79f),
                    new Vector2(.88f, .80f), new Vector2(.43f, .78f), new Vector2(.84f, .57f)
                }),
                new RoomSpec(SockSpawnRoom.Kitchen, 7, new[] { kitchen }, new[]
                {
                    new Vector2(.12f, .17f), new Vector2(.28f, .38f), new Vector2(.48f, .27f),
                    new Vector2(.72f, .20f), new Vector2(.86f, .49f), new Vector2(.62f, .62f),
                    new Vector2(.29f, .82f), new Vector2(.87f, .83f), new Vector2(.45f, .50f)
                }),
                new RoomSpec(SockSpawnRoom.LivingRoom, 8, new[] { lounge }, new[]
                {
                    new Vector2(.10f, .14f), new Vector2(.31f, .09f), new Vector2(.59f, .10f),
                    new Vector2(.89f, .20f), new Vector2(.11f, .42f), new Vector2(.88f, .47f),
                    new Vector2(.70f, .70f), new Vector2(.45f, .80f), new Vector2(.16f, .76f),
                    new Vector2(.50f, .38f), new Vector2(.76f, .34f)
                }),
                new RoomSpec(SockSpawnRoom.Hallway, 5, new[] { entry, dining }, new[]
                {
                    new Vector2(.38f, .25f), new Vector2(.53f, .73f), new Vector2(.14f, .24f),
                    new Vector2(.30f, .62f), new Vector2(.83f, .56f), new Vector2(.73f, .18f),
                    new Vector2(.93f, .83f), new Vector2(.55f, .87f)
                }),
                new RoomSpec(SockSpawnRoom.Bathroom, 5, new[] { bathroom }, new[]
                {
                    new Vector2(.14f, .18f), new Vector2(.37f, .34f), new Vector2(.76f, .17f),
                    new Vector2(.92f, .46f), new Vector2(.50f, .69f), new Vector2(.18f, .74f),
                    new Vector2(.79f, .81f), new Vector2(.34f, .56f)
                })
            };
        }

        private static bool TryGetFloorBounds(Scene scene, string floorName, out Bounds bounds)
        {
            bounds = default;
            var renderer = FindInScene<SpriteRenderer>(scene).SingleOrDefault(item => item.name == floorName);
            if (renderer == null) return false;
            bounds = renderer.bounds;
            return bounds.size.x > 0f && bounds.size.y > 0f;
        }

        private static Transform FindRootByName(Scene scene, string rootName)
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)
                    .Concat(new[] { root.transform }))
                .SingleOrDefault(transform => transform.name == rootName);
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        private readonly struct SpawnCandidate
        {
            public readonly SockSpawnRoom Room;
            public readonly Vector2 Position;
            public SpawnCandidate(SockSpawnRoom room, Vector2 position) { Room = room; Position = position; }
        }

        private sealed class RoomSpec
        {
            public readonly SockSpawnRoom Room;
            public readonly int TargetCount;
            public readonly Bounds[] Bounds;
            private readonly Vector2[] normalizedPattern;

            public RoomSpec(SockSpawnRoom room, int targetCount, Bounds[] bounds, Vector2[] normalizedPattern)
            {
                Room = room;
                TargetCount = targetCount;
                Bounds = bounds;
                this.normalizedPattern = normalizedPattern;
            }

            public IEnumerable<CandidateProposal> GetCandidates()
            {
                for (int i = 0; i < normalizedPattern.Length; i++)
                {
                    Bounds bounds = Bounds[Mathf.Min(i * Bounds.Length / normalizedPattern.Length, Bounds.Length - 1)];
                    Vector2 n = normalizedPattern[i];
                    yield return new CandidateProposal(new Vector2(Mathf.Lerp(bounds.min.x, bounds.max.x, n.x), Mathf.Lerp(bounds.min.y, bounds.max.y, n.y)), bounds);
                }
            }
        }

        private readonly struct CandidateProposal
        {
            public readonly Vector2 Position;
            public readonly Bounds FloorBounds;
            public CandidateProposal(Vector2 position, Bounds floorBounds) { Position = position; FloorBounds = floorBounds; }
        }
    }
}
