using System;
using System.Collections.Generic;
using UnityEngine;

public interface FirebaseDelegate {
    void ScoredUpdated(List<HighScoreEntry> entries);
}

// Local high-score store. Originally backed by Firebase Realtime Database (removed for the PC-only Unity 6 upgrade).
// Name and public API are kept so callers (HighscoreController) are unchanged. Scores persist in PlayerPrefs on this machine only.
public class FirebaseManager
{
    public static FirebaseManager sharedInstance = new FirebaseManager();

    private const string PREFS_KEY = "LocalHighscores";

    [Serializable]
    private class HighScoreList {
        public List<HighScoreEntry> entries = new List<HighScoreEntry>();
    }

    public FirebaseDelegate firebaseDelegate;

    List<HighScoreEntry> newEntries = null;

    // Delivers pending results to the delegate on the main thread; called from HighscoreController.Update
    public void Update() {
        if (newEntries != null && firebaseDelegate != null) {
            firebaseDelegate.ScoredUpdated(newEntries);
            newEntries = null;
        }
    }

    private HighScoreList Load() {
        string json = PlayerPrefs.GetString(PREFS_KEY, null);
        if (string.IsNullOrEmpty(json)) {
            return new HighScoreList();
        }

        try {
            return JsonUtility.FromJson<HighScoreList>(json) ?? new HighScoreList();
        } catch (Exception ex) {
            Debug.LogWarning("Could not read local highscores, resetting: " + ex.Message);
            return new HighScoreList();
        }
    }

    /*
     * Public Interface
     * */

    public void WriteScore(string name, int value, Action onComplete)
    {
        HighScoreList list = Load();
        list.entries.Add(new HighScoreEntry(name, value));

        PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(list));
        PlayerPrefs.Save();

        onComplete?.Invoke();
    }

    public void ReadScore()
    {
        newEntries = Load().entries;
    }
}
