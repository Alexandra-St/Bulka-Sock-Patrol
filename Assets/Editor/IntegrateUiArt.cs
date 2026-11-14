using UnityEditor;
using UnityEngine;
using BulkoGame.Art;

public static class IntegrateUiArt
{
    [MenuItem("Bulka: Sock Patrol/Integrate UI art")]
    public static void Integrate()
    {
        string root = "Assets/Art/PixelArt/";
        string[] names = { "HudCard", "FinishCard", "TrapZoneMarker", "TrapPromptChip" };
        foreach (string name in names)
        {
            string path = root + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.IO.FileNotFoundException("Missing UI art", path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        var art = AssetDatabase.LoadAssetAtPath<ApartmentArtSet>("Assets/Art/ApartmentArtSet.asset");
        art.trapPromptChip = AssetDatabase.LoadAssetAtPath<Sprite>(root + "TrapPromptChip.png");
        EditorUtility.SetDirty(art);
        AssetDatabase.SaveAssets();
    }
}
