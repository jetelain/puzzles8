using System;
using Windows.Storage;

namespace PuzzleCollection
{
    /// <summary>
    /// Stores games individual settings
    /// </summary>
    internal static class GameSettingsManager
    {
        private static ApplicationDataContainer GetContainer()
        {
            return ApplicationData.Current.LocalSettings.CreateContainer("gameSettings", Windows.Storage.ApplicationDataCreateDisposition.Always);
        }

        public static string GetGameSettings(string gameId)
        {
            var container = GetContainer();
            object settings;
            if (container.Values.TryGetValue(gameId, out settings))
            {
                return settings as String;
            }
            return null;
        }

        public static void SetGameSettings(string gameId, string settings)
        {
            var container = GetContainer();
            container.Values[gameId] = settings;
        }
    }
}
