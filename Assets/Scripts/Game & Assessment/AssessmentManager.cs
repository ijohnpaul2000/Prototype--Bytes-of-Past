﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssessmentManager : MonoBehaviour
{
    // GAMEOBJECT REFERENCES
    public GameObject questionText;

    public GameObject btnAnswer1;

    public GameObject btnAnswer2;

    public GameObject btnAnswer3;

    public GameObject btnAnswer4;

    public GameObject endGameMenu;

    public GameObject txtScore;

    public GameObject txtTopic;

    public GameObject txtTestTopic;

    public GameObject txtPreOrPostAssessment;

    // QUESTIONS DATA
    private List<QuestionData> questions = new List<QuestionData>();

    private int currentQuestionIndex = 0;

    private QuestionData currentQuestion;

    private string[] currentChoices;

    private int currentScore = 0;

    // STATIC DATA
    private TOPIC selectedTopic;

    private bool isPostAssessment;

    StaticData staticData;

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
            Debug.LogError("Static Data Not Found: Play from the Main Menu");
            staticData = new StaticData();
        }

        // Bind OnClick to functions
        btnAnswer1
            .GetComponent<Button>()
            .onClick
            .AddListener(() => SelectAnswer1());

        btnAnswer2
            .GetComponent<Button>()
            .onClick
            .AddListener(() => SelectAnswer2());

        btnAnswer3
            .GetComponent<Button>()
            .onClick
            .AddListener(() => SelectAnswer3());

        btnAnswer4
            .GetComponent<Button>()
            .onClick
            .AddListener(() => SelectAnswer4());

        LoadQuestions();
        ShowNextQuestion();
    }

    void LoadQuestions()
    {
        // Load Question depending on Topic
        selectedTopic = staticData.SelectedTopic;
        isPostAssessment = staticData.IsPostAssessment;

        txtTestTopic.GetComponent<TextMeshProUGUI>().text =
            TopicUtils.GetName((TOPIC) selectedTopic);

        QuestionData[] resourcesQuestions = null;

        switch (selectedTopic)
        {
            case TOPIC.Computer:
                resourcesQuestions =
                    Resources.LoadAll<QuestionData>("AssessmentTests/Computer");
                break;
            case TOPIC.Networking:
                resourcesQuestions =
                    Resources
                        .LoadAll<QuestionData>("AssessmentTests/Networking");
                break;
            case TOPIC.Software:
                resourcesQuestions =
                    Resources.LoadAll<QuestionData>("AssessmentTests/Software");
                break;
            default:
                resourcesQuestions =
                    Resources.LoadAll<QuestionData>("AssessmentTests");
                break;
        }

        txtPreOrPostAssessment.GetComponent<TextMeshProUGUI>().text =
            isPostAssessment ? "Post-Assessment" : "Pre-Assessment";

        if (!isPostAssessment)
        {
            // If Pre-Assessment, load first 5 questions only
            questions = resourcesQuestions.Take(5).ToList();
        }
        else
        {
            // Else, shuffle the questions
            questions =
                resourcesQuestions.OrderBy(x => Random.Range(0f, 1f)).ToList();
        }
    }

    void ShowNextQuestion()
    {
        Debug.Log($"SCORE: {currentScore}/{questions.Count}");

        if (currentQuestionIndex >= questions.Count)
        {
            EndTest();
            return;
        }

        currentQuestion = questions.ElementAt(currentQuestionIndex);
        currentChoices = currentQuestion.Choices;

        string _questionText =
            $"Question: {questions.Count - questions.Count}/" +
            questions.Count +
            "\n\n" +
            currentQuestion.Question;

        questionText.GetComponent<TextMeshProUGUI>().text = _questionText;

        TextMeshProUGUI[] choicesTexts =
        {
            btnAnswer1.GetComponentInChildren<TextMeshProUGUI>(),
            btnAnswer2.GetComponentInChildren<TextMeshProUGUI>(),
            btnAnswer3.GetComponentInChildren<TextMeshProUGUI>(),
            btnAnswer4.GetComponentInChildren<TextMeshProUGUI>()
        };

        for (int i = 0; i < choicesTexts.Length; i++)
        {
            choicesTexts[i].text = currentChoices[i] ?? "NO DATA";
        }

        currentQuestionIndex++;
    }

    void EndTest()
    {
        txtScore.GetComponent<TextMeshProUGUI>().text =
            $"Score: {currentScore}/{questions.Count}";

        txtTopic.GetComponent<TextMeshProUGUI>().text =
            TopicUtils.GetName((TOPIC) selectedTopic);

        endGameMenu.SetActive(true);

        // Save Score
        try
        {
            staticData
                .profileStatisticsData
                .UpdateAssessmentScore(isPostAssessment
                    ? GAMEMODE.PostAssessment
                    : GAMEMODE.PreAssessment,
                selectedTopic,
                currentScore);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to save data: " + ex);
        }

        if (!isPostAssessment)
        {
            PlayerPrefs
                .SetInt(TopicUtils
                    .GetPrefKey_IsPreAssessmentDone(selectedTopic),
                1);
        }
        else
        {
            PlayerPrefs
                .SetInt(TopicUtils
                    .GetPrefKey_IsPostAssessmentDone(selectedTopic),
                1);
        }
    }

    // Answer Methods
    void SelectAnswer1()
    {
        if (currentQuestion.isAnswerCorrect(currentChoices[0]))
        {
            currentScore++;
        }
        ShowNextQuestion();
    }

    void SelectAnswer2()
    {
        if (currentQuestion.isAnswerCorrect(currentChoices[1]))
        {
            currentScore++;
        }
        ShowNextQuestion();
    }

    void SelectAnswer3()
    {
        if (currentQuestion.isAnswerCorrect(currentChoices[2]))
        {
            currentScore++;
        }
        ShowNextQuestion();
    }

    void SelectAnswer4()
    {
        if (currentQuestion.isAnswerCorrect(currentChoices[3]))
        {
            currentScore++;
        }
        ShowNextQuestion();
    }
}
