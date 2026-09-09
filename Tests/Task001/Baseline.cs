using System;
using System.Collections.Generic;
namespace UnityEngine { public class MonoBehaviour {} public static class Debug { public static void LogException(Exception e, object o) {} } }
public class GridNodeBehaviour {}
public enum TowerBehaviourPackageType { None, Example }
public enum TowerUpgradeLayer { Basic, Behaviour, Elemental }
public class TowerUpgradeDefinition { public TowerUpgradeLayer UpgradeLayer; public TowerBehaviourPackageType BehaviourPackageType; }
public class TowerDefinition { public TowerLevelConfig GetLevelConfig(int n) { return null; } public int GetMaxConfiguredLevel() { return 3; } }
public class TowerLevelConfig { public int Level; public bool IsValid() { return true; } }
class Reproduction { static int Main() {
 var tower=new TowerInstance(); var upgrade=new TowerUpgradeDefinition(); bool consumed=false;
 tower.OnUpgradeRecorded+=(t,u)=>{throw new InvalidOperationException("Injected observer failure");};
 try { if(tower.TryRecordUpgrade(upgrade)) consumed=true; } catch(InvalidOperationException) {}
 if(!tower.HasUpgrade(upgrade)||consumed) return 1;
 Console.WriteLine("REPRODUCED: actual TowerInstance/UpgradeState recorded upgrade; throwing observer prevented following reward consumption. Managed stub harness, not Unity Play Mode."); return 0;
} }
