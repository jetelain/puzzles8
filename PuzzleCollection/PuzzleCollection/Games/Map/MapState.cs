using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Map
{
    [DataContract]
    public sealed class MapState : StateBase
    {
        [DataMember]
        internal MapSettings p;
        [DataMember]
        internal MapData map;
        [DataMember]
        internal int[] colouring;
        [DataMember]
        internal int[] pencil;
        [DataMember]
        internal bool completed;
        [DataMember]
        internal bool cheated;

        internal override bool IsCompleted
        {
            get { return completed; }
        }

        internal override bool HasCheated
        {
            get { return cheated; }
        }
    }
}
