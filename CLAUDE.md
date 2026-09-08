# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

This project is **no longer maintained** — the README directs users to [NetCoreServer](https://github.com/chronoxor/NetCoreServer). Keep that in mind before suggesting non-trivial new features; bug fixes and CI/build maintenance are the realistic scope.

## Platform

**Windows-only.** The library is C++/CLI (managed C++) and requires Visual Studio + MSBuild to build. There is no cross-platform path; do not try to make this build on macOS/Linux directly. CI runs on `windows-latest` (see [.github/workflows/build-windows-vs.yml](.github/workflows/build-windows-vs.yml)).

When opened on macOS (as in this environment), the `.h` / `.cpp` files under `source/CSharpServer/` will appear as **UTF-16 encoded** in Read output — every character is interleaved with a null byte. That is the on-disk encoding, not a Read tool bug. Use grep with care and pass `-a` when needed; prefer letting Visual Studio rewrite files rather than hand-editing them on a non-Windows host.

## Repository layout

- `source/CSharpServer/` — the C++/CLI managed assembly (`CSharpServer.dll`). This *is* the library.
- `modules/CppServer/` — the underlying C++ [CppServer](https://github.com/chronoxor/CppServer) library, pulled in as a [gil](https://github.com/chronoxor/gil) link (see [.gitlinks](.gitlinks)). Must be present and built before the managed assembly can link.
- `examples/` — C# example apps (TcpChatServer/Client, SslChatServer/Client, UdpEcho*, UdpMulticast*, AsioTimer). Each is a standalone .csproj that references the built CSharpServer assembly.
- `performance/` — C# benchmark apps (Tcp/Ssl/Udp Echo + Multicast servers/clients).
- `tools/certificates/` — OpenSSL certs for SSL examples/benchmarks (passphrase `qwerty`; regenerate with `generate.bat`/`generate.sh`).
- `build/` — top-level build entry points. `vs.bat` is the canonical "build everything" script.
- `CSharpServer.sln` — Visual Studio solution containing the library, all examples, and all benchmarks.

## Build commands

First-time setup (run from repo root):

```shell
pip3 install gil
gil update                                         # populates modules/CppServer
cd modules/CppServer/build/VisualStudio
01-generate.bat                                    # generates CppServer CMake projects
```

Full build (release zips produced under `release/`):

```shell
cd build
vs.bat                                             # runs 01-generate → 02-build → 03-tests → 04-release
```

Individual build steps live in [build/VisualStudio/](build/VisualStudio/):

- `01-generate.bat` — regenerates the CppServer CMake projects.
- `02-build.bat` — builds CppServer natively, then `nuget restore CSharpServer.sln && MSBuild CSharpServer.sln /p:Configuration=Release`.
- `03-tests.bat` — runs the CppServer test suite (there are **no managed/C# tests** in this repo; testing happens at the native layer).
- `04-release.bat` — packages `CSharpServer.zip`, `Examples.zip`, `Benchmarks.zip` into `release/`.

To build just the managed side from the solution: `MSBuild CSharpServer.sln /p:Configuration=Release` (assuming CppServer is already built). Use the Visual Studio IDE for development — open `CSharpServer.sln`.

## How the C#/C++ binding is structured

This is the architecturally load-bearing thing to understand. Every public type in `CSharpServer` (Service, TcpServer, TcpSession, TcpClient, SslServer/Session/Client/Context, UdpServer/Client, Timer, Endpoints, Resolvers) follows the **same three-layer pattern**:

1. **Native subclass `XxxEx`** — extends `CppServer::Asio::Xxx` from the C++ library. Holds a `gcroot<Xxx^> root` back to its managed wrapper so native callbacks can hop into managed code. Overrides native `onConnected`/`onReceived`/etc. and forwards them.
2. **Managed `ref class Xxx`** (the public API) — exposes properties and `Send`/`Receive`/`Start`/etc. that marshal to the native object. Defines `virtual void OnConnected()` etc. that users override in their C# subclasses.
3. **`InternalOnXxx` bridge methods** — `internal:` accessors on the managed class that the native `XxxEx` calls via the `gcroot`. They simply invoke the corresponding protected virtual `OnXxx`. This indirection exists because native code can't call protected managed methods directly.

The native shared_ptr is held inside the managed object via the `Embedded<T>` template in [source/CSharpServer/Embedded.h](source/CSharpServer/Embedded.h) — a small ref class that owns a `T*` on the native heap (`#pragma managed(push, off)` regions guard native-only STL includes). Finalizers (`!Xxx()`) call `_xxx.Release()` to delete the native object deterministically.

When adding a new wrapped type or method:
- Mirror the existing pattern (Ex class + ref class + InternalOnXxx bridge).
- All string marshalling uses `msclr::interop::marshal_as<std::string>` / `marshal_as<String^>`.
- Byte buffers use `pin_ptr<Byte>` over `array<Byte>^` before passing to native.
- `TimeSpan^` → native nanoseconds is `CppCommon::Timespan::nanoseconds(100 * timeout->Ticks)`.

## When CppServer's API changes

Because the managed assembly is a thin wrapper, signature changes in `modules/CppServer/include/server/asio/*.h` (e.g. new/removed virtuals on `TCPServer`, `TCPSession`, etc.) usually require matching edits in:
- The corresponding `XxxEx` native subclass override list,
- The managed `ref class Xxx` API surface,
- The `InternalOnXxx` bridges.

The CppServer submodule has its own [modules/CppServer/CLAUDE.md](modules/CppServer/CLAUDE.md) — consult it when the underlying C++ API is the source of the change.
