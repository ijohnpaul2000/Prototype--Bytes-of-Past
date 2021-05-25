﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class LevelLoader : MonoBehaviour
{
    public GameObject loadingScreen;
    public Slider slider;


    private void Start()
    {
        loadingScreen.gameObject.SetActive(false);
    }
    public void LoadLevel (int sceneIndex)
    {

        StartCoroutine(LoadAsynchronously(sceneIndex));
    }
    IEnumerator LoadAsynchronously (int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);


        loadingScreen.SetActive(true);

        while (!operation.isDone)
        {

            float progress = Mathf.Clamp01(operation.progress / .9f);

            slider.value = progress;
            Debug.Log(progress);

            yield return null;

        }

        
    }
}
