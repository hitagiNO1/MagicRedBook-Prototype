using Cinemachine;
using UnityEngine;
using System.Collections.Generic;

public class mutiplebattle : MonoBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera virtualCamera = null;
    public BattleSystem targetScript; // BattleSystem 的引用
    public SkillManager skillmanager;
    public BuffDebuffManager buffDebuffManager;
    public Enemyskill enemyskill;
    public List<GameObject> newEnemies; // 敵人預製件列表
    public List<Unit> enemyUnits; // 敵人 Unit 組件列表
    public MonsterEntryUI Addmonster;
    public List<MonsterData> monsters; // 怪物資料列表
    [SerializeField]
    private GameObject enemyBattleHUDPrefab; // EnemyBattleHUD 預製件的引用

    private List<BattleHUD> instantiatedEnemyHUDs = new List<BattleHUD>(); // 追蹤實例化的 HUD
    private Dictionary<GameObject, BattleHUD> enemyToHUDMap = new Dictionary<GameObject, BattleHUD>(); // 映射敵人到 HUD

    private void Start()
    {
        if (virtualCamera == null)
        {
            Debug.LogError("virtualCamera 未在 Inspector 中設置！");
        }
        else
        {
            virtualCamera.enabled = false;
        }

        // 驗證組件
        if (targetScript == null) Debug.LogError("targetScript 未設置！");
        if (skillmanager == null) Debug.LogError("skillmanager 未設置！");
        if (buffDebuffManager == null) Debug.LogError("buffDebuffManager 未設置！");
        if (enemyskill == null) Debug.LogError("enemyskill 未設置！");
        if (newEnemies == null || newEnemies.Count == 0) Debug.LogError("newEnemies 未設置或為空！");
        if (enemyUnits == null || enemyUnits.Count == 0) Debug.LogError("enemyUnits 未設置或為空！");
        if (Addmonster == null) Debug.LogError("Addmonster 未設置！");
        if (monsters == null || monsters.Count == 0) Debug.LogError("monsters 未設置或為空！");
        if (enemyBattleHUDPrefab == null) Debug.LogError("enemyBattleHUDPrefab 未設置！");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("玩家進入觸發區域");
            // 驗證所有必要的組件
            if (virtualCamera == null || targetScript == null || skillmanager == null ||
                buffDebuffManager == null || enemyskill == null || newEnemies == null || newEnemies.Count == 0 ||
                enemyUnits == null || enemyUnits.Count == 0 || Addmonster == null || monsters == null || monsters.Count == 0 ||
                enemyBattleHUDPrefab == null)
            {
                Debug.LogError("OnTriggerEnter：必要組件未設置，無法啟動戰鬥！");
                return;
            }

            virtualCamera.enabled = true;

            // 清除之前實例化的 HUD
            ClearInstantiatedHUDs();

            // 查找 battleUI 作為父物件
            Transform battleUI = GameObject.Find("BattleUI")?.transform;
            if (battleUI == null)
            {
                Debug.LogError("battleUI 未找到，請確保 Canvas 下有一個名為 'BattleUI' 的空物件！");
                return;
            }

            // 根據敵人數量實例化 EnemyBattleHUD
            instantiatedEnemyHUDs = new List<BattleHUD>();
            float baseY = -160f; // 固定 y 座標

            for (int i = 0; i < enemyUnits.Count; i++)
            {
                GameObject hudInstance = Instantiate(enemyBattleHUDPrefab, battleUI);
                BattleHUD hud = hudInstance.GetComponent<BattleHUD>();
                if (hud != null)
                {
                    hud.SetHUD(enemyUnits[i]); // 將對應敵人的 Unit 設置到 HUD
                    enemyUnits[i].battleHUD = hud;
                    instantiatedEnemyHUDs.Add(hud);
                    Debug.Log($"實例化 HUD 為敵人: {newEnemies[i].name}");

                    // 初始隱藏 HUD
                    hudInstance.SetActive(false);

                    // 映射敵人到 HUD
                    enemyToHUDMap[newEnemies[i]] = hud;

                    // 設置 HUD 位置
                    float xPosition = 0f;
                    if (enemyUnits.Count == 1)
                    {
                        xPosition = 0f; // 位置: (0, -160)
                    }
                    else if (enemyUnits.Count == 2)
                    {
                        xPosition = (i == 0 ? -400f : 400f); // 位置: (-400, -160), (400, -160)
                    }
                    else if (enemyUnits.Count == 3)
                    {
                        xPosition = (i == 0 ? -550f : i == 1 ? 0f : 550f); // 位置: (-550, -160), (0, -160), (550, -160)
                    }
                    RectTransform hudRectTransform = hudInstance.GetComponent<RectTransform>();
                    if (hudRectTransform != null)
                    {
                        hudRectTransform.anchoredPosition = new Vector2(xPosition, baseY);
                    }
                }
                else
                {
                    Debug.LogError($"EnemyBattleHUDPrefab 缺少 BattleHUD 組件！索引: {i}");
                }
            }

            // 將實例化的 HUD 傳遞給 BattleSystem
            targetScript.enemyHUDs = instantiatedEnemyHUDs;

            targetScript.enemyPrefabs = new List<GameObject>(newEnemies);
            List<Unit> enemyUnitsList = new List<Unit>(enemyUnits);
            skillmanager.enemyUnits = enemyUnitsList;
            buffDebuffManager.enemyUnits = enemyUnitsList;
            enemyskill.enemyUnits = enemyUnitsList;
            targetScript.buffDebuffManager = buffDebuffManager;
            targetScript.state = BattleState.START;

            // 添加多個怪物
            foreach (var monster in monsters)
            {
                Addmonster.AddMonster(monster);
            }

            Debug.Log("已新增多個敵人到 targetScript，並啟動戰鬥！");
        }
    }

    private void Update()
    {
        // 僅在戰鬥開始後檢查滑鼠
        if (targetScript != null && targetScript.state != BattleState.END)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, LayerMask.GetMask("Enemy"));

            if (hit.collider != null)
            {
                //Debug.Log($"Raycast2D 擊中: {hit.collider.gameObject.name}，位置: {hit.point}");
                GameObject hitObject = hit.collider.gameObject;
                if (newEnemies.Contains(hitObject) && enemyToHUDMap.ContainsKey(hitObject))
                {
                    //Debug.Log($"顯示 HUD 為敵人: {hitObject.name}");
                    enemyToHUDMap[hitObject].gameObject.SetActive(true);
                }
            }
            else
            {
                //Debug.Log($"Raycast2D 未擊中任何物件，滑鼠螢幕座標: {Input.mousePosition}, 世界座標: {Camera.main.ScreenToWorldPoint(Input.mousePosition)}");
                foreach (var hud in instantiatedEnemyHUDs)
                {
                    if (hud != null && hud.gameObject.activeSelf)
                    {
                        hud.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (virtualCamera != null)
            {
                virtualCamera.enabled = false;
            }

            if (targetScript != null)
            {
                targetScript.enemyPrefabs.Clear();
                targetScript.enemyHUDs.Clear(); // 清空 BattleSystem 的 enemyHUDs
            }
            if (skillmanager != null)
            {
                skillmanager.enemyUnits.Clear();
            }
            if (buffDebuffManager != null)
            {
                buffDebuffManager.enemyUnits.Clear(); // 清空敵人單位列表
            }
            if (enemyskill != null)
            {
                enemyskill.enemyUnits.Clear();
            }

            // 銷毀實例化的 HUD
            ClearInstantiatedHUDs();

            Debug.Log("已清空 targetScript、skillmanager、buffDebuffManager 和 enemyskill 的敵人數據！");
        }
    }

    private void ClearInstantiatedHUDs()
    {
        foreach (var hud in instantiatedEnemyHUDs)
        {
            if (hud != null && hud.gameObject != null)
            {
                Destroy(hud.gameObject);
            }
        }
        instantiatedEnemyHUDs.Clear();
        enemyToHUDMap.Clear();
    }
}