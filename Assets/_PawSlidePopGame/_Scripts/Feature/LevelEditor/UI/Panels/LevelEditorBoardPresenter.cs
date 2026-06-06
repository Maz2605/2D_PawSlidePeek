using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorBoardPresenter : MonoBehaviour
    {
        [SerializeField] private RectTransform boardContentRoot;
        [SerializeField] private LevelEditorBoardCellView cellTemplate;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private Vector2 gridSpacing = new Vector2(4f, 4f);
        [SerializeField] private float minCellSize = 16f;
        [SerializeField] private float maxCellSize = 72f;

        private LevelEditorUIController _service;
        private readonly List<LevelEditorBoardCellView> _cells = new List<LevelEditorBoardCellView>();
        private int _renderedWidth = -1;
        private int _renderedHeight = -1;

        public void Bind(LevelEditorUIController service)
        {
            _service = service;
        }

        public void Refresh()
        {
            if (_service?.Session?.board == null || boardContentRoot == null || cellTemplate == null)
            {
                return;
            }

            LevelEditorBoardState board = _service.Session.board;
            EnsureCells(board);
            ConfigureGrid(board);

            for (int y = 0; y < board.height; y++)
            {
                for (int x = 0; x < board.width; x++)
                {
                    LevelEditorCellState cell = board.GetCell(x, y);
                    int index = board.ToIndex(x, y);
                    LevelEditorBoardCellView view = _cells[index];
                    view.Bind(
                        x,
                        y,
                        ResolveIcon(cell.UnderlayId),
                        ResolveIcon(cell.TileId),
                        ResolveIcon(cell.OverlayId),
                        _service.Selection != null && _service.Selection.HasSelectedCell && _service.Selection.SelectedX == x && _service.Selection.SelectedY == y,
                        cell.Playable,
                        HandleCellAction);
                }
            }

            cellTemplate.gameObject.SetActive(false);
        }

        private void HandleCellAction(int x, int y, LevelEditorBoardCellAction action)
        {
            switch (action)
            {
                case LevelEditorBoardCellAction.Erase:
                    _service?.EraseCell(x, y);
                    break;
                case LevelEditorBoardCellAction.Paint:
                default:
                    _service?.PaintCell(x, y);
                    break;
            }
        }

        private void EnsureCells(LevelEditorBoardState board)
        {
            if (_renderedWidth == board.width && _renderedHeight == board.height && _cells.Count == board.CellCount)
            {
                return;
            }

            ClearCells();
            _renderedWidth = board.width;
            _renderedHeight = board.height;

            for (int i = 0; i < board.CellCount; i++)
            {
                LevelEditorBoardCellView view = Instantiate(cellTemplate, boardContentRoot);
                view.gameObject.SetActive(true);
                _cells.Add(view);
            }
        }

        private void ConfigureGrid(LevelEditorBoardState board)
        {
            if (gridLayout == null || boardContentRoot == null || board.width <= 0 || board.height <= 0)
            {
                return;
            }

            Rect rect = boardContentRoot.rect;
            float availableWidth = Mathf.Max(1f, rect.width - (gridSpacing.x * Mathf.Max(0, board.width - 1)));
            float availableHeight = Mathf.Max(1f, rect.height - (gridSpacing.y * Mathf.Max(0, board.height - 1)));
            float cellSize = Mathf.Min(availableWidth / board.width, availableHeight / board.height);
            cellSize = Mathf.Clamp(cellSize, minCellSize, maxCellSize);

            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = board.width;
            gridLayout.spacing = gridSpacing;
            gridLayout.cellSize = new Vector2(cellSize, cellSize);
        }

        private void ClearCells()
        {
            for (int i = _cells.Count - 1; i >= 0; i--)
            {
                if (_cells[i] != null)
                {
                    DestroyViewObject(_cells[i].gameObject);
                }
            }

            _cells.Clear();
        }

        private static void DestroyViewObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private Sprite ResolveIcon(int contentId)
        {
            if (contentId <= 0 || _service?.TileDatabase == null)
            {
                return null;
            }

            BoardContentDefinitionSO definition = _service.TileDatabase.GetContentDefinition(contentId);
            return definition != null ? definition.Icon : null;
        }
    }
}
