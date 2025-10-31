using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Holenumber
{
    H0, H1, H2, H3, H4, H5, H6, H7, H8,
    Invalid = -1
}

public class mouseborn : MonoBehaviour
{
    public Transform mouseRoot;
    public usercontrol usercontrol;
    [SerializeField] private Sprite spriteA;
    [SerializeField] private Sprite spriteB;
    [SerializeField] private Sprite spriteC4_1;
    [SerializeField] private Sprite spriteC4_2;
    [SerializeField] private Sprite spriteB_hover;
    [SerializeField] private Sprite spriteC4_1_hover;
    [SerializeField] private Sprite spriteC4_2_hover;
    [SerializeField] private GameObject[] holeObjectArray = new GameObject[9];

    private readonly Dictionary<Holenumber, GameObject> holeObjects = new Dictionary<Holenumber, GameObject>();
    private readonly Dictionary<Holenumber, Image> holeImages = new Dictionary<Holenumber, Image>();
    private readonly Dictionary<Holenumber, Sprite> spawnedSprites = new Dictionary<Holenumber, Sprite>();
    private readonly Dictionary<Holenumber, Sprite> hoverSprites = new Dictionary<Holenumber, Sprite>();
    private readonly List<Holenumber> occupiedHoles = new List<Holenumber>();
    private readonly HashSet<Holenumber> clickedHoles = new HashSet<Holenumber>();

    //  新增：每個洞的「世代代號」記錄，用來防止舊 coroutine 誤重置
    private readonly Dictionary<Holenumber, int> holeGeneration = new Dictionary<Holenumber, int>();
    private int globalGeneration = 0;

    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitude = 0.1f;
    [SerializeField] private float shakeSpeed = 15f;

    public System.Action<HoleState> OnHoleSpawned;
    public System.Action<HoleState> OnHoleRemoved;

    private bool isQuitting = false;

    void Awake()
    {
        if (mouseRoot != null) return;
        mouseRoot = transform.GetChild(1);
    }

    void Start()
    {
        isQuitting = false;
        usercontrol = FindObjectOfType<usercontrol>();
        if (usercontrol == null)
        {
            Debug.LogError("找不到 usercontrol！");
            enabled = false;
            return;
        }
        if (!ValidateSprites()) { enabled = false; return; }
        if (!ValidateHoleArray()) { enabled = false; return; }
        InitializeHoles();
        ScheduleSpawns();
    }

    void OnEnable() => ScheduleSpawns();
    void OnDisable() => CancelAllInvokesAndCoroutines();
    void OnDestroy()
    {
        isQuitting = true;
        CancelAllInvokesAndCoroutines();
        NotifyAllHoleStatesQuitting();
    }

    private bool ValidateSprites()
    {
        if (spriteA == null || spriteB == null || spriteC4_1 == null || spriteC4_2 == null ||
            spriteB_hover == null || spriteC4_1_hover == null || spriteC4_2_hover == null)
        {
            Debug.LogError("有 Sprite 未設定！");
            return false;
        }
        return true;
    }

    private bool ValidateHoleArray()
    {
        if (holeObjectArray == null || holeObjectArray.Length != 9)
        {
            Debug.LogError("holeObjectArray 必須為 9 個！");
            return false;
        }
        for (int i = 0; i < 9; i++)
            if (holeObjectArray[i] == null)
            {
                Debug.LogError($"holeObjectArray[{i}] 為 null！");
                return false;
            }
        return true;
    }

