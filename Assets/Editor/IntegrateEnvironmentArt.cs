using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BulkoGame.Art;
using BulkoGame.GameFlow;

namespace BulkoGame.Editor
{
    public static class IntegrateEnvironmentArt
    {
        private const string Root = "Assets/Art/PixelArt/Environment/";
        private const string AssetPath = "Assets/Art/EnvironmentArtSet.asset";
        private const string ScenePath = "Assets/Scenes/Level_01.unity";

        [MenuItem("Bulka: Sock Patrol/Import Environment Art")]
        public static void Apply()
        {
            string[] bindings = {
                "bed|Bedroom/Bed", "wardrobe|Bedroom/Wardrobe", "bedsideTableLamp|Bedroom/BedsideTableLamp",
                "lowBookShelf|Bedroom/LowBookShelf", "bedroomRug|Bedroom/BedroomRug",
                "sofa|LivingRoom/Sofa", "coffeeTable|LivingRoom/CoffeeTable", "tvConsole|LivingRoom/TVConsole",
                "sideboard|LivingRoom/Sideboard", "armchair|LivingRoom/Armchair", "livingRoomRug|LivingRoom/LivingRoomRug",
                "refrigerator|Kitchen/Refrigerator", "stoveOven|Kitchen/StoveOven", "sinkCabinet|Kitchen/SinkCabinet",
                "counterModule|Kitchen/CounterModule", "diningTableChairs|Kitchen/DiningTableChairs",
                "kitchenRug|Kitchen/KitchenRug", "coffeeCounter|Kitchen/CoffeeCounter",
                "bathtub|Bathroom/Bathtub", "bathroomSinkCabinet|Bathroom/SinkCabinet", "toilet|Bathroom/Toilet",
                "narrowCabinet|Bathroom/NarrowCabinet", "shower|Bathroom/Shower", "bathroomRug|Bathroom/BathroomRug",
                "laundryBasket|Bathroom/LaundryBasket", "accessoryShelf|Bathroom/AccessoryShelf",
                "monsteraPlant|Decor/MonsteraPlant", "trailingPlant|Decor/TrailingPlant", "tableLamp|Decor/TableLamp",
                "books|Decor/Books", "framedLandscape|Decor/FramedLandscape", "greenSlippers|Decor/GreenSlippers",
                "sleepingCat|Decor/SleepingCat",
                "sockBasketLow|Gameplay/SockBasketLow", "sockBasketMedium|Gameplay/SockBasketMedium",
                "sockBasketFull|Gameplay/SockBasketFull", "sockCreamStripe|Gameplay/SockCreamStripe",
                "sockGreen|Gameplay/SockGreen", "sockLeaf|Gameplay/SockLeaf",
                "sockOrangeDots|Gameplay/SockOrangeDots", "sockGreenCheck|Gameplay/SockGreenCheck",
                "sockHeart|Gameplay/SockHeart", "slowTrapArmed|Gameplay/SlowTrapArmed",
                "slowTrapTriggered|Gameplay/SlowTrapTriggered", "slowTrapUsed|Gameplay/SlowTrapUsed"
            };

            AssetDatabase.Refresh();
            foreach (string binding in bindings)
            {
                string path = Root + binding.Split('|')[1] + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new Exception("Missing sprite: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            var set = AssetDatabase.LoadAssetAtPath<EnvironmentArtSet>(AssetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<EnvironmentArtSet>();
                AssetDatabase.CreateAsset(set, AssetPath);
            }
            var serialized = new SerializedObject(set);
            foreach (string binding in bindings)
            {
                string[] parts = binding.Split('|');
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + parts[1] + ".png");
                if (sprite == null) throw new Exception("Sprite import failed: " + parts[1]);
                var property = serialized.FindProperty(parts[0]);
                if (property == null) throw new Exception("Missing art field: " + parts[0]);
                property.objectReferenceValue = sprite;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(ScenePath);
            var sceneBindings = UnityEngine.Object.FindAnyObjectByType<BulkoGame.Levels.LevelSceneBindings>();
            if (sceneBindings == null || sceneBindings.definition == null) throw new Exception("Level_01 has no LevelSceneBindings/LevelDefinition.");
            sceneBindings.definition.environmentArt = set;
            EditorUtility.SetDirty(sceneBindings.definition);
            AssetDatabase.SaveAssets();
            Debug.Log("Environment art imported and assigned through Level_01Definition.");
        }
    }
}
