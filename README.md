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
 │    ├── Controllers/     # 各種控制器，角色、CG、鏡頭、動畫等等
 │    ├── Entities/        # 自定義腳本，Asset、 Scriptable Object
 │    ├── Others/          # 其他小工具程式
 │    ├── QTE/             # 小遊戲程式
 │    ├── RedBook/         # 紅書UI介面與相關控制
 │    ├── Story/           # 劇情管理、播放控制
 │    ├── TapeGame/        # 小遊戲程式
 │    └── donked/          # 小遊戲程式

## `Battle`
- `Battle/BattleHUD.cs`：UI 顯示與互動
- `Battle/BattleSystem.cs`：戰鬥流程邏輯
- `Battle/BuffDebuffManager.cs`：集中管理流程
- `Battle/CSVLoader.cs`：戰鬥流程邏輯
- `Battle/DefenseData.cs`：資料定義（SO）
- `Battle/Enemyskill.cs`：敵人行為控制
- `Battle/EquipmentButtonUI.cs`：UI 顯示與互動
- `Battle/EquipmentManager.cs`：集中管理流程
- `Battle/EquipmentSlot.cs`：UI 顯示與互動
- `Battle/Itemuse.cs`：道具處理
- `Battle/mutiplebattle.cs`：戰鬥流程邏輯
- `Battle/SkillManager.cs`：集中管理流程
- `Battle/TeleportTrigger.cs`：觸發條件與切換
- `Battle/Unit.cs`：戰鬥流程邏輯

## `Controllers`
- `Controllers/BGMManager.cs`：集中管理流程
- `Controllers/CGManager.cs`：集中管理流程
- `Controllers/CGTrigger.cs`：觸發條件與切換
- `Controllers/CharControlles.cs`：控制輸入/行為
- `Controllers/CharControllesFORCH2.cs`：控制輸入/行為
- `Controllers/EffectManager.cs`：集中管理流程
- `Controllers/FollowObject.cs`：跨系統控制邏輯
- `Controllers/GameStateManager.cs`：集中管理流程
- `Controllers/ItemPickup.cs`：道具處理
- `Controllers/MainMenuController.cs`：控制輸入/行為
- `Controllers/MainMenuManager.cs`：集中管理流程
- `Controllers/MapTrigger.cs`：觸發條件與切換
- `Controllers/MiniMapController.cs`：控制輸入/行為
- `Controllers/PagePickup.cs`：跨系統控制邏輯
- `Controllers/SceneLoadTrigger.cs`：觸發條件與切換
- `Controllers/SceneLoadTrigger2D.cs`：觸發條件與切換
- `Controllers/StoryCamera.cs`：鏡頭相關控制
- `Controllers/TimelineManager.cs`：集中管理流程
- `Controllers/Tree.cs`：跨系統控制邏輯
- `Controllers/TreeManager.cs`：集中管理流程

## `Entities`
- `Entities/CGContent.cs`：資料/模型邏輯
- `Entities/ChooseScene.cs`：場景切換流程
- `Entities/EquipmentData.cs`：資料定義（SO）
- `Entities/EquipmentSlotUI.cs`：UI 顯示與互動
- `Entities/ItemData.cs`：資料定義（SO）
- `Entities/MonsterData.cs`：資料定義（SO）
- `Entities/PageCgData.cs`：資料定義（SO）
- `Entities/SceneSwapper.cs`：場景切換流程
- `Entities/Speaker.cs`：資料定義（SO）
- `Entities/StoryScene.cs`：資料定義（SO）

## `Others`
- `Others/AnimEvent.cs`：通用輔助邏輯
- `Others/BloomFlicker.cs`：通用輔助邏輯
- `Others/CamZone.cs`：鏡頭相關控制
- `Others/CamZoneLyla.cs`：鏡頭相關控制
- `Others/CursorController.cs`：控制輸入/行為
- `Others/DynamicSortingFORCAT.cs`：通用輔助邏輯
- `Others/DynamicSortingOrder.cs`：通用輔助邏輯
- `Others/FireLightFlicker.cs`：通用輔助邏輯
- `Others/FloatingUpDown.cs`：通用輔助邏輯
- `Others/FloatingUpDownZ.cs`：通用輔助邏輯
- `Others/FollowAI.cs`：通用輔助邏輯
- `Others/GameEndTrigger.cs`：觸發條件與切換
- `Others/InvertMask.cs`：通用輔助邏輯
- `Others/RescueStoryManager.cs`：集中管理流程
- `Others/ScenePortal.cs`：場景切換流程
- `Others/SmoothFollow.cs`：通用輔助邏輯
- `Others/SpineColorControl.cs`：通用輔助邏輯
- `Others/TextureSwitcher.cs`：通用輔助邏輯
- `Others/ToCGScene.cs`：場景切換流程
- `Others/TriggerToggle2D.cs`：觸發條件與切換

