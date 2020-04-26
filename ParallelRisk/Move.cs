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

            // From is 1+unit count
            // probabilities http://datagenetics.com/blog/november22011/index.html
            if (From.TroopCount == 2)
            {
                // 1 attacker vs defense
                if (To.TroopCount == 1) {
                    // Attack wins
                    yield return (41.67, _state.AttackUpdate(From.ModifyTroops(-1), To.ChangeControl(From.Player, 1)));
                    // Defense wins
                    yield return (58.33, _state.AttackUpdate(From.ModifyTroops(-1), To));
                }
            } else if (From.TroopCount == 3) {
                // 2 attackers vs defense
                if (To.TroopCount == 2) {
                    // 2-0 Attack Win
                    yield return (22.76, _state.AttackUpdate(From.ModifyTroops(-1), To.ChangeControl(From.Player, 1)));
                    // Draw
                    yield return (32.41, _state.AttackUpdate(From.ModifyTroops(-1), To.ModifyTroops(-1)));
                    // 2-0 Defense Win
                    yield return (44.83, _state.AttackUpdate(From.ModifyTroops(-2), To));
                } else if (To.TroopCount == 1) {
                    // 2-0 Attack Win
                    yield return (57.87, _state.AttackUpdate(From.ModifyTroops(-1), To.ChangeControl(From.Player, 1)));
                    // Defense wins 1
                    yield return (42.13, _state.AttackUpdate(From.ModifyTroops(-1), To));
                }
            } else if (From.TroopCount >= 4) {
                // 3+ versus defense
                if (To.TroopCount == 1) {
                    // Attack wins
                    yield return (65.97, _state.AttackUpdate(From.ModifyTroops(-1), To.ChangeControl(From.Player, 1)));
                    // Defense wins
                    yield return (34.03, _state.AttackUpdate(From.ModifyTroops(-1), To));
                } else if (To.TroopCount == 2) {
                    // 2-0 Attack Win
                    yield return (37.17, _state.AttackUpdate(From.ModifyTroops(-1), To.ChangeControl(From.Player, 1)));
                    // Draw
                    yield return (29.26, _state.AttackUpdate(From.ModifyTroops(-1), To.ModifyTroops(-1)));
                    // 2-0 Defense Win
                    yield return (33.58, _state.AttackUpdate(From.ModifyTroops(-2), To));
                }
            }

        }
    }
}