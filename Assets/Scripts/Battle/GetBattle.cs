using Cinemachine;
using UnityEngine;

public class GetBattle : MonoBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera virtualCamera = null;
    public BattleSystem targetScript; // 拖入目標腳本 (可以用程式碼取得)
    public SkillManager skillmanager;
    public BuffDebuffManager buffDebuffManager;
    public Enemyskill enemyskill;
    public GameObject newEnemy; // 要指派的新物件
    public Unit enemyUnit;
    public MonsterEntryUI Addmonster;
    public MonsterData monster;
    //public Unit enemybuffdebuff;
    private void Start()
    {
        virtualCamera.enabled = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            virtualCamera.enabled = true;
            targetScript.enemyPrefab = newEnemy;
            skillmanager.enemyUnit = enemyUnit;
            Debug.Log(enemyUnit);
            buffDebuffManager.enemyUnit = enemyUnit;
            enemyskill.enemyUnit = enemyUnit;
            targetScript.buffDebuffManager = buffDebuffManager;
            targetScript.state = BattleState.START;
            Addmonster.AddMonster(monster);
            Debug.Log("已新增新物件到 targetScript！");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            virtualCamera.enabled = false;
            targetScript.enemyPrefab = null;
            skillmanager.enemyUnit = null;
            enemyskill.enemyUnit = null;
           buffDebuffManager.enemyUnit = null;
            targetScript.buffDebuffManager = null;
            Debug.Log("已移除 targetScript 內的物件！");
        }
    }
    private void OnValidate()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}
