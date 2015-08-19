using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WB.Core.SharedKernels.DataCollection
{
    public class RosterVector : IEnumerable<decimal>
    {
        private readonly ReadOnlyCollection<decimal> coordinates;

        public RosterVector(IEnumerable<decimal> coordinates)
        {
            if (coordinates == null) throw new ArgumentNullException("coordinates");

            this.coordinates = new ReadOnlyCollection<decimal>(new List<decimal>(coordinates));
        }

        public IReadOnlyCollection<decimal> Coordinates
        {
            get { return this.coordinates; }
        }

        public override string ToString()
        {
            return string.Format("<{0}>", string.Join("-", this.Coordinates));
        }

        #region Backward compatibility with decimal[]

        private decimal[] array;

        private decimal[] Array
        {
            get { return this.array ?? (this.array = this.Coordinates.ToArray()); }
        }

        IEnumerator<decimal> IEnumerable<decimal>.GetEnumerator()
        {
            return ((IEnumerable<decimal>)this.Array).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Array.GetEnumerator();
        }

        public int Length
        {
            get { return this.Array.Length; }
        }

        public decimal this[int index]
        {
            get { return this.Array[index]; }
        }

        public static implicit operator decimal[](RosterVector rosterVector)
        {
            return rosterVector.Array;
        }

        public static implicit operator RosterVector(decimal[] array)
        {
            return new RosterVector(array);
        }

        public bool Identical(RosterVector other)
        {
            if (other == null) return false;

            if (this.Length == 0 && other.Length == 0 || ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.Length != other.Length)
            {
                return false;
            }

            return this.Array.SequenceEqual(other.Array);
        }

        #endregion
    }
}