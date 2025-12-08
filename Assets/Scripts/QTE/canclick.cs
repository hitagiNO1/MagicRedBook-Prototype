using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class canclick : MonoBehaviour
{
    public GameObject tileWhitePrefab;
    public GameObject tileBlackPrefab;
    public Transform spawnPoint;
    public float columnSpacing = 10f; // 設定軌道間距
    public int targetSuccessCount = 15;
    private int currentSuccessCount = 0;
    public bool gameEnded = false;
    private int blackTileGenerated = 0;
    public GameObject startButton;
    private List<Tile> activeTiles = new List<Tile>();
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI resultText;
    public GameObject restartButton;
    public GameObject BackButton;
    public float columnAngle = 0f;
    public GameFlowManager gameflow;
    public ItemData[] itemOptions;
    private ItemData selectedItem;
    public int randomAmount;
    public float fallSpeed = 5f;

    [Header("Return Settings")]
    public string mainSceneName; // 必須填寫：你要回去的那個主場景名稱

    // 白色 Tile 的 Sprite
    public Sprite[] columnSpecificWhiteSprites; // 類型 1：左、中、右的專屬白色 Sprite（長度為 3）
    public Sprite commonWhiteSprite; // 類型 2：單一通用白色 Sprite
    public float commonSpriteChance = 0.5f; // 類型 2 出現的機率（0.0 到 1.0）

    // 黑色 Tile 的 Sprite
    public Sprite[] columnSpecificBlackSprites; // 左、中、右的專屬黑色 Sprite（長度為 3）

    void Start()
    {
        if (itemOptions.Length == 0)
        {
            Debug.LogWarning("請在 Inspector 設定 itemOptions！");
            return;
        }
        if (columnSpecificWhiteSprites.Length < 3)
        {
            Debug.LogWarning("請在 Inspector 為 columnSpecificWhiteSprites 設置 3 個不同的 Sprite！");
            return;
        }
        if (commonWhiteSprite == null)
        {
            Debug.LogWarning("請在 Inspector 設置 commonWhiteSprite！");
            return;
        }
        if (columnSpecificBlackSprites.Length < 3)
        {
            Debug.LogWarning("請在 Inspector 為 columnSpecificBlackSprites 設置 3 個不同的黑色 Sprite！");
            return;
        }
        int randomIndex = Random.Range(0, itemOptions.Length);
        selectedItem = itemOptions[randomIndex];
        randomAmount = Random.Range(1, 4);
        Debug.Log($"選中：{selectedItem.itemName}，數量：{randomAmount}");
    }

    void Update()
    {
        if (gameEnded) return;

        if (Input.GetMouseButtonDown(0))
        {
            // 3D Raycast：從相機發射射線經過滑鼠位置
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            Debug.Log($"3D 點擊：螢幕位置 {Input.mousePosition}");

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"3D Hit: {hit.collider.gameObject.name} (距離: {hit.distance:F2})");

                Tile tile = hit.collider.GetComponent<Tile>();
                if (tile != null)
                {
                    Debug.Log($"點擊 Tile: isBlack={tile.isBlack}, col={tile.col}");
                    tile.OnClicked();
                }
                else
                {
                    Debug.LogWarning($"擊中物件無 Tile 腳本: {hit.collider.gameObject.name}");
                }
            }
            else
            {
                Debug.Log("3D 射線未擊中任何 Collider");
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
        Debug.Log($" 生成第 {blackTileGenerated + 1} 行，黑 Tile 在 col={blackCol}");
        for (int col = 0; col < 3; col++)
        {
            // 每列必生成 Tile
            GameObject prefab = (col == blackCol) ? tileBlackPrefab : tileWhitePrefab;
            GameObject tile = Instantiate(prefab);
            tile.name = $"{((col == blackCol) ? "BlackTile" : "WhiteTile")}_col{col}_row{blackTileGenerated}";
            tile.transform.position = spawnPoint.position + new Vector3((col - 1) * columnSpacing, 0f, 0f);
            Tile tileScript = tile.GetComponent<Tile>();
            if (tileScript == null)
            {
                Debug.LogError($" {(col == blackCol ? "tileBlackPrefab" : "tileWhitePrefab")} 缺少 Tile 腳本！");
                Destroy(tile);
                continue;
            }
            tileScript.isBlack = (col == blackCol);
            tileScript.col = col;
            tileScript.canclick = this;
            SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
            BoxCollider2D collider = tile.GetComponent<BoxCollider2D>();
            if (spriteRenderer != null)
            {
                if (tileScript.isBlack)
                {
                    if (columnSpecificBlackSprites.Length > col)
                    {
                        spriteRenderer.sprite = columnSpecificBlackSprites[col];
                    }
                    else
                    {
                        Debug.LogError($" 黑 Tile col={col} 缺少對應 Sprite！");
                    }
                }
                else
                {
                    // 每個白色 Tile 獨立隨機
                    bool useCommonSprite = Random.value < commonSpriteChance; // 移到這裡
                    if (useCommonSprite)
                    {
                        spriteRenderer.sprite = commonWhiteSprite;
                        Debug.Log($"白 Tile col={col} 使用通用 Sprite: {spriteRenderer.sprite.name}");
                    }
                    else
                    {
                        if (columnSpecificWhiteSprites.Length > col)
                        {
                            spriteRenderer.sprite = columnSpecificWhiteSprites[col];
                            Debug.Log($"白 Tile col={col} 使用專屬 Sprite: {spriteRenderer.sprite.name}");
                        }
                        else
                        {
                            Debug.LogError($" 白 Tile col={col} 缺少對應 Sprite！");
                        }
                    }
                }
                if (collider != null)
                {
                    collider.size = spriteRenderer.sprite.bounds.size;
                    collider.offset = Vector2.zero;
                    Debug.Log($" {(tileScript.isBlack ? "黑" : "白")} Tile col={col}: Sprite={spriteRenderer.sprite.name}, Collider={collider.size}");
                }
                else
                {
                    Debug.LogWarning($" {(tileScript.isBlack ? "黑" : "白")} Tile col={col} 缺少 BoxCollider2D！");
                }
            }
            else
            {
                Debug.LogError($" {(tileScript.isBlack ? "tileBlackPrefab" : "tileWhitePrefab")} 缺少 SpriteRenderer！");
            }
            activeTiles.Add(tileScript);
        }
        blackTileGenerated++;
    }

    public void OnCorrectTileClicked(Tile tile)
    {
        if (gameEnded) return;
        Debug.Log($" OnCorrectTileClicked: col={tile.col}, isBlack={tile.isBlack}");
        activeTiles.Remove(tile);
        Destroy(tile.gameObject);
        currentSuccessCount++;
        UpdateScoreText();
        Debug.Log($"正確點擊：{currentSuccessCount} / {targetSuccessCount}");
        if (currentSuccessCount >= targetSuccessCount)
        {
            GameClear();
        }
    }

    public void OnWrongTileClicked(Tile tile)
    {
        if (gameEnded) return;
        Debug.Log($" OnWrongTileClicked: col={tile.col}, isBlack={tile.isBlack}");
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
        resultText.text = $"失敗！\n{reason}";
        resultText.gameObject.SetActive(true);
        restartButton.SetActive(true);
        Debug.Log("遊戲結束：" + reason);
    }

    void GameClear()
    {
        gameEnded = true;
        CancelInvoke(nameof(SpawnRow));
        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();
        gameflow.ShowResult();
        resultText.text = "成功！\n你點滿了 15 個黑塊！";
        resultText.gameObject.SetActive(true);
        restartButton.SetActive(true);
        Debug.Log("遊戲成功！");
        InventoryManager.Instance.AddItem(selectedItem, randomAmount);
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

  

    // 這個方法可以在小遊戲結束時呼叫 (例如：打贏魔王、按下離開按鈕)
    public void EndMiniGame()
    {
        StartCoroutine(ReturnToMainScene());
    }

    private IEnumerator ReturnToMainScene()
    {
        // 1. 找到原本的主場景
        Scene mainScene = SceneManager.GetSceneByName(mainSceneName);

        if (mainScene.IsValid() && mainScene.isLoaded)
        {
            // 2. 把主場景的所有物件重新打開 (Resume)
            GameObject[] rootObjects = mainScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                obj.SetActive(true);
            }

            // 設定回主場景為 Active
            SceneManager.SetActiveScene(mainScene);
        }
        else
        {
            Debug.LogError("找不到主場景，可能已被卸載或是名稱錯誤！");
        }

        // 3. 卸載目前的小遊戲場景
        // 注意：這裡使用 gameObject.scene 可以自動抓取腳本所在的場景
        yield return SceneManager.UnloadSceneAsync(gameObject.scene);
    }


public void RestartGame()
    {
        gameEnded = false;
        currentSuccessCount = 0;
        blackTileGenerated = 0;
        resultText.gameObject.SetActive(false);
        restartButton.SetActive(false);
        startButton.SetActive(true);
        UpdateScoreText();
        foreach (var tile in activeTiles)
        {
            Destroy(tile.gameObject);
        }
        activeTiles.Clear();
    }

    void UpdateScoreText()
    {
        scoreText.text = $"{currentSuccessCount}/{targetSuccessCount}";
    }
}