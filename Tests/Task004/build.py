from pathlib import Path
import subprocess
root=Path(__file__).resolve().parents[2]
sources=[root/p for p in (
'Assets/Scripts/TowerDeployment/TowerPlacementSubmission.cs',
'Assets/Scripts/TowerDeployment/DeployedTowerCollection.cs',
'Assets/Scripts/TowerDeployment/TowerPlacementCandidate.cs',
'Assets/Scripts/TowerDeployment/TowerPlacementValidator.cs',
'Assets/Scripts/TowerDeployment/TowerSubmissionResult.cs',
'Assets/Scripts/TowerDeployment/TowerInvestmentCommitObservation.cs',
'Assets/Scripts/TowerDeployment/PendingDraftCollection.cs',
'Assets/Scripts/TowerDeployment/DraftAttemptToken.cs',
'Assets/Scripts/TowerDeployment/DraftResult.cs',
'Assets/Scripts/TowerUpgrade/TowerUpgradeSystem.cs',
'Assets/Scripts/TowerUpgrade/TowerUpgradeState.cs',
'Assets/Scripts/TowerFramework/TowerInstance.cs',
'Tests/Task004/BoundaryDoubles.cs',
'Tests/Task004/SubmissionFixture.cs')]
def build(folder,extra,main):
 exe=Path(folder)/'Contracts.exe'
 subprocess.run(['csc','-nologo','-nowarn:0649','-langversion:8.0','-define:UNITY_EDITOR',f'-main:{main}',f'-out:{exe}',*map(str,sources+extra)],check=True,cwd=root)
 subprocess.run(['mono',str(exe)],check=True,cwd=root)
