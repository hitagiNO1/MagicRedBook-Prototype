using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

using Random = UnityEngine.Random;

public class Enemyskill : MonoBehaviour
{
    public List<Unit> enemyUnits;
    public TextMeshProUGUI dialogueText;
    public List<Unit> playerUnits;
    public BuffDebuffManager buffDebuffManager;

    // �ޯ�N�o�r��]�Ҧ��ĤH�@�Ρ^
    private Dictionary<string, int> skillCooldowns = new Dictionary<string, int>();

    // ==================== �U�Ǫ��ޯ�N�o�ɶ� ====================
    private int skill2CooldownTurns = 3;           // �즳�]�i�����ΫO�d�^
    private int skill3CooldownTurns = 2;
    private int skill4CooldownTurns = 3;
    private int skill5CooldownTurns = 3;
    private int catskill2CooldownTurns = 3;
    private int catskill3CooldownTurns = 2;
    private int catskill4CooldownTurns = 3;
    private int catskill5CooldownTurns = 4;

    // DCAT �M�ΧN�o�ɶ�
    private int dcatskill2CooldownTurns = 2;
    private int dcatskill3CooldownTurns = 2;
    private int dcatskill4CooldownTurns = 3;

    // ==================== �D�n�����k ====================
    private bool IsSkillReady(string skillKey)
    {
        if (!skillCooldowns.ContainsKey(skillKey))
            skillCooldowns[skillKey] = 0;

        return skillCooldowns[skillKey] <= 0;
    }

    public IEnumerator ExecuteEnemySkillCoroutine(Unit enemyUnit)
    {
        if (enemyUnit == null)
        {
            Debug.LogError("�ĤH���L�ġI");
            yield break;
        }

        string enemyName = enemyUnit.originalUnitName;

        if (enemyName == "SOFT")
        {
            yield return StartCoroutine(ExecuteSoftSkillsCoroutine(enemyUnit));
        }
        else if (enemyName == "DCAT")
        {
            yield return StartCoroutine(ExecuteDcatSkillsCoroutine(enemyUnit));
        }
        else
        {
            var skills = GetEnemySkills(enemyName);
            if (skills.Count > 0)
            {
                int randomIndex = Random.Range(0, skills.Count);
                yield return StartCoroutine(skills[randomIndex](enemyUnit));
            }
        }
    }

    // ==================== SOFT �M�Χޯ����޿� ====================
    private IEnumerator ExecuteSoftSkillsCoroutine(Unit self)
    {
        // �i�Χޯ��
        var availableSkills = new List<Func<Unit, IEnumerator>>();

        string skill2Key = $"{self.unitName}_Softskill_2";
        string skill3Key = $"{self.unitName}_Softskill_3";

        if (IsSkillReady(skill2Key))
            availableSkills.Add(Softskill_2Coroutine);

        if (IsSkillReady(skill3Key))
            availableSkills.Add(Softskill_3Coroutine);

        //  �p�G������i�Χޯ� �� �H����@��
        if (availableSkills.Count > 0)
        {
            var chosenSkill = availableSkills[Random.Range(0, availableSkills.Count)];
            yield return StartCoroutine(chosenSkill(self));
            yield break;
        }

        //  �����N�o �� �ϥγƥΧޯ�
        Debug.Log($"{self.unitName} �Ҧ��ޯ�N�o���A�ϥ� Softskill_1");
        yield return StartCoroutine(PerformSoftskill_1(self));
    }

    // ==================== DCAT �M�Χޯ����޿� ====================
    private IEnumerator ExecuteDcatSkillsCoroutine(Unit self)
    {
        var availableSkills = new List<Func<Unit, IEnumerator>>();

        string skill2Key = $"{self.unitName}_Dcatskill_2";
        string skill3Key = $"{self.unitName}_Dcatskill_3";
        string skill4Key = $"{self.unitName}_Dcatskill_4";

        if (IsSkillReady(skill2Key))
            availableSkills.Add(Dcatskill_2Coroutine);

        if (IsSkillReady(skill3Key))
            availableSkills.Add(Dcatskill_3Coroutine);

        if (IsSkillReady(skill4Key))
            availableSkills.Add(Dcatskill_4Coroutine);

        //  ���ޯ�i�� �� �H����
        if (availableSkills.Count > 0)
        {
            var chosenSkill = availableSkills[Random.Range(0, availableSkills.Count)];
            yield return StartCoroutine(chosenSkill(self));
            yield break;
        }

        // �����N�o �� �ƥΧޯ�
        Debug.Log($"{self.unitName} �Ҧ��ޯ�N�o���A�ϥ� Dcatskill_1");
        yield return StartCoroutine(Dcatskill_1Coroutine(self));
    }

