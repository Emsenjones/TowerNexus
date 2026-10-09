from pathlib import Path
import subprocess,tempfile
root=Path(__file__).resolve().parents[3]
with tempfile.TemporaryDirectory(prefix='towernexus-interaction-') as folder:
 exe=Path(folder)/'Input.exe'
 subprocess.run(['csc','-nologo','-langversion:8.0','-nowarn:0649,0414,0067',f'-out:{exe}',str(root/'Assets/Scripts/Camera/CameraPanController.cs'),str(Path(__file__).with_name('InputTests.cs'))],check=True)
 subprocess.run(['mono',str(exe)],check=True)
