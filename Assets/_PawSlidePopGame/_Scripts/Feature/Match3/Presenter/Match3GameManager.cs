using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Resolution;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Rules;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Presenter
{
    public class Match3GameManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private Match3LevelDefinitionSO levelDefinition;
        [SerializeField] private Match3TileDatabaseSO tileDatabase;

        [Header("Runtime Config")]
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private bool resolveBoardOnStart = true;

        private BoardModel _board;
        private BoardRuleSet _ruleSet;
        private System.Random _random;
        private BoardResolutionResult _lastMoveResult;
        private BoardMoveExecutionResult _lastExecutionResult;
        private bool _isInitialized;

        public BoardModel Board => _board;
        public Match3LevelData LevelData => levelDefinition != null ? levelDefinition.LevelData : null;
        public Match3TileDatabaseSO TileDatabase => tileDatabase;
        public BoardResolutionResult LastMoveResult => _lastMoveResult;
        public BoardMoveExecutionResult LastExecutionResult => _lastExecutionResult;
        public bool IsInitialized => _isInitialized;

        public event Action<BoardModel> OnBoardInitialized;
        public event Action<BoardMoveExecutionResult> OnMoveExecuted;

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            if (_isInitialized)
            {
                return;
            }

            if (levelDefinition == null)
            {
                Debug.LogError("[Match3GameManager] Missing level definition.");
                return;
            }

            if (tileDatabase == null)
            {
                Debug.LogError("[Match3GameManager] Missing tile database.");
                return;
            }

            Match3LevelData levelData = levelDefinition.LevelData;
            if (levelData == null)
            {
                Debug.LogError("[Match3GameManager] Level definition has no level data.");
                return;
            }

            _random = new System.Random(randomSeed);
            _ruleSet = BoardRuleSet.Default;
            _board = new BoardModel(levelData);
            tileDatabase.RebuildCache();
            _board.PopulateBoard(levelData.gridLayout, tileDatabase);

            if (resolveBoardOnStart)
            {
                BoardRefillService.Apply(_board, levelData, tileDatabase, _random);
                BoardResolutionService.ResolveBoard(_board, levelData, tileDatabase, _random, _ruleSet);
            }

            _lastMoveResult = new BoardResolutionResult();
            _lastExecutionResult = new BoardMoveExecutionResult
            {
                ResolutionResult = _lastMoveResult
            };
            _isInitialized = true;
            OnBoardInitialized?.Invoke(_board);
        }

        public BoardMoveExecutionResult ExecuteMove(BoardMoveRequest request)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[Match3GameManager] Game is not initialized.");
                return new BoardMoveExecutionResult();
            }

            _lastExecutionResult = BoardResolutionService.ExecuteMove(
                _board,
                request,
                levelDefinition.LevelData,
                tileDatabase,
                _random,
                _ruleSet);

            _lastMoveResult = _lastExecutionResult.ResolutionResult;
            if (_lastExecutionResult.IsApplied)
            {
                OnMoveExecuted?.Invoke(_lastExecutionResult);
            }

            return _lastExecutionResult;
        }

        public bool TryResolveMove(BoardMoveRequest request)
        {
            return ExecuteMove(request).IsAccepted;
        }

        public BoardMoveExecutionResult ExecuteTileActivation(int x, int y)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[Match3GameManager] Game is not initialized.");
                return new BoardMoveExecutionResult();
            }

            _lastExecutionResult = BoardResolutionService.ExecuteTileActivation(
                _board,
                x,
                y,
                levelDefinition.LevelData,
                tileDatabase,
                _random,
                _ruleSet);

            _lastMoveResult = _lastExecutionResult.ResolutionResult;
            if (_lastExecutionResult.IsApplied)
            {
                OnMoveExecuted?.Invoke(_lastExecutionResult);
            }

            return _lastExecutionResult;
        }
    }
}
