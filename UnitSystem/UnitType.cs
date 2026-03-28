using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
#nullable disable
namespace UnitSystem
{
    [Serializable]
    public sealed class UnitType : ISerializable
    {
        #region BaseUnitType support

        private static ReaderWriterLock baseUnitTypeLock = new ReaderWriterLock();
        private static IList<string> baseUnitNames = new List<string>();

        private static string GetBaseUnitName(int index)
        {
            // Lock baseUnitNames:
            baseUnitTypeLock.AcquireReaderLock(2000);

            try
            {
                return baseUnitNames[index];
            }
            finally
            {
                // Release lock:
                baseUnitTypeLock.ReleaseReaderLock();
            }
        }

        private static int GetBaseUnitIndex(string unitName)
        {
            // Verify unitName does not contain pipe char (which is used in serializations):
            if (unitName.Contains('|'))
                throw new ArgumentException("The name of a UnitType must not contain the '|' (pipe) character.", "unitName");

            // Lock baseUnitNames:
            baseUnitTypeLock.AcquireReaderLock(2000);

            try
            {
                // Retrieve index of unitName:
                int index = baseUnitNames.IndexOf(unitName);

                // If not found, register unitName:
                if (index == -1)
                {
                    baseUnitTypeLock.UpgradeToWriterLock(2000);
                    index = baseUnitNames.Count;
                    baseUnitNames.Add(unitName);
                }

                // Return index:
                return index;
            }
            finally
            {
                // Release lock:
                baseUnitTypeLock.ReleaseLock();
            }
        }

        #endregion BaseUnitType support

        private sbyte[] baseUnitIndices;

        [NonSerialized]
        private int cachedHashCode;

        #region Constructor methods

        private static UnitType none = new UnitType(0);

        public UnitType(string unitName)
        {

            int unitIndex = GetBaseUnitIndex(unitName);
            baseUnitIndices = new sbyte[unitIndex + 1];
            baseUnitIndices[unitIndex] = 1;
        }

        private UnitType(int indicesLength)
        {
            baseUnitIndices = new sbyte[indicesLength];
        }

        private UnitType(sbyte[] baseUnitIndices)
        {
            this.baseUnitIndices = (sbyte[])baseUnitIndices.Clone();
        }

        private UnitType(SerializationInfo info, StreamingContext c)
        {
            // Retrieve data from serialization:
            sbyte[] tstoreexp = info.GetString("exps").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToSByte(x))
                .ToArray();
            int[] tstoreind = info.GetString("names").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => GetBaseUnitIndex(x))
                .ToArray();

            // Construct instance:
            if (tstoreexp.Length > 0)
            {
                baseUnitIndices = new sbyte[tstoreind.Max() + 1];
                for (int i = 0; i < tstoreexp.Length; i++)
                {
                    baseUnitIndices[tstoreind[i]] = tstoreexp[i];
                }
            }
            else
            {
                baseUnitIndices = new sbyte[0];
            }
        }

        public static UnitType None
        {
            get { return none; }
        }

        #endregion Constructor methods

        #region Public implementation

        /// <summary>
        /// Returns the unit type raised to the specified power.
        /// </summary>
        public UnitType Power(double power)
        {
            UnitType result = new UnitType(baseUnitIndices);
            for (int i = 0; i < result.baseUnitIndices.Length; i++)
                result.baseUnitIndices[i] = (sbyte)(result.baseUnitIndices[i] * power);
            return result;
        }

        public override bool Equals(object obj)
        {
            return this == obj as UnitType;
        }

        public override int GetHashCode()
        {
            if (cachedHashCode == 0)
            {
                int hash = 0;
                for (int i = 0; i < baseUnitIndices.Length; i++)
                {
                    int factor = i + i + 1;
                    hash += factor * factor * baseUnitIndices[i] * baseUnitIndices[i];
                }
                cachedHashCode = hash;
            }
            return cachedHashCode;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string sep = string.Empty;
            for (int i = 0; i < baseUnitIndices.Length; i++)
            {
                if (baseUnitIndices[i] != 0)
                {
                    sb.Append(sep);
                    sb.Append(GetBaseUnitName(i));
                    sb.Append('^');
                    sb.Append(baseUnitIndices[i]);
                    sep = " * ";
                }
            }
            return sb.ToString();
        }

        #endregion Public implementation

        #region Operator overloads

        public static UnitType operator *(UnitType left, UnitType right)
        {
            UnitType result = new UnitType(Math.Max(left.baseUnitIndices.Length, right.baseUnitIndices.Length));
            left.baseUnitIndices.CopyTo(result.baseUnitIndices, 0);
            for (int i = 0; i < right.baseUnitIndices.Length; i++)
                result.baseUnitIndices[i] += right.baseUnitIndices[i];
            return result;
        }

        public static UnitType operator /(UnitType left, UnitType right)
        {
            UnitType result = new UnitType(Math.Max(left.baseUnitIndices.Length, right.baseUnitIndices.Length));
            left.baseUnitIndices.CopyTo(result.baseUnitIndices, 0);
            for (int i = 0; i < right.baseUnitIndices.Length; i++)
                result.baseUnitIndices[i] -= right.baseUnitIndices[i];
            return result;
        }

        public static bool operator ==(UnitType left, UnitType right)
        {
            // Handle special cases:
            if (ReferenceEquals(left, right))
                return true;
            else if (ReferenceEquals(left, null))
                return false;
            else if (ReferenceEquals(right, null))
                return false;

            // Determine longest and shortest baseUnitUndice arrays:
            sbyte[] longest, shortest;
            int leftlen = left.baseUnitIndices.Length;
            int rightlen = right.baseUnitIndices.Length;
            if (leftlen > rightlen)
            {
                longest = left.baseUnitIndices;
                shortest = right.baseUnitIndices;
            }
            else
            {
                longest = right.baseUnitIndices;
                shortest = left.baseUnitIndices;
            }

            // Compare baseUnitIndice array content:
            for (int i = 0; i < shortest.Length; i++)
                if (shortest[i] != longest[i]) return false;
            for (int i = shortest.Length; i < longest.Length; i++)
                if (longest[i] != 0) return false;
            return true;
        }

        public static bool operator !=(UnitType left, UnitType right)
        {
            return !(left == right);
        }

        #endregion Operator overloads

        #region ISerializable Members


        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            bool first = true;
            StringBuilder sbn = new StringBuilder(baseUnitIndices.Length * 8);
            StringBuilder sbx = new StringBuilder(baseUnitIndices.Length * 4);
            for (int i = 0; i < baseUnitIndices.Length; i++)
            {
                if (baseUnitIndices[i] != 0)
                {
                    if (!first) sbn.Append('|');
                    sbn.Append(GetBaseUnitName(i));
                    if (!first) sbx.Append('|');
                    sbx.Append(baseUnitIndices[i]);
                    first = false;
                }
            }
            info.AddValue("names", sbn.ToString());
            info.AddValue("exps", sbx.ToString());
        }

        #endregion
    }
}
