using System.Linq;
using _PawSlidePopGame._Scripts.Data.LevelProvider;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Contracts;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Core.Model;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Scene;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.UI;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Tests.Editor
{
    public class LevelEditorApplicationServiceTests
    {
        private GameObject _root;
        private LevelEditorRuntimeBridge _bridge;
        private LevelEditorSessionStateHolder _sessionHolder;
        private LevelEditorSelectionStateHolder _selectionHolder;
        private LevelEditorUIController _service;
        private Match3TileDatabaseSO _database;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("LevelEditorTestRoot");
            _bridge = _root.AddComponent<LevelEditorRuntimeBridge>();
            _sessionHolder = _root.AddComponent<LevelEditorSessionStateHolder>();
            _selectionHolder = _root.AddComponent<LevelEditorSelectionStateHolder>();
            _service = _root.AddComponent<LevelEditorUIController>();

            _database = AssetDatabase.LoadAssetAtPath<Match3TileDatabaseSO>("Assets/_PawSlidePopGame/_Data/Tiles/Database.asset");
            _bridge.SetTileDatabaseForEditor(_database);
            _service.Configure(_bridge, _sessionHolder, _selectionHolder);
        }

        [TearDown]
        public void TearDown()
        {
            string assetPath = LevelPathUtility.GetAssetPath("Level_Editor_Test");
            if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.Refresh();
            }

            Object.DestroyImmediate(_root);
        }

        [Test]
        public void ApplySelectionPayload_WritesBoardStateAndMarksDirty()
        {
            _service.NewLevel("Level_Editor_Test", 12, 4, 4, 18);
            _service.SetSelectedLayer(_PawSlidePopGame._Scripts.Feature.Match3.Core.Enum.BoardLayer.Tile, 101);
            _service.SetSelectedLayer(_PawSlidePopGame._Scripts.Feature.Match3.Core.Enum.BoardLayer.Overlay, 301);
            _service.SelectCell(1, 2);

            _service.ApplySelectionPayload();

            var cell = _service.Session.board.GetCell(1, 2);
            Assert.That(cell.UnderlayId, Is.EqualTo(0));
            Assert.That(cell.TileId, Is.EqualTo(101));
            Assert.That(cell.OverlayId, Is.EqualTo(301));
            Assert.That(cell.Playable, Is.True);
            Assert.That(_service.Session.isDirty, Is.True);
        }

        [Test]
        public void SaveLevel_RejectsInitialMatches()
        {
            _service.NewLevel("Level_Editor_Test", 12, 3, 3, 18);
            _service.AddGoalWithTile(105, 3);

            _service.SetSelectedLayer(_PawSlidePopGame._Scripts.Feature.Match3.Core.Enum.BoardLayer.Tile, 101);
            _service.SelectCell(0, 0);
            _service.ApplySelectionPayload();
            _service.SelectCell(1, 0);
            _service.ApplySelectionPayload();
            _service.SelectCell(2, 0);
            _service.ApplySelectionPayload();

            var result = _service.SaveLevel();

            Assert.That(result.success, Is.False);
            Assert.That(result.validationResult, Is.Not.Null);
            Assert.That(result.validationResult.Issues.Any(issue => issue.Code == "INITIAL_MATCH"), Is.True);
        }

        [Test]
        public void SaveLevel_ThenLoadLevel_RoundTripsBoardAndGoals()
        {
            _service.NewLevel("Level_Editor_Test", 12, 4, 4, 18);
            _service.SetSelectedLayer(_PawSlidePopGame._Scripts.Feature.Match3.Core.Enum.BoardLayer.Tile, 101);
            _service.SetSelectedLayer(_PawSlidePopGame._Scripts.Feature.Match3.Core.Enum.BoardLayer.Overlay, 301);
            _service.SelectCell(0, 0);
            _service.ApplySelectionPayload();
            _service.AddGoalWithTile(105, 5);

            var saveResult = _service.SaveLevel();
            Assert.That(saveResult.success, Is.True, saveResult.message);

            var loadResult = _service.LoadLevel("Level_Editor_Test");
            Assert.That(loadResult.success, Is.True, loadResult.message);
            Assert.That(_service.Session.board.GetCell(0, 0).TileId, Is.EqualTo(101));
            Assert.That(_service.Session.board.GetCell(0, 0).UnderlayId, Is.EqualTo(0));
            Assert.That(_service.Session.board.GetCell(0, 0).OverlayId, Is.EqualTo(301));
            Assert.That(_service.Targets.Any(goal => goal.tileId == 105 && goal.requiredCount == 5), Is.True);
        }

        [Test]
        public void ResizeBoard_PreservesExistingCellsWithinNewBounds()
        {
            _service.NewLevel("Level_Editor_Test", 12, 4, 4, 18);
            _service.SetSelectedLayer(_PawSlidePopGame._Scripts.Feature.Match3.Core.Enum.BoardLayer.Tile, 101);
            _service.SetSelectedLayer(_PawSlidePopGame._Scripts.Feature.Match3.Core.Enum.BoardLayer.Overlay, 301);
            _service.SelectCell(1, 1);
            _service.ApplySelectionPayload();
            _service.SelectCell(3, 3);
            _service.ApplySelectionPayload();

            _service.ResizeBoard(2, 2);

            Assert.That(_service.Session.board.width, Is.EqualTo(2));
            Assert.That(_service.Session.board.height, Is.EqualTo(2));
            Assert.That(_service.Session.board.GetCell(1, 1).TileId, Is.EqualTo(101));
            Assert.That(_service.Session.board.GetCell(1, 1).OverlayId, Is.EqualTo(301));
            Assert.That(_service.Session.isDirty, Is.True);
        }

        [Test]
        public void ResizeBoard_ExpandsWithEmptyPlayableCells()
        {
            _service.NewLevel("Level_Editor_Test", 12, 2, 2, 18);
            _service.SetSelectedLayer(_PawSlidePopGame._Scripts.Feature.Match3.Core.Enum.BoardLayer.Tile, 101);
            _service.SelectCell(0, 0);
            _service.ApplySelectionPayload();

            _service.ResizeBoard(4, 3);

            var existingCell = _service.Session.board.GetCell(0, 0);
            var newCell = _service.Session.board.GetCell(3, 2);
            Assert.That(existingCell.TileId, Is.EqualTo(101));
            Assert.That(newCell.TileId, Is.EqualTo(0));
            Assert.That(newCell.UnderlayId, Is.EqualTo(0));
            Assert.That(newCell.OverlayId, Is.EqualTo(0));
            Assert.That(newCell.Playable, Is.True);
        }

        [Test]
        public void SaveLevel_ThenLoadLevel_RoundTripsAfterResize()
        {
            _service.NewLevel("Level_Editor_Test", 12, 2, 2, 18);
            _service.SetSelectedLayer(_PawSlidePopGame._Scripts.Feature.Match3.Core.Enum.BoardLayer.Tile, 101);
            _service.SetSelectedLayer(_PawSlidePopGame._Scripts.Feature.Match3.Core.Enum.BoardLayer.Overlay, 301);
            _service.SelectCell(0, 0);
            _service.ApplySelectionPayload();
            _service.AddGoalWithTile(105, 5);

            _service.ResizeBoard(4, 3);
            var saveResult = _service.SaveLevel();
            Assert.That(saveResult.success, Is.True, saveResult.message);

            var loadResult = _service.LoadLevel("Level_Editor_Test");
            Assert.That(loadResult.success, Is.True, loadResult.message);
            Assert.That(_service.Session.board.width, Is.EqualTo(4));
            Assert.That(_service.Session.board.height, Is.EqualTo(3));
            Assert.That(_service.Session.board.GetCell(0, 0).TileId, Is.EqualTo(101));
            Assert.That(_service.Session.board.GetCell(0, 0).OverlayId, Is.EqualTo(301));
            Assert.That(_service.Targets.Any(goal => goal.tileId == 105 && goal.requiredCount == 5), Is.True);
        }

        [Test]
        public void PaintCell_OnlyWritesActiveTileLayer_WhenNormalOrSpecialSectionActive()
        {
            _service.NewLevel("Level_Editor_Test", 12, 3, 3, 18);
            _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.Overlay, 301);
            _service.PaintCell(1, 1);

            _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.ItemNormal, 101);
            _service.PaintCell(1, 1);

            var cell = _service.Session.board.GetCell(1, 1);
            Assert.That(cell.TileId, Is.EqualTo(101));
            Assert.That(cell.OverlayId, Is.EqualTo(301));
            Assert.That(cell.UnderlayId, Is.EqualTo(0));
            Assert.That(cell.Playable, Is.True);
        }

        [Test]
        public void PaintCell_OnlyWritesOverlay_WhenOverlaySectionActive()
        {
            _service.NewLevel("Level_Editor_Test", 12, 3, 3, 18);
            _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.ItemNormal, 101);
            _service.PaintCell(1, 1);

            _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.Overlay, 301);
            _service.PaintCell(1, 1);

            var cell = _service.Session.board.GetCell(1, 1);
            Assert.That(cell.TileId, Is.EqualTo(101));
            Assert.That(cell.OverlayId, Is.EqualTo(301));
            Assert.That(cell.UnderlayId, Is.EqualTo(0));
            Assert.That(cell.Playable, Is.True);
        }

        [Test]
        public void PaintCell_OnlyWritesUnderlay_WhenUnderlaySectionActive()
        {
            _service.NewLevel("Level_Editor_Test", 12, 3, 3, 18);
            _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.ItemNormal, 101);
            _service.PaintCell(1, 1);

            _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.Underlay, 201);
            _service.PaintCell(1, 1);

            var cell = _service.Session.board.GetCell(1, 1);
            Assert.That(cell.TileId, Is.EqualTo(101));
            Assert.That(cell.OverlayId, Is.EqualTo(0));
            Assert.That(cell.UnderlayId, Is.EqualTo(201));
            Assert.That(cell.Playable, Is.True);
        }

        [Test]
        public void EraseCell_OnlyClearsActiveLayer_WithRightClickEquivalent()
        {
            _service.NewLevel("Level_Editor_Test", 12, 3, 3, 18);
            _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.ItemNormal, 101);
            _service.PaintCell(1, 1);
            _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.Overlay, 301);
            _service.PaintCell(1, 1);

            _service.EraseCell(1, 1);

            var cell = _service.Session.board.GetCell(1, 1);
            Assert.That(cell.TileId, Is.EqualTo(101));
            Assert.That(cell.OverlayId, Is.EqualTo(0));
            Assert.That(cell.UnderlayId, Is.EqualTo(0));
            Assert.That(cell.Playable, Is.True);
        }

        [Test]
        public void EraseCell_DisablesPlayableOnlyWhenCellBecomesEmpty()
        {
            _service.NewLevel("Level_Editor_Test", 12, 3, 3, 18);
            _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.ItemNormal, 101);
            _service.PaintCell(1, 1);
            _service.SetSelectedSectionLayer(LevelEditorPaletteSectionType.Overlay, 301);
            _service.PaintCell(1, 1);

            _service.EraseCell(1, 1);
            Assert.That(_service.Session.board.GetCell(1, 1).Playable, Is.True);

            _service.SetActiveSection(LevelEditorPaletteSectionType.ItemNormal);
            _service.EraseCell(1, 1);

            var cell = _service.Session.board.GetCell(1, 1);
            Assert.That(cell.TileId, Is.EqualTo(0));
            Assert.That(cell.OverlayId, Is.EqualTo(0));
            Assert.That(cell.UnderlayId, Is.EqualTo(0));
            Assert.That(cell.Playable, Is.False);
        }

    }
}
