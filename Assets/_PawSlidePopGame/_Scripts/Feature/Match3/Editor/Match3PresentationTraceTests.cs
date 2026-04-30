using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Editor
{
    public class Match3PresentationTraceTests
    {
        [Test]
        public void ExecuteMove_InvalidMove_ProducesRollbackAndRestoresBoard()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateNormalTile(103, AnimalTileId.Fox));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 102, 103,
                102, 103, 101,
                103, 101, 102
            });

            BoardModel board = new BoardModel(levelData);
            board.PopulateBoard(levelData.gridLayout, database);
            int[] beforeGrid = CaptureBoard(board);

            BoardMoveExecutionResult result = BoardResolutionService.ExecuteMove(
                board,
                new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right),
                levelData,
                database,
                new System.Random(7));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.PresentationTrace.MoveAttempt, Is.Not.Null);
            Assert.That(result.PresentationTrace.MoveAttempt.TravelOps.Count, Is.EqualTo(3));
            Assert.That(result.PresentationTrace.Rollback, Is.Not.Null);
            Assert.That(result.PresentationTrace.Rollback.TravelOps.Count, Is.EqualTo(3));
            Assert.That(result.PresentationTrace.Cascades.Count, Is.EqualTo(0));
            Assert.That(CaptureBoard(board), Is.EqualTo(beforeGrid));
        }

        [Test]
        public void ExecuteMove_ValidCascade_ProducesClearGravityAndRefillTrace()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateNormalTile(103, AnimalTileId.Fox));

            Match3LevelData levelData = CreateLevelData(3, 4, new[]
            {
                101, 102, 101,
                103, 101, 102,
                102, 103, 101,
                101, 103, 102
            });

            levelData.spawnableTileIds = new List<int> { 101, 102, 103 };

            BoardModel board = new BoardModel(levelData);
            board.PopulateBoard(levelData.gridLayout, database);

            BoardMoveExecutionResult result = BoardResolutionService.ExecuteMove(
                board,
                new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right),
                levelData,
                database,
                new System.Random(11));

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.ResolutionResult.CascadesResolved, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.PresentationTrace.Cascades.Count, Is.GreaterThanOrEqualTo(1));

            CascadeTrace firstCascade = result.PresentationTrace.Cascades[0];
            Assert.That(firstCascade.ClearPhase.ClearOps.Count, Is.EqualTo(3));
            Assert.That(firstCascade.GravityPhase.TravelOps.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(firstCascade.RefillPhase.SpawnOps.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(firstCascade.RefillPhase.SpawnOps.TrueForAll(op => op.SpawnFromRowAboveBoard < 0), Is.True);
            Assert.That(firstCascade.RefillPhase.SpawnOps.TrueForAll(op => op.Definition != null), Is.True);
        }

        [Test]
        public void BombActivation_RecordsActivateAndNeighborClears()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(201, TileLogicType.BombBooster));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 201, 101,
                101, 101, 101
            });

            BoardModel board = new BoardModel(levelData);
            board.PopulateBoard(levelData.gridLayout, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace);
            CellModel centerCell = board.GetCell(1, 1);

            centerCell.CurrentTile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ActivateOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(9));
            Assert.That(centerCell.CurrentTile, Is.Null);
        }

        [Test]
        public void IceExplosion_RecordsDamageAndClearWhenDestroyed()
        {
            Match3TileDatabaseSO database = CreateDatabase(CreateBlockerTile(301, 1));
            Match3LevelData levelData = CreateLevelData(1, 1, new[] { 301 });

            BoardModel board = new BoardModel(levelData);
            board.PopulateBoard(levelData.gridLayout, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace);
            CellModel cell = board.GetCell(0, 0);

            cell.CurrentTile.Explode(board, cell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.DamageOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.DamageOps[0].Destroyed, Is.True);
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(1));
            Assert.That(cell.CurrentTile, Is.Null);
        }

        private static Match3LevelData CreateLevelData(int width, int height, int[] layout)
        {
            return new Match3LevelData
            {
                width = width,
                height = height,
                movesLimit = 20,
                gridLayout = layout,
                spawnableTileIds = new List<int>()
            };
        }

        private static Match3TileDatabaseSO CreateDatabase(params TileDefinitionSO[] tiles)
        {
            Match3TileDatabaseSO database = ScriptableObject.CreateInstance<Match3TileDatabaseSO>();
            SetPrivateField(database, "tiles", new List<TileDefinitionSO>(tiles));
            database.RebuildCache();
            return database;
        }

        private static NormalAnimalTileDefinitionSO CreateNormalTile(int tileId, AnimalTileId animalId)
        {
            NormalAnimalTileDefinitionSO tile = ScriptableObject.CreateInstance<NormalAnimalTileDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "animalId", animalId);
            SetPrivateField(tile, "canSpawnOnRefill", true);
            SetPrivateField(tile, "spawnWeight", 1);
            return tile;
        }

        private static BoosterTileDefinitionSO CreateBoosterTile(int tileId, TileLogicType logicType)
        {
            BoosterTileDefinitionSO tile = ScriptableObject.CreateInstance<BoosterTileDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "boosterLogicType", logicType);
            SetPrivateField(tile, "canSpawnOnRefill", false);
            SetPrivateField(tile, "spawnWeight", 0);
            return tile;
        }

        private static BlockerTileDefinitionSO CreateBlockerTile(int tileId, int defaultHp)
        {
            BlockerTileDefinitionSO tile = ScriptableObject.CreateInstance<BlockerTileDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "blockerLogicType", TileLogicType.IceBlocker);
            SetPrivateField(tile, "defaultHP", defaultHp);
            SetPrivateField(tile, "canSpawnOnRefill", false);
            SetPrivateField(tile, "spawnWeight", 0);
            return tile;
        }

        private static int[] CaptureBoard(BoardModel board)
        {
            List<int> ids = new List<int>();
            foreach (CellModel cell in board.GetAllCells())
            {
                ids.Add(cell.CurrentTile != null ? cell.CurrentTile.TileId : 0);
            }

            return ids.ToArray();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = null;
            System.Type currentType = target.GetType();
            while (currentType != null && fieldInfo == null)
            {
                fieldInfo = currentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                currentType = currentType.BaseType;
            }

            Assert.That(fieldInfo, Is.Not.Null, $"Missing field '{fieldName}' on {target.GetType().Name}.");
            fieldInfo.SetValue(target, value);
        }
    }
}
