using System.Collections;
using UnityEngine;
using BulkoGame.UI;
using BulkoGame.Player;
using BulkoGame.Audio;

namespace BulkoGame.Defense
{
    /// <summary>Defense-only Bark ability. Owns its input, cooldown, and temporary feedback.</summary>
    public sealed class BulkaDefenseAbility : MonoBehaviour
    {
        [SerializeField, Range(2f, 3f)] private float radius = 2.5f;
        [SerializeField] private float cooldown = 4f;
        [SerializeField, Range(1f, 1.5f)] private float fearDuration = 1.25f;
        [SerializeField] private float retreatDistance = 1.15f;
        [SerializeField] private float retreatSpeed = 2.2f;

        private Transform bulka;
        private BulkaVisualState bulkaVisual;
        private GameAudio audio;
        private GameHUD hud;
        private BulkoGame.GameFlow.GameSessionState session;
        private float cooldownRemaining;
        private int barkActivationId;
        private bool defenseActive;
        private SpriteRenderer pulseRenderer;
        private TextMesh woofLabel;
        private TextMesh woofShadow;
        private static Sprite pulseSprite;

        public float Radius => radius;
        public float Cooldown => cooldown;
        public float FearDuration => fearDuration;
        public float CooldownRemaining => cooldownRemaining;

        public void Initialize(Transform player, GameHUD gameHud, BulkoGame.GameFlow.GameBalanceConfig balance, GameAudio gameAudio, BulkoGame.GameFlow.GameSessionState gameSession)
        {
            bulka = player;
            bulkaVisual = player.GetComponent<BulkaVisualState>();
            audio = gameAudio;
            hud = gameHud;
            session = gameSession;
            radius = balance.barkRadius;
            cooldown = balance.barkCooldown;
            fearDuration = balance.fearDuration;
            if (pulseSprite == null) pulseSprite = CreatePixelRing();
        }

        public void BeginDefense()
        {
            defenseActive = true;
            cooldownRemaining = 0f;
            barkActivationId = 0;
            hud.ShowBark(true);
            RefreshHud();
        }

        public void EndDefense()
        {
            defenseActive = false;
            cooldownRemaining = 0f;
            if (hud != null) hud.ShowBark(false);
            if (pulseRenderer != null) pulseRenderer.gameObject.SetActive(false);
            if (woofLabel != null) woofLabel.gameObject.SetActive(false);
            if (woofShadow != null) woofShadow.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!defenseActive || Time.timeScale <= 0f) return;
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
                RefreshHud();
            }
            if (Input.GetKeyDown(KeyCode.Space)) TryBark();
        }

        public bool TryBark()
        {
            if (!defenseActive || Time.timeScale <= 0f || bulka == null || cooldownRemaining > 0f || (session != null && session.IsCarryingDefenseSock)) return false;
            cooldownRemaining = cooldown;
            barkActivationId++;
            if (barkActivationId == 0) barkActivationId = 1;
            if (audio != null) audio.Play(GameSound.BulkaBark);
            if (bulkaVisual != null) bulkaVisual.PlayBark();
            var cats = FindObjectsByType<CatController>();
            Vector2 origin = bulka.position;
            float radiusSquared = radius * radius;
            bool fearSoundPlayed = false;
            foreach (var cat in cats)
            {
                if (cat == null || cat.AtExit) continue;
                if (((Vector2)cat.transform.position - origin).sqrMagnitude > radiusSquared) continue;
                if (cat.ApplyBarkHit(barkActivationId, origin, fearDuration, retreatDistance, retreatSpeed) && !fearSoundPlayed)
                {
                    if (audio != null) audio.Play(GameSound.CatFear);
                    fearSoundPlayed = true;
                }
            }
            PlayFeedback(origin);
            RefreshHud();
            return true;
        }

        private void RefreshHud()
        {
            if (hud == null) return;
            hud.RefreshBarkState(cooldownRemaining);
        }

        private void PlayFeedback(Vector2 origin)
        {
            if (pulseRenderer == null)
            {
                var ring = new GameObject("Bark Wave");
                pulseRenderer = ring.AddComponent<SpriteRenderer>();
                pulseRenderer.sprite = pulseSprite;
            }
            pulseRenderer.transform.position = new Vector3(origin.x, origin.y, .02f);
            pulseRenderer.transform.localScale = Vector3.one * .7f;
            pulseRenderer.sortingOrder = -Mathf.RoundToInt(origin.y * 20f) + 8;
            pulseRenderer.color = Color.white;
            pulseRenderer.gameObject.SetActive(true);
            StopCoroutine(nameof(AnimateFeedback));
            StartCoroutine(AnimateFeedback());

            if (woofLabel == null)
            {
                var label = new GameObject("Bark WOOF Label");
                woofLabel = label.AddComponent<TextMesh>();
                woofLabel.text = "WOOF!";
                woofLabel.fontSize = 25;
                woofLabel.characterSize = .095f;
                woofLabel.anchor = TextAnchor.MiddleCenter;
                woofLabel.alignment = TextAlignment.Center;
                woofLabel.color = new Color32(255, 226, 162, 255);
                label.GetComponent<MeshRenderer>().sortingOrder = 550;
                var shadow = new GameObject("Bark WOOF Outline");
                woofShadow = shadow.AddComponent<TextMesh>();
                woofShadow.text = "WOOF!";
                woofShadow.fontSize = 25;
                woofShadow.characterSize = .095f;
                woofShadow.anchor = TextAnchor.MiddleCenter;
                woofShadow.alignment = TextAlignment.Center;
                woofShadow.color = new Color32(71, 44, 36, 255);
                shadow.GetComponent<MeshRenderer>().sortingOrder = 549;
            }
            woofLabel.transform.position = new Vector3(origin.x, origin.y + .88f, -.2f);
            woofShadow.transform.position = woofLabel.transform.position + new Vector3(.045f, -.045f, .01f);
            woofLabel.gameObject.SetActive(true);
            woofShadow.gameObject.SetActive(true);
            StopCoroutine(nameof(HideWoof));
            StartCoroutine(HideWoof());
        }

        private IEnumerator AnimateFeedback()
        {
            const float duration = .34f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                pulseRenderer.transform.localScale = Vector3.one * Mathf.Lerp(.55f, 1.04f, t);
                Color color = pulseRenderer.color;
                color.a = Mathf.Lerp(.9f, 0f, t);
                pulseRenderer.color = color;
                yield return null;
            }
            pulseRenderer.gameObject.SetActive(false);
        }

        private IEnumerator HideWoof()
        {
            yield return new WaitForSeconds(.52f);
            if (woofLabel != null) woofLabel.gameObject.SetActive(false);
            if (woofShadow != null) woofShadow.gameObject.SetActive(false);
        }

        private static Sprite CreatePixelRing()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Pixel Bark Shockwave";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[size * size];
            float center = (size - 1) * .5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float waviness = Mathf.Sin(angle * 12f) * .75f;
                    bool outerBand = Mathf.Abs(distance - (26f + waviness)) < 1.5f;
                    bool innerBand = Mathf.Abs(distance - (19f + waviness * .6f)) < .9f;
                    bool stipple = ((x * 3 + y * 5) & 7) < 5;
                    if (outerBand && stipple)
                        pixels[y * size + x] = new Color32(255, 231, 165, 205);
                    else if (innerBand && ((x + y) & 3) != 0)
                        pixels[y * size + x] = new Color32(255, 245, 208, 125);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 16f);
        }
    }
}
