using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mouseborn : MonoBehaviour
{
    public Transform mouseRoot;
    public usercontrol usercontrol;
    [SerializeField] private Sprite spriteA; // 預設圖片（地洞）
    [SerializeField] private Sprite spriteB; // 老鼠圖片
    [SerializeField] private Sprite spriteC4_1; // C4 第一種圖片
    [SerializeField] private Sprite spriteC4_2; // C4 第二種圖片
    [SerializeField] private Sprite spriteB_hover; // 老鼠 Hover 圖片
    [SerializeField] private Sprite spriteC4_1_hover; // C4_1 Hover 圖片
    [SerializeField] private Sprite spriteC4_2_hover; // C4_2 Hover 圖片
    [SerializeField] private GameObject[] holeObjectArray; // 9 個生成點
    private Dictionary<Holenumber, GameObject> holeObjects = new Dictionary<Holenumber, GameObject>();
    private Dictionary<Holenumber, Sprite> spawnedSprites = new Dictionary<Holenumber, Sprite>(); // 追蹤生成的 Sprite
    private Dictionary<Holenumber, Sprite> hoverSprites = new Dictionary<Holenumber, Sprite>(); // 追蹤 Hover Sprite
    private List<Holenumber> occupiedHoles = new List<Holenumber>();
    private HashSet<Holenumber> clickedHoles = new HashSet<Holenumber>(); // 追蹤已點擊的位置

    // 搖晃動畫的參數
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitude = 0.1f;
    [SerializeField] private float shakeSpeed = 15f;

    void Start()
    {
        occupiedHoles = new List<Holenumber>();
        clickedHoles = new HashSet<Holenumber>();
        spawnedSprites = new Dictionary<Holenumber, Sprite>();
        hoverSprites = new Dictionary<Holenumber, Sprite>();
        mouseRoot = transform.GetChild(1);
        usercontrol = FindObjectOfType<usercontrol>();

        if (usercontrol == null)
        {
            Debug.LogError("未找到 usercontrol 組件！腳本將禁用。");
            enabled = false;
            return;
        }
        if (spriteA == null || spriteB == null || spriteC4_1 == null || spriteC4_2 == null ||
            spriteB_hover == null || spriteC4_1_hover == null || spriteC4_2_hover == null)
        {
            Debug.LogError("圖片 spriteA, spriteB, spriteC4_1, spriteC4_2, spriteB_hover, spriteC4_1_hover 或 spriteC4_2_hover 未設置！腳本將禁用。");
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
            // 確保每個地洞有 BoxCollider2D，但預設禁用
            BoxCollider2D collider = holeObjectArray[i].GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = holeObjectArray[i].AddComponent<BoxCollider2D>();
            }
            collider.enabled = false;
        }

        InvokeRepeating("Create", 0, 1.5f);
        InvokeRepeating("Boom", 3f, 3f);
    }

    void Update()
    {
        if (usercontrol != null && !usercontrol.IsGameActive())
        {
            CancelInvoke("Create");
            CancelInvoke("Boom");
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
            StartCoroutine(ShakeAndSpawn(newHole, spriteB, true, 2f)); // 生成老鼠
        }
    }

    public void Boom()
    {
        if (usercontrol == null || !usercontrol.IsGameActive() || !gameObject.activeInHierarchy)
            return;

        Holenumber newHole = GetUniqueHole();
        if (holeObjects.ContainsKey(newHole))
        {
            Sprite c4Sprite = Random.value > 0.5f ? spriteC4_1 : spriteC4_2;
            StartCoroutine(ShakeAndSpawn(newHole, c4Sprite, false, 5f)); // 生成 C4
        }
    }

    private IEnumerator ShakeAndSpawn(Holenumber hole, Sprite targetSprite, bool isMouse, float destroyDelay)
    {
        if (holeObjects.ContainsKey(hole))
        {
            GameObject holeObject = holeObjects[hole];
            SpriteRenderer sr = holeObject.GetComponent<SpriteRenderer>();
            BoxCollider2D collider = holeObject.GetComponent<BoxCollider2D>();
            if (sr != null && collider != null)
            {
                yield return StartCoroutine(Shake(holeObject.transform));
                sr.sprite = targetSprite;
                spawnedSprites[hole] = targetSprite;

                // 設置 Hover Sprite
                Sprite hoverSprite = isMouse ? spriteB_hover : (targetSprite == spriteC4_1 ? spriteC4_1_hover : spriteC4_2_hover);
                hoverSprites[hole] = hoverSprite;

                collider.enabled = true; // 啟用碰撞器
                holeObject.tag = isMouse ? "mouse" : "C4"; // 設置標籤
                HoleState state = holeObject.GetComponent<HoleState>();
                if (state == null)
                {
                    state = holeObject.AddComponent<HoleState>();
                }
                state.Initialize(hole, mouseRoot, isMouse, this);
                occupiedHoles.Add(hole);
                clickedHoles.Remove(hole);
                StartCoroutine(RemoveHoleAfterDelay(hole, destroyDelay));
            }
            else
            {
                Debug.LogWarning($"Holenumber {hole} 的生成點缺少 SpriteRenderer 或 BoxCollider2D！");
            }
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
            ResetHole(hole);
        }
    }

    public void OnObjectClicked(Holenumber hole)
    {
        if (holeObjects.ContainsKey(hole))
        {
            ResetHole(hole);
            clickedHoles.Add(hole);
            occupiedHoles.Remove(hole);
        }
    }

    private void ResetHole(Holenumber hole)
    {
        if (holeObjects.ContainsKey(hole))
        {
            GameObject holeObject = holeObjects[hole];
            SpriteRenderer sr = holeObject.GetComponent<SpriteRenderer>();
            BoxCollider2D collider = holeObject.GetComponent<BoxCollider2D>();
            HoleState state = holeObject.GetComponent<HoleState>();
            if (sr != null)
            {
                sr.sprite = spriteA;
            }
            if (collider != null)
            {
                collider.enabled = false;
            }
            if (state != null)
            {
                Destroy(state);
            }
            holeObject.tag = "Untagged"; // 移除標籤
            spawnedSprites.Remove(hole);
            hoverSprites.Remove(hole); // 清除 Hover 記錄
        }
    }

    public Sprite GetHoverSprite(Holenumber hole)
    {
        return hoverSprites.ContainsKey(hole) ? hoverSprites[hole] : null;
    }

    public void Restart()
    {
        CancelInvoke("Create");
        CancelInvoke("Boom");
        occupiedHoles.Clear();
        clickedHoles.Clear();
        spawnedSprites.Clear();
        hoverSprites.Clear();
        foreach (var pair in holeObjects)
        {
            ResetHole(pair.Key);
        }
        InvokeRepeating("Create", 0, 1.5f);
        InvokeRepeating("Boom", 3f, 3f);
        enabled = true;
    }
}