    // ==================== ���o�ޯ�C�� ====================
    public List<Func<Unit, IEnumerator>> GetEnemySkills(string enemyName)
    {
        var coroutines = new List<Func<Unit, IEnumerator>>();

        if (enemyName == "FISH")
        {
            coroutines.Add(PerformFishskill_1);
            coroutines.Add(PerformFishskill_2);
        }
        else if (enemyName == "SNAKE")
        {
            coroutines.Add(PerformSnakeskill_1);
            coroutines.Add(PerformSnakeskill_2);
        }
        else if (enemyName == "CLOCK")
        {
            coroutines.Add(PerformClockskill_1);
            coroutines.Add(PerformClockskill_2);
        }
        else if (enemyName == "SOFT")
        {
            coroutines.Add(PerformSoftskill_1);
            coroutines.Add(Softskill_2Coroutine);
            coroutines.Add(Softskill_3Coroutine);
        }
        else if (enemyName == "Death")
        {
            coroutines.Add(Dcatskill_1Coroutine);
            coroutines.Add(Dcatskill_2Coroutine);
            coroutines.Add(Dcatskill_3Coroutine);
            coroutines.Add(Dcatskill_4Coroutine);
        }
        else
        {
            Debug.LogWarning($"�������ĤH����: {enemyName}");
        }

        return coroutines;
    }

