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

    public static BoardManager  instance; //Singleton pattern
    public List<Sprite>         characters = new List<Sprite>(); //Sprites for tiles
    public GameObject           tile; //Tile prefab
    public int                  xSize, ySize; //X and Y dimensions of the board
    public float                shiftDelay; // Time delay for tile shifting

    //Tell the game when a match is found and the board is re-filling
    public bool                 IsShifting { get; set; } //<<<<<????? Зачем гет сет? почему нельзая просто бул

    private GameObject[,] tiles; //Array for tiles in the board

	// Use this for initialization
	void Start ()
	{
	    //Sets the singleton with reference of the BoardManager
	    instance = GetComponent<BoardManager>(); 

	    Vector2 offset = tile.GetComponent<SpriteRenderer>().bounds.size;
	    CreateBoard(offset.x, offset.y);
	}

    private void CreateBoard (float xOffset, float yOffset)
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
            for (int k = 0; k < renders.Count-1; k++) {
                renders[k].sprite = renders[k + 1].sprite;
                // Adding new random sprite
                renders[k + 1].sprite = GetNewSprite(x, ySize - 1);
            }
            //renders[renders.Count - nullCount].sprite = GetNewSprite(x, ySize);
            //Debug.Log(renders.Count);
        }
        IsShifting = false;
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

	// Update is called once per frame
	void Update () {
		
	}
}
