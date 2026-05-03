using System.Collections.Generic;
using System.Reflection;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Data.Events.Payloads;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Flow;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Feature.Match3.Presenter;
using _PawSlidePopGame._Scripts.UI.Manager;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Editor
{
    public class GameplayHudFlowTests
    {
        [SetUp]
        public void SetUp()
        {
            EventManager<LogicGameEvent>.ClearAll();
            EventManager<VisualGameEvent>.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            EventManager<LogicGameEvent>.ClearAll();
            EventManager<VisualGameEvent>.ClearAll();

            foreach (var manager in Object.FindObjectsByType<GameFlowManager>(FindObjectsSortMode.None))
            {
                if (manager != null)
                {
                    Object.DestroyImmediate(manager.gameObject);
                }
            }

            foreach (var uiManager in Object.FindObjectsByType<UIManager>(FindObjectsSortMode.None))
            {
                if (uiManager != null)
                {
                    Object.DestroyImmediate(uiManager.gameObject);
                }
            }
        }

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
        public void ObjectiveTracker_ApplyExecutionResult_EmitsSequentialPerItemProgress()
        {
            Match3LevelData levelData = new Match3LevelData
            {
                targets = new List<LevelTargetData>
                {
                    new LevelTargetData(101, 3)
                }
            };

            Match3ObjectiveTracker tracker = new Match3ObjectiveTracker(levelData, null);
            BoardMoveExecutionResult result = CreateExecutionResult(101, 101, 101);

            List<TargetProgressChangedPayload> changes = tracker.ApplyExecutionResult(result);

            Assert.That(changes.Count, Is.EqualTo(3));
            Assert.That(changes[0].PreviousCount, Is.EqualTo(0));
            Assert.That(changes[0].CurrentCount, Is.EqualTo(1));
            Assert.That(changes[1].PreviousCount, Is.EqualTo(1));
            Assert.That(changes[1].CurrentCount, Is.EqualTo(2));
            Assert.That(changes[2].PreviousCount, Is.EqualTo(2));
            Assert.That(changes[2].CurrentCount, Is.EqualTo(3));
        }

        [Test]
        public void ObjectiveTracker_ApplyExecutionResult_DoesNotEmitOverflowOrNonTargetChanges()
        {
            Match3LevelData levelData = new Match3LevelData
            {
                targets = new List<LevelTargetData>
                {
                    new LevelTargetData(101, 2)
                }
            };

            Match3ObjectiveTracker tracker = new Match3ObjectiveTracker(levelData, null);
            BoardMoveExecutionResult result = CreateExecutionResult(999, 101, 101, 101);

            List<TargetProgressChangedPayload> changes = tracker.ApplyExecutionResult(result);

            Assert.That(changes.Count, Is.EqualTo(2));
            Assert.That(tracker.Targets[0].currentCount, Is.EqualTo(2));
            Assert.That(tracker.Targets[0].isCompleted, Is.True);
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
                starScoreThresholds = new[] { 50, 100, 150, 200 },
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

        [TestCase(0, 0)]
        [TestCase(50, 1)]
        [TestCase(120, 2)]
        [TestCase(175, 3)]
        [TestCase(250, 4)]
        public void GameplayHudSnapshotBuilder_CountReachedStars_SupportsFourThresholds(int score, int expectedStars)
        {
            int reachedStars = GameplayHudSnapshotBuilder.CountReachedStars(score, new[] { 50, 100, 150, 200 });

            Assert.That(reachedStars, Is.EqualTo(expectedStars));
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

        [Test]
        public void Match3LevelData_GetSafeStarThresholds_ReturnsFourFallbackThresholdsWhenMissing()
        {
            Match3LevelData levelData = new Match3LevelData
            {
                starScoreThresholds = null
            };

            int[] thresholds = levelData.GetSafeStarThresholds();

            Assert.That(thresholds, Is.EqualTo(new[] { 100, 250, 500, 1500 }));
        }

        [Test]
        public void GameFlowManager_RequestMove_AcceptedMovePublishesMovesChangedImmediately()
        {
            Match3LevelData levelData = CreateLevelData(3, 4, new[]
            {
                101, 102, 101,
                103, 101, 102,
                102, 103, 101,
                101, 103, 102
            });
            levelData.spawnableTileIds = new List<int> { 101, 102, 103 };

            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateNormalTile(103, AnimalTileId.Fox));

            GameFlowManager flowManager = CreateFlowManager(levelData, database);
            RemainingMovesChangedPayload publishedPayload = default;
            int publishCount = 0;
            EventManager<LogicGameEvent>.AddListener<RemainingMovesChangedPayload>(
                LogicGameEvent.GameplayMovesChanged,
                payload =>
                {
                    publishedPayload = payload;
                    publishCount++;
                });

            BoardMoveExecutionResult result = flowManager.RequestMove(
                new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right, 1, 1));

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(publishCount, Is.EqualTo(1));
            Assert.That(publishedPayload.PreviousMoves, Is.EqualTo(20));
            Assert.That(publishedPayload.CurrentMoves, Is.EqualTo(19));
        }

        [Test]
        public void GameFlowManager_RequestMove_InvalidMoveDoesNotPublishMovesChanged()
        {
            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 102, 103,
                102, 103, 101,
                103, 101, 102
            });

            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateNormalTile(102, AnimalTileId.Dog),
                CreateNormalTile(103, AnimalTileId.Fox));

            GameFlowManager flowManager = CreateFlowManager(levelData, database);
            int publishCount = 0;
            EventManager<LogicGameEvent>.AddListener<RemainingMovesChangedPayload>(
                LogicGameEvent.GameplayMovesChanged,
                _ => publishCount++);

            BoardMoveExecutionResult result = flowManager.RequestMove(
                new BoardMoveRequest(MoveAxis.Row, 1, LineSlideDirection.Right, 1, 1));

            Assert.That(result.IsAccepted, Is.False);
            Assert.That(publishCount, Is.EqualTo(0));
        }

        [Test]
        public void GameFlowManager_RequestTileActivation_PublishesMoveChangedImmediately()
        {
            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 202, 101,
                101, 101, 101
            });
            levelData.spawnableTileIds = new List<int> { 101 };

            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(202, TileLogicType.CrossBomb));

            GameFlowManager flowManager = CreateFlowManager(levelData, database);
            RemainingMovesChangedPayload publishedPayload = default;
            int publishCount = 0;
            EventManager<LogicGameEvent>.AddListener<RemainingMovesChangedPayload>(
                LogicGameEvent.GameplayMovesChanged,
                payload =>
                {
                    publishedPayload = payload;
                    publishCount++;
                });

            BoardMoveExecutionResult result = flowManager.RequestTileActivation(1, 1);

            Assert.That(result.IsAccepted, Is.True);
            Assert.That(publishCount, Is.EqualTo(1));
            Assert.That(publishedPayload.PreviousMoves, Is.EqualTo(20));
            Assert.That(publishedPayload.CurrentMoves, Is.EqualTo(19));
        }

        [Test]
        public void GameFlowManager_PlaybackCallbacks_PublishScoreTargetAndCompletionEvents()
        {
            Match3LevelData levelData = CreateLevelData(3, 3, new[]
            {
                101, 101, 101,
                101, 202, 101,
                101, 101, 101
            });
            levelData.spawnableTileIds = new List<int> { 101 };
            levelData.targets = new List<LevelTargetData> { new LevelTargetData(101, 1) };
            levelData.starScoreThresholds = new[] { 10, 50, 100, 150 };

            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, AnimalTileId.Cat),
                CreateBoosterTile(202, TileLogicType.CrossBomb));

            GameFlowManager flowManager = CreateFlowManager(levelData, database);
            BoardMoveExecutionResult result = flowManager.RequestTileActivation(1, 1);
            CascadeTrace cascade = result.PresentationTrace.Cascades[0];
            ScoreChangedPayload scorePayload = default;
            TargetProgressChangedPayload targetPayload = default;
            TargetCollectedFxPayload collectedPayload = default;
            StarReachedPayload starPayload = default;
            int scoreEventCount = 0;
            int completionEventCount = 0;

            EventManager<LogicGameEvent>.AddListener<ScoreChangedPayload>(
                LogicGameEvent.GameplayScoreChanged,
                payload =>
                {
                    scorePayload = payload;
                    scoreEventCount++;
                });
            EventManager<VisualGameEvent>.AddListener<TargetProgressChangedPayload>(
                VisualGameEvent.TopHudTargetProgressFx,
                payload => targetPayload = payload);
            EventManager<VisualGameEvent>.AddListener<TargetCollectedFxPayload>(
                VisualGameEvent.TopHudTargetCollectedFx,
                payload => collectedPayload = payload);
            EventManager<VisualGameEvent>.AddListener<StarReachedPayload>(
                VisualGameEvent.TopHudStarReachedFx,
                payload => starPayload = payload);
            EventManager<VisualGameEvent>.AddListener(
                VisualGameEvent.TopHudTargetsCompletedFx,
                () => completionEventCount++);

            flowManager.NotifyScoreGainedDuringPlayback(cascade.ClearPhase.ScoreGainOps[0]);
            flowManager.NotifyTileClearedDuringPlayback(cascade.ClearPhase.ClearOps[1], new Vector3(1f, 2f, 0f));

            Assert.That(scoreEventCount, Is.EqualTo(1));
            Assert.That(scorePayload.PreviousScore, Is.EqualTo(0));
            Assert.That(scorePayload.CurrentScore, Is.EqualTo(cascade.ClearPhase.ScoreGainOps[0].RunningScore));
            Assert.That(starPayload.StarIndex, Is.EqualTo(1));
            Assert.That(targetPayload.TileId, Is.EqualTo(101));
            Assert.That(targetPayload.CurrentCount, Is.EqualTo(1));
            Assert.That(collectedPayload.WorldPosition, Is.EqualTo(new Vector3(1f, 2f, 0f)));
            Assert.That(completionEventCount, Is.EqualTo(1));
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

        private static GameFlowManager CreateFlowManager(Match3LevelData levelData, Match3TileDatabaseSO database)
        {
            GameObject root = new GameObject("GameFlowManagerTests");
            Match3GameManager gameManager = root.AddComponent<Match3GameManager>();
            GameFlowManager flowManager = root.AddComponent<GameFlowManager>();

            Match3LevelDefinitionSO levelDefinition = ScriptableObject.CreateInstance<Match3LevelDefinitionSO>();
            SetPrivateField(levelDefinition, "levelData", levelData);
            SetPrivateField(gameManager, "levelDefinition", levelDefinition);
            SetPrivateField(gameManager, "tileDatabase", database);
            SetPrivateField(gameManager, "resolveBoardOnStart", false);
            SetPrivateField(flowManager, "gameManager", gameManager);
            SetPrivateField(flowManager, "autoStartFlow", false);

            flowManager.EnterGameplay();
            return flowManager;
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
