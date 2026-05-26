using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Holenumber
{
    H0, H1, H2, H3, H4, H5, H6, H7, H8,
    Invalid = -1
}

public class mouseborn : MonoBehaviour
{
    public Transform mouseRoot;
    public usercontrol usercontrol;

    [Header("Sprites")]
    [SerializeField] private Sprite spriteA;           // 空洞
    [SerializeField] private Sprite spriteB;           // 老鼠
    [SerializeField] private Sprite spriteC4_1;        // C4 類型1
    [SerializeField] private Sprite spriteC4_2;        // C4 類型2
    [SerializeField] private Sprite spriteB_hover;     // 老鼠 hover
    [SerializeField] private Sprite spriteC4_1_hover;  // C4 hover 1
    [SerializeField] private Sprite spriteC4_2_hover;  // C4 hover 2

    [Header("洞物件 (共9個)")]
    [SerializeField] private GameObject[] holeObjectArray = new GameObject[9];

    [Header("生成與持續時間設定")]
    [SerializeField, Tooltip("老鼠存在時間 (秒)")]
    private float mouseExistTime = 3f;
    [SerializeField, Tooltip("C4存在時間 (秒)")]
    private float c4ExistTime = 4f;
    [SerializeField, Tooltip("老鼠生成間隔 (秒)")]
    private float mouseSpawnInterval = 1.5f;
    [SerializeField, Tooltip("C4生成間隔 (秒)")]
    private float c4SpawnInterval = 3f;

