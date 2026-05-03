using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;
using _PawSlidePopGame._Scripts.Feature.Match3.View.Factory;
using _PawSlidePopGame.Scripts.DesignPattern.ObjectPooling;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class Match3BoardView : MonoBehaviour
    {
        [Header("Authoring")]
        [SerializeField] private Match3CellView cellPrefab;
        [SerializeField] private Transform cellRoot;
        [SerializeField] private Transform tileRoot;

        [Header("Layout")]
        [SerializeField] private float cellStepX = 1f;
        [SerializeField] private float cellStepY = 1f;
        [SerializeField] private Vector2 boardOffset;

        [Header("FX Timing")]
        [SerializeField] private float moveDuration = 0.18f;
        [SerializeField] private float rollbackDuration = 0.14f;
        [SerializeField] private float gravityDurationPerCell = 0.09f;
        [SerializeField] private float refillDurationPerCell = 0.1f;
        [SerializeField] private float phaseGap = 0.04f;
        [SerializeField] private int spawnableTilePrewarmReserve = 2;

        private readonly Dictionary<CellModel, Match3CellView> _cellViews = new Dictionary<CellModel, Match3CellView>();
        private readonly Dictionary<int, Match3TileView> _tileViews = new Dictionary<int, Match3TileView>();
        private readonly HashSet<int> _previewedTileIds = new HashSet<int>();
        private readonly Match3CellViewFactory _cellViewFactory = new Match3CellViewFactory();
        private readonly Match3TileViewFactory _tileViewFactory = new Match3TileViewFactory();
        private BoardModel _board;
        private Match3LevelData _levelData;
        private Match3TileDatabaseSO _tileDatabase;
        private bool _isIdleEnabled = true;

        public event Action<TileClearOp, Vector3> OnTileClearPlaybackStarted;
        public event Action<ScoreGainOp> OnScoreGainPlaybackStarted;

        public void Bind(BoardModel board, Match3LevelData levelData, Match3TileDatabaseSO tileDatabase)
        {
            _board = board;
            _levelData = levelData;
            _tileDatabase = tileDatabase;
            Rebuild();
        }

        public CellModel GetCellAtScreenPosition(Vector2 screenPosition)
        {
            if (Camera.main == null)
            {
                return null;
            }

            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
            worldPosition.z = 0f;
            return GetCellAtWorldPosition(worldPosition);
        }

        public CellModel GetCellAtWorldPosition(Vector3 worldPosition)
        {
            if (_board == null)
            {
                return null;
            }

            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            float offsetX = -((_board.Width - 1) * cellStepX) * 0.5f + boardOffset.x;
            float offsetY = ((_board.Height - 1) * cellStepY) * 0.5f + boardOffset.y;

            float left = offsetX - (cellStepX * 0.5f);
            float top = offsetY + (cellStepY * 0.5f);

            float relativeX = localPosition.x - left;
            float relativeY = top - localPosition.y;

            if (relativeX < 0f || relativeY < 0f)
            {
                return null;
            }

            int x = Mathf.FloorToInt(relativeX / cellStepX);
            int y = Mathf.FloorToInt(relativeY / cellStepY);

            if (x < 0 || x >= _board.Width || y < 0 || y >= _board.Height)
            {
                return null;
            }

            CellModel cell = _board.GetCell(x, y);
            return cell != null && cell.IsPlayable ? cell : null;
        }

        public void SetIdleEnabled(bool isEnabled)
        {
            _isIdleEnabled = isEnabled;

            foreach (KeyValuePair<int, Match3TileView> pair in _tileViews)
            {
                if (pair.Value != null)
                {
                    pair.Value.SetIdleEnabled(isEnabled);
                }
            }
        }

        public void ShowPressPreview(CellModel cell)
        {
            ClearPreview();

            if (cell?.CurrentTile == null)
            {
                return;
            }

            if (_tileViews.TryGetValue(cell.CurrentTile.InstanceId, out Match3TileView tileView) && tileView != null)
            {
                tileView.SetShadowState(TileShadowState.Active);
                _previewedTileIds.Add(cell.CurrentTile.InstanceId);
            }
        }

        public void ShowLinePreview(BoardLinePreview preview)
        {
            if (preview.FocusedCell == null)
            {
                ClearPreview();
                return;
            }

            ClearPreview();
            ShowPressPreview(preview.FocusedCell);

            if (_board == null || !preview.HasLockedLine || !preview.Axis.HasValue)
            {
                return;
            }

            List<CellModel> cells = _board.GetPlayableCellsForMove(preview.Axis.Value, preview.LineIndex);
            for (int i = 0; i < cells.Count; i++)
            {
                TileModel tile = cells[i].CurrentTile;
                if (tile == null || !_tileViews.TryGetValue(tile.InstanceId, out Match3TileView tileView) || tileView == null)
                {
                    continue;
                }

                TileShadowState state = preview.FocusedCell.CurrentTile != null &&
                                        preview.FocusedCell.CurrentTile.InstanceId == tile.InstanceId
                    ? TileShadowState.Active
                    : TileShadowState.Preview;
                tileView.SetShadowState(state);
                _previewedTileIds.Add(tile.InstanceId);
            }
        }

        public void ClearPreview()
        {
            if (_previewedTileIds.Count == 0)
            {
                return;
            }

            foreach (int tileInstanceId in _previewedTileIds)
            {
                if (_tileViews.TryGetValue(tileInstanceId, out Match3TileView tileView) && tileView != null)
                {
                    tileView.SetShadowState(TileShadowState.Off);
                }
            }

            _previewedTileIds.Clear();
        }

        public IEnumerator PlayMoveExecution(BoardMoveExecutionResult executionResult)
        {
            if (executionResult == null || executionResult.PresentationTrace == null)
            {
                yield break;
            }

            BoardPresentationTrace trace = executionResult.PresentationTrace;

            if (trace.MoveAttempt != null && trace.MoveAttempt.TravelOps.Count > 0)
            {
                yield return StartCoroutine(PlayLineTravel(trace.MoveAttempt.TravelOps, trace.MoveAttempt.Axis, trace.MoveAttempt.Direction, moveDuration));
            }

            if (!executionResult.IsAccepted)
            {
                if (trace.Rollback != null && trace.Rollback.TravelOps.Count > 0)
                {
                    yield return StartCoroutine(PlayLineTravel(trace.Rollback.TravelOps, trace.Rollback.Axis, trace.Rollback.Direction, rollbackDuration));
                }

                yield break;
            }

            for (int i = 0; i < trace.Cascades.Count; i++)
            {
                CascadeTrace cascadeTrace = trace.Cascades[i];
                yield return StartCoroutine(PlayClearPhase(cascadeTrace.ClearPhase));
                yield return StartCoroutine(PlayGravityPhase(cascadeTrace.GravityPhase));
                yield return StartCoroutine(PlayRefillPhase(cascadeTrace.RefillPhase));
            }
        }

        public void SyncToBoardState()
        {
            if (_board == null)
            {
                return;
            }

            HashSet<int> aliveTileIds = new HashSet<int>();

            foreach (CellModel cell in _board.GetAllCells())
            {
                if (cell == null || !cell.IsPlayable || cell.CurrentTile == null)
                {
                    continue;
                }

                TileModel tile = cell.CurrentTile;
                aliveTileIds.Add(tile.InstanceId);

                Vector3 targetLocalPosition = GetTileLocalPosition(cell);
                if (!_tileViews.TryGetValue(tile.InstanceId, out Match3TileView tileView) || tileView == null)
                {
                    tileView = CreateTileView(tile, tile.Definition, targetLocalPosition);
                }
                else
                {
                    tileView.Bind(tile, tile.Definition);
                    tileView.SnapToLocalPosition(targetLocalPosition);
                }

                if (tileView != null)
                {
                    tileView.SetShadowState(TileShadowState.Off);
                    tileView.SetIdleEnabled(_isIdleEnabled);
                }
            }

            List<int> staleTileIds = new List<int>();
            foreach (KeyValuePair<int, Match3TileView> pair in _tileViews)
            {
                if (!aliveTileIds.Contains(pair.Key))
                {
                    staleTileIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleTileIds.Count; i++)
            {
                DestroyTileView(staleTileIds[i]);
            }
        }

        private void Rebuild()
        {
            if (_board == null)
            {
                Debug.LogError("[Match3BoardView] Board is null.");
                return;
            }

            if (cellPrefab == null)
            {
                Debug.LogError("[Match3BoardView] Missing cell prefab.");
                return;
            }

            ClearCells();
            ClearTileViews();
            PrewarmBoardPools();

            foreach (CellModel cell in _board.GetAllCells())
            {
                if (!cell.IsPlayable)
                {
                    continue;
                }

                Match3CellView cellView = CreateCellView(cell);
                if (cellView != null)
                {
                    _cellViews[cell] = cellView;
                }
            }

            SyncToBoardState();
        }

        private void ClearCells()
        {
            if (Application.isPlaying)
            {
                List<Match3CellView> cellViews = new List<Match3CellView>(_cellViews.Values);
                for (int i = 0; i < cellViews.Count; i++)
                {
                    _cellViewFactory.ReturnVisual(cellViews[i]);
                }

                _cellViews.Clear();
                return;
            }

            Transform parent = cellRoot != null ? cellRoot : transform;
            List<Transform> children = new List<Transform>();

            for (int i = 0; i < parent.childCount; i++)
            {
                children.Add(parent.GetChild(i));
            }

            for (int i = 0; i < children.Count; i++)
            {
                if (Application.isPlaying)
                {
                    Destroy(children[i].gameObject);
                }
                else
                {
                    DestroyImmediate(children[i].gameObject);
                }
            }

            _cellViews.Clear();
        }

        private void ClearTileViews()
        {
            List<int> tileIds = new List<int>(_tileViews.Keys);
            for (int i = 0; i < tileIds.Count; i++)
            {
                DestroyTileView(tileIds[i]);
            }
        }

        private Match3TileView CreateTileView(TileModel tile, _PawSlidePopGame._Scripts.Feature.Match3.Data.TileDefinitionSO definition, Vector3 localPosition)
        {
            if (tile == null || definition == null || definition.TileViewPrefab == null)
            {
                return null;
            }

            Transform parent = tileRoot != null ? tileRoot : transform;
            Match3TileView tileView = Application.isPlaying
                ? _tileViewFactory.CreateVisual(
                    new TileViewSpawnData(
                        definition.TileViewPrefab,
                        tile,
                        definition,
                        localPosition,
                        _isIdleEnabled,
                        $"Tile_{tile.InstanceId}_{tile.TileId}"),
                    parent)
                : Instantiate(definition.TileViewPrefab, parent);

            if (!Application.isPlaying && tileView != null)
            {
                tileView.name = $"Tile_{tile.InstanceId}_{tile.TileId}";
                tileView.Bind(tile, definition);
                tileView.SnapToLocalPosition(localPosition);
                tileView.SetIdleEnabled(_isIdleEnabled);
            }

            if (tileView == null)
            {
                return null;
            }

            _tileViews[tile.InstanceId] = tileView;
            return tileView;
        }

        private void DestroyTileView(int tileInstanceId)
        {
            if (!_tileViews.TryGetValue(tileInstanceId, out Match3TileView tileView))
            {
                return;
            }

            _tileViews.Remove(tileInstanceId);
            _previewedTileIds.Remove(tileInstanceId);

            if (tileView == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                _tileViewFactory.ReturnVisual(tileView);
                return;
            }

            DestroyImmediate(tileView.gameObject);
        }

        private Match3CellView CreateCellView(CellModel cell)
        {
            Transform parent = cellRoot != null ? cellRoot : transform;
            Vector3 localPosition = GetLocalPosition(cell.X, cell.Y);

            if (Application.isPlaying)
            {
                return _cellViewFactory.CreateVisual(
                    new CellViewSpawnData(cellPrefab, cell, localPosition, $"Cell_{cell.X}_{cell.Y}"),
                    parent);
            }

            Match3CellView cellView = Instantiate(cellPrefab, parent);
            cellView.name = $"Cell_{cell.X}_{cell.Y}";
            cellView.transform.localPosition = localPosition;
            cellView.Initialize(cell);
            return cellView;
        }

        private void PrewarmBoardPools()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (cellPrefab != null)
            {
                PoolingManager.Instance.Prewarm(cellPrefab.gameObject, CountPlayableCells());
            }

            foreach (KeyValuePair<GameObject, int> pair in BuildTilePrewarmCounts())
            {
                if (pair.Key == null || pair.Value <= 0)
                {
                    continue;
                }

                PoolingManager.Instance.Prewarm(pair.Key, pair.Value);
            }
        }

        private int CountPlayableCells()
        {
            if (_board == null)
            {
                return 0;
            }

            int count = 0;
            foreach (CellModel cell in _board.GetAllCells())
            {
                if (cell != null && cell.IsPlayable)
                {
                    count++;
                }
            }

            return count;
        }

        private Dictionary<GameObject, int> BuildTilePrewarmCounts()
        {
            Dictionary<GameObject, int> counts = new Dictionary<GameObject, int>();

            if (_board != null)
            {
                foreach (CellModel cell in _board.GetAllCells())
                {
                    TileDefinitionSO definition = cell?.CurrentTile?.Definition;
                    Match3TileView prefab = definition?.TileViewPrefab;
                    if (prefab == null)
                    {
                        continue;
                    }

                    GameObject prefabObject = prefab.gameObject;
                    counts[prefabObject] = counts.TryGetValue(prefabObject, out int currentCount)
                        ? currentCount + 1
                        : 1;
                }
            }

            if (_levelData == null || _tileDatabase == null || _levelData.spawnableTileIds == null || spawnableTilePrewarmReserve <= 0)
            {
                return counts;
            }

            HashSet<GameObject> reservedPrefabs = new HashSet<GameObject>();
            for (int i = 0; i < _levelData.spawnableTileIds.Count; i++)
            {
                TileDefinitionSO definition = _tileDatabase.GetTileDefinition(_levelData.spawnableTileIds[i]);
                Match3TileView prefab = definition?.TileViewPrefab;
                if (prefab == null)
                {
                    continue;
                }

                GameObject prefabObject = prefab.gameObject;
                if (!reservedPrefabs.Add(prefabObject))
                {
                    continue;
                }

                counts[prefabObject] = counts.TryGetValue(prefabObject, out int currentCount)
                    ? currentCount + spawnableTilePrewarmReserve
                    : spawnableTilePrewarmReserve;
            }

            return counts;
        }

        private IEnumerator PlayClearPhase(ClearPhaseTrace clearPhase)
        {
            if (clearPhase == null)
            {
                yield break;
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            List<ScoreGainOp> pendingScoreGains = clearPhase.ScoreGainOps != null
                ? new List<ScoreGainOp>(clearPhase.ScoreGainOps)
                : new List<ScoreGainOp>();

            for (int i = 0; i < clearPhase.ActivateOps.Count; i++)
            {
                TileActivateOp op = clearPhase.ActivateOps[i];
                routines.Add(PlayActivateOp(op));
            }

            yield return StartCoroutine(RunParallel(routines));
            routines.Clear();

            for (int i = 0; i < clearPhase.TargetSelectionOps.Count; i++)
            {
                TargetSelectionOp op = clearPhase.TargetSelectionOps[i];
                if (_tileViews.TryGetValue(op.TargetTileInstanceId, out Match3TileView tileView) && tileView != null)
                {
                    routines.Add(tileView.PlayTargetSelectionAsync());
                }
            }

            yield return StartCoroutine(RunParallel(routines));
            routines.Clear();

            for (int i = 0; i < clearPhase.DamageOps.Count; i++)
            {
                TileDamageOp op = clearPhase.DamageOps[i];
                if (_tileViews.TryGetValue(op.TileInstanceId, out Match3TileView tileView) && tileView != null)
                {
                    routines.Add(tileView.PlayDamageAsync(op.CurrentHP, op.PreviousHP));
                }
            }

            yield return StartCoroutine(RunParallel(routines));
            routines.Clear();

            for (int i = 0; i < clearPhase.ClearOps.Count; i++)
            {
                TileClearOp op = clearPhase.ClearOps[i];
                OnTileClearPlaybackStarted?.Invoke(op, ResolveWorldPosition(op));
                DispatchScoreGainsForClearOp(op, pendingScoreGains);

                if (_tileViews.TryGetValue(op.TileInstanceId, out Match3TileView tileView) && tileView != null)
                {
                    routines.Add(tileView.PlayClearAsync());
                }
            }

            yield return StartCoroutine(RunParallel(routines));
            routines.Clear();

            for (int i = 0; i < clearPhase.ClearOps.Count; i++)
            {
                DestroyTileView(clearPhase.ClearOps[i].TileInstanceId);
            }

            for (int i = 0; i < clearPhase.SpecialCreateOps.Count; i++)
            {
                SpecialCreateOp op = clearPhase.SpecialCreateOps[i];
                DispatchScoreGainsForSpecialCreateOp(op, pendingScoreGains);
                routines.Add(PlaySpecialCreateOp(op));
            }

            DispatchRemainingScoreGains(pendingScoreGains);
            yield return StartCoroutine(RunParallel(routines));
            routines.Clear();

            if (phaseGap > 0f)
            {
                yield return new WaitForSeconds(phaseGap);
            }
        }

        private IEnumerator PlayGravityPhase(GravityPhaseTrace gravityPhase)
        {
            if (gravityPhase == null || gravityPhase.TravelOps.Count == 0)
            {
                yield break;
            }

            List<IEnumerator> moveRoutines = new List<IEnumerator>();
            List<Match3TileView> landingViews = new List<Match3TileView>();

            for (int i = 0; i < gravityPhase.TravelOps.Count; i++)
            {
                TileTravelOp op = gravityPhase.TravelOps[i];
                if (!_tileViews.TryGetValue(op.TileInstanceId, out Match3TileView tileView) || tileView == null)
                {
                    continue;
                }

                float duration = Mathf.Max(0.05f, gravityDurationPerCell * Mathf.Max(1, op.Distance));
                moveRoutines.Add(tileView.PlayMoveAsync(GetLocalPosition(op.ToCell.X, op.ToCell.Y), duration));
                landingViews.Add(tileView);
            }

            yield return StartCoroutine(RunParallel(moveRoutines));
            yield return StartCoroutine(PlayLandings(landingViews, gravityPhase.TravelOps));

            if (phaseGap > 0f)
            {
                yield return new WaitForSeconds(phaseGap);
            }
        }

        private IEnumerator PlayRefillPhase(RefillPhaseTrace refillPhase)
        {
            if (refillPhase == null || refillPhase.SpawnOps.Count == 0)
            {
                yield break;
            }

            List<IEnumerator> spawnRoutines = new List<IEnumerator>();
            List<Match3TileView> landingViews = new List<Match3TileView>();
            List<int> landingDistances = new List<int>();

            for (int i = 0; i < refillPhase.SpawnOps.Count; i++)
            {
                TileSpawnOp op = refillPhase.SpawnOps[i];
                if (_tileViews.TryGetValue(op.TileInstanceId, out Match3TileView existingTileView) && existingTileView != null)
                {
                    continue;
                }

                Match3TileView tileView = CreateTileView(new TileModel(op.TileInstanceId, op.Definition), op.Definition, GetLocalPosition(op.ToCell.X, op.SpawnFromRowAboveBoard));
                if (tileView == null)
                {
                    continue;
                }

                float distance = Mathf.Abs(op.ToCell.Y - op.SpawnFromRowAboveBoard);
                float duration = Mathf.Max(0.08f, refillDurationPerCell * Mathf.Max(1f, distance));
                spawnRoutines.Add(tileView.PlaySpawnFallAsync(
                    GetLocalPosition(op.ToCell.X, op.SpawnFromRowAboveBoard),
                    GetLocalPosition(op.ToCell.X, op.ToCell.Y),
                    duration));
                landingViews.Add(tileView);
                landingDistances.Add(Mathf.Max(1, Mathf.RoundToInt(distance)));
            }

            yield return StartCoroutine(RunParallel(spawnRoutines));

            List<TileTravelOp> landingOps = new List<TileTravelOp>(landingViews.Count);
            for (int i = 0; i < landingViews.Count; i++)
            {
                landingOps.Add(new TileTravelOp
                {
                    TileInstanceId = landingViews[i].TileInstanceId,
                    Distance = landingDistances[i]
                });
            }

            yield return StartCoroutine(PlayLandings(landingViews, landingOps));

            if (phaseGap > 0f)
            {
                yield return new WaitForSeconds(phaseGap);
            }
        }

        private IEnumerator PlayLineTravel(IReadOnlyList<TileTravelOp> travelOps, MoveAxis axis, LineSlideDirection direction, float duration)
        {
            if (travelOps == null || travelOps.Count == 0)
            {
                yield break;
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            List<WrapSnapData> wrapSnaps = new List<WrapSnapData>();

            for (int i = 0; i < travelOps.Count; i++)
            {
                TileTravelOp op = travelOps[i];
                if (!_tileViews.TryGetValue(op.TileInstanceId, out Match3TileView tileView) || tileView == null)
                {
                    continue;
                }

                Vector3 finalTarget = GetLocalPosition(op.ToCell.X, op.ToCell.Y);
                Vector3 tweenTarget = op.IsWrapAround ? GetWrapExitPosition(op.FromCell, axis, direction) : finalTarget;
                routines.Add(tileView.PlayMoveAsync(tweenTarget, duration));

                if (op.IsWrapAround)
                {
                    wrapSnaps.Add(new WrapSnapData(tileView, finalTarget));
                }
            }

            yield return StartCoroutine(RunParallel(routines));

            for (int i = 0; i < travelOps.Count; i++)
            {
                if (_tileViews.TryGetValue(travelOps[i].TileInstanceId, out Match3TileView tileView) && tileView != null)
                {
                    tileView.SetShadowState(TileShadowState.Off);
                }
            }

            for (int i = 0; i < wrapSnaps.Count; i++)
            {
                wrapSnaps[i].TileView.SnapToLocalPosition(wrapSnaps[i].TargetLocalPosition);
                wrapSnaps[i].TileView.SetShadowState(TileShadowState.Off);
            }

            if (phaseGap > 0f)
            {
                yield return new WaitForSeconds(phaseGap);
            }
        }

        private IEnumerator PlayLandings(IReadOnlyList<Match3TileView> tileViews, IReadOnlyList<TileTravelOp> ops)
        {
            if (tileViews == null || tileViews.Count == 0)
            {
                yield break;
            }

            List<IEnumerator> routines = new List<IEnumerator>();
            for (int i = 0; i < tileViews.Count; i++)
            {
                Match3TileView tileView = tileViews[i];
                int distance = ops != null && i < ops.Count ? Mathf.Max(1, ops[i].Distance) : 1;
                routines.Add(tileView.PlayLandAsync(Mathf.Clamp(distance * 0.35f, 0.7f, 1.5f)));
            }

            yield return StartCoroutine(RunParallel(routines));
        }

        private IEnumerator RunParallel(IReadOnlyList<IEnumerator> routines)
        {
            if (routines == null || routines.Count == 0)
            {
                yield break;
            }

            int remaining = 0;
            for (int i = 0; i < routines.Count; i++)
            {
                if (routines[i] == null)
                {
                    continue;
                }

                remaining++;
                StartCoroutine(WrapRoutine(routines[i], () => remaining--));
            }

            while (remaining > 0)
            {
                yield return null;
            }
        }

        private IEnumerator PlayActivateOp(TileActivateOp activateOp)
        {
            if (activateOp.LogicType == TileLogicType.CrossBomb)
            {
                yield return StartCoroutine(PlayCrossHighlight(activateOp.Cell));
            }

            if (_tileViews.TryGetValue(activateOp.TileInstanceId, out Match3TileView tileView) && tileView != null)
            {
                yield return StartCoroutine(tileView.PlayActivateAsync());
            }
        }

        private IEnumerator PlaySpecialCreateOp(SpecialCreateOp createOp)
        {
            Match3TileView sourceTileView = null;
            if (_tileViews.TryGetValue(createOp.SourceTileInstanceId, out Match3TileView existingView))
            {
                sourceTileView = existingView;
            }

            if (sourceTileView != null)
            {
                yield return StartCoroutine(sourceTileView.PlaySpecialCreateAsync());
            }

            DestroyTileView(createOp.SourceTileInstanceId);

            CellModel boardCell = _board != null ? _board.GetCell(createOp.Cell.X, createOp.Cell.Y) : null;
            TileModel boardTile = boardCell?.CurrentTile;
            if (boardTile == null || boardTile.InstanceId != createOp.NewTileInstanceId)
            {
                boardTile = new TileModel(createOp.NewTileInstanceId, createOp.Definition);
            }

            Match3TileView newTileView = CreateTileView(boardTile, createOp.Definition, GetLocalPosition(createOp.Cell.X, createOp.Cell.Y));
            if (newTileView != null)
            {
                yield return StartCoroutine(newTileView.PlaySpecialCreateAsync());
            }
        }

        private IEnumerator PlayCrossHighlight(BoardCellPosition center)
        {
            if (_board == null)
            {
                yield break;
            }

            List<Match3TileView> highlighted = new List<Match3TileView>();
            HighlightCells(_board.GetPlayableCellsInRow(center.Y), highlighted);
            HighlightCells(_board.GetPlayableCellsInColumn(center.X), highlighted);
            yield return new WaitForSeconds(Mathf.Max(0.04f, phaseGap + 0.04f));

            for (int i = 0; i < highlighted.Count; i++)
            {
                if (highlighted[i] != null)
                {
                    highlighted[i].SetShadowState(TileShadowState.Off);
                }
            }
        }

        private void HighlightCells(IReadOnlyList<CellModel> cells, List<Match3TileView> highlighted)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                TileModel tile = cells[i].CurrentTile;
                if (tile == null || !_tileViews.TryGetValue(tile.InstanceId, out Match3TileView tileView) || tileView == null)
                {
                    continue;
                }

                tileView.SetShadowState(TileShadowState.Active);
                if (!highlighted.Contains(tileView))
                {
                    highlighted.Add(tileView);
                }
            }
        }

        private IEnumerator WrapRoutine(IEnumerator routine, System.Action onComplete)
        {
            yield return StartCoroutine(routine);
            onComplete?.Invoke();
        }

        private void DispatchScoreGainsForClearOp(TileClearOp clearOp, List<ScoreGainOp> pendingScoreGains)
        {
            if (clearOp == null || pendingScoreGains == null || pendingScoreGains.Count == 0)
            {
                return;
            }

            for (int i = pendingScoreGains.Count - 1; i >= 0; i--)
            {
                ScoreGainOp scoreGainOp = pendingScoreGains[i];
                if (!MatchesClearOp(scoreGainOp, clearOp))
                {
                    continue;
                }

                OnScoreGainPlaybackStarted?.Invoke(scoreGainOp);
                pendingScoreGains.RemoveAt(i);
            }
        }

        private void DispatchScoreGainsForSpecialCreateOp(SpecialCreateOp specialCreateOp, List<ScoreGainOp> pendingScoreGains)
        {
            if (specialCreateOp == null || pendingScoreGains == null || pendingScoreGains.Count == 0)
            {
                return;
            }

            for (int i = pendingScoreGains.Count - 1; i >= 0; i--)
            {
                ScoreGainOp scoreGainOp = pendingScoreGains[i];
                if (!MatchesSpecialCreateOp(scoreGainOp, specialCreateOp))
                {
                    continue;
                }

                OnScoreGainPlaybackStarted?.Invoke(scoreGainOp);
                pendingScoreGains.RemoveAt(i);
            }
        }

        private void DispatchRemainingScoreGains(List<ScoreGainOp> pendingScoreGains)
        {
            if (pendingScoreGains == null || pendingScoreGains.Count == 0)
            {
                return;
            }

            for (int i = 0; i < pendingScoreGains.Count; i++)
            {
                OnScoreGainPlaybackStarted?.Invoke(pendingScoreGains[i]);
            }

            pendingScoreGains.Clear();
        }

        private Vector3 GetTileLocalPosition(CellModel cell)
        {
            if (cell != null && _cellViews.TryGetValue(cell, out Match3CellView cellView) && cellView != null)
            {
                return cellView.GetTileAnchorLocalPosition(transform);
            }

            return GetLocalPosition(cell.X, cell.Y);
        }

        private Vector3 ResolveWorldPosition(TileClearOp clearOp)
        {
            if (clearOp != null &&
                _tileViews.TryGetValue(clearOp.TileInstanceId, out Match3TileView tileView) &&
                tileView != null)
            {
                return tileView.transform.position;
            }

            return transform.TransformPoint(GetLocalPosition(clearOp.Cell.X, clearOp.Cell.Y));
        }

        private Vector3 GetWrapExitPosition(BoardCellPosition fromCell, MoveAxis axis, LineSlideDirection direction)
        {
            if (_board == null)
            {
                return GetLocalPosition(fromCell.X, fromCell.Y);
            }

            if (axis == MoveAxis.Row)
            {
                int exitX = direction == LineSlideDirection.Right ? _board.Width : -1;
                return GetLocalPosition(exitX, fromCell.Y);
            }

            int exitY = direction == LineSlideDirection.Down ? _board.Height : -1;
            return GetLocalPosition(fromCell.X, exitY);
        }

        private Vector3 GetLocalPosition(int x, int y)
        {
            float offsetX = _board != null ? -((_board.Width - 1) * cellStepX) * 0.5f : 0f;
            float offsetY = _board != null ? ((_board.Height - 1) * cellStepY) * 0.5f : 0f;
            return new Vector3(boardOffset.x + offsetX + (x * cellStepX), boardOffset.y + offsetY - (y * cellStepY), 0f);
        }

        private static bool MatchesClearOp(ScoreGainOp scoreGainOp, TileClearOp clearOp)
        {
            return scoreGainOp != null &&
                   clearOp != null &&
                   scoreGainOp.TileInstanceId == clearOp.TileInstanceId &&
                   scoreGainOp.Cell.Equals(clearOp.Cell);
        }

        private static bool MatchesSpecialCreateOp(ScoreGainOp scoreGainOp, SpecialCreateOp specialCreateOp)
        {
            return scoreGainOp != null &&
                   specialCreateOp != null &&
                   scoreGainOp.TileInstanceId == specialCreateOp.SourceTileInstanceId &&
                   scoreGainOp.Cell.Equals(specialCreateOp.Cell);
        }

        private void OnValidate()
        {
            if (spawnableTilePrewarmReserve < 0)
            {
                spawnableTilePrewarmReserve = 0;
            }
        }

        private readonly struct WrapSnapData
        {
            public Match3TileView TileView { get; }
            public Vector3 TargetLocalPosition { get; }

            public WrapSnapData(Match3TileView tileView, Vector3 targetLocalPosition)
            {
                TileView = tileView;
                TargetLocalPosition = targetLocalPosition;
            }
        }
    }
}
