﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopicSelect : MonoBehaviour
{
    StaticData staticData;

    public GameObject

            sceneLoader,
            btnComputer,
            btnNetworking,
            btnSoftware;

    void Awake()
    {
        try
        {
            staticData =
                GameObject
                    .FindWithTag("Static Data")
                    .GetComponent<StaticData>();
        }
        catch (System.NullReferenceException)
        {
            Debug.LogError("No Static Data detected: Run from Main Menu");
        }

        btnComputer
            .GetComponent<Button>()
            .onClick
            .AddListener(() => OnTopicSelect(TOPIC.Computer));

        btnNetworking
            .GetComponent<Button>()
            .onClick
            .AddListener(() => OnTopicSelect(TOPIC.Networking));

        btnSoftware
            .GetComponent<Button>()
            .onClick
            .AddListener(() => OnTopicSelect(TOPIC.Software));

        SetTopicDisabled(btnComputer.GetComponent<Button>(), TOPIC.Computer);
        SetTopicDisabled(btnNetworking.GetComponent<Button>(),
        TOPIC.Networking);
        SetTopicDisabled(btnSoftware.GetComponent<Button>(), TOPIC.Software);
    }

    void SetTopicDisabled(Button button, TOPIC topic)
    {
        bool isPreAssessmentDone =
            PlayerPrefs
                .GetInt(TopicUtils.GetPrefKey_IsPreAssessmentDone(topic), 0) ==
            1;

        bool isPlayed =
            PlayerPrefs.GetInt(TopicUtils.GetPrefKey_IsPlayed(topic), 0) == 1;

        bool isPostAssessmentDone =
            PlayerPrefs
                .GetInt(TopicUtils.GetPrefKey_IsPostAssessmentDone(topic), 0) ==
            1;
        TextMeshProUGUI buttonText =
            button
                .gameObject
                .transform
                .Find("Button")
                .GetComponentInChildren<TextMeshProUGUI>();

        if (staticData.SelectedGameMode == GAMEMODE.PostAssessment)
        {
            if (isPreAssessmentDone && !isPostAssessmentDone)
            {
                /*
                TODO: Uncomment last condition on release. Student shouldn't be able to
                take POST-Assessment without playing the game first!
                */
                button.interactable =
                    !(isPreAssessmentDone && !isPostAssessmentDone); /* && isPlayed */

                buttonText.text = "Locked";
            }
        }
        else if (staticData.SelectedGameMode == GAMEMODE.SinglePlayer)
        {
            if (!isPreAssessmentDone)
            {
                buttonText.text = "Take Pre-Test";
            }
        }

        Debug
            .Log(topic +
            $"\nPRE-Assessment Done? {isPreAssessmentDone}" +
            $"\nPLAYED? {isPlayed}" +
            $"\nPOST-Assessment Done? {isPostAssessmentDone}");
    }

    void OnTopicSelect(TOPIC topic)
    {
        staticData.SelectedTopic = topic;

        GAMEMODE gameMode = staticData.SelectedGameMode;

        switch (gameMode)
        {
            case GAMEMODE.SinglePlayer:
                if (
                    PlayerPrefs
                        .GetInt(TopicUtils
                            .GetPrefKey_IsPreAssessmentDone(staticData
                                .SelectedTopic),
                        0) ==
                    1
                )
                {
                    sceneLoader
                        .GetComponent<SceneLoader>()
                        .GoToDifficultySelect();
                }
                else
                {
                    // if the player hasn't played the topic yet, do a pre-assessment first
                    LoadPreAssessment();
                }
                break;
            case GAMEMODE.Multiplayer:
                sceneLoader.GetComponent<SceneLoader>().GoToDifficultySelect();
                break;
            case GAMEMODE.PreAssessment:
                break;
            case GAMEMODE.PostAssessment:
                staticData.IsPostAssessment = true;
                sceneLoader.GetComponent<SceneLoader>().GoToAssessmentTest();
                break;
            default:
                Debug.LogError("No Game Mode Selected");
                sceneLoader.GetComponent<SceneLoader>().GoToDifficultySelect();
                break;
        }
    }

    void LoadPreAssessment()
    {
        staticData.SelectedGameMode = GAMEMODE.PreAssessment;
        staticData.IsPostAssessment = false;
        sceneLoader.GetComponent<SceneLoader>().GoToAssessmentTest();
    }
}
