using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PubNubAPI;
using UnityEngine.UI;

public class ScoreObject {
    public string username;
    public string score;
    public string test;
}

public class HighscoreController : MonoBehaviour {     
    void Start() {
        PNConfiguration pnConfiguration = new PNConfiguration();
        pnConfiguration.SubscribeKey = "sub-c-705a78cc-ce95-11e9-8b24-569e8a5c3af3";
        pnConfiguration.PublishKey = "pub-c-c6e1fb2f-39fb-4003-8526-29e45b48e1e2";
        pnConfiguration.SecretKey = "sec-c-MDA3ZDE5YmItYmQ5Mi00MjFjLWIwNmEtYzY2ZTQ5NzdjNTEz";
        pnConfiguration.LogVerbosity = PNLogVerbosity.BODY;
        pnConfiguration.UUID = "PubNubUnityExample";

        Dictionary<string, string> message = new Dictionary<string, string>();
        message.Add("msg", "hello");

        PubNub pubnub = new PubNub(pnConfiguration);

        //pubnub.SubscribeCallback += (sender, e) => {
        //    SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;

        //    if(mea.Status != null) {
        //        if(mea.Status.Category.Equals(PNStatusCategory.PNConnectedCategory)) {
        //            pubnub.Publish()
        //                .Channel("my_channel")
        //                .Message(message)
        //                .Async((result, status) => {
        //                    if(!status.Error) {
        //                        Debug.Log(string.Format("DateTime {0}, In Publish Example, Timetoken: {1}", System.DateTime.UtcNow, result.Timetoken));
        //                    } else {
        //                        Debug.Log(status.Error);
        //                        Debug.Log(status.ErrorData.Info);
        //                    }
        //                });
        //        }
        //    }
        //    if(mea.MessageResult != null) {
        //        Debug.Log("In Example, SubscribeCallback in message" + mea.MessageResult.Channel);
        //        Dictionary<string, string> msg = mea.MessageResult.Payload as Dictionary<string, string>;
        //        Debug.Log("msg: " + msg["msg"]);
        //    }
        //    if(mea.PresenceEventResult != null) {
        //        Debug.Log("In Example, SubscribeCallback in presence" + mea.PresenceEventResult.Channel + mea.PresenceEventResult.Occupancy + mea.PresenceEventResult.Event);
        //    }
        //};

        //pubnub.Subscribe()
        //    .Channels(new List<string>(){
        //"my_channel"
        //    })
        //    .Execute();


        const string channel = "leaderboard";

        ScoreObject myFireObject = new ScoreObject();
        myFireObject.test = "new user";
        string fireobject = JsonUtility.ToJson(myFireObject);


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

        pubnub.SubscribeCallback += (sender, e) => {

            //print("callback");

            SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;
            if(mea.Status != null) {
            }
            if(mea.MessageResult != null) {
                Dictionary<string, object> msg = mea.MessageResult.Payload as Dictionary<string, object>;
                string[] strArr = msg["username"] as string[];
                string[] strScores = msg["score"] as string[];
                int usernamevar = 1;
                foreach(string username in strArr) {
                    string usernameobject = "Line" + usernamevar;
                    GameObject.Find(usernameobject).GetComponent<Text>().text = usernamevar.ToString() + ". " + username.ToString();
                    usernamevar++;
                    Debug.Log(username);
                }
                int scorevar = 1;
                foreach(string score in strScores) {
                    string scoreobject = "Score" + scorevar;
                    GameObject.Find(scoreobject).GetComponent<Text>().text = "Score: " + score.ToString();
                    scorevar++;
                    Debug.Log(score);
                }
            }
            if(mea.PresenceEventResult != null) {
                Debug.Log("In Example, SusbcribeCallback in presence" + mea.PresenceEventResult.Channel + mea.PresenceEventResult.Occupancy + mea.PresenceEventResult.Event);
            }
        };


        pubnub.Subscribe()
          .Channels(new List<string>() {
        channel
          })
          .WithPresence()
          .Execute();





        var usernametext = "Test";
        var scoretext = "0";
        ScoreObject newScoreObject = new ScoreObject();
        newScoreObject.username = usernametext;
        newScoreObject.score = scoretext;
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
