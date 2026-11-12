using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BulkoGame.GameFlow;
using BulkoGame.UI;
using BulkoGame.Audio;

namespace BulkoGame.Defense
{
    /// <summary>One small authored defense wave for Level 01.</summary>
    public sealed class DefenseWaveController : MonoBehaviour
    {
        private GameBalanceConfig balance;
        private GameHudPresenter presenter;
        private readonly List<CatController> activeCats = new List<CatController>();
        private readonly List<CatRoute> routeSequence = new List<CatRoute>();
        private GameFlow.GameFlow flow;
        private GameObject catPrefab;
        private Sprite sockSprite;
        private TrapPlacementManager traps;
        private BulkaDefenseAbility bulkaAbility;
        private GameAudio audio;
        private int spawned;
        private bool running, resultShown, initialized, spawningFinished;

        public void Initialize(IReadOnlyList<CatRoute> orderedRouteSequence, GameFlow.GameFlow gameFlow,
            GameHudPresenter ui, GameBalanceConfig config, GameObject reusableCatPrefab, Sprite carriedSockSprite, TrapPlacementManager trapManager, BulkaDefenseAbility defenseAbility, GameAudio gameAudio)
        {
            routeSequence.Clear();
            if (orderedRouteSequence != null)
                for (int i = 0; i < orderedRouteSequence.Count; i++) routeSequence.Add(orderedRouteSequence[i]);
            flow = gameFlow;
            presenter = ui; balance = config;
            catPrefab = reusableCatPrefab; sockSprite = carriedSockSprite; traps = trapManager; bulkaAbility = defenseAbility;
            audio = gameAudio;
            initialized = catPrefab != null && HasValidRouteSequence();
            if (!initialized)
                Debug.LogError($"Defense wave initialization failed: route sequence must contain at least {((balance != null) ? balance.waveSize : 0)} valid CatRoutes.");
        }

        public void BeginWave()
        {
            if (running || resultShown || !initialized) return;
            running = true;
            spawned = 0;
            spawningFinished = false;
            activeCats.Clear();
            presenter.WaveStart();
            StartCoroutine(SpawnWave());
        }

        private IEnumerator SpawnWave()
        {
            while (spawned < balance.waveSize && running)
            {
                while (Time.timeScale <= 0f && running) yield return null;
                while (activeCats.Count >= balance.maxSimultaneousCats && running) yield return null;
                if (!running) yield break;
                if (Time.timeScale <= 0f) continue;
                SpawnCat(routeSequence[spawned]);
                spawned++;
                presenter.Cats();
                if (spawned < balance.waveSize) yield return new WaitForSeconds(balance.spawnInterval);
            }
            spawningFinished = true;
        }

        private void SpawnCat(CatRoute route)
        {
            if (audio != null) audio.Play(GameSound.CatEntrance);
            Vector2 start = route.Entrance;
            var cat = Instantiate(catPrefab, new Vector3(start.x, start.y, .72f), Quaternion.identity, transform.parent);
            cat.name = "Orange Cat - Entrance " + route.RouteId;
            var visual = cat.transform.Find("Orange Cat Visual");
            var renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            var animator = visual != null ? visual.GetComponent<Animator>() : null;
            var controller = cat.GetComponent<CatController>();
            if (visual == null || renderer == null || animator == null || controller == null)
            {
                Debug.LogError("OrangeCat prefab is missing its visual Animator or CatController.");
                Destroy(cat);
                return;
            }
            renderer.sortingOrder = -Mathf.RoundToInt(start.y * 20f) + 1;
            animator.applyRootMotion = false;
            controller.Finished += OnCatFinished;
            controller.Begin(route, flow, visual, sockSprite, balance, audio);
            activeCats.Add(controller);
        }

        private void Update()
        {
            if (!running || resultShown || Time.timeScale <= 0f) return;
            activeCats.RemoveAll(cat => cat == null);

            if (spawningFinished && flow != null && flow.Session != null && flow.Session.Phase == GamePhase.Defense &&
                flow.SocksSafe <= 0 &&
                !flow.Session.IsCarryingDefenseSock &&
                flow.Session.UnresolvedDefenseSocks <= 0 &&
                !activeCats.Exists(cat => cat != null && cat.CanStillDropCarriedSock))
            {
                FinishWave(false);
                return;
            }

            if (spawningFinished && activeCats.Count == 0 && flow.Session.UnresolvedDefenseSocks == 0 && !flow.Session.IsCarryingDefenseSock)
                FinishWave(flow.SocksSafe > 0);
        }

        private void OnCatFinished(CatController cat)
        {
            activeCats.Remove(cat);
            flow.Session.FinishCat();
            presenter.Cats();
        }

        private void FinishWave(bool won)
        {
            if (resultShown) return;
            resultShown = true;
            running = false;
            StopAllCoroutines();
            foreach (var cat in activeCats) if (cat != null) cat.StopBehavior();
            activeCats.Clear();
            if (traps != null) traps.EndDefense();
            if (bulkaAbility != null) bulkaAbility.EndDefense();
            flow.FinishDefense(won);
        }

        private bool HasValidRouteSequence()
        {
            if (balance == null || routeSequence.Count < balance.waveSize) return false;
            for (int i = 0; i < balance.waveSize; i++)
                if (routeSequence[i] == null || routeSequence[i].Count < 2 || string.IsNullOrWhiteSpace(routeSequence[i].RouteId))
                    return false;
            return true;
        }
    }
}
