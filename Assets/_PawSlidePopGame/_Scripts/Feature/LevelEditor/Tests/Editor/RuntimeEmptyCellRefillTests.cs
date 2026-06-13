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
    }
}
