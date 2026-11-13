using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using BulkoGame.Art;
using BulkoGame.Player;
using BulkoGame.Collectibles;
using BulkoGame.UI;
using BulkoGame.Defense;
using BulkoGame.Audio;
using BulkoGame.Levels;

namespace BulkoGame.GameFlow
{
    public sealed class LevelBootstrap : MonoBehaviour
    {
        public LevelDefinition definition;
        public LevelSceneBindings sceneBindings;

        public LevelDefinition Definition => definition;
        public LevelSceneBindings SceneBindings => sceneBindings;

        private DefenseWaveController waveController;
        private TrapPlacementManager trapPlacementManager;
        private BulkaDefenseAbility bulkaDefenseAbility;
        private GameAudio audio;

        private void Awake()
        {
            Time.timeScale = 1f;
        }

        private void Start()
        {
            if (!ValidateRequiredSetup(out List<CatRoute> routeSequence)) return;

            ApartmentArtSet art = definition.art;
            EnvironmentArtSet environmentArt = definition.environmentArt;
            GameBalanceConfig balance = definition.balance;

            Physics2D.SyncTransforms();
            var sockSpawner = GetComponent<SockSpawnController>();
            if (sockSpawner == null) sockSpawner = gameObject.AddComponent<SockSpawnController>();
            sockSpawner.Initialize(definition, sceneBindings.sockSpawnPoints, art.sockPrefab, art.sockPink, new[]
            {
                environmentArt.sockCreamStripe, environmentArt.sockGreen,
                environmentArt.sockLeaf, environmentArt.sockOrangeDots,
                environmentArt.sockGreenCheck, environmentArt.sockHeart
            });
            var socks = sockSpawner.SpawnSocks();
            if (socks == null) return;
            if (socks.Count != definition.sockCount)
            {
                Debug.LogError($"Level '{definition.levelId}' spawned {socks.Count} socks; expected {definition.sockCount}. Check its authored points and colliders.", this);
                return;
            }

            audio = GameAudio.EnsureInstance(definition.audio);
            if (audio != null) audio.PlayGameplayMusic();
            foreach (var zone in sceneBindings.trapZones)
                zone.Initialize(art.trapZoneMarker);

            var dog = Instantiate(art.bulkaPrefab, sceneBindings.playerSpawn.position,
                sceneBindings.playerSpawn.rotation, transform);
            dog.name = "Bulka";
            var playerRenderer = dog.GetComponentInChildren<SpriteRenderer>();
            foreach (var occluder in FindObjectsByType<ForegroundOcclusion>())
                if (occluder.gameObject.scene == gameObject.scene) occluder.Initialize(playerRenderer);
            var animator = dog.GetComponentInChildren<Animator>();
            var bulkaVisual = dog.GetComponent<BulkaVisualState>();
            var player = dog.GetComponent<PlayerController>();
            bulkaDefenseAbility = dog.GetComponent<BulkaDefenseAbility>();
            var carryVisual = dog.GetComponent<BulkaSockCarryVisual>();
            if (animator == null || bulkaVisual == null || player == null || bulkaDefenseAbility == null || carryVisual == null)
            {
                Debug.LogError("The configured Bulka prefab must contain Animator, BulkaVisualState, PlayerController, BulkaDefenseAbility and BulkaSockCarryVisual.", this);
                Destroy(dog);
                return;
            }

            animator.applyRootMotion = false;
            bulkaVisual.Initialize(animator);
            var flow = gameObject.AddComponent<GameFlow>();
            var uiRoot = new GameObject("Game UI");
            var hud = uiRoot.AddComponent<GameHUD>();
            hud.Build(art, audio);
            var session = new GameSessionState();
            var presenter = new GameHudPresenter(hud, session, balance, socks.Count);
            player.Initialize(flow, balance);
            foreach (var sock in socks) sock.Initialize(flow, audio);

            var trapManagerObject = new GameObject("Trap Placement Manager");
            trapManagerObject.transform.SetParent(transform);
            trapPlacementManager = trapManagerObject.AddComponent<TrapPlacementManager>();
            trapPlacementManager.Initialize(dog.transform, sceneBindings.trapZones, environmentArt.slowTrapArmed,
                environmentArt.slowTrapTriggered, environmentArt.slowTrapUsed, art.slowTrapPrefab, hud, presenter, session, balance, audio);
            bulkaDefenseAbility.Initialize(dog.transform, hud, balance, audio, session);
            waveController = gameObject.AddComponent<DefenseWaveController>();
            waveController.Initialize(routeSequence, flow, presenter, balance, art.orangeCatPrefab, art.sockPink,
                trapPlacementManager, bulkaDefenseAbility, audio);
            flow.Setup(balance, presenter, session, socks.Count, StartDefense, audio);
            hud.BindGameFlow(flow);
            carryVisual.Initialize(flow, environmentArt.sockCreamStripe);
            BuildBasket(flow, sceneBindings.basketRoot, art.basketPrefab);
            flow.ResultShown += bulkaVisual.ShowResult;

            string nextLevelSceneName = definition.nextLevelSceneName != null
                ? definition.nextLevelSceneName.Trim() : string.Empty;
            Action loadNextLevel = string.IsNullOrWhiteSpace(nextLevelSceneName)
                ? null
                : () => SceneManager.LoadScene(nextLevelSceneName, LoadSceneMode.Single);
            hud.SetResultActions(ReloadCurrentGameplayScene,
                () => SceneManager.LoadScene("MainMenu"), loadNextLevel);
        }

