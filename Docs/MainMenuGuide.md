# 主界面说明文档（Phase 3）

本文档描述游戏主界面（`MainMenu.unity`）的架构、脚本职责、按钮行为、场景搭建与测试方式。场景流与转场相关内容见 [SceneFlowGuide.md](SceneFlowGuide.md)。

---

## 一、概述与已确认规则

| 项 | 决定 |
|----|------|
| 存档 | 本地 `save.json`，仅关卡进度（`hasSave` / `currentLevelIndex` / `completedLevelIndices`） |
| 进入关卡 | 一律使用场景**初始位置**，不恢复点位 |
| 继续按钮 | **始终显示**；无存档时灰色不可点（`CanContinue` = `hasSave`） |
| 关卡数 | 4 关，场景名 `level1`–`level4` |
| 解锁 | 线性：第 1 关默认解锁，通关第 N 关解锁第 N+1 关 |
| UI 组织 | uGUI + **Controller + View**；代码不生成界面，美术在 Editor 摆 Canvas 并拖引用 |
| 星级 | 后移为扩展 Phase；当前仅占位 API，成就页暂不显示真实星级 |
| 接口层 | **无** `IMenuPanel` / `IOverwriteSaveDialog`；直接使用具体 View 类 |

### Phase 3 完成状态

