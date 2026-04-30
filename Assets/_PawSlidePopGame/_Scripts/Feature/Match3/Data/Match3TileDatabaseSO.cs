using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Data
{
    [CreateAssetMenu(fileName = "Match3TileDatabase", menuName = "_PawSlidePopGame/Match3/Tile Database")]
    public class Match3TileDatabaseSO : ScriptableObject
    {
        private const int NormalMinId = 100;
        private const int NormalMaxId = 199;
        private const int BoosterMinId = 200;
        private const int BoosterMaxId = 299;
        private const int BlockerMinId = 300;
        private const int BlockerMaxId = 399;
        private const int MechanicMinId = 400;
        private const int MechanicMaxId = 499;

        [SerializeField] private List<TileDefinitionSO> tiles = new List<TileDefinitionSO>();

        private readonly Dictionary<int, TileDefinitionSO> _tilesById = new Dictionary<int, TileDefinitionSO>();
        private readonly List<TileDefinitionSO> _spawnableTiles = new List<TileDefinitionSO>();

        public IReadOnlyList<TileDefinitionSO> Tiles => tiles;
        public int TileCount => tiles.Count;
        public int CachedTileCount => _tilesById.Count;
        public int SpawnableTileCount => _spawnableTiles.Count;

        private void OnEnable()
        {
            RebuildCache();
        }

        private void OnValidate()
        {
            RebuildCache();
        }

        public void RebuildCache()
        {
            _tilesById.Clear();
            _spawnableTiles.Clear();
            HashSet<int> seenIds = new HashSet<int>();

            for (int i = 0; i < tiles.Count; i++)
            {
                TileDefinitionSO tile = tiles[i];
                if (tile == null)
                {
                    Debug.LogError($"[Match3TileDatabase] Null tile entry at index {i} in database {name}.", this);
                    continue;
                }

                if (tile.TileId <= 0)
                {
                    Debug.LogError($"[Match3TileDatabase] Tile '{tile.name}' has invalid Tile Id {tile.TileId}.", tile);
                    continue;
                }

                if (!seenIds.Add(tile.TileId))
                {
                    Debug.LogError($"[Match3TileDatabase] Duplicate Tile Id {tile.TileId} found in database {name}.", tile);
                    continue;
                }

                if (!IsTileIdInExpectedRange(tile))
                {
                    Debug.LogWarning(
                        $"[Match3TileDatabase] Tile '{tile.name}' with kind {tile.TileKind} should use id in range {GetExpectedRangeLabel(tile.TileKind)}, but current id is {tile.TileId}.",
                        tile);
                }

                _tilesById[tile.TileId] = tile;
                if (tile.CanSpawnOnRefill && tile.TileKind == TileKind.Normal && tile.SpawnWeight > 0)
                {
                    _spawnableTiles.Add(tile);
                }

                if (tile.TileViewPrefab == null)
                {
                    Debug.LogWarning($"[Match3TileDatabase] Tile '{tile.name}' has no Tile View Prefab assigned.", tile);
                }
            }
        }

        public void ValidateAndLogSummary()
        {
            RebuildCache();
            Debug.Log(
                $"[Match3TileDatabase] Validation finished for '{name}'. Total Entries: {TileCount}, Valid Unique Tiles: {CachedTileCount}, Spawnable Normal Tiles: {SpawnableTileCount}.",
                this);
        }

        public TileDefinitionSO GetTileDefinition(int tileId)
        {
            if (_tilesById.Count == 0)
            {
                RebuildCache();
            }

            _tilesById.TryGetValue(tileId, out TileDefinitionSO definition);
            return definition;
        }

        public int GetRandomSpawnableTileId(System.Random random, IReadOnlyList<int> restrictedTileIds = null)
        {
            if (_tilesById.Count == 0)
            {
                RebuildCache();
            }

            if (_spawnableTiles.Count == 0)
            {
                return 0;
            }

            int totalWeight = 0;
            for (int i = 0; i < _spawnableTiles.Count; i++)
            {
                TileDefinitionSO tile = _spawnableTiles[i];
                if (IsRestricted(tile.TileId, restrictedTileIds))
                {
                    continue;
                }

                totalWeight += Mathf.Max(0, tile.SpawnWeight);
            }

            if (totalWeight <= 0)
            {
                return 0;
            }

            int roll = random.Next(0, totalWeight);
            for (int i = 0; i < _spawnableTiles.Count; i++)
            {
                TileDefinitionSO tile = _spawnableTiles[i];
                if (IsRestricted(tile.TileId, restrictedTileIds))
                {
                    continue;
                }

                roll -= Mathf.Max(0, tile.SpawnWeight);
                if (roll < 0)
                {
                    return tile.TileId;
                }
            }

            return 0;
        }

        private static bool IsRestricted(int tileId, IReadOnlyList<int> restrictedTileIds)
        {
            if (restrictedTileIds == null)
            {
                return false;
            }

            for (int i = 0; i < restrictedTileIds.Count; i++)
            {
                if (restrictedTileIds[i] == tileId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTileIdInExpectedRange(TileDefinitionSO tile)
        {
            switch (tile.TileKind)
            {
                case TileKind.Normal:
                    return IsInRange(tile.TileId, NormalMinId, NormalMaxId);
                case TileKind.Booster:
                    return IsInRange(tile.TileId, BoosterMinId, BoosterMaxId);
                case TileKind.Blocker:
                    return IsInRange(tile.TileId, BlockerMinId, BlockerMaxId);
                case TileKind.Mechanic:
                    return IsInRange(tile.TileId, MechanicMinId, MechanicMaxId);
                default:
                    return false;
            }
        }

        private static bool IsInRange(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        private static string GetExpectedRangeLabel(TileKind tileKind)
        {
            switch (tileKind)
            {
                case TileKind.Normal:
                    return $"{NormalMinId}-{NormalMaxId}";
                case TileKind.Booster:
                    return $"{BoosterMinId}-{BoosterMaxId}";
                case TileKind.Blocker:
                    return $"{BlockerMinId}-{BlockerMaxId}";
                case TileKind.Mechanic:
                    return $"{MechanicMinId}-{MechanicMaxId}";
                default:
                    return "unknown";
            }
        }
    }
}
