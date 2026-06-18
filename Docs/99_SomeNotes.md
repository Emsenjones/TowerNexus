## AI Workflow
1. 我和ChatGPT先聊一聊纯抽象的设计内容和迭代激活
2. Codex 根据我和ChatGPT聊的第一阶段内容维护 ProjectOverview + System Documents
3. Codex 根据 System Documents 生成轻量 Task Docs
4. Codex 先读代码，输出 Implementation Plan
5. 你审核 Plan
6. Codex 实现并总结改动
7. 我进Unity配置数据+调试
8. Task Docs 用完归档或删除


## Codex Implementation chat
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

## Wittle Defender
1. Average each wave duration: 400 s;
2. Wave count: 20;
## Future Development
1. *Add Monster health bar.
2. *Add Monster hit effect.
3. *Add damage display.
4. *Tower and Projectile effect.
5. Implement Tower upgrade system.
   1. 10_TowerUpgradeSystem.md
   2. 07_TowerFrameworkSystem.md
   3. 05_DraftSystem.md
   4. 02_BattleHUDUISystem.md
   5. 06_TowerPlacementSystem.md
   6. 01_PlayerSystem.md
   7. 04_MonsterSystem.md
   8. 00_ProjectOverview.md
      
      
      
      
      
      
      
6. Develop Monster & Projectile object pool.
