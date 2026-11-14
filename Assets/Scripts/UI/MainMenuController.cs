using UnityEngine;
using UnityEngine.SceneManagement;
using BulkoGame.Audio;
using GameFlowController = BulkoGame.GameFlow.GameFlow;
using System.Collections.Generic;

namespace BulkoGame.UI
{
    /// <summary>Small alpha start/tutorial flow for the existing Level_01 scene.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private enum ScreenState { MainMenu, LevelSelect, Controls }

        private const string LevelOneScene = "Level_01";
        private static Texture2D pixel;
        private static bool guiStylesReady;
        private static GUIStyle titleStyle;
        private static GUIStyle footerStyle;
        private static GUIStyle buttonStyle;
        [SerializeField] private Texture2D background;
        public AudioConfig audioConfig;
        private GameAudio audio;
        private readonly Dictionary<string, bool> buttonHoverStates = new Dictionary<string, bool>();

        private ScreenState state = ScreenState.MainMenu;
        private bool waitingForKeyRelease;

        private void OnEnable()
        {
            Time.timeScale = 1f;
            var menuCamera = Camera.main;
            if (menuCamera != null && menuCamera.GetComponent<AudioListener>() == null)
                menuCamera.gameObject.AddComponent<AudioListener>();

            audio = GameAudio.EnsureInstance(audioConfig);
            audio.PlayMainMenuMusic();
        }

        private void Update()
        {
            if (state != ScreenState.Controls) return;
            if (waitingForKeyRelease)
            {
                if (!Input.anyKey) waitingForKeyRelease = false;
                return;
            }
            if (!Input.anyKeyDown) return;

            SceneManager.LoadScene(LevelOneScene, LoadSceneMode.Single);
        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            scale = Mathf.Max(.45f, scale);
            var fullScreen = new Rect(0f, 0f, Screen.width, Screen.height);
            DrawBackground(fullScreen);
            if (state == ScreenState.MainMenu) DrawMainMenu(scale);
            else if (state == ScreenState.LevelSelect) DrawLevelSelect(scale);
            else DrawControls(scale);
        }

        private void DrawBackground(Rect screen)
        {
            if (background == null) return;
            float aspect=(float)background.width/background.height, target=screen.width/screen.height;
            float w=screen.width,h=screen.height;
            if(aspect>target) w=h*aspect; else h=w/aspect;
            GUI.DrawTexture(new Rect((screen.width-w)/2,(screen.height-h)/2,w,h),background,ScaleMode.StretchToFill,false);
        }

        private void DrawMainMenu(float scale)
        {
            float x=Screen.width*.72f,w=340*scale,h=54*scale,y=Screen.height*.36f;
            DrawLabel("Bulka: Sock Patrol",new Rect(x-w*.6f,y-112*scale,w*1.2f,82*scale),titleStyle,40*scale);
            DrawButton("PLAY",new Rect(x-w/2,y,w,h),scale,BeginControls);
            DrawButton("LEVEL SELECT",new Rect(x-w/2,y+72*scale,w,h),scale,()=>state=ScreenState.LevelSelect);
            DrawButton("QUIT",new Rect(x-w/2,y+144*scale,w,h),scale,Quit);
            DrawLabel("Alpha 0.7",new Rect(x-w/2,y+220*scale,w,28*scale),footerStyle,18*scale);
        }

        private void Quit() { if (!Application.isEditor) Application.Quit(); }

        private void DrawLevelSelect(float scale)
        {
            float centerX = Screen.width * .72f;
            float buttonWidth = 148f * scale;
            float buttonHeight = 46f * scale;
            float columnGap = 14f * scale;
            float rowGap = 8f * scale;
            float gridTop = Screen.height * .30f;
            float totalWidth = buttonWidth * 2f + columnGap;
            float left = centerX - totalWidth * .5f;
            DrawLabel("SELECT LEVEL", new Rect(centerX - totalWidth * .65f, gridTop - 94f * scale, totalWidth * 1.3f, 64f * scale), titleStyle, 46f * scale);

            for (int level = 1; level <= GameFlowController.CampaignLevelCount; level++)
            {
                int index = level - 1;
                int column = index % 2;
                int row = index / 2;
                int selectedLevel = level;
                var rect = new Rect(left + column * (buttonWidth + columnGap), gridTop + row * (buttonHeight + rowGap), buttonWidth, buttonHeight);
                if (GameFlowController.IsCampaignLevelUnlocked(level))
                    DrawButton($"LEVEL {level}", rect, scale, () => LoadCampaignLevel(selectedLevel));
                else
                    DrawLockedButton(rect, scale, level);
            }

            float backY = gridTop + 5f * (buttonHeight + rowGap) + 4f * scale;
            DrawButton("BACK", new Rect(centerX - buttonWidth * .5f, backY, buttonWidth, buttonHeight), scale, () => state = ScreenState.MainMenu);
        }

