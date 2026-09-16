mkdir ..\..\release
cd ..\..\release

xcopy /Y ..\source\CSharpServer\bin\Release\Ijwhost.dll .
xcopy /Y ..\source\CSharpServer\bin\Release\CSharpServer.dll .
xcopy /Y ..\source\CSharpServer\bin\Release\CSharpServer.pdb .
7z a CSharpServer.zip Ijwhost.dll CSharpServer.dll CSharpServer.pdb
del /Q Ijwhost.dll CSharpServer.dll CSharpServer.pdb

mkdir examples
cd examples
xcopy /Y ..\..\examples\SslChatClient\bin\Release\net10.0\*.* .
xcopy /Y ..\..\examples\SslChatServer\bin\Release\net10.0\*.* .
xcopy /Y ..\..\examples\TcpChatClient\bin\Release\net10.0\*.* .
xcopy /Y ..\..\examples\TcpChatServer\bin\Release\net10.0\*.* .
xcopy /Y ..\..\examples\UdpEchoClient\bin\Release\net10.0\*.* .
xcopy /Y ..\..\examples\UdpEchoServer\bin\Release\net10.0\*.* .
xcopy /Y ..\..\examples\UdpMulticastClient\bin\Release\net10.0\*.* .
xcopy /Y ..\..\examples\UdpMulticastServer\bin\Release\net10.0\*.* .
xcopy /Y ..\..\tools\certificates\*.pem .
7z a ..\Examples.zip *.*
del /Q *.*
cd ..
rmdir examples

mkdir performance
cd performance
xcopy /Y ..\..\performance\SslEchoClient\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\SslEchoServer\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\SslMulticastClient\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\SslMulticastServer\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\TcpEchoClient\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\TcpEchoServer\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\TcpMulticastClient\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\TcpMulticastServer\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\UdpEchoClient\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\UdpEchoServer\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\UdpMulticastClient\bin\Release\net10.0\*.* .
xcopy /Y ..\..\performance\UdpMulticastServer\bin\Release\net10.0\*.* .
xcopy /Y ..\..\tools\certificates\*.pem .
7z a ..\Benchmarks.zip *.*
del /Q *.*
cd ..
rmdir performance

cd ../build/VisualStudio
