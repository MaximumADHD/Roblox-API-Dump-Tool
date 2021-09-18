@ echo off
echo Updating GitHub Pages...

RobloxAPIDumpTool.exe -updatePages %~dp0..\maximumadhd.github.io

if %errorlevel% EQU 0 (
	echo Done!
	pause
) else (
	echo Failed :(
	pause
	exit /b %errorlevel%
)
