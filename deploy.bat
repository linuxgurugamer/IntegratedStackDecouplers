
@echo off

set H=R:\KSP_1.5.1_dev
set GAMEDIR=IntegratedStackDecouplers

echo %H%

copy /Y "%1%2" "GameData\%GAMEDIR%\Plugins"
copy /Y %GAMEDIR%.version GameData\%GAMEDIR%

xcopy /y /s /I GameData\%GAMEDIR% "%H%\GameData\%GAMEDIR%"

