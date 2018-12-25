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

public enum Clip { Select, Swap, Clear, Explosion, Clock, X2Score, Snowflake, Win, Lose, Help, Hyperfun };

public class SFXManager : MonoBehaviour {
	public static SFXManager instance;

	private AudioSource[] sfx;

    /*
    void Awake() {
        if (instance == null) {
            DontDestroyOnLoad(gameObject);
            instance = GetComponent<SFXManager>();
            sfx = GetComponents<AudioSource>();
        } else {
            Destroy(gameObject);
        }
    }
     */

	void Start () {
        if (instance == null) {
            DontDestroyOnLoad(gameObject);
            instance = GetComponent<SFXManager>();
            sfx = GetComponents<AudioSource>();
        } else {
            Destroy(gameObject);
        }
    }

	public void PlaySFX(Clip audioClip) {
		sfx[(int)audioClip].Play();	    
	}

    public void StopSFX(Clip audioClip) {
        sfx[(int)audioClip].Stop();
    }

    public void PauseSFX(Clip audioClip) {
        sfx[(int)audioClip].Pause();
    }

    public void UnPauseSFX(Clip audioClip) {
        sfx[(int)audioClip].UnPause();
    }

    public bool IsPlayingSFX(Clip audioClip) {
        return sfx[(int)audioClip].isPlaying;
    }
}
