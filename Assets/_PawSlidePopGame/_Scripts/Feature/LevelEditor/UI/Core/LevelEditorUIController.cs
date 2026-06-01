using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Mapping;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Services;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Persistence;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Scene;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI
{
    public sealed class LevelEditorUIController : MonoBehaviour
    {
        [SerializeField] private LevelEditorRuntimeBridge runtimeBridge;
        [SerializeField] private LevelEditorSessionStateHolder sessionStateHolder;
        [SerializeField] private LevelEditorSelectionStateHolder selectionStateHolder;

        private readonly JsonLevelDataProvider _levelDataProvider = new JsonLevelDataProvider();
        private readonly ResourcesLevelCatalogProvider _levelCatalogProvider = new ResourcesLevelCatalogProvider();
        private readonly LevelJsonExportService _exportService = new LevelJsonExportService();
        private readonly List<LevelTargetRequirement> _targets = new List<LevelTargetRequirement>();

        public event Action Changed;

        public Match3TileDatabaseSO TileDatabase => runtimeBridge != null ? runtimeBridge.TileDatabase : null;
        public LevelEditorSessionContext Session => sessionStateHolder != null ? sessionStateHolder.Context : null;
        public LevelEditorSelectionStateHolder Selection => selectionStateHolder;
        public IReadOnlyList<LevelTargetRequirement> Targets => _targets;

        private void Awake()
        {
            EnsureSession();
        }

        public void Configure(LevelEditorRuntimeBridge bridge, LevelEditorSessionStateHolder sessionHolder, LevelEditorSelectionStateHolder selectionHolder)
        {
            runtimeBridge = bridge;
            sessionStateHolder = sessionHolder;
            selectionStateHolder = selectionHolder;
            EnsureSession();
        }

        public void EnsureSession()
        {
            if (sessionStateHolder == null)
            {
                return;
            }

            if (sessionStateHolder.Context.board == null || sessionStateHolder.Context.board.CellCount <= 0)
            {
                NewLevel(
                    sessionStateHolder.CurrentLevelId,
                    sessionStateHolder.DisplayLevelNumber,
                    sessionStateHolder.Width,
                    sessionStateHolder.Height,
                    sessionStateHolder.MovesLimit);
            }
        }

        public void NewLevel(string levelId, int displayLevelNumber, int width, int height, int movesLimit)
        {
            LevelEditorSessionContext context = LevelEditorDocumentMapper.CreateNew(
                levelId,
                displayLevelNumber,
                width,
                height,
                movesLimit);
            context.spawnableTileIds = BuildDefaultSpawnables();
            context.statusMessage = "Created new level session.";
            context.errorMessage = string.Empty;
            _targets.Clear();
            context.lastValidationResult = ValidateDocument(LevelEditorDocumentMapper.BuildDocument(context, TileDatabase, _targets).levelData);
            sessionStateHolder.LoadContext(context);
            selectionStateHolder.ClearSelection();
            NotifyChanged();
        }

        public LevelEditorLoadResult LoadLevel(string levelId)
        {
            LevelEditorLoadResult result = new LevelEditorLoadResult
            {
                success = false,
                levelId = levelId
            };

            if (!_levelDataProvider.TryGetLevelDocument(levelId, out LevelSaveData document))
            {
                result.message = $"Failed to load level '{levelId}'.";
                sessionStateHolder.SetStatus(string.Empty, result.message);
                NotifyChanged();
                return result;
            }

            LevelEditorSessionContext context = LevelEditorDocumentMapper.LoadFromDocument(document);
            context.statusMessage = $"Loaded level '{context.levelId}'.";
            context.errorMessage = string.Empty;
            context.lastValidationResult = ValidateDocument(document.levelData);
            _targets.Clear();
            _targets.AddRange(LevelEditorDocumentMapper.LoadTargetsFromDocument(document));

            sessionStateHolder.LoadContext(context);
            selectionStateHolder.ClearSelection();

            result.success = true;
            result.message = context.statusMessage;
            result.document = document;
            result.session = context.Clone();
            NotifyChanged();
            return result;
        }

        public LevelEditorSaveResult SaveLevel(bool overwrite = true)
        {
            LevelEditorSaveResult result = new LevelEditorSaveResult
            {
                success = false,
                levelId = Session != null ? Session.levelId : string.Empty
            };

            if (Session == null || Session.board == null)
            {
                result.message = "No active level editor session.";
                sessionStateHolder?.SetStatus(string.Empty, result.message);
                NotifyChanged();
                return result;
            }

            LevelSaveData document = LevelEditorDocumentMapper.BuildDocument(Session, TileDatabase, _targets);
            Match3LevelDataValidationResult validation = ValidateDocument(document.levelData);
            result.validationResult = validation;
            result.document = document;

            if (!validation.IsValid)
            {
                result.message = "Save blocked because the level is invalid.";
                sessionStateHolder.SetValidation(validation);
                sessionStateHolder.SetStatus(string.Empty, result.message);
                NotifyChanged();
                return result;
            }

            if (!_exportService.TryExport(document, document.levelId, overwrite))
            {
                result.message = $"Failed to export level '{document.levelId}'.";
                sessionStateHolder.SetValidation(validation);
                sessionStateHolder.SetStatus(string.Empty, result.message);
                NotifyChanged();
                return result;
            }

            sessionStateHolder.MarkSaved(document.levelId, validation);
            result.success = true;
            result.message = $"Saved level '{document.levelId}'.";
            NotifyChanged();
            return result;
        }

        public void ResizeBoard(int width, int height)
        {
            if (Session == null)
            {
                return;
            }

            LevelEditorSessionContext resized = LevelEditorDocumentMapper.CreateNew(
                Session.levelId,
                Session.displayLevelNumber,
                width,
                height,
                Session.movesLimit,
                Session.spawnableTileIds);
            resized.isDirty = true;
            resized.statusMessage = $"Resized board to {resized.board.width}x{resized.board.height}.";
            resized.errorMessage = string.Empty;
            sessionStateHolder.LoadContext(resized);
            selectionStateHolder.ClearSelection();
            sessionStateHolder.SetValidation(ValidateDocument(LevelEditorDocumentMapper.BuildDocument(Session, TileDatabase, _targets).levelData));
            NotifyChanged();
        }

        public void SetMetadata(string levelId, int displayLevelNumber, int movesLimit)
        {
            if (Session == null)
            {
                return;
            }

            sessionStateHolder.SetMetadata(levelId, displayLevelNumber, movesLimit);
            RevalidateSession();
            NotifyChanged();
        }

        public void SelectCell(int x, int y)
        {
            if (Session?.board == null || !Session.board.IsInBounds(x, y))
            {
                return;
            }

            selectionStateHolder.SetSelectedCoordinate(x, y);
            sessionStateHolder.SetStatus($"Selected cell ({x}, {y}).", string.Empty);
            NotifyChanged();
        }

        public void SetSelectedLayer(BoardLayer layer, int contentId)
        {
            if (selectionStateHolder == null)
            {
                return;
            }

            selectionStateHolder.SetSelectedLayer(layer, contentId);
            sessionStateHolder?.SetStatus($"Selected {layer} id {Mathf.Max(0, contentId)}.", string.Empty);
            NotifyChanged();
        }

        public void HandleCellClicked(int x, int y)
        {
            PaintCell(x, y);
        }

        public void PaintCell(int x, int y)
        {
            if (Session?.board == null || selectionStateHolder == null || !Session.board.IsInBounds(x, y))
            {
                return;
            }

            selectionStateHolder.SetSelectedCoordinate(x, y);
            if (!selectionStateHolder.HasAnySelectedLayer())
            {
                sessionStateHolder.SetStatus($"Selected cell ({x}, {y}).", string.Empty);
                NotifyChanged();
                return;
            }

            ApplyCellContent(
                x,
                y,
                selectionStateHolder.SelectedUnderlayId,
                selectionStateHolder.SelectedTileId,
                selectionStateHolder.SelectedOverlayId);
            sessionStateHolder.MarkDirty($"Painted cell ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        public void ApplySelectionPayload()
        {
            if (Session?.board == null || selectionStateHolder == null || !selectionStateHolder.HasSelectedCell)
            {
                return;
            }

            int x = selectionStateHolder.SelectedX;
            int y = selectionStateHolder.SelectedY;
            int underlayId = selectionStateHolder.SelectedUnderlayId;
            int tileId = selectionStateHolder.SelectedTileId;
            int overlayId = selectionStateHolder.SelectedOverlayId;
            bool hasAnyContent = underlayId > 0 || tileId > 0 || overlayId > 0;

            if (!hasAnyContent)
            {
                Session.board.ClearCell(x, y, false);
            }
            else
            {
                Session.board.SetUnderlayId(x, y, underlayId);
                Session.board.SetTileId(x, y, tileId);
                Session.board.SetOverlayId(x, y, overlayId);
                Session.board.SetPlayable(x, y, true);
            }

            sessionStateHolder.MarkDirty($"Applied selection payload to ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        public void ClearSelectedCell()
        {
            if (Session?.board == null || selectionStateHolder == null || !selectionStateHolder.HasSelectedCell)
            {
                return;
            }

            int x = selectionStateHolder.SelectedX;
            int y = selectionStateHolder.SelectedY;
            Session.board.ClearCell(x, y, false);
            sessionStateHolder.MarkDirty($"Cleared cell ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        public void ClearSelectionPayload()
        {
            if (selectionStateHolder == null)
            {
                return;
            }

            selectionStateHolder.ClearSelectedLayers();
            sessionStateHolder?.SetStatus("Cleared selection payload.", string.Empty);
            NotifyChanged();
        }

        public void ClearSelectedCellTile()
        {
            ClearSelectedCellLayer(BoardLayer.Tile, "Cleared tile");
        }

        public void ClearSelectedCellUnderlay()
        {
            ClearSelectedCellLayer(BoardLayer.Underlay, "Cleared underlay");
        }

        public void ClearSelectedCellOverlay()
        {
            ClearSelectedCellLayer(BoardLayer.Overlay, "Cleared overlay");
        }

        public void AddGoal()
        {
            if (Session == null)
            {
                return;
            }

            List<LevelEditorPaletteEntryData> targetEntries = GetGoalOptions();
            if (targetEntries.Count == 0)
            {
                sessionStateHolder.SetStatus(string.Empty, "No valid target definitions found in tile database.");
                NotifyChanged();
                return;
            }

            _targets.Add(new LevelTargetRequirement(targetEntries[0].Id, 1));
            sessionStateHolder.MarkDirty("Added goal.");
            RevalidateSession();
            NotifyChanged();
        }

        public bool AddGoalWithTile(int tileId, int requiredCount = 1)
        {
            if (Session == null || _targets.Count >= 3 || tileId <= 0)
            {
                return false;
            }

            _targets.Add(new LevelTargetRequirement(tileId, Mathf.Max(1, requiredCount)));
            sessionStateHolder.MarkDirty("Added target.");
            RevalidateSession();
            NotifyChanged();
            return true;
        }

        public void UpdateGoal(int index, int tileId, int requiredCount)
        {
            if (Session == null || index < 0 || index >= _targets.Count)
            {
                return;
            }

            _targets[index] = new LevelTargetRequirement(tileId, Mathf.Max(1, requiredCount));
            sessionStateHolder.MarkDirty($"Updated goal #{index + 1}.");
            RevalidateSession();
            NotifyChanged();
        }

        public void UpdateGoalTile(int index, int tileId)
        {
            if (Session == null || index < 0 || index >= _targets.Count || tileId <= 0)
            {
                return;
            }

            LevelTargetRequirement existingGoal = _targets[index];
            _targets[index] = new LevelTargetRequirement(tileId, Mathf.Max(1, existingGoal.requiredCount));
            sessionStateHolder.MarkDirty($"Updated target #{index + 1} tile.");
            RevalidateSession();
            NotifyChanged();
        }

        public void UpdateGoalCount(int index, int requiredCount)
        {
            if (Session == null || index < 0 || index >= _targets.Count)
            {
                return;
            }

            LevelTargetRequirement existingGoal = _targets[index];
            _targets[index] = new LevelTargetRequirement(existingGoal.tileId, Mathf.Max(1, requiredCount));
            sessionStateHolder.MarkDirty($"Updated target #{index + 1} count.");
            RevalidateSession();
            NotifyChanged();
        }

        public void RemoveGoal(int index)
        {
            if (Session == null || index < 0 || index >= _targets.Count)
            {
                return;
            }

            _targets.RemoveAt(index);
            sessionStateHolder.MarkDirty($"Removed goal #{index + 1}.");
            RevalidateSession();
            NotifyChanged();
        }

        public Match3LevelDataValidationResult ValidateCurrentLevel()
        {
            if (Session == null)
            {
                return new Match3LevelDataValidationResult();
            }

            LevelSaveData document = LevelEditorDocumentMapper.BuildDocument(Session, TileDatabase, _targets);
            Match3LevelDataValidationResult validation = ValidateDocument(document.levelData);
            sessionStateHolder.SetValidation(validation);
            NotifyChanged();
            return validation;
        }

        public List<LevelEditorPaletteEntryData> GetPaletteEntries(BoardLayer layer)
        {
            return LevelEditorPaletteBuilder.Build(TileDatabase, layer);
        }

        public List<LevelEditorPaletteEntryData> GetPaletteEntries(LevelEditorPaletteSectionType sectionType)
        {
            return LevelEditorPaletteBuilder.BuildSection(TileDatabase, sectionType);
        }

        public List<LevelEditorPaletteEntryData> GetGoalOptions()
        {
            return LevelEditorPaletteBuilder.BuildTargetEntries(TileDatabase);
        }

        public List<LevelEditorPaletteEntryData> GetTargetOptions(LevelEditorPaletteSectionType sectionType)
        {
            return LevelEditorPaletteBuilder.BuildTargetEntries(TileDatabase, sectionType);
        }

        public IReadOnlyList<string> GetAvailableLevelIds(string filter = null)
        {
            return _levelCatalogProvider.GetLevelIds(filter);
        }

        public string GetContentDisplayName(BoardLayer layer, int contentId)
        {
            if (contentId <= 0 || TileDatabase == null)
            {
                return "None";
            }

            BoardContentDefinitionSO definition = null;
            switch (layer)
            {
                case BoardLayer.Underlay:
                    definition = TileDatabase.GetUnderlayDefinition(contentId);
                    break;
                case BoardLayer.Tile:
                    definition = TileDatabase.GetTileDefinition(contentId);
                    break;
                case BoardLayer.Overlay:
                    definition = TileDatabase.GetOverlayDefinition(contentId);
                    break;
            }

            return definition != null ? definition.name : "None";
        }

        private Match3LevelDataValidationResult ValidateDocument(Match3LevelData levelData)
        {
            return Match3LevelDataValidator.Validate(levelData, TileDatabase);
        }

        private void ClearSelectedCellLayer(BoardLayer layer, string actionLabel)
        {
            if (Session?.board == null || selectionStateHolder == null || !selectionStateHolder.HasSelectedCell)
            {
                return;
            }

            int x = selectionStateHolder.SelectedX;
            int y = selectionStateHolder.SelectedY;
            LevelEditorCellState cell = Session.board.GetCell(x, y);

            int underlayId = cell.UnderlayId;
            int tileId = cell.TileId;
            int overlayId = cell.OverlayId;

            switch (layer)
            {
                case BoardLayer.Underlay:
                    underlayId = 0;
                    break;
                case BoardLayer.Tile:
                    tileId = 0;
                    break;
                case BoardLayer.Overlay:
                    overlayId = 0;
                    break;
            }

            ApplyCellContent(x, y, underlayId, tileId, overlayId);
            sessionStateHolder.MarkDirty($"{actionLabel} at ({x}, {y}).");
            RevalidateSession();
            NotifyChanged();
        }

        private void ApplyCellContent(int x, int y, int underlayId, int tileId, int overlayId)
        {
            bool hasAnyContent = underlayId > 0 || tileId > 0 || overlayId > 0;
            if (!hasAnyContent)
            {
                Session.board.ClearCell(x, y, false);
                return;
            }

            Session.board.SetUnderlayId(x, y, underlayId);
            Session.board.SetTileId(x, y, tileId);
            Session.board.SetOverlayId(x, y, overlayId);
            Session.board.SetPlayable(x, y, true);
        }

        private void RevalidateSession()
        {
            if (Session == null || TileDatabase == null)
            {
                return;
            }

            LevelSaveData document = LevelEditorDocumentMapper.BuildDocument(Session, TileDatabase, _targets);
            sessionStateHolder.SetValidation(ValidateDocument(document.levelData));
        }

        private List<int> BuildDefaultSpawnables()
        {
            List<int> spawnables = new List<int>();
            if (TileDatabase == null)
            {
                return spawnables;
            }

            IReadOnlyList<BoardContentDefinitionSO> entries = TileDatabase.Tiles;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] is TileDefinitionSO tileDefinition &&
                    tileDefinition.TileKind == TileKind.Normal &&
                    tileDefinition.CanSpawnOnRefill &&
                    tileDefinition.SpawnWeight > 0)
                {
                    spawnables.Add(tileDefinition.TileId);
                }
            }

            return spawnables;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
