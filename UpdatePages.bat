@ echo off

echo Updating LatestChanges.md...
RobloxAPIDumpTool.exe -difflog -out %~dp0..\maximumadhd.github.io\LatestChanges.md

if %errorlevel% EQU 0 (
	echo Patched!
) else (
	echo Failed :(
	pause
	exit /b %errorlevel%
)

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
