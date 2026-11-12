using System.Collections.Generic;
using BulkoGame.Levels;
using UnityEngine;

namespace BulkoGame.Collectibles
{
    /// <summary>Creates this run's socks from the level's authored safe spawn points.</summary>
    public sealed class SockSpawnController : MonoBehaviour
    {
        private const int PixelsPerUnit = 16;

        private LevelDefinition definition;
        private SockSpawnPoint[] spawnPoints;
        private GameObject sockPrefab;
        private Sprite baseSprite;
        private Sprite[] visualVariants;

        public void Initialize(LevelDefinition levelDefinition, SockSpawnPoint[] boundSpawnPoints,
            GameObject reusableSockPrefab, Sprite sockBaseSprite, Sprite[] sockVisualVariants)
        {
            definition = levelDefinition;
            spawnPoints = boundSpawnPoints;
            sockPrefab = reusableSockPrefab;
            baseSprite = sockBaseSprite;
            visualVariants = sockVisualVariants;
        }

        public List<SockCollectible> SpawnSocks()
        {
            var result = new List<SockCollectible>();
            if (definition == null)
            {
                Debug.LogError("Sock spawning requires a LevelDefinition.");
                return result;
            }
            if (definition.sockCount <= 0 || definition.minimumSocksPerZone < 0
                || definition.maximumSocksPerZone < definition.minimumSocksPerZone)
            {
                Debug.LogError("LevelDefinition contains invalid sock count or zone limits.");
                return result;
            }
            if (sockPrefab == null || baseSprite == null || visualVariants == null || visualVariants.Length == 0)
            {
                Debug.LogError("Sock spawning requires a sock prefab, base sprite, and at least one visual variant.");
                return result;
            }

            var valid = new List<SockSpawnPoint>();
            if (spawnPoints != null)
            {
                foreach (var point in spawnPoints)
                {
                    if (point == null) continue;
                    if (Physics2D.OverlapCircle(point.transform.position, .2f) != null)
                    {
                        Debug.LogError("Sock spawn is inside a collider: " + point.name);
                        continue;
                    }
                    if (!valid.Contains(point)) valid.Add(point);
                }
            }

            if (valid.Count < definition.sockCount)
            {
                Debug.LogError($"Sock spawn pool has fewer than {definition.sockCount} safe unique points.");
                return result;
            }

            var selected = new List<SockSpawnPoint>(definition.sockCount);
            var counts = new Dictionary<SockSpawnRoom, int>();
            var rooms = (SockSpawnRoom[])System.Enum.GetValues(typeof(SockSpawnRoom));

            foreach (var room in rooms)
            {
                var candidates = valid.FindAll(point => point.Room == room);
                if (candidates.Count < definition.minimumSocksPerZone)
                {
                    Debug.LogError("Sock spawn pool has too few points in " + room + ".");
                    return result;
                }

                counts[room] = 0;
                for (int i = 0; i < definition.minimumSocksPerZone; i++)
                {
                    var point = candidates[Random.Range(0, candidates.Count)];
                    selected.Add(point);
                    valid.Remove(point);
                    candidates.Remove(point);
                    counts[room]++;
                }
            }

            while (selected.Count < definition.sockCount)
            {
                var candidates = valid.FindAll(point => !selected.Contains(point)
                    && counts[point.Room] < definition.maximumSocksPerZone);
                if (candidates.Count == 0)
                {
                    Debug.LogError("Sock spawn pool cannot fill the requested count while respecting zone limits.");
                    return new List<SockCollectible>();
                }

                var point = candidates[Random.Range(0, candidates.Count)];
                selected.Add(point);
                counts[point.Room]++;
            }

            foreach (var point in selected)
            {
                var variant = visualVariants[Random.Range(0, visualVariants.Length)];
                result.Add(CreateSock(point.transform.position, variant));
            }
            return result;
        }

        private SockCollectible CreateSock(Vector2 position, Sprite productionSprite)
        {
            var sockObject = Instantiate(sockPrefab, new Vector3(position.x, position.y, .18f), Quaternion.identity, transform);
            sockObject.name = "Striped sock";
            sockObject.transform.localScale = new Vector3(
                .72f / (baseSprite.texture.width / (float)PixelsPerUnit),
                1.05f / (baseSprite.texture.height / (float)PixelsPerUnit), 1f);

            var renderer = sockObject.GetComponent<SpriteRenderer>();
            var collectible = sockObject.GetComponent<SockCollectible>();
            var trigger = sockObject.GetComponent<CircleCollider2D>();
            var visual = sockObject.transform.Find("Production visual");
            var visualRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || collectible == null || trigger == null || visualRenderer == null)
            {
                Debug.LogError("Sock prefab is missing its SpriteRenderer, Production visual, trigger, or SockCollectible.");
                Destroy(sockObject);
                return null;
            }

            renderer.sprite = baseSprite;
            renderer.sortingOrder = Mathf.RoundToInt(-position.y * 20f);
            renderer.enabled = false;
            visualRenderer.sprite = productionSprite;
            visualRenderer.sortingOrder = renderer.sortingOrder;
            float fit = Mathf.Min(.55f / productionSprite.bounds.size.x, .78f / productionSprite.bounds.size.y);
            visual.transform.localScale = new Vector3(fit / sockObject.transform.lossyScale.x,
                fit / sockObject.transform.lossyScale.y, 1f);
            trigger.isTrigger = true;
            trigger.radius = .43f;
            return collectible;
        }
    }
}
