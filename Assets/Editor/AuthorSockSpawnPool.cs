using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BulkoGame.Collectibles;
using BulkoGame.Levels;

namespace BulkoGame.Editor
{
    public static class AuthorSockSpawnPool
    {
        private struct PointData
        {
            public readonly string room;
            public readonly Vector2 position;
            public PointData(string room, float x, float y) { this.room = room; position = new Vector2(x, y); }
        }

        private static readonly PointData[] Points = {
            new PointData("Bedroom",-10.7f,7.7f), new PointData("Bedroom",-8.0f,4.25f),
            new PointData("Bedroom",-14.3f,-2.5f), new PointData("Bedroom",-9.8f,-6.6f),
            new PointData("Bedroom",-9.2f,-4.6f),
            new PointData("Hallway",-5.6f,-6.5f), new PointData("Hallway",-3.5f,-3.7f),
            new PointData("Hallway",-1.0f,-7.0f), new PointData("Hallway",2.0f,-4.8f),
            new PointData("Hallway",5.9f,-6.0f),
            new PointData("LivingRoom",-5.4f,0.0f), new PointData("LivingRoom",-2.8f,7.0f),
            new PointData("LivingRoom",-.8f,6.6f), new PointData("LivingRoom",1.8f,6.4f),
            new PointData("LivingRoom",5.8f,4.7f), new PointData("LivingRoom",3.0f,-.5f),
            new PointData("Kitchen",8.4f,5.2f), new PointData("Kitchen",9.6f,5.7f),
            new PointData("Kitchen",13.8f,5.4f), new PointData("Kitchen",14.1f,3.7f),
            new PointData("Kitchen",8.0f,2.6f), new PointData("Kitchen",12.0f,2.6f),
            new PointData("Bathroom",10.5f,-1.7f), new PointData("Bathroom",11.4f,-2.8f),
            new PointData("Bathroom",10.1f,-4.5f), new PointData("Bathroom",11.4f,-7.5f),
            new PointData("Bathroom",9.7f,-7.3f), new PointData("Bathroom",11.6f,-.9f)
        };

        [MenuItem("BulkoGame/Author Sock Spawn Pool")]
        public static void Author()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Level_01.unity");
            var bindings = Object.FindAnyObjectByType<LevelSceneBindings>();
            if (bindings == null) throw new System.SystemException("The scene has no LevelSceneBindings.");
            if (bindings.sockSpawnPoints != null && bindings.sockSpawnPoints.Length > 0)
                throw new System.SystemException("The scene already has a sock spawn pool; no objects were changed.");

            var root = new GameObject("Sock Spawn Pool");
            root.transform.SetParent(bindings.transform, false);
            var points = new List<SockSpawnPoint>(Points.Length);
            var roomCounts = new Dictionary<string, int>();
            foreach (var data in Points)
            {
                roomCounts.TryGetValue(data.room, out int number);
                number++;
                roomCounts[data.room] = number;
                var go = new GameObject("Sock Spawn - " + data.room + " " + number.ToString("00"));
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(data.position.x, data.position.y, .16f);
                var marker = go.AddComponent<SockSpawnPoint>();
                var serialized = new SerializedObject(marker);
                serialized.FindProperty("room").enumValueIndex = (int)(SockSpawnRoom)System.Enum.Parse(typeof(SockSpawnRoom), data.room);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                points.Add(marker);
            }
            bindings.sockSpawnPoints = points.ToArray();
            EditorUtility.SetDirty(bindings);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Authored " + points.Count + " sock spawn points: Bedroom 5, Hallway 5, LivingRoom 6, Kitchen 6, Bathroom 6.");
        }
    }
}
