## AI Workflow
1. 我和ChatGPT先聊一聊纯抽象的设计内容和迭代激活；
2. Codex(High)根据我和ChatGPT聊的内容维护 ProjectOverview + System Documents
3. Codex(High) 根据 System Documents 生成轻量 Task Docs
4. Codex(Medium) 先读代码，输出 Implementation Plan
5. 我和ChatGPT审核Plan
6. Codex(Medium) 实现并总结改动
7. 我进Unity配置数据+调试
8. Task Docs 用完归档或删除
9. 有问题再让Codex(Medium/High)修bug



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
1. *Add Monster health bar;
2. *Add Monster hit effect;
3. *Add damage display;
4. *Tower and Projectile effect;
5. *Refactor System documents based on new four types of Towers;
6. *Implement the Tower upgrade system:
   1. *Tower Framework foundation;
   2. *Tower Framework foundation;
   3. *Projectile foundation + four base tower runtime;
   4. *Separate the Tower and it blocks;
   5. *Draft integration for deploy / level-up / upgrade;
   6. *Archer/Cannon/MagicOrb/Drone tower behavior layer Phase1 implementation;
7. Buff and Effect system implementation:
   1. *Effect trigger and Binding foundation;
   2. *Definition action and targeting foundation
   3. *Monster buff runtime foundation
   4. *Elemental upgrade profile and eligibility
   5. *Fire element vertical slice
   6. *Monster Slow And Frozen Control API
   7. *Cold and electric element slices
   8. Effect zone foundation
   9. Tower upgrade behavior layer Phase2 implementation;
   10. *Wind element and dedicated WindVortex
8. Player Level up by resolving monsters;
9. Level configuration design;
10. Tap tower to pop up TowerInfoWindow;
11. Develop Monster & Projectile object pool;
12. (Optional)Phase3 Tower upgrade items: Hunting Arrow、Bouncing Shell、Resonance Orb、Missile Drone、Final Dive.
