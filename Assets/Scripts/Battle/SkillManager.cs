using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillManager : MonoBehaviour
{
    public Unit playerUnit;
    public Unit enemyUnit;
    public TextMeshProUGUI dialogueText;
    public BattleHUD enemyHUD;
    public BattleSystem battlesystem;
    public BuffDebuffManager buffDebuffManager;

    private int skill2CooldownTurns = 3;
    private int skill2CooldownRemaining = 0;
    private int skill3CooldownTurns = 2;
    private int skill3CooldownRemaining = 0;
    private int skill4CooldownTurns = 3;
    private int skill4CooldownRemaining = 0;
    private int skill5CooldownTurns = 2;
    private int skill5CooldownRemaining = 0;
    private int catskill2CooldownTurns = 2;
    private int catskill2CooldownRemaining = 0;
    private int catskill3CooldownTurns = 1;
    private int catskill3CooldownRemaining = 0;
    private int catskill4CooldownTurns = 2;
    private int catskill4CooldownRemaining = 0;
    private int catskill5CooldownTurns = 3;
    private int catskill5CooldownRemaining = 0;

    public AudioSource audioSource;
    public GameObject skillIconPrefab;      // 技能圖示 Prefab
    public Transform skillIconParent;       // UI 父物件（例如 Horizontal Layout）
    private List<GameObject> skillIcons = new List<GameObject>(); // 儲存目前顯示的圖示
    private List<System.Action> skillQueue = new List<System.Action>();
    private int maxSkillPerTurn = 3;
    private List<string> selectedSkillNames = new List<string>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();


    }

    public void Update()
    {

    }

    public void Skill_1()
    {
        if (selectedSkillNames.Contains("Skill_1"))
        {
            // 移除技能
            int index = selectedSkillNames.IndexOf("Skill_1");
            selectedSkillNames.RemoveAt(index);
            skillQueue.RemoveAt(index);
            dialogueText.text = "取消了技能 1";
            AddSkillIcon();
            return;
        }
        if (skillQueue.Count >= maxSkillPerTurn) return;
        selectedSkillNames.Add("Skill_1");
        skillQueue.Add(() =>
        {
            PlaySoundByName("skill1");
            int buffedDamage = Mathf.RoundToInt(playerUnit.damage * 1.2f);
            PerformBasicAttack(buffedDamage);

            dialogueText.text = "布萊克甩出手上的球攻擊一名敵人";
        });
        dialogueText.text = "選擇了技能 1";
        RemoveSkillIcon();
    }

    public void Skill_2()
    {
        if (selectedSkillNames.Contains("Skill_2"))
        {
            int index = selectedSkillNames.IndexOf("Skill_2");
            selectedSkillNames.RemoveAt(index);
            skillQueue.RemoveAt(index);
            dialogueText.text = "取消了技能 2";
            AddSkillIcon();
            return;
        }
        if (skillQueue.Count >= maxSkillPerTurn || skill2CooldownRemaining > 0) return;
        selectedSkillNames.Add("Skill_2");
        skillQueue.Add(() =>
        {
            PlaySoundByName("skill2");
            int buffedDamage = Mathf.RoundToInt(playerUnit.damage * 1.5f);
            PerformBasicAttack(buffedDamage);
            skill2CooldownRemaining = skill2CooldownTurns;

            dialogueText.text = "布萊克將手上的球分散甩出攻擊全部敵人";
        });
        dialogueText.text = "選擇了技能 2";
        RemoveSkillIcon();
    }

    public void Skill_3()
    {
        if (selectedSkillNames.Contains("Skill_3"))
        {
            int index = selectedSkillNames.IndexOf("Skill_3");
            selectedSkillNames.RemoveAt(index);
            skillQueue.RemoveAt(index);
            dialogueText.text = "取消了技能 3";
            AddSkillIcon();
            return;
        }
        if (skillQueue.Count >= maxSkillPerTurn || skill3CooldownRemaining > 0) return;
        selectedSkillNames.Add("Skill_3");
        skillQueue.Add(() =>
        {
            PlaySoundByName("skill3");
            buffDebuffManager.ApplyDefenseBuff(playerUnit, 1.5f, 1);
            skill3CooldownRemaining = skill3CooldownTurns;

            dialogueText.text = "布萊克將身上的毛整理好，使防禦力上升";
        });
        dialogueText.text = "選擇了技能 3";
        RemoveSkillIcon();
    }

    public void Skill_4()
    {
        if (selectedSkillNames.Contains("Skill_4"))
        {
            int index = selectedSkillNames.IndexOf("Skill_4");
            selectedSkillNames.RemoveAt(index);
            skillQueue.RemoveAt(index);
            dialogueText.text = "取消了技能 4";
            AddSkillIcon();
            return;
        }
        if (skillQueue.Count >= maxSkillPerTurn || skill5CooldownRemaining > 0) return;
        selectedSkillNames.Add("Skill_4");
        skillQueue.Add(() =>
        {
            PlaySoundByName("Skill_4");
            buffDebuffManager.ApplyDefenseBuff(playerUnit, 2.0f, 2);  // 增加 200%，持續 2 回合
            skill5CooldownRemaining = skill5CooldownTurns;

            dialogueText.text = "布萊克緊縮成一團，使敵人找不到縫隙攻擊";
        });
        dialogueText.text = "選擇了技能 4";
        RemoveSkillIcon();
    }

    public void Skill_5()
    {
        if (selectedSkillNames.Contains("Skill_5"))
        {
            int index = selectedSkillNames.IndexOf("Skill_5");
            selectedSkillNames.RemoveAt(index);
            skillQueue.RemoveAt(index);
            dialogueText.text = "取消了技能 5";
            AddSkillIcon();
            return;
        }
        if (skillQueue.Count >= maxSkillPerTurn || skill4CooldownRemaining > 0) return;
        selectedSkillNames.Add("Skill_5");
        skillQueue.Add(() =>
        {
            PlaySoundByName("skill4-1");
            PlaySoundByName("skill4-2");
            buffDebuffManager.ApplyDefenseDebuff(enemyUnit, 0.7f, 2);
            int buffedDamage = Mathf.RoundToInt(playerUnit.damage * 1.3f);
            PerformBasicAttack(buffedDamage);
            skill4CooldownRemaining = skill4CooldownTurns;

            dialogueText.text = "布萊克抱住敵方，使出最強攻擊兔子踹！";
        });
        dialogueText.text = "選擇了技能 5";
        RemoveSkillIcon();
    }

    public void catSkill_1()
    {
        if (selectedSkillNames.Contains("catSkill_1"))
        {
            // 移除技能
            int index = selectedSkillNames.IndexOf("catSkill_1");
            selectedSkillNames.RemoveAt(index);
            skillQueue.RemoveAt(index);
            dialogueText.text = "取消了技能 1";
            AddSkillIcon();
            return;
        }
        if (skillQueue.Count >= maxSkillPerTurn) return;
        selectedSkillNames.Add("catSkill_1");
        skillQueue.Add(() =>
        {
           // PlaySoundByName("skill1");
            int buffedDamage = Mathf.RoundToInt(playerUnit.damage * 1f);
            PerformBasicAttack(buffedDamage);

            dialogueText.text = "萊菈撿起身邊的東西丟向敵人";
        });
        dialogueText.text = "選擇了技能 1";
        RemoveSkillIcon();
    }

    public void catSkill_2()
    {
        if (selectedSkillNames.Contains("catSkill_2"))
        {
            int index = selectedSkillNames.IndexOf("catSkill_2");
            selectedSkillNames.RemoveAt(index);
            skillQueue.RemoveAt(index);
            dialogueText.text = "取消了技能 2";
            AddSkillIcon();
            return;
        }
        if (skillQueue.Count >= maxSkillPerTurn || catskill2CooldownRemaining > 0) return;
        selectedSkillNames.Add("catSkill_2");
        skillQueue.Add(() =>
        {
            //PlaySoundByName("skill2");
            buffDebuffManager.Heal(playerUnit, 0.75f);
            catskill2CooldownRemaining = catskill2CooldownTurns;

            dialogueText.text = "萊菈將傷口進行包紮";
        });
        dialogueText.text = "選擇了技能 2";
        RemoveSkillIcon();
    }

    public void catSkill_3()
    {
        if (selectedSkillNames.Contains("catSkill_3"))
        {
            int index = selectedSkillNames.IndexOf("catSkill_3");
            selectedSkillNames.RemoveAt(index);
            skillQueue.RemoveAt(index);
            dialogueText.text = "取消了技能 3";
            AddSkillIcon();
            return;
        }
        if (skillQueue.Count >= maxSkillPerTurn || catskill3CooldownRemaining > 0) return;
        selectedSkillNames.Add("catSkill_3");
        skillQueue.Add(() =>
        {
           // PlaySoundByName("skill3");
            buffDebuffManager.ApplyDefenseBuff(playerUnit, 1.5f, 1);
            catskill3CooldownRemaining = catskill3CooldownTurns;

            dialogueText.text = "萊菈透過唱歌給全體敵人造成傷害";
        });
        dialogueText.text = "選擇了技能 3";
        RemoveSkillIcon();
    }

    public void catSkill_4()
    {
        if (selectedSkillNames.Contains("catSkill_4"))
        {
            int index = selectedSkillNames.IndexOf("catSkill_4");
            selectedSkillNames.RemoveAt(index);
            skillQueue.RemoveAt(index);
            dialogueText.text = "取消了技能 4";
            AddSkillIcon();
            return;
        }
        if (skillQueue.Count >= maxSkillPerTurn || catskill4CooldownRemaining > 0) return;
        selectedSkillNames.Add("catSkill_4");
        skillQueue.Add(() =>
        {
            //PlaySoundByName("catSkill_4");
            buffDebuffManager.ApplyDefenseBuff(playerUnit, 2.0f, 2);  // 增加 200%，持續 2 回合
            catskill5CooldownRemaining = catskill5CooldownTurns;

            dialogueText.text = "萊菈施展魔法給予我方的攻擊加成";
        });
        dialogueText.text = "選擇了技能 4";
        RemoveSkillIcon();
    }

    public void catSkill_5()
    {
        if (selectedSkillNames.Contains("catSkill_5"))
        {
            int index = selectedSkillNames.IndexOf("catSkill_5");
            selectedSkillNames.RemoveAt(index);
            skillQueue.RemoveAt(index);
            dialogueText.text = "取消了技能 5";
            AddSkillIcon();
            return;
        }
        if (skillQueue.Count >= maxSkillPerTurn || catskill5CooldownRemaining > 0) return;
        selectedSkillNames.Add("catSkill_5");
        skillQueue.Add(() =>
        {
            //PlaySoundByName("skill4-1");
            //PlaySoundByName("skill4-2");
            buffDebuffManager.ApplyFixedDamageReduction(playerUnit, 0.7f, 1); // 減少 70% 傷害，觸發 1 次
            catskill5CooldownRemaining = catskill5CooldownTurns;

            dialogueText.text = "萊菈透過歌聲振奮士氣,使我方承受傷害降低";
        });
        dialogueText.text = "選擇了技能 5";
        RemoveSkillIcon();
    }

    public void ReduceCooldown()
    {
        if (skill2CooldownRemaining > 0) skill2CooldownRemaining--;
        if (skill3CooldownRemaining > 0) skill3CooldownRemaining--;
        if (skill4CooldownRemaining > 0) skill4CooldownRemaining--;
        if (skill5CooldownRemaining > 0) skill5CooldownRemaining--;
        if (catskill2CooldownRemaining > 0) catskill2CooldownRemaining--;
        if (catskill3CooldownRemaining > 0) catskill3CooldownRemaining--;
        if (catskill4CooldownRemaining > 0) catskill4CooldownRemaining--;
        if (catskill5CooldownRemaining > 0) catskill5CooldownRemaining--;
    }

    public void OnConfirmSkillQueue()
    {
        if (skillQueue.Count == 0) return;
        StartCoroutine(ExecuteSkillQueue());
    }

    IEnumerator ExecuteSkillQueue()
    {
        foreach (var skillAction in skillQueue)
        {
            skillAction.Invoke();
            yield return new WaitForSeconds(1f);
        }

        skillQueue.Clear();
        selectedSkillNames.Clear();
        ReduceCooldown();
        yield return new WaitForSeconds(0.5f);

        if (battlesystem.state == BattleState.WON || battlesystem.state == BattleState.LOST)
        {
            yield break; // 不要再進行敵人回合   
        }

        // 進入敵人回合
        battlesystem.turnCounter++;
        battlesystem.state = BattleState.ENEMYTURN;
        StartCoroutine(battlesystem.EnemyTurn());
    }

    void PerformBasicAttack(int damageOverride = -1)
    {
        int actualDamage = (damageOverride > 0) ? damageOverride : playerUnit.damage;
        bool isDead = enemyUnit.TakeDamage(actualDamage);
        enemyHUD.SetHP(enemyUnit.currentHP);

        if (isDead)
        {
            battlesystem.state = BattleState.WON;
            battlesystem.EndBattle();
        }
    }

    void RemoveSkillIcon()
    {
        if (skillIcons.Count > 0)
        {
            GameObject lastIcon = skillIcons[skillIcons.Count - 1];
            skillIcons.RemoveAt(skillIcons.Count - 1);
            Destroy(lastIcon);
        }
    }

    void AddSkillIcon()
    {
        if (skillIcons.Count < maxSkillPerTurn)
        {
            GameObject icon = Instantiate(skillIconPrefab, skillIconParent);
            skillIcons.Add(icon);
        }
    }

    public void PlaySoundByName(string soundName)
    {
        AudioClip clip = Resources.Load<AudioClip>("soundeffects/" + soundName);
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning("音效找不到: " + soundName);
        }
    }

    void ResetSkillIcons()
    {
        // 先刪除現有圖示
        foreach (GameObject icon in skillIcons)
        {
            Destroy(icon);
        }
        skillIcons.Clear();

        // 重新生成圖示
        for (int i = 0; i < maxSkillPerTurn; i++)
        {
            GameObject icon = Instantiate(skillIconPrefab, skillIconParent);
            skillIcons.Add(icon);
        }
    }

    public void InitSkillIcons()
    {
        ResetSkillIcons(); // 這個方法你已經定義好會清空並重建圖示
    }
    public IEnumerator DelayedInitSkillIcons(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        ResetSkillIcons();
    }

    public void ClearSkillIcons()
    {
        foreach (GameObject icon in skillIcons)
        {
            Destroy(icon);
        }
        skillIcons.Clear();
    }


}