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

public class BoardManager0 : MonoBehaviour {

    public static BoardManager  instance; //Singleton pattern
    public List<Sprite>         characters = new List<Sprite>(); //Sprites for tiles
    public GameObject           tile; //Tile prefab
    public int                  xSize, ySize; //X and Y dimensions of the board
    private GameObject[,]       tiles; //Array for tiles in the board
    public bool                 IsShifting { get; set; } //Tell the game when a match is found and the board is re-filling


	// Use this for initialization
	void Start ()
	{
	    instance = GetComponent<BoardManager>(); //Sets the singleton with reference of the BoardManager

	    Vector2 offset = tile.GetComponent<SpriteRenderer>().bounds.size;
	    CreateBoard(offset.x, offset.y);
	}

    private void CreateBoard (float xOffset, float yOffset)
    {
        tiles = new GameObject[xSize, ySize];

        float startX = transform.position.x; //Starting position for the board generation
        float startY = transform.position.y;

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                GameObject newTile = Instantiate(tile, new Vector3(startX + (xOffset * x), startY + (yOffset * y), 0), tile.transform.rotation);
                tiles[x, y] = newTile;
            }
        }        
    }

	// Update is called once per frame
	void Update () {
		
	}
}
