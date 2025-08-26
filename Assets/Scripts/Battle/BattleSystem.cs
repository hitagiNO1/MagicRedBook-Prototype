using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST, RUN, END }
public class BattleSystem : MonoBehaviour
{

    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    public Unit playerUnit;
    public Unit enemyUnit;

    public TextMeshProUGUI dialogueText;
    public BattleHUD playerHUD;
    public BattleHUD enemyHUD;
    public Animator animator;

    private bool isHidden = false;
    public BattleState state;

    public SkillManager skillManager;
    public BuffDebuffManager buffDebuffManager;
    public Enemyskill enemyskill;  // 指向技能腳本（拖拉或程式指派）
    private List<System.Action> enemySkills;     // 儲存當前敵人的技能列表
    public int turnCounter = 0;
    private GetBattle GetBattle;
    public GameController Gamescript;
    void Start()
    {
        //SetupSkillsForEnemy(enemyUnit.unitName); // 假設 unitName 為 "Fish"
        state = BattleState.END;
        animator = animator.GetComponent<Animator>();
        skillManager = GetComponent<SkillManager>();
        buffDebuffManager = GetComponent<BuffDebuffManager>();
        /*
        if (enemySkills == null || enemySkills.Count == 0)
        {
            Debug.LogError("enemySkills 尚未初始化或為空！");
        }
        */
    }

    private void Update()
    {
        if (state == BattleState.START && !isHidden)
        {
            StartCoroutine(SetupBattle());
            GameStateManager.Instance.SetState(GameState.Battle); //控制遊戲狀態
            animator.SetTrigger("Show");
            isHidden = true;
        }
    }

    IEnumerator SetupBattle()
    {
        playerUnit = playerPrefab.GetComponent<Unit>();
        enemyUnit = enemyPrefab.GetComponent<Unit>();
        enemyskill = enemyPrefab.GetComponent<Enemyskill>(); // 建立後才指派
        GetBattle = enemyPrefab.GetComponentInChildren<GetBattle>();
        if (enemyskill == null)
        {
            Debug.LogError(" 無法在 enemyPrefab 上取得 Enemyskill 組件！");
        }
        else
        {
            Debug.Log(" 成功取得 Enemyskill 組件");
        }

        SetupSkillsForEnemy(enemyUnit.unitName); // 這時候才呼叫
        dialogueText.text = "野生的 " + enemyUnit.unitName + " 出現了..";
        playerHUD.SetHUD(playerUnit);
        enemyHUD.SetHUD(enemyUnit);
        yield return new WaitForSeconds(1f);
        StartCoroutine(skillManager.DelayedInitSkillIcons(2f));
        state = BattleState.PLAYERTURN;
        PlayerTurn();
    }

    void PlayerTurn()
    {

        
        dialogueText.text = "選擇你的行動:";
    }

    public IEnumerator EnemyTurn()
    {

        dialogueText.text = enemyUnit.unitName + " 正在思考要使用哪個技能...";
        yield return new WaitForSeconds(1f);

        int index = Random.Range(0, enemySkills.Count);
        enemySkills[index].Invoke();

        yield return new WaitForSeconds(1.5f);

        buffDebuffManager.TickDefenseDebuffs();
        buffDebuffManager.TickDefenseBuffs();
        buffDebuffManager.TickPoison();
        if (playerUnit.currentHP <= 0)
        {
            state = BattleState.LOST;
            EndBattle();
        }
        else
        {
            skillManager.InitSkillIcons();
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
    }

    //多怪物

    public void OnRunbutton()
    {
        if (state != BattleState.PLAYERTURN) return;
        StartCoroutine(PlayerRun());
    }

    IEnumerator PlayerRun()
    {
        state = BattleState.RUN;
        yield return new WaitForSeconds(0.5f);
        EndBattle();
    }

    public void EndBattle()
    {
        if (state == BattleState.WON)
        {
            dialogueText.text = "你贏得了戰鬥!";

            if (GetBattle.IfScenes.Count == 1)
            {
                Gamescript.PlayScene(GetBattle.IfScenes[0] as StoryScene); //如果GetBattle有劇情就撥放
            }
            else
            {
                GameStateManager.Instance.SetState(GameState.Playing);
            }
            Destroy(enemyPrefab);
        }
        else if (state == BattleState.LOST)
        {
            dialogueText.text = "你輸了...";
            GameStateManager.Instance.SetState(GameState.Playing);
        }
        else if (state == BattleState.RUN)
        {
            dialogueText.text = "逃跑成功...";
            GameStateManager.Instance.SetState(GameState.Playing);
        }
        animator.SetTrigger("Hide");
        skillManager.ClearSkillIcons();
        isHidden = false;
    }

    public void SetupSkillsForEnemy(string enemyName)
    {
        enemySkills = new List<System.Action>();

        if (enemyName == "FISH")
        {
            enemySkills.Add(() => enemyskill.Fishskill_1());
            enemySkills.Add(() => enemyskill.Fishskill_2());
        }
        else if(enemyName == "SNAKE")
        {
            enemySkills.Add(() => enemyskill.Snakeskill_1());
            enemySkills.Add(() => enemyskill.Snakeskill_2());
        }
        /*else if (enemyName == "Slime")
        {
            enemySkills.Add(() => enemySkillManager.SlimeSkill_1());
            // 加入更多 slime 專屬技能
        }*/

    }

}
