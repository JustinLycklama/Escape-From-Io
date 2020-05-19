using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface HighscoreCellDelegate {
    void DidSubmit(HighscoreCell cell);
}

public class HighscoreCell : MonoBehaviour, GameButtonDelegate {

    public Text rank;
    public Text time;
    public Text fullName;

    public InputField firstName;
    public InputField lastName;
    public GameButton submitButton;

    public GameObject fullNamePanel;
    public GameObject InputFieldLayout;

    public Image backgroundImage;

    [HideInInspector]
    public HighscoreCellDelegate submitDelegate;

    [HideInInspector]
    public float score;

    private bool canSubmit = false;
    private bool subCell = false;

    private void Awake() {
        submitButton.buttonDelegate = this;

        firstName.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
        lastName.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
    }

    void Update() {
        if(!subCell) { return; }

        if(firstName.isFocused && Input.GetKeyDown(KeyCode.Tab)) {
            lastName.Select();
            lastName.ActivateInputField();
        }
    }

    private void ValueChangeCheck() {
        submitButton.SetEnabled(canSubmit && firstName.text.Length > 0/* && lastName.text.Length > 0*/);
    }

    public void SetRank(int rank) {
        this.rank.text = rank.ToString() + ".";
    }

    public void SetScore(float score) {
        this.score = score;

        System.TimeSpan timeSpan = new System.TimeSpan(0, 0, Mathf.FloorToInt(score)); 
        time.text = timeSpan.ToString();
    }

    public void SetIsSubmissionCell(bool subCell, bool canSubmit = false) {
        Color baseColor = backgroundImage.color;

        if (subCell) {
            SetObjectActive(fullNamePanel, false);
            SetObjectActive(InputFieldLayout, true);
            SetObjectActive(submitButton.gameObject, true);

            this.subCell = subCell;
            this.canSubmit = canSubmit;
            ValueChangeCheck();

            firstName.Select();
            firstName.ActivateInputField();

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
        submitDelegate.DidSubmit(this);
    }
}
