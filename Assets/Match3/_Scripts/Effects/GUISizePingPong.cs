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
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class GUISizePingPong : MonoBehaviour {

	public Vector3 minScale = new Vector3(.5f, .5f, .5f);
	public float changeDelay = .03f;
	RectTransform rectTransform;

    public Text menuHighScoreTxt; // Used only in the "Menu" scene.


	void Start() {
		rectTransform = GetComponent<RectTransform>();
		StartCoroutine(PingPongSize());

        if (PlayerPrefs.HasKey("HighScore")) {
            menuHighScoreTxt.text = "HighScore: " + PlayerPrefs.GetInt("HighScore").ToString() + "!";
        } else menuHighScoreTxt.text = "";
	}

	// Change size down to minScale then back up to max scale
	IEnumerator PingPongSize() {
		Vector3 startingScale = rectTransform.localScale;
		Vector3 currentScale = startingScale;
		
		// How much to change the scale each iteration
		Vector3 changeScale = new Vector3(.01f, .01f, .01f);
		bool shrink = true;

		while (true) {
			if (shrink) {
				currentScale -= changeScale;
				if (currentScale.x <= minScale.x) {
					shrink = false;
				}
			} else {
				currentScale += changeScale;
				if (currentScale.x >= 1) {
					shrink = true;
				}
            }
			rectTransform.localScale = currentScale;
			yield return new WaitForSeconds(changeDelay);
		}
	}
}
