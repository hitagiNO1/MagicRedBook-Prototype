using Cinemachine;
using UnityEngine;
using System.Collections.Generic;

public class mutiplebattle : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera = null;
    public BattleSystem targetScript;
    public SkillManager skillmanager;
    public BuffDebuffManager buffDebuffManager;
    public Enemyskill enemyskill;
    public List<GameObject> newEnemies; // 敵人預製件（僅用來取資料）
    public List<Unit> enemyUnits; // 敵人 Unit（場景中的實例！）
    public MonsterEntryUI Addmonster;
    public List<MonsterData> monsters;
    [SerializeField] private GameObject enemyBattleHUDPrefab;
    private List<BattleHUD> instantiatedEnemyHUDs = new List<BattleHUD>();
    private Dictionary<GameObject, BattleHUD> enemyToHUDMap = new Dictionary<GameObject, BattleHUD>(); // 實例 → HUD

    private void Start()
    {
        if (virtualCamera == null) Debug.LogError("virtualCamera 未設置！");
        else virtualCamera.enabled = false;
        if (targetScript == null) Debug.LogError("targetScript 未設置！");
        if (skillmanager == null) Debug.LogError("skillmanager 未設置！");
        if (buffDebuffManager == null) Debug.LogError("buffDebuffManager 未設置！");
        if (enemyskill == null) Debug.LogError("enemyskill 未設置！");
        if (newEnemies == null || newEnemies.Count == 0) Debug.LogError("newEnemies 未設置！");
        if (enemyUnits == null || enemyUnits.Count == 0) Debug.LogError("enemyUnits 未設置！");
        if (Addmonster == null) Debug.LogError("Addmonster 未設置！");
        if (monsters == null || monsters.Count == 0) Debug.LogError("monsters 未設置！");
        if (enemyBattleHUDPrefab == null) Debug.LogError("enemyBattleHUDPrefab 未設置！");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log("玩家進入觸發區域");

        if (virtualCamera == null || targetScript == null || skillmanager == null ||
            buffDebuffManager == null || enemyskill == null || newEnemies == null || newEnemies.Count == 0 ||
            enemyUnits == null || enemyUnits.Count == 0 || Addmonster == null ||
            monsters == null || monsters.Count == 0 || enemyBattleHUDPrefab == null)
        {
            Debug.LogError("必要組件未設置！");
            return;
        }

        virtualCamera.enabled = true;
        ClearInstantiatedHUDs();

        Transform battleUI = GameObject.Find("BattleUI")?.transform;
        if (battleUI == null)
        {
            Debug.LogError("找不到 BattleUI！");
            return;
        }

        instantiatedEnemyHUDs.Clear();
        enemyToHUDMap.Clear();

        // 動態調整 Y 位置（可選優化）
        float baseY = enemyUnits.Count <= 3 ? 230f : 270f;

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            Unit unit = enemyUnits[i];

            // 關鍵修改：從 MonsterData 複製資料到場景中的 Unit
            if (i < monsters.Count)
            {
                MonsterData data = monsters[i];
                unit.unitName = data.monsterName;     // 名字
                //unit.maxHP = data.maxHP;
               // unit.currentHP = data.maxHP;
                //unit.damage = data.damage;
               // unit.defense = data.defense;
                // unit.unitLevel = data.level;       // 如有需要
                // unit.defenseData = data.defenseData; // 如有需要
            }
            else
            {
                Debug.LogWarning($"monsters 資料不足，敵人 {i} 使用預設值");
            }

            // 創建 HUD
            GameObject hudInstance = Instantiate(enemyBattleHUDPrefab, battleUI);
            BattleHUD hud = hudInstance.GetComponent<BattleHUD>();
            if (hud == null)
            {
                Debug.LogError($"HUD prefab 缺少 BattleHUD！索引: {i}");
                Destroy(hudInstance);
                continue;
            }

            // 設定 HUD（會自動顯示 unitName）
            hud.SetHUD(unit);
            unit.battleHUD = hud;

            instantiatedEnemyHUDs.Add(hud);
            hudInstance.SetActive(false);

            // 用場景中的 enemy GameObject 做 key
            GameObject enemyGameObject = unit.gameObject;
            enemyToHUDMap[enemyGameObject] = hud;

            // 位置計算
            float xPosition;
            switch (enemyUnits.Count)
            {
                case 1: xPosition = 0f; break;
                case 2: xPosition = i == 0 ? -400f : 400f; break;
                case 3: xPosition = i == 0 ? -550f : i == 1 ? 0f : 550f; break;
                default: xPosition = i * 200f - (enemyUnits.Count - 1) * 100f; break;
            }

            RectTransform rt = hudInstance.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = new Vector2(xPosition, baseY);

            Debug.Log($"HUD 綁定敵人: {enemyGameObject.name}，顯示名: {unit.unitName}");
        }

        // 傳給 BattleSystem
        targetScript.enemyHUDs = instantiatedEnemyHUDs;
        targetScript.enemyPrefabs = enemyUnits.ConvertAll(u => u.gameObject);
        List<Unit> enemyUnitsList = new List<Unit>(enemyUnits);
        skillmanager.enemyUnits = enemyUnitsList;
        buffDebuffManager.enemyUnits = enemyUnitsList;
        enemyskill.enemyUnits = enemyUnitsList;
        targetScript.buffDebuffManager = buffDebuffManager;
        targetScript.state = BattleState.START;

        foreach (var monster in monsters)
            Addmonster.AddMonster(monster);

        Debug.Log("戰鬥啟動，HUD 已綁定並顯示敵人名字");
    }

    private void Update()
    {
        if (targetScript == null) return;

        // 【修正點】：當戰鬥狀態為 END 或 RUN 時，主動關閉相機並清理 HUD
        if (targetScript.state == BattleState.END || targetScript.state == BattleState.RUN)
        {
            if (virtualCamera.enabled)
            {
                virtualCamera.enabled = false;

                // 戰鬥結束時也應該把 HUD 清理掉，不然可能會殘留在畫面上
                ClearInstantiatedHUDs();

                Debug.Log("戰鬥狀態結束，已關閉 VCAM 並清理 HUD");
            }
            return; // 結束 Update，不執行後面的滑鼠射線邏輯
        }

        // 原有：滑鼠懸停顯示邏輯
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, LayerMask.GetMask("Enemy"));

        // 先隱藏所有 HUD
        foreach (var hud in instantiatedEnemyHUDs)
            if (hud != null && hud.gameObject.activeSelf)
                hud.gameObject.SetActive(false);

        // 顯示滑鼠懸停的敵人 HUD
        if (hit.collider != null)
        {
            GameObject hitEnemy = hit.collider.gameObject;
            if (enemyToHUDMap.TryGetValue(hitEnemy, out BattleHUD hud))
            {
                hud.gameObject.SetActive(true);
            }
        }

        // 動態追蹤所有敵人 X 軸位置
        foreach (var kvp in enemyToHUDMap)
        {
            if (kvp.Key == null) continue; // 敵人已銷毀

            GameObject enemy = kvp.Key;
            BattleHUD hud = kvp.Value;

            if (hud != null)
            {
                // 將敵人世界座標轉換為 Canvas 本地座標
                Vector3 enemyScreenPos = Camera.main.WorldToScreenPoint(enemy.transform.position);
                RectTransform hudRect = hud.GetComponent<RectTransform>();
                RectTransform canvasRect = hudRect.parent as RectTransform;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    enemyScreenPos,
                    null,
                    out Vector2 localPoint
                );

                // 只更新 X 軸，Y 軸保持固定（baseY）
                hudRect.anchoredPosition = new Vector2(localPoint.x, 290f);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        virtualCamera.enabled = false;

        if (targetScript != null)
        {
            targetScript.enemyPrefabs.Clear();
            targetScript.enemyHUDs.Clear();
        }
        if (skillmanager != null) skillmanager.enemyUnits.Clear();
        if (buffDebuffManager != null) buffDebuffManager.enemyUnits.Clear();
        if (enemyskill != null) enemyskill.enemyUnits.Clear();

        ClearInstantiatedHUDs();
        Debug.Log("戰鬥結束，HUD 已清除");
    }

    private void ClearInstantiatedHUDs()
    {
        foreach (var hud in instantiatedEnemyHUDs)
            if (hud != null && hud.gameObject != null)
                Destroy(hud.gameObject);

        instantiatedEnemyHUDs.Clear();
        enemyToHUDMap.Clear();
    }
}