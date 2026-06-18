using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.LevelEditor.Validation;
using NUnit.Framework;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.Tests.Editor
{
    public class Match3LevelDataValidatorTests
    {
        [Test]
        public void Validate_AllowsMixedContentTargetsWhenDefinitionsSupportObjectives()
        {
            Match3LevelData levelData = CreateLevelData();
            levelData.targets = new List<LevelTargetData>
            {
                new LevelTargetData(101, 1),
                new LevelTargetData(301, 1),
                new LevelTargetData(201, 1),
                new LevelTargetData(401, 1)
            };
            levelData.overlayLayout[0] = 301;
            levelData.underlayLayout[0] = 401;

            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, CreateSprite()),
                CreateIceOverlay(301, CreateSprite()),
                CreateMechanicTile(201, CreateSprite()),
                CreateLockUnderlay(401, CreateSprite()));

            Match3LevelDataValidationResult validation = Match3LevelDataValidator.Validate(levelData, database);

            Assert.That(validation.ErrorCount, Is.EqualTo(0));
        }

        [Test]
        public void Validate_FlagsUnsupportedUnderlayTargets()
        {
            Match3LevelData levelData = CreateLevelData();
            levelData.targets = new List<LevelTargetData> { new LevelTargetData(402, 1) };
            levelData.underlayLayout[0] = 402;

            Match3TileDatabaseSO database = CreateDatabase(
                CreateNormalTile(101, CreateSprite()),
                CreateConveyorUnderlay(402, CreateSprite()));

            Match3LevelDataValidationResult validation = Match3LevelDataValidator.Validate(levelData, database);

            Assert.That(validation.Issues.Any(issue => issue.Code == "TARGET_UNSUPPORTED"), Is.True);
        }

        [Test]
        public void Validate_WarnsWhenTargetIconIsMissing()
        {
            Match3LevelData levelData = CreateLevelData();
            levelData.targets = new List<LevelTargetData> { new LevelTargetData(101, 1) };

            Match3TileDatabaseSO database = CreateDatabase(CreateNormalTile(101));

            Match3LevelDataValidationResult validation = Match3LevelDataValidator.Validate(levelData, database);

            Assert.That(validation.Issues.Any(issue => issue.Code == "TARGET_ICON_MISSING"), Is.True);
            Assert.That(validation.ErrorCount, Is.EqualTo(0));
        }

        private static Match3LevelData CreateLevelData()
        {
            return new Match3LevelData
            {
                levelID = "ValidatorTest",
                width = 1,
                height = 1,
                tileLayout = new[] { 101 },
                overlayLayout = new[] { 0 },
                underlayLayout = new[] { 0 },
                playableMask = new[] { true },
                spawnableTileIds = new List<int> { 101 }
            };
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

        private static NormalAnimalTileDefinitionSO CreateNormalTile(int tileId, Sprite icon = null)
        {
            NormalAnimalTileDefinitionSO tile = ScriptableObject.CreateInstance<NormalAnimalTileDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "animalId", AnimalTileId.Cat);
            SetPrivateField(tile, "canSpawnOnRefill", true);
            SetPrivateField(tile, "spawnWeight", 1);
            SetPrivateField(tile, "icon", icon);
            return tile;
        }

        private static IceOverlayDefinitionSO CreateIceOverlay(int tileId, Sprite icon)
        {
            IceOverlayDefinitionSO tile = ScriptableObject.CreateInstance<IceOverlayDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "defaultHP", 1);
            SetPrivateField(tile, "canSpawnOnRefill", false);
            SetPrivateField(tile, "spawnWeight", 0);
            SetPrivateField(tile, "icon", icon);
            return tile;
        }

        private static DeliveryTileDefinitionSO CreateMechanicTile(int tileId, Sprite icon)
        {
            DeliveryTileDefinitionSO tile = ScriptableObject.CreateInstance<DeliveryTileDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "canSpawnOnRefill", false);
            SetPrivateField(tile, "spawnWeight", 0);
            SetPrivateField(tile, "icon", icon);
            return tile;
        }

        private static LockCellDefinitionSO CreateLockUnderlay(int tileId, Sprite icon)
        {
            LockCellDefinitionSO tile = ScriptableObject.CreateInstance<LockCellDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "defaultHP", 1);
            SetPrivateField(tile, "canSpawnOnRefill", false);
            SetPrivateField(tile, "spawnWeight", 0);
            SetPrivateField(tile, "icon", icon);
            return tile;
        }

        private static ConveyorCellDefinitionSO CreateConveyorUnderlay(int tileId, Sprite icon)
        {
            ConveyorCellDefinitionSO tile = ScriptableObject.CreateInstance<ConveyorCellDefinitionSO>();
            SetPrivateField(tile, "tileId", tileId);
            SetPrivateField(tile, "canSpawnOnRefill", false);
            SetPrivateField(tile, "spawnWeight", 0);
            SetPrivateField(tile, "icon", icon);
            return tile;
        }

        private static Sprite CreateSprite()
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
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