    [Header("震動設定")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeMagnitude = 0.1f;
    [SerializeField] private float shakeSpeed = 15f;

    private readonly Dictionary<Holenumber, GameObject> holeObjects = new Dictionary<Holenumber, GameObject>();
    private readonly Dictionary<Holenumber, SpriteRenderer> holeRenderers = new Dictionary<Holenumber, SpriteRenderer>();
    private readonly Dictionary<Holenumber, Sprite> spawnedSprites = new Dictionary<Holenumber, Sprite>();
    private readonly Dictionary<Holenumber, Sprite> hoverSprites = new Dictionary<Holenumber, Sprite>();
    private readonly List<Holenumber> occupiedHoles = new List<Holenumber>();
    private readonly HashSet<Holenumber> clickedHoles = new HashSet<Holenumber>();
    private readonly Dictionary<Holenumber, int> holeGeneration = new Dictionary<Holenumber, int>();
    private readonly Dictionary<Holenumber, Vector3> initialHolePositions = new Dictionary<Holenumber, Vector3>();

    private int globalGeneration = 0;
    private bool isInitialized = false;
    public System.Action<HoleState> OnHoleSpawned;
    public System.Action<HoleState> OnHoleRemoved;

    private bool isQuitting = false;

    void Awake()
    {
        if (mouseRoot == null)
        {
            mouseRoot = transform.GetChild(1);
        }

        // ★ 提早到 Awake 執行，確保在畫面第一次渲染前，所有洞都已經是 spriteA
        if (holeObjectArray != null && holeObjectArray.Length == 9)
        {
            InitializeHoles();
        }
    }

    void Start()
    {
        isQuitting = false;
        isInitialized = false; // 預設為 false

        if (!ValidateSprites()) { enabled = false; return; }
        if (!ValidateHoleArray()) { enabled = false; return; }

        InitializeHoles(); // 這裡會記錄初始位置

        // ★ 關鍵修改：不立刻 ScheduleSpawns()。
        // 改為在 0.1 秒後（確保所有 Dictionary 都存好了）再執行
        Invoke(nameof(CompleteInitialization), 0.1f);
    }

    // 新增一個函式
    private void CompleteInitialization()
    {
        isInitialized = true;
        ScheduleSpawns(); // 只有到這裡才真正開始生老鼠
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
            initialHolePositions[hole] = go.transform.position;

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = spriteA;
                holeRenderers[hole] = sr;
            }
            else
            {
                continue;
            }

            var col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
            col.enabled = false;
            col.isTrigger = true;

            holeGeneration[hole] = 0;
        }
    }

    private void ScheduleSpawns()
    {
        CancelInvoke();
        // ★ 修正：不要用 0f，改用 mouseSpawnInterval
        InvokeRepeating(nameof(Create), mouseSpawnInterval, mouseSpawnInterval);
        InvokeRepeating(nameof(Boom), c4SpawnInterval, c4SpawnInterval);
    }

    void Update()
    {
        if (usercontrol == null)
        {
            usercontrol = FindObjectOfType<usercontrol>();
        }

        // ★ 修正：註解掉或刪除下面這段，不要殺死協程！
        /*
        if (usercontrol != null && !usercontrol.IsGameActive())
        {
            CancelAllInvokesAndCoroutines();
        }
        */
    }

    private void CancelAllInvokesAndCoroutines()
    {
        CancelInvoke();
        StopAllCoroutines();
    }



    public void Create()
    {
        if (!CanSpawn()) return;
        Holenumber hole = GetUniqueHole();
        if (hole == Holenumber.Invalid || !holeObjects.ContainsKey(hole)) return;

        // ★ 修正二：選好洞後「立刻」標記為已佔用，避免在播放 Shake 動畫期間被其他物件選走
        occupiedHoles.Add(hole);
        StartCoroutineSafe(ShakeAndSpawn(hole, spriteB, true, mouseExistTime));
    }

    public void Boom()
    {
        if (!CanSpawn()) return;
        Holenumber hole = GetUniqueHole();
        if (hole == Holenumber.Invalid || !holeObjects.ContainsKey(hole)) return;

        // ★ 修正二：選好洞後「立刻」標記為已佔用
        occupiedHoles.Add(hole);
        Sprite c4 = Random.value > 0.5f ? spriteC4_1 : spriteC4_2;
        StartCoroutineSafe(ShakeAndSpawn(hole, c4, false, c4ExistTime));
    }

    private bool CanSpawn()
    {
        return usercontrol != null
            && usercontrol.IsGameActive()
            && gameObject.activeInHierarchy
            && !isQuitting
            && isInitialized    // ★ 新增這行：確認初始化已完全完成
            && occupiedHoles.Count < 8;
    }

    private int GetMouseCount()
    {
        int count = 0;
        foreach (Holenumber hole in occupiedHoles)
        {
            if (holeObjects.TryGetValue(hole, out GameObject go) && go != null)
            {
                HoleState state = go.GetComponent<HoleState>();
                if (state != null && state.IsMouse)
                    count++;
            }
        }
        return count;
    }

    private IEnumerator ShakeAndSpawn(Holenumber hole, Sprite target, bool isMouse, float delay)
    {
        if (isQuitting || !holeObjects.TryGetValue(hole, out GameObject go) || go == null)
        {
            occupiedHoles.Remove(hole); // 如果發生錯誤，將洞釋放
            yield break;
        }
        if (!holeRenderers.TryGetValue(hole, out SpriteRenderer sr) || sr == null)
        {
            occupiedHoles.Remove(hole);
            yield break;
        }

        globalGeneration++;
        int currentGen = globalGeneration;
        holeGeneration[hole] = currentGen;

        yield return Shake(go.transform);

        sr.sprite = target;
        spawnedSprites[hole] = target;
        Sprite hover = isMouse ? spriteB_hover :
            (target == spriteC4_1 ? spriteC4_1_hover : spriteC4_2_hover);
        hoverSprites[hole] = hover;

        var col = go.GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = true;
        go.tag = isMouse ? "mouse" : "C4";

        HoleState state = go.GetComponent<HoleState>() ?? go.AddComponent<HoleState>();
        state.Initialize(hole, mouseRoot, isMouse, this);

        // 注意：這裡原本有 occupiedHoles.Add(hole); 已經移到 Create 和 Boom 中了
        clickedHoles.Remove(hole);
        OnHoleSpawned?.Invoke(state);

        yield return new WaitForSeconds(delay);

        if (holeGeneration.TryGetValue(hole, out int gen) && gen == currentGen)
        {
            if (occupiedHoles.Contains(hole) && !clickedHoles.Contains(hole))
            {
                ResetHole(hole);
            }
        }
    }

    private IEnumerator Shake(Transform trans)
    {
        Vector3 orig = trans.position;
        float t = 0f;
        while (t < shakeDuration)
        {
            if (trans == null) yield break;
            float x = Mathf.Sin(t * shakeSpeed) * shakeMagnitude;
            trans.position = orig + new Vector3(x, 0f, 0f);
            t += Time.deltaTime;
            yield return null;
        }
        if (trans != null) trans.position = orig;
    }

    public Holenumber GetUniqueHole()
    {
        if (occupiedHoles.Count >= 8) return Holenumber.Invalid;
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

        if (initialHolePositions.ContainsKey(hole))
        {
            go.transform.position = initialHolePositions[hole];
        }
        Animator anim = go.GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        if (!holeRenderers.TryGetValue(hole, out SpriteRenderer sr)) return;

        HoleState state = go.GetComponent<HoleState>();
        if (state != null) OnHoleRemoved?.Invoke(state);

        sr.sprite = spriteA;
        var col = go.GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = false;
        if (state != null) Destroy(state);
        go.tag = "Untagged";

        spawnedSprites.Remove(hole);
        hoverSprites.Remove(hole);
        occupiedHoles.Remove(hole);
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