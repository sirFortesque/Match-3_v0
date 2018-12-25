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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class BoardManager : MonoBehaviour {
    public static BoardManager              instance; //Singleton pattern    
    public GameObject                       tile; //Tile prefab
    public ParticleSystem                   explosionPrefab;
    public GameObject                       moveCounterPrefab;
    public int                              xSize, ySize; //X and Y dimensions of the board
    public float                            shiftDelay; // Time delay for tile shifting 
    public bool                             wasShifted = false;
    public float                            scoreBlinkDelay;
    public int                              scorePointsInspector;

    public List<Sprite>                     characters = new List<Sprite>(); //Sprites for tiles
    public float                            chanceForNewSpecialTile;
    public List<Sprite>                     specialTiles = new List<Sprite>();
    public List<float>                      specialTilesChances = new List<float>();
    /* specialTilesChances:
     * Bomb big = 1.8
     * bomb cross = 0.3
     * clock = 0.6
     * x2score = 1.3
     * snowflake = 0.9
     */
    public static Dictionary<string, float> specialTilesChancesDictionary = new Dictionary<string, float>();       
    public static GameObject[,]             tiles; //Array for tiles in the board    
    public static List<List<GameObject>>    potentialMatches = new List<List<GameObject>>();
    
    public static readonly float            OpacityAnimationFrameDelay = .03f;
    public static readonly float            WaitBeforePotentialMatchesCheck = 1f;
    public static readonly float            WaitBeforeCheckNextPotentialMatches = 1f;
    public static readonly float            MoveFreezeTime = 7f;
    public static float                     X2scoreTime = 6f;   
    public static int                       FreezeTileSteps = 5;
    public static int                       ScorePoints;    

    private static IEnumerator              ShiftTilesDownCoroutine;
    private static IEnumerator              AnimatePotentialMatchesCoroutine;
    private static IEnumerator              CheckPotentialMatchesCoroutine;
    private static IEnumerator              BombCrossCoroutine;
    private static IEnumerator              MoveFreezeCoroutine;
    private static IEnumerator              X2scoreCoroutine;
    private static IEnumerator              FreezeTileCoroutine;
    private static IEnumerator              HighlightScoreCoroutine;
    private static IEnumerator              LoopedPlayCoroutine;

    private Color                           moveCounterPrefabDefaultColor;

    //Tell the game when a match is found and the board is re-filling
    public bool IsShifting { get; set; } 

    public static bool moveFreeze { get; set; }

    void Awake() {
        moveFreeze = false;

        moveCounterPrefabDefaultColor = moveCounterPrefab.GetComponent<Image>().color;

        if (specialTilesChancesDictionary.Count == 0) {
            for (int i = 0; i < specialTiles.Count; i++) {
                specialTilesChancesDictionary.Add(specialTiles[i].name, specialTilesChances[i]);
            }
        }

       /*
       //PlayerPrefs.SetInt("Level", PlayerPrefs.HasKey("Level") ? (PlayerPrefs.GetInt("Level") + 1) : 0);

        if (PlayerPrefs.HasKey("Level")) {
            PlayerPrefs.SetInt("Level", PlayerPrefs.GetInt("Level") + 1);
        }
        else {
            PlayerPrefs.SetInt("Level", 0);
        }
        
        /*
        Debug.Log("specialTilesChancesDictionary are:");
        string str = "";
        foreach (KeyValuePair<string, float> kvp in specialTilesChancesDictionary) {
                str += kvp.Key + ": " + kvp.Value + "\n";
            }
        Debug.Log(str);
         */
        OnSpecBombBig += HandleOnSpecBombBig;
        OnSpecBombCross += HandleOnSpecBombCross;
        OnSpecClock += HandleOnSpecClock;
        OnSpecSnowflake += HandleOnSpecSnowflake;
        OnSpecX2score += HandleOnSpecX2score;
    }

	// Use this for initialization
	void Start ()
	{
	    //Sets the singleton with reference of the BoardManager
	    instance = GetComponent<BoardManager>(); 	                    

	    Vector2 offset = tile.GetComponent<SpriteRenderer>().bounds.size;
	    CreateBoard(offset.x, offset.y);

        ScorePoints = scorePointsInspector;
	}

    void Update() {        
        if (CheckPotentialMatchesCoroutine != null && AnimatePotentialMatchesCoroutine != null && (Tile.instance.wasSwapped || BoardManager.instance.wasShifted)) {
            StopCheckForPotentialMatches();
        }             
    }
       
    public void CreateBoard (float xOffset, float yOffset)
    {                     
        tiles = new GameObject[xSize, ySize];
        //Starting position for the board generation
        float startX = transform.position.x; 
        float startY = transform.position.y;
        //Variables for preventing repetition of the tiles
        Sprite[]    previousLeft = new Sprite[ySize];
        Sprite      previousBelow = null;
        //position steps for special tiles
        int xPosSpecial = 0,
            yPosSpecial = 0; 
        /*
        //random number of SpecialTiles
        int rndNmbrSpecial = 0;
        rndNmbrSpecial = Random.Range(3, (int)((xSize * ySize) / specialTilesListCoef));
        if (rndNmbrSpecial > xSize) rndNmbrSpecial = xSize;
        Debug.Log("rndNmbrSpecial = " + rndNmbrSpecial);
         */                          
        xPosSpecial = UnityEngine.Random.Range(1, xSize);        
        for (int x = 0; x < xSize; x++) {

            yPosSpecial = UnityEngine.Random.Range(0, ySize);            
            for (int y = 0; y < ySize; y++)
            {                
                GameObject newTile = Instantiate(tile, new Vector3(startX + (xOffset * x), startY + (yOffset * y), 0), tile.transform.rotation);
                tiles[x,y] = newTile;
                //BoardManager is the parent for all tiles. For keeping Hierarchy clean 
                newTile.transform.parent = transform;

                newTile.GetComponent<Tile>().x = x;
                newTile.GetComponent<Tile>().y = y;
                
                // If there is appropriate coord instantiate special tile sprite // with special Attribute   
                if ( (x % xPosSpecial == 0) && (y == yPosSpecial) ) {                    
                    newTile.GetComponent<SpriteRenderer>().sprite = GetNewSpecialSprite();
                    //newTile.GetComponent<Tile>().attribute = newTile.GetComponent<SpriteRenderer>().sprite.name 
                    previousLeft[y] = null;
                    previousBelow = null;
                }
                else {                                                 
                    //Generation of the list of possible characters for this Sprite
                    List<Sprite> possibleCharacters = new List<Sprite>();
                    possibleCharacters.AddRange(characters);
                    possibleCharacters.Remove(previousLeft[y]);
                    possibleCharacters.Remove(previousBelow);

                    //Set the newly created tile's sprite to the randomly choosen sprite from the possibleCharacters array
                    Sprite newSprite = possibleCharacters[UnityEngine.Random.Range(0, possibleCharacters.Count)];
                    newTile.GetComponent<SpriteRenderer>().sprite = newSprite;                            
                    previousLeft[y] = newSprite;
                    previousBelow = newSprite;
                } 
            }
        }
        wasShifted = true;
    }

    // Coroutine for FindingNullTiles after matching
    public IEnumerator FindNullTiles() 
    {
        for (int x = 0; x < xSize; x++) {
            for (int y = 0; y < ySize; y++) {

                //if (tiles[x, y].GetComponent<Tile>().freezeTile) Debug.Log("FREEZE");

                if (tiles[x, y].GetComponent<SpriteRenderer>().sprite == null) {
                    //ShiftTilesDownCoroutine = ShiftTilesDown(x, y, shiftDelay);
                    yield return StartCoroutine(ShiftTilesDown(x, y, shiftDelay));
                    break;
                }
            }
        }
        // Re-check all tiles after matching 
        // and ClearAllMatches if they were created after shifting
        for (int x = 0; x < xSize; x++) {
            for (int y = 0; y < ySize; y++) {
                tiles[x, y].GetComponent<Tile>().ClearAllMatchess();
            }
        }
    }

    // Coroutine for ShiftingTilesDown after finding null tiles
    private IEnumerator ShiftTilesDown(int x, int yStart, float shiftDelay = .2f) {
        List<SpriteRenderer>    renders = new List<SpriteRenderer>();
        int                     nullCount = 0;
                                IsShifting = true;                        

        // Loop through and finds how many spaces it needs to shift downwards
        for (int y = yStart; y < ySize; y++) {

            /*
            if (tiles[x, y].GetComponent<Tile>().freezeTile) {
                Debug.Log("freeeze!!!");
                StopCoroutine(ShiftTilesDownCoroutine);
            }
             */

            SpriteRenderer render = tiles[x, y].GetComponent<SpriteRenderer>();
            if (render.sprite == null) {
                nullCount++; // Number of spaces
            }
            renders.Add(render); // Generating list of renders for shifting
        }

        // Shifting
        for (int i = 0; i < nullCount; i++) {
            GUIManager.instance.Score += ScorePoints;
            yield return new WaitForSeconds(shiftDelay);

            if (renders.Count == 1) {
                renders[0].sprite = GetNewSprite(x, ySize - 1);
                break;
            }

            for (int k = 0; k < renders.Count-1; k++) {
                renders[k].sprite = renders[k + 1].sprite;                

                // At the end of the shifting add new random sprite
                if (k == renders.Count-2) {
                    //if ("Random chance">1) then generate specialTile otherwise normal tile
                    if ((UnityEngine.Random.Range(0f, 1f) + chanceForNewSpecialTile) > 1) {
                        renders[k + 1].sprite = GetNewSpecialSprite();
                        //Debug.Log(renders[k + 1].sprite.name);
                    } else {
                        renders[k + 1].sprite = GetNewSprite(x, ySize - 1);
                    }                       
                }                
            }                       
        }        
        IsShifting = false;
        wasShifted = true;
    }

    private Sprite GetNewSprite(int x, int y) {        
        Sprite          sprite = null;        
        List<Sprite>    possibleCharacters = new List<Sprite>();        

        possibleCharacters.AddRange(characters);
        // A series of "if" statements make us sure that we dont go out of bounds.
        // Inside the "if" statements we remove possible duplicates 
        // that could cause an accidental match when choosing a new sprite
        if (x > 0) {
            possibleCharacters.Remove(tiles[x - 1, y].GetComponent<SpriteRenderer>().sprite);
        }
        if (x < xSize - 1) {
            possibleCharacters.Remove(tiles[x + 1, y].GetComponent<SpriteRenderer>().sprite);
        }        
        if (y > 0) {
            possibleCharacters.Remove(tiles[x, y - 1].GetComponent<SpriteRenderer>().sprite);
        }

        sprite = possibleCharacters[UnityEngine.Random.Range(0, possibleCharacters.Count)];        
        return sprite;                
    }

    private Sprite GetNewSpecialSprite() {
        Sprite  specialSprite = null;
        string  name = "";
        int     index = 0;
        float   chance = 0;
        float   specialTileChance = 0;
        float   rnd = 0;        

        while (chance < 3) {
            //Debug.Log("============================");            
            chance = 0;
            index = UnityEngine.Random.Range(0, specialTilesChancesDictionary.Values.Count);
            //Debug.Log("index = " + index);

            int j = 0;
            foreach (KeyValuePair<string, float> kvp in specialTilesChancesDictionary) {
                if (j == index) {
                    //Debug.Log("j = " + j + ", index = " + index);
                    specialTileChance = kvp.Value;
                    //Debug.Log("kvp.Value = " + kvp.Value);
                    //Debug.Log("specialTileChance = " + specialTileChance);
                    break;
                }
                j++;
            }

            rnd = UnityEngine.Random.Range(0f, 3f);
            //Debug.Log("rnd = " + rnd);
            chance = rnd + specialTileChance;
            //Debug.Log("chance = " + chance);
            //if (chance > 1) Debug.Log("!!!chance > 1!!!");
            //Debug.Log("============================");
        }

        foreach (KeyValuePair<string, float> kvp in specialTilesChancesDictionary) {
            if (specialTileChance == kvp.Value) {
                name = kvp.Key;
                //Debug.Log("kvp.Key - " + kvp.Key);
                break;
            }
        }
        //Debug.Log("name - " + name);

        foreach (Sprite sprite in specialTiles) {
            if (name == sprite.name) specialSprite = sprite;
        }
        //Debug.Log("specialSprite - " + specialSprite);

        return specialSprite;
    }

    ///////////////////////////////////////////////////////////////////////////
    ////////////////////////  SPECIAL TILES  //////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////
    public void ChooseEvent(GameObject tile) {
        string tileName = tile.GetComponent<SpriteRenderer>().sprite.name;
        switch (tileName) {
            case "SPEC_BOMB_BIG":
                OnSpecBombBig(tile);
                break;
            case "SPEC_BOMB_CROSS":
                OnSpecBombCross(tile);
                break;
            case "SPEC_CLOCK":
                OnSpecClock(tile);
                break;
            case "SPEC_SNOWFLAKE":
                OnSpecSnowflake(tile);
                break;
            case "SPEC_X2SCORE":
                OnSpecX2score(tile);
                break;
            default:
                Debug.Log("default");
                break;
        }
    }    


    public Action<GameObject> OnSpecBombBig;
    private void HandleOnSpecBombBig(GameObject tile) {
        //Debug.Log("IN OnSpecBombBig - " + tile.GetComponent<SpriteRenderer>().sprite.name);          
        ParticleSystem explosion;      
        SFXManager.instance.PlaySFX(Clip.Explosion);

        foreach (var item in Tile.instance.GetAllAdjacentTiles(tile.transform)) {
            if (item) {
                explosion = Instantiate(explosionPrefab, item.transform.position, item.transform.rotation);
                explosion.Play();
                item.GetComponent<SpriteRenderer>().sprite = null;
            }
                
        }
        explosion = Instantiate(explosionPrefab, tile.transform.position, tile.transform.rotation);
        explosion.Play();
        tile.GetComponent<SpriteRenderer>().sprite = null;
    }


    public Action<GameObject> OnSpecBombCross;
    private void HandleOnSpecBombCross(GameObject tile) {
        //Debug.Log("IN OnSpecBombCross - " + tile.GetComponent<SpriteRenderer>().sprite.name);
        int x = 0,
            y = 0;

        x = tile.GetComponent<Tile>().x;
        y = tile.GetComponent<Tile>().y;

        ParticleSystem explosion;

        SFXManager.instance.PauseSFX(Clip.Hyperfun);
        SFXManager.instance.PlaySFX(Clip.Explosion);        

        for (int i = 0; i < xSize; i++) {
            if (tiles[i, y]) {
                explosion = Instantiate(explosionPrefab, tiles[i, y].transform.position, tiles[i, y].transform.rotation);
                explosion.Play();
                tiles[i, y].GetComponent<SpriteRenderer>().sprite = null;                
            }
        }

        for (int j = 0; j < ySize; j++) {
            if (tiles[x, j]) {
                explosion = Instantiate(explosionPrefab, tiles[x, j].transform.position, tiles[x, j].transform.rotation);
                explosion.Play();
                tiles[x, j].GetComponent<SpriteRenderer>().sprite = null;                
            }
        }

        SFXManager.instance.UnPauseSFX(Clip.Hyperfun);
    }

    /*
    private void HandleOnSpecBombCross(GameObject tile) {        
        if (BombCrossCoroutine != null) StopCoroutine(BombCrossCoroutine);
        BombCrossCoroutine = BombCrossMethod(tile);
        StartCoroutine(BombCrossCoroutine);
    }

    private IEnumerator BombCrossMethod(GameObject tile) {
        //Debug.Log("IN OnSpecBombCross - " + tile.GetComponent<SpriteRenderer>().sprite.name);
        int x = 0,
            y = 0;

        x = tile.GetComponent<Tile>().x;
        y = tile.GetComponent<Tile>().y;

        ParticleSystem explosion;

        SFXManager.instance.PauseSFX(Clip.Hyperfun);
        //SFXManager.instance.PlaySFX(Clip.Explosion);        

        for (int i = 0; i < xSize; i++) {
            if (tiles[i, y]) {
                explosion = Instantiate(explosionPrefab, tiles[i, y].transform.position, tiles[i, y].transform.rotation);
                explosion.Play();
                tiles[i, y].GetComponent<SpriteRenderer>().sprite = null;
                SFXManager.instance.PlaySFX(Clip.Explosion);
                yield return new WaitUntil(() => !SFXManager.instance.IsPlayingSFX(Clip.Explosion ) );
            }
        }

        for (int j = 0; j < ySize; j++) {
            if (tiles[x, j]) {
                explosion = Instantiate(explosionPrefab, tiles[x, j].transform.position, tiles[x, j].transform.rotation);
                explosion.Play();
                tiles[x, j].GetComponent<SpriteRenderer>().sprite = null;
                SFXManager.instance.PlaySFX(Clip.Explosion);
                yield return new WaitUntil(() => !SFXManager.instance.IsPlayingSFX(Clip.Explosion));
            }
        }

        SFXManager.instance.UnPauseSFX(Clip.Hyperfun);
    }
     */

    public Action<GameObject> OnSpecClock;
    private void HandleOnSpecClock(GameObject tile) {
        //Debug.Log("IN OnSpecClock - " + tile.GetComponent<SpriteRenderer>().sprite.name);
        tile.GetComponent<SpriteRenderer>().sprite = null;
        if (MoveFreezeCoroutine != null) BoardManager.instance.StopCoroutine(MoveFreezeCoroutine);
        MoveFreezeCoroutine = MoveFreezeMethod();
        BoardManager.instance.StartCoroutine(MoveFreezeCoroutine);
    }
    private IEnumerator MoveFreezeMethod() {
        moveFreeze = true;
        //Debug.Log(moveFreeze.ToString() + ": " + Time.time.ToString());         
        Color myColor = new Color();
        //ColorUtility.TryParseHtmlString("#31FF00", out myColor);
        //moveCounterPrefab.GetComponent<Image>().color = myColor;

        ColorUtility.TryParseHtmlString("#00DEFF", out myColor);
        GUIManager.instance.moveCounterTxt.color = myColor;

        SFXManager.instance.PauseSFX(Clip.Hyperfun);
        SFXManager.instance.PlaySFX(Clip.Clock);
        
        yield return new WaitForSeconds(MoveFreezeTime);

        SFXManager.instance.StopSFX(Clip.Clock);
        SFXManager.instance.UnPauseSFX(Clip.Hyperfun);

        //moveCounterPrefab.GetComponent<Image>().color = Color.white;
        GUIManager.instance.moveCounterTxt.color = Color.white;
        moveFreeze = false;
        //Debug.Log(moveFreeze.ToString() + ": " + Time.time.ToString());
    }   


    public Action<GameObject> OnSpecX2score;
    public void HandleOnSpecX2score(GameObject tile) {
        //Debug.Log("IN OnSpecX2score - " + tile.GetComponent<SpriteRenderer>().sprite.name);
        tile.GetComponent<SpriteRenderer>().sprite = null;
        if (X2scoreCoroutine != null) BoardManager.instance.StopCoroutine(X2scoreCoroutine);
        X2scoreCoroutine = X2scoreMethod();
        BoardManager.instance.StartCoroutine(X2scoreCoroutine);
    }
    /* Other options of color blink

    // Random color
    //color.r = UnityEngine.Random.Range(0f, 1f);
    //color.g = UnityEngine.Random.Range(0f, 1f);
    //color.b = UnityEngine.Random.Range(0f, 1f);
    //color.a = UnityEngine.Random.Range(0f, 1f);

    One color blink
     
    if (rb == 1f) {
        color.r -= 0.2f;
        color.b -= 0.2f;
        GUIManager.instance.scoreTxt.color = color;
        if (color.r <= 0f) {
            rb = 0f;
            yield return new WaitForSeconds(scoreBlinkDelay);
            continue;
        }
    }
    if (rb == 0f) {                
        color.r += 0.2f;
        color.b += 0.2f;
        GUIManager.instance.scoreTxt.color = color;
        if (color.r >= 1f) {
            rb = 1f;                   
        }
    }       
    yield return new WaitForSeconds(scoreBlinkDelay);
     */
    public IEnumerator X2scoreMethod() {
        Color color = GUIManager.instance.scoreTxt.color;
        
        int[] rgbArr = new int[3];
        // red   -> rgbArr[0]
        // green -> rgbArr[1]
        // blue  -> rgbArr[2]                                      
                     
        ScorePoints = ScorePoints * 2;
        SFXManager.instance.PauseSFX(Clip.Hyperfun);
        SFXManager.instance.PlaySFX(Clip.X2Score);

        float startTime = Time.time;
        while ((Time.time - startTime) < X2scoreTime) { // Timer            

                // Increasing and decreasing RGB values 
                // one by one for color blinking
                for (int i = 0; i < rgbArr.Length; i++) {

                    if (rgbArr[i] == 0) {
                        for (float j = 0; j <= 1; j += 0.2f) {
                            if (i == 0) {
                                color.r = j;
                                GUIManager.instance.scoreTxt.color = color;
                            }
                            if (i == 1) {
                                color.g = j;
                                GUIManager.instance.scoreTxt.color = color;
                            }
                            if (i == 2) {
                                color.b = j;
                                GUIManager.instance.scoreTxt.color = color;
                            }
                            yield return new WaitForSeconds(scoreBlinkDelay);
                        }
                        rgbArr[i] = 1;
                        continue;
                    } else {
                        if (rgbArr[i] == 1) {
                            for (float j = 1; j >= 0; j -= 0.2f) {
                                if (i == 0) {
                                    color.r = j;
                                    GUIManager.instance.scoreTxt.color = color;
                                }
                                if (i == 1) {
                                    color.g = j;
                                    GUIManager.instance.scoreTxt.color = color;
                                }
                                if (i == 2) {
                                    color.b = j;
                                    GUIManager.instance.scoreTxt.color = color;
                                }
                                yield return new WaitForSeconds(scoreBlinkDelay);
                            }
                        }
                        rgbArr[i] = 0;
                        continue;
                    }
                }                                  
        }
        ScorePoints = ScorePoints / 2;       
        color.r = 1f;
        color.g = 1f;
        color.b = 1f;        
        GUIManager.instance.scoreTxt.color = color;
        SFXManager.instance.StopSFX(Clip.X2Score);
        SFXManager.instance.UnPauseSFX(Clip.Hyperfun);
    }


    public Action<GameObject> OnSpecSnowflake;
    private void HandleOnSpecSnowflake(GameObject tile) {
        //Debug.Log("IN OnSpecSnowflake - " + tile.GetComponent<SpriteRenderer>().sprite.name);              
        if (FreezeTileCoroutine != null) StopCoroutine(FreezeTileCoroutine);
        FreezeTileCoroutine = FreezeTileMethod(tile);
        StartCoroutine(FreezeTileCoroutine); 
        tile.GetComponent<SpriteRenderer>().sprite = null;
    } 
    private IEnumerator FreezeTileMethod(GameObject tile) {
        int x = tile.GetComponent<Tile>().x,
            y = tile.GetComponent<Tile>().y;

        List<GameObject> adjacentTiles = Tile.instance.GetAllAdjacentTiles(tile.transform);

        if ((x - 1) >= 0) {
            adjacentTiles.AddRange(Tile.instance.GetAllAdjacentTiles(tiles[x - 1, y].transform));
        }

        if ((x + 1) <= (xSize - 1)) {
            adjacentTiles.AddRange(Tile.instance.GetAllAdjacentTiles(tiles[x + 1, y].transform));
        }

        adjacentTiles = RemoveNullElements(adjacentTiles);

        foreach (var item in adjacentTiles) {
            item.GetComponent<Tile>().freezeTile = true;
            item.GetComponent<SpriteRenderer>().color = Color.blue;
        }
      
        SFXManager.instance.PlaySFX(Clip.Snowflake);
                
        int moveCounterNow = GUIManager.instance.MoveCounter;
        yield return new WaitUntil(() => (GUIManager.instance.MoveCounter == (moveCounterNow - FreezeTileSteps)));

        foreach (var item in adjacentTiles) {
            item.GetComponent<Tile>().freezeTile = false;
            item.GetComponent<SpriteRenderer>().color = Color.white;
        }        

        foreach (var item in adjacentTiles) {
            item.GetComponent<Tile>().ClearAllMatchess();
        }        
    }

    ///////////////////////////////////////////////////////////////////////////
    ////////////////////////  HELP BUTTON  ////////////////////////////////////
    ///////////////////////////////////////////////////////////////////////////
    /*
                            //Debug.Log("adjacentTiles[k] - " + adjacentTiles[k].GetComponent<SpriteRenderer>().sprite.name);                            
                            //Debug.Log("tiles[i,j] - " + tiles[i, j].GetComponent<SpriteRenderer>().sprite.name);
                            //Debug.Log("Iteration-[ " + k + "], adjacentTiles.Count - " + adjacentTiles.Count + ", "  + adjacentTiles[k].GetComponent<SpriteRenderer>().sprite.name);
     * 
     *  //Debug.Log("On [" + k + " ] iteration adjacentTiles are:");
                                for (int m = 0; m < adjacentTiles.Count; m++) {

                                    //Debug.Log("adjacent[ " + m + "] - " + adjacentTiles[m].GetComponent<SpriteRenderer>().sprite.name + ";");
                                    
                                }      
     * 
     *  Debug.Log("In potentialMatches.Add(tempList): ");
                            foreach (GameObject tile in tempList) {
                                Debug.Log(tile.GetComponent<SpriteRenderer>().sprite.name);
                            }
                            Debug.Log("Last potentialMatches:");
                            foreach (GameObject tile in potentialMatches[potentialMatches.Count - 1]) {
                                Debug.Log(tile.GetComponent<SpriteRenderer>().sprite.name);
                            }
     * 
     *  for (int m = 0; m < potentialMatches.Count; m++) {
                            foreach (GameObject tile in potentialMatches[m]) {
                                Debug.Log("tile - " + tile.GetComponent<SpriteRenderer>().sprite.name);
                            }
                        }
     

    
                        // Remove null elements
                        for (int k = 0; k < adjacentTiles2.Count; k++) {
                            if (adjacentTiles2[k] != null) {
                                tempList2.Add(adjacentTiles2[k]);
                            }
                        }
                         
    
    // Remove unmatched elements                                               
    for (int k = 0; k < tempList2.Count; k++) {
        if (tempList2[k].GetComponent<SpriteRenderer>().sprite ==
            tiles[i, j].GetComponent<SpriteRenderer>().sprite) {
            adjacentTiles2.Add(tempList2[k]);
        }
    }
    */    
    public void FindPotentialMatches() {
        if (Tile.instance.wasSwapped || BoardManager.instance.wasShifted) {
            potentialMatches.Clear();
            CheckCrossLike();
            CheckHorizontal();            
            CheckVertical();
        }

        if (HighlightScoreCoroutine != null) { 
            BoardManager.instance.StopCoroutine(HighlightScoreCoroutine);
        }
        HighlightScoreCoroutine = HighlightScore();
        BoardManager.instance.StartCoroutine(HighlightScoreCoroutine);

        // Animate potentialMatches
        StartCheckForPotentialMatches(); 

        Tile.instance.wasSwapped = false;
        BoardManager.instance.wasShifted = false;

        /*
        Debug.Log("potentialMatches are:");
        string str = "";
        for (int m = 0; m < potentialMatches.Count; m++) {
            str = "[" + m + "]:";
            foreach (GameObject tile in potentialMatches[m]) {
                str += tile.GetComponent<SpriteRenderer>().sprite.name + "; ";
            }
            Debug.Log(str);
        } 
         */ 
    }    

    // Check cross-like potential matches
    public void CheckCrossLike() {
        /* 
        *  *******
        *  ***&***
        *  **&*&**
        *  ***&***
        *  *******
        */

        List<GameObject> tempList = new List<GameObject>();
        List<GameObject> adjacentTiles = new List<GameObject>();

        for (int i = 0; i < xSize; i++) {
            for (int j = 0; j < ySize; j++) {                

                adjacentTiles.AddRange(Tile.instance.GetAllAdjacentTiles(tiles[i, j].GetComponent<Transform>()));                

                tempList = RemoveNullElements(adjacentTiles);

                // Comparing elements one by one for finding potencial matches
                for (int k = 0; k < tempList.Count - 2; k++) {
                    for (int l = k + 1; l < tempList.Count - 1; l++) {
                        if (tempList[k].GetComponent<SpriteRenderer>().sprite ==
                            tempList[l].GetComponent<SpriteRenderer>().sprite) {
                            for (int m = l + 1; m < tempList.Count; m++) {
                                List<GameObject> adjacentTiles2 = new List<GameObject>();

                                adjacentTiles2.AddRange(new List<GameObject>()
                                {
                                    tempList[k],    
                                    tempList[l],
                                    tempList[m] });
                                /*
                                Debug.Log("adjacentTiles are:");
                                string str = "";
                                for (int n = 0; n < adjacentTiles2.Count; n++) {
                                    str += "[" + n + "]:" + adjacentTiles2[n].GetComponent<SpriteRenderer>().sprite.name + "; ";
                                }
                                Debug.Log(str); 
                                 */

                                if (tempList[k].GetComponent<SpriteRenderer>().sprite ==
                                    tempList[m].GetComponent<SpriteRenderer>().sprite && !PotentialMatchesHas(adjacentTiles2)) {
                                    potentialMatches.Add(adjacentTiles2);
                                }
                            }
                        }
                    }
                }

                tempList.Clear();
                adjacentTiles.Clear();
            }
        }
    }

    // Check Horizontal potential matches
    public void CheckHorizontal() {

        List<GameObject> tempList = new List<GameObject>();

        for (int i = 0; i < xSize - 1; i++) {
            for (int j = 0; j < ySize; j++) {

                if (tiles[i, j].GetComponent<SpriteRenderer>().sprite ==
                    tiles[i + 1, j].GetComponent<SpriteRenderer>().sprite) {

                    List<GameObject> adjacentTiles = new List<GameObject>();

                    if (i > 0 && i < xSize - 2) {
                        adjacentTiles.AddRange(
                            Tile.instance.GetAllAdjacentTiles(tiles[i - 1, j].GetComponent<Transform>()));
                        adjacentTiles.AddRange(
                            Tile.instance.GetAllAdjacentTiles(tiles[i + 2, j].GetComponent<Transform>()));
                        // "Remove" null elements
                        tempList = RemoveNullElements(adjacentTiles);
                        // "Remove" unmatched elements                                       
                        adjacentTiles = RemoveUnmatchedElements(tempList, tiles[i, j]);      
                        // Add to the potentialMatches if the same is not present
                        if (adjacentTiles.Count >= 3 && !PotentialMatchesHas(adjacentTiles)) {
                            potentialMatches.Add(adjacentTiles);
                        }                                                                  
                    }
                                                                     
                    if (i == 0) {                        
                        adjacentTiles.Add(tiles[i,j]);
                        adjacentTiles.AddRange(
                            Tile.instance.GetAllAdjacentTiles(tiles[i + 2, j].GetComponent<Transform>()));                        
                        tempList = RemoveNullElements(adjacentTiles);
                        adjacentTiles = RemoveUnmatchedElements(tempList, tiles[i, j]);
                        if (adjacentTiles.Count >= 3 && !PotentialMatchesHas(adjacentTiles)) {
                            potentialMatches.Add(adjacentTiles);
                        }     
                    }                   

                    
                    if (i == xSize - 2) {
                        adjacentTiles.Add(tiles[i + 1, j]);
                        adjacentTiles.AddRange(Tile.instance.GetAllAdjacentTiles(tiles[i - 1, j].GetComponent<Transform>()));                                     
                        tempList = RemoveNullElements(adjacentTiles);
                        adjacentTiles = RemoveUnmatchedElements(tempList, tiles[i, j]);                   
                        if (adjacentTiles.Count >= 3 && !PotentialMatchesHas(adjacentTiles)) {
                            potentialMatches.Add(adjacentTiles);
                        }     
                    }
                     
                }
            }
        }
    }

    //Check Vertical potential matches
    private void CheckVertical() {
        
        List<GameObject> tempList = new List<GameObject>();

        for (int i = 0; i < xSize; i++) {
            for (int j = 0; j < ySize - 1; j++) {

                if (tiles[i, j].GetComponent<SpriteRenderer>().sprite ==
                    tiles[i, j + 1].GetComponent<SpriteRenderer>().sprite) {

                    List<GameObject> adjacentTiles = new List<GameObject>();


                    if (j > 0 && j < ySize - 2) {
                        adjacentTiles.AddRange(
                            Tile.instance.GetAllAdjacentTiles(tiles[i, j - 1].GetComponent<Transform>()));
                        adjacentTiles.AddRange(
                            Tile.instance.GetAllAdjacentTiles(tiles[i, j + 2].GetComponent<Transform>()));
                        // "Remove" null elements
                        tempList = RemoveNullElements(adjacentTiles);
                        // "Remove" unmatched elements                                       
                        adjacentTiles = RemoveUnmatchedElements(tempList, tiles[i, j]);
                        // Add to the potentialMatches if the same is not present
                        if (adjacentTiles.Count >= 3 && !PotentialMatchesHas(adjacentTiles)) {
                            potentialMatches.Add(adjacentTiles);
                        }
                    }

                    if (j == 0) {
                        adjacentTiles.Add(tiles[i, j]);
                        adjacentTiles.AddRange(
                            Tile.instance.GetAllAdjacentTiles(tiles[i, j + 2].GetComponent<Transform>()));
                        tempList = RemoveNullElements(adjacentTiles);
                        adjacentTiles = RemoveUnmatchedElements(tempList, tiles[i, j]);
                        if (adjacentTiles.Count >= 3 && !PotentialMatchesHas(adjacentTiles)) {
                            potentialMatches.Add(adjacentTiles);
                        }
                    }


                    if (j == ySize - 2) {
                        adjacentTiles.Add(tiles[i, j + 1]);
                        adjacentTiles.AddRange(Tile.instance.GetAllAdjacentTiles(tiles[i, j - 1].GetComponent<Transform>()));
                        tempList = RemoveNullElements(adjacentTiles);
                        adjacentTiles = RemoveUnmatchedElements(tempList, tiles[i, j]);
                        if (adjacentTiles.Count >= 3 && !PotentialMatchesHas(adjacentTiles)) {
                            potentialMatches.Add(adjacentTiles);
                        }
                    }
                    
                }
            }
        }
    }

    IEnumerator HighlightScore() {
        GUIManager.instance.Score -= 300;
        GUIManager.instance.scoreTxt.color = Color.red;
        yield return new WaitForSeconds(0.2f);
        GUIManager.instance.scoreTxt.color = Color.white;
    }

    private List<GameObject> RemoveNullElements(List<GameObject> adjacentTiles) {
        List<GameObject> tempList = new List<GameObject>();
        for (int k = 0; k < adjacentTiles.Count; k++) {
            if (adjacentTiles[k] != null) {
                tempList.Add(adjacentTiles[k]);
            }
        }
        return tempList;
    } // <<< CHANGE! (make it thru out/ref)

    private List<GameObject> RemoveUnmatchedElements(List<GameObject> adjacentTiles, GameObject tile) {
        List<GameObject> tempList = new List<GameObject>();
        for (int k = 0; k < adjacentTiles.Count; k++) {
            if (adjacentTiles[k].GetComponent<SpriteRenderer>().sprite ==
                tile.GetComponent<SpriteRenderer>().sprite) {
                    tempList.Add(adjacentTiles[k]);
            }
        }
        return tempList;
        /*
        Debug.Log("Tile - " + tile.GetComponent<SpriteRenderer>().sprite.name);

        Debug.Log("adjacentTiles are:");
        string str = "";
        for (int m = 0; m < adjacentTiles.Count; m++) {
            str += adjacentTiles[m].GetComponent<SpriteRenderer>().sprite.name + "; ";
        }
        Debug.Log(str);

        Debug.Log("tempList are:");
        string str2 = "";
        for (int m = 0; m < tempList.Count; m++) {
            str2 += tempList[m].GetComponent<SpriteRenderer>().sprite.name + "; ";
        }
        Debug.Log(str2);
         */        
    }

    private bool PotentialMatchesHas(List<GameObject> tempList) {
        /*
        Debug.Log("IN <<PotentialMatchesHas>> tempList is:");
        string str = "";
        foreach (var item in tempList) {
                str += item.GetComponent<SpriteRenderer>().sprite.name + "; ";
            }
            Debug.Log(str);
        */
        List<GameObject> fetchedPM = new List<GameObject>();

        for (int i = 0; i < potentialMatches.Count; i++) {
            fetchedPM = potentialMatches[i];
            if (fetchedPM.Count == tempList.Count) {
                for (int j = 0; j < tempList.Count; j++) {
                    if (tempList[j] != fetchedPM[j]) {
                        goto nextPM;
                    }
                }                
                return true;
            }
            nextPM: ;
        }        
        return false;
    }

    private void StartCheckForPotentialMatches() {
        StopCheckForPotentialMatches();
        //get a reference to stop it later
        CheckPotentialMatchesCoroutine = CheckPotentialMatches();  
        BoardManager.instance.StartCoroutine(CheckPotentialMatchesCoroutine);
    }

    public void StopCheckForPotentialMatches() {
        if (AnimatePotentialMatchesCoroutine != null)
            BoardManager.instance.StopCoroutine(AnimatePotentialMatchesCoroutine);
        if (CheckPotentialMatchesCoroutine != null)
            BoardManager.instance.StopCoroutine(CheckPotentialMatchesCoroutine);
        ResetOpacityOnPotentialMatches();
    }

    private void ResetOpacityOnPotentialMatches() {
        if (potentialMatches != null)
            for (int i = 0; i < potentialMatches.Count; i++) {
                foreach (var item in potentialMatches[i]) {
                    if (item == null) break;
                    Color c = item.GetComponent<SpriteRenderer>().color;
                    c.a = 1.0f;
                    item.GetComponent<SpriteRenderer>().color = c;
                }
            }
    }

    private IEnumerator CheckPotentialMatches() {
        yield return new WaitForSeconds(WaitBeforePotentialMatchesCheck);
        if (potentialMatches != null) {            
                AnimatePotentialMatchesCoroutine = AnimateAllPotentialMatches(potentialMatches);
                BoardManager.instance.StartCoroutine(AnimatePotentialMatchesCoroutine);
                //yield return new WaitForSeconds(WaitBeforePotentialMatchesCheck);            
        }
    }

    public static IEnumerator AnimateAllPotentialMatches(List<List<GameObject>> potentialMatches) {
            for (int j = 0; j < potentialMatches.Count; j++) {
                SFXManager.instance.PlaySFX(Clip.Help);

                for (float i = 1f; i >= 0.3f; i -= 0.1f) {
                    foreach (var item in potentialMatches[j]) {
                        Color c = item.GetComponent<SpriteRenderer>().color;
                        c.a = i;
                        item.GetComponent<SpriteRenderer>().color = c;                       
                    }
                    yield return new WaitForSeconds(OpacityAnimationFrameDelay);
                }

                for (float i = 0.3f; i <= 1f; i += 0.1f) {
                    foreach (var item in potentialMatches[j]) {
                        Color c = item.GetComponent<SpriteRenderer>().color;
                        c.a = i;
                        item.GetComponent<SpriteRenderer>().color = c;                       
                    }
                    yield return new WaitForSeconds(OpacityAnimationFrameDelay);
                }

                yield return new WaitForSeconds(WaitBeforeCheckNextPotentialMatches);
            }       
    }
}
