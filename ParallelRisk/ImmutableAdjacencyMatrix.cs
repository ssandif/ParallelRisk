using System;
using System.Collections.Generic;

namespace ParallelRisk
{
    // An immutable adjacency matrix for use with undirected graphs.
    public readonly struct ImmutableAdjacencyMatrix

    {
        // True = connection exists, false = no connection
        private readonly bool[,] _matrix;

        private ImmutableAdjacencyMatrix(bool[,] matrix)
        {
            _matrix = matrix;
        }

        // Returns the territory id of every territory adjacent to the specified territory.
        public IEnumerable<int> Adjacent(int id)
        {
            // Go across the row represented by "id", checking for the bool being true
            for (int col = 0; col < _matrix.GetLength(1); col++)
            {
                if (_matrix[id, col])
                    yield return col;
            }
        }

        // Creates a new Builder object used to contruct an ImmutableAdjacencyMatrix. Capacity must be equal to the
        // number of nodes in the graph. The adjacency matrix starts with no connections between nodes.
        public static Builder CreateBuilder(int capacity)
        {
            return new Builder(capacity);
        }

        // Builder class used to contruct an ImmutableAdjacencyMatrix, initialized to no connections.
        public class Builder
        {
            private bool[,] _matrix;

            internal Builder(int capacity)
            {
                _matrix = new bool[capacity, capacity];
            }

            // Get or set a connection between nodes. The graph is assumed to be undirected, so you only need to set
            // one direction.
            public bool this[int row, int column]
            {
                get
                {
                    if (_matrix == null)
                        throw new InvalidOperationException();
                    return _matrix[row, column];
                }

                set
                {
                    if (_matrix == null)
                        throw new InvalidOperationException();
                    _matrix[row, column] = value;
                    _matrix[column, row] = value;
                }
            }

            // Creates an ImmutableAdjacencyMatrix by copying the internals from this builder. The builder is safe to
            // use to create anoher adjacency matrix.
            public ImmutableAdjacencyMatrix ToImmutable()
            {
                bool[,] copy = (bool[,])_matrix.Clone();
                var mat = new ImmutableAdjacencyMatrix(copy);
                return mat;
            }

            // Creates an ImmutableAdjacencyMatrix by transferring the internals from this builder. The builder must
            // not be used after this function is called.
            public ImmutableAdjacencyMatrix MoveToImmutable()
            {
                var mat = new ImmutableAdjacencyMatrix(_matrix);
                _matrix = null;
                return mat;
            }
        }
    }
}
