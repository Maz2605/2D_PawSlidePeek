using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move
{
    public readonly struct BoardMoveRequest
    {
        public MoveAxis Axis { get; }
        public int LineIndex { get; }
        public LineSlideDirection Direction { get; }

        public BoardMoveRequest(MoveAxis axis, int lineIndex, LineSlideDirection direction)
        {
            Axis = axis;
            LineIndex = lineIndex;
            Direction = direction;
        }

        public bool IsValid()
        {
            return Axis == MoveAxis.Row
                ? Direction == LineSlideDirection.Left || Direction == LineSlideDirection.Right
                : Direction == LineSlideDirection.Up || Direction == LineSlideDirection.Down;
        }

        public int GetRotationStep()
        {
            switch (Direction)
            {
                case LineSlideDirection.Right:
                case LineSlideDirection.Down:
                    return 1;
                case LineSlideDirection.Left:
                case LineSlideDirection.Up:
                    return -1;
                default:
                    return 0;
            }
        }
    }
}
