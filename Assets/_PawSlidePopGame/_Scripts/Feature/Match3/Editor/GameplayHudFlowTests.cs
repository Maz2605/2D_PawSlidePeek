using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using NUnit.Framework;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Editor
{
    public class GameplayHudFlowTests
    {
        [Test]
        public void ObjectiveTracker_ApplyExecutionResult_UpdatesMatchingTargetsAndCapsAtRequiredCount()
        {
            Match3LevelData levelData = new Match3LevelData
            {
                targets = new List<LevelTargetData>
                {
                    new LevelTargetData(101, 2),
                    new LevelTargetData(202, 1)
                }
            };

            Match3ObjectiveTracker tracker = new Match3ObjectiveTracker(levelData, null);
            BoardMoveExecutionResult result = CreateExecutionResult(101, 202, 101, 999);

            List<TargetProgressChangedPayload> changes = tracker.ApplyExecutionResult(result);

            Assert.That(changes.Count, Is.EqualTo(2));
            Assert.That(tracker.Targets[0].currentCount, Is.EqualTo(2));
            Assert.That(tracker.Targets[0].isCompleted, Is.True);
            Assert.That(tracker.Targets[1].currentCount, Is.EqualTo(1));
            Assert.That(tracker.Targets[1].isCompleted, Is.True);
            Assert.That(tracker.AreAllTargetsComplete, Is.True);
        }

        [Test]
        public void GameplayHudSnapshotBuilder_Build_UsesBoardAndLevelState()
        {
            Match3LevelData levelData = new Match3LevelData
            {
                displayLevelNumber = 7,
                movesLimit = 18,
                width = 1,
                height = 1,
                starScoreThresholds = new[] { 50, 100, 150 },
                targets = new List<LevelTargetData>
                {
                    new LevelTargetData(101, 3)
                }
            };

            BoardModel board = new BoardModel(levelData);
            board.AddScore(120);
            board.ConsumeMove();

            Match3ObjectiveTracker tracker = new Match3ObjectiveTracker(levelData, null);
            tracker.ApplyExecutionResult(CreateExecutionResult(101, 101));

            GameplayHudSnapshot snapshot = GameplayHudSnapshotBuilder.Build(levelData, board, tracker);

            Assert.That(snapshot.levelNumber, Is.EqualTo(7));
            Assert.That(snapshot.remainingMoves, Is.EqualTo(17));
            Assert.That(snapshot.currentScore, Is.EqualTo(120));
            Assert.That(snapshot.reachedStars, Is.EqualTo(2));
            Assert.That(snapshot.targets.Count, Is.EqualTo(1));
            Assert.That(snapshot.targets[0].currentCount, Is.EqualTo(2));
            Assert.That(snapshot.targets[0].requiredCount, Is.EqualTo(3));
        }

        [Test]
        public void Match3LevelData_GetSafeStarThresholds_ReturnsMonotonicPositiveValues()
        {
            Match3LevelData levelData = new Match3LevelData
            {
                starScoreThresholds = new[] { -10, 80, 40, 120 }
            };

            int[] thresholds = levelData.GetSafeStarThresholds();

            Assert.That(thresholds, Is.EqualTo(new[] { 0, 80, 80, 120 }));
        }

        private static BoardMoveExecutionResult CreateExecutionResult(params int[] clearedTileIds)
        {
            BoardMoveExecutionResult result = new BoardMoveExecutionResult
            {
                IsApplied = true,
                IsAccepted = true,
                PresentationTrace = new BoardPresentationTrace()
            };

            CascadeTrace cascade = new CascadeTrace();
            for (int i = 0; i < clearedTileIds.Length; i++)
            {
                cascade.ClearPhase.ClearOps.Add(new TileClearOp
                {
                    TileId = clearedTileIds[i]
                });
            }

            result.PresentationTrace.Cascades.Add(cascade);
            return result;
        }
    }
}
