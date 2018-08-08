mkdir ..\..\release
cd ..\..\release

xcopy /Y ..\CSharpServer\bin\Release\CSharpServer.dll .
xcopy /Y ..\CSharpServer\bin\Release\CSharpServer.pdb .
7z a CSharpServer.zip CSharpServer.dll CSharpServer.pdb
del /Q CSharpServer.dll CSharpServer.pdb

mkdir examples
cd examples
xcopy /Y ..\..\examples\SslChatClient\bin\Release\*.* .
xcopy /Y ..\..\examples\SslChatServer\bin\Release\*.* .
xcopy /Y ..\..\examples\TcpChatClient\bin\Release\*.* .
xcopy /Y ..\..\examples\TcpChatServer\bin\Release\*.* .
xcopy /Y ..\..\examples\UdpEchoClient\bin\Release\*.* .
xcopy /Y ..\..\examples\UdpEchoServer\bin\Release\*.* .
xcopy /Y ..\..\examples\UdpMulticastClient\bin\Release\*.* .
xcopy /Y ..\..\examples\UdpMulticastServer\bin\Release\*.* .
7z a ..\Examples.zip *.*
del /Q *.*
cd ..
rmdir examples

mkdir performance
cd performance
xcopy /Y ..\..\performance\SslEchoClient\bin\Release\*.* .
xcopy /Y ..\..\performance\SslEchoServer\bin\Release\*.* .
xcopy /Y ..\..\performance\SslMulticastClient\bin\Release\*.* .
xcopy /Y ..\..\performance\SslMulticastServer\bin\Release\*.* .
xcopy /Y ..\..\performance\TcpEchoClient\bin\Release\*.* .
xcopy /Y ..\..\performance\TcpEchoServer\bin\Release\*.* .
xcopy /Y ..\..\performance\TcpMulticastClient\bin\Release\*.* .
xcopy /Y ..\..\performance\TcpMulticastServer\bin\Release\*.* .
xcopy /Y ..\..\performance\UdpEchoClient\bin\Release\*.* .
xcopy /Y ..\..\performance\UdpEchoServer\bin\Release\*.* .
xcopy /Y ..\..\performance\UdpMulticastClient\bin\Release\*.* .
xcopy /Y ..\..\performance\UdpMulticastServer\bin\Release\*.* .
7z a ..\Benchmarks.zip *.*
del /Q *.*
cd ..
rmdir performance

cd ../build/VisualStudio
