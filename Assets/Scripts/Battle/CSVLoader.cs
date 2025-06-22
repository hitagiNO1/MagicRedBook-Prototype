using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CSVLoader : MonoBehaviour
{
    public TextAsset csvFile; // 在 Inspector 拖曳 CSV 檔案
    public DefenseData defenseData; // 連結 ScriptableObject

    void Start()
    {
        LoadCSV();
    }

    void LoadCSV()
    {
        if (csvFile == null || defenseData == null) return;

        defenseData.defenseEntries.Clear(); // 清空原有數據
        string[] lines = csvFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // 跳過標題列
        {
            string[] values = lines[i].Split(',');

            if (values.Length < 2) continue;

            if (int.TryParse(values[0], out int defense))
            {
                // 轉換百分比為浮點數
                string percentageString = values[1].Trim().Replace("%", ""); // 去掉 %
                if (float.TryParse(percentageString, out float reduction))
                {
                    reduction /= 100f; // 轉換為 0~1 之間的數值

                    defenseData.defenseEntries.Add(new DefenseData.DefenseEntry
                    {
                        defenseValue = defense,
                        damageReduction = reduction
                    });
                }
            }
        }

        Debug.Log("CSV 加載完成，總共載入：" + defenseData.defenseEntries.Count + " 筆數據");
    }

}