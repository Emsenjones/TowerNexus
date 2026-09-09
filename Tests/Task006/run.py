from pathlib import Path
import subprocess,tempfile
root=Path(__file__).resolve().parents[2]
subprocess.run(['python3',str(root/'Tests/Task006/replay.py')],check=True)
with tempfile.TemporaryDirectory(prefix='task006-') as folder:
 exe=Path(folder)/'Lifecycle.exe'
 sources=['Assets/Scripts/Diagnostics/CombatDiagnosticScope.cs','Assets/Scripts/Diagnostics/CombatDiagnosticSource.cs','Assets/Scripts/Diagnostics/CombatBalance/CombatReportExporter.cs','Tests/Task006/LifecycleTests.cs']
 subprocess.run(['csc','-nologo','-nowarn:0649','-langversion:8.0','-define:UNITY_EDITOR',f'-out:{exe}',*[str(root/p) for p in sources]],check=True)
 subprocess.run(['mono',str(exe)],check=True)

subprocess.run(["python3",str(root/"Tests/Task006/integrity.py")],check=True)
