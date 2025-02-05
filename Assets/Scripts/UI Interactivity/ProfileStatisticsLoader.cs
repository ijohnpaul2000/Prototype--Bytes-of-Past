﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProfileStatisticsLoader : MonoBehaviour
{
    [Header("UI References")]
    public GameObject txtAccuracy;
    public GameObject txtTotalGames;
    public GameObject txtSPWinOrLoss;
    public GameObject txtMPWinOrLoss;

    // ASSESSMENT SCORES
    // Size of [2]
    // 2 for Pre & Post Assessment
    public GameObject[] txtAssessment_Computer_Score;
    public GameObject[] txtAssessment_Networking_Score;
    public GameObject[] txtAssessment_Software_Score;

    public TextMeshProUGUI txtProfile;

    StaticData staticData;

    int preAssessmentTotalQuestions = 15;
    int postAssessmentTotalQuestions = 20;

    void Awake()
    {
        staticData = StaticData.Instance;
        ShowStatisticsData();
    }

    void ShowStatisticsData()
    {
        LoadProfileInfo();
        LoadAssessmentScores();
        LoadGameAccuracy();
        LoadSPGameWinLoss();
    }

    void LoadProfileInfo()
    {
        txtProfile.text = $"{StaticData.Instance.GetPlayerName()} ({StaticData.Instance.GetPlayerSectionString()})";
    }

    void LoadAssessmentScores()
    {
        string notDoneScore = "- / -";

        int[,] assessmentScores =
            staticData.profileStatisticsData.assessmentScores;

        // Assessment Scores
        for (int i = 0; i < 2; i++)
        {
            // [0] for PRE scores, [1] for POST scores
            bool loadingPreAssessment = i == 0;

            // Total Question Count based on gamemode index
            int totalQuestions = loadingPreAssessment ?
                preAssessmentTotalQuestions : postAssessmentTotalQuestions;

            // Assessment Done for each topic?
            bool assessDone1, assessDone2, assessDone3;

            if (loadingPreAssessment)
            {
                assessDone1 = PrefsConverter.IntToBoolean(
                    PlayerPrefs.GetInt(TopicUtils.GetPrefKey_IsPreAssessmentDone(HistoryTopic.COMPUTER), 0)
                );
                assessDone2 = PrefsConverter.IntToBoolean(
                    PlayerPrefs.GetInt(TopicUtils.GetPrefKey_IsPreAssessmentDone(HistoryTopic.NETWORKING), 0)
                );
                assessDone3 = PrefsConverter.IntToBoolean(
                    PlayerPrefs.GetInt(TopicUtils.GetPrefKey_IsPreAssessmentDone(HistoryTopic.SOFTWARE), 0)
                );
            }
            else
            {
                assessDone1 = PrefsConverter.IntToBoolean(
                    PlayerPrefs.GetInt(TopicUtils.GetPrefKey_IsPostAssessmentDone(HistoryTopic.COMPUTER), 0)
                );
                assessDone2 = PrefsConverter.IntToBoolean(
                    PlayerPrefs.GetInt(TopicUtils.GetPrefKey_IsPostAssessmentDone(HistoryTopic.NETWORKING), 0)
                );
                assessDone3 = PrefsConverter.IntToBoolean(
                    PlayerPrefs.GetInt(TopicUtils.GetPrefKey_IsPostAssessmentDone(HistoryTopic.SOFTWARE), 0)
                );
            }

            TextMeshProUGUI txtComputer = txtAssessment_Computer_Score[i].GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI txtNetworking = txtAssessment_Networking_Score[i].GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI txtSoftware = txtAssessment_Software_Score[i].GetComponent<TextMeshProUGUI>();

            txtComputer.text = assessDone1 ?
                $"{assessmentScores[i, (int)HistoryTopic.COMPUTER]} / {totalQuestions}" :
                notDoneScore;

            txtNetworking.text = assessDone2 ?
                $"{assessmentScores[i, (int)HistoryTopic.NETWORKING]} / {totalQuestions}" :
                notDoneScore;

            txtSoftware.text = assessDone3 ?
                $"{assessmentScores[i, (int)HistoryTopic.SOFTWARE]} / {totalQuestions}" :
                notDoneScore;
        }
    }

    void LoadGameAccuracy()
    {
        float[,] spGameAccuracy = staticData.profileStatisticsData.SPGameAccuracy;
        float[] mpGameAccuracy = staticData.profileStatisticsData.MPGameAccuracy;

        int totalAccuracyCount = 0;
        float totalAccuracy = 0;

        foreach (float accuracy in spGameAccuracy)
        {
            Debug.Log($"SP ACC: {accuracy}");

            if (accuracy > 0f && accuracy != float.NaN)
            {
                totalAccuracy += accuracy;
                totalAccuracyCount++;
            }
        }

        foreach (float accuracy in mpGameAccuracy)
        {
            Debug.Log($"MP ACC: {accuracy}");

            if (accuracy > 0f && accuracy != float.NaN)
            {
                totalAccuracy += accuracy;
                totalAccuracyCount++;
            }
        }

        // If there aren't accuracies to be averaged, return 0f immediately to avoid division by 0 / 0
        float avgAccuracy =
            (totalAccuracyCount > 0) ? totalAccuracy / totalAccuracyCount : 0f;

        Debug.Log($"Total: {totalAccuracyCount}, TotalAcc: {totalAccuracy}, Avg: {avgAccuracy}");

        double avgAccuracyText = System.Math.Round((avgAccuracy * 100), 2);
        txtAccuracy.GetComponent<TextMeshProUGUI>().text = $"{avgAccuracyText}%";
    }

    void LoadSPGameWinLoss()
    {
        // FIXME: Possibly not working / accurate
        int[,,] SPWinLossCount =
            staticData.profileStatisticsData.SPWinLossCount;

        int[,] MPWinLossCount =
            staticData.profileStatisticsData.MPWinLossCount;

        int spWin = 0;
        int spLoss = 0;

        for (
            int topicIndex = 0;
            topicIndex < SPWinLossCount.GetLength(0);
            topicIndex++
        )
        {
            for (
                int difficultyIndex = 0;
                difficultyIndex < SPWinLossCount.GetLength(1);
                difficultyIndex++
            )
            {
                spWin += SPWinLossCount[topicIndex, difficultyIndex, 0];
                spLoss += SPWinLossCount[topicIndex, difficultyIndex, 1];
            }
        }

        int mpWin = 0;
        int mpLoss = 0;

        for (
            int topicIndex = 0;
            topicIndex < SPWinLossCount.GetLength(0);
            topicIndex++
        )
        {
            mpWin += MPWinLossCount[topicIndex, 0];
            mpLoss += MPWinLossCount[topicIndex, 1];
        }

        txtSPWinOrLoss.GetComponent<TextMeshProUGUI>().text =
            $"{spWin}W / {spLoss}L";
        txtMPWinOrLoss.GetComponent<TextMeshProUGUI>().text =
            $"{mpWin}W / {mpLoss}L";

        int totalGames = spWin + spLoss + mpWin + mpLoss;

        txtTotalGames.GetComponent<TextMeshProUGUI>().text =
            totalGames + " game/s";
    }
}
