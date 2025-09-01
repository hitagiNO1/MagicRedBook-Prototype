using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mouseborn : MonoBehaviour
{
    public GameObject mouse;
    public GameObject C4;
    public Transform mouseRoot;
    public List<Holenumber> occupiedHoles = new List<Holenumber>();
    public usercontrol usercontrol;
    [SerializeField] private Sprite spriteA; // 預設圖片（例如地洞）
    [SerializeField] private Sprite spriteB; // 生成時的圖片（例如高亮）
    [SerializeField] private GameObject[] holeObjectArray; // 9 個生成點
    private Dictionary<Holenumber, GameObject> holeObjects = new Dictionary<Holenumber, GameObject>();
    private HashSet<Holenumber> clickedHoles = new HashSet<Holenumber>(); // 新增：追蹤已點擊的位置

    void Start()
    {
        occupiedHoles = new List<Holenumber>();
        clickedHoles = new HashSet<Holenumber>();
        mouseRoot = transform.GetChild(1);
        usercontrol = FindObjectOfType<usercontrol>();
        if (usercontrol == null)
        {
            Debug.LogError("未找到 MouseClickHandler 組件！腳本將禁用。");
            enabled = false;
            return;
        }
        if (spriteA == null || spriteB == null)
        {
            Debug.LogError("圖片 A 或圖片 B 未設置！腳本將禁用。");
            enabled = false;
            return;
        }
        if (holeObjectArray.Length != 9)
        {
            Debug.LogError("必須設置 9 個生成點 GameObject！腳本將禁用。");
            enabled = false;
            return;
        }
        for (int i = 0; i < holeObjectArray.Length; i++)
        {
            if (holeObjectArray[i] == null)
            {
                Debug.LogError($"生成點 {i} 未設置！腳本將禁用。");
                enabled = false;
                return;
            }
            holeObjects[(Holenumber)i] = holeObjectArray[i];
            SpriteRenderer sr = holeObjectArray[i].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = spriteA;
            }
            else
            {
                Debug.LogError($"生成點 {i} 缺少 SpriteRenderer 組件！");
            }
        }
        InvokeRepeating("Create", 0, 1.5f);
        InvokeRepeating("boom", 3f, 3f);
    }

    void Update()
    {
        if (usercontrol != null && !usercontrol.IsGameActive())
        {
            CancelInvoke("Create");
            CancelInvoke("boom");
            enabled = false;
        }
    }

    void OnDisable()
    {
        Debug.Log($"GameCanvas 在 {Time.time} 秒時被停用");
    }

    public void Create()
    {
        if (usercontrol == null || !usercontrol.IsGameActive() || !gameObject.activeInHierarchy)
            return;

        Holenumber newHole = GetUniqueHole();
        if (holeObjects.ContainsKey(newHole))
        {
            SpriteRenderer sr = holeObjects[newHole].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = spriteB;
            }
            else
            {
                Debug.LogWarning($"Holenumber {newHole} 的生成點缺少 SpriteRenderer！");
            }
        }
        GameObject go = Instantiate(mouse);
        go.AddComponent<mouseset>().GetHole = newHole;
        go.GetComponent<mouseset>().GetParent = mouseRoot;
        occupiedHoles.Add(newHole);
        clickedHoles.Remove(newHole); // 確保新生成的位置未被標記為點擊
        StartCoroutine(RemoveHoleAfterDelay(newHole, 2f));
        Destroy(go, 2f);
    }

    public void boom()
    {
        if (usercontrol == null || !usercontrol.IsGameActive() || !gameObject.activeInHierarchy)
            return;

        Holenumber newHole = GetUniqueHole();
        if (holeObjects.ContainsKey(newHole))
        {
            SpriteRenderer sr = holeObjects[newHole].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = spriteB;
            }
            else
            {
                Debug.LogWarning($"Holenumber {newHole} 的生成點缺少 SpriteRenderer！");
            }
        }
        GameObject go = Instantiate(C4);
        go.AddComponent<mouseset>().GetHole = newHole;
        go.GetComponent<mouseset>().GetParent = mouseRoot;
        occupiedHoles.Add(newHole);
        clickedHoles.Remove(newHole); // 確保新生成的位置未被標記為點擊
        StartCoroutine(RemoveHoleAfterDelay(newHole, 5f));
        Destroy(go, 5f);
    }

    public Holenumber GetUniqueHole()
    {
        Holenumber newHole;
        do
        {
            newHole = (Holenumber)Random.Range(0, 9);
        } while (occupiedHoles.Contains(newHole));
        return newHole;
    }

    public IEnumerator RemoveHoleAfterDelay(Holenumber hole, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (occupiedHoles.Contains(hole) && !clickedHoles.Contains(hole))
        {
            occupiedHoles.Remove(hole);
            if (holeObjects.ContainsKey(hole))
            {
                SpriteRenderer sr = holeObjects[hole].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = spriteA;
                }
            }
        }
    }

    // 新增：處理點擊時的圖片切換
    public void OnObjectClicked(Holenumber hole)
    {
        if (holeObjects.ContainsKey(hole))
        {
            SpriteRenderer sr = holeObjects[hole].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = spriteA;
            }
            clickedHoles.Add(hole); // 標記為已點擊
            occupiedHoles.Remove(hole); // 立即移除占用狀態
        }
    }

    // 新增：重啟遊戲
    public void Restart()
    {
        CancelInvoke("Create");
        CancelInvoke("boom");
        occupiedHoles.Clear();
        clickedHoles.Clear();
        foreach (var pair in holeObjects)
        {
            SpriteRenderer sr = pair.Value.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = spriteA;
            }
        }
        InvokeRepeating("Create", 0, 1.5f);
        InvokeRepeating("boom", 3f, 3f);
        enabled = true;
    }
}