using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
  

    public bool isBlack = false;
    public int col;                      // 欄位：0, 1, 2
    public canclick canclick;           // 控制腳本


    void Update()
    {

        if (canclick == null || canclick.gameEnded) return;

        // 1. 斜下落的基礎方向（x 軸傾斜 30 度）
        Vector3 baseFallDir = Quaternion.Euler(30f, 0f, 0f) * Vector3.down;

        // 2. 根據 col 計算外擴角度，這邊加上「-」反轉，讓他往外擴而非聚攏
        int relativeIndex = col - 1; // col: 0 → -1, 1 → 0, 2 → +1
        float angleY = -relativeIndex * canclick.columnAngle; // ← 加上負號
        Quaternion yRotation = Quaternion.Euler(0f, angleY, 0f);
        Vector3 finalDirection = yRotation * baseFallDir;

        // 3. 平移
        transform.Translate(finalDirection.normalized * canclick.fallSpeed * Time.deltaTime, Space.World);

        // 4. 近大遠小縮放
        float scale = Mathf.Lerp(0.1f, 0.2f, Mathf.InverseLerp(7f, -4f, transform.position.z));
        transform.localScale = new Vector3(scale, scale, scale);
    }


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
