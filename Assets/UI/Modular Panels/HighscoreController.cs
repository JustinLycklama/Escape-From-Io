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

    public string deviceId;

    public PubNubScoreObject(string score, string firstName, string lastName, string deviceId) {
        this.score = score;
        this.firstName = firstName;
        this.lastName = lastName;
        this.deviceId = deviceId;
    }
}

public struct LeaderboardRow {
    
}

public class HighscoreController : MonoBehaviour {

    private PubNub pubnub;
    private const string channel = "leaderboard";

    List<LeaderboardRow> leaderboardItems;
    public event EventHandler<List<LeaderboardRow>> leaderboardCollectionUpdate;

    void Start() {
        PNConfiguration pnConfiguration = new PNConfiguration();
        pnConfiguration.SubscribeKey = "sub-c-705a78cc-ce95-11e9-8b24-569e8a5c3af3";
        pnConfiguration.PublishKey = "pub-c-c6e1fb2f-39fb-4003-8526-29e45b48e1e2";
        pnConfiguration.SecretKey = "sec-c-MDA3ZDE5YmItYmQ5Mi00MjFjLWIwNmEtYzY2ZTQ5NzdjNTEz";
        pnConfiguration.LogVerbosity = PNLogVerbosity.BODY;
        pnConfiguration.UUID = "PubNubUnityExample";

        pubnub = new PubNub(pnConfiguration);


        /*pubnub.SubscribeCallback += (sender, e) => {
            SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;

            if(mea.Status != null) {
                if(mea.Status.Category.Equals(PNStatusCategory.PNConnectedCategory)) {
                    pubnub.Publish()
                        .Channel(channel)
                        .Message(message)
                        .Async((result, status) => {
                            if(!status.Error) {
                                Debug.Log(string.Format("DateTime {0}, In Publish Example, Timetoken: {1}", System.DateTime.UtcNow, result.Timetoken));
                            } else {
                                Debug.Log(status.Error);
                                Debug.Log(status.ErrorData.Info);
                            }
                        });
                }
            }
            if(mea.MessageResult != null) {
                //Debug.Log("In Example, SubscribeCallback in message" + mea.MessageResult.Channel);
                //Dictionary<string, string> msg = mea.MessageResult.Payload as Dictionary<string, string>;
                //Debug.Log("msg: " + msg["msg"]);
            }
            if(mea.PresenceEventResult != null) {
                //Debug.Log("In Example, SubscribeCallback in presence" + mea.PresenceEventResult.Channel + mea.PresenceEventResult.Occupancy + mea.PresenceEventResult.Event);
            }
        };*/

        //pubnub.Subscribe()
        //    .Channels(new List<string>(){
        //"my_channel"
        //    })
        //    .Execute();


        PubNubScoreObject myFireObject = new PubNubScoreObject();
        //myFireObject.test = "new user";
        string fireobject = JsonUtility.ToJson(myFireObject);

        // Get initial data

        pubnub.Fire()
          .Channel(channel)
          .Message(fireobject)
          .Async((result, status) => {
              if(status.Error) {
                  Debug.Log(status.Error);
                  Debug.Log(status.ErrorData.Info);
              } else {
                  Debug.Log(string.Format("Fire Timetoken: {0}", result.Timetoken));
              }
          });

        // Subscribe to updates

        /*pubnub.SubscribeCallback += (sender, e) => {

            SubscribeEventEventArgs msg = e as SubscribeEventEventArgs;
            if(msg.Status != null) {
            }
            if(msg.MessageResult != null) {

                print(msg);

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
            if(msg.PresenceEventResult != null) {
                Debug.Log("In Example, SusbcribeCallback in presence" + msg.PresenceEventResult.Channel + msg.PresenceEventResult.Occupancy + msg.PresenceEventResult.Event);
            }
        };*/

        // Subscribe to channels

        pubnub.Subscribe()
          .Channels(new List<string>() {
        channel
          })
          .WithPresence()
          .Execute();

        SubmitScore(100, "justin", "l");
    }

    public void SubmitScore(float score, string firstName, string lastName) {        
        PubNubScoreObject newScoreObject = new PubNubScoreObject(score.ToString(), firstName, lastName, SystemInfo.deviceUniqueIdentifier);
        string json = JsonUtility.ToJson(newScoreObject);

        pubnub.Publish()
          .Channel(channel)
          .Message(json)
          .Async((result, status) => {
              if(!status.Error) {
                  Debug.Log(string.Format("Publish Timetoken: {0}", result.Timetoken));
              } else {
                  Debug.Log(status.Error);
                  Debug.Log(status.ErrorData.Info);
              }
          });
    }
}
