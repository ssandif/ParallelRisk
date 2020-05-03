using static System.Math;

namespace ParallelRisk
{
    public static class Minimax
    {
        public static TMove SerialMove<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            // this should be returned at the end of the minimax player's turn
            if (depth == 0 || node.IsTerminal() || !node.IsMaxPlayerTurn)
                return default;

            if (node.IsMaxPlayerTurn)
            {
                //System.Console.WriteLine("Attempting move");   
                (TMove Move, double Utility) value = (default, double.PositiveInfinity);
                foreach (TMove move in node.ReinforceMoves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth);
                    if (newUtil < value.Utility)
                        value = (move, newUtil);
                }
                return value.Move;
            } else {
                throw new System.InvalidOperationException("Should call SerialMove on AI turn");
            }

            //return default;
        }

        public static TMove Serial<TState, TMove>(TState node, int depth)
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
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth);
                    if (newUtil > value.Utility)
                        value = (move, newUtil);
                }
                return value.Move;
            }
            else
            {
                (TMove Move, double Utility) value = (default, double.PositiveInfinity);
                foreach (TMove move in node.Moves())
                {
                    double newUtil = SerialEstimatedOutcome<TState, TMove>(move, depth);
                    if (newUtil < value.Utility)
                        value = (move, newUtil);
                }
                return value.Move;
            }
        }

        private static double SerialMinimax<TState, TMove>(TState node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            if (depth == 0 || node.IsTerminal())
                return node.Heuristic();

            if (node.IsMaxPlayerTurn)
            {
                double value = double.NegativeInfinity;
                foreach (TMove atk in node.Moves())
                    value = Max(value, SerialEstimatedOutcome<TState, TMove>(atk, depth));
                return value;
            }
            else
            {
                double value = double.PositiveInfinity;
                foreach (TMove move in node.Moves())
                    value = Min(value, SerialEstimatedOutcome<TState, TMove>(move, depth));
                return value;
            }
        }

        private static double SerialEstimatedOutcome<TState, TMove>(TMove node, int depth)
            where TState : IState<TMove>
            where TMove : IMove<TState>
        {
            double value = 0;
            foreach ((double probability, TState outcome) in node.Outcomes())
            {
                value += probability * SerialMinimax<TState, TMove>(outcome, depth - 1);
            }
            return value;
        }
    }
}
