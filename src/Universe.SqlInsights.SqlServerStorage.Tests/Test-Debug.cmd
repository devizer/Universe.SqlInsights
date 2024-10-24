@echo off
dotnet build -c Debug -f net6.0 -v:q
dotnet test --no-restore --nologo -c Debug -f net6.0 --filter "Name~SandboxTestLab"