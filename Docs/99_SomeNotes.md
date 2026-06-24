## AI Workflow
1. 我和ChatGPT先聊一聊纯抽象的设计内容和迭代激活
2. Codex根据我和ChatGPT聊的内容维护 ProjectOverview + System Documents
3. Codex 根据 System Documents 生成轻量 Task Docs
4. Codex 先读代码，输出 Implementation Plan
5. 我和ChatGPT审核Plan
6. Codex 实现并总结改动
7. 我进Unity配置数据+调试
8. Task Docs 用完归档或删除


## Tower Nexus Framework
1. Tower(Fantasy):玩家为什么想造这座塔？
   1. Archer：箭塔
   2. Cannon：炮塔
   3. Magic Orb：环绕法球塔
   4. Drone：自主无人机
   5. Missile：导弹塔
   6. Spike：地刺陷阱
   7. ...
2. Base Behavior:这座塔没有任何 Upgrade 时，是怎么工作的？
   1. Attack Entity：攻击手段是什么？
      1. Arrow
      2. Cannon
      3. Magic Orb
      4. Drone
      5. Missile
      6. Spike Trap
      7. Laser
      8. ...
   2. Activation：什么时候开始攻击？
      1. Enemy In Range
      2. Always active
      3. ... 
   3. Targeting：目标怎么选择？
      1. Nearest
      2. Highest HP
      3. Lowest HP
      4. Random
      5. ...
   4. Behaviour：Attack Entity在战场上的行为
   5. Lifetime：Attack Entity如何结束？
      1. Destroy On Hit
      2. Destroy After Time
      3. Maximum Hit Count
      4. Battery
      5. Infinite
      6. ...
3. Upgrade Rules
   1. When(Condition)：如何触发？
   2. Do(Action)：触发后做什么？
   
   e.g.:
   1. OnHit - ContinueFlight
   2. OnSpawn - SpawnProjectile
   3. ...

## Wittle Defender
1. Average each wave duration: 400 s;
2. Wave count: 20;
## Future Development
1. *Add Monster health bar.
2. *Add Monster hit effect.
3. *Add damage display.
4. *Tower and Projectile effect.
5. *Refactor System documents based on new four types of Towers.
6. Implement the Tower upgrade system.
   *Phase 1: Tower Framework foundation
   *Phase 2: Projectile foundation + four base tower runtime
   Phase 3: Separate the Tower and it blocks.
   Phase 4: Upgrade data/application foundation
   Phase 5: Draft integration for deploy / level-up / upgrade
   Phase 6: Runtime upgrade effects
   Phase 7: Player progression polish
7. Develop Monster & Projectile object pool.

