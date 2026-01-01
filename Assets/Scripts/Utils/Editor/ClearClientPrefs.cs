namespace Utils.Editor
{
    public class ClearClientPrefs
    {
        [UnityEditor.MenuItem("Tools/Clear Client Prefs")]
        public static void ClearPrefs()
        {
            UnityEngine.PlayerPrefs.DeleteAll();
            UnityEngine.Debug.Log("Client preferences cleared.");
        }
    }
}