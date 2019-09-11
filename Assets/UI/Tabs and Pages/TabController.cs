using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TabController : MonoBehaviour, GameButtonDelegate {

    public GameButton prototypeButton;
    //public Text prototypeText;

    public GameObject detailPane;
    public HorizontalLayoutGroup tabButtonLayout;

    public List<TabElement> tabs;    

    private Dictionary<GameButton, TabElement> tabButtonsMap;

    private void Start() {
        tabButtonsMap = new Dictionary<GameButton, TabElement>();

        GameButton first = null;
        foreach(TabElement tab in tabs) {
            //obj.transform.SetParent(null);
            tab.transform.SetParent(detailPane.transform, false);
            tab.gameObject.SetActive(false);

            GameButton newButton = Instantiate(prototypeButton);
            Text newText = newButton.GetComponentInChildren<Text>();

            newText.transform.SetParent(newButton.transform);
            newButton.transform.SetParent(tabButtonLayout.transform);

            newText.text = tab.tabTitle;
            newButton.buttonDelegate = this;

            tabButtonsMap[newButton] = tab;

            if(first == null) {
                first = newButton;
            }
        }

        prototypeButton.gameObject.SetActive(false);
        ButtonDidClick(first);
    }

    /*
     * ButtonDelegate Interface
     * */

    public void ButtonDidClick(GameButton selectedButton) {
        foreach(GameButton button in tabButtonsMap.Keys) {
            button.SetHoverLock(false);

            if (tabButtonsMap[button].gameObject.activeSelf != false) {
                tabButtonsMap[button].gameObject.SetActive(false);
            }
        }

        selectedButton.SetHoverLock(true);
        tabButtonsMap[selectedButton].gameObject.SetActive(true);
    }
}