        private bool ValidateRequiredSetup(out List<CatRoute> routeSequence)
        {
            routeSequence = null;
            bool valid = true;
            if (definition == null) return Fail("Assign a LevelDefinition to LevelBootstrap.");
            if (sceneBindings == null) return Fail("Assign the scene's LevelSceneBindings to LevelBootstrap.");
            if (sceneBindings.gameObject.scene != gameObject.scene) valid &= Fail("LevelSceneBindings must belong to the same scene as LevelBootstrap.");
            if (sceneBindings.definition == null) valid &= Fail("LevelSceneBindings is missing its LevelDefinition.");
            else if (sceneBindings.definition != definition) valid &= Fail("LevelBootstrap.definition and LevelSceneBindings.definition must reference the same asset.");

            if (definition.balance == null) valid &= Fail($"Level '{definition.levelId}' is missing its GameBalanceConfig.");
            if (definition.audio == null) valid &= Fail($"Level '{definition.levelId}' is missing its AudioConfig.");
            if (definition.art == null) valid &= Fail($"Level '{definition.levelId}' is missing its ApartmentArtSet.");
            if (definition.environmentArt == null) valid &= Fail($"Level '{definition.levelId}' is missing its EnvironmentArtSet.");
            if (definition.art == null || definition.environmentArt == null || definition.balance == null) return false;

            if (sceneBindings.playerSpawn == null) valid &= Fail("LevelSceneBindings is missing playerSpawn.");
            if (sceneBindings.basketRoot == null) valid &= Fail("LevelSceneBindings is missing basketRoot.");
            if (sceneBindings.gameplayCamera == null) valid &= Fail("LevelSceneBindings is missing gameplayCamera.");
            else if (!sceneBindings.gameplayCamera.isActiveAndEnabled) valid &= Fail("The referenced gameplayCamera must be active and enabled.");

            var art = definition.art;
            if (art.bulkaPrefab == null) valid &= Fail("ApartmentArtSet is missing bulkaPrefab.");
            if (art.sockPrefab == null) valid &= Fail("ApartmentArtSet is missing sockPrefab.");
            if (art.basketPrefab == null) valid &= Fail("ApartmentArtSet is missing basketPrefab.");
            if (art.orangeCatPrefab == null) valid &= Fail("ApartmentArtSet is missing orangeCatPrefab.");
            if (art.slowTrapPrefab == null) valid &= Fail("ApartmentArtSet is missing slowTrapPrefab.");
            if (art.sockPink == null) valid &= Fail("ApartmentArtSet is missing the base sock sprite.");
            if (art.hudCard == null || art.finishCard == null) valid &= Fail("ApartmentArtSet requires hudCard and finishCard.");

            var environmentArt = definition.environmentArt;
            if (environmentArt.sockCreamStripe == null || environmentArt.sockGreen == null || environmentArt.sockLeaf == null
                || environmentArt.sockOrangeDots == null || environmentArt.sockGreenCheck == null || environmentArt.sockHeart == null)
                valid &= Fail("EnvironmentArtSet must contain all six sock visual variants.");
            if (environmentArt.slowTrapArmed == null || environmentArt.slowTrapTriggered == null || environmentArt.slowTrapUsed == null)
                valid &= Fail("EnvironmentArtSet must contain armed, triggered and used Slow Trap sprites.");
            if (art.trapZoneMarker == null) valid &= Fail("ApartmentArtSet is missing trapZoneMarker.");

            if (definition.sockCount <= 0) valid &= Fail("LevelDefinition.sockCount must be greater than zero.");
            if (sceneBindings.sockSpawnPoints == null || sceneBindings.sockSpawnPoints.Length < definition.sockCount)
                valid &= Fail($"LevelSceneBindings needs at least {definition.sockCount} sockSpawnPoints.");
            else
                for (int i = 0; i < sceneBindings.sockSpawnPoints.Length; i++)
                    if (sceneBindings.sockSpawnPoints[i] == null) { valid &= Fail($"LevelSceneBindings.sockSpawnPoints[{i}] is missing."); break; }

            if (sceneBindings.trapZones == null || sceneBindings.trapZones.Length == 0)
                valid &= Fail("LevelSceneBindings needs at least one trapZone.");
            else
                for (int i = 0; i < sceneBindings.trapZones.Length; i++)
                    if (sceneBindings.trapZones[i] == null) { valid &= Fail($"LevelSceneBindings.trapZones[{i}] is missing."); break; }

            if (valid) routeSequence = ResolveRouteSequence(definition, sceneBindings.routes, definition.balance.waveSize);
            return valid && routeSequence != null;
        }

