using System.Collections.Generic;

namespace ParallelRisk
{
    public readonly struct Move : IMove<BoardState>
    {
        private readonly BoardState _state;
        public readonly Territory From { get; }
        public readonly Territory To { get; }
        public readonly bool IsAttack { get; }

        public static Move PassTurn(in BoardState state)
        {
            return new Move(state, default, default, false);
        }

        public static Move Attack(in BoardState state, in Territory from, in Territory to)
        {
            return new Move(state, from, to, true);
        }

        private Move(in BoardState state, in Territory from, in Territory to, bool passTurn)
        {
            _state = state;
            From = from;
            To = to;
            IsAttack = passTurn;
        }

        public IEnumerable<(double, BoardState)> Outcomes()
        {
            if (!IsAttack)
            {
                yield return (1, _state.PassTurn());
                yield break;
            }

            // probabilities http://datagenetics.com/blog/november22011/index.html
            if (From.TroopCount == 2 && To.TroopCount == 1)
            {
                // Attack wins
                yield return (41.67, _state.AttackUpdate(From.ModifyTroops(-1), To.ChangeControl(From.Player, 1)));
                // Defense wins
                yield return (58.33, _state.AttackUpdate(From.ModifyTroops(-1), To));
            }
        }
    }
}