# VolleyFire

<div align="center">

![VolleyFire](https://img.shields.io/badge/VolleyFire-3D%20STG-blue)
![Unity](https://img.shields.io/badge/Unity-2021.3+-black)
![License](https://img.shields.io/badge/License-MIT-green)

**一款 3D 宇宙彈幕射擊遊戲**

[遊戲概述](#遊戲概述) • [功能特色](#功能特色) • [快速開始](#快速開始) • [操作說明](#操作說明) • [專案結構](#專案結構)

</div>

---

## 遊戲概述

**VolleyFire** 是一款使用 Unity 引擎開發的 3D 宇宙射擊遊戲（Shoot 'em up / STG）。玩家將操控戰機在浩瀚的宇宙中與各種敵人戰鬥，體驗流暢的射擊手感、豐富的敵人 AI 和精彩的 Boss 戰。

### 核心玩法

- 🎮 **流暢的 3D 射擊體驗**：在 3D 空間中自由移動和射擊
- 🤖 **多樣化的敵人 AI**：每種敵人都有獨特的行為模式和攻擊方式
- 🎯 **Funnel 浮游炮系統**：召喚輔助武器協助戰鬥
- 📖 **豐富的劇情對話**：通過對話系統推進劇情
- 💥 **刺激的 Boss 戰**：挑戰強大的 Boss 敵人

---

## 功能特色

### ✨ 核心系統

- **玩家控制系統**
  - 流暢的 2D 平面移動（WASD/方向鍵）
  - 滑鼠瞄準射擊系統
  - 多發射點武器系統
  - 抓取系統（可控制特定物體）

- **敵人系統**
  - 多種敵人類型（飛行昆蟲、機器人、無人機、蓋亞等）
  - 複雜的 AI 行為模式
  - 波次系統管理敵人出場
  - **機器人（機動戰士）**：血量降低時會召喚浮游炮進行輔助攻擊

- **Funnel 浮游炮系統**
  - 多種狀態模式（待命、攻擊、啟動等）
  - 智能位置分配
  - 自動瞄準射擊
  - 由機器人 Boss 在戰鬥中召喚使用

- **波次與關卡系統**
  - 完整的波次流程管理
  - 對話系統整合
  - Boss 血條顯示
  - 重試系統

- **子彈系統**
  - 多種子彈類型（直線、追蹤、可控等）
  - 豐富的視覺效果

### 🎨 視覺與音效

- Bloom 後處理效果
- 傷害文字動畫
- 準心視覺反饋
- 多種音效和背景音樂

---

## 快速開始

### 系統要求

- **作業系統**：Windows 10/11
- **Unity 版本**：2021.3 LTS 或更高版本
- **硬體要求**：
  - CPU：Intel Core i5 或同等級
  - 記憶體：8 GB RAM
  - 顯示卡：支援 DirectX 11
  - 硬碟空間：2 GB 可用空間

### 安裝步驟

1. **克隆專案**
   ```bash
   git clone https://github.com/yourusername/VolleyFire.git
   cd VolleyFire
   ```

2. **開啟 Unity**
   - 使用 Unity Hub 開啟專案
   - 確保 Unity 版本為 2021.3 LTS 或更高

3. **載入場景**
   - 在 Unity 編輯器中開啟 `Assets/Scenes/Stage0.unity`
   - 點擊 Play 按鈕開始遊戲

### 開發環境設置

專案使用以下技術和工具：

- **Unity 引擎**：2021.3 LTS+
- **渲染管線**：Universal Render Pipeline (URP)
- **輸入系統**：Unity Input System
- **對話系統**：Yarn Spinner
- **程式語言**：C#

---

## 操作說明

### 基本操作

| 操作 | 按鍵 | 說明 |
|------|------|------|
| 移動 | **W/A/S/D** 或 **方向鍵** | 在 2D 平面內移動戰機 |
| 射擊 | **滑鼠左鍵**（按住） | 連續射擊，自動瞄準 |
| 瞄準 | **滑鼠移動** | 控制準心位置 |

### 遊戲機制

- **移動**：戰機在 X/Y 平面內移動，有邊界限制
- **射擊**：按住滑鼠左鍵可連續射擊，準心會自動瞄準敵人
- **準心反饋**：
  - 普通準心：無目標時
  - 精準目標：射線命中敵人（綠色）
  - 寬鬆目標：檢測盒命中敵人（黃色）

### 戰鬥提示

- 注意閃避敵人的攻擊
- **機器人 Boss**：當其血量降低時會召喚浮游炮，需要同時應對本體和浮游炮的攻擊
- Boss 戰時注意觀察血條和攻擊模式
- 善用對話系統了解劇情和提示

---

## 專案結構

```
VolleyFire/
├── Assets/
│   ├── Scripts/              # 遊戲腳本
│   │   ├── Player/          # 玩家相關腳本
│   │   ├── Enemy/           # 敵人相關腳本
│   │   ├── Funnel/          # Funnel 系統腳本
│   │   ├── Bullet/          # 子彈系統腳本
│   │   ├── Health/          # 生命值系統腳本
│   │   ├── StageSystem/     # 關卡系統腳本
│   │   ├── Dialogue/        # 對話系統腳本
│   │   └── ...
│   ├── Scenes/              # 遊戲場景
│   ├── Prefabs/             # 預製體
│   ├── Animations/          # 動畫文件
│   ├── Textures/            # 貼圖資源
│   ├── Audio/               # 音效資源
│   └── ...
├── ProjectSettings/         # Unity 專案設置
├── Packages/                # Unity 套件
├── GDD.md                   # 遊戲設計文件
└── README.md                # 本文件
```

### 主要腳本說明

詳細的腳本系統說明請參考：[`Assets/Scripts/README.md`](Assets/Scripts/README.md)

#### 核心腳本

- **PlayerController.cs**：玩家控制器，處理移動和射擊
- **WeaponSystem.cs**：武器系統，管理多發射點射擊
- **EnemyController.cs**：敵人控制器基類
- **EnemyWave.cs**：波次管理系統
- **FunnelSystem.cs**：Funnel 浮游炮系統
- **StageManager.cs**：關卡管理器

---

## 開發指南

### 添加新敵人

1. 創建新的腳本繼承 `EnemyBehavior`
2. 實現 `Tick()` 方法定義敵人行為
3. 在 Unity 編輯器中設置敵人預製體
4. 將敵人添加到波次中

範例：
```csharp
public class MyEnemyBehavior : EnemyBehavior
{
    public override void Tick()
    {
        // 實現敵人行為邏輯
    }
}
```

### 添加新子彈類型

1. 創建新的腳本繼承 `BulletBehavior`
2. 實現子彈的移動和碰撞邏輯
3. 創建子彈預製體並設置到武器系統

### 配置波次

1. 在場景中創建 `EnemyWave` GameObject
2. 添加敵人到 `enemies` 列表
3. 設置 `targetPosition` 目標位置
4. 配置對話和提示系統

---

## 已知問題

- 部分敵人的 AI 行為可能需要進一步優化
- 某些場景的效能可能需要優化

---

## 未來計劃

- [ ] 更多敵人類型和 Boss
- [ ] 武器升級系統
- [ ] 技能系統
- [ ] 成就系統
- [ ] 更多關卡和劇情
- [ ] 效能優化
- [ ] 專案腳本架構優化

---

## 貢獻指南

歡迎貢獻！請遵循以下步驟：

1. Fork 本專案
2. 創建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 開啟 Pull Request

### 程式碼規範

- 使用 C# 命名規範
- 添加適當的註釋
- 保持程式碼整潔和可讀性

---

## 授權

本專案採用 MIT 授權條款。詳見 [LICENSE](LICENSE) 文件。

---

## 聯絡方式

- **專案維護者**：VolleyFire 開發團隊
- **問題回報**：[GitHub Issues](https://github.com/yourusername/VolleyFire/issues)

---

## 致謝

感謝所有為本專案做出貢獻的開發者和測試者！

---

<div align="center">

**享受遊戲！** 🎮✨

Made with ❤️ by VolleyFire Team

</div>
