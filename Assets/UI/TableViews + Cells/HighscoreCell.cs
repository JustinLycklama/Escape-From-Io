using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HighscoreCell : MonoBehaviour, ButtonDelegate {

    public Text time;
    public Text fullName;

    public InputField firstName;
    public InputField lastName;
    public GameButton submitButton;

    public GameObject fullNamePanel;
    public GameObject InputFieldLayout;

    public Image backgroundImage;

    private void Awake() {
        submitButton.buttonDelegate = this;
    }

    public void SetIsSubmissionCell(bool subCell) {
        Color baseColor = backgroundImage.color;

        if (subCell) {
            SetObjectActive(fullNamePanel, false);
            SetObjectActive(InputFieldLayout, true);
            SetObjectActive(submitButton.gameObject, true);

            submitButton.SetEnabled(SceneManagement.sharedInstance.score != null);            

            baseColor.a = 1f;
        } else {
            SetObjectActive(fullNamePanel, true);
            SetObjectActive(InputFieldLayout, false);
            SetObjectActive(submitButton.gameObject, false);

            baseColor.a = 0.5f;
        }

        backgroundImage.color = baseColor;
    }

    private void SetObjectActive(GameObject gameObject, bool active) {
        if (gameObject.activeSelf != active) {
            gameObject.SetActive(active);
        }
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        button.SetEnabled(false);


    }
}
