rmdir /s /q Release
dotnet publish ..\PushMonitorGui/PushMonitorGui.csproj -c Release -p:PublishProfile=FolderProfile
"c:\Program Files (x86)\Inno Setup 6\iscc.exe" /D"RELEASE" setup.iss
rmdir /s /q Release
