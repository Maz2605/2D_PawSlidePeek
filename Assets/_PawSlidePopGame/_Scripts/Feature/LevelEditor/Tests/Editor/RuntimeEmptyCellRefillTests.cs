using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using NUnit.Framework;
using UnityEditor;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Tests.Editor
{
    public sealed class RuntimeEmptyCellRefillTests
    {
        [Test]
        public void BoardRefill_FillsPlayableEmptyCells_WithNormalTilesOnly()
        {
            Match3TileDatabaseSO database = AssetDatabase.LoadAssetAtPath<Match3TileDatabaseSO>("Assets/_PawSlidePopGame/_Data/Tiles/Database.asset");
            Match3LevelData levelData = new Match3LevelData
            {
                levelID = "Level_Runtime_Refill_Test",
                width = 3,
                height = 3,
                movesLimit = 20,
                tileLayout = new[]
                {
                    101, 0, 0,
                    0, 0, 0,
                    0, 0, 0
                },
                underlayLayout = new int[9],
                overlayLayout = new int[9],
                playableMask = new[]
                {
                    true, true, true,
                    true, false, true,
                    true, true, true
                },
                spawnableTileIds = new List<int> { 101, 102, 151, 201, 301 }
            };

            BoardModel board = new BoardModel(levelData);
            board.PopulateBoard(levelData.underlayLayout, levelData.tileLayout, levelData.overlayLayout, database);

            int spawned = BoardRefillService.Apply(board, levelData, database, new Random(12));

            Assert.That(spawned, Is.EqualTo(7));
            Assert.That(board.GetCell(0, 0).Tile.TileId, Is.EqualTo(101));
            Assert.That(board.GetCell(1, 1).IsPlayable, Is.False);
            Assert.That(board.GetCell(1, 1).Tile, Is.Null);
            Assert.That(board.GetCell(2, 2).Tile, Is.Not.Null);

            foreach (var cell in board.GetAllCells())
            {
                if (!cell.IsPlayable)
                {
                    continue;
                }

                Assert.That(cell.Tile, Is.Not.Null);
                Assert.That(cell.Tile.TileKind, Is.EqualTo(TileKind.Normal));
                CollectionAssert.Contains(new[] { 101, 102 }, cell.Tile.TileId);
            }
        }

        [Test]
        public void BoardPopulate_RetainsOverlay_OnPlayableEmptyCells()
        {
            Match3TileDatabaseSO database = AssetDatabase.LoadAssetAtPath<Match3TileDatabaseSO>("Assets/_PawSlidePopGame/_Data/Tiles/Database.asset");
            Match3LevelData levelData = new Match3LevelData
            {
                levelID = "Level_Overlay_EmptyCell_Test",
                width = 3,
                height = 3,
                movesLimit = 20,
                tileLayout = new[]
                {
                    101, 0, 0,
                    0, 0, 0,
                    0, 0, 0
                },
                underlayLayout = new int[9],
                overlayLayout = new[]
                {
                    0, 301, 0,
                    0, 0, 0,
                    0, 0, 0
                },
                playableMask = new[]
                {
                    true, true, true,
                    true, true, true,
                    true, true, true
                },
                spawnableTileIds = new List<int> { 101, 102 }
            };

            BoardModel board = new BoardModel(levelData);
            board.PopulateBoard(levelData.underlayLayout, levelData.tileLayout, levelData.overlayLayout, database);

            // Verify the overlay at cell (1, 0) was NOT cleared during population even though the cell had no base tile.
            CellModel cellWithOverlay = board.GetCell(1, 0);
            Assert.That(cellWithOverlay.Overlay, Is.Not.Null);
            Assert.That(cellWithOverlay.Overlay.TileId, Is.EqualTo(301));
            Assert.That(cellWithOverlay.Tile, Is.Null);

            // Verify refill successfully adds a tile under the overlay
            int spawned = BoardRefillService.Apply(board, levelData, database, new Random(12));
            Assert.That(spawned, Is.EqualTo(8));
            Assert.That(cellWithOverlay.Tile, Is.Not.Null);
            Assert.That(cellWithOverlay.Overlay, Is.Not.Null);
            Assert.That(cellWithOverlay.Overlay.TileId, Is.EqualTo(301));
        }

        [Test]
        public void BoardRefill_UsesCustomSpawnWeights_AndDynamicBalancingSettings()
        {
            Match3TileDatabaseSO database = AssetDatabase.LoadAssetAtPath<Match3TileDatabaseSO>("Assets/_PawSlidePopGame/_Data/Tiles/Database.asset");
            Match3LevelData levelData = new Match3LevelData
            {
                levelID = "Level_Custom_Weights_Test",
                width = 6,
                height = 6,
                movesLimit = 20,
                tileLayout = new int[36],
                underlayLayout = new int[36],
                overlayLayout = new int[36],
                playableMask = new bool[36],
                enableDynamicBalancing = false, // turn off dynamic balancing to see pure weight ratio
                targetSpawnBias = 1.0f,
                spawnableTileConfigs = new List<LevelSpawnableTileConfig>
                {
                    new LevelSpawnableTileConfig(101, 1000, true), // Tile 101 has extremely high weight
                    new LevelSpawnableTileConfig(102, 1, true)     // Tile 102 has extremely low weight
                }
            };

            for (int i = 0; i < 36; i++)
            {
                levelData.playableMask[i] = true;
            }

            BoardModel board = new BoardModel(levelData);
            board.PopulateBoard(levelData.underlayLayout, levelData.tileLayout, levelData.overlayLayout, database);

            // Refill the empty board
            int spawned = BoardRefillService.Apply(board, levelData, database, new Random(42));
            Assert.That(spawned, Is.EqualTo(36));

            int count101 = 0;
            int count102 = 0;

            foreach (var cell in board.GetAllCells())
            {
                Assert.That(cell.Tile, Is.Not.Null);
                if (cell.Tile.TileId == 101)
                {
                    count101++;
                }
                else if (cell.Tile.TileId == 102)
                {
                    count102++;
                }
            }

            // Since weight is 1000 vs 1 and dynamic balancing is disabled, 101 should dominate up to the match-3 restriction limit
            Assert.That(count101, Is.GreaterThan(18));
            Assert.That(count101, Is.GreaterThan(count102));
        }
    }
}
