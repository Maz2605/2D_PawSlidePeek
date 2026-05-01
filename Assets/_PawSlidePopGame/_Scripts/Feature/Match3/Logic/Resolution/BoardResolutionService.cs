using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Match;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Rules;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution
{
    public static class BoardResolutionService
    {
        public static BoardMoveExecutionResult ExecuteMove(
            BoardModel board,
            BoardMoveRequest moveRequest,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            BoardMoveExecutionResult executionResult = new BoardMoveExecutionResult();
            BoardRuleSet activeRuleSet = ruleSet ?? BoardRuleSet.Default;
            if (activeRuleSet.MoveRule == null || activeRuleSet.MatchRule == null)
            {
                return executionResult;
            }

            if (!activeRuleSet.MoveRule.TryApply(board, moveRequest, out BoardMoveContext moveContext))
            {
                return executionResult;
            }

            executionResult.IsApplied = true;

            BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder(moveRequest);
            traceBuilder.RecordMoveAttempt(BuildMoveTrace(moveContext));

            BoardResolutionResult resolutionResult = new BoardResolutionResult();
            int scoreBefore = board.CurrentScore;
            bool hasTriggeredRule = ApplyPostMoveRules(board, moveContext, resolutionResult, activeRuleSet.PostMoveRules);
            BoardMatchAnalysis initialAnalysis = activeRuleSet.MatchRule.Analyze(board);

            if (!initialAnalysis.HasMatches && !hasTriggeredRule)
            {
                traceBuilder.RecordRollback(moveRequest, ReverseOperations(traceBuilder.Build().MoveAttempt.TravelOps));
                moveContext.Snapshot.Restore();
                executionResult.IsAccepted = false;
                executionResult.ResolutionResult = resolutionResult;
                executionResult.PresentationTrace = traceBuilder.Build();
                return executionResult;
            }

            if (board.CanConsumeMove())
            {
                board.ConsumeMove();
            }

            executionResult.IsAccepted = true;
            resolutionResult.IsMoveAccepted = true;
            resolutionResult.HasTriggeredPostMoveRule = hasTriggeredRule;
            ResolveBoard(
                board,
                levelData,
                tileDatabase,
                random,
                resolutionResult,
                activeRuleSet.MatchRule,
                initialAnalysis.HasMatches ? initialAnalysis : null,
                traceBuilder,
                moveRequest);
            resolutionResult.ScoreDelta = board.CurrentScore - scoreBefore;

            executionResult.ResolutionResult = resolutionResult;
            executionResult.PresentationTrace = traceBuilder.Build();
            return executionResult;
        }

        public static bool TryResolveMove(
            BoardModel board,
            BoardMoveRequest moveRequest,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            out BoardResolutionResult result,
            BoardRuleSet ruleSet = null)
        {
            BoardMoveExecutionResult executionResult = ExecuteMove(board, moveRequest, levelData, tileDatabase, random, ruleSet);
            result = executionResult.ResolutionResult;
            return executionResult.IsAccepted;
        }

        public static BoardMoveExecutionResult ExecuteTileActivation(
            BoardModel board,
            int x,
            int y,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            BoardMoveExecutionResult executionResult = new BoardMoveExecutionResult();
            BoardRuleSet activeRuleSet = ruleSet ?? BoardRuleSet.Default;
            if (board == null || levelData == null || tileDatabase == null || activeRuleSet.MatchRule == null)
            {
                return executionResult;
            }

            CellModel cell = board.GetCell(x, y);
            TileModel tile = cell?.CurrentTile;
            if (cell == null || !cell.IsPlayable || tile == null || tile.TileKind != Core.Enum.TileKind.Booster)
            {
                return executionResult;
            }

            if (!board.CanConsumeMove())
            {
                return executionResult;
            }

            executionResult.IsApplied = true;
            executionResult.IsAccepted = true;

            board.ConsumeMove();

            BoardPresentationTraceBuilder traceBuilder = new BoardPresentationTraceBuilder();
            BoardResolutionResult resolutionResult = new BoardResolutionResult
            {
                IsMoveAccepted = true
            };

            int scoreBefore = board.CurrentScore;
            CascadeTrace activationCascade = traceBuilder.BeginCascade();
            BoardFxContext fxContext = new BoardFxContext(activationCascade, random);

            tile.Activate(board, cell, fxContext);
            resolutionResult.CascadesResolved += CountCascadeAsResolved(activationCascade);
            resolutionResult.ClearedTiles += activationCascade.ClearPhase.ClearOps.Count;

            BoardGravityService.Apply(board, activationCascade.GravityPhase.TravelOps);
            resolutionResult.SpawnedTiles += BoardRefillService.Apply(board, levelData, tileDatabase, random, activationCascade.RefillPhase.SpawnOps);

            ResolveBoard(board, levelData, tileDatabase, random, resolutionResult, activeRuleSet.MatchRule, null, traceBuilder, null);
            resolutionResult.ScoreDelta = board.CurrentScore - scoreBefore;

            executionResult.ResolutionResult = resolutionResult;
            executionResult.PresentationTrace = traceBuilder.Build();
            return executionResult;
        }

        public static BoardResolutionResult ResolveBoard(
            BoardModel board,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardRuleSet ruleSet = null)
        {
            BoardRuleSet activeRuleSet = ruleSet ?? BoardRuleSet.Default;
            int scoreBefore = board.CurrentScore;
            BoardResolutionResult result = new BoardResolutionResult();
            ResolveBoard(board, levelData, tileDatabase, random, result, activeRuleSet.MatchRule, null, null, null);
            result.ScoreDelta = board.CurrentScore - scoreBefore;
            return result;
        }

        private static void ResolveBoard(
            BoardModel board,
            Match3LevelData levelData,
            Match3TileDatabaseSO tileDatabase,
            Random random,
            BoardResolutionResult result,
            IBoardMatchRule matchRule,
            BoardMatchAnalysis startingAnalysis,
            BoardPresentationTraceBuilder traceBuilder,
            BoardMoveRequest? sourceMoveRequest)
        {
            BoardMatchAnalysis matchAnalysis = startingAnalysis ?? matchRule.Analyze(board);
            while (matchAnalysis.HasMatches)
            {
                CascadeTrace cascadeTrace = traceBuilder?.BeginCascade();
                BoardFxContext fxContext = cascadeTrace != null ? new BoardFxContext(cascadeTrace, random) : null;
                BoardMoveRequest decisionMoveRequest = sourceMoveRequest ?? new BoardMoveRequest(Core.Enum.MoveAxis.Row, -1, Core.Enum.LineSlideDirection.Left, -1, -1);
                List<SpecialSpawnDecision> decisions = SpecialCreationService.CreateDecisions(matchAnalysis, decisionMoveRequest, tileDatabase);
                result.CascadesResolved++;
                result.ClearedTiles += ClearMatches(board, matchAnalysis, decisions, tileDatabase, fxContext);
                BoardGravityService.Apply(board, cascadeTrace?.GravityPhase.TravelOps);
                result.SpawnedTiles += BoardRefillService.Apply(board, levelData, tileDatabase, random, cascadeTrace?.RefillPhase.SpawnOps);
                matchAnalysis = matchRule.Analyze(board);
            }
        }

        private static int ClearMatches(
            BoardModel board,
            BoardMatchAnalysis matchAnalysis,
            IReadOnlyList<SpecialSpawnDecision> decisions,
            Match3TileDatabaseSO tileDatabase,
            BoardFxContext fxContext)
        {
            Dictionary<int, SpecialSpawnDecision> decisionsByGroupId = new Dictionary<int, SpecialSpawnDecision>();
            if (decisions != null)
            {
                for (int i = 0; i < decisions.Count; i++)
                {
                    decisionsByGroupId[decisions[i].GroupId] = decisions[i];
                }
            }

            int clearedTiles = 0;
            List<PendingSpecialCreate> pendingCreates = new List<PendingSpecialCreate>();

            for (int groupIndex = 0; groupIndex < matchAnalysis.Groups.Count; groupIndex++)
            {
                MatchGroup group = matchAnalysis.Groups[groupIndex];
                decisionsByGroupId.TryGetValue(group.Id, out SpecialSpawnDecision decision);

                for (int cellIndex = 0; cellIndex < group.Cells.Count; cellIndex++)
                {
                    CellModel cell = group.Cells[cellIndex];
                    if (cell?.CurrentTile == null)
                    {
                        continue;
                    }

                    if (decision != null && cell == decision.SpawnCell)
                    {
                        pendingCreates.Add(new PendingSpecialCreate(decision, cell.CurrentTile, cell));
                        if (cell.CurrentTile.CanMatch())
                        {
                            board.AddScore(10);
                        }

                        continue;
                    }

                    clearedTiles++;
                    cell.CurrentTile.Match(board, cell, fxContext);
                }
            }

            for (int i = 0; i < pendingCreates.Count; i++)
            {
                PendingSpecialCreate pendingCreate = pendingCreates[i];
                BoosterTileDefinitionSO boosterDefinition = tileDatabase != null
                    ? tileDatabase.GetBoosterDefinition(pendingCreate.Decision.LogicType)
                    : null;
                if (boosterDefinition == null || pendingCreate.Cell == null)
                {
                    continue;
                }

                TileModel createdTile = board.CreateTileFromDefinitionId(boosterDefinition.TileId, tileDatabase);
                if (createdTile == null)
                {
                    continue;
                }

                board.SetTile(pendingCreate.Cell, createdTile);
                fxContext?.RecordSpecialCreate(pendingCreate.SourceTile, createdTile, pendingCreate.Cell);
            }

            return clearedTiles;
        }

        private static bool ApplyPostMoveRules(
            BoardModel board,
            BoardMoveContext moveContext,
            BoardResolutionResult result,
            IReadOnlyList<IBoardPostMoveRule> postMoveRules)
        {
            bool hasTriggeredRule = false;
            if (postMoveRules == null)
            {
                return false;
            }

            for (int i = 0; i < postMoveRules.Count; i++)
            {
                IBoardPostMoveRule rule = postMoveRules[i];
                if (rule != null && rule.TryApply(board, moveContext, result))
                {
                    hasTriggeredRule = true;
                }
            }

            return hasTriggeredRule;
        }

        private static List<TileTravelOp> BuildMoveTrace(BoardMoveContext moveContext)
        {
            List<TileTravelOp> operations = new List<TileTravelOp>();
            if (moveContext?.AffectedCells == null || moveContext.Snapshot == null)
            {
                return operations;
            }

            int count = moveContext.AffectedCells.Count;
            if (count <= 0)
            {
                return operations;
            }

            int normalizedStep = moveContext.Request.GetRotationStep() % count;
            if (normalizedStep < 0)
            {
                normalizedStep += count;
            }

            for (int i = 0; i < count; i++)
            {
                TileModel tile = moveContext.Snapshot.GetTile(i);
                if (tile == null)
                {
                    continue;
                }

                CellModel sourceCell = moveContext.Snapshot.GetCell(i);
                CellModel destinationCell = moveContext.AffectedCells[(i + normalizedStep) % count];
                operations.Add(new TileTravelOp
                {
                    TileInstanceId = tile.InstanceId,
                    TileId = tile.TileId,
                    FromCell = new BoardCellPosition(sourceCell.X, sourceCell.Y),
                    ToCell = new BoardCellPosition(destinationCell.X, destinationCell.Y),
                    Distance = 1,
                    IsWrapAround = IsWrapAround(moveContext.Request, sourceCell, destinationCell)
                });
            }

            return operations;
        }

        private static List<TileTravelOp> ReverseOperations(IReadOnlyList<TileTravelOp> operations)
        {
            List<TileTravelOp> reversed = new List<TileTravelOp>();
            if (operations == null)
            {
                return reversed;
            }

            for (int i = 0; i < operations.Count; i++)
            {
                TileTravelOp operation = operations[i];
                reversed.Add(new TileTravelOp
                {
                    TileInstanceId = operation.TileInstanceId,
                    TileId = operation.TileId,
                    FromCell = operation.ToCell,
                    ToCell = operation.FromCell,
                    Distance = operation.Distance,
                    IsWrapAround = operation.IsWrapAround
                });
            }

            return reversed;
        }

        private static bool IsWrapAround(BoardMoveRequest request, CellModel sourceCell, CellModel destinationCell)
        {
            if (sourceCell == null || destinationCell == null)
            {
                return false;
            }

            if (request.Axis == Core.Enum.MoveAxis.Row)
            {
                return Math.Abs(sourceCell.X - destinationCell.X) > 1;
            }

            return Math.Abs(sourceCell.Y - destinationCell.Y) > 1;
        }

        private static int CountCascadeAsResolved(CascadeTrace cascadeTrace)
        {
            if (cascadeTrace == null)
            {
                return 0;
            }

            return cascadeTrace.ClearPhase.ActivateOps.Count > 0 ||
                   cascadeTrace.ClearPhase.TargetSelectionOps.Count > 0 ||
                   cascadeTrace.ClearPhase.DamageOps.Count > 0 ||
                   cascadeTrace.ClearPhase.ClearOps.Count > 0 ||
                   cascadeTrace.ClearPhase.SpecialCreateOps.Count > 0
                ? 1
                : 0;
        }

        private static int CompareCells(CellModel left, CellModel right)
        {
            int byY = left.Y.CompareTo(right.Y);
            return byY != 0 ? byY : left.X.CompareTo(right.X);
        }

        private readonly struct PendingSpecialCreate
        {
            public SpecialSpawnDecision Decision { get; }
            public TileModel SourceTile { get; }
            public CellModel Cell { get; }

            public PendingSpecialCreate(SpecialSpawnDecision decision, TileModel sourceTile, CellModel cell)
            {
                Decision = decision;
                SourceTile = sourceTile;
                Cell = cell;
            }
        }
    }
}
