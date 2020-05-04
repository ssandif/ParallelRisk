using static System.Math;

namespace ParallelRisk
{
    public static class AlphaBeta
    {
        public static TMove Serial<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        => Serial<TState, TMove>(node, depth, double.NegativeInfinity, double.PositiveInfinity);

        private static TMove Serial<TState, TMove>(TState node, int depth, double alpha, double beta)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return default;

            if (node.IsMaxPlayerTurn)
            {
                (TMove Move, double Utility) value = (default, double.NegativeInfinity);
                foreach (TMove move in node.Moves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta);
                    if (newUtil > value.Utility)
                        value = (move, newUtil);

                    alpha = Max(alpha, value.Utility);
                    if (alpha >= beta)
                        break;
                }
                return value.Move;
            }
            else
            {
                (TMove Move, double Utility) value = (default, double.PositiveInfinity);
                foreach (TMove move in node.Moves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta);
                    if (newUtil < value.Utility)
                        value = (move, newUtil);

                    beta = Min(beta, value.Utility);
                    if (alpha >= beta)
                        break;
                }
                return value.Move;
            }
        }

        private static double SerialAlphaBeta<TState, TMove>(TState node, int depth, double alpha, double beta)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return node.Heuristic();

            if (node.IsMaxPlayerTurn)
            {
                double value = double.NegativeInfinity;
                foreach (TMove atk in node.Moves())
                {
                    value = Max(value, SerialEstimatedOutcome<TState, TMove>(atk, depth, alpha, beta));

                    alpha = Max(alpha, value);
                    if (alpha >= beta)
                        break;
                }
                return value;
            }
            else
            {
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                {
                    value = Min(value, SerialEstimatedOutcome<TState, TMove>(move, depth, alpha, beta));

                    beta = Min(beta, value);
                    if (alpha >= beta)
                        break;
                }
                return value;
            }
        }

        private static double SerialEstimatedOutcome<TState, TMove>(TMove node, int depth, double alpha, double beta)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            double value = 0;
            foreach ((double probability, TState outcome) in node.Outcomes())
            {
                value += probability * SerialAlphaBeta<TState, TMove>(outcome, depth - 1, alpha, beta);
            }
            return value;
        }
    }
}
