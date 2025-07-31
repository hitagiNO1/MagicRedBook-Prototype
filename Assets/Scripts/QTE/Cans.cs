using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cans : MonoBehaviour
{
    public int row;
    public int col;
    public shotcan manager;

    public void OnShot()
    {
        manager.HandleCanShot(this.row, this.col);
        Destroy(gameObject);
    }
}