## `QTE`
- `QTE/canclick.cs`：QTE 小遊戲邏輯
- `QTE/Cans.cs`：QTE 小遊戲邏輯
- `QTE/GameFlowManager.cs`：集中管理流程
- `QTE/Interactable.cs`：QTE 小遊戲邏輯
- `QTE/locationstore.cs`：QTE 小遊戲邏輯
- `QTE/QTEUI.cs`：UI 顯示與互動
- `QTE/RestorePlayerPosition.cs`：玩家行為控制
- `QTE/shotcan.cs`：QTE 小遊戲邏輯
- `QTE/Tile.cs`：QTE 小遊戲邏輯

## `RedBook`
- `RedBook/CharStateUI.cs`：UI 顯示與互動
- `RedBook/GoalController.cs`：控制輸入/行為
- `RedBook/InventoryManager.cs`：集中管理流程
- `RedBook/InventoryUI.cs`：UI 顯示與互動
- `RedBook/ItemSlot.cs`：道具處理
- `RedBook/MonsterDexManager.cs`：集中管理流程
- `RedBook/MonsterEntryUI.cs`：UI 顯示與互動
- `RedBook/PageCGUI.cs`：UI 顯示與互動
- `RedBook/RedBook.cs`：圖鑑/書本功能
- `RedBook/RedBookMain.cs`：圖鑑/書本功能
- `RedBook/TalkingBox.cs`：圖鑑/背包邏輯

## `Story`
- `Story/AfterBattleStory.cs`：劇情流程邏輯
- `Story/AfterBattleStoryDcat.cs`：劇情流程邏輯
- `Story/AfterBattleTL.cs`：劇情流程邏輯
- `Story/BottomBarController.cs`：控制輸入/行為
- `Story/CGSprite.cs`：劇情流程邏輯
- `Story/CGText.cs`：劇情流程邏輯
- `Story/CGTextEND.cs`：劇情流程邏輯
- `Story/FirstText.cs`：劇情流程邏輯
- `Story/GameController.cs`：控制輸入/行為
- `Story/SpriteController.cs`：控制輸入/行為
- `Story/SpriteSwitcher.cs`：劇情流程邏輯
- `Story/StoryGate.cs`：劇情流程邏輯
- `Story/StoryGate2.cs`：劇情流程邏輯
- `Story/StoryNMangaTrigger.cs`：觸發條件與切換
- `Story/StoryTrigger.cs`：觸發條件與切換
- `Story/StoryTrigger1.cs`：觸發條件與切換
- `Story/StoryTrigger2.cs`：觸發條件與切換
- `Story/StoryTrigger2D.cs`：觸發條件與切換
- `Story/StoryTriggerE.cs`：觸發條件與切換
- `Story/StoryWatcher.cs`：劇情流程邏輯

## `TapeGame`
- `TapeGame/Enemy.cs`：敵人行為控制
- `TapeGame/EnemySpawner.cs`：物件生成控制
- `TapeGame/GameManager.cs`：集中管理流程
- `TapeGame/InfiniteGround.cs`：Tape 小遊戲邏輯
- `TapeGame/PlayerController.cs`：控制輸入/行為
- `TapeGame/PlayerRunner.cs`：玩家行為控制
- `TapeGame/WorldMover.cs`：Tape 小遊戲邏輯

## `donked`
- `donked/BackgroundFitScreen.cs`：打地鼠小遊戲邏輯
- `donked/boxgameflow.cs`：打地鼠小遊戲邏輯
- `donked/HoleState.cs`：打地鼠小遊戲邏輯
- `donked/mouseborn.cs`：打地鼠小遊戲邏輯
- `donked/mouseset.cs`：打地鼠小遊戲邏輯
- `donked/usercontrol.cs`：打地鼠小遊戲邏輯

```

License & Copyright

© 2026 Aedh (吳杰霖). All rights reserved.

本專案作品（包含程式碼、美術資材、文件）僅供個人作品集展示與面試審查使用，未經許可禁止任何商業用途、二次修改或轉載。
