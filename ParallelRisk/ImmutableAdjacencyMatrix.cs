using System;
using System.Collections.Generic;

namespace ParallelRisk
{
    public struct ImmutableAdjacencyMatrix

    {
        private readonly bool[,] _matrix;

        private ImmutableAdjacencyMatrix(bool[,] matrix)
        {
            _matrix = matrix;
        }

        public IEnumerable<int> Adjacent(int id)
        {
            for (int col = 0; col < _matrix.GetLength(1); col++)
            {
                if (_matrix[id, col])
                    yield return col;
            }
        }

        public static Builder CreateBuilder(int capacity)
        {
            return new Builder(capacity);
        }

        public class Builder
        {
            private bool[,] _matrix;

            public Builder(int capacity)
            {
                _matrix = new bool[capacity, capacity];
            }

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

            public ImmutableAdjacencyMatrix ToImmutable()
            {
                bool[,] copy = (bool[,])_matrix.Clone();
                var mat = new ImmutableAdjacencyMatrix(copy);
                return mat;
            }

            public ImmutableAdjacencyMatrix MoveToImmutable()
            {
                var mat = new ImmutableAdjacencyMatrix(_matrix);
                _matrix = null;
                return mat;
            }
        }
    }
}
