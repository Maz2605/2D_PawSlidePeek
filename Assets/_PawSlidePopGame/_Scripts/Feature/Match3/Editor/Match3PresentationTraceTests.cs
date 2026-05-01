using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Match;
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

            BoardModel board = CreateBoard(levelData, database);
            int[] beforeGrid = CaptureBoard(board);

            BoardMoveExecutionResult result = BoardResolutionService.ExecuteMove(
                board,
                new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right, 1, 1),
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

            BoardModel board = CreateBoard(levelData, database);

            BoardMoveExecutionResult result = BoardResolutionService.ExecuteMove(
                board,
                new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right, 1, 1),
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
        public void Analyze_PureTwoByTwoSquare_IsDetectedWithoutStraightRun()
        {
            Match3TileDatabaseSO database = CreateDatabase(CreateNormalTile(101, AnimalTileId.Cat));
            Match3LevelData levelData = CreateLevelData(2, 2, new[]
            {
                101, 101,
                101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            BoardMatchAnalysis analysis = new BoardMatchFinder().Analyze(board);

            Assert.That(analysis.HasMatches, Is.True);
            Assert.That(analysis.Groups.Count, Is.EqualTo(1));
            Assert.That(analysis.Groups[0].ContainsSquare2X2, Is.True);
            Assert.That(analysis.Groups[0].ClusterSize, Is.EqualTo(4));
            Assert.That(analysis.Groups[0].MaxStraightRunLength, Is.EqualTo(0));
        }

        [Test]
        public void SpecialCreationService_UsesClusterPriorityOverSquare()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(201, TileLogicType.BombBooster),
                CreateBoosterTile(202, TileLogicType.SquareBomb),
                CreateBoosterTile(203, TileLogicType.AreaBombMedium),
                CreateBoosterTile(205, TileLogicType.AreaBombLarge));

            Match3LevelData levelData = CreateLevelData(2, 3, new[]
            {
                101, 101,
                101, 101,
                101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            BoardMatchAnalysis analysis = new BoardMatchFinder().Analyze(board);

            List<SpecialSpawnDecision> decisions = SpecialCreationService.CreateDecisions(
                analysis,
                new BoardMoveRequest(MoveAxis.Column, 1, LineSlideDirection.Down, 1, 1),
                database);

            Assert.That(decisions.Count, Is.EqualTo(1));
            Assert.That(decisions[0].LogicType, Is.EqualTo(TileLogicType.AreaBombLarge));
            Assert.That(decisions[0].SpawnCell.X, Is.EqualTo(1));
            Assert.That(decisions[0].SpawnCell.Y, Is.EqualTo(1));
        }

        [Test]
        public void SpecialCreationService_SevenCellCluster_CreatesLargeBomb()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(205, TileLogicType.AreaBombLarge));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 101, 101,
                0, 101, 0
            });

            BoardModel board = CreateBoard(levelData, database);
            BoardMatchAnalysis analysis = new BoardMatchFinder().Analyze(board);

            List<SpecialSpawnDecision> decisions = SpecialCreationService.CreateDecisions(
                analysis,
                new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right, 1, 1),
                database);

            Assert.That(decisions.Count, Is.EqualTo(1));
            Assert.That(decisions[0].LogicType, Is.EqualTo(TileLogicType.AreaBombLarge));
        }

        [Test]
        public void SpecialCreationService_FiveCellCluster_CreatesMediumBomb()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(204, TileLogicType.AreaBombMedium));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                0, 101, 0,
                101, 101, 101,
                0, 101, 0
            });

            BoardModel board = CreateBoard(levelData, database);
            BoardMatchAnalysis analysis = new BoardMatchFinder().Analyze(board);

            List<SpecialSpawnDecision> decisions = SpecialCreationService.CreateDecisions(
                analysis,
                new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right, 1, 1),
                database);

            Assert.That(decisions.Count, Is.EqualTo(1));
            Assert.That(decisions[0].LogicType, Is.EqualTo(TileLogicType.AreaBombMedium));
        }

        [Test]
        public void ExecuteMove_FourInRow_CreatesCrossBombAndTrace()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(202, TileLogicType.CrossBomb));

            Match3LevelData levelData = CreateLevelData(4, 1, new[]
            {
                101, 101, 101, 101
            });
            levelData.spawnableTileIds = new List<int> { 101 };

            BoardModel board = CreateBoard(levelData, database);

            BoardMoveExecutionResult result = BoardResolutionService.ExecuteMove(
                board,
                new BoardMoveRequest(MoveAxis.Row, 0, LineSlideDirection.Right, 1, 0),
                levelData,
                database,
                new System.Random(3));

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.PresentationTrace.Cascades.Count, Is.EqualTo(1));
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.SpecialCreateOps.Count, Is.EqualTo(1));

            SpecialCreateOp createOp = result.PresentationTrace.Cascades[0].ClearPhase.SpecialCreateOps[0];
            Assert.That(createOp.LogicType, Is.EqualTo(TileLogicType.CrossBomb));

            CellModel sourceCell = board.GetCell(1, 0);
            Assert.That(sourceCell.CurrentTile, Is.Not.Null);
            Assert.That(sourceCell.CurrentTile.LogicType, Is.EqualTo(TileLogicType.CrossBomb));
        }

        [Test]
        public void ExecuteMove_TwoByTwoMatch_CreatesSquareBombAndTrace()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(203, TileLogicType.SquareBomb));

            Match3LevelData levelData = CreateLevelData(2, 2, new[]
            {
                101, 101,
                101, 101
            });
            levelData.spawnableTileIds = new List<int> { 101 };

            BoardModel board = CreateBoard(levelData, database);

            BoardMoveExecutionResult result = BoardResolutionService.ExecuteMove(
                board,
                new BoardMoveRequest(MoveAxis.Row, 0, LineSlideDirection.Right, 0, 0),
                levelData,
                database,
                new System.Random(5));

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.PresentationTrace.Cascades.Count, Is.EqualTo(1));
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.SpecialCreateOps.Count, Is.EqualTo(1));

            SpecialCreateOp createOp = result.PresentationTrace.Cascades[0].ClearPhase.SpecialCreateOps[0];
            Assert.That(createOp.LogicType, Is.EqualTo(TileLogicType.SquareBomb));

            CellModel sourceCell = board.GetCell(0, 0);
            Assert.That(sourceCell.CurrentTile, Is.Not.Null);
            Assert.That(sourceCell.CurrentTile.LogicType, Is.EqualTo(TileLogicType.SquareBomb));
        }

        [Test]
        public void ExecuteTileActivation_BoosterTap_ActivatesConsumesMoveAndRefillsBoard()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(202, TileLogicType.CrossBomb));

            Match3LevelData levelData = CreateFilledLevel(3, 3, 101);
            levelData.gridLayout[4] = 202;
            levelData.spawnableTileIds = new List<int> { 101 };

            BoardModel board = CreateBoard(levelData, database);
            int movesBefore = board.RemainingMoves;

            BoardMoveExecutionResult result = BoardResolutionService.ExecuteTileActivation(
                board,
                1,
                1,
                levelData,
                database,
                new System.Random(13));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.PresentationTrace.MoveAttempt, Is.Null);
            Assert.That(result.PresentationTrace.Cascades.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.ActivateOps.Count, Is.EqualTo(1));
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.ClearOps.Count, Is.EqualTo(5));
            Assert.That(board.RemainingMoves, Is.EqualTo(movesBefore - 1));
            Assert.That(board.GetCell(1, 1).CurrentTile, Is.Not.Null);
            Assert.That(board.GetCell(1, 1).CurrentTile.TileKind, Is.EqualTo(TileKind.Normal));
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

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(1, 1);

            centerCell.CurrentTile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ActivateOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(9));
            Assert.That(centerCell.CurrentTile, Is.Null);
        }

        [Test]
        public void CrossBombActivation_RecordsFullRowAndColumnClears()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(202, TileLogicType.CrossBomb));

            Match3LevelData levelData = CreateLevelData(5, 5, new[]
            {
                101, 101, 101, 101, 101,
                101, 101, 101, 101, 101,
                101, 101, 202, 101, 101,
                101, 101, 101, 101, 101,
                101, 101, 101, 101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(2, 2);

            centerCell.CurrentTile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ActivateOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(9));
            Assert.That(centerCell.CurrentTile, Is.Null);
        }

        [Test]
        public void SquareBombActivation_RecordsTargetSelection()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(203, TileLogicType.SquareBomb));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 203, 101,
                101, 101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(1, 1);
            CellModel expectedTarget = board.GetCell(0, 2);
            int expectedTargetInstanceId = expectedTarget.CurrentTile.InstanceId;

            centerCell.CurrentTile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ActivateOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.TargetSelectionOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.TargetSelectionOps[0].TargetCell, Is.EqualTo(new BoardCellPosition(0, 2)));
            Assert.That(cascadeTrace.ClearPhase.TargetSelectionOps[0].TargetTileInstanceId, Is.EqualTo(expectedTargetInstanceId));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(4));
            Assert.That(centerCell.CurrentTile, Is.Null);
        }

        [Test]
        public void AreaBombMediumActivation_ClearsDiamondRadiusTwo()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(204, TileLogicType.AreaBombMedium));

            Match3LevelData levelData = CreateFilledLevel(5, 5, 101);
            levelData.gridLayout[12] = 204;

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(2, 2);

            centerCell.CurrentTile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(13));
            Assert.That(centerCell.CurrentTile, Is.Null);
        }

        [Test]
        public void AreaBombLargeActivation_ClearsFiveByFive()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(205, TileLogicType.AreaBombLarge));

            Match3LevelData levelData = CreateFilledLevel(7, 7, 101);
            levelData.gridLayout[24] = 205;

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(3, 3);

            centerCell.CurrentTile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(25));
            Assert.That(centerCell.CurrentTile, Is.Null);
        }

        [Test]
        public void IceExplosion_RecordsDamageAndClearWhenDestroyed()
        {
            Match3TileDatabaseSO database = CreateDatabase(CreateBlockerTile(301, 1));
            Match3LevelData levelData = CreateLevelData(1, 1, new[] { 301 });

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel cell = board.GetCell(0, 0);

            cell.CurrentTile.Explode(board, cell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.DamageOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.DamageOps[0].Destroyed, Is.True);
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(1));
            Assert.That(cell.CurrentTile, Is.Null);
        }

        private static Match3LevelData CreateFilledLevel(int width, int height, int tileId)
        {
            int[] layout = new int[width * height];
            for (int i = 0; i < layout.Length; i++)
            {
                layout[i] = tileId;
            }

            return CreateLevelData(width, height, layout);
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

        private static BoardModel CreateBoard(Match3LevelData levelData, Match3TileDatabaseSO database)
        {
            BoardModel board = new BoardModel(levelData);
            board.PopulateBoard(levelData.gridLayout, database);
            return board;
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
