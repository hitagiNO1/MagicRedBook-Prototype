using Cinemachine;
using UnityEngine;
using System.Collections.Generic;

public class mutiplebattle : MonoBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera virtualCamera = null;
    public BattleSystem targetScript; // 拖入目標腳本
    public SkillManager skillmanager;
    public BuffDebuffManager buffDebuffManager;
    public Enemyskill enemyskill;
    public List<GameObject> newEnemies; // 要指派的多個新敵人物件
    public List<Unit> enemyUnits; // 多個敵人單位
    public MonsterEntryUI Addmonster;
    public List<MonsterData> monsters; // 多個怪物數據
    public List<StoryScene> IfScenes; // 改為 StoryScene 以匹配 BattleSystem

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

        // 檢查必要的組件
        if (targetScript == null) Debug.LogError("targetScript 未設置！");
        if (skillmanager == null) Debug.LogError("skillmanager 未設置！");
        if (buffDebuffManager == null) Debug.LogError("buffDebuffManager 未設置！");
        if (enemyskill == null) Debug.LogError("enemyskill 未設置！");
        if (newEnemies == null || newEnemies.Count == 0) Debug.LogError("newEnemies 未設置或為空！");
        if (enemyUnits == null || enemyUnits.Count == 0) Debug.LogError("enemyUnits 未設置或為空！");
        if (Addmonster == null) Debug.LogError("Addmonster 未設置！");
        if (monsters == null || monsters.Count == 0) Debug.LogError("monsters 未設置或為空！");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (virtualCamera == null || targetScript == null || skillmanager == null ||
                buffDebuffManager == null || enemyskill == null || newEnemies == null || newEnemies.Count == 0 ||
                enemyUnits == null || enemyUnits.Count == 0 || Addmonster == null || monsters == null || monsters.Count == 0)
            {
                Debug.LogError("OnTriggerEnter：必要組件未設置，無法啟動戰鬥！");
                return;
            }

            virtualCamera.enabled = true;

            targetScript.enemyPrefabs = new List<GameObject>(newEnemies);
            List<Unit> enemyUnitsList = new List<Unit>(enemyUnits);
            skillmanager.enemyUnits = enemyUnitsList;
            buffDebuffManager.enemyUnits = enemyUnitsList; // 設置敵人單位列表
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

            Debug.Log("已清空 targetScript、skillmanager、buffDebuffManager 和 enemyskill 的敵人數據！");
        }
    }

    private void OnValidate()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        else
        {
            Debug.LogError("GetBattle 缺少 Collider 組件！");
        }
    }
}
