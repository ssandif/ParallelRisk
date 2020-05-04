using System.Collections.Generic;

namespace ParallelRisk
{
    public readonly struct Move : IMove<BoardState>
    {
        private readonly BoardState _state;
        public int FromId { get; }
        public int ToId { get; }
        public int FortifyCount { get; }
        public bool IsAttack { get; }

        public static Move Pass(in BoardState state)
        {
            return new Move(state, 0, 0, 0, false);
        }

        public static Move PassAndFortify(in BoardState state, int fromId, int toId, int fortifyCount)
        {
            return new Move(state, fromId, toId, fortifyCount, false);
        }

        public static Move Attack(in BoardState state, int fromId, int toId)
        {
            return new Move(state, fromId, toId, 0, true);
        }

        private Move(in BoardState state, int fromId, int toId, int fortifyCount, bool isAttack)
        {
            _state = state;
            FromId = fromId;
            ToId = toId;
            FortifyCount = fortifyCount;
            IsAttack = isAttack;
        }

        public IEnumerable<(double Probability, BoardState Outcome)> Outcomes()
        {
            //if (Action == MoveType.Pass)
            if (!IsAttack)
            {
                if (FortifyCount > 0)
                    yield return (1, _state.PassAndFortify(FromId, ToId, FortifyCount));
                else
                    yield return (1, _state.Pass());

                yield break;
            }

            Territory from = _state.Territories[FromId];
            Territory to = _state.Territories[ToId];

            // From is 1+unit count
            // probabilities http://datagenetics.com/blog/november22011/index.html
            if (from.TroopCount == 2)
            {
                // 1 attacker vs defense
                if (to.TroopCount == 1) {
                    // Attack wins
                    yield return (41.67, _state.AttackUpdate(from.ModifyTroops(-1), to.ChangeControl(from.Player, 1)));
                    // Defense wins
                    yield return (58.33, _state.AttackUpdate(from.ModifyTroops(-1), to));
                } else if (to.TroopCount == 2) {
                    // Attack wins
                    yield return (25.46, _state.AttackUpdate(from.ModifyTroops(-1), to.ChangeControl(from.Player, 1)));
                    // Defense wins
                    yield return (74.33, _state.AttackUpdate(from.ModifyTroops(-1), to));
                } else if (to.TroopCount >= 2) {
                    // Attack wins
                    yield return (25.46, _state.AttackUpdate(from.ModifyTroops(-1), to.ModifyTroops(-1)));
                    // Defense wins
                    yield return (74.33, _state.AttackUpdate(from.ModifyTroops(-1), to));
                }
            } else if (from.TroopCount == 3) {
                // 2 attackers vs defense
                if (to.TroopCount == 2) {
                    // 2-0 Attack Win
                    yield return (22.76, _state.AttackUpdate(from.ModifyTroops(-2), to.ChangeControl(from.Player, 2)));
                    // Draw
                    yield return (32.41, _state.AttackUpdate(from.ModifyTroops(-1), to.ModifyTroops(-1)));
                    // 2-0 Defense Win
                    yield return (44.83, _state.AttackUpdate(from.ModifyTroops(-2), to));
                } else if (to.TroopCount == 1) {
                    // 2-0 Attack Win
                    yield return (57.87, _state.AttackUpdate(from.ModifyTroops(-2), to.ChangeControl(from.Player, 2)));
                    // Defense wins 1
                    yield return (42.13, _state.AttackUpdate(from.ModifyTroops(-1), to));
                } else if (to.TroopCount >= 3) {
                    // 2-0 Attack Win
                    yield return (22.76, _state.AttackUpdate(from.ModifyTroops(-2), to.ModifyTroops(-2)));
                    // Draw
                    yield return (32.41, _state.AttackUpdate(from.ModifyTroops(-1), to.ModifyTroops(-1)));
                    // 2-0 Defense Win
                    yield return (44.83, _state.AttackUpdate(from.ModifyTroops(-2), to));
                }
            } else if (from.TroopCount >= 4) {
                // 3+ versus defense
                if (to.TroopCount == 1) {
                    int occupiers = _state.OptimalOccupyingTroops(FromId, ToId, 3);
                    // Attack wins
                    yield return (65.97, _state.AttackUpdate(from.ModifyTroops(-occupiers), to.ChangeControl(from.Player, occupiers)));
                    // Defense wins
                    yield return (34.03, _state.AttackUpdate(from.ModifyTroops(-1), to));
                } else if (to.TroopCount == 2) {
                    int occupiers = _state.OptimalOccupyingTroops(FromId, ToId, 3);
                    // 2-0 Attack Win
                    yield return (37.17, _state.AttackUpdate(from.ModifyTroops(-occupiers), to.ChangeControl(from.Player, occupiers)));
                    // Draw
                    yield return (29.26, _state.AttackUpdate(from.ModifyTroops(-1), to.ModifyTroops(-1)));
                    // 2-0 Defense Win
                    yield return (33.58, _state.AttackUpdate(from.ModifyTroops(-2), to));
                } else if (to.TroopCount >= 3) {
                    // 2-0 Attack Win
                    yield return (37.17, _state.AttackUpdate(from.ModifyTroops(-3), to.ModifyTroops(-2)));
                    // Draw
                    yield return (29.26, _state.AttackUpdate(from.ModifyTroops(-1), to.ModifyTroops(-1)));
                    // 2-0 Defense Win
                    yield return (33.58, _state.AttackUpdate(from.ModifyTroops(-2), to));
                }
            }
        }
    }
}