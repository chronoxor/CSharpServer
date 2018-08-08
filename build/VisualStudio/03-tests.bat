cd ../../modules/CppServer/build/VisualStudio
call 03-tests.bat
if %errorlevel% neq 0 exit /b %errorlevel%
cd ../../../../build/VisualStudio
