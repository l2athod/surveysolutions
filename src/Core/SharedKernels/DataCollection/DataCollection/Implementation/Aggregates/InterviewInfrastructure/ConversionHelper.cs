﻿using System;
using System.Globalization;
using System.Linq;
using WB.Core.Infrastructure.BaseStructures;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates
{
    public static class ConversionHelper
    {
        /// <remarks>
        /// The opposite operation (get id or vector from string) should never be performed!
        /// This is one-way transformation. Opposite operation is too slow.
        /// If you need to compactify data and get it back, you should use another datatype, not a string.
        /// </remarks>
        internal static string ConvertIdAndRosterVectorToString(Guid id, decimal[] rosterVector)
        {
            return string.Format("{0:N}[{1}]", id, string.Join("-", rosterVector.Select(v => v.ToString("0.############################", CultureInfo.InvariantCulture))));
        }

        public static string ConvertIdentityToString(Identity identity)
        {
            return ConvertIdAndRosterVectorToString(identity.Id, identity.RosterVector);
        }
    }
}
