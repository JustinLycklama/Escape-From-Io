using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;
using System;
using System.Linq;


public interface FirebaseDelegate {
    void ScoredUpdated(List<HighScoreEntry> entries);
}

public class FirebaseManager
{
    public static FirebaseManager sharedInstance = new FirebaseManager();

    private const string HIGHSCORE_NODE_NAME = "highscore";

    private const string HIGHSCORE_NAME_FIELD = "name";
    private const string HIGHSCORE_SCORE_FIELD = "score";

    Firebase.Auth.FirebaseAuth auth;
    Firebase.Auth.FirebaseUser user;    

    DatabaseReference dbReference;
    DatabaseReference highscoreRef;

    public FirebaseDelegate firebaseDelegate;

    List<HighScoreEntry> newEntries = null;

    private bool isInit = false;

    ~FirebaseManager() {
        auth.StateChanged -= AuthStateChanged;
        auth = null;
    }

    public void Update() {
        if (newEntries != null && firebaseDelegate != null) {
            firebaseDelegate.ScoredUpdated(newEntries);
            newEntries = null;
        }
    }

    void CheckFirebase()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                //   app = Firebase.FirebaseApp.DefaultInstance;

                // Set a flag here to indicate whether Firebase is ready to use by your app.
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }

    void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase Auth");
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    // Track state changes of the auth object.
    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }

    public void TestSignIn(Action onComplete)
    {       
        auth.SignInAnonymouslyAsync().ContinueWith(task => {
            onComplete?.Invoke();

            if (task.IsCanceled)
            {
                Debug.LogError("SignInAnonymouslyAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInAnonymouslyAsync encountered an error: " + task.Exception);
                return;
            }

            Firebase.Auth.FirebaseUser newUser = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                newUser.DisplayName, newUser.UserId);
        });
    }

    private void InitDatabase()
    {
        // Set up the Editor before calling into the realtime database.
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://test1-e5512.firebaseio.com/");

        // Get the root reference location of the database.
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        highscoreRef = dbReference.Child(HIGHSCORE_NODE_NAME);
    }

    private void InitFlow(Action onComplete) {
        CheckFirebase();
        InitializeFirebase();

        InitDatabase();

        TestSignIn(() => {
            isInit = true;
            onComplete?.Invoke();
        });
    }

    /*
     * Public Interface
     * */    

    public void WriteScore(string name, int value, Action onComplete)
    {
        Action action = () => {
            HighScoreEntry entry = new HighScoreEntry(name, value);
            string json = JsonUtility.ToJson(entry);

            string insertKey = highscoreRef.Push().Key;

            highscoreRef.Child(insertKey).SetRawJsonValueAsync(json).ContinueWith(t => {
                if(t.IsFaulted) {
                    Debug.Log("Faulted..");
                }

                if(t.IsCanceled) {
                    Debug.Log("Cancelled..");
                }

                if(t.IsCompleted) {
                    Debug.Log("Complete");
                }

                onComplete?.Invoke();
            });
        };

        if(!isInit) {
            InitFlow(action);
        } else {
            action.Invoke();
        }
    }

    public void ReadScore()
    {
        Action action = () => {
            highscoreRef.GetValueAsync().ContinueWith(task => {
                if(task.IsFaulted) {
                    // Handle the error...
                } else if(task.IsCompleted) {
                    DataSnapshot snapshot = task.Result;
                    List<HighScoreEntry> highscores = new List<HighScoreEntry>();

                    foreach(DataSnapshot child in snapshot.Children) {
                        string name = "";
                        long value = 0;

                        try {
                            name = child.Child(HIGHSCORE_NAME_FIELD).Value as string;
                            value = (long)child.Child(HIGHSCORE_SCORE_FIELD).Value; // Firebase nonsense, values returned as long
                        } catch(Exception ex) {
                            Debug.Log(ex);
                            continue;
                        }

                        highscores.Add(new HighScoreEntry(name, (int)value));
                    }

                    newEntries = highscores;
                }
            });
        };

        if(!isInit) {
            InitFlow(action);
        } else {
            action.Invoke();
        }
    }
}
