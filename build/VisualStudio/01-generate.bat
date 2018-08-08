cd ../../modules/CppServer/build/VisualStudio
call 01-generate.bat
if %errorlevel% neq 0 exit /b %errorlevel%
cd ../../../../build/VisualStudio
