using UnityEngine;
using BulkoGame.UI;
using BulkoGame.GameFlow;
using BulkoGame.Audio;

namespace BulkoGame.Defense
{
    public sealed class TrapPlacementManager : MonoBehaviour
    {
        private const float InteractionRange = 1.45f;
        private const float TrapTriggerRadius = .9f;

        private Transform player;
        private TrapPlacementZone[] zones;
        private Sprite trapSprite;
        private Sprite triggeredSprite;
        private Sprite usedSprite;
        private GameObject trapPrefab;
        private GameHUD hud;
        private GameSessionState session;
        private GameBalanceConfig balance;
        private GameHudPresenter presenter;
        private GameAudio audio;
        private bool defenseActive;
        public int TrapsPlaced => session.TrapsPlaced;

        public void Initialize(Transform playerTransform, TrapPlacementZone[] placementZones,
            Sprite slowTrapSprite, Sprite triggeredTrapSprite, Sprite usedTrapSprite, GameObject reusableTrapPrefab,
            GameHUD gameHud, GameHudPresenter ui, GameSessionState gameSession, GameBalanceConfig config, GameAudio gameAudio)
        {
            session = gameSession; balance = config; presenter = ui;
            player = playerTransform;
            zones = placementZones;
            trapSprite = slowTrapSprite;
            triggeredSprite = triggeredTrapSprite;
            usedSprite = usedTrapSprite;
            trapPrefab = reusableTrapPrefab;
            hud = gameHud;
            audio = gameAudio;
            if (zones == null) zones = new TrapPlacementZone[0];
            foreach (var zone in zones)
                if (zone != null) zone.SetDefenseVisible(false);
        }

        public void BeginDefense()
        {
            defenseActive = true;
            foreach (var zone in zones)
                if (zone != null) zone.SetDefenseVisible(true);
            presenter.Traps();
        }

        private void Update()
        {
            if (!defenseActive || Time.timeScale <= 0f || player == null) return;

            TrapPlacementZone nearest = FindNearestFreeZone();
            bool canPlace = !session.IsCarryingDefenseSock && session.TrapsPlaced < balance.maxTraps && nearest != null;
            hud.ShowTrapPrompt(canPlace, canPlace ? nearest.transform.position : Vector3.zero);

            foreach (var zone in zones)
                if (zone != null) zone.SetNearby(canPlace && zone == nearest);

            if (canPlace && Input.GetKeyDown(KeyCode.E)) TryPlaceTrapAt(nearest);
        }

        public void EndDefense()
        {
            defenseActive = false;
            if (hud != null) hud.ShowTrapPrompt(false, Vector3.zero);
            foreach (var zone in zones)
                if (zone != null) { zone.SetNearby(false); zone.SetDefenseVisible(false); }
        }

        private TrapPlacementZone FindNearestFreeZone()
        {
            TrapPlacementZone nearest = null;
            float nearestDistance = InteractionRange * InteractionRange;
            foreach (var zone in zones)
            {
                if (zone == null || zone.IsOccupied) continue;
                float distance = ((Vector2)player.position - (Vector2)zone.transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = zone;
            }
            return nearest;
        }

        public bool TryPlaceTrapAt(TrapPlacementZone zone)
        {
            if (!defenseActive || Time.timeScale <= 0f || session.IsCarryingDefenseSock || zone == null || zone.IsOccupied || session.TrapsPlaced >= balance.maxTraps || trapSprite == null || trapPrefab == null) return false;

            var trapObject = Instantiate(trapPrefab, new Vector3(zone.transform.position.x, zone.transform.position.y, .55f), Quaternion.identity, transform.parent);
            trapObject.name = "Slow Trap";
            var trap = trapObject.GetComponent<SlowTrap>();
            if (trap == null)
            {
                Debug.LogError("SlowTrap prefab is missing its SlowTrap component.");
                Destroy(trapObject);
                return false;
            }
            zone.MarkOccupied();
            if (audio != null) audio.Play(GameSound.TrapPlaced);
            trap.Initialize(trapSprite, triggeredSprite, usedSprite, TrapTriggerRadius, balance, audio);
            session.PlaceTrap();
            presenter.Traps();
            return true;
        }
    }
}
