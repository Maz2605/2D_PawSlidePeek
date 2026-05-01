using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using UnityEngine;
using UnityEngine.Serialization;

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

        [Header("Authoring")]
        [SerializeField] private List<NormalAnimalTileDefinitionSO> normalTiles = new List<NormalAnimalTileDefinitionSO>();
        [SerializeField] private List<BoosterTileDefinitionSO> boosterTiles = new List<BoosterTileDefinitionSO>();
        [SerializeField] private List<BlockerTileDefinitionSO> blockerTiles = new List<BlockerTileDefinitionSO>();
        [SerializeField] private List<MechanicTileDefinitionSO> mechanicTiles = new List<MechanicTileDefinitionSO>();

        [FormerlySerializedAs("tiles")]
        [SerializeField, HideInInspector] private List<TileDefinitionSO> tiles = new List<TileDefinitionSO>();

        private readonly Dictionary<int, TileDefinitionSO> _tilesById = new Dictionary<int, TileDefinitionSO>();
        private readonly List<TileDefinitionSO> _spawnableTiles = new List<TileDefinitionSO>();
        private readonly List<TileDefinitionSO> _allTiles = new List<TileDefinitionSO>();

        public IReadOnlyList<TileDefinitionSO> Tiles => _allTiles;
        public int TileCount => _allTiles.Count;
        public int NormalTileCount => normalTiles.Count;
        public int BoosterTileCount => boosterTiles.Count;
        public int BlockerTileCount => blockerTiles.Count;
        public int MechanicTileCount => mechanicTiles.Count;
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
            MigrateLegacyTiles();

            _tilesById.Clear();
            _spawnableTiles.Clear();
            _allTiles.Clear();

            HashSet<int> seenIds = new HashSet<int>();

            ValidateAndCacheList(normalTiles, "normalTiles", seenIds);
            ValidateAndCacheList(boosterTiles, "boosterTiles", seenIds);
            ValidateAndCacheList(blockerTiles, "blockerTiles", seenIds);
            ValidateAndCacheList(mechanicTiles, "mechanicTiles", seenIds);
        }

        public void ValidateAndLogSummary()
        {
            RebuildCache();
            Debug.Log(
                $"[Match3TileDatabase] Validation finished for '{name}'. Total Entries: {TileCount}, Normal: {NormalTileCount}, Booster: {BoosterTileCount}, Blocker: {BlockerTileCount}, Mechanic: {MechanicTileCount}, Valid Unique Tiles: {CachedTileCount}, Spawnable Normal Tiles: {SpawnableTileCount}.",
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

        public BoosterTileDefinitionSO GetBoosterDefinition(TileLogicType logicType)
        {
            if (_tilesById.Count == 0)
            {
                RebuildCache();
            }

            for (int i = 0; i < boosterTiles.Count; i++)
            {
                BoosterTileDefinitionSO booster = boosterTiles[i];
                if (booster != null && booster.LogicType == logicType)
                {
                    return booster;
                }
            }

            return null;
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

        private void ValidateAndCacheList<TTile>(List<TTile> source, string listName, HashSet<int> seenIds) where TTile : TileDefinitionSO
        {
            for (int i = 0; i < source.Count; i++)
            {
                TileDefinitionSO tile = source[i];
                ValidateAndCacheTile(tile, listName, i, seenIds);
            }
        }

        private void ValidateAndCacheTile(TileDefinitionSO tile, string listName, int index, HashSet<int> seenIds)
        {
            if (tile == null)
            {
                Debug.LogError($"[Match3TileDatabase] Null tile entry at index {index} in {listName} on database {name}.", this);
                return;
            }

            _allTiles.Add(tile);

            if (tile.TileId <= 0)
            {
                Debug.LogError($"[Match3TileDatabase] Tile '{tile.name}' has invalid Tile Id {tile.TileId}.", tile);
                return;
            }

            if (!seenIds.Add(tile.TileId))
            {
                Debug.LogError($"[Match3TileDatabase] Duplicate Tile Id {tile.TileId} found in database {name}.", tile);
                return;
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

        private void MigrateLegacyTiles()
        {
            if (tiles == null || tiles.Count == 0)
            {
                return;
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                TileDefinitionSO tile = tiles[i];
                if (tile == null)
                {
                    continue;
                }

                switch (tile)
                {
                    case NormalAnimalTileDefinitionSO normalTile:
                        AddIfMissing(normalTiles, normalTile);
                        break;
                    case BoosterTileDefinitionSO boosterTile:
                        AddIfMissing(boosterTiles, boosterTile);
                        break;
                    case BlockerTileDefinitionSO blockerTile:
                        AddIfMissing(blockerTiles, blockerTile);
                        break;
                    case MechanicTileDefinitionSO mechanicTile:
                        AddIfMissing(mechanicTiles, mechanicTile);
                        break;
                }
            }

            tiles.Clear();
        }

        private static void AddIfMissing<TTile>(List<TTile> target, TTile tile) where TTile : TileDefinitionSO
        {
            if (tile == null || target.Contains(tile))
            {
                return;
            }

            target.Add(tile);
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
