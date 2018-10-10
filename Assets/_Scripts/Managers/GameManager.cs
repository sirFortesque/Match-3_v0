/*
 * Part of this code were taken from the internet tutorial and requires the following comment:
 * 
 * Copyright (c) 2017 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

public class GameManager : MonoBehaviour {

	public static GameManager   instance;
	public GameObject           faderObj;
	public Image                faderImg;
	public bool                 gameOver = false;
	public float                fadeSpeed = .02f;
    public Text                 menuHighScoreTxt; // Used only in the "Menu" scene.
    public int                  levelNumber;
    public int                  startGoal = 1500;
    public int                  goal;
    public int                  numberOfRuleSheets;  
    public List<GameObject>     ruleSheets = new List<GameObject>();    
    
	private Color               fadeTransparency = new Color(0, 0, 0, .04f);
	private string              currentScene;
	private AsyncOperation      async;
    

    void Awake() {
        // Only 1 Game Manager can exist at a time
        if (instance == null) {
            DontDestroyOnLoad(gameObject);            
            instance = GetComponent<GameManager>();
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
            levelNumber = 0;
            goal = 0;
        }
        else {
            Destroy(gameObject);
        }

        if (menuHighScoreTxt && PlayerPrefs.HasKey("HighScore")) {
            menuHighScoreTxt.text = "HighScore: " + PlayerPrefs.GetInt("HighScore").ToString() + "!";
        }                
    }  

    private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode) {
        currentScene = scene.name;

        // increasing level difficulty
        if (CurrentSceneName == "Game") {            
            levelNumber++;
            goal = startGoal * levelNumber;
            GUIManager.instance.MoveCounter -= 5 * levelNumber;
                        
            Debug.Log("level - " + levelNumber);            
            Debug.Log("goal - " + goal);         
            Debug.Log("GUIManager.instance.MoveCounter - " + GUIManager.instance.moveCounter);
        }

        if (CurrentSceneName == "Menu") {            
            ruleSheets.Clear();
            for (int i = 0; i < numberOfRuleSheets; i++) {
                ruleSheets.Add(new GameObject());
            }

            var allGO = Resources.FindObjectsOfTypeAll<GameObject>();
            // OR(!!!)
            // ruleSheets enabled,
            // add references to ruleSheets,
            // ruleSheets disabled.

            foreach (var item in allGO) {
                if (item.name.StartsWith("Sheet_") && !(item.scene.name == null)) {
                    // item.scene.name == null - ITS PREFAB!!                      

                    // "item.name.Substring(6)" its digit after "Sheet_" 
                    // and minus one its index for ruleSheets list
                    ruleSheets[Convert.ToInt32(item.name.Substring(6)) - 1] = item;     
                }
            }            
        }

        instance.StartCoroutine(FadeIn(instance.faderObj, instance.faderImg));
    }

    // Iterate the fader transparency to 0%
    public IEnumerator FadeIn(GameObject faderObject, Image fader) {
        while (fader.color.a > 0) {
            fader.color -= fadeTransparency;
            yield return new WaitForSeconds(fadeSpeed);
        }
        faderObject.SetActive(false);
    }

    //Iterate the fader transparency to 100%
    IEnumerator FadeOut(GameObject faderObject, Image fader) {
        faderObject.SetActive(true);
        while (fader.color.a < 1) {
            fader.color += fadeTransparency;
            yield return new WaitForSeconds(fadeSpeed);
        }
        ActivateScene(); //Activate the scene when the fade ends
    }

    // Allows the scene to change once it is loaded
    public void ActivateScene() {
        async.allowSceneActivation = true;                
    }

    private bool isReturning = false;
    public void ReturnToMenu() {
        if (isReturning) {
            return;
        }

        if (CurrentSceneName != "Menu") {
            Time.timeScale = 1;
            StopAllCoroutines();            
            LoadScene("Menu");
            GameManager.instance.levelNumber = 0;
            GameManager.instance.goal = 0;            
            isReturning = true;
        }
    }

    // Get the current scene name
    public string CurrentSceneName {
        get {
            return currentScene;
        }
    }

	// Load a scene with a specified string name
	public void LoadScene(string sceneName) {
		instance.StartCoroutine(Load(sceneName));
		instance.StartCoroutine(FadeOut(instance.faderObj, instance.faderImg));
	}

    // Begin loading a scene with a specified string asynchronously
    IEnumerator Load(string sceneName) {
        async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;
        yield return async;
        isReturning = false;
    }
    
	// Reload the current scene
	public void ReloadScene() {
		LoadScene(SceneManager.GetActiveScene().name);
	}
    
    public void ExitGame() {
		// If we are running in a standalone build of the game
		#if UNITY_STANDALONE
			// Quit the application
			Application.Quit();
		#endif

		// If we are running in the editor
		#if UNITY_EDITOR
			// Stop playing the scene
			UnityEditor.EditorApplication.isPlaying = false;
		#endif
	}

    public void RulesButton(bool prevORnext) { // prev = false; next = true
        // Find which sheet is active now
        int index = 0;        
        int i = 0;
        foreach (var item in GameManager.instance.ruleSheets) {
            if (GameManager.instance.ruleSheets[i].activeSelf) {
                index = i;                
                break;
            }
            i++;
        }

        if (prevORnext) { // to the Next Sheet            
            GameManager.instance.ruleSheets[index].SetActive(false);
            GameManager.instance.ruleSheets[index + 1].SetActive(true);            
        }
        else { // to the Prev Sheet
            GameManager.instance.ruleSheets[index].SetActive(false);
            GameManager.instance.ruleSheets[index - 1].SetActive(true);            
        }
    }
}
