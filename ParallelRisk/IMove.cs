﻿using System;
using System.Collections.Generic;

namespace ParallelRisk
{
    public interface IMove<TState>
    {
        IEnumerable<(double Probability, TState Outcome)> Outcomes();
    }
}