        private bool Fail(string message)
        {
            Debug.LogError("LevelBootstrap setup error: " + message, this);
            return false;
        }

        private void StartDefense(int safeCount)
        {
            if (audio != null) audio.PlayDefenseMusicVariation();
            trapPlacementManager.BeginDefense();
            bulkaDefenseAbility.BeginDefense();
            waveController.BeginWave();
        }

        private static List<CatRoute> ResolveRouteSequence(LevelDefinition definition, CatRoute[] authoredRoutes, int waveSize)
        {
            if (definition.catRouteSequence == null || definition.catRouteSequence.Length < waveSize)
            {
                Debug.LogError($"Level '{definition.levelId}' route sequence has {definition.catRouteSequence?.Length ?? 0} entries; waveSize {waveSize} requires at least that many.");
                return null;
            }
            if (authoredRoutes == null || authoredRoutes.Length == 0)
            {
                Debug.LogError($"Level '{definition.levelId}' has no CatRoutes in LevelSceneBindings.");
                return null;
            }

            var routesById = new Dictionary<string, CatRoute>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var route in authoredRoutes)
            {
                if (route == null)
                {
                    Debug.LogError($"Level '{definition.levelId}' has a missing CatRoute reference.");
                    return null;
                }
                if (string.IsNullOrWhiteSpace(route.RouteId))
                {
                    Debug.LogError($"CatRoute '{route.name}' has no routeId. Assign a unique route ID in the Inspector.");
                    return null;
                }
                if (route.Count < 2)
                {
                    Debug.LogError($"CatRoute '{route.RouteId}' needs at least two waypoints.");
                    return null;
                }
                if (!routesById.TryAdd(route.RouteId.Trim(), route))
                {
                    Debug.LogError($"Level '{definition.levelId}' has duplicate CatRoute ID '{route.RouteId}'.");
                    return null;
                }
            }

            var resolved = new List<CatRoute>(definition.catRouteSequence.Length);
            foreach (string routeId in definition.catRouteSequence)
            {
                if (string.IsNullOrWhiteSpace(routeId) || !routesById.TryGetValue(routeId.Trim(), out var route))
                {
                    Debug.LogError($"Level '{definition.levelId}' route sequence references unknown CatRoute ID '{routeId}'.");
                    return null;
                }
                resolved.Add(route);
            }
            return resolved;
        }

        private void BuildBasket(GameFlow flow, Transform basketAnchor, GameObject basketPrefab)
        {
            var basket = Instantiate(basketPrefab, basketAnchor.position, basketAnchor.rotation, transform);
            basket.name = "Central laundry basket";
            int sortingOrder = Mathf.RoundToInt(-basketAnchor.position.y * 20f);
            foreach (var renderer in basket.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.sortingOrder = sortingOrder;
            var depositZone = basket.GetComponentInChildren<BasketDepositZone>(true);
            if (depositZone == null)
            {
                Debug.LogError("Configured Basket prefab is missing its child BasketDepositZone.", this);
                Destroy(basket);
                return;
            }
            depositZone.Initialize(flow, 1.35f);
            depositZone.InitializeAudio(audio);
        }

        private void ReloadCurrentGameplayScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || string.IsNullOrWhiteSpace(activeScene.path))
            {
                Debug.LogError("PLAY AGAIN could not resolve the active gameplay scene path.", this);
                return;
            }
            SceneManager.LoadScene(activeScene.path, LoadSceneMode.Single);
        }
    }
}
