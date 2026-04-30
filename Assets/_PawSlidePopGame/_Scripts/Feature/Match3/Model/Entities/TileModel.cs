using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Core;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Factory;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Board;
using _PawSlidePopGame._Scripts.Feature.Match3.Presentation;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities
{
    public class TileModel
    {
        public int InstanceId { get; }
        public TileDefinitionSO Definition { get; }
        public ITileLogic Logic { get; }
        public int CurrentHP { get; private set; }

        public int TileId => Definition.TileId;
        public TileKind TileKind => Definition.TileKind;
        public TileLogicType LogicType => Definition.LogicType;
        public bool IsDead => CurrentHP <= 0;

        public TileModel(int instanceId, TileDefinitionSO definition)
        {
            InstanceId = instanceId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            CurrentHP = definition.DefaultHP;
            Logic = TileLogicFactory.CreateLogic(definition);
        }

        public void TakeDamage(int damage)
        {
            CurrentHP = Math.Max(0, CurrentHP - damage);
        }

        public bool CanBeMoved()
        {
            return Logic != null && Logic.CanBeMoved();
        }

        public bool CanFall()
        {
            return Logic != null && Logic.CanFall();
        }

        public bool CanMatch()
        {
            return Logic != null && Logic.CanMatch();
        }

        public bool IsMatchableWith(TileModel other)
        {
            if (other == null || !CanMatch() || !other.CanMatch())
            {
                return false;
            }

            if (!(Definition is NormalAnimalTileDefinitionSO leftDefinition) ||
                !(other.Definition is NormalAnimalTileDefinitionSO rightDefinition))
            {
                return false;
            }

            return leftDefinition.AnimalId != AnimalTileId.None &&
                   leftDefinition.AnimalId == rightDefinition.AnimalId;
        }

        public void Match(BoardModel board, CellModel cell, BoardFxContext fxContext = null)
        {
            Logic?.OnMatched(board, cell, fxContext);
        }

        public void Explode(BoardModel board, CellModel cell, BoardFxContext fxContext = null)
        {
            if (IsDead)
            {
                return;
            }

            Logic?.OnExploded(board, cell, fxContext);
        }

        public void Activate(BoardModel board, CellModel cell, BoardFxContext fxContext = null)
        {
            Logic?.OnActivated(board, cell, fxContext);
        }
    }
}
