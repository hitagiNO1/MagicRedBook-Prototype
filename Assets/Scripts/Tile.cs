using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public bool isBlack;
    public int col; // 0 左, 1 中, 2 右
    public canclick canclick;

    public void OnClicked()
    {
        if (canclick.gameEnded) return;

        if (isBlack)
            canclick.OnCorrectTileClicked(this);
        else
            canclick.OnWrongTileClicked(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("EndLine") && isBlack)
        {
            if (!canclick.gameEnded)
            {
                canclick.GameOver("你漏點了黑塊！");
            }
        }
    }
}
