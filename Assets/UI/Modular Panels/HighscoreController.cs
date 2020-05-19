using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class LeaderboardItem {
    public float score;
    public string firstName;
    public string lastName;

    public bool submitted;

    public LeaderboardItem(string score, string firstName, string lastName) {
        if(score.Length > 0) {
            this.score = float.Parse(score);
        } else {
            this.score = 0f;
        }
        
        this.firstName = firstName;
        this.lastName = lastName;

        submitted = true;
    }

    public LeaderboardItem(float score) {
        this.score = score;

        submitted = false;
    }
}


public class HighscoreController : MonoBehaviour, FirebaseDelegate {

    //private PubNub pubnub;
    private const string channel = "leaderboard";

    public List<LeaderboardItem> leaderboardItems = new List<LeaderboardItem>();
    public event EventHandler<List<LeaderboardItem>> leaderboardCollectionUpdate;

    // Have we submitted our new value? 
    public static bool preSubmit = false;
    public static int preSubmitValue = int.MaxValue;

    [SerializeField]
    FirebaseManager firebaseManager;

    void Start() {
        //if(SceneManagement.sharedInstance.state == SceneManagement.State.GameFinish && SceneManagement.sharedInstance.score != null) {
        //    preSubmit = true;
        //    preSubmitValue = Mathf.FloorToInt(SceneManagement.sharedInstance.score.Value);
        //    SceneManagement.sharedInstance.score = null;

        //    AddPlaceholderScoreCell();
        //}

        
        preSubmit = true;
        preSubmitValue = Mathf.FloorToInt(11);
        //SceneManagement.sharedInstance.score = null;

        AddPlaceholderScoreCell();       
    }    

    private void UpdateLeaderItems() {
        leaderboardItems = leaderboardItems.OrderBy(scoreObj => scoreObj.score).ToList();

        if(leaderboardCollectionUpdate != null) {
            leaderboardCollectionUpdate.Invoke(this, leaderboardItems);
        }
    }

    private void AddPlaceholderScoreCell() {
        preSubmit = true;
        leaderboardItems.Add(new LeaderboardItem(preSubmitValue));
        UpdateLeaderItems();
    }

    private void RemovePlaceholderScoreCell() {
        foreach(LeaderboardItem item in leaderboardItems.ToArray()) {
            if (item.submitted == false) {
                leaderboardItems.Remove(item);
                break;
            }
        }

        UpdateLeaderItems();
    }

    public void SubmitScore(float score, string firstName, string lastName) {

        firebaseManager.WriteScore(firstName, Mathf.FloorToInt(score), () => {
            firebaseManager.ReadScore();
        });

        preSubmit = false;
    }

    /*
     * FirebaseDelegate Interface
     * */

    public void ScoredUpdated(List<HighScoreEntry> entries) {        
        leaderboardItems = entries.Select(hs => { return new LeaderboardItem(hs.score.ToString(), hs.name, ""); }).ToList();

        if(preSubmit) {
            AddPlaceholderScoreCell();
        } else {
            UpdateLeaderItems();
        }        
    }
}
