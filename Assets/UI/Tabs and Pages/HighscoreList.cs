using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighscoreList : TabElement, TableViewDelegate, HighscoreCellDelegate {

    public TableView table;
    public HighscoreController highscoreController;
    
    

    private List<LeaderboardItem> scoreList;

    private void Start() {


        highscoreController.leaderboardCollectionUpdate += (sender, e) => {
            UpdateScore();
        };

        table.dataDelegate = this;
        UpdateScore();
    }

    private void OnDestroy() {
        // TODO: Ubsubscribe
        //highscoreController.leaderboardCollectionUpdate -= this;
    }

    private void UpdateScore() {
        scoreList = highscoreController.leaderboardItems;
        table.ReloadData();
    }

    /*
     * TableViewDelegate Interface
     * */

    public int NumberOfRows(TableView table) {
        return scoreList.Count;
    }

    public void CellForRowAtIndex(TableView table, int row, GameObject cell) {
        HighscoreCell hsCell = cell.GetComponent<HighscoreCell>();
        LeaderboardItem scoreObject = scoreList[row];

        hsCell.fullName.text = scoreObject.firstName + " " + scoreObject.lastName;
        hsCell.SetScore(scoreObject.score);
        hsCell.SetRank(row + 1);

        hsCell.SetIsSubmissionCell(!scoreObject.submitted, HighscoreController.preSubmit);

        hsCell.submitDelegate = this;
    }

    /*
     * HighscoreCellDelegate Interface
     * */

    public void DidSubmit(HighscoreCell cell) {
        highscoreController.SubmitScore(cell.score, cell.firstName.text, cell.lastName.text);
    }
}
