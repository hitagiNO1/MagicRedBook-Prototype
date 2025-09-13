using Cinemachine;
using UnityEngine;
using System.Collections.Generic;

public class GetBattle : MonoBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera virtualCamera = null;
    public BattleSystem targetScript; // 拖入目標腳本
    public SkillManager skillmanager;
    public BuffDebuffManager buffDebuffManager;
    public Enemyskill enemyskill;
    public GameObject newEnemy; // 要指派的新敵人物件
    public Unit enemyUnit; // 敵人單位
    public MonsterEntryUI Addmonster;
    public MonsterData monster;
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
        if (newEnemy == null) Debug.LogError("newEnemy 未設置！");
        if (enemyUnit == null) Debug.LogError("enemyUnit 未設置！");
        if (Addmonster == null) Debug.LogError("Addmonster 未設置！");
        if (monster == null) Debug.LogError("monster 未設置！");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (virtualCamera == null || targetScript == null || skillmanager == null ||
                buffDebuffManager == null || enemyskill == null || newEnemy == null ||
                enemyUnit == null || Addmonster == null || monster == null)
            {
                Debug.LogError("OnTriggerEnter：必要組件未設置，無法啟動戰鬥！");
                return;
            }

            virtualCamera.enabled = true;

            targetScript.enemyPrefabs = new List<GameObject> { newEnemy };
            List<Unit> enemyUnitsList = new List<Unit> { enemyUnit };
            skillmanager.enemyUnits = enemyUnitsList;
            buffDebuffManager.enemyUnits = enemyUnitsList; // 設置敵人單位列表
            enemyskill.enemyUnits = enemyUnitsList;
            targetScript.buffDebuffManager = buffDebuffManager;
            targetScript.enemySkill = enemyskill; // 將 enemyskill 指派給 BattleSystem 的 enemySkill
            targetScript.state = BattleState.START;
            Addmonster.AddMonster(monster);

            Debug.Log("已新增敵人到 targetScript，並啟動戰鬥！已設置 enemySkill。");
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