using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PuzzleCollection.Games.Pattern
{
    [DataContract]
    public sealed class PatternState : StateBase
    {
        [DataMember]
        internal int w;
        [DataMember]
        internal int h;
        [DataMember]
        internal byte[] grid;
        [DataMember]
        internal int rowsize;
        [DataMember]
        internal int[] rowdata;
        [DataMember]
        internal int[] rowlen;
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
