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
    private Dictionary<Holenumber, GameObject> spawnedObjects = new Dictionary<Holenumber, GameObject>(); // 新增：追蹤生成的物件
    private HashSet<Holenumber> clickedHoles = new HashSet<Holenumber>(); // 追蹤已點擊的位置

    // 搖晃動畫的參數
    [SerializeField] private float shakeDuration = 0.5f; // 保持不變
    [SerializeField] private float shakeMagnitude = 0.1f; // 增加幅度
    [SerializeField] private float shakeSpeed = 15f; // 加快速度

    void Start()
    {
        occupiedHoles = new List<Holenumber>();
        clickedHoles = new HashSet<Holenumber>();
        spawnedObjects = new Dictionary<Holenumber, GameObject>(); // 初始化
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
            StartCoroutine(ShakeAndSpawn(newHole, mouse, 2f)); // 啟動搖晃並生成 mouse
        }
    }

    public void boom()
    {
        if (usercontrol == null || !usercontrol.IsGameActive() || !gameObject.activeInHierarchy)
            return;

        Holenumber newHole = GetUniqueHole();
        if (holeObjects.ContainsKey(newHole))
        {
            StartCoroutine(ShakeAndSpawn(newHole, C4, 5f)); // 啟動搖晃並生成 C4
        }
    }

    private IEnumerator ShakeAndSpawn(Holenumber hole, GameObject prefab, float destroyDelay)
    {
        if (holeObjects.ContainsKey(hole))
        {
            GameObject holeObject = holeObjects[hole];
            SpriteRenderer sr = holeObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                yield return StartCoroutine(Shake(holeObject.transform));
                sr.sprite = spriteB;
            }
            else
            {
                Debug.LogWarning($"Holenumber {hole} 的生成點缺少 SpriteRenderer！");
            }

            GameObject go = Instantiate(prefab);
            go.AddComponent<mouseset>().GetHole = hole;
            go.GetComponent<mouseset>().GetParent = mouseRoot;
            spawnedObjects[hole] = go; // 儲存生成的物件
            occupiedHoles.Add(hole);
            clickedHoles.Remove(hole);
            StartCoroutine(RemoveHoleAfterDelay(hole, destroyDelay));
            Destroy(go, destroyDelay);
        }
    }

    private IEnumerator Shake(Transform target)
    {
        Vector3 originalPosition = target.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float xOffset = Mathf.Sin(Time.time * shakeSpeed) * shakeMagnitude;
            target.localPosition = originalPosition + new Vector3(xOffset, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPosition;
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
            if (spawnedObjects.ContainsKey(hole))
            {
                Destroy(spawnedObjects[hole]); // 確保物件被銷毀
                spawnedObjects.Remove(hole);
            }
            if (holeObjects.ContainsKey(hole))
            {
                SpriteRenderer sr = holeObjects[hole].GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != spriteA)
                {
                    sr.sprite = spriteA;
                }
            }
        }
    }

    public void OnObjectClicked(Holenumber hole)
    {
        if (holeObjects.ContainsKey(hole))
        {
            if (spawnedObjects.ContainsKey(hole))
            {
                Destroy(spawnedObjects[hole]); // 立即銷毀物件
                spawnedObjects.Remove(hole);
            }

            SpriteRenderer sr = holeObjects[hole].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = spriteA;
            }
            clickedHoles.Add(hole);
            occupiedHoles.Remove(hole);
        }
    }

    public void Restart()
    {
        CancelInvoke("Create");
        CancelInvoke("boom");
        occupiedHoles.Clear();
        clickedHoles.Clear();
        spawnedObjects.Clear(); // 清空生成的物件
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