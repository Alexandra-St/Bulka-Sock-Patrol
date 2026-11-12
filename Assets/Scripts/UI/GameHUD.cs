using UnityEngine;
using System;
using BulkoGame.GameFlow;
using BulkoGame.Art;
using BulkoGame.Audio;

namespace BulkoGame.UI
{
    public sealed class GameHUD : MonoBehaviour
    {
        private TextMesh counter;
        private TextMesh clock;
        private TextMesh carryingCounter;
        private TextMesh finishReason;
        private TextMesh trapCounter;
        private TextMesh trapPrompt;
        private TextMesh catCounter;
        private TextMesh barkCounter;
        private TextMesh resultSubheading;
        private TextMesh defenseHeading;
        private GameObject finishPanel;
        private GameObject trapPromptCard;
        private Action playAgainAction;
        private Action mainMenuAction;
        private Action nextLevelAction;
        private bool showResultButtons;
        private bool showNextLevelButton;
        private GUIStyle resultButtonStyle;
        private GUIStyle focusedButtonStyle;
        private Texture2D normalButtonTexture;
        private Texture2D hoverButtonTexture;
        private Texture2D pressedButtonTexture;
        private int focusedButton;
        private int hoveredButton = -1;
        private GameAudio audio;
        private BulkoGame.GameFlow.GameFlow gameFlow;
        private bool isPaused;
        private GUIStyle pausePanelStyle;
        private GUIStyle pauseTitleStyle;
        private Texture2D pausePanelTexture;
        private SpriteRenderer resultPanelRenderer;
        private Texture2D hudPanelTexture;
        private Sprite hudPanelSprite;

        public void Build(ApartmentArtSet art, GameAudio gameAudio)
        {
            audio = gameAudio;
            hudPanelTexture = CreateHudPanelTexture();
            hudPanelSprite = UnityEngine.Sprite.Create(hudPanelTexture, new Rect(0, 0, hudPanelTexture.width, hudPanelTexture.height), new Vector2(.5f, .5f), 16f);
            Sprite("HUD Card",new Vector3(-13.0f,7.3f,-1.25f),hudPanelSprite,new Vector2(5.9f,2.8f),400);
            counter=Label("Sock Counter",new Vector3(-15.58f,8.17f,-.8f),"Socks: 0 / 10",32,TextAnchor.UpperLeft,401);
            clock=Label("Collect Timer",new Vector3(-15.58f,7.63f,-.8f),"01:00",32,TextAnchor.UpperLeft,401);
            clock.color=new Color32(255,207,141,255);
            carryingCounter=Label("Carrying Counter",new Vector3(-15.58f,7.09f,-.8f),"Carrying: 0/1",27,TextAnchor.UpperLeft,401);
            carryingCounter.color=new Color32(230,217,181,255);
            counter.characterSize = clock.characterSize = carryingCounter.characterSize = .12f;
            counter.color = new Color32(255, 242, 214, 255);
            clock.color = new Color32(255, 226, 176, 255);
            carryingCounter.color = new Color32(255, 239, 202, 255);
            trapCounter=Label("Trap Counter",new Vector3(-15.58f,7.63f,-.8f),"Traps: 0 / 2",29,TextAnchor.UpperLeft,401);
            trapCounter.characterSize = .12f;
            trapCounter.gameObject.SetActive(false);
            catCounter=Label("Defense Cat Counter",new Vector3(-15.58f,7.08f,-.8f),"Cats: 0 / 5",29,TextAnchor.UpperLeft,401);
            catCounter.characterSize = .12f;
            catCounter.gameObject.SetActive(false);
            barkCounter=Label("Bark Cooldown",new Vector3(-15.58f,6.53f,-.8f),"Bark: Ready",29,TextAnchor.UpperLeft,401);
            barkCounter.characterSize = .12f;
            barkCounter.gameObject.SetActive(false);
            trapPromptCard=Sprite("Trap Placement Prompt Chip",Vector3.zero,art.trapPromptChip != null ? art.trapPromptChip : art.hudCard,new Vector2(2.75f,.625f),500);
            trapPrompt=Label("Trap Placement Prompt",Vector3.zero,"E — Place trap",24,TextAnchor.MiddleCenter,501);
            trapPrompt.characterSize=.115f;
            trapPrompt.color=new Color32(65,43,35,255);
            trapPromptCard.SetActive(false);
            trapPrompt.gameObject.SetActive(false);
            finishPanel=new GameObject("Defense Phase Overlay"); finishPanel.SetActive(false);
            var resultCard = Sprite("Overlay Card",new Vector3(0,0,-1.3f),art.finishCard,new Vector2(16f,10.6f),700,finishPanel.transform);
            resultPanelRenderer = resultCard.GetComponent<SpriteRenderer>();
            resultPanelRenderer.color = new Color32(118, 88, 78, 232);
            finishReason=Label("Finish Reason",new Vector3(0,.7f,-.7f),"ALL SOCKS COLLECTED!",39,TextAnchor.MiddleCenter,701,finishPanel.transform);
            finishReason.color=new Color32(255,239,202,255);
            defenseHeading=Label("Defense Phase Heading",new Vector3(0,-.22f,-.7f),"DEFENSE PHASE",46,TextAnchor.MiddleCenter,701,finishPanel.transform);
            resultSubheading=Label("Defense Result Detail",new Vector3(-1.25f,-.65f,-.7f),"",27,TextAnchor.MiddleCenter,701,finishPanel.transform);
            resultSubheading.gameObject.SetActive(false);
        }

