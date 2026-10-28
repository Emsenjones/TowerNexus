# TowerNexus Toon Rendering — Task Index

Document Set: Task

Status: Requirements prepared; implementation and visual acceptance pending.

本组任务记录 2026-10-28 讨论确定的第一版视觉实现范围，供后续开发使用。
工作分支为 `codex/vertical-slice`。先阅读对应 Task，再在开发会话中确定实现计划。
本文档不表示 Shader、材质、场景或设备验证已经完成。

## 1. Visual Target And Approved Scope

- 以 Sable 启发的线条与平涂视觉为方向：有限色系、大块干净颜色、清楚的主要形体。
- 使用项目的正交游戏视角验证效果；概念图用于表达方向，不要求逐像素复现。
- 第一版固定为受光、阴影两档；窄过渡可用于改善边界，不能形成大面积写实渐变。
- 基础贴图保留固有色、雕刻图案和装饰线；动态渲染负责光照变化与外轮廓。
- 多个材质共用一个 Toon Shader。塔身、金属、晶体等区域分别设置亮部与暗部配色。
- 区域划分采用已有子网格或独立部件的材质分配，不制作 RGB 区域遮罩。
- 第一版不要求 Normal Map，但 Mesh 法线必须正确；简化模型仍需保留主要转折面。
- 先支持静态、不透明的塔和地面。晶体可先用不透明配色表达。
- 第三档高光、镜面反射、透明折射、Bloom、多光源叠加、独立线稿遮罩和批量资产迁移均暂缓。
- 动画怪物的蒙皮、受击表现和描边兼容需要真实样本另行验证，不由静态塔测试推定通过。

概念生成 Prompt 允许“两或三档”，这是探索图片时的自由度；本组任务的实现范围明确为两档。

## 2. Execution Order

| Task | 交付目标 | 依赖 |
|---|---|---|
| [Task001 — Two-Tone Shader](Task001_ToonShaderFoundation.md) | 两档明暗、投射与接收阴影、独立材质参数、基础验证场景 | 本文约定；可使用占位几何体 |
| [Task002 — Dynamic Outline](Task002_ToonOutline.md) | 可调动态轮廓、遮挡正确、正交视角下稳定 | Task001 的材质与场景基线 |
| [Task003 — Asset Integration And Validation](Task003_ToonAssetIntegration.md) | 一座真实塔的接入流程、实际游戏视角验收与设备证据 | Task001、Task002；用户提供真实资产 |

任务编号从本轮重新开始。旧 Task 文档已按用户要求清理；它们保存在
Git 提交 `f54e8a17` 中。旧功能的验收状态以该历史版本记录的证据边界为准。

## 3. References And Ownership

- [Concept Prompt](../Art/00_SableInspiredConceptPrompts.md)
- [Reference 1](../Art/00_SableInspiredConceptPrompt_Ref1.png)、[Reference 2](../Art/00_SableInspiredConceptPrompt_Ref2.png)、[Reference 3](../Art/00_SableInspiredConceptPrompt_Ref3.png)、[Reference 4](../Art/00_SableInspiredConceptPrompt_Ref4.png)
- [Tower Framework](../System/10_TowerFrameworkSystem.md)：模型挂载、层级替换、Anchor 和 Tower 状态展示的既有约定。
- [Camera System](../System/06_CameraSystem.md)：游戏镜头与输入行为。
- [Tower Placement](../System/09_TowerPlacementSystem.md)：放置预览、占地与交互语义。

用户负责参考图选择、Tripo 模型与贴图生成、必要的资产整理，以及最终视觉判断。
开发任务负责渲染实现、材质配置、接入说明和验证支持。
首座塔允许双方根据 Unity 中的结果迭代模型、贴图和参数。
Shader 不承担修复轮廓、重新划分材质区域或去除贴图中固定光影的职责。

这些任务属于视觉呈现，不新增游戏业务系统，不改变战斗、寻路、放置、升级或相机输入的所有权。
后续真实资产按现有 TowerModelRoot 等约定接入，不以视觉需求重定义游戏坐标或占地。

## 4. Repository Baseline — Static Inspection, 2026-10-28

| 项目 | 已确认的磁盘配置 |
|---|---|
| Unity | `6000.3.14f1`，来源 `ProjectSettings/ProjectVersion.txt` |
| URP | `17.3.0`，来源 `Packages/manifest.json` |
| 游戏相机 Prefab | `Prefab_GameRuntime.prefab` 中为 Orthographic，Size 为 6；这是当前配置，不是锁定的美术标准 |
| Quality | 当前记录为 Medium；iPhone 默认索引为 2，Standalone 默认索引为 5 |
| 3D Renderer | Medium、Ultra 的 Pipeline Asset 引用 `Assets/URPDefaultResources/Default_Forward_Renderer.asset` |
| Graphics 默认项 | 引用 `Assets/Settings/UniversalRP.asset`，其 Renderer 列表与上述 Quality 配置不同 |

开发前在 Unity 中确认目标平台实际生效的 Quality、Pipeline Asset、Camera Renderer、
Render Graph/Compatibility 状态及阴影配置。上述静态记录不能代替运行时确认；
也不能只修改 Graphics 默认项就假定游戏和 iOS 已使用新设置。

现有 `TowerVisualController` 通过 `_BaseColor` / `_Color` 改变预览颜色，
并按材质槽使用 MaterialPropertyBlock 做升级目标提示。
新 Shader 的参数必须考虑这条已存在的接入路径，具体要求见 Task001、Task003。

## 5. Completion And Evidence

- 先用“一座塔或占位模型、一块地面、一个方向光、正交相机”验证。
- 每个任务分别记录：静态检查、Unity 导入/Shader 编译、Play Mode、Player/设备、用户视觉确认。
- 未执行的项目标注 Pending；占位模型通过不代表 Tripo 资产已通过。
- 样例塔通过不代表全部塔、动画怪物或整个 Vertical Slice 已完成。
- 正式合入前需记录真实素材、材质参数、实际 Renderer 配置、截图、设备条件及已知限制。

技术参考：[URP 自定义光照](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-lighting.html)、
[URP 自定义阴影](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/use-built-in-shader-methods-shadows.html)。
