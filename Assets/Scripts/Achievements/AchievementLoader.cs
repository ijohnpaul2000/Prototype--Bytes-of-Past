﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementLoader : MonoBehaviour
{
    public Transform achievementsContainer;
    public GameObject achievementItemPrefab;

    void Start()
    {
        LoadAchievements();
        GetComponent<CanvasGroup>().interactable = true;
        GetComponent<CanvasGroup>().blocksRaycasts = true;
    }


    void LoadAchievements()
    {
        AchievementData[] achievements = AchievementChecker.GetAchievements();

        foreach (AchievementData data in achievements)
        {
            AddItem(data, achievementsContainer, achievementItemPrefab);
        }

        // Set scroll to beginning
        GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
    }

    public static void AddItem(AchievementData data, Transform parent, GameObject prefab)
    {
        GameObject item = Instantiate(prefab, parent);

        Debug.Log(item.transform.Find("TextCol").Find("Title") == null);
        Debug.Log(item.transform.Find("TextCol").Find("Description") == null);

        Debug.Log(item.transform.Find("ImageCol").Find("Image") == null);

        Debug.Log(data == null);

        item.transform.Find("TextCol").Find("Title").GetComponent<TextMeshProUGUI>().text = data.title;
        item.transform.Find("TextCol").Find("Description").GetComponent<TextMeshProUGUI>().text = data.description;

        if (!data.isDone)
        {
            item.transform.Find("ImageCol").Find("Image").GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }
}