        public void BindGameFlow(BulkoGame.GameFlow.GameFlow flow)
        {
            gameFlow = flow;
            isPaused = false;
            Time.timeScale = 1f;
            if (gameFlow != null) gameFlow.SetPaused(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        }

        public void TogglePause()
        {
            if (gameFlow == null || gameFlow.Session == null) return;
            GamePhase phase = gameFlow.Session.Phase;
            if (phase != GamePhase.Collect && phase != GamePhase.Defense) return;

            SetPaused(!isPaused);
        }

        private void SetPaused(bool paused)
        {
            isPaused = paused;
            Time.timeScale = isPaused ? 0f : 1f;
            if (gameFlow != null) gameFlow.SetPaused(isPaused);
        }

        public void Refresh(int collected,int total,float seconds,bool carrying)
        {
            counter.text="Socks: "+collected+" / "+total;
            carryingCounter.text=carrying?"Carrying: 1/1":"Carrying: 0/1";
            int s=Mathf.CeilToInt(seconds);
            clock.text=string.Format("{0:00}:{1:00}",s/60,s%60);
        }
        public void ShowDefense(bool allCollected)
        {
            finishReason.text=allCollected?"ALL SOCKS COLLECTED!":"TIME'S UP!";
            finishPanel.SetActive(true);
        }

        public void BeginDefense(int socksSafe)
        {
            finishPanel.SetActive(false);
            clock.gameObject.SetActive(false);
            carryingCounter.gameObject.SetActive(false);
            counter.text = "Socks safe: " + socksSafe;
            trapCounter.gameObject.SetActive(true);
            catCounter.gameObject.SetActive(true);
        }


        public void BeginWave(int totalCats)
        {
            catCounter.text = "Cats: 0 / " + totalCats;
        }

        public void RefreshCatCount(int finished, int totalCats)
        {
            catCounter.text = "Cats: " + finished + " / " + totalCats;
        }

        public void ShowWaveResult(bool won, int socksSurvived)
        {
            finishReason.text = won ? "YOU DID IT!" : "OH NO!";
            defenseHeading.gameObject.SetActive(false);
            finishReason.transform.localPosition = new Vector3(0f, 2.15f, -.7f);
            finishReason.fontSize = 58;
            finishReason.characterSize = .14f;
            resultSubheading.transform.localPosition = new Vector3(0f, 1.0f, -.7f);
            resultSubheading.text = "Socks saved: " + socksSurvived + " / 10";
            resultSubheading.fontSize = 34;
            resultSubheading.characterSize = .12f;
            finishReason.color = won ? new Color32(255, 239, 202, 255) : new Color32(255, 222, 203, 255);
            resultSubheading.color = new Color32(255, 231, 194, 255);
            resultSubheading.gameObject.SetActive(true);
            if (audio != null) audio.Play(GameSound.ResultPanel);
            showResultButtons = true;
            showNextLevelButton = won && nextLevelAction != null;
            focusedButton = 0;
            finishPanel.SetActive(true);
        }

        public void SetResultActions(Action playAgain, Action mainMenu, Action nextLevel = null)
        {
            playAgainAction = playAgain;
            mainMenuAction = mainMenu;
            nextLevelAction = nextLevel;
        }

        private void OnGUI()
        {
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            scale = Mathf.Max(.45f, scale);
            EnsureButtonStyles();
            DrawPauseUI(scale);
            if (!showResultButtons || finishPanel == null || !finishPanel.activeSelf) return;
            int buttonCount = showNextLevelButton ? 3 : 2;
            focusedButton = Mathf.Clamp(focusedButton, 0, buttonCount - 1);
            float width = 280f * scale;
            float height = 58f * scale;
            float gap = 12f * scale;
            float left = (Screen.width - width) * .5f;
            float stackHeight = buttonCount * height + (buttonCount - 1) * gap;
            float top = Screen.height * .62f - stackHeight * .5f;
            var nextRect = new Rect(left, top, width, height);
            var playRect = new Rect(left, top + (showNextLevelButton ? height + gap : 0f), width, height);
            var menuRect = new Rect(left, top + (showNextLevelButton ? 2f : 1f) * (height + gap), width, height);
            Event current = Event.current;
            if (current.type == EventType.KeyDown)
            {
                if (current.keyCode == KeyCode.LeftArrow || current.keyCode == KeyCode.UpArrow)
                { focusedButton = (focusedButton - 1 + buttonCount) % buttonCount; current.Use(); }
                else if (current.keyCode == KeyCode.RightArrow || current.keyCode == KeyCode.DownArrow || current.keyCode == KeyCode.Tab)
                { focusedButton = (focusedButton + 1) % buttonCount; current.Use(); }
                else if (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter || current.keyCode == KeyCode.Space)
                {
                    current.Use();
                    if (audio != null) audio.Play(GameSound.ButtonClick);
                    InvokeFocusedResultAction();
                    return;
                }
            }
            if (current.type == EventType.MouseMove)
            {
                int nextHovered = showNextLevelButton && nextRect.Contains(current.mousePosition) ? 0
                    : playRect.Contains(current.mousePosition) ? (showNextLevelButton ? 1 : 0)
                    : menuRect.Contains(current.mousePosition) ? (showNextLevelButton ? 2 : 1) : -1;
                if (nextHovered != hoveredButton && nextHovered >= 0 && audio != null) audio.Play(GameSound.ButtonHover);
                hoveredButton = nextHovered;
                if (nextHovered >= 0) focusedButton = nextHovered;
            }
            resultButtonStyle.fontSize = focusedButtonStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(22f * scale));
            if (showNextLevelButton && GUI.Button(nextRect, "NEXT LEVEL", focusedButton == 0 ? focusedButtonStyle : resultButtonStyle))
            { PlayButtonClick(); RunNavigation(nextLevelAction); return; }
            if (GUI.Button(playRect, "PLAY AGAIN", focusedButton == (showNextLevelButton ? 1 : 0) ? focusedButtonStyle : resultButtonStyle))
            { PlayButtonClick(); RunNavigation(playAgainAction); return; }
            if (GUI.Button(menuRect, "MAIN MENU", focusedButton == (showNextLevelButton ? 2 : 1) ? focusedButtonStyle : resultButtonStyle))
            { PlayButtonClick(); RunNavigation(mainMenuAction); }
        }

