using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using BulkoGame.Art;

public static class IntegrateCharacterSheets
{
    private const string FrameRoot = "Assets/Art/PixelArt/Characters";
    private const string ClipRoot = "Assets/Art/Animations/Characters";
    private static readonly string[] Directions = { "Down", "Up", "Left", "Right" };

    [MenuItem("Bulka: Sock Patrol/Integrate character sheets")]
    public static void Integrate()
    {
        if (!EditorUtility.DisplayDialog("Integrate character sheets",
                "This generator will overwrite/recreate generated character animation clips and controllers. This operation cannot be automatically undone through Unity Undo.",
                "Regenerate", "Cancel")) return;

        if (!HasRequiredFrames())
        {
            Debug.LogError("Integrate character sheets cancelled: one or more required source frames are missing.");
            return;
        }

        System.IO.Directory.CreateDirectory(ClipRoot);
        AssetDatabase.Refresh();
        var bulka = BuildCharacter("Bulka");
        var cat = BuildCharacter("OrangeCat");
        var set = AssetDatabase.LoadAssetAtPath<ApartmentArtSet>("Assets/Art/ApartmentArtSet.asset");
        set.bulka = LoadFrame("Bulka", "Gameplay", 1, 1);
        set.orangeCat = LoadFrame("OrangeCat", "Gameplay", 1, 1);
        set.bulkaAnimator = bulka;
        set.orangeCatAnimator = cat;
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        Debug.Log("Bulka: Sock Patrol: integrated 140 production character frames, 36 animation clips and two visual controllers.");
    }

    private static bool HasRequiredFrames()
    {
        string[] characters = { "Bulka", "OrangeCat" };
        string[] sheets = { "Gameplay", "Bark", "Victory", "Lose", "CarryingSock", "Fear" };
        foreach (string character in characters)
        foreach (string sheet in sheets)
        for (int row = 1; row <= 4; row++)
        for (int column = 1; column <= 5; column++)
        {
            string path = FrameRoot + "/" + character + "/" + character + "_" + sheet + "_R" + row + "_F" + column.ToString("00") + ".png";
            if (AssetImporter.GetAtPath(path) == null) return false;
        }
        return AssetDatabase.LoadAssetAtPath<ApartmentArtSet>("Assets/Art/ApartmentArtSet.asset") != null;
    }

    private static AnimatorController BuildCharacter(string character)
    {
        bool dog = character == "Bulka";
        var clips = new List<AnimationClip>();
        for (int d = 0; d < 4; d++)
        {
            int row = d + 1;
            string direction = Directions[d];
            clips.Add(Clip(character + "_Idle_" + direction,
                new[] { LoadFrame(character, "Gameplay", row, 1) }, true, 8));
            clips.Add(Clip(character + "_Walk_" + direction,
                Frames(character, "Gameplay", row, 2, 3, 4, 5), true, 8));
            if (dog)
            {
                clips.Add(Clip(character + "_Bark_" + direction,
                    Frames(character, "Bark", row, 1, 2, 3, 4), false, 8));
                int[] chosen = { 1, 2, 3, 4 };
                clips.Add(Clip(character + "_Victory_" + direction,
                    Frames(character, "Victory", row, chosen), true, 6));
                clips.Add(Clip(character + "_Lose_" + direction,
                    Frames(character, "Lose", row, 1, 2, 3, 4), true, 5));
            }
            else
            {
                clips.Add(Clip(character + "_CarryingSock_" + direction,
                    Frames(character, "CarryingSock", row, 2, 3, 4, 5), true, 8));
                clips.Add(Clip(character + "_Fear_" + direction,
                    Frames(character, "Fear", row, 1, 2, 3, 4), true, 8));
            }
        }
        string path = ClipRoot + "/" + character + ".controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach (var clip in clips)
        {
            var state = machine.AddState(clip.name);
            state.motion = clip;
            if (clip.name == character + "_Idle_Down") machine.defaultState = state;
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static Sprite[] Frames(string character, string sheet, int row, params int[] columns)
    {
        var frames = new Sprite[columns.Length];
        for (int i = 0; i < columns.Length; i++) frames[i] = LoadFrame(character, sheet, row, columns[i]);
        return frames;
    }

    private static Sprite LoadFrame(string character, string sheet, int row, int column)
    {
        string path = FrameRoot + "/" + character + "/" + character + "_" + sheet + "_R" + row + "_F" + column.ToString("00") + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new System.IO.FileNotFoundException("Missing character frame", path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static AnimationClip Clip(string name, Sprite[] frames, bool loop, int fps)
    {
        string path = ClipRoot + "/" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { name = name, frameRate = fps };
        var keys = new ObjectReferenceKeyframe[frames.Length + (loop && frames.Length > 1 ? 1 : 0)];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / (float)fps, value = frames[i] };
        if (keys.Length > frames.Length) keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / (float)fps, value = frames[0] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.stopTime = Mathf.Max(1, frames.Length) / (float)fps;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }
}
