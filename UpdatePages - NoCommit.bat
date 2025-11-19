@ echo off
set /p rbx_api_version=[NO COMMIT] Which version of Roblox? (Empty = Latest) 

if defined rbx_api_version (
	echo Updating GitHub Pages to version %rbx_api_version%...
	RobloxAPIDumpTool.exe -full -updatePages %~dp0..\maximumadhd.github.io -version %rbx_api_version% -nocommit
) else (
	echo Updating GitHub Pages to latest version...
	RobloxAPIDumpTool.exe -full -updatePages %~dp0..\maximumadhd.github.io -nocommit
)

if %errorlevel% EQU 0 (
	echo Done!
	pause
) else (
	echo Failed :(
	pause
	exit /b %errorlevel%
)
