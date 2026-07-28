# 魔法紅書 (Magic Red Book) - 2.5D Side-Scrolling RPG Prototype


## 專案簡介 (Project Overview)
《魔法紅書》是一款以貓咪跑馬燈回憶為主題的 2.5D 橫向捲軸劇情 RPG。
玩家操作黑貓布萊克與萊菈，在奇幻世界中尋找失落的日記頁面，解開事件真相。

* **開發引擎**：Unity 2022 (URP)
* **專案角色**：Aedh 專案負責人 / 主程式 / 特效與資材整合
* **主要語言**：C#

---

## 專案架構與關鍵程式碼 (Code Structure)

```text
Assets/
 ├── Scripts/
 │    ├── Battle/          # 回合制戰鬥邏輯與數值計算
 │    ├── Editor/          # 自訂編輯器工具 (Handles 碰撞框)
 │    ├── UI/              # 紅書手帳介面與對話系統邏輯
 │    └── Utils/           # Z軸動態排序與通用工具
 └── Visuals/              # Shader Graph、特效與動畫
