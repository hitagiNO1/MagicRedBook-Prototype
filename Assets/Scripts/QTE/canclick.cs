using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class canclick : MonoBehaviour
{
    public GameObject tileWhitePrefab;
    public GameObject tileBlackPrefab;
    public Transform spawnPoint;
    public float columnSpacing = 10f;
    public int targetSuccessCount = 15;
    private int currentSuccessCount = 0;
    public bool gameEnded = false;
    private int blackTileGenerated = 0;
    public GameObject startButton;
    private List<Tile> activeTiles = new List<Tile>();
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI resulItem;
    public GameObject BackButton;
    public float columnAngle = 0f;
    public GameFlowManager gameflow;
    public float fallSpeed = 5f;

    [Header("Return Settings")]
    public string mainSceneName;
    public Image fadeImage;
    public float fadeDuration = 1f;

    // Sprite 設置
    public Sprite[] columnSpecificWhiteSprites;
    public Sprite commonWhiteSprite;
    public float commonSpriteChance = 0.5f;
    public Sprite[] columnSpecificBlackSprites;

    [Header("Item Settings")]
    public ItemData[] itemOptions;

    [Header("Equipment Reward Settings")]
    public string rareEquipmentName = "劍客鞋子"; // 裝備名稱
    public float equipmentDropChance = 0.05f;    // 裝備掉落機率 (0.05 = 5%)
    private bool wonRareEquipment = false;       // 記錄本次是否抽中裝備

    // ★ 修改 1: 定義一個結構來儲存 "道具+數量"
    struct RewardInfo
    {
        public ItemData item;
        public int amount;
    }

    // ★ 修改 2: 使用 List 來儲存多個選中的獎勵
    private List<RewardInfo> selectedRewards = new List<RewardInfo>();

    void Start()
    {
        if (itemOptions.Length == 0)
        {
            Debug.LogWarning("請在 Inspector 設定 itemOptions！");
            return;
        }

        // 隨機選取獎勵
        GenerateRandomRewards();
    }

    // ★ 新增方法: 專門處理隨機獎勵邏輯
    void GenerateRandomRewards()
    {
        selectedRewards.Clear();
        wonRareEquipment = false; // 每次重新生成獎勵時，重置裝備抽獎狀態

        // 1. 建立一個暫存清單 (為了避免抽到重複的道具)
        List<ItemData> tempPool = new List<ItemData>(itemOptions);

        // 2. 決定要抽幾種道具 (1 ~ 3 種)
        int typesCount = Random.Range(1, 4);
        int finalCount = Mathf.Min(typesCount, tempPool.Count);

        Debug.Log($"本次將抽出 {finalCount} 種道具");

        for (int i = 0; i < finalCount; i++)
        {
            int randomIndex = Random.Range(0, tempPool.Count);
            ItemData pickedItem = tempPool[randomIndex];
            int randomAmt = Random.Range(1, 3); // 隨機數量 1 或 2

            selectedRewards.Add(new RewardInfo { item = pickedItem, amount = randomAmt });
            tempPool.RemoveAt(randomIndex);

            Debug.Log($"-- 選中獎勵: {pickedItem.itemName}, 數量: {randomAmt}");
        }

        // 3. 裝備的 5% 獨立抽獎機制
        if (Random.value <= equipmentDropChance)
        {
            // 防呆檢查：如果玩家還沒拿過這件裝備，才給予獎勵
            if (EquipmentManager.Instance != null && !EquipmentManager.Instance.ownedEquipment.Contains(rareEquipmentName))
            {
                wonRareEquipment = true;
                Debug.Log($"-- 幸運觸發！抽中稀有裝備: {rareEquipmentName}");
            }
            else
            {
                Debug.Log($"-- 抽中稀有裝備，但玩家已經擁有了，跳過發放。");
            }
        }
    }

    void Update()
    {
        if (gameEnded) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    tile.OnClicked();
                }
            }
        }

        if (activeTiles.Count > 0)
        {
            Tile bottomTile = activeTiles[0];
            if (bottomTile.transform.position.y < -5f)
            {
                if (bottomTile.isBlack)
                {
                    GameOver("漏點黑 tile！");
                }
                else
                {
                    activeTiles.RemoveAt(0);
                    Destroy(bottomTile.gameObject);
                }
            }
        }
    }

    void SpawnRow()
    {
        if (blackTileGenerated >= targetSuccessCount) return;
        int blackCol = Random.Range(0, 3);
        for (int col = 0; col < 3; col++)
        {
            GameObject prefab = (col == blackCol) ? tileBlackPrefab : tileWhitePrefab;
            GameObject tile = Instantiate(prefab);
            tile.name = $"{((col == blackCol) ? "BlackTile" : "WhiteTile")}_col{col}_row{blackTileGenerated}";
            tile.transform.position = spawnPoint.position + new Vector3((col - 1) * columnSpacing, 0f, 0f);
            Tile tileScript = tile.GetComponent<Tile>();

            tileScript.isBlack = (col == blackCol);
            tileScript.col = col;
            tileScript.canclick = this;

            SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
            BoxCollider2D collider = tile.GetComponent<BoxCollider2D>();

            if (tileScript.isBlack)
            {
                if (columnSpecificBlackSprites.Length > col) spriteRenderer.sprite = columnSpecificBlackSprites[col];
            }
            else
            {
                bool useCommonSprite = Random.value < commonSpriteChance;
                if (useCommonSprite) spriteRenderer.sprite = commonWhiteSprite;
                else if (columnSpecificWhiteSprites.Length > col) spriteRenderer.sprite = columnSpecificWhiteSprites[col];
            }

            if (collider != null)
            {
                collider.size = spriteRenderer.sprite.bounds.size;
                collider.offset = Vector2.zero;
            }
            activeTiles.Add(tileScript);
        }
        blackTileGenerated++;
    }

    public void OnCorrectTileClicked(Tile tile)
    {
        if (gameEnded) return;
        activeTiles.Remove(tile);
        Destroy(tile.gameObject);
        currentSuccessCount++;
        UpdateScoreText();
        if (currentSuccessCount >= targetSuccessCount)
        {
            GameClear();
        }
    }

    public void OnWrongTileClicked(Tile tile)
    {
        if (gameEnded) return;
        GameOver("你點錯了！");
    }

    public void GameOver(string reason)
    {
        if (gameEnded) return;
        gameEnded = true;
        CancelInvoke(nameof(SpawnRow));
        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();
        gameflow.ShowResult();
        resultText.text = "失敗";
        resultText.gameObject.SetActive(true);
    }

    void GameClear()
    {
        gameEnded = true;
        CancelInvoke(nameof(SpawnRow));

        // 清理場景上剩餘的方塊
        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();

        if (gameflow != null)
        {
            gameflow.ShowResult();
        }

        // 1. 準備文字
        string finalMessage = "成功！";
        string finalItem = "";

        // 2. 遍歷一般道具獎勵清單
        foreach (RewardInfo reward in selectedRewards)
        {
            if (reward.item != null)
            {
                finalItem += $"{reward.item.itemName} x{reward.amount}\n";

                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem(reward.item, reward.amount);
                }
            }
        }

        // ★ 新增: 結算時如果有抽中裝備，發放並加入文字 (只顯示名字，不顯示數量)
        if (wonRareEquipment)
        {
            finalItem += $"{rareEquipmentName}\n";
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.AddEquipment(rareEquipmentName);
            }
        }

        // 3. 檢查背包系統
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("【注意】找不到 InventoryManager，物品僅顯示於文字，未實際加入背包。");
        }

        // 4. 顯示 UI
        if (resulItem != null)
        {
            resulItem.text = finalItem;
            resulItem.gameObject.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.text = finalMessage;
            resultText.gameObject.SetActive(true);
        }

        Debug.Log("遊戲結算完成，訊息已更新。");
    }

    public void StartGame()
    {
        if (gameEnded) return;
        StartCoroutine(SpawnRowCoroutine());
        startButton.SetActive(false);
    }

    IEnumerator SpawnRowCoroutine()
    {
        float delay = 0.6f;
        while (!gameEnded && blackTileGenerated < targetSuccessCount)
        {
            SpawnRow();
            yield return new WaitForSeconds(delay);
        }
    }

    public void EndMiniGame()
    {
        StartCoroutine(ReturnToMainScene());
    }

    private IEnumerator ReturnToMainScene()
    {
        yield return StartCoroutine(FadeOutAnimation());
        yield return new WaitForSeconds(0.5f);
        SceneSwapper.RestoreMainScene(mainSceneName);
        yield return null;
        yield return SceneManager.UnloadSceneAsync(gameObject.scene);
    }

    private IEnumerator FadeOutAnimation()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 1);
        }
    }

    public void RestartGame()
    {
        gameEnded = false;
        currentSuccessCount = 0;
        blackTileGenerated = 0;
        resultText.gameObject.SetActive(false);
        startButton.SetActive(true);
        UpdateScoreText();
        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();

        GenerateRandomRewards();
    }

    void UpdateScoreText()
    {
        scoreText.text = $"{currentSuccessCount}/{targetSuccessCount}";
    }
}