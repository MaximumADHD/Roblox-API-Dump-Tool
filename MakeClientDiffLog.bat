@ echo off

set /p robloxVersion="Enter a valid version number: "
echo Working...

RobloxAPIDumpTool.exe -difflog %robloxVersion%

if %errorlevel% EQU 0 (
	echo Done!
	pause
) else (
	echo Failed :(
	pause
	exit /b %errorlevel%
)