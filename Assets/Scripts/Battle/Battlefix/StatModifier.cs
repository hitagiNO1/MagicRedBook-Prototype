using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatModifier
{
    // 修改的數值 (例如 +10, 或者 +0.2)
    public float Value { get; private set; }

    // 修改的方式 (固定值、百分比加法、百分比乘法)
    public ModifierType Type { get; private set; }

    // 來源 (用來記錄是誰給了這個屬性，方便之後脫裝備或Buff結束時能準確移除)
    public object Source { get; private set; }

    // 建構子：當我們產生一個新的修飾者時，需要傳入這些資料
    public StatModifier(float value, ModifierType type, object source = null)
    {
        Value = value;
        Type = type;
        Source = source;
    }
}