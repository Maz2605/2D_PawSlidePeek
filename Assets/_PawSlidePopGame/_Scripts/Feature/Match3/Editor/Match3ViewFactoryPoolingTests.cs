using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.View;
using _PawSlidePopGame._Scripts.Feature.Match3.View.Factory;
using _PawSlidePopGame.Scripts.DesignPattern.ObjectPooling;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Editor
{
    public class Match3ViewFactoryPoolingTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            PoolingManager manager = UnityEngine.Object.FindAnyObjectByType<PoolingManager>();
            if (manager != null)
            {
                manager.ClearPools();
                UnityEngine.Object.DestroyImmediate(manager.gameObject);
            }

            PooledItem[] pooledItems = Resources.FindObjectsOfTypeAll<PooledItem>();
            for (int i = 0; i < pooledItems.Length; i++)
            {
                if (pooledItems[i] != null && pooledItems[i].gameObject.scene.IsValid())
                {
                    UnityEngine.Object.DestroyImmediate(pooledItems[i].gameObject);
                }
            }

            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void TileViewFactory_CreateReturnCreate_ReusesSameInstanceAndResetsState()
        {
            Match3TileViewFactory factory = new Match3TileViewFactory();
            Transform parent = CreateGameObject("TileParent").transform;
            MechanicTileView prefab = CreateTilePrefab();
            DeliveryTileDefinitionSO definition = CreateMechanicDefinition(prefab);

            Match3TileView firstView = factory.CreateVisual(
                new TileViewSpawnData(
                    prefab,
                    new TileModel(1, definition),
                    definition,
                    new Vector3(1f, 2f, 0f),
                    false,
                    "Tile_A"),
                parent);

            SpriteRenderer bodyRenderer = GetPrivateField<SpriteRenderer>(firstView, "bodyRenderer");
            SpriteRenderer shadowRenderer = GetPrivateField<SpriteRenderer>(firstView, "shadowRenderer");

            firstView.SetShadowState(TileShadowState.Active);
            firstView.transform.localScale = new Vector3(2f, 2f, 2f);
            firstView.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            bodyRenderer.color = Color.red;

            factory.ReturnVisual(firstView);

            Match3TileView secondView = factory.CreateVisual(
                new TileViewSpawnData(
                    prefab,
                    new TileModel(2, definition),
                    definition,
                    new Vector3(-2f, 3f, 0f),
                    false,
                    "Tile_B"),
                parent);

            Assert.That(secondView, Is.SameAs(firstView));
            Assert.That(secondView.TileInstanceId, Is.EqualTo(2));
            Assert.That(secondView.transform.localPosition, Is.EqualTo(new Vector3(-2f, 3f, 0f)));
            Assert.That(secondView.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(Quaternion.Angle(secondView.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f));
            Assert.That(bodyRenderer.color, Is.EqualTo(Color.white));
            Assert.That(shadowRenderer.enabled, Is.False);
        }

        [Test]
        public void CellViewFactory_CreateReturnCreate_ReusesSameInstanceAndResetsTransform()
        {
            Match3CellViewFactory factory = new Match3CellViewFactory();
            Transform parent = CreateGameObject("CellParent").transform;
            Match3CellView prefab = CreateCellPrefab();

            Match3CellView firstView = factory.CreateVisual(
                new CellViewSpawnData(prefab, new CellModel(0, 0, true), new Vector3(1f, 1f, 0f), "Cell_A"),
                parent);

            firstView.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            firstView.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);

            factory.ReturnVisual(firstView);

            Match3CellView secondView = factory.CreateVisual(
                new CellViewSpawnData(prefab, new CellModel(1, 1, true), new Vector3(-1f, 2f, 0f), "Cell_B"),
                parent);

            Assert.That(secondView, Is.SameAs(firstView));
            Assert.That(secondView.transform.localPosition, Is.EqualTo(new Vector3(-1f, 2f, 0f)));
            Assert.That(secondView.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(Quaternion.Angle(secondView.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        private MechanicTileView CreateTilePrefab()
        {
            GameObject prefabObject = CreateGameObject("TilePrefab");
            MechanicTileView tileView = prefabObject.AddComponent<MechanicTileView>();
            SpriteRenderer bodyRenderer = prefabObject.AddComponent<SpriteRenderer>();
            SpriteRenderer shadowRenderer = prefabObject.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = CreateSprite();
            shadowRenderer.sprite = bodyRenderer.sprite;

            SetPrivateField(tileView, "bodyRenderer", bodyRenderer);
            SetPrivateField(tileView, "shadowRenderer", shadowRenderer);

            return tileView;
        }

        private Match3CellView CreateCellPrefab()
        {
            GameObject prefabObject = CreateGameObject("CellPrefab");
            Match3CellView cellView = prefabObject.AddComponent<Match3CellView>();
            GameObject anchor = CreateGameObject("TileAnchor");
            anchor.transform.SetParent(prefabObject.transform, false);
            SetPrivateField(cellView, "tileAnchor", anchor.transform);
            return cellView;
        }

        private DeliveryTileDefinitionSO CreateMechanicDefinition(MechanicTileView prefab)
        {
            DeliveryTileDefinitionSO definition = ScriptableObject.CreateInstance<DeliveryTileDefinitionSO>();
            _createdObjects.Add(definition);
            SetPrivateField(definition, "tileId", 201);
            SetPrivateField(definition, "tileViewPrefab", prefab);
            return definition;
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            _createdObjects.Add(texture);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
            _createdObjects.Add(sprite);
            return sprite;
        }

        private GameObject CreateGameObject(string name)
        {
            GameObject gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private static TField GetPrivateField<TField>(object target, string fieldName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            return field != null ? (TField)field.GetValue(target) : default(TField);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }

            field.SetValue(target, value);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}

