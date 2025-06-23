using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cans : MonoBehaviour
{
    void OnMouseDown()
    {
        FindObjectOfType<shotcan>().OnCansShot();
        Destroy(gameObject); // 播動畫替代也可
    }
}
