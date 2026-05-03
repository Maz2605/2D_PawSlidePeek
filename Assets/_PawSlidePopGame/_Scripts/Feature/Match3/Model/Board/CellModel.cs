using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Model.Board
{
    public class CellModel
    {
        public int X { get; }
        public int Y { get; }
        public bool IsPlayable { get; }
        public TileModel BaseTile { get; private set; }
        public TileModel OverlayTile { get; private set; }
        public TileModel CurrentTile => BaseTile;
        public TileModel TopTile => OverlayTile ?? BaseTile;
        public Action<TileModel> OnTileChanged;

        public CellModel(int x, int y, bool isPlayable)
        {
            X = x;
            Y = y;
            IsPlayable = isPlayable;
        }

        public void SetTile(TileModel newTile)
        {
            SetBaseTile(newTile);
        }

        public void SetBaseTile(TileModel newTile)
        {
            BaseTile = newTile;
            OnTileChanged?.Invoke(BaseTile);
        }

        public void SetOverlayTile(TileModel newTile)
        {
            OverlayTile = newTile;
            OnTileChanged?.Invoke(BaseTile);
        }

        public void ClearTile()
        {
            ClearBaseTile();
        }

        public void ClearBaseTile()
        {
            BaseTile = null;
            OnTileChanged?.Invoke(null);
        }

        public void ClearOverlayTile()
        {
            OverlayTile = null;
            OnTileChanged?.Invoke(BaseTile);
        }

        public bool IsEmpty()
        {
            return IsBaseEmpty();
        }

        public bool HasTile()
        {
            return HasBaseTile();
        }

        public bool IsBaseEmpty()
        {
            return BaseTile == null && IsPlayable;
        }

        public bool HasBaseTile()
        {
            return BaseTile != null;
        }

        public bool HasOverlayTile()
        {
            return OverlayTile != null;
        }

        public bool HasBlockingOverlay()
        {
            return OverlayTile != null && OverlayTile.BlocksTileBelow();
        }

        public bool HasOverlayLogic(TileLogicType logicType)
        {
            return OverlayTile != null && OverlayTile.LogicType == logicType;
        }

        public bool CanBaseTileMatch()
        {
            return BaseTile != null && !HasBlockingOverlay() && BaseTile.CanMatch();
        }

        public bool CanBaseTileActivate()
        {
            return BaseTile != null && !HasBlockingOverlay();
        }

        public TileModel GetTile(TileStackLayer layer)
        {
            return layer == TileStackLayer.Overlay ? OverlayTile : BaseTile;
        }

        public void SetTile(TileStackLayer layer, TileModel tile)
        {
            if (layer == TileStackLayer.Overlay)
            {
                SetOverlayTile(tile);
                return;
            }

            SetBaseTile(tile);
        }

        public void ClearTile(TileStackLayer layer)
        {
            if (layer == TileStackLayer.Overlay)
            {
                ClearOverlayTile();
                return;
            }

            ClearBaseTile();
        }

        public TileStackLayer? GetTileLayer(TileModel tile)
        {
            if (tile == null)
            {
                return null;
            }

            if (ReferenceEquals(OverlayTile, tile))
            {
                return TileStackLayer.Overlay;
            }

            if (ReferenceEquals(BaseTile, tile))
            {
                return TileStackLayer.Base;
            }

            return null;
        }
    }
}