        private void DrawPauseUI(float scale)
        {
            bool activeGameplay = gameFlow != null && gameFlow.Session != null
                && (gameFlow.Session.Phase == GamePhase.Collect || gameFlow.Session.Phase == GamePhase.Defense);
            if (!activeGameplay) return;
            resultButtonStyle.fontSize = focusedButtonStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(19f * scale));

            if (isPaused)
            {
                GUI.color = new Color(0f, 0f, 0f, .42f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;

                float width = 440f * scale;
                float height = 310f * scale;
                var panel = new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);
                GUI.Box(panel, GUIContent.none, pausePanelStyle);
                pauseTitleStyle.fontSize = Mathf.Max(20, Mathf.RoundToInt(38f * scale));
                GUI.Label(new Rect(panel.x, panel.y + 28f * scale, width, 56f * scale), "PAUSED", pauseTitleStyle);
                float buttonWidth = 270f * scale;
                float buttonHeight = 54f * scale;
                float buttonX = panel.center.x - buttonWidth * .5f;
                var continueRect = new Rect(buttonX, panel.y + 116f * scale, buttonWidth, buttonHeight);
                var menuRect = new Rect(buttonX, panel.y + 188f * scale, buttonWidth, buttonHeight);
                if (GUI.Button(continueRect, "CONTINUE", focusedButtonStyle)) SetPaused(false);
                if (GUI.Button(menuRect, "MAIN MENU", resultButtonStyle))
                {
                    SetPaused(false);
                    RunNavigation(() => UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single));
                }
                return;
            }

