using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 宣告一個資料結構來存放一列 CSV 的資料
public struct DefenseData
{
    public float Defense;
    public float Reduction;
}

public class DefenseCSVReader : MonoBehaviour
{
    // 單例模式：讓其他腳本不用 Find 就能直接呼叫它
    public static DefenseCSVReader Instance { get; private set; }

    [Header("請在此放入你的防禦力 CSV 檔案")]
    public TextAsset csvFile;

    // 存放解析完畢的表格資料
    private List<DefenseData> defenseTable = new List<DefenseData>();

    private void Awake()
    {
        // 單例模式初始化
        if (Instance == null)
        {
            Instance = this;
            LoadCSV();
            // DontDestroyOnLoad(gameObject); // 如果你需要跨場景保留這個讀取器，可以解除這行的註解
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadCSV()
    {
        if (csvFile == null)
        {
            Debug.LogError("DefenseCSVReader: 沒有放入 CSV 檔案！");
            return;
        }

        // 根據換行符號切割字串 (\n 或 \r\n 都能兼容)
        string[] lines = csvFile.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 假設第一行是標題 (Defense, Reduction)，所以從 i = 1 開始跑迴圈
        for (int i = 1; i < lines.Length; i++)
        {
            string[] columns = lines[i].Split(',');

            if (columns.Length >= 2)
            {
                // 清除空白，並將第二欄位的 '%' 符號替換為空白字串
                string defString = columns[0].Trim();
                string redString = columns[1].Trim().Replace("%", "");

                // 將字串轉換為浮點數
                if (float.TryParse(defString, out float def) && float.TryParse(redString, out float red))
                {
                    // 將讀取到的數值 (如 29.58) 除以 100，轉換為小數 (0.2958) 存入記憶體
                    defenseTable.Add(new DefenseData { Defense = def, Reduction = red / 100f });
                }
            }
        }

        // 為了確保查詢邏輯正確，強制將資料依照防禦力由小到大排序
        defenseTable = defenseTable.OrderBy(x => x.Defense).ToList();
        Debug.Log($"成功讀取防禦力 CSV，共載入 {defenseTable.Count} 筆資料。");
    }

    /// <summary>
    /// 傳入當前防禦力，查表回傳減傷比例
    /// </summary>
    public float GetDamageReduction(float currentDefense)
    {
        if (defenseTable.Count == 0) return 0f;

        // 狀況 1：防禦力比表單上的最小值還低
        if (currentDefense <= defenseTable[0].Defense)
            return defenseTable[0].Reduction;

        // 狀況 2：防禦力比表單上的最大值還高 (封頂)
        if (currentDefense >= defenseTable[defenseTable.Count - 1].Defense)
            return defenseTable[defenseTable.Count - 1].Reduction;

        // 狀況 3：尋找防禦力所在的區間，並計算平滑的減傷值
        for (int i = 0; i < defenseTable.Count - 1; i++)
        {
            if (currentDefense >= defenseTable[i].Defense && currentDefense <= defenseTable[i + 1].Defense)
            {
                // 使用 Lerp (線性內插) 算出精確的百分比
                // 例如表單是 10防=0.1減傷, 20防=0.2減傷，如果當前防禦是 15，則會精確回傳 0.15
                float t = (currentDefense - defenseTable[i].Defense) / (defenseTable[i + 1].Defense - defenseTable[i].Defense);
                return Mathf.Lerp(defenseTable[i].Reduction, defenseTable[i + 1].Reduction, t);
            }
        }

        return 0f;
    }
}