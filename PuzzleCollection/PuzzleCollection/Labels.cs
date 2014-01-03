using Windows.ApplicationModel.Resources;

namespace PuzzleCollection
{
    /// <summary>
    /// Helper class to access labels defined in resources files (resw)
    /// </summary>
    internal static class Labels
    {
        private static readonly ResourceLoader resourceLoader = new ResourceLoader();
		
        public static string GetString(string name)
		{
			return resourceLoader.GetString(name);
		}

        // For labels used at more than one place, define a static property to avoid typo in constant.

        public static string Easy { get { return GetString("Easy"); } }
        
        public static string Tricky { get { return GetString("Tricky"); } }
        
        public static string Hard { get { return GetString("Hard"); } }
        
        public static string Unknown { get { return GetString("Unknown"); } }
        
        public static string Normal { get { return GetString("Normal"); } }

        public static string Unreasonable { get { return GetString("Unreasonable"); } }

        public static string Help { get { return GetString("Help"); } }


        public static string NewGame { get { return GetString("NewGame"); } }
        public static string Cancel { get { return GetString("Cancel"); } }
        public static string RestartGame { get { return GetString("RestartGame"); } }
        public static string Close { get { return GetString("Close"); } }

        public static string About { get { return GetString("About"); } }
    }
}
