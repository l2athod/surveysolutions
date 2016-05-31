﻿using System.Collections.Generic;

namespace WB.Core.GenericSubdomains.Portable
{
    public interface ITopologicalSorter<T>
    {
        List<T> Sort(Dictionary<T, T[]> dependencies);

        List<List<T>> DetectCycles(Dictionary<T, T[]> dependencies);
    }
}
