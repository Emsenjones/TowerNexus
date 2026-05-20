## AI Workflow
1. ChatGPT 和你维护 ProjectOverview + System Documents
2. ChatGPT 根据 System Documents 生成轻量 Task Docs
3. Codex 先读代码，输出 Implementation Plan
4. 你审核 Plan
5. Codex 实现
6. Codex 总结改动
8. Task Docs 用完归档或删除


## Codex车轱辘话
Please review the following task document first:

Docs/Tasks/xxx.md

Before writing any code, inspect the current project structure and provide an implementation plan.

Do not implement anything yet.

Your implementation plan must include:

1. Existing scripts/classes related to this task
2. Whether each responsibility should be implemented by:
    - extending an existing script
    - creating a new script
    - doing a small refactor
3. Files you expect to modify or create
4. Reasons for creating any new scripts
5. Risks, assumptions, or possible conflicts
6. How you will verify the implementation after approval

Important constraints:

- Do not write code yet
- Do not modify files yet
- Do not create new scripts yet
- Keep the scope strictly within xxx.md

After I approve your plan, I will ask you to proceed with implementation.



## Temp

Please review the following Monster System task documents together:

Docs/Tasks/Task_001_MonsterDefinition.md
Docs/Tasks/Task_002_MonsterWaveSpawner.md
Docs/Tasks/Task_003_AStarPathfinding.md
Docs/Tasks/Task_004_MonsterMovement.md
Docs/Tasks/Task_005_DynamicPathRecalculation.md
Docs/Tasks/Task_006_PathBlockingValidation.md
Docs/Tasks/Task_007_MonsterDeathAndReward.md

Also review the source documents:

Docs/00_ProjectOverview.md
Docs/03_MonsterSystem.md
Docs/01_MapSystem.md
Docs/02_TowerDeploySystem.md

Before writing any code, inspect the current project structure and provide one overall implementation plan for the full Monster System.

Do not implement anything yet.

Your plan must include:

1. Existing scripts/classes related to these tasks
2. Proposed runtime architecture
3. Expected new scripts/classes
4. Expected files to modify
5. Dependency order between Task_001 to Task_007
6. Shared APIs that should stay stable across tasks
7. How Monster System integrates with MapSystem and TowerDeploySystem
8. Risks, assumptions, and possible conflicts
9. Suggested implementation order
10. Verification plan after all tasks are implemented

Important constraints:

- Do not write code yet
- Do not implement anything yet
- Do not modify unrelated systems
- Do not over-engineer future monster features
- Keep the implementation aligned with the task documents
- Keep each task scope isolated, but make sure APIs are compatible across all seven tasks