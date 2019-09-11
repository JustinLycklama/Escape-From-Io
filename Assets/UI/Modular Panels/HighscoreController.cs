using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Linq;

using PubNubAPI;
using System;

public struct PubNubScoreObject {
    public string score;

    public string firstName;
    public string lastName;

    //public string deviceId;

    public PubNubScoreObject(string score, string firstName, string lastName) {
        this.score = score;
        this.firstName = firstName;
        this.lastName = lastName;
        //this.deviceId = SystemInfo.deviceUniqueIdentifier;
    }
}

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

    public LeaderboardItem(PubNubScoreObject pubNubObject) {
        score = float.Parse(pubNubObject.score);
        firstName = pubNubObject.firstName;
        lastName = pubNubObject.lastName;

        submitted = true;
    }

    public LeaderboardItem(float score) {
        this.score = score;

        submitted = false;
    }
}


public class HighscoreController : MonoBehaviour {

    private PubNub pubnub;
    private const string channel = "leaderboard";

    public List<LeaderboardItem> leaderboardItems { get; private set; }
    public event EventHandler<List<LeaderboardItem>> leaderboardCollectionUpdate;

    // Have we submitted our new value? 
    public static bool preSubmit = false;
    public static int preSubmitValue = int.MaxValue;

    private void Awake() {
        leaderboardItems = new List<LeaderboardItem>();
    }

    void Start() {

        SceneManagement.sharedInstance.sceneLoadEvent += () => {
            if (SceneManagement.sharedInstance.state == SceneManagement.State.GameFinish && SceneManagement.sharedInstance.score != null) {
                preSubmit = true;
                preSubmitValue = Mathf.FloorToInt(SceneManagement.sharedInstance.score.Value);
                SceneManagement.sharedInstance.score = null;

                AddPlaceholderScoreCell();
            }
        };

        PNConfiguration pnConfiguration = new PNConfiguration();
        pnConfiguration.SubscribeKey = "sub-c-705a78cc-ce95-11e9-8b24-569e8a5c3af3";
        pnConfiguration.PublishKey = "pub-c-c6e1fb2f-39fb-4003-8526-29e45b48e1e2";
        pnConfiguration.SecretKey = "sec-c-MDA3ZDE5YmItYmQ5Mi00MjFjLWIwNmEtYzY2ZTQ5NzdjNTEz";
        pnConfiguration.LogVerbosity = PNLogVerbosity.BODY;
        pnConfiguration.UUID = "PubNubUnityExample";

        pubnub = new PubNub(pnConfiguration);

        // Get initial Data
        pubnub.Fire()
          .Channel(channel)
          .Message("{}")
          .Async((result, status) => {
              if(status.Error) {
                  Debug.Log(status.Error);
                  Debug.Log(status.ErrorData.Info);
              } else {

                  Debug.Log(string.Format("Fire Timetoken: {0}", result.Timetoken));
              }
          });
        
          // Subscribe to updates
          pubnub.SubscribeCallback += (sender, e) => {

            SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;
            if(mea.Status != null) {
            }
            if(mea.MessageResult != null) {
                  Dictionary<string, object> fullMsg = mea.MessageResult.Payload as Dictionary<string, object>;

                  if (fullMsg != null && fullMsg.ContainsKey("scoreList")) {
                      Dictionary<string, object>[] scores = fullMsg["scoreList"] as Dictionary<string, object>[];

                      print("Clear Data");
                      leaderboardItems.Clear();

                      print("Populate");
                      print(scores);


                      foreach(Dictionary <string, object> scoreJson in scores ?? new Dictionary<string, object>[0]) {
                          LeaderboardItem item = new LeaderboardItem(scoreJson["score"] as string, scoreJson["firstName"] as string, scoreJson["lastName"] as string);

                          leaderboardItems.Add(item);
                      }

                      if (preSubmit) {
                          AddPlaceholderScoreCell();
                      } else {
                          UpdateLeaderItems();
                      }
                  }

                  // Additional fires for new messages

                  //string msg = mea.MessageResult.Payload as string;
                  //if(msg != null) {
                  //    PubNubScoreObject scoreObject = JsonUtility.FromJson<PubNubScoreObject>(msg);

                  //    leaderboardItems.Add(new LeaderboardItem(scoreObject));
                  //    UpdateLeaderItems();
                  //}

                  //Dictionary<string, object> msg = mea.MessageResult.Payload as Dictionary<string, object>;
                  //string[] strArr = msg["username"] as string[];
                  //string[] strScores = msg["score"] as string[];
                  //int usernamevar = 1;
                  //foreach(string username in strArr) {
                  //    string usernameobject = "Line" + usernamevar;
                  //    GameObject.Find(usernameobject).GetComponent<Text>().text = usernamevar.ToString() + ". " + username.ToString();
                  //    usernamevar++;
                  //    Debug.Log(username);
                  //}
                  //int scorevar = 1;
                  //foreach(string score in strScores) {
                  //    string scoreobject = "Score" + scorevar;
                  //    GameObject.Find(scoreobject).GetComponent<Text>().text = "Score: " + score.ToString();
                  //    scorevar++;
                  //    Debug.Log(score);
                  //}
              }
            if(mea.PresenceEventResult != null) {
                Debug.Log("In Example, SusbcribeCallback in presence" + mea.PresenceEventResult.Channel + mea.PresenceEventResult.Occupancy + mea.PresenceEventResult.Event);
            }
        };

        // Subscribe to channels

        pubnub.Subscribe()
          .Channels(new List<string>() {
        channel
          })
          .WithPresence()
          .Execute();

        //SubmitScore(100, "justin", "l");
        //preSubmitValue = 25;
        //AddPlaceholderScoreCell();
    }    

    public void UpdateLeaderItems() {
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

        string formattedFirst = firstName.First().ToString().ToUpper() + firstName.Substring(1).ToLower();
        string formattedLast = lastName.First().ToString().ToUpper() + lastName.Substring(1).ToLower();

        PubNubScoreObject newScoreObject = new PubNubScoreObject(score.ToString(), formattedFirst, formattedLast);
        string json = JsonUtility.ToJson(newScoreObject);

        json = json.Replace(@"\", string.Empty);

        preSubmit = false;

        pubnub.Publish()
          .Channel(channel)
          .Message(newScoreObject)
          .Async((result, status) => {
              if(!status.Error) {
                  // No error, remove the placeholder cell from our list
                  RemovePlaceholderScoreCell();

                  Debug.Log(string.Format("Publish Timetoken: {0}", result.Timetoken));
              } else {
                  // There was some error, present the error and allow the user to submit again

                  preSubmit = true;
                  UpdateLeaderItems();
              }
          });
    }
}
