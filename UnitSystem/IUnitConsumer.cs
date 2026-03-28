using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitSystem
{
    public interface IUnitConsumer
    {
        /// <summary>
        /// The unit of the consumer.
        /// </summary>
        UnitMeasure Unit { get; }
    }
}
