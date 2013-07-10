@ECHO OFF
SET v40-30319="%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild"
SET v35="%WINDIR%\Microsoft.NET\Framework\v3.5\msbuild"

SET SOLUTION="Banshee.sln"

IF EXIST "..\..\%SOLUTION%" (
	CD ..\..
)

ECHO "Looking for Microsoft.NET MSBuild..."
IF EXIST %v40-30319% (
	ECHO "Building with Microsoft.NET v4.0 MSBuild"
	SET MSBUILD_PATH=%v40-30319%
) ELSE IF EXIST "%v35%" (
	ECHO "Building with Microsoft.NET v3.5 MSBuild"
	SET MSBUILD_PATH=%v35%
) ELSE (
	ECHO "Build failed: Microsoft.NET MSBuild (msbuild.exe) not found"
	GOTO END
)

%MSBUILD_PATH% %SOLUTION% /p:Configuration=Windows /p:Platform="Any CPU" && ^
ECHO 'Running "post-build.bat"' && ^
build\windows\post-build.bat && ^
CD build\windows

:END
