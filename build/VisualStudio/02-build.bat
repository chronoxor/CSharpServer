cd ../../modules/CppServer/build/VisualStudio
call 02-build.bat
if %errorlevel% neq 0 exit /b %errorlevel%
cd ../../../../build/VisualStudio

cd ../..
nuget restore CSharpServer.sln
MSBuild CSharpServer.sln /p:Configuration=Release
if %errorlevel% neq 0 exit /b %errorlevel%
cd build/VisualStudio
