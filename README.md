# CSharpServer

[![Windows build status](https://img.shields.io/appveyor/ci/chronoxor/CSharpServer/master.svg?label=Windows)](https://ci.appveyor.com/project/chronoxor/CSharpServer)

C# Server Library provides functionality to create different kind of
client/server solutions.

[CSharpServer documentation](https://chronoxor.github.io/CSharpServer)<br/>
[CSharpServer downloads](https://github.com/chronoxor/CSharpServer/releases)<br/>

# Contents
  * [Features](#features)
  * [Requirements](#requirements)
  * [How to build?](#how-to-build)
    * [Clone repository with submodules](#clone-repository-with-submodules)
    * [Generate CMake projects](#generate-cmake-projects)
    * [Windows (Visual Studio)](#windows-visual-studio)

# Features
* [Asynchronous communication](https://think-async.com)
* Supported CPU scalability designs: IO service per thread, thread pool
* Supported transport protocols: [TCP](#example-tcp-chat-server), [SSL](#example-ssl-chat-server),
  [UDP](#example-udp-echo-server), [UDP multicast](#example-udp-multicast-server)

# Requirements
* Windows 7 / Windows 10
* [7-Zip](https://www.7-zip.org)
* [CMake](https://www.cmake.org)
* [GIT](https://git-scm.com)
* [Visual Studio](https://www.visualstudio.com)

# How to build?

## Clone repository with submodules
```shell
git clone https://github.com/chronoxor/CSharpServer.git CSharpServer
cd CSharpServer
git submodule update --init --recursive --remote
```

## Generate CMake projects
Run CMake script to generate C++ projects:

```shell
cd modules/CppServer/build/VisualStudio
01-generate.bat
```

## Windows (Visual Studio)
Open and build CSharpServer.sln or run the build script:

```shell
cd build
vs.bat
```

The build script will create "release" directory with zip files:
* [CSharpServer.zip](https://github.com/chronoxor/CSharpServer/releases/download/1.0.0.0/CSharpServer.zip) - C# Server assembly
* [Benchmarks.zip](https://github.com/chronoxor/CSharpServer/releases/download/1.0.0.0/Benchmarks.zip) - C# Server benchmarks
* [Examples.zip](https://github.com/chronoxor/CSharpServer/releases/download/1.0.0.0/Examples.zip) - C# Server examples
