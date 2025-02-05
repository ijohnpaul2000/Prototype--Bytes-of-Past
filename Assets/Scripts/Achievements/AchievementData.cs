﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AchievementData", menuName = "ScriptableObjects/AchievementData")]
public class AchievementData : ScriptableObject
{
    public int id;
    public string title;
    public string description;
    public bool isDone;
}

public class GameData
{
    public GameMode gameMode;
    public GameDifficulty difficulty;
    public bool gameWon;
    public float accuracy;
    public int remainingLife;
    public int initialLife;

    public GameData(GameMode gm, GameDifficulty diff, bool gameWon, float accuracy, int remainingLife, int initialLife)
    {
        this.gameMode = gm;
        this.difficulty = diff;
        this.gameWon = gameWon;
        this.accuracy = accuracy;
        this.remainingLife = remainingLife;
        this.initialLife = initialLife;
    }

}
