@echo off
REM Enterprise Commerce Platform — root CLI wrapper (Windows cmd)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ecp.ps1" %*
