using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution
{
    public static class BoardRefillService
    {
        public static int Apply(
            BoardModel board,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            IList<TileSpawnOp> spawnOps = null)
        {
            int spawnedTiles = 0;
            Random rng = random ?? new Random();

            for (int x = 0; x < board.Width; x++)
            {
                int spawnOffset = 0;
                for (int y = board.Height - 1; y >= 0; y--)
                {
                    CellModel cell = board.GetCell(x, y);
                    if (cell == null || !cell.IsPlayable || cell.HasTile())
                    {
                        continue;
                    }

                    int tileId = PickSpawnTileId(board, x, y, levelData, tileDatabase, rng);
                    if (tileId == 0)
                    {
                        continue;
                    }

                    TileModel tile = board.CreateTileFromDefinitionId(tileId, tileDatabase);
                    if (tile == null)
                    {
                        continue;
                    }

                    board.SetTile(cell, tile);
                    spawnOffset++;
                    spawnOps?.Add(new TileSpawnOp
                    {
                        TileInstanceId = tile.InstanceId,
                        TileId = tile.TileId,
                        Layer = TileStackLayer.Base,
                        Definition = tile.Definition,
                        SpawnFromRowAboveBoard = -spawnOffset,
                        ToCell = new BoardCellPosition(cell.X, cell.Y)
                    });
                    spawnedTiles++;
                }
            }

            return spawnedTiles;
        }

        private static int PickSpawnTileId(BoardModel board, int x, int y, Match3LevelData levelData, Match3TileDatabaseSO tileDatabase, Random random)
        {
            List<int> restrictedIds = new List<int>(2);
            AddNeighborRestriction(board.GetCell(x - 1, y), board.GetCell(x - 2, y), restrictedIds);
            AddNeighborRestriction(board.GetCell(x, y + 1), board.GetCell(x, y + 2), restrictedIds);

            if (levelData != null && levelData.spawnableTileIds != null && levelData.spawnableTileIds.Count > 0)
            {
                List<int> filteredIds = new List<int>();
                for (int i = 0; i < levelData.spawnableTileIds.Count; i++)
                {
                    int candidateId = levelData.spawnableTileIds[i];
                    if (!IsRestricted(candidateId, restrictedIds))
                    {
                        filteredIds.Add(candidateId);
                    }
                }

                if (filteredIds.Count > 0)
                {
                    return filteredIds[random.Next(0, filteredIds.Count)];
                }
            }

            return tileDatabase != null ? tileDatabase.GetRandomSpawnableTileId(random, restrictedIds) : 0;
        }

        private static void AddNeighborRestriction(CellModel first, CellModel second, List<int> restrictedIds)
        {
            if (first?.Tile == null || second?.Tile == null)
            {
                return;
            }

            if (!first.Tile.IsMatchableWith(second.Tile))
            {
                return;
            }

            restrictedIds.Add(first.Tile.TileId);
        }

        private static bool IsRestricted(int tileId, List<int> restrictedIds)
        {
            for (int i = 0; i < restrictedIds.Count; i++)
            {
                if (restrictedIds[i] == tileId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

