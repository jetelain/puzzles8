using System.Runtime.Serialization;

namespace PuzzleCollection.Games
{
    [DataContract]
    public class GameSave
    {
        //[DataMember]
        //public string GameId { get; set; }

        [DataMember]
        public string Settings { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string[] HistoryMoves { get; set; }

        [DataMember]
        public string[] RedoListMoves { get; set; }
    }
}
