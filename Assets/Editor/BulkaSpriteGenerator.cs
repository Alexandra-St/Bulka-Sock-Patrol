using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using BulkoGame.Art;

public static class BulkaSpriteGenerator
{
    private const string SpriteRoot = "Assets/Art/PixelArt/Bulka";
    private const string AnimationRoot = "Assets/Art/Animations";
    private const int Width = 96;
    private const int Height = 64;
    private const int Ppu = 16;
    private const int Fps = 8;
    private static readonly Color32 Outline = C(37, 34, 41);
    private static readonly Color32 Coat = C(53, 51, 60);
    private static readonly Color32 CoatLight = C(73, 71, 81);
    private static readonly Color32 CoatShade = C(43, 42, 50);
    private static readonly Color32 Tan = C(165, 98, 62);
    private static readonly Color32 TanLight = C(198, 132, 83);
    private static readonly Color32 Muzzle = C(174, 174, 168);
    private static readonly Color32 MuzzleLight = C(208, 207, 198);

    private enum Facing { Up, Down, Left, Right }

    public static void Generate()
    {
        if (!EditorUtility.DisplayDialog("Generate Bulka sprites",
                "This generator will overwrite generated Bulka PNGs and recreate animation clips and the controller. This operation cannot be automatically undone through Unity Undo.",
                "Regenerate", "Cancel")) return;

        Directory.CreateDirectory(SpriteRoot);
        Directory.CreateDirectory(AnimationRoot);
        var frameNames = new List<string>();
        foreach (Facing facing in Enum.GetValues(typeof(Facing)))
        {
            string direction = facing.ToString();
            SaveFrame("Bulka_Idle_" + direction, DrawFrame(facing, 0, true));
            frameNames.Add("Bulka_Idle_" + direction);
            for (int i = 0; i < 4; i++)
            {
                string frame = "Bulka_Walk_" + direction + "_" + (i + 1).ToString("00");
                SaveFrame(frame, DrawFrame(facing, i, false));
                frameNames.Add(frame);
            }
        }
        AssetDatabase.Refresh();
        var sprites = new Dictionary<string, Sprite>();
        foreach (var name in frameNames)
        {
            string path = SpriteRoot + "/" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePivot = new Vector2(.5f, .5f);
            importer.SaveAndReimport();
            sprites[name] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        foreach (Facing facing in Enum.GetValues(typeof(Facing)))
        {
            var idle = new[] { sprites["Bulka_Idle_" + facing] };
            CreateClip("Bulka_Idle_" + facing, idle, false);
            var walk = new Sprite[4];
            for (int i = 0; i < walk.Length; i++) walk[i] = sprites["Bulka_Walk_" + facing + "_" + (i + 1).ToString("00")];
            CreateClip("Bulka_Walk_" + facing, walk, true);
        }
        var controller = CreateController();
        UpdateArtSet(sprites["Bulka_Idle_Down"], controller);
        AssetDatabase.DeleteAsset("Assets/Art/PixelArt/Bulka.png");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Bulka: Sock Patrol: generated 20 Bulka frames, eight directional animation clips and Bulka.controller.");
    }

    private static Texture2D DrawFrame(Facing facing, int step, bool idle)
    {
        var p = new Raster(Width, Height);
        if (facing == Facing.Right) DrawSide(p, step, idle);
        else if (facing == Facing.Left) { DrawSide(p, step, idle); p.FlipX(); }
        else DrawTopDown(p, facing == Facing.Down, step, idle);
        p.Apply();
        return p.Texture;
    }

    private static void DrawSide(Raster p, int step, bool idle)
    {
        int phase = idle ? 0 : new[] { -2, 1, 2, -1 }[step];
        // Short upward tail and the long, low body silhouette.
        p.Rect(10, 31, 15, 5, Outline); p.Rect(12, 34, 12, 4, Coat);
        p.Rect(18, 23, 57, 22, Outline); p.Rect(20, 25, 53, 18, Coat);
        p.Rect(27, 26, 36, 3, CoatLight); p.Rect(24, 39, 37, 3, CoatShade);
        // Small tan saddle marks and chest patch.
        p.Rect(35, 26, 8, 4, Tan); p.Rect(36, 26, 6, 2, TanLight);
        p.Rect(55, 26, 7, 4, Tan); p.Rect(56, 26, 5, 2, TanLight);
        p.Rect(65, 31, 7, 10, Tan); p.Rect(66, 33, 5, 7, TanLight);
        // Four very short paws articulate without moving the body or pivot.
        int a = 19 + phase, b = 30 - phase, c = 57 - phase, d = 68 + phase;
        Leg(p, a, 21, false); Leg(p, b, 19, true); Leg(p, c, 20, false); Leg(p, d, 21, true);
        // Head, long muzzle, soft floppy ear, eyes and nose. Faces right.
        p.Rect(63, 27, 18, 19, Outline); p.Rect(66, 29, 14, 15, Coat);
        p.Rect(72, 30, 7, 6, Tan); p.Rect(73, 31, 5, 4, TanLight);
        p.Rect(70, 37, 16, 9, Outline); p.Rect(74, 38, 13, 6, Muzzle);
        p.Rect(79, 39, 8, 4, MuzzleLight); p.Rect(87, 39, 4, 4, Outline);
        p.Pixel(77, 42, C(45, 40, 43));
        p.Rect(68, 38, 8, 12, Outline); p.Rect(70, 39, 5, 9, CoatShade);
        p.Pixel(76, 35, C(247, 224, 187)); p.Pixel(77, 35, Outline);
        p.Pixel(71, 34, TanLight); p.Pixel(79, 34, TanLight);
    }

    private static void DrawTopDown(Raster p, bool facesDown, int step, bool idle)
    {
        int phase = idle ? 0 : new[] { -2, 1, 2, -1 }[step];
        // Long back viewed from above; same silhouette length as the side pose.
        p.Rect(34, 8, 29, 48, Outline); p.Rect(37, 10, 23, 43, Coat);
        p.Rect(40, 13, 17, 37, CoatLight); p.Rect(43, 15, 11, 31, Coat);
        // Four short legs stay tied to the same body anchor in every frame.
        int yLowL = 13 + phase, yLowR = 15 - phase, yHighL = 43 - phase, yHighR = 41 + phase;
        if (facesDown)
        {
            Paw(p, 30, yLowL); Paw(p, 57, yLowR); Paw(p, 30, yHighL); Paw(p, 57, yHighR);
            // Neck blaze, tan eyebrows/cheeks and a pale expressive muzzle at the front.
            p.Rect(39, 16, 19, 14, Tan); p.Rect(42, 17, 13, 11, TanLight);
            p.Rect(35, 8, 27, 18, Outline); p.Rect(38, 10, 21, 14, Coat);
            p.Rect(35, 7, 9, 13, Outline); p.Rect(52, 7, 9, 13, Outline);
            p.Rect(37, 9, 5, 9, CoatShade); p.Rect(54, 9, 5, 9, CoatShade);
            p.Rect(37, 11, 4, 3, TanLight); p.Rect(53, 11, 4, 3, TanLight);
            p.Pixel(39, 12, C(250, 226, 190)); p.Pixel(40, 12, Outline);
            p.Pixel(54, 12, C(250, 226, 190)); p.Pixel(55, 12, Outline);
            p.Rect(40, 3, 16, 8, Muzzle); p.Rect(42, 4, 12, 4, MuzzleLight);
            p.Rect(46, 2, 5, 3, Outline);
        }
        else
        {
            Paw(p, 30, yLowL); Paw(p, 57, yLowR); Paw(p, 30, yHighL); Paw(p, 57, yHighR);
            // Back of the head, dark drooping ears, tan neck markings, tiny tail.
            p.Rect(38, 40, 21, 16, Outline); p.Rect(41, 42, 15, 11, Coat);
            p.Rect(34, 40, 9, 15, Outline); p.Rect(53, 40, 9, 15, Outline);
            p.Rect(37, 42, 5, 10, CoatShade); p.Rect(54, 42, 5, 10, CoatShade);
            p.Rect(42, 38, 13, 5, Tan); p.Rect(44, 38, 9, 3, TanLight);
            p.Rect(45, 6, 7, 8, Outline); p.Rect(46, 7, 4, 5, CoatShade);
        }
        // Small shoulders and tan points keep the black-and-tan pattern readable overhead.
        p.Rect(37, 31, 5, 8, Tan); p.Rect(55, 31, 5, 8, Tan);
        p.Rect(38, 32, 3, 5, TanLight); p.Rect(56, 32, 3, 5, TanLight);
    }

    private static void Leg(Raster p, int x, int y, bool tanPaw)
    {
        p.Rect(x, 15, 8, 10, Outline); p.Rect(x + 2, 16, 5, 8, Coat);
        p.Rect(x + 1, y - 4, 8, 8, Outline); p.Rect(x + 3, y - 3, 5, 6, tanPaw ? TanLight : CoatShade);
        p.Rect(x + 2, y - 1, 6, 2, tanPaw ? Tan : Coat);
    }

    private static void Paw(Raster p, int x, int y)
    {
        p.Rect(x, y, 9, 10, Outline); p.Rect(x + 2, y + 2, 5, 6, Tan); p.Rect(x + 3, y + 3, 3, 3, TanLight);
    }

    private static void SaveFrame(string name, Texture2D texture)
    {
        File.WriteAllBytes(SpriteRoot + "/" + name + ".png", texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
    }

    private static void CreateClip(string name, Sprite[] frames, bool walking)
    {
        string path = AnimationRoot + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { name = name, frameRate = Fps };
        var keys = new ObjectReferenceKeyframe[walking ? 5 : 1];
        if (walking)
        {
            for (int i = 0; i < 4; i++) keys[i] = new ObjectReferenceKeyframe { time = i / (float)Fps, value = frames[i] };
            keys[4] = new ObjectReferenceKeyframe { time = 4f / Fps, value = frames[0] };
        }
        else keys[0] = new ObjectReferenceKeyframe { time = 0, value = frames[0] };
        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.stopTime = walking ? 4f / Fps : 1f / Fps;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
    }

    private static AnimatorController CreateController()
    {
        string path = AnimationRoot + "/Bulka.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("MoveDirection", AnimatorControllerParameterType.Int);
        var sm = controller.layers[0].stateMachine;
        var states = new AnimatorState[8];
        var directions = new[] { "Down", "Up", "Left", "Right" };
        for (int d = 0; d < directions.Length; d++)
        {
            string dir = directions[d];
            states[d * 2] = AddState(sm, "Bulka_Idle_" + dir, dir, false);
            states[d * 2 + 1] = AddState(sm, "Bulka_Walk_" + dir, dir, true);
            AddTransition(sm, states[d * 2], d, false);
            AddTransition(sm, states[d * 2 + 1], d, true);
        }
        sm.defaultState = states[0];
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string name, string clipDirection, bool walking)
    {
        var state = sm.AddState(name);
        state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationRoot + "/Bulka_" + (walking ? "Walk_" : "Idle_") + clipDirection + ".anim");
        state.writeDefaultValues = true;
        return state;
    }

    private static void AddTransition(AnimatorStateMachine sm, AnimatorState target, int direction, bool walking)
    {
        var transition = sm.AddAnyStateTransition(target);
        transition.hasExitTime = false;
        transition.duration = 0;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
        transition.AddCondition(walking ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsMoving");
        transition.AddCondition(AnimatorConditionMode.Equals, direction, "MoveDirection");
    }

    private static void UpdateArtSet(Sprite idleDown, RuntimeAnimatorController controller)
    {
        var set = AssetDatabase.LoadAssetAtPath<ApartmentArtSet>("Assets/Art/ApartmentArtSet.asset");
        var so = new SerializedObject(set);
        so.FindProperty("bulka").objectReferenceValue = idleDown;
        so.FindProperty("bulkaAnimator").objectReferenceValue = controller;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(set);
    }

    private static Color32 C(byte r, byte g, byte b, byte a = 255) => new Color32(r, g, b, a);

    private sealed class Raster
    {
        public readonly Texture2D Texture;
        private readonly Color32[] pixels;
        private readonly int width, height;
        public Raster(int w, int h)
        {
            width = w; height = h;
            Texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            pixels = new Color32[w * h];
            Array.Fill(pixels, C(0, 0, 0, 0));
        }
        public void Rect(int x, int y, int w, int h, Color32 color)
        {
            for (int yy = Math.Max(0, y); yy < Math.Min(height, y + h); yy++)
                for (int xx = Math.Max(0, x); xx < Math.Min(width, x + w); xx++) pixels[yy * width + xx] = color;
        }
        public void Pixel(int x, int y, Color32 color) => Rect(x, y, 1, 1, color);
        public void FlipX()
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width / 2; x++)
                {
                    int a = y * width + x, b = y * width + width - 1 - x;
                    (pixels[a], pixels[b]) = (pixels[b], pixels[a]);
                }
        }
        public void Apply() { Texture.SetPixels32(pixels); Texture.Apply(false, false); }
    }
}