            float buttonSize = 54f * scale;
            float margin = 24f * scale;
            var pauseRect = new Rect(Screen.width - buttonSize - margin, margin, buttonSize, buttonSize);
            int previousFontSize = resultButtonStyle.fontSize;
            resultButtonStyle.fontSize = Mathf.Max(18, Mathf.RoundToInt(25f * scale));
            bool pauseClicked = GUI.Button(pauseRect, "II", resultButtonStyle);
            resultButtonStyle.fontSize = previousFontSize;
            if (pauseClicked) TogglePause();
        }

        private void RunNavigation(Action action)
        {
            Time.timeScale = 1f;
            isPaused = false;
            if (gameFlow != null) gameFlow.SetPaused(false);
            action?.Invoke();
        }

        private void InvokeFocusedResultAction()
        {
            if (showNextLevelButton && focusedButton == 0) RunNavigation(nextLevelAction);
            else if (focusedButton == (showNextLevelButton ? 1 : 0)) RunNavigation(playAgainAction);
            else RunNavigation(mainMenuAction);
        }

        private void PlayButtonClick()
        {
            if (audio != null) audio.Play(GameSound.ButtonClick);
        }

        public void ShowBark(bool visible)
        {
            if (barkCounter != null) barkCounter.gameObject.SetActive(visible);
        }

        public void RefreshBarkState(float cooldownRemaining)
        {
            if (barkCounter == null) return;
            barkCounter.text = cooldownRemaining <= 0f ? "Bark: Ready" : "Bark: " + cooldownRemaining.ToString("0.0") + "s";
        }

        public void RefreshDefenseSafe(int socksSafe)
        {
            counter.text = "Socks safe: " + socksSafe;
        }

        public void ShowDefenseCarry(bool carrying)
        {
            carryingCounter.transform.position = new Vector3(-15.58f, 6.1f, -.8f);
            carryingCounter.text = "Carrying: 1/1";
            carryingCounter.gameObject.SetActive(carrying);
        }

        public void RefreshTrapCount(int placed, int maximum)
        {
            trapCounter.text = "Traps: " + placed + " / " + maximum;
        }

        public void ShowTrapPrompt(bool visible, Vector3 playerPosition)
        {
            if (trapPromptCard.activeSelf != visible) trapPromptCard.SetActive(visible);
            if (trapPrompt.gameObject.activeSelf != visible) trapPrompt.gameObject.SetActive(visible);
            if (!visible) return;
            Vector3 promptPosition = playerPosition + new Vector3(0f, 1.3f, -.25f);
            trapPromptCard.transform.position = promptPosition;
            trapPrompt.transform.position = promptPosition;
        }

        private TextMesh Label(string name,Vector3 position,string value,int fontSize,TextAnchor anchor,int order,Transform parent=null)
        {
            var go=new GameObject(name); if(parent!=null)go.transform.SetParent(parent,false); go.transform.position=position;
            var tm=go.AddComponent<TextMesh>(); tm.text=value; tm.fontSize=fontSize; tm.characterSize=.11f; tm.anchor=anchor; tm.alignment=anchor==TextAnchor.MiddleCenter?TextAlignment.Center:TextAlignment.Left;
            tm.color=new Color32(255,246,225,255); go.GetComponent<MeshRenderer>().sortingOrder=order; return tm;
        }
        private GameObject Sprite(string name,Vector3 position,Sprite sprite,Vector2 size,int order,Transform parent=null)
        {
            var go=new GameObject(name); if(parent!=null)go.transform.SetParent(parent,false); go.transform.position=position;
            go.transform.localScale=new Vector3(size.x/(sprite.texture.width/16f),size.y/(sprite.texture.height/16f),1);
            var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=sprite; sr.sortingOrder=order; return go;
        }

        private void EnsureButtonStyles()
        {
            if (resultButtonStyle == null)
            {
                normalButtonTexture = ButtonTexture(new Color32(91, 61, 54, 248));
                hoverButtonTexture = ButtonTexture(new Color32(145, 96, 63, 255));
                pressedButtonTexture = ButtonTexture(new Color32(67, 46, 48, 255));
                resultButtonStyle = new GUIStyle(GUI.skin.button);
                resultButtonStyle.fontStyle = FontStyle.Bold;
                resultButtonStyle.alignment = TextAnchor.MiddleCenter;
                resultButtonStyle.border = new RectOffset(7, 7, 7, 7);
                resultButtonStyle.normal.background = normalButtonTexture;
                resultButtonStyle.hover.background = hoverButtonTexture;
                resultButtonStyle.active.background = pressedButtonTexture;
                resultButtonStyle.focused.background = hoverButtonTexture;
                resultButtonStyle.normal.textColor = new Color32(255, 239, 202, 255);
                resultButtonStyle.hover.textColor = new Color32(255, 247, 223, 255);
                resultButtonStyle.active.textColor = new Color32(255, 239, 202, 255);
                resultButtonStyle.focused.textColor = new Color32(255, 247, 223, 255);
                focusedButtonStyle = new GUIStyle(resultButtonStyle);
                focusedButtonStyle.normal.background = hoverButtonTexture;
            }

            if (pausePanelStyle == null)
            {
                pausePanelStyle = new GUIStyle(GUI.skin.box);
                pausePanelTexture = PanelTexture();
                pausePanelStyle.normal.background = pausePanelTexture;
                pausePanelStyle.border = new RectOffset(8, 8, 8, 8);
                pauseTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 38,
                    normal = { textColor = new Color32(255, 239, 202, 255) }
                };
            }
        }

        private void OnDestroy()
        {
            if (normalButtonTexture != null) Destroy(normalButtonTexture);
            if (hoverButtonTexture != null) Destroy(hoverButtonTexture);
            if (pressedButtonTexture != null) Destroy(pressedButtonTexture);
            if (pausePanelTexture != null) Destroy(pausePanelTexture);
            if (hudPanelSprite != null) Destroy(hudPanelSprite);
            if (hudPanelTexture != null) Destroy(hudPanelTexture);
        }

        private static Texture2D CreateHudPanelTexture()
        {
            const int width = 96, height = 48;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[width * height];
            var outline = new Color32(45, 32, 38, 238);
            var rim = new Color32(133, 89, 63, 235);
            var fill = new Color32(38, 32, 39, 224);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool edge = x < 2 || x >= width - 2 || y < 2 || y >= height - 2;
                    bool innerEdge = x < 5 || x >= width - 5 || y < 5 || y >= height - 5;
                    pixels[y * width + x] = edge ? outline : innerEdge ? rim : fill;
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D PanelTexture()
        {
            const int size = 24;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[size * size];
            var outline = new Color32(45, 32, 38, 238);
            var rim = new Color32(133, 89, 63, 235);
            var fill = new Color32(38, 32, 39, 224);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < 2 || x >= size - 2 || y < 2 || y >= size - 2;
                    bool innerEdge = x < 4 || x >= size - 4 || y < 4 || y >= size - 4;
                    pixels[y * size + x] = edge ? outline : innerEdge ? rim : fill;
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D ButtonTexture(Color32 fill)
        {
            const int width = 96, height = 32;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[width * height];
            var outline = new Color32(61, 40, 33, 255);
            var rim = new Color32(151, 92, 57, 255);
            var shine = new Color32(255, 230, 176, 255);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool corner = (x < 3 || x >= width - 3) && (y < 3 || y >= height - 3);
                    Color32 color = corner ? new Color32(0, 0, 0, 0)
                        : x < 2 || x >= width - 2 || y < 2 || y >= height - 2 ? outline
                        : x < 5 || x >= width - 5 || y < 5 || y >= height - 5 ? rim : fill;
                    if (y == height - 7 && x >= 9 && x < width - 9) color = shine;
                    pixels[y * width + x] = color;
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
