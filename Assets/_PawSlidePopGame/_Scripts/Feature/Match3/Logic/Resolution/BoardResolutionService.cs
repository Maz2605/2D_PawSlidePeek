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
            HashSet<CellModel> initialMatches = activeRuleSet.MatchRule.FindMatchedCells(board);

            if (initialMatches.Count == 0 && !hasTriggeredRule)
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
                initialMatches.Count > 0 ? initialMatches : null,
                traceBuilder);
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
            ResolveBoard(board, levelData, tileDatabase, random, result, activeRuleSet.MatchRule, null, null);
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
            HashSet<CellModel> startingMatches,
            BoardPresentationTraceBuilder traceBuilder)
        {
            HashSet<CellModel> matches = startingMatches ?? matchRule.FindMatchedCells(board);
            while (matches.Count > 0)
            {
                CascadeTrace cascadeTrace = traceBuilder?.BeginCascade();
                BoardFxContext fxContext = cascadeTrace != null ? new BoardFxContext(cascadeTrace) : null;
                result.CascadesResolved++;
                result.ClearedTiles += ClearMatches(board, matches, fxContext);
                BoardGravityService.Apply(board, cascadeTrace?.GravityPhase.TravelOps);
                result.SpawnedTiles += BoardRefillService.Apply(board, levelData, tileDatabase, random, cascadeTrace?.RefillPhase.SpawnOps);
                matches = matchRule.FindMatchedCells(board);
            }
        }

        private static int ClearMatches(BoardModel board, HashSet<CellModel> matches, BoardFxContext fxContext)
        {
            int clearedTiles = 0;
            List<CellModel> orderedMatches = new List<CellModel>(matches);
            orderedMatches.Sort(CompareCells);

            foreach (CellModel cell in orderedMatches)
            {
                if (cell?.CurrentTile == null)
                {
                    continue;
                }

                clearedTiles++;
                cell.CurrentTile.Match(board, cell, fxContext);
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

        private static int CompareCells(CellModel left, CellModel right)
        {
            int byY = left.Y.CompareTo(right.Y);
            return byY != 0 ? byY : left.X.CompareTo(right.X);
        }
    }
}
