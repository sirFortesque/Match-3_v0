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
using System.Collections;

public class GUIManager : MonoBehaviour {
    public static GUIManager   instance; // Singleton pattern

	// Level GUI
	public  Text                scoreTxt;
	public  Text                moveCounterTxt;
    public  int                 moveCounter;
    private int                 score;
    // Start Screen
    public  Text                levelNumberTxt;
    public  Text                goalTxt;
    // Pause Screen
    public  GameObject          pauseScreen;
    public  Text                levelNumberPauseScreenTxt;
    public  Text                goalPauseScreenTxt;
    public  Text                highScorePauseScreenTxt;
    private static bool         paused;
    // Win screen
    public  GameObject          winScreen;
    public  Text                scoreWinTxt;
    public  Text                highScoreWinTxt;
    // Lose screen
    public  GameObject          loseScreen;
    public  Text                scoreLoseTxt;
    public  Text                highScoreLoseTxt;
    // Game over panel
    public  GameObject          gameOverPanel;
    public  Text                yourScoreTxt;
    public  Text                highScoreTxt;

    /*
    public GameObject levelGUI;
    public GameObject menu;
    public GameObject faderObj;
    public Image faderImg;
    public void ButtonPlay() {
        menu.SetActive(false);
        faderObj.SetActive(true);
        GameManager.instance.FadeIn(faderObj, faderImg);
        levelGUI.SetActive(true);
    }
     */

    void Awake() {
        moveCounterTxt.text = moveCounter.ToString();
        instance = GetComponent<GUIManager>();

        paused = false;
        /*
        // Finds inactive PauseScreen and assign it
        foreach (var GO in Resources.FindObjectsOfTypeAll(typeof(GameObject))) {
            if (GO.name == "PauseScreen") pauseScreen = (GameObject)GO;
        }
         */
    }

    void Start() {
        levelNumberTxt.text = "Level " + GameManager.instance.levelNumber.ToString() + " !";
        levelNumberPauseScreenTxt.text = levelNumberTxt.text;
        goalTxt.text = "Your goal \n" + GameManager.instance.goal.ToString() + " points";
        goalPauseScreenTxt.text = goalTxt.text;
        highScorePauseScreenTxt.text = "Best: " + PlayerPrefs.GetInt("HighScore").ToString();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            PauseMethod();
        }
    }

    /*
   void OnGUI() {
       Event e = Event.current;
       if (e.isKey && (e.keyCode == KeyCode.Escape)) {
           if (e.type == EventType.KeyUp) {
               PauseMethod();
           }
       }
   }
    */

    public int Score {
        get {
            return score;            
        }
        set {
            score = value;            
            if (score >= GameManager.instance.goal) {
                StartCoroutine(WaitForShifting());
            }             
            scoreTxt.text = score.ToString();
        }
    }

    public int MoveCounter {
        get {
            return moveCounter;            
        }
        set {
            moveCounter = value;
            if (moveCounter <= 0) {
                moveCounter = 0;
                StartCoroutine(WaitForShifting());
            }
            moveCounterTxt.text = moveCounter.ToString();
        }
    }

	// Show the game over panel
	public void GameOver() {
        StopAllCoroutines();

	    if (score >= GameManager.instance.goal) {            
	        winScreen.SetActive(true);

            SFXManager.instance.PauseSFX(Clip.Hyperfun);
            SFXManager.instance.StopSFX(Clip.X2Score);
            SFXManager.instance.StopSFX(Clip.Clock);
	        SFXManager.instance.PlaySFX(Clip.Win);

	        if (score > PlayerPrefs.GetInt("HighScore")) {
	            PlayerPrefs.SetInt("HighScore", score);
	            highScoreWinTxt.text = "New Best: " + PlayerPrefs.GetInt("HighScore").ToString();
	        } else {
	            highScoreWinTxt.text = "Best: " + PlayerPrefs.GetInt("HighScore").ToString();
	        }

	        scoreWinTxt.text = score.ToString();
	        return;
	    }

	    if (moveCounter <= 0) {
            loseScreen.SetActive(true);
	        GameManager.instance.gameOver = true;            	        

            GameManager.instance.StopSFX();
	        SFXManager.instance.PlaySFX(Clip.Lose);

	        if (score > PlayerPrefs.GetInt("HighScore")) {
	            PlayerPrefs.SetInt("HighScore", score);
	            highScoreLoseTxt.text = "New Best: " + PlayerPrefs.GetInt("HighScore").ToString();
	        } else {
	            highScoreLoseTxt.text = "Best: " + PlayerPrefs.GetInt("HighScore").ToString();
	        }

	        scoreLoseTxt.text = score.ToString();
	    }

	    /*
		//gameOverPanel.SetActive(true);

		if (score > PlayerPrefs.GetInt("HighScore")) {
			PlayerPrefs.SetInt("HighScore", score);
			highScoreTxt.text = "New Best: " + PlayerPrefs.GetInt("HighScore").ToString();
		} else {
			highScoreTxt.text = "Best: " + PlayerPrefs.GetInt("HighScore").ToString();
		}

		yourScoreTxt.text = score.ToString();
         */
	}

    private IEnumerator WaitForShifting() {
        yield return new WaitUntil(() => !BoardManager.instance.IsShifting);
        yield return new WaitForSeconds(.25f);
        GameOver();
    }   

    public void PauseMethod() {
        if (!paused) {
            Time.timeScale = 0;
            pauseScreen.SetActive(true);
            SFXManager.instance.PauseSFX(Clip.Hyperfun);
            paused = true;
        } else {
            Time.timeScale = 1;
            pauseScreen.SetActive(false);
            SFXManager.instance.UnPauseSFX(Clip.Hyperfun);
            paused = false;
        }
    }

}