    // ==================== SOFT �ޯ�N�o�ˬd ====================
    private IEnumerator Softskill_2Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Softskill_2";
        if (!skillCooldowns.ContainsKey(skillKey)) skillCooldowns[skillKey] = 0;
        if (skillCooldowns[skillKey] > 0)
        {
            Debug.Log($"{self.unitName} �� Softskill_2 �N�o���A�Ѿl {skillCooldowns[skillKey]} �^�X");
            yield break;
        }
        yield return StartCoroutine(PerformSoftskill_2(self));
        skillCooldowns[skillKey] = 2;
    }

    private IEnumerator Softskill_3Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Softskill_3";
        if (!skillCooldowns.ContainsKey(skillKey)) skillCooldowns[skillKey] = 0;
        if (skillCooldowns[skillKey] > 0)
        {
            Debug.Log($"{self.unitName} �� Softskill_3 �N�o���A�Ѿl {skillCooldowns[skillKey]} �^�X");
            yield break;
        }
        yield return StartCoroutine(PerformSoftskill_3(self));
        skillCooldowns[skillKey] = 2;
    }

    // ==================== FISH �ޯ� ====================
    private IEnumerator PerformFishskill_1(Unit self)
    {
        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = self.damage;
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥX�F ���e���\n����⤤�e�l��V {target.unitName}\n" +
                               $"{target.unitName} ����F {actualDamageDealt} �I�ˮ`�I";
        }

        bool playLeft = target.unitName != "���ܧJ";
        EffectManager.Instance.PlayUIEffectOneSide("Fish1", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformFishskill_2(Unit self)
    {
        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = Mathf.RoundToInt(self.damage * 1.2f);
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥX�F ��������I\n�W�O�ਭ�A�Χ��ڥϦV {target.unitName}�C\n" +
                               $"{target.unitName} ����F {actualDamageDealt} �I�ˮ`�I";
        }

        bool playLeft = target.unitName != "���ܧJ";
        EffectManager.Instance.PlayUIEffectOneSide("Fish2", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    // ==================== SNAKE �ޯ� ====================
    private IEnumerator PerformSnakeskill_1(Unit self)
    {
        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = self.damage * 2;
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥΤF �r�r�r�I\n�j�f�r�V {target.unitName}�C\n" +
                               $"{target.unitName} ����F {actualDamageDealt} �I�ˮ`�I";
        }

        bool playLeft = target.unitName != "���ܧJ";
        EffectManager.Instance.PlayUIEffectOneSide("Snake1", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformSnakeskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;
        if (buffDebuffManager == null) yield break;

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        buffDebuffManager.ApplyPoison(target);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥX�F ���~�ѷ�I\n" +
                               $"�� {target.unitName} �Q�X�a�r�H�G�A {target.unitName} ���r�C\n" +
                               $"{target.unitName} �N�b���U�ӴX�^�X�������ˮ`...";
        }

        bool playLeft = target.unitName != "���ܧJ";
        EffectManager.Instance.PlayUIEffectOneSide("Snake2", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    // ==================== CLOCK �ޯ� ====================
    private IEnumerator PerformClockskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = self.damage * 2;
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥX�F ���Y�γܡI\n" +
                               $"���l�l�V�V {target.unitName}�I\n" +
                               $"{target.unitName} ����F {actualDamageDealt} �I�ˮ`�I";
        }

        bool playLeft = target.unitName != "���ܧJ";
        EffectManager.Instance.PlayUIEffectOneSide("Clock1", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformClockskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;
        if (buffDebuffManager == null) yield break;

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        buffDebuffManager.ApplyDamageBuff(target, 0.7f, 2);
        buffDebuffManager.ApplyHealDebuff(target, 2);

        int damage = Mathf.RoundToInt(self.damage * 1.5f);
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥX�F �a�a�a�I\n" +
                               $"�V���ۤv���U�A�o�X�n�i���� {target.unitName}�I\n" +
                               $"{target.unitName} ����F {actualDamageDealt} �I�ˮ`�A" +
                               "���U�� 2 �^�X�ˮ`�P�v���ĪG�j�T���C�I";
        }

        EffectManager.Instance.PlayUIEffect("Clock2", new Vector2(0, 0));
        yield return new WaitForSeconds(1.5f);
    }

    // ==================== SOFT �ޯ� ====================
    private IEnumerator PerformSoftskill_1(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = self.damage * 2;
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥX�F �n�n�����I\n" +
                               $"�� {target.unitName} �ϤF�@�ڴx�I\n" +
                               $"{target.unitName} ����F {actualDamageDealt} �I�ˮ`�I";
        }
        bool playLeft = target.unitName != "���ܧJ";
        EffectManager.Instance.PlayUIEffectOneSide("Blanket1", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformSoftskill_2(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;
        if (buffDebuffManager == null) yield break;

        List<Unit> validTargets = playerUnits.FindAll(unit => unit != null && unit.currentHP > 0);
        if (validTargets.Count == 0)
        {
            Debug.LogWarning("�L���Ī����a�ؼСI");
            yield break;
        }

        Unit target = validTargets[Random.Range(0, validTargets.Count)];

        buffDebuffManager.ApplySkillDisable(target, 2);

        int damage = Mathf.RoundToInt(self.damage * 0.5f);
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥX�F �n�n��¶�I\n" +
                               $"��� {target.unitName}�A {target.unitName} �ʼu���o�C\n" +
                               $"{target.unitName} ����F {actualDamageDealt} �I�ˮ`�A" +
                               "�åB�U�@�^�X�L�k�ϥΧޯ�I";
        }

        bool playLeft = target.unitName != "���ܧJ";
        EffectManager.Instance.PlayUIEffectOneSide("Blanket2", playLeft);
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator PerformSoftskill_3(Unit self)
    {
        if (playerUnits == null || playerUnits.Count == 0) yield break;

        int totalDamageDealt = 0;
        foreach (Unit target in playerUnits)
        {
            if (target == null || target.currentHP <= 0) continue;

            int damage = Mathf.RoundToInt(self.damage * 3f);
            int actualDamageDealt;
            bool isKilled = target.TakeDamage(damage, out actualDamageDealt);
            totalDamageDealt += actualDamageDealt;
        }

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥΤF �Ѷ½г����I\n" +
                               $"�ڤ����F�`�@ {totalDamageDealt} �I�ˮ`�I";
        }

        EffectManager.Instance.PlayUIEffect("Blanket3", new Vector2(0f, 0f));
        yield return new WaitForSeconds(1.5f);
    }

    // ==================== DCAT �ޯ� ====================
    private IEnumerator Dcatskill_1Coroutine(Unit self)
    {
        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int damage = Mathf.RoundToInt(self.damage * 2.5f);
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥX�F �����I\n" +
                               $"�� {target.unitName} �i������I\n" +
                               $"{target.unitName} ����F {actualDamageDealt} �I�ˮ`�I";
        }


        Debug.Log("DC1");
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator Dcatskill_2Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Dcatskill_2";
        if (!skillCooldowns.ContainsKey(skillKey))
            skillCooldowns[skillKey] = 0;

        if (skillCooldowns[skillKey] > 0)
        {
            Debug.Log($"{self.unitName} �� Dcatskill_2 �N�o���A�Ѿl {skillCooldowns[skillKey]} �^�X");
            yield break;
        }

        int totalAffected = 0;

        foreach (Unit target in playerUnits)
        {
            if (target == null || target.isDead || target.currentHP <= 0) continue;

            buffDebuffManager.ApplyPoison(target, 3);  // ��C�Ӧs�����a�I�[ 3 �^�X���r
            totalAffected++;
        }

        if (dialogueText != null)
        {
            if (totalAffected > 0)
            {
                dialogueText.text = $"{self.unitName} �ϥX�F ��𮧡I\n" +
                                   $"���`�����b�n�������I\n" ;
            }
        }


        Debug.Log("DC2");
        skillCooldowns[skillKey] = dcatskill2CooldownTurns;
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator Dcatskill_3Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Dcatskill_3";
        if (!skillCooldowns.ContainsKey(skillKey))
            skillCooldowns[skillKey] = 0;

        if (skillCooldowns[skillKey] > 0)
        {
            Debug.Log($"{self.unitName} �� Dcatskill_3 �N�o���A�Ѿl {skillCooldowns[skillKey]} �^�X");
            yield break;
        }

        Unit target = GetRandomValidTarget(playerUnits);
        if (target == null || target.isDead) yield break;

        int lostHP = target.maxHP - target.currentHP;
        int damage = Mathf.RoundToInt(lostHP * 0.75f);

        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);

        buffDebuffManager.ApplyDefenseBuff(self, 1.5f, 2);   // ���m +150%
        buffDebuffManager.ApplyDamageBuff(self, 2.0f, 2);    // ���� +200%

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥX�F �������@�I\n" +
                               $"{target.unitName} ����F {actualDamageDealt} �I�ˮ`�A\n" +
                               $"�H�H�H �����m�P�����j�T���ɡI";

        }

        bool playLeft = target.unitName != "���ܧJ";
        EffectManager.Instance.PlayUIEffectOneSide("Dcat3", playLeft);

        skillCooldowns[skillKey] = dcatskill3CooldownTurns;
        Debug.Log("DC3");
        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator Dcatskill_4Coroutine(Unit self)
    {
        string skillKey = $"{self.unitName}_Dcatskill_4";
        if (!skillCooldowns.ContainsKey(skillKey))
            skillCooldowns[skillKey] = 0;

        if (skillCooldowns[skillKey] > 0)
        {
            Debug.Log($"{self.unitName} �� Dcatskill_4 �N�o���A�Ѿl {skillCooldowns[skillKey]} �^�X");
            yield break;
        }

        int totalDamageDealt = 0;
        int damage = Mathf.RoundToInt(self.damage * 3.5f);

        foreach (Unit target in playerUnits)
        {
            if (target == null || target.currentHP <= 0) continue;

            int actualDamageDealt;
            bool isKilled = target.TakeDamage(damage, out actualDamageDealt);
            totalDamageDealt += actualDamageDealt;
        }

        if (dialogueText != null)
        {
            dialogueText.text = $"{self.unitName} �ϥX�F �����Aӥ�I\n" +
                               $"�W����q���@���A�¦V�������I\n" +
                               $"�������F�`�@ {totalDamageDealt} �I�ˮ`�I";
        }

        EffectManager.Instance.PlayUIEffect("Dcat4", new Vector2(0, 0));

        skillCooldowns[skillKey] = dcatskill4CooldownTurns;
        Debug.Log("DC4");
        yield return new WaitForSeconds(1.5f);
    }

    // ==================== �q�λ��U��k ====================
    private Unit GetRandomValidTarget(List<Unit> units)
    {
        List<Unit> validTargets = units.FindAll(unit => unit != null && unit.currentHP > 0);
        if (validTargets == null || validTargets.Count == 0)
        {
            return null;
        }
        int randomIndex = Random.Range(0, validTargets.Count);
        return validTargets[randomIndex];
    }

    private void PerformBasicAttack(Unit attacker, Unit target, int damage)
    {
        if (attacker == null || target == null || target.isDead)
        {
            Debug.LogWarning("�����ؼеL�ġA���L����");
            return;
        }
        int actualDamageDealt;
        bool isKilled = target.TakeDamage(damage, out actualDamageDealt);
    }

    public void ReduceCooldowns()
    {
        foreach (var skill in new List<string>(skillCooldowns.Keys))
        {
            if (skillCooldowns[skill] > 0)
            {
                skillCooldowns[skill]--;
                Debug.Log($"�ޯ� {skill} �N�o�Ѿl {skillCooldowns[skill]} �^�X");
            }
        }
    }
}