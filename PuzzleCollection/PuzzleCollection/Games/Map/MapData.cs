using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Map
{
    [DataContract(IsReference=true)]
    public sealed class MapData
    {
        [DataMember]
        internal int refcount;
        [DataMember]
        internal int[] map;
        [DataMember]
        internal int[] graph;
        [DataMember]
        internal int n;
        [DataMember]
        internal int ngraph;
        [DataMember]
        internal bool[] immutable;
        [DataMember]
        internal int[] edgex;
        [DataMember]
        internal int[] edgey;		       /* position of a point on each edge */
        [DataMember]
        internal int[] regionx;
        [DataMember]
        internal int[] regiony;            /* position of a point in each region */
    }
}
