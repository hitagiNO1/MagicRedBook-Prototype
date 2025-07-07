using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public bool isBlack;
    public int col; // 0 ¥ª, 1 ¤¤, 2 ¥k
    public canclick canclick;

    public void OnClicked()
    {
        if (isBlack)
            canclick.OnCorrectTileClicked(this);
        else
            canclick.OnWrongTileClicked(this);
    }
}
