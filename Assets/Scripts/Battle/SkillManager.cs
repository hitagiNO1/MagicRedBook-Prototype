using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillManager : MonoBehaviour
{
    public List<Unit> playerUnits; // 支援多玩家
    public List<Unit> enemyUnits;  // 支援多敵人
    public TextMeshProUGUI dialogueText;
    public List<BattleHUD> enemyHUDs; // 支援多敵人 HUD
    public BattleSystem battlesystem;
    public BuffDebuffManager buffDebuffManager;

    // 每個玩家的冷卻時間字典（key: skillName + playerIndex, value: remaining turns）
    private Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    private int skill2CooldownTurns = 3;
    private int skill3CooldownTurns = 2;
    private int skill4CooldownTurns = 3;
    private int skill5CooldownTurns = 2;
    private int catskill2CooldownTurns = 2;
    private int catskill3CooldownTurns = 1;
    private int catskill4CooldownTurns = 2;
    private int catskill5CooldownTurns = 3;

    public AudioSource audioSource;
    public GameObject skillIconPrefab;
    public Transform skillIconParent; // 單一共享的技能圖示父物件
    private List<GameObject> skillIcons = new List<GameObject>(); // 共享的圖示列表
    private Dictionary<int, List<System.Action>> skillQueues = new Dictionary<int, List<System.Action>>();
    private Dictionary<int, List<string>> selectedSkillNames = new Dictionary<int, List<string>>();
    private int maxSkillPerTurn = 3; // 兩個玩家共享的總上限

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        for (int i = 0; i < playerUnits.Count; i++)
        {
            skillQueues[i] = new List<System.Action>();
            selectedSkillNames[i] = new List<string>();
            // 初始化冷卻時間
            if (i == 0) // 假設 player0 是布萊克
            {
                skillCooldowns["Skill_2_0"] = 0;
                skillCooldowns["Skill_3_0"] = 0;
                skillCooldowns["Skill_4_0"] = 0;
                skillCooldowns["Skill_5_0"] = 0;
            }
            else if (i == 1) // player1 是萊菈
            {
                skillCooldowns["catSkill_2_1"] = 0;
                skillCooldowns["catSkill_3_1"] = 0;
                skillCooldowns["catSkill_4_1"] = 0;
                skillCooldowns["catSkill_5_1"] = 0;
            }
        }
    }

    // 技能方法，接受 playerIndex 和 target
    public void Skill_1(int playerIndex, Unit target)
    {
        if (!ValidateSkill(playerIndex, "Skill_1")) return;
        selectedSkillNames[playerIndex].Add("Skill_1");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("skill1");
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.2f);
            PerformBasicAttack(playerIndex, target, buffedDamage);
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 甩出手上的球攻擊 {target.unitName}";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 1";
        RemoveSkillIcon();
    }

    public void Skill_2(int playerIndex) // 全體攻擊，無需 target
    {
        string skillKey = "Skill_2_" + playerIndex;
        if (!ValidateSkill(playerIndex, "Skill_2") || skillCooldowns[skillKey] > 0) return;
        selectedSkillNames[playerIndex].Add("Skill_2");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("skill2");
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.5f);
            PerformMultiTargetAttack(playerIndex, buffedDamage);
            skillCooldowns[skillKey] = skill2CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 將手上的球分散甩出攻擊全部敵人";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 2";
        RemoveSkillIcon();
    }

    public void Skill_3(int playerIndex, Unit target)
    {
        string skillKey = "Skill_3_" + playerIndex;
        if (!ValidateSkill(playerIndex, "Skill_3") || skillCooldowns[skillKey] > 0) return;
        selectedSkillNames[playerIndex].Add("Skill_3");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("skill3");
            buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 1.5f, 1);
            skillCooldowns[skillKey] = skill3CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 將身上的毛整理好，使防禦力上升";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 3";
        RemoveSkillIcon();
    }

    public void Skill_4(int playerIndex, Unit target)
    {
        string skillKey = "Skill_4_" + playerIndex;
        if (!ValidateSkill(playerIndex, "Skill_4") || skillCooldowns[skillKey] > 0) return;
        selectedSkillNames[playerIndex].Add("Skill_4");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("Skill_4");
            buffDebuffManager.ApplyDefenseBuff(playerUnits[playerIndex], 2.0f, 2);
            skillCooldowns[skillKey] = skill4CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 緊縮成一團，使敵人找不到縫隙攻擊";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 4";
        RemoveSkillIcon();
    }

    public void Skill_5(int playerIndex, Unit target)
    {
        string skillKey = "Skill_5_" + playerIndex;
        if (!ValidateSkill(playerIndex, "Skill_5") || skillCooldowns[skillKey] > 0) return;
        selectedSkillNames[playerIndex].Add("Skill_5");
        skillQueues[playerIndex].Add(() =>
        {
            PlaySoundByName("skill4-1");
            PlaySoundByName("skill4-2");
            buffDebuffManager.ApplyDefenseDebuff(target, 0.7f, 2);
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1.3f);
            PerformBasicAttack(playerIndex, target, buffedDamage);
            skillCooldowns[skillKey] = skill5CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 抱住敵方，使出最強攻擊兔子踹！";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 5";
        RemoveSkillIcon();
    }

    public void catSkill_1(int playerIndex, Unit target)
    {
        if (!ValidateSkill(playerIndex, "catSkill_1")) return;
        selectedSkillNames[playerIndex].Add("catSkill_1");
        skillQueues[playerIndex].Add(() =>
        {
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
            PerformBasicAttack(playerIndex, target, buffedDamage);
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 撿起身邊的東西丟向 {target.unitName}";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 1";
        RemoveSkillIcon();
    }

    public void catSkill_2(int playerIndex, Unit target)
    {
        string skillKey = "catSkill_2_" + playerIndex;
        if (!ValidateSkill(playerIndex, "catSkill_2") || skillCooldowns[skillKey] > 0) return;
        selectedSkillNames[playerIndex].Add("catSkill_2");
        skillQueues[playerIndex].Add(() =>
        {
            buffDebuffManager.Heal(playerUnits[playerIndex], 0.75f);
            skillCooldowns[skillKey] = catskill2CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 將傷口進行包紮";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 2";
        RemoveSkillIcon();
    }

    public void catSkill_3(int playerIndex) // 全體敵人傷害
    {
        string skillKey = "catSkill_3_" + playerIndex;
        if (!ValidateSkill(playerIndex, "catSkill_3") || skillCooldowns[skillKey] > 0) return;
        selectedSkillNames[playerIndex].Add("catSkill_3");
        skillQueues[playerIndex].Add(() =>
        {
            int buffedDamage = Mathf.RoundToInt(playerUnits[playerIndex].damage * 1f);
            PerformMultiTargetAttack(playerIndex, buffedDamage);
            skillCooldowns[skillKey] = catskill3CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 透過唱歌給全體敵人造成傷害";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 3";
        RemoveSkillIcon();
    }

    public void catSkill_4(int playerIndex, Unit target)
    {
        string skillKey = "catSkill_4_" + playerIndex;
        if (!ValidateSkill(playerIndex, "catSkill_4") || skillCooldowns[skillKey] > 0) return;
        selectedSkillNames[playerIndex].Add("catSkill_4");
        skillQueues[playerIndex].Add(() =>
        {
            buffDebuffManager.ApplyDamageBuff(playerUnits[playerIndex], 2.0f, 2); // 攻擊力 * 2，持續 2 回合
            skillCooldowns[skillKey] = catskill4CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 施展魔法給予我方攻擊加成";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 4";
        RemoveSkillIcon();
    }

    public void catSkill_5(int playerIndex, Unit target)
    {
        string skillKey = "catSkill_5_" + playerIndex;
        if (!ValidateSkill(playerIndex, "catSkill_5") || skillCooldowns[skillKey] > 0) return;
        selectedSkillNames[playerIndex].Add("catSkill_5");
        skillQueues[playerIndex].Add(() =>
        {
            buffDebuffManager.ApplyFixedDamageReduction(playerUnits[playerIndex], 0.7f, 1);
            skillCooldowns[skillKey] = catskill5CooldownTurns;
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 透過歌聲振奮士氣,使我方承受傷害降低";
        });
        dialogueText.text = $"{playerUnits[playerIndex].unitName} 選擇了技能 5";
        RemoveSkillIcon();
    }

    private bool ValidateSkill(int playerIndex, string skillName)
    {
        if (selectedSkillNames[playerIndex].Contains(skillName))
        {
            int index = selectedSkillNames[playerIndex].IndexOf(skillName);
            selectedSkillNames[playerIndex].RemoveAt(index);
            skillQueues[playerIndex].RemoveAt(index);
            dialogueText.text = $"{playerUnits[playerIndex].unitName} 取消了 {skillName}";
            AddSkillIcon();
            return false;
        }
        if (GetTotalSelectedSkills() >= maxSkillPerTurn) return false;
        return true;
    }

    private int GetTotalSelectedSkills()
    {
        int total = 0;
        foreach (var queue in skillQueues.Values)
        {
            total += queue.Count;
        }
        return total;
    }

    public void OnConfirmSkillQueue(int playerIndex)
    {
        if (skillQueues[playerIndex].Count == 0) return;
        StartCoroutine(ExecuteSkillQueue(playerIndex));
        battlesystem.OnPlayerConfirmSkillQueue(playerIndex);
    }

    IEnumerator ExecuteSkillQueue(int playerIndex)
    {
        foreach (var skillAction in skillQueues[playerIndex])
        {
            skillAction.Invoke();
            yield return new WaitForSeconds(1f);
        }
        skillQueues[playerIndex].Clear();
        selectedSkillNames[playerIndex].Clear();
    }

    void PerformBasicAttack(int playerIndex, Unit target, int damageOverride = -1)
    {
        int actualDamage = damageOverride > 0 ? damageOverride : playerUnits[playerIndex].damage;
        bool isDead = target.TakeDamage(actualDamage);
        int targetIndex = enemyUnits.IndexOf(target);
        if (targetIndex >= 0 && targetIndex < enemyHUDs.Count)
        {
            enemyHUDs[targetIndex].SetHP(target.currentHP);
        }
        if (isDead && AllEnemiesDead())
        {
            battlesystem.state = BattleState.WON;
            battlesystem.EndBattle();
        }
    }

    void PerformMultiTargetAttack(int playerIndex, int damage)
    {
        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.currentHP > 0)
            {
                bool isDead = enemy.TakeDamage(damage);
                int targetIndex = enemyUnits.IndexOf(enemy);
                if (targetIndex >= 0 && targetIndex < enemyHUDs.Count)
                {
                    enemyHUDs[targetIndex].SetHP(enemy.currentHP);
                }
                if (isDead && AllEnemiesDead())
                {
                    battlesystem.state = BattleState.WON;
                    battlesystem.EndBattle();
                }
            }
        }
    }

    private bool AllEnemiesDead()
    {
        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.currentHP > 0) return false;
        }
        return true;
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
        if (GetTotalSelectedSkills() < maxSkillPerTurn)
        {
            GameObject icon = Instantiate(skillIconPrefab, skillIconParent);
            skillIcons.Add(icon);
        }
    }

    public void ReduceCooldown()
    {
        foreach (var key in new List<string>(skillCooldowns.Keys))
        {
            if (skillCooldowns[key] > 0) skillCooldowns[key]--;
        }
    }

    void ResetSkillIcons()
    {
        foreach (GameObject icon in skillIcons)
        {
            Destroy(icon);
        }
        skillIcons.Clear();

        // 初始化為 maxSkillPerTurn（共享）
        for (int i = 0; i < maxSkillPerTurn; i++)
        {
            GameObject icon = Instantiate(skillIconPrefab, skillIconParent);
            skillIcons.Add(icon);
        }
    }

    public void InitSkillIcons()
    {
        ResetSkillIcons();
    }

    public IEnumerator DelayedInitSkillIcons(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        InitSkillIcons();
    }

    public void ClearSkillIcons()
    {
        foreach (GameObject icon in skillIcons)
        {
            Destroy(icon);
        }
        skillIcons.Clear();
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
}