    private void InitializeHoles()
    {
        for (int i = 0; i < 9; i++)
        {
            GameObject go = holeObjectArray[i];
            Holenumber hole = (Holenumber)i;
            holeObjects[hole] = go;
            Image img = go.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = spriteA;
                holeImages[hole] = img;
            }
            else
            {
                Debug.LogError($"Hole {i} 沒有 Image！");
                continue;
            }
            var col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
            col.enabled = false;

            //  初始化世代代號
            holeGeneration[hole] = 0;
        }
    }

    private void ScheduleSpawns()
    {
        CancelInvoke();
        InvokeRepeating(nameof(Create), 0f, 1.5f);
        InvokeRepeating(nameof(Boom), 3f, 3f);
    }

    private void CancelAllInvokesAndCoroutines()
    {
        CancelInvoke();
        StopAllCoroutines();
    }

    void Update()
    {
        if (usercontrol != null && !usercontrol.IsGameActive())
        {
            CancelAllInvokesAndCoroutines();
            // enabled = false; // 註解掉，避免腳本完全停用
        }
    }

    public void Create()
    {
        if (!CanSpawn()) return;
        Holenumber hole = GetUniqueHole();
        if (hole == Holenumber.Invalid || !holeObjects.ContainsKey(hole)) return;
        StartCoroutineSafe(ShakeAndSpawn(hole, spriteB, true, 3f)); // 老鼠 3 秒
    }

    public void Boom()
    {
        if (!CanSpawn()) return;
        Holenumber hole = GetUniqueHole();
        if (hole == Holenumber.Invalid || !holeObjects.ContainsKey(hole)) return;
        Sprite c4 = Random.value > 0.5f ? spriteC4_1 : spriteC4_2;
        StartCoroutineSafe(ShakeAndSpawn(hole, c4, false, 4f)); // C4 4 秒
    }

    private bool CanSpawn()
    {
        return usercontrol != null
            && usercontrol.IsGameActive()
            && gameObject.activeInHierarchy
            && !isQuitting
            && occupiedHoles.Count < 8; // 留 1 個空位，防臨界滿
    }

    private IEnumerator ShakeAndSpawn(Holenumber hole, Sprite target, bool isMouse, float delay)
    {
        if (isQuitting || !holeObjects.TryGetValue(hole, out GameObject go) || go == null) yield break;
        if (!holeImages.TryGetValue(hole, out Image img) || img == null) yield break;

        //  每次生成前，更新這個洞的世代代號
        globalGeneration++;
        int currentGen = globalGeneration;
        holeGeneration[hole] = currentGen;

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null) yield return Shake(rt);

        img.sprite = target;
        spawnedSprites[hole] = target;
        Sprite hover = isMouse ? spriteB_hover :
            (target == spriteC4_1 ? spriteC4_1_hover : spriteC4_2_hover);
        hoverSprites[hole] = hover;

        var col = go.GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = true;
        go.tag = isMouse ? "mouse" : "C4";

        HoleState state = go.GetComponent<HoleState>() ?? go.AddComponent<HoleState>();
        state.Initialize(hole, mouseRoot, isMouse, this);

        occupiedHoles.Add(hole);
        clickedHoles.Remove(hole);
        OnHoleSpawned?.Invoke(state);

        yield return new WaitForSeconds(delay);

        //防止舊 coroutine 誤 Reset 新生成的物件
        if (holeGeneration.TryGetValue(hole, out int gen) && gen == currentGen)
        {
            if (occupiedHoles.Contains(hole) && !clickedHoles.Contains(hole))
            {
                ResetHole(hole);
            }
        }
    }

    private IEnumerator Shake(RectTransform rt)
    {
        Vector2 orig = rt.anchoredPosition;
        float t = 0f;
        while (t < shakeDuration)
        {
            if (rt == null) yield break;
            float x = Mathf.Sin(t * shakeSpeed) * shakeMagnitude;
            rt.anchoredPosition = orig + new Vector2(x, 0f);
            t += Time.deltaTime;
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = orig;
    }

    public Holenumber GetUniqueHole()
    {
        if (occupiedHoles.Count >= 8) return Holenumber.Invalid; // 提前防滿

        int attempts = 0;
        Holenumber h;
        do
        {
            h = (Holenumber)Random.Range(0, 9);
            if (++attempts > 30) return Holenumber.Invalid;
        }
        while (occupiedHoles.Contains(h));

        return h;
    }

    public void OnObjectClicked(Holenumber hole)
    {
        if (!holeObjects.ContainsKey(hole)) return;
        ResetHole(hole);
        clickedHoles.Add(hole);
        occupiedHoles.Remove(hole);
    }

    private void ResetHole(Holenumber hole)
    {
        if (!holeObjects.TryGetValue(hole, out GameObject go) || go == null) return;
        if (!holeImages.TryGetValue(hole, out Image img)) return;

        HoleState state = go.GetComponent<HoleState>();
        if (state != null) OnHoleRemoved?.Invoke(state);

        img.sprite = spriteA;
        var col = go.GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = false;
        if (state != null) Destroy(state);
        go.tag = "Untagged";

        spawnedSprites.Remove(hole);
        hoverSprites.Remove(hole);
        occupiedHoles.Remove(hole);

        // 清除該洞的世代紀錄
        holeGeneration[hole] = 0;
    }

    private void NotifyAllHoleStatesQuitting()
    {
        foreach (var pair in holeObjects)
        {
            if (pair.Value == null) continue;
            HoleState state = pair.Value.GetComponent<HoleState>();
            if (state != null)
            {
                var field = typeof(HoleState).GetField("isQuitting",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(state, true);
            }
        }
    }

    public Sprite GetSpawnedSprite(Holenumber hole) => spawnedSprites.TryGetValue(hole, out var s) ? s : null;
    public Sprite GetHoverSprite(Holenumber hole) => hoverSprites.TryGetValue(hole, out var s) ? s : null;

    private void StartCoroutineSafe(IEnumerator routine)
    {
        if (!isQuitting && gameObject.activeInHierarchy)
            StartCoroutine(routine);
    }

    public void Restart()
    {
        CancelAllInvokesAndCoroutines();
        occupiedHoles.Clear();
        clickedHoles.Clear();
        spawnedSprites.Clear();
        hoverSprites.Clear();
        foreach (var pair in holeObjects)
            ResetHole(pair.Key);
        ScheduleSpawns();
        enabled = true;
    }
}
