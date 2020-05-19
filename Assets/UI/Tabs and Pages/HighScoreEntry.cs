using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighScoreEntry
{
    public string name;
    public int score;
    public long date;

    public HighScoreEntry(string name, int score)
    {
        this.name = name;
        this.score = score;
        date = DateTime.UtcNow.Ticks;
    }
}
