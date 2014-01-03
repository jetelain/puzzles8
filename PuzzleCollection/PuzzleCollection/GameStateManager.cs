using PuzzleCollection.Games;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PuzzleCollection
{
    internal sealed class GameStateManager
    {
        private static Dictionary<string, GameSave> states = new Dictionary<string, GameSave>();
        private static List<Type> _knownTypes = new List<Type>() { typeof(GameSave) };
        private const string statesFilename = "gamesaves.xml";

        public static Dictionary<string, GameSave> States
        {
            get { return states; }
        }

        public static async void SaveAsync()
        {
            try
            {
                await SaveAsyncPrivate();
            }
            catch (Exception e)
            {
                // Unable to write file... We may need to prompt user about this problem
                Debug.WriteLine("ERROR: " + e.ToString());
            }
        }

        private static async Task SaveAsyncPrivate()
        {
            MemoryStream sessionData = new MemoryStream();
            DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, GameSave>), _knownTypes);
            serializer.WriteObject(sessionData, states);

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(statesFilename, CreationCollisionOption.ReplaceExisting);
            using (Stream fileStream = await file.OpenStreamForWriteAsync())
            {
                sessionData.Seek(0, SeekOrigin.Begin);
                await sessionData.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
        }

        public static async Task RestoreAsync()
        {
            try
            {
                await RestoreAsyncPrivate();
            }
            catch (Exception e)
            {
                // Unable to read file : file is probably corrupted
                Debug.WriteLine("ERROR: " + e.ToString());
                states = new Dictionary<String, GameSave>();
            }
        }

        private static async Task RestoreAsyncPrivate()
        {
            states = new Dictionary<String, GameSave>();
            StorageFile file;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync(statesFilename);
            }
            catch (FileNotFoundException)
            {
                return; // ... XXX: There is no FileExists method; Find a more elegant way to achieve that
            }
            using (IInputStream inStream = await file.OpenSequentialReadAsync())
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, GameSave>), _knownTypes);
                states = (Dictionary<string, GameSave>)serializer.ReadObject(inStream.AsStreamForRead());
            }
        }
    }
}
