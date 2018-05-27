/*
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

public class BoardManager : MonoBehaviour {
    public static BoardManager              instance; //Singleton pattern    
    public GameObject                       tile; //Tile prefab
    public int                              xSize, ySize; //X and Y dimensions of the board
    public float                            shiftDelay; // Time delay for tile shifting 
    public bool                             wasShifted = false;
    
    public List<Sprite>                     characters = new List<Sprite>(); //Sprites for tiles
    public static GameObject[,]             tiles; //Array for tiles in the board    
    public static List<List<GameObject>>    potentialMatches = new List<List<GameObject>>();

    public static readonly float            OpacityAnimationFrameDelay = .03f;
    public static readonly float            WaitBeforePotentialMatchesCheck = 1f;
    public static readonly float            WaitBeforeCheckNextPotentialMatches = 1f;

    private static IEnumerator              AnimatePotentialMatchesCoroutine;
    private static IEnumerator              CheckPotentialMatchesCoroutine;

    //Tell the game when a match is found and the board is re-filling
    public bool IsShifting { get; set; } //<<<<<?????

	// Use this for initialization
	void Start ()
	{
	    //Sets the singleton with reference of the BoardManager
	    instance = GetComponent<BoardManager>(); 

	    Vector2 offset = tile.GetComponent<SpriteRenderer>().bounds.size;
	    CreateBoard(offset.x, offset.y);
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
        Sprite[] previousLeft = new Sprite[ySize];
        Sprite previousBelow = null;

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                GameObject newTile = Instantiate(tile, new Vector3(startX + (xOffset * x), startY + (yOffset * y), 0), tile.transform.rotation);
                tiles[x,y] = newTile;
                //BoardManager is the parent for all tiles. For keeping Hierarchy clean 
                newTile.transform.parent = transform;

                //Generation of the list of possible characters for this Sprite
                List<Sprite> possibleCharacters = new List<Sprite>();
                possibleCharacters.AddRange(characters);
                possibleCharacters.Remove(previousLeft[y]);
                possibleCharacters.Remove(previousBelow);

                //Set the newly created tile's sprite to the randomly choosen sprite from the possibleCharacters array
                Sprite newSprite = possibleCharacters[Random.Range(0, possibleCharacters.Count)];
                newTile.GetComponent<SpriteRenderer>().sprite = newSprite;

                previousLeft[y] = newSprite;
                previousBelow = newSprite;
            }
        }
        wasShifted = true;
    }

    // Coroutine for FindingNullTiles after matching
    public IEnumerator FindNullTiles() 
    {
        for (int x = 0; x < xSize; x++) {
            for (int y = 0; y < ySize; y++) {
                if (tiles[x, y].GetComponent<SpriteRenderer>().sprite == null) {
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
        IsShifting = true;
        List<SpriteRenderer> renders = new List<SpriteRenderer>();
        int nullCount = 0;

        // Loop through and finds how many spaces it needs to shift downwards
        for (int y = yStart; y < ySize; y++) {
            SpriteRenderer render = tiles[x, y].GetComponent<SpriteRenderer>();
            if (render.sprite == null) {
                nullCount++; // Number of spaces
            }
            renders.Add(render); // Generating list of renders for shifting
        }

        // Shifting
        for (int i = 0; i < nullCount; i++) {
            GUIManager.instance.Score += 50;
            yield return new WaitForSeconds(shiftDelay);

            if (renders.Count == 1) {
                renders[0].sprite = GetNewSprite(x, ySize - 1);
                break;
            }

            for (int k = 0; k < renders.Count-1; k++) {
                renders[k].sprite = renders[k + 1].sprite;
                // Adding new random sprite
                renders[k + 1].sprite = GetNewSprite(x, ySize - 1);
            }           
            //renders[renders.Count - nullCount].sprite = GetNewSprite(x, ySize);
            //Debug.Log(renders.Count);
        }
        IsShifting = false;
        wasShifted = true;
    }

    private Sprite GetNewSprite(int x, int y) {
        List<Sprite> possibleCharacters = new List<Sprite>();
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
        // We dont need to check upper bound of "y" becouse its unreal situation
        // when random sprite provokes match by verticaly when tiles are shifting after matching
        if (y > 0) {
            possibleCharacters.Remove(tiles[x, y - 1].GetComponent<SpriteRenderer>().sprite);
        }

        return possibleCharacters[Random.Range(0, possibleCharacters.Count)];
    }
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

    
    /// /////////////////////////////////////////////

    public void FindPotentialMatches() {
        if (Tile.instance.wasSwapped || BoardManager.instance.wasShifted) {
            potentialMatches.Clear();
            CheckCrossLike();
            CheckHorizontal();            
            CheckVertical();
        }

        // Animate potentialMatches
        StartCheckForPotentialMatches();

        Tile.instance.wasSwapped = false;
        BoardManager.instance.wasShifted = false;

        
        Debug.Log("potentialMatches are:");
        string str = "";
        for (int m = 0; m < potentialMatches.Count; m++) {
            str = "[" + m + "]:";
            foreach (GameObject tile in potentialMatches[m]) {
                str += tile.GetComponent<SpriteRenderer>().sprite.name + "; ";
            }
            Debug.Log(str);
        }                          
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

    private List<GameObject> RemoveNullElements(List<GameObject> adjacentTiles) {
        List<GameObject> tempList = new List<GameObject>();
        for (int k = 0; k < adjacentTiles.Count; k++) {
            if (adjacentTiles[k] != null) {
                tempList.Add(adjacentTiles[k]);
            }
        }
        return tempList;
    }

    private List<GameObject> RemoveUnmatchedElements(List<GameObject> adjacentTiles, GameObject tile) {
        List<GameObject> tempList = new List<GameObject>();
        for (int k = 0; k < adjacentTiles.Count; k++) {
            if (adjacentTiles[k].GetComponent<SpriteRenderer>().sprite ==
                tile.GetComponent<SpriteRenderer>().sprite) {
                    tempList.Add(adjacentTiles[k]);
            }
        }
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

        return tempList;
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
                //Debug.Log("Yep!");
                return true;
            }
            nextPM: ;
        }

        //Debug.Log("Noup");
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
