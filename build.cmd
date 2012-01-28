@echo off

powershell -NoProfile -ExecutionPolicy unrestricted -Command "& { . .\build\chewie.ps1;invoke-chewie }"

pushd "%~dp0\build"

powershell -NoProfile -ExecutionPolicy unrestricted -Command "& { .\default.ps1 %* }"

popd