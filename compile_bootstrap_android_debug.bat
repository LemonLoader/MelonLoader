@echo off
dotnet restore
dotnet publish -r linux-bionic-arm64 -p:DisableUnsupportedError=true -p:PublishAotUsingRuntimePack=true -p:Configuration=Debug

pause