        private void DrawLockedButton(Rect rect, float scale, int level)
        {
            DrawRect(new Rect(rect.x, rect.y + 5f * scale, rect.width, rect.height), new Color32(44, 42, 43, 235));
            DrawRect(rect, new Color32(74, 68, 65, 245));
            DrawRect(new Rect(rect.x + 3f * scale, rect.y + 3f * scale, rect.width - 6f * scale, rect.height - 6f * scale), new Color32(112, 101, 91, 240));
            Color old = GUI.color;
            GUI.color = new Color32(184, 176, 161, 255);
            DrawLabel($"LEVEL {level} LOCKED", rect, buttonStyle, 15f * scale);
            GUI.color = old;
        }

        private void LoadLevel(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        private void LoadCampaignLevel(int level)
        {
            if (!GameFlowController.IsCampaignLevelUnlocked(level)) return;
            LoadLevel($"Level_{level:00}");
        }

        private void DrawControls(float scale)
        {
            float x=Screen.width*.77f,w=340*scale,y=Screen.height*.3f;
            DrawLabel("HOW TO PLAY",new Rect(x-w/2,y-70*scale,w,56*scale),titleStyle,34*scale);
            DrawLabel("WASD / Arrows — Move\nCollect socks and return to basket\n\nE — Place trap\nSpace — Bark\n\nPress any key to continue",new Rect(x-w/2,y,w,260*scale),footerStyle,18*scale);
        }

        private void BeginControls()
        {
            state = ScreenState.Controls;
            waitingForKeyRelease = true;
            if (audio != null) audio.Play(GameSound.StartGame);
        }

        private void DrawButton(string label, Rect rect, float scale, System.Action onClick)
        {
            buttonStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(28f * scale));
            bool hovered = rect.Contains(Event.current.mousePosition);
            bool pressed = hovered && Event.current.type == EventType.MouseDown && Event.current.button == 0;
            bool wasHovered = buttonHoverStates.TryGetValue(label, out bool previousHover) && previousHover;
            if (hovered && !wasHovered && audio != null) audio.Play(GameSound.ButtonHover);
            buttonHoverStates[label] = hovered;
            DrawRect(new Rect(rect.x, rect.y + 5f * scale, rect.width, rect.height), new Color32(62, 43, 42, 245));
            DrawRect(rect, new Color32(88, 55, 47, 255));
            DrawRect(new Rect(rect.x + 3f * scale, rect.y + 3f * scale, rect.width - 6f * scale, rect.height - 6f * scale),
                pressed ? new Color32(147, 88, 60, 255) : hovered ? new Color32(194, 126, 76, 255) : new Color32(166, 101, 65, 255));
            DrawRect(new Rect(rect.x + 7f * scale, rect.y + 7f * scale, rect.width - 14f * scale, 2f * scale), new Color32(225, 174, 119, 230));
            if (GUI.Button(rect, label, buttonStyle))
            {
                if (audio != null) audio.Play(GameSound.ButtonClick);
                onClick?.Invoke();
            }
        }

        private static void DrawLabel(string text, Rect rect, GUIStyle style, float fontSize, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            style.fontSize = Mathf.Max(12, Mathf.RoundToInt(fontSize));
            style.alignment = anchor;
            GUI.Label(rect, text, style);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = old;
        }

        private static void EnsureStyles()
        {
            if (pixel == null)
            {
                pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Menu Pixel" };
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply(false, true);
            }
            if (guiStylesReady) return;

            titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color32(255, 239, 202, 255) } };
            footerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color32(238, 211, 169, 255) } };
            buttonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color32(255, 243, 218, 255), background = null },
                hover = { textColor = Color.white, background = null },
                active = { textColor = Color.white, background = null }
            };
            guiStylesReady = true;
        }
    }
}
