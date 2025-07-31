using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class locationstore : MonoBehaviour
{
    public static locationstore Instance;

    public Vector3 lastPlayerPosition;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 不要在切換場景時被銷毀
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SavePlayerPosition(Vector3 position)
    {
        lastPlayerPosition = position;
    }
}
