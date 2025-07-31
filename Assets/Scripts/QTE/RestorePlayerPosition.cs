using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestorePlayerPosition : MonoBehaviour
{
    void Start()
    {
        // 只有當 locationstore 存有有效位置時才移動
        if (locationstore.Instance != null && locationstore.Instance.lastPlayerPosition != Vector3.zero)
        {
            transform.position = locationstore.Instance.lastPlayerPosition;
        }
    }
}
