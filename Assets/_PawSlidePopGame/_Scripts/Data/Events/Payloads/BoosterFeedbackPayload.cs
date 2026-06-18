using _PawSlidePopGame._Scripts.Feature.Match3.Boosters;

namespace _PawSlidePopGame._Scripts.Data.Events.Payloads
{
    public readonly struct BoosterFeedbackPayload
    {
        public BoosterType BoosterType { get; }

        public BoosterFeedbackPayload(BoosterType boosterType)
        {
            BoosterType = boosterType;
        }
    }
}
