using System;
using UnityEngine;
using BulkoGame.GameFlow;
using BulkoGame.Audio;

namespace BulkoGame.Defense
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(CapsuleCollider2D))]
    public sealed class CatController : MonoBehaviour
    {
        private enum CatState { ApproachingBasket, ReturningToEntrance, Stunned, RecoveringDroppedSock, PermanentlyFleeing }

        [SerializeField] private float moveSpeed = 2.4f;
        private Rigidbody2D body;
        private CircleCollider2D triggerCollider;
        private CapsuleCollider2D bodyCollider;
        private CatRoute route;
        private int waypoint;
        private CatState state;
        private GameFlow.GameFlow flow;
        private GameAudio audio;
        private SpriteRenderer bodyRenderer;
        private Animator visualAnimator;
        private string currentVisualState;
        private int facing;
        private Transform visual;
        private Transform carriedSock;
        private bool atExit;
        private float exitPause;
        private float slowRemaining;
        private float speedMultiplier = 1f;
        private float fearRemaining;
        private float fearRetreatRemaining;
        private float fearRetreatSpeed;
        private Vector2 fearSource;
        private bool behaviorStopped;
        private bool isPermanentlyFleeing;
        private int damage;
        private int lastBarkActivationId = int.MinValue;
        private float stunDuration;
        private DefenseDroppedSock droppedSock;
        private Sprite sockSprite;

        private const float StuckWindow = 1.4f;
        private const float StuckDistance = .08f;
        private const float MaxRecoveryDistance = .6f;
        private Vector2 progressPosition;
        private float progressTime;
        private bool retryingRoute;
        private readonly Collider2D[] recoveryOverlaps = new Collider2D[32];
        private readonly RaycastHit2D[] recoveryHits = new RaycastHit2D[32];

        public event Action<CatController> Finished;

        public bool HasReachedBasket { get; private set; }
        public bool IsCarryingSock { get; private set; }
        public bool IsSlowed => slowRemaining > 0f;
        public bool IsFeared => isPermanentlyFleeing || state == CatState.Stunned || fearRemaining > 0f;
        public bool AtExit => atExit;
        public int Damage => damage;
        public bool IsPermanentlyFleeing => isPermanentlyFleeing;
        public bool CanStillDropCarriedSock => IsCarryingSock && !isPermanentlyFleeing && !behaviorStopped && !atExit && state != CatState.Stunned;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            triggerCollider = GetComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = .48f;
            bodyCollider = GetComponent<CapsuleCollider2D>();
            bodyCollider.isTrigger = false;
            bodyCollider.direction = CapsuleDirection2D.Horizontal;
            bodyCollider.size = new Vector2(.78f, .5f);
            bodyCollider.offset = new Vector2(0f, -.1f);
            IgnoreOtherCatBodyCollisions();
            IgnoreCatShortcutCollisions();
        }

        private void IgnoreOtherCatBodyCollisions()
        {
            var cats = FindObjectsByType<CatController>();
            foreach (var other in cats)
            {
                if (other == null || other == this) continue;
                var otherBodyCollider = other.bodyCollider != null
                    ? other.bodyCollider
                    : other.GetComponent<CapsuleCollider2D>();
                if (otherBodyCollider != null)
                    Physics2D.IgnoreCollision(bodyCollider, otherBodyCollider, true);
            }
        }

        private void IgnoreCatShortcutCollisions()
        {
            var shortcuts = FindObjectsByType<CatShortcutBlocker>(FindObjectsInactive.Exclude);
            foreach (var shortcut in shortcuts)
            {
                if (shortcut == null || shortcut.BlockerCollider == null) continue;
                Physics2D.IgnoreCollision(bodyCollider, shortcut.BlockerCollider, true);
            }
        }

        public void Begin(CatRoute catRoute, GameFlow.GameFlow gameFlow, Transform catVisual, Sprite sockSprite, GameBalanceConfig balance, GameAudio gameAudio)
        {
            moveSpeed = balance.catSpeed;
            stunDuration = balance.defenseSockStunDuration;
            this.sockSprite = sockSprite;
            route = catRoute;
            flow = gameFlow;
            audio = gameAudio;
            visual = catVisual;
            bodyRenderer = visual.GetComponent<SpriteRenderer>();
            visualAnimator = visual.GetComponent<Animator>();
            body.position = route.Entrance;
            state = CatState.ApproachingBasket;
            waypoint = 1;
            ResetProgress();
            bodyRenderer.sortingOrder = OrderAt(body.position.y) + 1;
            if (visualAnimator == null) CreateCarriedSock(sockSprite);
            RefreshVisualState();
        }

        private void Update()
        {
            if (slowRemaining > 0f)
            {
                slowRemaining = Mathf.Max(0f, slowRemaining - Time.deltaTime);
                if (slowRemaining <= 0f) speedMultiplier = 1f;
            }

            if (state == CatState.Stunned)
            {
                stunRemaining -= Time.deltaTime;
                if (stunRemaining <= 0f)
                {
                    if (droppedSock != null) state = CatState.RecoveringDroppedSock;
                    else EnterPermanentFlee();
                }
            }
            if (fearRemaining > 0f)
            {
                fearRemaining = Mathf.Max(0f, fearRemaining - Time.deltaTime);
                if (fearRemaining <= 0f) fearRetreatRemaining = 0f;
            }

            UpdateAppearance();
            RefreshVisualState();
        }

        private void FixedUpdate()
        {
            if (behaviorStopped || route == null || route.Count < 2) return;
            if (state == CatState.Stunned) { ResetProgress(); body.linearVelocity = Vector2.zero; return; }
            if (state == CatState.RecoveringDroppedSock)
            {
                ResetProgress();
                if (droppedSock == null) { EnterPermanentFlee(); return; }
                Vector2 deltaSock = (Vector2)droppedSock.transform.position - body.position;
                if (fearRemaining > 0f) { MoveDuringFear(); return; }
                body.MovePosition(Vector2.MoveTowards(body.position, (Vector2)droppedSock.transform.position, moveSpeed * speedMultiplier * Time.fixedDeltaTime));
                if (deltaSock.sqrMagnitude > .0001f) Face(deltaSock);
                if (deltaSock.sqrMagnitude < .2f * .2f) droppedSock.TryReclaim(this);
                return;
            }
            if (fearRemaining > 0f)
            {
                ResetProgress();
                MoveDuringFear();
                if (carriedSock != null) PositionSock();
                return;
            }

            CheckRouteProgress();
            Vector2 destination = state == CatState.ApproachingBasket
                ? route.Point(Mathf.Min(waypoint, route.Count - 1))
                : waypoint >= 0 ? route.Point(waypoint) : route.Entrance;
            Vector2 current = body.position;
            Vector2 delta = destination - current;
            Vector2 next = Vector2.MoveTowards(current, destination,
                moveSpeed * speedMultiplier * Time.fixedDeltaTime);
            if (delta.sqrMagnitude > .0001f)
            {
                body.MovePosition(next);
                Face(delta);
            }

            if (carriedSock != null) PositionSock();
            if ((destination - next).sqrMagnitude > .0025f) return;

            if (state == CatState.ApproachingBasket)
            {
                if (waypoint < route.Count - 1) { waypoint++; return; }
                HasReachedBasket = true;
                bool stole = flow != null && flow.TryStealSock();
                IsCarryingSock = stole;
                if (stole && audio != null) audio.Play(GameSound.SockStolen);
                if (IsCarryingSock && carriedSock != null) carriedSock.gameObject.SetActive(true);
                RefreshVisualState();
                state = CatState.ReturningToEntrance;
                waypoint = route.Count - 2;
                if (waypoint < 0) waypoint = -1;
            }
            else
            {
                if (waypoint >= 0) { waypoint--; return; }
                atExit = true;
                exitPause += Time.fixedDeltaTime;
                if (exitPause >= .35f)
                {
                    behaviorStopped = true;
                    Finished?.Invoke(this);
                    // CatExit is intentionally an optional prepared slot and is not played per cat by default.
                    Destroy(gameObject);
                }
            }
        }

        private void ResetProgress()
        {
            if (body != null) progressPosition = body.position;
            progressTime = 0f;
            retryingRoute = false;
        }

        private void CheckRouteProgress()
        {
            if (atExit || behaviorStopped || route == null || route.Count < 2)
            {
                ResetProgress();
                return;
            }
            progressTime += Time.fixedDeltaTime;
            if (progressTime < StuckWindow) return;
            if ((body.position - progressPosition).sqrMagnitude >= StuckDistance * StuckDistance)
            {
                ResetProgress();
                return;
            }
            progressPosition = body.position;
            progressTime = 0f;
            // Do not interfere with intentional shortcut crossing or nearby collision filtering.
            int nearby = Physics2D.OverlapCircle(body.position, 1f, ContactFilter2D.noFilter, recoveryOverlaps);
            for (int i = 0; i < nearby; i++)
                if (recoveryOverlaps[i] != null && recoveryOverlaps[i].GetComponentInParent<CatShortcutBlocker>() != null)
                {
                    retryingRoute = false;
                    return;
                }
            if (!retryingRoute)
            {
                waypoint = Mathf.Clamp(waypoint, state == CatState.ApproachingBasket ? 1 : -1, route.Count - 1);
                body.linearVelocity = Vector2.zero;
                retryingRoute = true;
                return;
            }
            TryRecoverToRoute();
            ResetProgress();
        }

        private void TryRecoverToRoute()
        {
            int target = Mathf.Clamp(waypoint, 0, route.Count - 1);
            int previous = state == CatState.ApproachingBasket ? Mathf.Max(0, target - 1) : Mathf.Min(route.Count - 1, target + 1);
            Vector2 a = route.Point(previous), b = route.Point(target), segment = b - a;
            float t = segment.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector2.Dot(body.position - a, segment) / segment.sqrMagnitude) : 0f;
            Vector2 candidate = a + segment * t;
            Vector2 move = candidate - body.position;
            float distance = move.magnitude;
            if (distance < .03f || distance > MaxRecoveryDistance) return;
            var filter = new ContactFilter2D();
            filter = ContactFilter2D.noFilter;
            filter.useTriggers = false;
            Vector2 scale = transform.lossyScale;
            Vector2 size = Vector2.Scale(bodyCollider.size, new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            Vector2 offset = Vector2.Scale(bodyCollider.offset, scale);
            int overlaps = Physics2D.OverlapCapsule(candidate + offset, size, bodyCollider.direction, 0f, filter, recoveryOverlaps);
            if (overlaps == recoveryOverlaps.Length) return;
            for (int i = 0; i < overlaps; i++)
                if (BlocksRecovery(recoveryOverlaps[i])) return;
            int hits = bodyCollider.Cast(move / distance, filter, recoveryHits, distance);
            if (hits == recoveryHits.Length) return;
            for (int i = 0; i < hits; i++)
                if (BlocksRecovery(recoveryHits[i].collider)) return;
            body.linearVelocity = Vector2.zero;
            body.position = candidate;
        }

        private bool BlocksRecovery(Collider2D other) => other != null && other != bodyCollider &&
            other.attachedRigidbody != body && !Physics2D.GetIgnoreCollision(bodyCollider, other);

        private void LateUpdate()
        {
            if (bodyRenderer == null || body == null) return;
            bodyRenderer.sortingOrder = OrderAt(body.position.y) + 1;
            if (carriedSock != null)
                carriedSock.GetComponent<SpriteRenderer>().sortingOrder = bodyRenderer.sortingOrder + 1;
        }

        public void StopBehavior()
        {
            behaviorStopped = true;
            if (body != null) body.position = body.position;
        }

        public void ApplySlow(float multiplier, float duration)
        {
            if (isPermanentlyFleeing || behaviorStopped || atExit || state == CatState.Stunned || duration <= 0f || multiplier <= 0f) return;
            slowRemaining = Mathf.Max(slowRemaining, duration);
            speedMultiplier = Mathf.Min(speedMultiplier, multiplier);
            UpdateAppearance();
        }

        public void RegisterTrapTrigger()
        {
            RegisterDamage();
        }

        public bool ApplyBarkHit(int activationId, Vector2 sourcePosition, float duration, float retreatDistance, float retreatSpeed)
        {
            if (isPermanentlyFleeing || behaviorStopped || atExit || state == CatState.Stunned || duration <= 0f) return false;
            if (lastBarkActivationId == activationId) return false;
            lastBarkActivationId = activationId;
            if (RegisterDamage())
            {
                ShowFearMark();
                return true;
            }
            fearSource = sourcePosition;
            fearRemaining = Mathf.Max(fearRemaining, duration);
            fearRetreatRemaining = Mathf.Max(fearRetreatRemaining, Mathf.Max(0f, retreatDistance));
            fearRetreatSpeed = Mathf.Max(0f, retreatSpeed);
            UpdateAppearance();
            ShowFearMark();
            return true;
        }

        private bool RegisterDamage()
        {
            if (isPermanentlyFleeing || behaviorStopped || atExit || state == CatState.Stunned) return false;
            damage++;
            if (state == CatState.RecoveringDroppedSock) return false;
            if (damage < 2) return false;
            if (IsCarryingSock) DropCarriedSock(); else EnterPermanentFlee();
            return true;
        }

        private float stunRemaining;
        private void DropCarriedSock()
        {
            IsCarryingSock = false;
            body.linearVelocity = Vector2.zero;
            if (carriedSock != null) carriedSock.gameObject.SetActive(false);
            var drop = new GameObject("Dropped Defense Sock");
            drop.transform.position = new Vector3(body.position.x, body.position.y, .55f);
            droppedSock = drop.AddComponent<DefenseDroppedSock>();
            droppedSock.Initialize(this, flow, sockSprite);
            flow.NotifyDefenseSockDropped();
            fearRemaining = fearRetreatRemaining = 0f;
            stunRemaining = stunDuration;
            state = CatState.Stunned;
        }

        public bool AcceptDroppedSock(DefenseDroppedSock sock)
        {
            if (sock != droppedSock || state != CatState.RecoveringDroppedSock) return false;
            droppedSock = null; IsCarryingSock = true; damage = 0;
            if (carriedSock != null) carriedSock.gameObject.SetActive(true);
            state = CatState.ReturningToEntrance;
            RefreshVisualState(); return true;
        }

        public void OnDroppedSockRecoveredByBulka()
        {
            droppedSock = null; EnterPermanentFlee();
        }

        private void EnterPermanentFlee()
        {
            if (isPermanentlyFleeing) return;
            isPermanentlyFleeing = true;
            fearRemaining = 0f;
            fearRetreatRemaining = 0f;
            state = CatState.PermanentlyFleeing;
            if (waypoint < 0) waypoint = 0;
            else if (HasReachedBasket) waypoint = Mathf.Min(waypoint, route.Count - 2);
            else waypoint = Mathf.Max(0, waypoint - 1);
            UpdateAppearance();
            RefreshVisualState();
        }

        private void MoveDuringFear()
        {
            if (fearRetreatRemaining <= 0f || fearRetreatSpeed <= 0f) return;
            Vector2 current = body.position;
            Vector2 retreatTarget;
            if (state == CatState.ApproachingBasket)
            {
                retreatTarget = route.Point(Mathf.Max(0, waypoint - 1));
            }
            else
            {
                int previousOnRoute = waypoint < 0 ? 1 : Mathf.Min(waypoint + 1, route.Count - 1);
                retreatTarget = route.Point(previousOnRoute);
            }

            Vector2 direction = retreatTarget - current;
            if (direction.sqrMagnitude < .0001f)
                direction = (current - fearSource).sqrMagnitude > .0001f ? current - fearSource : Vector2.left;
            direction.Normalize();
            float step = Mathf.Min(fearRetreatSpeed * speedMultiplier * Time.fixedDeltaTime, fearRetreatRemaining);
            body.MovePosition(current + direction * step);
            fearRetreatRemaining = Mathf.Max(0f, fearRetreatRemaining - step);
            Face(direction);
        }

        private void UpdateAppearance()
        {
            if (bodyRenderer == null) return;
            bodyRenderer.color = IsFeared ? new Color32(255, 205, 131, 255)
                : IsSlowed ? new Color32(255, 225, 154, 255) : Color.white;
        }

        private void ShowFearMark()
        {
            var mark = new GameObject("Bark Fear Mark");
            mark.transform.SetParent(transform, false);
            mark.transform.localPosition = new Vector3(.12f, .78f, -.12f);
            var text = mark.AddComponent<TextMesh>();
            text.text = "!";
            text.fontSize = 30;
            text.characterSize = .12f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = new Color32(255, 221, 141, 255);
            mark.GetComponent<MeshRenderer>().sortingOrder = bodyRenderer != null ? bodyRenderer.sortingOrder + 3 : 10;
            Destroy(mark, Mathf.Max(.15f, fearRemaining));
        }

        private void CreateCarriedSock(Sprite sockSprite)
        {
            if (sockSprite == null) return;
            var sock = new GameObject("Stolen Sock");
            sock.transform.SetParent(visual, false);
            sock.transform.localScale = new Vector3(.42f, .42f, 1f);
            var renderer = sock.AddComponent<SpriteRenderer>();
            renderer.sprite = sockSprite;
            renderer.sortingOrder = 1;
            sock.SetActive(false);
            carriedSock = sock.transform;
        }

        private void PositionSock()
        {
            carriedSock.localPosition = new Vector3(.48f, .22f, -.02f);
        }

        private void Face(Vector2 direction)
        {
            facing = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                ? (direction.x < 0f ? 2 : 3) : (direction.y > 0f ? 1 : 0);
            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            if (visualAnimator == null) return;
            string mode = IsFeared ? "Fear" : IsCarryingSock ? "CarryingSock" : "Walk";
            string[] directions = { "Down", "Up", "Left", "Right" };
            string next = "OrangeCat_" + mode + "_" + directions[facing];
            if (currentVisualState == next) return;
            currentVisualState = next;
            visualAnimator.Play(next, 0, 0f);
        }

        private static int OrderAt(float y) => -Mathf.RoundToInt(y * 20f);
    }
}
