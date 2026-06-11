using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct SpecialTileFeedbackPayload
    {
        public TileLogicType LogicType { get; }

        public SpecialTileFeedbackPayload(TileLogicType logicType)
        {
            LogicType = logicType;
        }
    }
}
