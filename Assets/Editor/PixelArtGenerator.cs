using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using BulkoGame.Art;

public static class PixelArtGenerator
{
    private const string Root = "Assets/Art/PixelArt";
    private static readonly Color32 Ink = C(69, 49, 44), DarkWood = C(111, 70, 51), Wood = C(169, 111, 70), LightWood = C(220, 164, 105), Cream = C(247, 223, 183), Sage = C(125, 151, 119), SageLight = C(171, 189, 151), Tan = C(177, 111, 67);

    [MenuItem("BulkoGame/Generate Pixel Art Sprites")]
    public static void Generate()
    {
        Directory.CreateDirectory(Root);
        var assets = new Dictionary<string, Texture2D>();
        assets["WoodFloorLeft"] = Floor(140, 272, false);
        assets["WoodFloorCenter"] = Floor(212, 272, false);
        assets["KitchenFloor"] = Floor(120, 112, true);
        assets["BathroomFloor"] = Floor(120, 160, true, true);
        assets["Wall"] = Draw(24, 24, p => { p.R(0,0,24,24,Ink); p.R(2,2,20,20,C(236,190,132)); p.R(2,2,20,3,C(255,222,164)); p.R(3,19,18,2,C(186,127,83)); p.R(7,6,2,13,C(224,172,112)); p.R(16,6,2,13,C(224,172,112)); });
        assets["Bed"] = Bed();
        assets["Nightstand"] = Nightstand();
        assets["Wardrobe"] = Wardrobe();
        assets["Bookshelf"] = Bookshelf();
        assets["Sofa"] = Sofa();
        assets["SideTable"] = SideTable();
        assets["CoffeeTable"] = CoffeeTable();
        assets["Basket"] = Basket();
        assets["BedroomRug"] = Rug(80, 56, false);
        assets["LivingRug"] = Rug(104, 64, true);
        assets["KitchenCounter"] = KitchenCounter();
        assets["DiningTable"] = DiningTable();
        assets["Fridge"] = Fridge();
        assets["Bathtub"] = Bathtub();
        assets["Vanity"] = Vanity();
        assets["Plant"] = Plant();
        assets["BathroomMat"] = Rug(56, 32, false);
        assets["SockPink"] = Sock(C(228, 95, 150));
        assets["SockBlue"] = Sock(C(87, 144, 215));
        assets["SockYellow"] = Sock(C(235, 193, 84));
        assets["SockGreen"] = Sock(C(101, 166, 121));
        assets["OrangeCat"] = Cat();
        assets["TrapZoneMarker"] = TrapZoneMarker();
        assets["SlowTrap"] = SlowTrap();
        assets["HudCard"] = UiCard(false);
        assets["FinishCard"] = UiCard(true);
        foreach (var pair in assets) Write(pair.Key, pair.Value);
        AssetDatabase.Refresh();
        foreach (var pair in assets)
        {
            var path = Root + "/" + pair.Key + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        var setPath = "Assets/Art/ApartmentArtSet.asset";
        var artSet = AssetDatabase.LoadAssetAtPath<ApartmentArtSet>(setPath);
        if (artSet == null) { artSet = ScriptableObject.CreateInstance<ApartmentArtSet>(); AssetDatabase.CreateAsset(artSet, setPath); }
        var so = new SerializedObject(artSet);
        foreach (var pair in assets)
        {
            var prop = so.FindProperty(CharLower(pair.Key));
            if (prop != null) prop.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/" + pair.Key + ".png");
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(artSet); AssetDatabase.SaveAssets();
        Debug.Log("BulkoGame: authored and imported " + assets.Count + " crisp pixel-art sprites.");
    }

    private static string CharLower(string s) => char.ToLowerInvariant(s[0]) + s.Substring(1);
    private static void Write(string name, Texture2D t) { File.WriteAllBytes(Root + "/" + name + ".png", t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t); }
    private static Texture2D Draw(int w,int h,Action<Pixel> paint) { var t = new Texture2D(w,h,TextureFormat.RGBA32,false); t.filterMode=FilterMode.Point; t.wrapMode=TextureWrapMode.Clamp; var a=new Color32[w*h]; Array.Fill(a,C(0,0,0,0)); t.SetPixels32(a); paint(new Pixel(t,w,h)); t.Apply(false,false); return t; }
    private static Texture2D Floor(int w,int h,bool tile,bool cool=false) => Draw(w,h,p => {
        var baseC = tile ? (cool ? C(212,216,200) : C(232,220,190)) : C(232,174,112); p.R(0,0,w,h,baseC);
        if(tile){var grout=cool?C(169,181,164):C(199,177,142); for(int x=0;x<w;x+=16)p.R(x,0,1,h,grout); for(int y=0;y<h;y+=16)p.R(0,y,w,1,grout); for(int y=3;y<h;y+=16)for(int x=3;x<w;x+=16)p.R(x,y,3,1,C(241,231,207));}
        else { for(int y=2;y<h;y+=16){p.R(0,y,w,2,C(193,132,83)); int off=((y/16)&1)*24; for(int x=off;x<w;x+=64){p.R(x,y+2,2,12,C(211,148,93));p.R(x+9,y+9,12,1,C(239,191,130));} } }
    });
    private static Texture2D Bed() => Draw(64,80,p=>{p.R(3,4,58,72,C(86,54,43,90));p.R(4,3,56,72,Ink);p.R(7,5,50,68,DarkWood);p.R(9,7,46,62,C(174,124,83));p.R(11,9,42,58,C(207,159,112));p.R(12,10,40,15,C(245,230,200));p.R(12,26,40,37,Sage);p.R(14,28,36,32,SageLight);p.R(14,30,36,2,C(113,143,111));for(int x=16;x<50;x+=8){p.R(x,36,2,19,C(157,176,142));p.R(x+2,55,2,7,C(111,145,112));}p.R(8,4,6,5,DarkWood);p.R(50,4,6,5,DarkWood);});
    private static Texture2D Nightstand() => Draw(40,40,p=>{p.R(3,5,34,31,Ink);p.R(6,7,28,26,Wood);p.R(8,9,24,9,LightWood);p.R(9,21,22,10,C(151,91,59));p.R(18,24,4,3,C(238,195,126));p.R(7,33,4,4,DarkWood);p.R(29,33,4,4,DarkWood);p.R(17,1,7,7,C(255,225,156));p.R(19,2,3,8,C(255,241,199));});
    private static Texture2D Wardrobe() => Draw(56,72,p=>{p.R(3,4,50,64,Ink);p.R(6,6,44,60,DarkWood);p.R(8,8,40,56,Wood);p.R(10,10,17,52,LightWood);p.R(29,10,17,52,C(185,120,75));p.R(27,9,2,54,Ink);p.R(23,34,3,5,C(246,205,132));p.R(34,34,3,5,C(246,205,132));p.R(9,61,7,3,DarkWood);p.R(40,61,7,3,DarkWood);});
    private static Texture2D Bookshelf() => Draw(80,40,p=>{p.R(3,5,74,31,Ink);p.R(6,7,68,27,DarkWood);p.R(9,10,62,21,C(205,158,108));p.R(10,10,60,3,LightWood);var cols=new[]{C(87,132,126),C(203,128,88),C(225,190,117),C(102,119,151),C(166,91,86),C(120,151,115)};int x=13;for(int i=0;i<12;i++){int bw=3+(i%3);p.R(x,15,bw,15,cols[i%cols.Length]);p.R(x,15,bw,2,C(241,213,160));x+=5;}p.R(12,31,55,3,DarkWood);});
    private static Texture2D Sofa() => Draw(56,80,p=>{p.R(3,4,50,72,C(70,47,40,100));p.R(4,4,48,71,Ink);p.R(7,6,42,67,C(94,121,93));p.R(10,9,36,60,Sage);p.R(12,10,32,55,SageLight);p.R(11,10,34,8,C(110,143,109));p.R(11,58,34,8,C(108,141,104));p.R(8,15,6,43,C(112,143,108));p.R(42,15,6,43,C(112,143,108));p.R(16,20,24,2,C(196,202,160));p.R(16,47,24,2,C(196,202,160));p.R(15,72,5,4,DarkWood);p.R(36,72,5,4,DarkWood);p.R(18,33,20,11,C(208,156,133));p.R(21,36,14,2,C(232,191,151));});
    private static Texture2D SideTable()=>Draw(40,40,p=>{p.R(4,5,32,29,Ink);p.R(7,7,26,24,Wood);p.R(9,9,22,20,LightWood);p.R(12,12,16,14,C(196,136,86));p.R(18,17,4,4,C(242,202,136));p.R(9,31,4,4,DarkWood);p.R(27,31,4,4,DarkWood);});
    private static Texture2D CoffeeTable()=>Draw(72,56,p=>{p.R(4,6,64,44,C(60,43,38,90));p.R(4,4,64,43,Ink);p.R(7,7,58,37,DarkWood);p.R(10,9,52,33,Wood);p.R(12,11,48,29,LightWood);p.R(14,13,44,25,C(192,130,79));p.R(16,16,40,2,C(226,169,111));p.R(17,47,5,4,Ink);p.R(53,47,5,4,Ink);p.R(30,20,10,8,C(93,131,110));p.R(32,18,6,4,C(69,107,87));p.R(47,20,9,10,C(229,205,165));p.R(49,22,5,2,C(128,83,59));});
    private static Texture2D Basket()=>Draw(72,64,p=>{p.R(5,13,62,45,C(59,43,39,100));p.R(7,13,58,44,Ink);p.R(10,17,52,36,C(165,111,65));p.R(12,20,48,30,C(198,148,88));for(int x=14;x<58;x+=7){p.R(x,20,2,27,C(157,104,61));p.R(x+2,21,2,25,C(222,169,104));}p.R(12,17,48,5,C(235,190,126));p.R(14,15,44,3,C(249,213,156));p.R(20,5,13,20,C(92,136,174));p.R(20,5,20,10,C(92,136,174));p.R(23,7,8,13,C(135,177,209));p.R(39,2,11,24,C(219,91,142));p.R(39,2,17,9,C(219,91,142));p.R(42,5,7,14,C(245,181,99));p.R(53,8,10,19,C(121,163,118));p.R(53,8,16,8,C(121,163,118));p.R(56,10,5,11,C(174,208,151));});
    private static Texture2D Rug(int w,int h,bool green)=>Draw(w,h,p=>{var main=green?C(177,174,139):C(220,196,153);p.R(2,3,w-4,h-6,Ink);p.R(4,5,w-8,h-10,C(171,126,86));p.R(6,7,w-12,h-14,main);p.R(8,9,w-16,h-18,green?C(204,195,152):C(233,216,176));p.R(10,11,w-20,h-22,main);for(int x=12;x<w-12;x+=8){p.R(x,13,2,2,C(193,140,96));p.R(x, h-15,2,2,C(193,140,96));}p.R(8,10,2,h-20,C(242,226,188));p.R(w-10,10,2,h-20,C(242,226,188));});
    private static Texture2D KitchenCounter()=>Draw(136,40,p=>{p.R(3,6,130,30,Ink);p.R(6,8,124,26,C(130,83,58));p.R(8,9,120,5,C(224,162,105));p.R(9,16,34,16,C(184,124,81));p.R(11,18,30,4,C(230,180,126));p.R(46,16,29,16,C(151,104,73));p.R(49,18,23,2,C(238,196,144));p.R(84,14,26,18,C(75,70,72));p.R(87,17,20,13,C(106,100,96));p.R(90,20,4,4,Ink);p.R(100,20,4,4,Ink);p.R(108,16,17,16,C(218,199,162));p.R(111,18,11,8,C(151,183,177));p.R(117,20,2,5,C(100,135,132));p.R(9,34,6,4,DarkWood);p.R(120,34,6,4,DarkWood);});
    private static Texture2D DiningTable()=>Draw(80,64,p=>{p.R(4,4,72,54,Ink);p.R(8,7,64,46,DarkWood);p.R(11,10,58,40,Wood);p.R(14,13,52,34,LightWood);p.R(16,15,48,30,C(205,142,86));p.R(17,16,46,3,C(231,173,113));p.R(8,0,8,10,DarkWood);p.R(64,0,8,10,DarkWood);p.R(8,52,8,10,DarkWood);p.R(64,52,8,10,DarkWood);p.R(34,25,12,10,C(75,123,91));p.R(37,21,6,5,C(56,104,76));p.R(20,20,7,7,C(247,236,213));p.R(58,20,7,7,C(247,236,213));});
    private static Texture2D Fridge()=>Draw(48,72,p=>{p.R(3,3,42,66,Ink);p.R(6,5,36,62,C(218,218,199));p.R(8,7,32,30,C(238,237,217));p.R(8,39,32,26,C(227,227,209));p.R(8,37,32,2,C(159,164,153));p.R(35,15,3,15,C(139,142,131));p.R(35,46,3,13,C(139,142,131));p.R(10,8,27,2,C(255,250,229));});
    private static Texture2D Bathtub()=>Draw(56,112,p=>{p.R(3,5,50,101,Ink);p.R(6,8,44,95,C(204,211,199));p.R(9,11,38,88,C(244,239,220));p.R(12,14,32,82,C(169,188,183));p.R(15,18,26,73,C(207,222,212));p.R(18,22,20,64,C(194,218,215));p.R(21,26,14,55,C(216,232,224));p.R(10,11,36,4,C(252,247,229));p.R(23,98,10,3,C(144,158,153));});
    private static Texture2D Vanity()=>Draw(56,48,p=>{p.R(4,5,48,38,Ink);p.R(7,8,42,32,DarkWood);p.R(10,11,36,25,Wood);p.R(12,13,32,12,C(229,220,198));p.R(14,15,28,8,C(183,202,195));p.R(22,17,12,4,C(244,238,220));p.R(25,15,6,3,C(135,150,145));p.R(12,27,32,9,C(165,102,67));p.R(26,29,4,3,C(236,190,123));});
    private static Texture2D Plant()=>Draw(40,48,p=>{p.R(8,29,24,15,Ink);p.R(10,31,20,11,C(185,117,81));p.R(12,33,16,7,C(218,158,111));p.R(17,15,6,18,C(64,109,79));p.R(8,16,13,8,C(83,136,91));p.R(4,11,13,7,C(104,157,99));p.R(18,8,10,13,C(71,124,82));p.R(25,12,12,8,C(100,151,93));p.R(27,20,9,7,C(130,173,105));p.R(10,18,8,8,C(126,169,104));p.R(19,10,2,10,C(158,191,122));});
    private static Texture2D Sock(Color32 color)=>Draw(20,28,p=>{p.R(4,2,12,22,Ink);p.R(7,4,8,17,color);p.R(7,4,12,6,color);p.R(7,9,8,2,C(247,222,177));p.R(7,16,8,2,C(247,222,177));p.R(6,2,10,3,C(251,239,210));p.R(6,22,10,4,color);p.R(10,24,11,3,Ink);p.R(12,24,8,2,color);});
    private static Texture2D Cat()=>Draw(48,40,p=>{
        var orange=C(235,116,48); var light=C(255,173,79); var cream=C(246,218,174); var deep=C(83,47,40);
        p.R(5,11,36,22,deep); p.R(8,8,31,24,orange); p.R(11,5,8,10,orange); p.R(29,5,8,10,orange);
        p.R(12,7,5,7,light); p.R(31,7,4,7,light); p.R(10,11,26,15,light); p.R(8,15,30,13,orange);
        p.R(12,29,5,7,deep); p.R(20,30,5,7,deep); p.R(29,29,5,7,deep); p.R(36,28,5,7,deep);
        p.R(14,13,5,5,cream); p.R(29,13,5,5,cream); p.R(16,14,2,4,deep); p.R(31,14,2,4,deep);
        p.R(22,18,6,4,C(207,108,115)); p.R(24,21,2,3,deep); p.R(12,21,8,2,cream); p.R(28,21,8,2,cream);
        p.R(5,22,7,3,deep); p.R(1,21,7,3,deep); p.R(2,19,5,3,deep); p.R(11,9,3,2,C(255,205,121)); p.R(31,9,3,2,C(255,205,121));
    });
    private static Texture2D TrapZoneMarker()=>Draw(32,32,p=>{
        var edge=C(115,160,145,195); var glint=C(211,223,177,190); var dot=C(175,202,162,150);
        p.R(4,3,7,2,edge); p.R(3,4,2,7,edge); p.R(21,3,7,2,edge); p.R(27,4,2,7,edge);
        p.R(4,27,7,2,edge); p.R(3,21,2,7,edge); p.R(21,27,7,2,edge); p.R(27,21,2,7,edge);
        p.R(5,5,5,1,glint); p.R(5,6,1,4,glint); p.R(22,5,5,1,glint); p.R(26,6,1,4,glint);
        p.R(5,24,5,1,glint); p.R(5,22,1,4,glint); p.R(22,24,5,1,glint); p.R(26,22,1,4,glint);
        p.R(13,13,2,2,dot); p.R(17,13,2,2,dot); p.R(15,17,2,2,dot);
    });
    private static Texture2D SlowTrap()=>Draw(32,24,p=>{
        var shadow=C(63,47,54,95); var mat=C(181,118,163); var light=C(225,165,197); var trim=C(121,78,124); var paw=C(245,201,129); var ink=C(75,54,62);
        p.R(4,5,24,15,shadow); p.R(3,4,24,15,ink); p.R(5,3,22,14,trim); p.R(6,4,20,12,mat);
        p.R(8,5,16,2,light); p.R(5,8,2,7,light); p.R(23,8,2,7,trim); p.R(8,15,16,2,trim);
        p.R(14,8,5,4,paw); p.R(12,7,2,2,paw); p.R(15,6,2,2,paw); p.R(18,7,2,2,paw);
        p.R(12,12,9,2,C(202,144,177)); p.R(10,10,2,2,C(244,208,221)); p.R(21,12,2,2,C(244,208,221));
        p.R(7,18,4,2,C(140,96,93)); p.R(21,18,4,2,C(140,96,93));
    });
    private static Texture2D UiCard(bool finish)=>Draw(finish?128:96,finish?64:32,p=>{int w=finish?128:96,h=finish?64:32;p.R(0,0,w,h,C(54,39,38,250));p.R(2,2,w-4,h-4,C(76,51,45,250));p.R(4,4,w-8,2,C(246,195,129));p.R(4,h-6,w-8,2,C(140,91,65));p.R(4,6,2,h-12,C(246,195,129));p.R(w-6,6,2,h-12,C(140,91,65));});
    private static Color32 C(byte r,byte g,byte b,byte a=255)=>new Color32(r,g,b,a);
    private sealed class Pixel
    {
        private readonly Texture2D t; private readonly int w,h; public Pixel(Texture2D tex,int width,int height){t=tex;w=width;h=height;}
        public void R(int x,int y,int rw,int rh,Color32 c){for(int yy=Mathf.Max(0,y);yy<Mathf.Min(h,y+rh);yy++)for(int xx=Mathf.Max(0,x);xx<Mathf.Min(w,x+rw);xx++)t.SetPixel(xx,yy,c);}
    }
}
