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
        public void ExecuteMove_InvalidMoveWithChocolateOverlay_RestoresBaseAndOverlayStack()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateNormalTile(103, AnimalTileId.Fox),
                CreateChocolateOverlayTile(303, 1));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 102, 103,
                102, 103, 101,
                103, 101, 102
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 303, 0,
                0, 0, 0
            };

            BoardModel board = CreateBoard(levelData, database);

            BoardMoveExecutionResult result = BoardResolutionService.ExecuteMove(
                board,
                new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right, 1, 1),
                levelData,
                database,
                new System.Random(17));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.False);
            Assert.That(result.PresentationTrace.MoveAttempt.TravelOps.Exists(op =>
                op.TileId == 303 && op.Layer == TileStackLayer.Overlay), Is.True);
            Assert.That(result.PresentationTrace.Rollback.TravelOps.Exists(op =>
                op.TileId == 303 && op.Layer == TileStackLayer.Overlay), Is.True);
            Assert.That(board.GetCell(1, 1).Tile, Is.Not.Null);
            Assert.That(board.GetCell(1, 1).Tile.TileId, Is.EqualTo(103));
            Assert.That(board.GetCell(1, 1).Overlay, Is.Not.Null);
            Assert.That(board.GetCell(1, 1).Overlay.TileId, Is.EqualTo(303));
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
            Assert.That(firstCascade.ClearPhase.ScoreGainOps.Count, Is.EqualTo(3));
            Assert.That(firstCascade.ClearPhase.ScoreGainOps.TrueForAll(op => op.Amount == 10), Is.True);
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
                CreateBoosterTile(151, TileLogicType.BombBooster),
                CreateBoosterTile(152, TileLogicType.SquareBomb),
                CreateBoosterTile(153, TileLogicType.AreaBombMedium),
                CreateBoosterTile(155, TileLogicType.AreaBombLarge));

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
                CreateBoosterTile(155, TileLogicType.AreaBombLarge));

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
                CreateBoosterTile(154, TileLogicType.AreaBombMedium));

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
                CreateBoosterTile(152, TileLogicType.CrossBomb));

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
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.ScoreGainOps.Count, Is.EqualTo(4));

            SpecialCreateOp createOp = result.PresentationTrace.Cascades[0].ClearPhase.SpecialCreateOps[0];
            Assert.That(createOp.LogicType, Is.EqualTo(TileLogicType.CrossBomb));
            Assert.That(
                result.PresentationTrace.Cascades[0].ClearPhase.ScoreGainOps.Exists(op =>
                    op.TileInstanceId == createOp.SourceTileInstanceId &&
                    op.Cell.Equals(createOp.Cell)),
                Is.True);

            CellModel sourceCell = board.GetCell(1, 0);
            Assert.That(sourceCell.Tile, Is.Not.Null);
            Assert.That(sourceCell.Tile.LogicType, Is.EqualTo(TileLogicType.CrossBomb));
        }

        [Test]
        public void ExecuteMove_TwoByTwoMatch_CreatesSquareBombAndTrace()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(153, TileLogicType.SquareBomb));

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
            Assert.That(sourceCell.Tile, Is.Not.Null);
            Assert.That(sourceCell.Tile.LogicType, Is.EqualTo(TileLogicType.SquareBomb));
        }

        [Test]
        public void ExecuteTileActivation_BoosterTap_ActivatesConsumesMoveAndRefillsBoard()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(152, TileLogicType.CrossBomb));

            Match3LevelData levelData = CreateFilledLevel(3, 3, 101);
            levelData.tileLayout[4] = 152;
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
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.ScoreGainOps.Count, Is.EqualTo(5));
            Assert.That(board.RemainingMoves, Is.EqualTo(movesBefore - 1));
            Assert.That(board.GetCell(1, 1).Tile, Is.Not.Null);
            Assert.That(board.GetCell(1, 1).Tile.TileKind, Is.EqualTo(TileKind.Normal));
        }

        [Test]
        public void BombActivation_RecordsActivateAndNeighborClears()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(151, TileLogicType.BombBooster));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 151, 101,
                101, 101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(1, 1);

            centerCell.Tile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ActivateOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(9));
            Assert.That(cascadeTrace.ClearPhase.ScoreGainOps.Count, Is.EqualTo(9));
            Assert.That(centerCell.Tile, Is.Null);
        }

        [Test]
        public void CrossBombActivation_RecordsFullRowAndColumnClears()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(152, TileLogicType.CrossBomb));

            Match3LevelData levelData = CreateLevelData(5, 5, new[]
            {
                101, 101, 101, 101, 101,
                101, 101, 101, 101, 101,
                101, 101, 152, 101, 101,
                101, 101, 101, 101, 101,
                101, 101, 101, 101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(2, 2);

            centerCell.Tile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ActivateOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(9));
            Assert.That(cascadeTrace.ClearPhase.ScoreGainOps.Count, Is.EqualTo(9));
            Assert.That(centerCell.Tile, Is.Null);
        }

        [Test]
        public void SquareBombActivation_RecordsTargetSelection()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(153, TileLogicType.SquareBomb));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 153, 101,
                101, 101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(1, 1);
            CellModel expectedTarget = board.GetCell(0, 2);
            int expectedTargetInstanceId = expectedTarget.Tile.InstanceId;

            centerCell.Tile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ActivateOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.TargetSelectionOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.TargetSelectionOps[0].TargetCell, Is.EqualTo(new BoardCellPosition(0, 2)));
            Assert.That(cascadeTrace.ClearPhase.TargetSelectionOps[0].TargetTileInstanceId, Is.EqualTo(expectedTargetInstanceId));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(4));
            Assert.That(cascadeTrace.ClearPhase.ScoreGainOps.Count, Is.EqualTo(4));
            Assert.That(centerCell.Tile, Is.Null);
        }

        [Test]
        public void AreaBombMediumActivation_ClearsDiamondRadiusTwo()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(154, TileLogicType.AreaBombMedium));

            Match3LevelData levelData = CreateFilledLevel(5, 5, 101);
            levelData.tileLayout[12] = 154;

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(2, 2);

            centerCell.Tile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(13));
            Assert.That(centerCell.Tile, Is.Null);
        }

        [Test]
        public void AreaBombLargeActivation_ClearsFiveByFive()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(155, TileLogicType.AreaBombLarge));

            Match3LevelData levelData = CreateFilledLevel(7, 7, 101);
            levelData.tileLayout[24] = 155;

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(3, 3);

            centerCell.Tile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(25));
            Assert.That(centerCell.Tile, Is.Null);
        }

        [Test]
        public void IceExplosion_RecordsDamageAndClearWhenDestroyed()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateOverlayTile(301, 1));
            Match3LevelData levelData = CreateLevelData(1, 1, new[] { 101 });
            levelData.overlayLayout = new[] { 301 };

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel cell = board.GetCell(0, 0);

            cell.Overlay.Explode(board, cell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.DamageOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.DamageOps[0].Destroyed, Is.True);
            Assert.That(cascadeTrace.ClearPhase.DamageOps[0].Layer, Is.EqualTo(TileStackLayer.Overlay));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.ClearOps[0].Layer, Is.EqualTo(TileStackLayer.Overlay));
            Assert.That(cascadeTrace.ClearPhase.ScoreGainOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.ScoreGainOps[0].Amount, Is.EqualTo(50));
            Assert.That(cell.Overlay, Is.Null);
            Assert.That(cell.Tile, Is.Not.Null);
        }

        [Test]
        public void Overlay_BlocksBaseMatchUntilDestroyed()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateOverlayTile(301, 1));
            Match3LevelData levelData = CreateLevelData(3, 1, new[] { 101, 101, 101 });
            levelData.overlayLayout = new[] { 0, 301, 0 };

            BoardModel board = CreateBoard(levelData, database);
            BoardMatchAnalysis blockedAnalysis = new BoardMatchFinder().Analyze(board);

            Assert.That(blockedAnalysis.HasMatches, Is.False);

            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(1, 0);
            centerCell.Overlay.Explode(board, centerCell, fxContext);

            Assert.That(centerCell.Overlay, Is.Null);

            BoardMatchAnalysis releasedAnalysis = new BoardMatchFinder().Analyze(board);
            Assert.That(releasedAnalysis.HasMatches, Is.True);
        }

        [Test]
        public void LineSlideMoveRule_IceOverlayLocksItsRowAndColumn()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateOverlayTile(301, 1));
            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 102, 101,
                102, 101, 102,
                101, 102, 101
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 301, 0,
                0, 0, 0
            };

            BoardModel board = CreateBoard(levelData, database);
            LineSlideMoveRule rule = new LineSlideMoveRule();

            Assert.That(rule.TryApply(board, new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right, 1, 1), out _), Is.False);
            Assert.That(rule.TryApply(board, new BoardMoveRequest(MoveAxis.Column, 1, LineSlideDirection.Down, 1, 1), out _), Is.False);
            Assert.That(rule.TryApply(board, new BoardMoveRequest(MoveAxis.Row, 0, LineSlideDirection.Right, 1, 0), out _), Is.True);
        }

        [Test]
        public void ResolveBoard_MatchAdjacentToIce_BreaksIceAndKeepsTile()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateNormalTile(103, AnimalTileId.Fox),
                CreateOverlayTile(301, 1));

            Match3LevelData levelData = CreateLevelData(3, 2, new[]
            {
                101, 101, 101,
                102, 102, 103
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 301, 0
            };

            BoardModel board = CreateBoard(levelData, database);

            BoardResolutionService.ResolveBoard(board, levelData, database, new System.Random(0));

            CellModel icedCell = board.GetCell(1, 1);
            Assert.That(icedCell.Overlay, Is.Null);
            Assert.That(icedCell.Tile, Is.Not.Null);
            Assert.That(icedCell.Tile.TileId, Is.EqualTo(102));
        }

        [Test]
        public void BombActivation_BreaksIceAndPreservesTile()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(151, TileLogicType.BombBooster),
                CreateOverlayTile(301, 1));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 151, 101,
                101, 101, 101
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 0, 0,
                0, 301, 0
            };

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(1, 1);
            CellModel icedCell = board.GetCell(1, 2);

            centerCell.Tile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.DamageOps.Exists(op =>
                op.Cell.Equals(new BoardCellPosition(1, 2)) &&
                op.Layer == TileStackLayer.Overlay &&
                op.TileId == 301), Is.True);
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op =>
                op.Cell.Equals(new BoardCellPosition(1, 2)) &&
                op.Layer == TileStackLayer.Overlay &&
                op.TileId == 301), Is.True);
            Assert.That(icedCell.Overlay, Is.Null);
            Assert.That(icedCell.Tile, Is.Not.Null);
            Assert.That(icedCell.Tile.TileId, Is.EqualTo(101));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op =>
                op.Cell.Equals(new BoardCellPosition(1, 2)) &&
                op.Layer == TileStackLayer.Base),
                Is.False);
        }

        [Test]
        public void BubbleOverlay_DoesNotBlockBaseMatchAndClearsWhenUnderlyingTileMatches()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateOverlayTile(302, 1, TileLogicType.BubbleOverlay));

            Match3LevelData levelData = CreateLevelData(3, 1, new[] { 101, 101, 101 });
            levelData.overlayLayout = new[] { 0, 302, 0 };

            BoardModel board = CreateBoard(levelData, database);
            BoardMoveExecutionResult result = BoardResolutionService.ExecuteMove(
                board,
                new BoardMoveRequest(MoveAxis.Row, 0, LineSlideDirection.Right, 1, 0),
                levelData,
                database,
                new System.Random(0));

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.PresentationTrace.Cascades.Count, Is.EqualTo(1));
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.ClearOps.Exists(op =>
                op.Cell.Equals(new BoardCellPosition(1, 0)) &&
                op.Layer == TileStackLayer.Overlay &&
                op.TileId == 302), Is.True);
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.ClearOps.Exists(op =>
                op.Cell.Equals(new BoardCellPosition(1, 0)) &&
                op.Layer == TileStackLayer.Base &&
                op.TileId == 101), Is.True);
        }

        [Test]
        public void LineSlideMoveRule_BubbleOverlayDoesNotLockItsRowOrColumn()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateOverlayTile(302, 1, TileLogicType.BubbleOverlay));
            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 102, 101,
                102, 101, 102,
                101, 102, 101
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 302, 0,
                0, 0, 0
            };

            BoardModel board = CreateBoard(levelData, database);
            LineSlideMoveRule rule = new LineSlideMoveRule();

            Assert.That(rule.TryApply(board, new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right, 1, 1), out _), Is.True);
            Assert.That(rule.TryApply(board, new BoardMoveRequest(MoveAxis.Column, 1, LineSlideDirection.Down, 1, 1), out _), Is.True);
        }

        [Test]
        public void BombActivation_ClearsBubbleAndUnderlyingTile()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(151, TileLogicType.BombBooster),
                CreateOverlayTile(302, 1, TileLogicType.BubbleOverlay));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 151, 101,
                101, 101, 101
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 0, 0,
                0, 302, 0
            };

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(1, 1);
            CellModel bubbleCell = board.GetCell(1, 2);

            centerCell.Tile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op =>
                op.Cell.Equals(new BoardCellPosition(1, 2)) &&
                op.Layer == TileStackLayer.Overlay &&
                op.TileId == 302), Is.True);
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op =>
                op.Cell.Equals(new BoardCellPosition(1, 2)) &&
                op.Layer == TileStackLayer.Base &&
                op.TileId == 101), Is.True);
            Assert.That(bubbleCell.Overlay, Is.Null);
            Assert.That(bubbleCell.Tile, Is.Null);
        }

        [Test]
        public void BubbleCoveredBooster_CanActivateNormally()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(151, TileLogicType.BombBooster),
                CreateOverlayTile(302, 1, TileLogicType.BubbleOverlay));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 151, 101,
                101, 101, 101
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 302, 0,
                0, 0, 0
            };

            BoardModel board = CreateBoard(levelData, database);
            BoardMoveExecutionResult result = BoardResolutionService.ExecuteTileActivation(
                board,
                1,
                1,
                levelData,
                database,
                new System.Random(0));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.True);
        }

        [Test]
        public void LineSlideMoveRule_ChocolateOverlayMovesWithItsRowAndColumn()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateChocolateOverlayTile(303, 1));
            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 102, 101,
                102, 101, 102,
                101, 102, 101
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 303, 0,
                0, 0, 0
            };

            LineSlideMoveRule rule = new LineSlideMoveRule();
            BoardModel rowBoard = CreateBoard(levelData, database);
            BoardModel columnBoard = CreateBoard(levelData, database);
            BoardModel unaffectedBoard = CreateBoard(levelData, database);

            Assert.That(rule.TryApply(rowBoard, new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right, 1, 1), out _), Is.True);
            Assert.That(rule.TryApply(columnBoard, new BoardMoveRequest(MoveAxis.Column, 1, LineSlideDirection.Down, 1, 1), out _), Is.True);
            Assert.That(rule.TryApply(unaffectedBoard, new BoardMoveRequest(MoveAxis.Row, 0, LineSlideDirection.Right, 1, 0), out _), Is.True);
        }

        [Test]
        public void ResolveBoard_MatchAdjacentToChocolate_BreaksChocolateAndKeepsTile()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateNormalTile(103, AnimalTileId.Fox),
                CreateChocolateOverlayTile(303, 1));

            Match3LevelData levelData = CreateLevelData(3, 2, new[]
            {
                101, 101, 101,
                102, 102, 103
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 303, 0
            };

            BoardModel board = CreateBoard(levelData, database);

            BoardResolutionService.ResolveBoard(board, levelData, database, new System.Random(0));

            CellModel chocolateCell = board.GetCell(1, 1);
            Assert.That(chocolateCell.Overlay, Is.Null);
            Assert.That(chocolateCell.Tile, Is.Not.Null);
            Assert.That(chocolateCell.Tile.TileId, Is.EqualTo(102));
        }

        [Test]
        public void BombActivation_BreaksChocolateAndPreservesTile()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(151, TileLogicType.BombBooster),
                CreateChocolateOverlayTile(303, 1));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 151, 101,
                101, 101, 101
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 0, 0,
                0, 303, 0
            };

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(1, 1);
            CellModel chocolateCell = board.GetCell(1, 2);

            centerCell.Tile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.DamageOps.Exists(op =>
                op.Cell.Equals(new BoardCellPosition(1, 2)) &&
                op.Layer == TileStackLayer.Overlay &&
                op.TileId == 303), Is.True);
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op =>
                op.Cell.Equals(new BoardCellPosition(1, 2)) &&
                op.Layer == TileStackLayer.Overlay &&
                op.TileId == 303), Is.True);
            Assert.That(chocolateCell.Overlay, Is.Null);
            Assert.That(chocolateCell.Tile, Is.Not.Null);
            Assert.That(chocolateCell.Tile.TileId, Is.EqualTo(101));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op =>
                op.Cell.Equals(new BoardCellPosition(1, 2)) &&
                op.Layer == TileStackLayer.Base),
                Is.False);
        }

        [Test]
        public void ExecuteTileActivation_NoChocolateCleared_GrowsOneChocolateAtEndOfTurn()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateBoosterTile(151, TileLogicType.BombBooster),
                CreateChocolateOverlayTile(303, 1));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 102, 101,
                101, 101, 101,
                101, 101, 151
            });
            levelData.playableMask = new[]
            {
                true, true, true,
                false, true, true,
                true, true, true
            };
            levelData.overlayLayout = new[]
            {
                303, 0, 0,
                0, 0, 0,
                0, 0, 0
            };
            levelData.spawnableTileIds = new List<int> { 101, 102 };

            BoardModel board = CreateBoard(levelData, database);
            BoardMoveExecutionResult result = BoardResolutionService.ExecuteTileActivation(
                board,
                2,
                2,
                levelData,
                database,
                new System.Random(0));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.True);
            SpecialCreateOp createOp = null;
            for (int i = 0; i < result.PresentationTrace.Cascades.Count && createOp == null; i++)
            {
                for (int j = 0; j < result.PresentationTrace.Cascades[i].ClearPhase.SpecialCreateOps.Count; j++)
                {
                    SpecialCreateOp candidate = result.PresentationTrace.Cascades[i].ClearPhase.SpecialCreateOps[j];
                    if (candidate.ToTileId == 303)
                    {
                        createOp = candidate;
                        break;
                    }
                }
            }

            Assert.That(createOp, Is.Not.Null);
            Assert.That(createOp.ToTileId, Is.EqualTo(303));
            Assert.That(createOp.Layer, Is.EqualTo(TileStackLayer.Overlay));
            Assert.That(createOp.ReplaceSourceTileView, Is.False);
            Assert.That(createOp.Cell, Is.EqualTo(new BoardCellPosition(1, 0)));

            Assert.That(board.GetCell(0, 0).Overlay, Is.Not.Null);
            Assert.That(board.GetCell(0, 0).Overlay.TileId, Is.EqualTo(303));
            Assert.That(board.GetCell(1, 0).Overlay, Is.Not.Null);
            Assert.That(board.GetCell(1, 0).Overlay.TileId, Is.EqualTo(303));
        }

        [Test]
        public void ExecuteTileActivation_NoChocolateCleared_GrowsConfiguredNumberOfChocolatesAtEndOfTurn()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateBoosterTile(151, TileLogicType.BombBooster),
                CreateChocolateOverlayTile(303, 1, 2));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 102, 101,
                101, 101, 101,
                101, 101, 151
            });
            levelData.playableMask = new[]
            {
                true, true, true,
                false, true, true,
                true, true, true
            };
            levelData.overlayLayout = new[]
            {
                303, 0, 0,
                0, 0, 0,
                0, 0, 0
            };
            levelData.spawnableTileIds = new List<int> { 101, 102 };

            BoardModel board = CreateBoard(levelData, database);
            BoardMoveExecutionResult result = BoardResolutionService.ExecuteTileActivation(
                board,
                2,
                2,
                levelData,
                database,
                new System.Random(0));

            List<SpecialCreateOp> createOps = new List<SpecialCreateOp>();
            for (int i = 0; i < result.PresentationTrace.Cascades.Count; i++)
            {
                for (int j = 0; j < result.PresentationTrace.Cascades[i].ClearPhase.SpecialCreateOps.Count; j++)
                {
                    SpecialCreateOp candidate = result.PresentationTrace.Cascades[i].ClearPhase.SpecialCreateOps[j];
                    if (candidate.ToTileId == 303)
                    {
                        createOps.Add(candidate);
                    }
                }
            }

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(createOps.Count, Is.EqualTo(2));
            Assert.That(board.GetCell(0, 0).Overlay, Is.Not.Null);
            Assert.That(board.GetCell(0, 0).Overlay.TileId, Is.EqualTo(303));
            Assert.That(board.GetCell(1, 0).Overlay, Is.Not.Null);
            Assert.That(board.GetCell(1, 0).Overlay.TileId, Is.EqualTo(303));
            Assert.That(board.GetCell(2, 0).Overlay, Is.Not.Null);
            Assert.That(board.GetCell(2, 0).Overlay.TileId, Is.EqualTo(303));
        }

        [Test]
        public void ExecuteTileActivation_ChocolateGrowthIntervalTwo_WaitsOneTurnBeforeGrowing()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateBoosterTile(151, TileLogicType.BombBooster),
                CreateChocolateOverlayTile(303, 1, 1, 2));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 102, 101,
                101, 101, 101,
                101, 101, 151
            });
            levelData.playableMask = new[]
            {
                true, true, true,
                false, true, true,
                true, true, true
            };
            levelData.overlayLayout = new[]
            {
                303, 0, 0,
                0, 0, 0,
                0, 0, 0
            };
            levelData.spawnableTileIds = new List<int> { 101, 102 };

            BoardModel board = CreateBoard(levelData, database);

            BoardMoveExecutionResult firstResult = BoardResolutionService.ExecuteTileActivation(
                board,
                2,
                2,
                levelData,
                database,
                new System.Random(0));

            Assert.That(firstResult.IsAccepted, Is.True);
            Assert.That(firstResult.PresentationTrace.Cascades.TrueForAll(cascade =>
                cascade.ClearPhase.SpecialCreateOps.TrueForAll(op => op.ToTileId != 303)), Is.True);
            Assert.That(board.GetCell(1, 0).Overlay, Is.Null);

            BoardMoveExecutionResult secondResult = BoardResolutionService.ExecuteTileActivation(
                board,
                2,
                2,
                levelData,
                database,
                new System.Random(1));

            Assert.That(secondResult.IsAccepted, Is.True);
            Assert.That(secondResult.PresentationTrace.Cascades.Exists(cascade =>
                cascade.ClearPhase.SpecialCreateOps.Exists(op => op.ToTileId == 303)), Is.True);
            Assert.That(board.GetCell(1, 0).Overlay, Is.Not.Null);
            Assert.That(board.GetCell(1, 0).Overlay.TileId, Is.EqualTo(303));
        }

        [Test]
        public void ExecuteTileActivation_ChocolateCleared_DoesNotGrowReplacementChocolate()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(151, TileLogicType.BombBooster),
                CreateChocolateOverlayTile(303, 1));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 151, 101,
                101, 101, 101
            });
            levelData.overlayLayout = new[]
            {
                0, 0, 0,
                0, 0, 0,
                0, 303, 0
            };
            levelData.spawnableTileIds = new List<int> { 101 };

            BoardModel board = CreateBoard(levelData, database);
            BoardMoveExecutionResult result = BoardResolutionService.ExecuteTileActivation(
                board,
                1,
                1,
                levelData,
                database,
                new System.Random(0));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.PresentationTrace.Cascades.TrueForAll(cascade =>
                cascade.ClearPhase.SpecialCreateOps.TrueForAll(op => op.ToTileId != 303)), Is.True);
            Assert.That(board.GetCell(1, 2).Overlay, Is.Null);
        }

        [Test]
        public void ResolveBoard_DoesNotGrowChocolateOutsideTurnResolution()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateChocolateOverlayTile(303, 1));

            Match3LevelData levelData = CreateLevelData(4, 2, new[]
            {
                101, 102, 102, 101,
                101, 101, 101, 101
            });
            levelData.playableMask = new[]
            {
                true, true, true, true,
                false, true, true, true
            };
            levelData.overlayLayout = new[]
            {
                303, 0, 0, 0,
                0, 0, 0, 0
            };
            levelData.spawnableTileIds = new List<int> { 101, 102 };

            BoardModel board = CreateBoard(levelData, database);

            BoardResolutionService.ResolveBoard(board, levelData, database, new System.Random(0));

            Assert.That(board.GetCell(0, 0).Overlay, Is.Not.Null);
            Assert.That(board.GetCell(0, 0).Overlay.TileId, Is.EqualTo(303));
            Assert.That(board.GetCell(1, 0).Overlay, Is.Null);
        }

        [Test]
        public void LineSlideMoveRule_CakeTileCanMoveWithLine()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(201, TileLogicType.CakeDelivery),
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog));

            Match3LevelData levelData = CreateLevelData(3, 2, new[]
            {
                201, 101, 102,
                101, 101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            LineSlideMoveRule rule = new LineSlideMoveRule();

            Assert.That(rule.TryApply(board, new BoardMoveRequest(MoveAxis.Row, 0, LineSlideDirection.Right, 1, 0), out _), Is.True);
        }

        [Test]
        public void BoardMatchFinder_CakeDoesNotParticipateInMatches()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(201, TileLogicType.CakeDelivery),
                CreateNormalTile(101, AnimalTileId.Cat));

            Match3LevelData levelData = CreateLevelData(3, 1, new[] { 101, 201, 101 });
            BoardModel board = CreateBoard(levelData, database);

            BoardMatchAnalysis analysis = new BoardMatchFinder().Analyze(board);

            Assert.That(analysis.HasMatches, Is.False);
        }

        [Test]
        public void BombActivation_DoesNotDestroyCakeDirectly()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(201, TileLogicType.CakeDelivery),
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(151, TileLogicType.BombBooster));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 201, 101,
                101, 151, 101,
                101, 101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(1, 1);
            CellModel cakeCell = board.GetCell(1, 0);

            centerCell.Tile.Activate(board, centerCell, fxContext);

            Assert.That(cakeCell.Tile, Is.Not.Null);
            Assert.That(cakeCell.Tile.TileId, Is.EqualTo(201));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op => op.TileId == 201), Is.False);
        }

        [Test]
        public void ExecuteTileActivation_CakeAtBottomAfterGravity_IsDeliveredAndClearedForTarget()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(201, TileLogicType.CakeDelivery),
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(151, TileLogicType.BombBooster));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 201, 101,
                101, 151, 101,
                101, 101, 101
            });
            levelData.spawnableTileIds = new List<int> { 101 };

            BoardModel board = CreateBoard(levelData, database);
            BoardMoveExecutionResult result = BoardResolutionService.ExecuteTileActivation(
                board,
                1,
                1,
                levelData,
                database,
                new System.Random(0));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.PresentationTrace.Cascades.Exists(cascade =>
                cascade.ClearPhase.ClearOps.Exists(op =>
                    op.TileId == 201 &&
                    op.Layer == TileStackLayer.Base &&
                    op.Cell.Equals(new BoardCellPosition(1, 2)))), Is.True);
            Assert.That(board.GetCell(1, 2).Tile, Is.Not.Null);
            Assert.That(board.GetCell(1, 2).Tile.TileId, Is.EqualTo(101));
            Assert.That(AllCells(board, tileId: 201), Is.False);
        }

        [Test]
        public void ResolveBoard_DoesNotAutoDeliverCakeOutsideTurnResolution()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(201, TileLogicType.CakeDelivery),
                CreateNormalTile(101, AnimalTileId.Cat));

            Match3LevelData levelData = CreateLevelData(1, 2, new[]
            {
                101,
                201
            });
            levelData.spawnableTileIds = new List<int> { 101 };

            BoardModel board = CreateBoard(levelData, database);

            BoardResolutionService.ResolveBoard(board, levelData, database, new System.Random(0));

            Assert.That(board.GetCell(0, 1).Tile, Is.Not.Null);
            Assert.That(board.GetCell(0, 1).Tile.TileId, Is.EqualTo(201));
        }

        [Test]
        public void LineSlideMoveRule_StoneTileCanMoveWithLine()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(231, TileLogicType.StoneMechanic),
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog));

            Match3LevelData levelData = CreateLevelData(3, 2, new[]
            {
                231, 101, 102,
                101, 101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            LineSlideMoveRule rule = new LineSlideMoveRule();

            Assert.That(rule.TryApply(board, new BoardMoveRequest(MoveAxis.Row, 0, LineSlideDirection.Right, 1, 0), out _), Is.True);
        }

        [Test]
        public void BoardMatchFinder_StoneDoesNotParticipateInMatches()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(231, TileLogicType.StoneMechanic),
                CreateNormalTile(101, AnimalTileId.Cat));

            Match3LevelData levelData = CreateLevelData(3, 1, new[] { 101, 231, 101 });
            BoardModel board = CreateBoard(levelData, database);

            BoardMatchAnalysis analysis = new BoardMatchFinder().Analyze(board);

            Assert.That(analysis.HasMatches, Is.False);
        }

        [Test]
        public void ResolveBoard_MatchAdjacentToStone_DoesNotDestroyStone()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(231, TileLogicType.StoneMechanic),
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateNormalTile(103, AnimalTileId.Fox));

            Match3LevelData levelData = CreateLevelData(3, 2, new[]
            {
                101, 101, 101,
                102, 231, 103
            });
            levelData.spawnableTileIds = new List<int> { 102, 103 };

            BoardModel board = CreateBoard(levelData, database);

            BoardResolutionService.ResolveBoard(board, levelData, database, new System.Random(0));

            Assert.That(board.GetCell(1, 1).Tile, Is.Not.Null);
            Assert.That(board.GetCell(1, 1).Tile.TileId, Is.EqualTo(231));
        }

        [Test]
        public void BombActivation_DestroysStoneDirectly()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(231, TileLogicType.StoneMechanic),
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(151, TileLogicType.BombBooster));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 231, 101,
                101, 151, 101,
                101, 101, 101
            });

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel centerCell = board.GetCell(1, 1);
            CellModel stoneCell = board.GetCell(1, 0);

            centerCell.Tile.Activate(board, centerCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.DamageOps.Exists(op =>
                op.TileId == 231 &&
                op.Layer == TileStackLayer.Base &&
                op.Cell.Equals(new BoardCellPosition(1, 0))), Is.True);
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op =>
                op.TileId == 231 &&
                op.Layer == TileStackLayer.Base &&
                op.Cell.Equals(new BoardCellPosition(1, 0))), Is.True);
            Assert.That(stoneCell.Tile, Is.Null);
        }

        [Test]
        public void BoardModel_PopulateBoard_MechanicOverlayIdsArePromotedToTiles()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(201, TileLogicType.CakeDelivery),
                CreateMechanicTile(231, TileLogicType.StoneMechanic),
                CreateOverlayTile(301, 1),
                CreateOverlayTile(302, 1, TileLogicType.BubbleOverlay));

            Match3LevelData levelData = CreateLevelData(2, 1, new[] { 201, 231 });
            levelData.overlayLayout = new[]
            {
                301, 302
            };

            BoardModel board = CreateBoard(levelData, database);

            Assert.That(board.GetCell(0, 0).Tile, Is.Not.Null);
            Assert.That(board.GetCell(0, 0).Tile.TileId, Is.EqualTo(201));
            Assert.That(board.GetCell(0, 0).Overlay, Is.Null);
            Assert.That(board.GetCell(1, 0).Tile, Is.Not.Null);
            Assert.That(board.GetCell(1, 0).Tile.TileId, Is.EqualTo(231));
            Assert.That(board.GetCell(1, 0).Overlay, Is.Null);
        }

        [Test]
        public void ExecuteTileActivation_NoChocolateCleared_DoesNotGrowOntoTargetTile()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateMechanicTile(201, TileLogicType.CakeDelivery),
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(151, TileLogicType.BombBooster),
                CreateChocolateOverlayTile(303, 1));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 201, 0,
                0, 0, 101,
                101, 101, 151
            });
            levelData.playableMask = new[]
            {
                true, true, false,
                false, false, true,
                true, true, true
            };
            levelData.overlayLayout = new[]
            {
                303, 0, 0,
                0, 0, 0,
                0, 0, 0
            };
            levelData.spawnableTileIds = new List<int> { 101 };

            BoardModel board = CreateBoard(levelData, database);
            BoardMoveExecutionResult result = BoardResolutionService.ExecuteTileActivation(
                board,
                2,
                2,
                levelData,
                database,
                new System.Random(0));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.PresentationTrace.Cascades.Exists(cascade =>
                cascade.ClearPhase.SpecialCreateOps.Exists(op => op.ToTileId == 303)), Is.False);
            Assert.That(board.GetCell(1, 0).Tile, Is.Not.Null);
            Assert.That(board.GetCell(1, 0).Tile.TileId, Is.EqualTo(201));
            Assert.That(board.GetCell(1, 0).Overlay, Is.Null);
            Assert.That(board.GetCell(0, 0).Overlay, Is.Not.Null);
            Assert.That(board.GetCell(0, 0).Overlay.TileId, Is.EqualTo(303));
        }

        [Test]
        public void ExecuteChargedPlacement_ReplacesNormalTileWithoutConsumingMove()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(155, TileLogicType.ChargedSweepBooster));

            Match3LevelData levelData = CreateLevelData(1, 1, new[] { 101 });
            BoardModel board = CreateBoard(levelData, database);
            int movesBefore = board.RemainingMoves;

            BoardMoveExecutionResult result = BoardResolutionService.ExecuteChargedPlacement(
                board,
                0,
                0,
                155,
                database,
                new System.Random(0));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.Kind, Is.EqualTo(BoardExecutionKind.ChargedPlacement));
            Assert.That(result.PresentationTrace.Cascades.Count, Is.EqualTo(1));
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.SpecialCreateOps.Count, Is.EqualTo(1));
            Assert.That(board.RemainingMoves, Is.EqualTo(movesBefore));
            Assert.That(board.GetCell(0, 0).Tile, Is.Not.Null);
            Assert.That(board.GetCell(0, 0).Tile.TileId, Is.EqualTo(155));
        }

        [Test]
        public void ChargedSweepActivation_SelectsNormalAnimalAndRespectsBlockingOverlay()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(155, TileLogicType.ChargedSweepBooster),
                CreateMechanicTile(201, TileLogicType.CakeDelivery),
                CreateMechanicTile(231, TileLogicType.StoneMechanic),
                CreateOverlayTile(301, 1, TileLogicType.IceOverlay));

            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 0,
                231, 155, 201,
                0, 0, 0
            });
            levelData.overlayLayout = new[]
            {
                301, 0, 0,
                0, 0, 0,
                0, 0, 0
            };

            BoardModel board = CreateBoard(levelData, database);
            CascadeTrace cascadeTrace = new CascadeTrace();
            BoardFxContext fxContext = new BoardFxContext(cascadeTrace, new System.Random(0));
            CellModel sourceCell = board.GetCell(1, 1);

            sourceCell.Tile.Activate(board, sourceCell, fxContext);

            Assert.That(cascadeTrace.ClearPhase.ActivateOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.TargetSelectionOps.Count, Is.EqualTo(1));
            Assert.That(cascadeTrace.ClearPhase.TargetSelectionOps[0].TargetCell, Is.EqualTo(new BoardCellPosition(1, 0)));
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op => op.TileId == 155), Is.True);
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op =>
                op.TileId == 101 &&
                op.Layer == TileStackLayer.Base &&
                op.Cell.Equals(new BoardCellPosition(1, 0))), Is.True);
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op =>
                op.TileId == 101 &&
                op.Layer == TileStackLayer.Base &&
                op.Cell.Equals(new BoardCellPosition(0, 0))), Is.False);
            Assert.That(cascadeTrace.ClearPhase.ClearOps.Exists(op =>
                op.TileId == 301 &&
                op.Layer == TileStackLayer.Overlay &&
                op.Cell.Equals(new BoardCellPosition(0, 0))), Is.True);
            Assert.That(board.GetCell(0, 0).Tile, Is.Not.Null);
            Assert.That(board.GetCell(0, 0).Tile.TileId, Is.EqualTo(101));
        }

        [Test]
        public void ExecuteChargedCombo_ClearsFullBoardIncludingBlockersAndMechanics()
        {
            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(155, TileLogicType.ChargedSweepBooster),
                CreateOverlayTile(301, 1),
                CreateMechanicTile(201, TileLogicType.CakeDelivery));

            Match3LevelData levelData = CreateLevelData(2, 2, new[]
            {
                155, 155,
                201, 101
            });
            levelData.overlayLayout = new[]
            {
                0, 0,
                301, 0
            };
            levelData.spawnableTileIds = new List<int> { 101 };

            BoardModel board = CreateBoard(levelData, database);
            int movesBefore = board.RemainingMoves;

            BoardMoveExecutionResult result = BoardResolutionService.ExecuteChargedCombo(
                board,
                new BoardCellPosition(0, 0),
                new BoardCellPosition(1, 0),
                levelData,
                database,
                new System.Random(0));

            Assert.That(result.IsApplied, Is.True);
            Assert.That(result.IsAccepted, Is.True);
            Assert.That(result.Kind, Is.EqualTo(BoardExecutionKind.ChargedCombo));
            Assert.That(board.RemainingMoves, Is.EqualTo(movesBefore - 1));
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.ClearOps.Exists(op => op.TileId == 155), Is.True);
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.ClearOps.Exists(op => op.TileId == 201), Is.True);
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.ClearOps.Exists(op => op.TileId == 301), Is.True);
            Assert.That(result.PresentationTrace.Cascades[0].ClearPhase.ClearOps.Exists(op => op.TileId == 101), Is.True);
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
                tileLayout = layout,
                overlayLayout = new int[layout.Length],
                spawnableTileIds = new List<int>()
            };
        }

        private static BoardModel CreateBoard(Match3LevelData levelData, Match3TileDatabaseSO database)
        {
            BoardModel board = new BoardModel(levelData);
            board.PopulateBoard(levelData.underlayLayout, levelData.tileLayout, levelData.overlayLayout, database);
            return board;
        }

        private static Match3TileDatabaseSO CreateDatabase(params BoardContentDefinitionSO[] definitions)
        {
            Match3TileDatabaseSO database = ScriptableObject.CreateInstance<Match3TileDatabaseSO>();
            List<TileDefinitionSO> tileDefinitions = new List<TileDefinitionSO>();
            List<OverlayDefinitionSO> overlayDefinitions = new List<OverlayDefinitionSO>();
            List<UnderlayDefinitionSO> underlayDefinitions = new List<UnderlayDefinitionSO>();
            for (int i = 0; i < definitions.Length; i++)
            {
                switch (definitions[i])
                {
                    case TileDefinitionSO tileDefinition:
                        tileDefinitions.Add(tileDefinition);
                        break;
                    case OverlayDefinitionSO overlayDefinition:
                        overlayDefinitions.Add(overlayDefinition);
                        break;
                    case UnderlayDefinitionSO underlayDefinition:
                        underlayDefinitions.Add(underlayDefinition);
                        break;
                }
            }

            SetPrivateField(database, "tileDefinitions", tileDefinitions);
            SetPrivateField(database, "overlayDefinitions", overlayDefinitions);
            SetPrivateField(database, "underlayDefinitions", underlayDefinitions);
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

        private static OverlayDefinitionSO CreateOverlayTile(int tileId, int defaultHp, TileLogicType logicType = TileLogicType.IceOverlay, int growthPerTurn = 1, int growthTurnInterval = 1)
        {
            OverlayDefinitionSO tile = logicType == TileLogicType.BubbleOverlay
                ? ScriptableObject.CreateInstance<BubbleOverlayDefinitionSO>()
                : ScriptableObject.CreateInstance<IceOverlayDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "defaultHP", defaultHp);
            SetPrivateField(tile, "canSpawnOnRefill", false);
            SetPrivateField(tile, "spawnWeight", 0);
            return tile;
        }

        private static ChocolateOverlayDefinitionSO CreateChocolateOverlayTile(
            int tileId,
            int defaultHp,
            int growthPerTurn = 1,
            int growthTurnInterval = 1)
        {
            ChocolateOverlayDefinitionSO tile = ScriptableObject.CreateInstance<ChocolateOverlayDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "defaultHP", defaultHp);
            SetPrivateField(tile, "growthPerTurn", growthPerTurn);
            SetPrivateField(tile, "growthTurnInterval", growthTurnInterval);
            SetPrivateField(tile, "canSpawnOnRefill", false);
            SetPrivateField(tile, "spawnWeight", 0);
            return tile;
        }

        private static TileDefinitionSO CreateMechanicTile(int tileId, TileLogicType logicType)
        {
            TileDefinitionSO tile = logicType == TileLogicType.StoneMechanic
                ? ScriptableObject.CreateInstance<StoneTileDefinitionSO>()
                : ScriptableObject.CreateInstance<DeliveryTileDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "canSpawnOnRefill", false);
            SetPrivateField(tile, "spawnWeight", 0);
            return tile;
        }

        private static bool AllCells(BoardModel board, int tileId)
        {
            foreach (CellModel cell in board.GetAllCells())
            {
                if (cell?.Tile != null && cell.Tile.TileId == tileId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int[] CaptureBoard(BoardModel board)
        {
            List<int> ids = new List<int>();
            foreach (CellModel cell in board.GetAllCells())
            {
                ids.Add(cell.Tile != null ? cell.Tile.TileId : 0);
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

