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

    public List<GameObject> newEnemies;     // 敵人預製件（僅用來取資料）
    public List<Unit> enemyUnits;           // 敵人 Unit（場景中的實例！）
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

        float baseY = -160f;

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            GameObject hudInstance = Instantiate(enemyBattleHUDPrefab, battleUI);
            BattleHUD hud = hudInstance.GetComponent<BattleHUD>();
            if (hud == null)
            {
                Debug.LogError($"HUD prefab 缺少 BattleHUD！索引: {i}");
                Destroy(hudInstance);
                continue;
            }

            Unit unit = enemyUnits[i];
            hud.SetHUD(unit);
            unit.battleHUD = hud;
            instantiatedEnemyHUDs.Add(hud);
            hudInstance.SetActive(false);

            // 關鍵修正：用「場景中的 enemy GameObject」做 key
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
            if (rt != null) rt.anchoredPosition = new Vector2(xPosition, baseY);

            Debug.Log($"HUD 綁定敵人: {enemyGameObject.name}");
        }

        // 傳給 BattleSystem
        targetScript.enemyHUDs = instantiatedEnemyHUDs;
        targetScript.enemyPrefabs = enemyUnits.ConvertAll(u => u.gameObject); // 傳「場景實例」

        List<Unit> enemyUnitsList = new List<Unit>(enemyUnits);
        skillmanager.enemyUnits = enemyUnitsList;
        buffDebuffManager.enemyUnits = enemyUnitsList;
        enemyskill.enemyUnits = enemyUnitsList;
        targetScript.buffDebuffManager = buffDebuffManager;
        targetScript.state = BattleState.START;

        foreach (var monster in monsters)
            Addmonster.AddMonster(monster);

        Debug.Log("戰鬥啟動，HUD 已綁定場景敵人");
    }

    private void Update()
    {
        if (targetScript == null || targetScript.state == BattleState.END || targetScript.state == BattleState.RUN)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, LayerMask.GetMask("Enemy"));

        // 先隱藏所有
        foreach (var hud in instantiatedEnemyHUDs)
            if (hud != null && hud.gameObject.activeSelf)
                hud.gameObject.SetActive(false);

        if (hit.collider != null)
        {
            GameObject hitEnemy = hit.collider.gameObject;
            if (enemyToHUDMap.TryGetValue(hitEnemy, out BattleHUD hud))
            {
                hud.gameObject.SetActive(true);
                // Debug.Log($"顯示 HUD: {hitEnemy.name}");
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