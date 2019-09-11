using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pages : MonoBehaviour, GameButtonDelegate {
    public Transform detailPanel;

    public GameButton backButton;
    public GameButton nextButton;

    public Text pagesCountText;
    public List<GameObject> pages;

    private int index = 0;

    private void Start() {
        backButton.buttonDelegate = this;
        nextButton.buttonDelegate = this;

        foreach(GameObject page in pages) {
            page.transform.SetParent(detailPanel, false);
            page.SetActive(false);
        }

        if (pages.Count > 0) {
            SetPage(0);
        }        
    }

    private void SetPage(int page) {
        pages[index].SetActive(false);

        index = page;

        bool backEndabled = false;
        if (page > 0) {
            backEndabled = true;
        }

        backButton.SetEnabled(backEndabled);

        bool nextEnabled = false;
        if (index + 1 < pages.Count) {
            nextEnabled = true;
        }

        nextButton.SetEnabled(nextEnabled);

        pages[index].SetActive(true);

        updatePageText();
    }

    private void updatePageText() {
        pagesCountText.text = (index + 1).ToString() + " / " + pages.Count.ToString();
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton button) {
        if (button == backButton) {
            SetPage(index - 1);
        } else if (button == nextButton) {
            SetPage(index + 1);
        }
    }
}
