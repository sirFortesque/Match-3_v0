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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour {
	private static Color    selectedColor = new Color(.5f, .5f, .5f, 1.0f);
	private static Tile     previousSelected = null;
	private SpriteRenderer  render;
	private bool            isSelected = false;
	private Vector2[]       adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    private bool            matchFound = false;

	void Awake() {
		render = GetComponent<SpriteRenderer>();
    }

	private void Select() {
		isSelected = true;
		render.color = selectedColor;
		previousSelected = gameObject.GetComponent<Tile>();
		SFXManager.instance.PlaySFX(Clip.Select);
	}

	private void Deselect() {
		isSelected = false;
		render.color = Color.white;
		previousSelected = null;
	}

    // Tile selecting
    void OnMouseDown()
    {
        // Don't select tile when it's empty or when the game ends
        if (render.sprite == null || BoardManager.instance.IsShifting)
        {
            return;
        }

        if (isSelected)
        {
            Deselect();
        }
        else
        {   // Is it the first tile selected?
            if (previousSelected == null)
            {
                Select();
            }
            // If it wasn't the first one that was selected...
            else
            {   // ...and surrounding tiles .Contains previousSelected tile
                // then swap tiles
                // and .ClearAllMatches if there are any
                if(GetAllAdjacentTiles().Contains(previousSelected.gameObject))
                {                    
                    SwapSprite(previousSelected.render);
                    previousSelected.ClearAllMatchess();
                    previousSelected.Deselect();
                    ClearAllMatchess();
                }
                else
                {   // The tile isn't next to the previously selected one,
                    // deselect the previous one and select the newly selected
                    previousSelected.GetComponent<Tile>().Deselect();// <<<<<<<????? зачем GetComponent если раньше deselect без него делали
                    Select();
                }
                
            }
        }
    }

    public void SwapSprite(SpriteRenderer render2)
    {
        // If sprites are the same, do nothing, 
        // as swapping two identical sprites would't make much sense
        if (render.sprite == render2.sprite)
        {
            return;
        }
        Sprite tempSprite = render2.sprite;
        render2.sprite = render.sprite;
        render.sprite = tempSprite;
        SFXManager.instance.PlaySFX(Clip.Swap);
        GUIManager.instance.MoveCounter--;
    }

    // Method for retrieving single adjacent tile
    private GameObject GetAdjacent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);

        if (hit.collider != null)
        {            
            return hit.collider.gameObject;
        }        
        return null;
    }

    // Method for retrieving a list of tiles surrounding the current tile
    private List<GameObject> GetAllAdjacentTiles()
    {
        List<GameObject> adjacentTiles = new List<GameObject>();
        for (int i = 0; i < adjacentDirections.Length; i++)
        {
            adjacentTiles.Add(GetAdjacent(adjacentDirections[i]));            
        }        
        return adjacentTiles;
        
    }

    // Method for finding matches accepts a Vector2 as a parameter,
    // which be the dirrection all raycasts will be fired in
    private List<GameObject> FindMatch(Vector2 castDir)
    {
        List<GameObject> matchingTiles = new List<GameObject>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);

        // Keep firing new raycasts until either your raycast hits nothing,
        // or the tiles sprite differs from the hit object sprite
        while (hit.collider != null && hit.collider.GetComponent<SpriteRenderer>().sprite == render.sprite)
        {
            // If both conditions are met, you consider it a match 
            // and add it to your list
            matchingTiles.Add(hit.collider.gameObject);
            // Check next tile
            hit = Physics2D.Raycast(hit.collider.transform.position, castDir);
        }
        return matchingTiles;
    }

    // Method finds all the matching tiles along the given paths,
    // and then clears the matches respectively
    private void ClearMatch(Vector2[] paths)
    {
        List<GameObject> matchingTiles = new List<GameObject>();

        // ...finds
        for (int i = 0; i < paths.Length; i++)
        {
            matchingTiles.AddRange(FindMatch(paths[i]));
        }

        // ...clears
        if (matchingTiles.Count >= 2) // Third match is the initial tile
        {
            for (int i = 0; i < matchingTiles.Count; i++)
            {
                matchingTiles[i].GetComponent<SpriteRenderer>().sprite = null;
            }
            matchFound = true;
        }
    }

    public void ClearAllMatchess()
    {
        if (render.sprite == null)
            return;

        ClearMatch(new Vector2[2] {Vector2.left, Vector2.right});   // horizontal
        ClearMatch(new Vector2[2] {Vector2.up, Vector2.down});      // vertical

        if (matchFound)
        {
            render.sprite = null;
            matchFound = false;
            // Stop Coroutine if it was already runing 
            // and start it again
            StopCoroutine(BoardManager.instance.FindNullTiles());  // <<<<????? зачем ее останавливать          
            // Shifting and re-filling
            StartCoroutine(BoardManager.instance.FindNullTiles()); 
            SFXManager.instance.PlaySFX(Clip.Clear);
        }
    }
}