using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cans : MonoBehaviour
{
    private bool isShot = false;

    void OnMouseDown()
    {
        if (isShot) return;

        isShot = true;
        FindObjectOfType<shotcan>().OnCanShot();

        // 可以加動畫或物理效果
        Destroy(gameObject);
    }
}