- **代码**：已完成（Controller、View、覆盖弹窗、角色轮播）
- **场景骨架**：`MainMenu.unity` 已有系统节点与 Canvas 占位
- **待美术/Editor**：6 个菜单 Button、弹窗 UI 与 Inspector 引用尚未配齐（见 [第九节](#九待美术editor-完成项)）

---

## 二、架构

```mermaid
flowchart TB
    subgraph scene [MainMenu.unity]
        Systems[MainMenuSystems]
        Canvas[MainMenuCanvas]
        Dialog[OverwriteSaveDialog]
    end

    subgraph scripts [脚本]
        Controller[MainMenuController]
        View[MainMenuView]
        Overwrite[OverwriteSaveDialogView]
        Carousel[CharacterCarousel]
    end

    subgraph managers [运行时 Manager]
        Save[SaveManager]
        Flow[SceneFlowManager]
        DB[LevelDatabaseSO]
    end

    Systems --> Controller
    Canvas --> View
    Canvas --> Carousel
    Dialog --> Overwrite

    Controller --> View
    Controller --> Overwrite
    Controller --> Save
    Controller --> Flow
    Controller --> DB
    View --> Carousel
```

**分工原则：**

- `MainMenuController`：存档逻辑、场景跳转、弹窗事件订阅，不持有具体 UI 节点
- `MainMenuView`：持有 Button 引用，运行时 `Bind(controller)` 注册点击
- `OverwriteSaveDialogView`：覆盖存档确认 UI，由 Controller 调用 `ShowForNewGame` / `ShowForLevelSelect`
- `CharacterCarousel`：展示区角色图轮播，数据来自 `LevelDatabase` 或手动 Sprite 列表

---

## 三、脚本职责

| 文件 | 职责 |
|------|------|
| [`MainMenuController.cs`](../Assets/_Game/Scripts/UI/MainMenuController.cs) | 菜单按钮回调、继续按钮刷新、`RequestEnterLevel`、Manager 初始化、弹窗确认后跳转 |
| [`MainMenuView.cs`](../Assets/_Game/Scripts/UI/MainMenuView.cs) | 6 个 Button 绑定、`SetContinueEnabled`、轮播初始化、引用校验 |
| [`OverwriteSaveDialogView.cs`](../Assets/_Game/Scripts/UI/OverwriteSaveDialogView.cs) | 覆盖存档弹窗显隐、动态/固定文案、确认/取消 |
| [`CharacterCarousel.cs`](../Assets/_Game/Scripts/UI/CharacterCarousel.cs) | 每 2 秒轮播角色图；`manualSprites` 优先，否则读 `LevelDatabase.characterSprite` |

**依赖的核心系统（Phase 1–2 已完成）：**

| 文件 | 用途 |
|------|------|
| [`SaveManager.cs`](../Assets/_Game/Scripts/Core/SaveManager.cs) | 存档读写、`CanContinue`、`IsLevelUnlocked`、星级占位 API |
| [`SceneFlowManager.cs`](../Assets/_Game/Scripts/Core/SceneFlowManager.cs) | 场景加载与转场 |
| [`LevelDatabase.asset`](../Assets/_Game/Data/ScriptableObjects/LevelDatabase.asset) | 4 关配置、`displayName`、角色/背景图 |

---

## 四、六个菜单按钮行为

| 按钮 | 方法 | 行为 |
|------|------|------|
| 继续游戏 | `OnContinueClicked` | `CanContinue` 为 false 时不响应；否则 `TryGetContinueLevel` → 加载当前进度关卡（场景初始位置） |
| 新游戏 | `OnNewGameClicked` | 有存档 → 弹覆盖确认；无存档 → `BeginNewGame()` + 加载第 1 关 |
| 关卡成就 | `OnLevelAchievementClicked` | Phase 4 接线；当前触发 `OnLevelAchievementRequested` 事件或 `Debug.Log` |
| 致谢名单 | `OnCreditsClicked` | Phase 4 接线；当前触发 `OnCreditsRequested` 事件或 `Debug.Log` |
| 规则 | `OnRulesClicked` | Phase 4 接线；当前触发 `OnRulesRequested` 事件或 `Debug.Log` |
| 设置 | `OnSettingsClicked` | Phase 4 接线；当前触发 `OnSettingsRequested` 事件或 `Debug.Log` |

**继续按钮灰态：**

- `MainMenuView.SetContinueEnabled(bool)` 设置 `continueButton.interactable`
- 可选：拖入 `continueDisabledOverlay`（Graphic），禁用时显示遮罩

---

## 五、覆盖存档弹窗

[`OverwriteSaveDialogView`](../Assets/_Game/Scripts/UI/OverwriteSaveDialogView.cs) 在 **有存档**（`hasSave`）时弹出，确认后进入对应关卡的**场景初始位置**。

### 调用方式

| 场景 | 方法 | 动态文案 |
|------|------|----------|
| 点击「新游戏」 | `ShowForNewGame()` | 「开启新游戏，」 |
| 关卡成就选关（Phase 4） | `ShowForLevelSelect(levelIndex)` | 「如进入指定关卡，」 |

固定文案默认：「会覆盖当前存档，是否确认？」（可在 Inspector 修改 `fixedLineDefault`）。

### 事件（Controller 已订阅）

| 事件 | 触发时机 | Controller 处理 |
|------|----------|-----------------|
| `OnNewGameConfirmed` | 确认新游戏 | `BeginNewGame()` + 加载第 1 关 |
| `OnLevelSelectConfirmed` | 确认选关 | `SetCurrentLevel(index)` + 加载该关 |
| `OnCanceled` | 点击取消 | 无操作 |

### Phase 4 选关入口

关卡成就页应调用：

```csharp
mainMenuController.RequestEnterLevel(levelIndex);
```

- 有存档且已配置弹窗 → 自动 `ShowForLevelSelect`
- 否则直接 `SetCurrentLevel` + 加载关卡

---

## 六、角色轮播

[`CharacterCarousel`](../Assets/_Game/Scripts/UI/CharacterCarousel.cs) 挂在展示区节点上。

| Inspector 字段 | 说明 |
|----------------|------|
| `displayImage` | 显示角色图的 Image |
| `manualSprites` | 手动 Sprite 列表（**优先**于 LevelDatabase） |
| `levelDatabase` | 可选；从各关 `characterSprite` 收集 |
| `intervalSeconds` | 轮播间隔，默认 2 秒 |
| `playOnEnable` | 启用时自动开始 |

无可用 Sprite 时显示灰色占位色。`MainMenuView.InitializeCarousel(levelDatabase)` 在 `Awake` 时由 Controller 调用。

---

## 七、场景搭建步骤（美术 / 策划）

### 推荐层级

```
MainMenu.unity
├── Main Camera
├── EventSystem
├── MainMenuSystems          ← MainMenuController + LevelDatabase 引用
└── MainMenuCanvas           ← Canvas + MainMenuView
    ├── Background           (Image)
    ├── TitleArea            (Text × 2，可选)
    ├── MenuArea             (Button × 6)
    ├── ShowcaseArea         (Image + CharacterCarousel)
    └── Panels/
        └── OverwriteSaveDialog   ← OverwriteSaveDialogView
            ├── Mask / PanelRoot
            ├── DynamicLineText
            ├── FixedLineText
            ├── ConfirmButton
            └── CancelButton
```

### 搭建顺序

1. 打开 `Assets/_Game/Scenes/MainMenu.unity`
2. 确认 `MainMenuSystems` 上已挂 `MainMenuController`，并拖入 `LevelDatabase`
3. 在 `MainMenuCanvas` 下摆背景、标题、6 个 Button、展示区
4. 在展示区子节点挂 `CharacterCarousel`，拖入 `displayImage`（及可选 `manualSprites`）
5. 在 `Panels/OverwriteSaveDialog` 搭弹窗 UI，挂 `OverwriteSaveDialogView`，拖齐引用
6. 在 `MainMenuController` 上拖入 `MainMenuView`、`OverwriteSaveDialogView`
7. 在 `MainMenuView` 上拖入 6 个 Button、可选遮罩、`CharacterCarousel`
8. 右键各 View 组件 → **Validate References**，确认 Console 无报错

### Inspector 拖引用清单

| 组件 | 需要拖入 |
|------|----------|
| `MainMenuController` | `MainMenuView`、`OverwriteSaveDialogView`、`LevelDatabase` |
| `MainMenuView` | 6 个菜单 Button、可选 `continueDisabledOverlay`、`CharacterCarousel` |
| `OverwriteSaveDialogView` | `panelRoot`、动态/固定文案 Text、确认/取消 Button |
| `CharacterCarousel` | `displayImage`、可选 `manualSprites` 或 `LevelDatabase` |

### 按钮接线

由 `MainMenuView.Bind()` 在运行时自动注册，**无需**在 Button 的 OnClick 里手动绑 `MainMenuController` 方法。

---

## 八、测试步骤

### 1. 引用校验

Play 前在 Editor 中对 `MainMenuView`、`OverwriteSaveDialogView` 执行 **Validate References**。

### 2. 主界面流程（Play MainMenu 场景）

| 步骤 | 操作 | 预期 |
|------|------|------|
| 无存档 | 进入主界面 | 「继续游戏」灰色不可点 |
| 新游戏 | 点击「新游戏」 | 直接进入 `level1`（场景初始位置） |
| 模拟有档 | 用 `SaveManagerTest` 按 `1` 新游戏或 `3` 模拟通关 | 存档写入 `save.json` |
| 继续 | 回到主界面，点「继续游戏」 | 进入存档中的当前关 |
| 覆盖确认 | 有档时点「新游戏」 | 弹出覆盖弹窗；确认后进第 1 关；取消无变化 |

### 3. 存档与解锁（SaveManagerTest）

将 [`SaveManagerTest`](../Assets/_Game/Scripts/Core/SaveManagerTest.cs) 挂到测试场景或 MainMenu，Play 后：

| 按键 | 功能 |
|------|------|
| `1` | 新游戏并重载第 1 关 |
| `2` | 继续游戏 |
| `3` | 模拟当前关卡通关 |
| `4` | 清空存档 |
| `5` | 打印存档路径与内容 |
| `6` | 打印四关解锁状态 |

### 4. 场景流（SceneFlowTest）

[`SceneFlowTest`](../Assets/_Game/Scripts/Core/SceneFlowTest.cs)：`O` 下一关、`P` 重载、`M` 回主菜单。在主菜单按 `O` 应进入 `level1`（已修复此前跳回主菜单的问题）。

### 5. Build 验证

Build Settings 已注册：`MainMenu`（Index 0）+ `level1`–`level4`。打包后应从主界面启动并可新游戏/继续。

---

## 九、待美术/Editor 完成项

当前 [`MainMenu.unity`](../Assets/_Game/Scenes/MainMenu.unity) 中以下引用**尚未配齐**：

| 项 | 状态 |
|----|------|
| `MainMenuView` 的 6 个 Button | 均为空（`{fileID: 0}`） |
| `MainMenuView` 的 `CharacterCarousel` | 为空 |
| `OverwriteSaveDialog` 脚本 | 需重新挂载 `OverwriteSaveDialogView`（场景中 `m_Script` 曾丢失） |
| 弹窗 `dynamicLineText`、`confirmButton`、`cancelButton` | 未拖入 |
| 菜单区 / 标题区 / 背景 UI | 需美术自行布局 |

完成上述接线后，Phase 3 即可验收。

---

## 十、与 Phase 4 的衔接

Phase 4 将在 Phase 3 基础上新增以下 View（同样 **无接口层**）：

| 计划文件 | 功能 |
|----------|------|
| `RulesDialogView` | 规则滚动 + 关闭 |
| `SettingsDialogView`（大厅版） | 关闭 + 退出游戏 |
| `LevelAchievementView` | 4 关列表、锁态、选关调用 `RequestEnterLevel` |
| `CreditsView` | 致谢滚动占位 |

**注意：** 覆盖存档弹窗已在 Phase 3 实现（`OverwriteSaveDialogView`），Phase 4 **不再新建**。

`MainMenuController` 已预留事件，Phase 4 可订阅或扩展 Controller 打开对应面板：

- `OnLevelAchievementRequested`
- `OnCreditsRequested`
- `OnRulesRequested`
- `OnSettingsRequested`

---

## 十一、相关文件索引

```
Assets/_Game/
├── Scenes/MainMenu.unity
├── Data/ScriptableObjects/LevelDatabase.asset
└── Scripts/
    ├── Core/
    │   ├── SaveManager.cs
    │   ├── SceneFlowManager.cs
    │   ├── SaveManagerTest.cs
    │   └── SceneFlowTest.cs
    └── UI/
        ├── MainMenuController.cs
        ├── MainMenuView.cs
        ├── OverwriteSaveDialogView.cs
        └── CharacterCarousel.cs
```

**已删除（勿再引用）：**

- `MainMenuUI.cs`（运行时生成 UI，已废弃）
- `IMenuPanel.cs`、`IOverwriteSaveDialog.cs`（接口层已移除）
- 点位存档相关（`CheckpointSpawnManager`、`AttachPointRegistry` 等）
