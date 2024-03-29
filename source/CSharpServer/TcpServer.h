#pragma once

#include "Endpoint.h"

#include <server/asio/tcp_server.h>

namespace CSharpServer {

    ref class TcpSession;
    ref class TcpServer;

    class TcpSessionEx : public CppServer::Asio::TCPSession
    {
    public:
        using CppServer::Asio::TCPSession::TCPSession;

        gcroot<TcpSession^> root;

        bool SendAsync(const void* buffer, size_t size) override;
        bool SendAsync(std::string_view text) override;

        void onConnected() override;
        void onDisconnected() override;
        void onReceived(const void* buffer, size_t size) override;
        void onSent(size_t sent, size_t pending) override;
        void onEmpty() override;
        void onError(int error, const std::string& category, const std::string& message) override;
    };

    class TcpServerEx : public CppServer::Asio::TCPServer
    {
    public:
        using CppServer::Asio::TCPServer::TCPServer;

        gcroot<TcpServer^> root;

        std::shared_ptr<CppServer::Asio::TCPSession> CreateSession(const std::shared_ptr<TCPServer>& server) override;

        void onStarted() override;
        void onStopped() override;
        void onConnected(std::shared_ptr<CppServer::Asio::TCPSession>& session) override;
        void onDisconnected(std::shared_ptr<CppServer::Asio::TCPSession>& session) override;
        void onError(int error, const std::string& category, const std::string& message) override;
    };

    //! TCP session
    /*!
        TCP session is used to read and write data from the connected TCP client.
    */
    public ref class TcpSession
    {
    public:
        //! Initialize the session with a given server
        /*!
            \param server - Connected server
        */
        TcpSession(TcpServer^ server);
        ~TcpSession() { this->!TcpSession(); }

        //! Get the session Id
        property String^ Id { String^ get() { return marshal_as<String^>(_session->get()->id().string()); } }

        //! Get the server
        property TcpServer^ Server { TcpServer^ get() { return _server; } }

        //! Get the number of bytes pending sent by the session
        property long long BytesPending { long long get() { return (long long)_session->get()->bytes_pending(); } }
        //! Get the number of bytes sent by the session
        property long long BytesSent { long long get() { return (long long)_session->get()->bytes_sent(); } }
        //! Get the number of bytes received by the session
        property long long BytesReceived { long long get() { return (long long)_session->get()->bytes_received(); } }

        //! Get the option: receive buffer limit
        property long OptionReceiveBufferLimit { long get() { return (long)_session->get()->option_receive_buffer_limit(); } }
        //! Get the option: receive buffer size
        property long OptionReceiveBufferSize { long get() { return (long)_session->get()->option_receive_buffer_size(); } }
        //! Get the option: send buffer limit
        property long OptionSendBufferLimit { long get() { return (long)_session->get()->option_send_buffer_limit(); } }
        //! Get the option: send buffer size
        property long OptionSendBufferSize { long get() { return (long)_session->get()->option_send_buffer_size(); } }

        //! Is the session connected?
        property bool IsConnected { bool get() { return _session->get()->IsConnected(); } }

        //! Disconnect the session
        /*!
            \return 'true' if the section was successfully disconnected, 'false' if the section is already disconnected
        */
        bool Disconnect() { return _session->get()->Disconnect(); }

        //! Send data to the client (synchronous)
        /*!
            \param buffer - Buffer to send
            \return Size of sent data
        */
        long long Send(array<Byte>^ buffer) { return Send(buffer, 0, buffer->Length); }
        //! Send data to the client (synchronous)
        /*!
            \param buffer - Buffer to send
            \param offset - Buffer offset
            \param size - Buffer size
            \return Size of sent data
        */
        long long Send(array<Byte>^ buffer, long long offset, long long size)
        {
            pin_ptr<Byte> ptr = &buffer[buffer->GetLowerBound(0) + (int)offset];
            return (long long)_session->get()->Send(ptr, size);
        }
        //! Send text to the client (synchronous)
        /*!
            \param text - Text string to send
            \return Size of sent text
        */
        long long Send(String^ text)
        {
            std::string temp = marshal_as<std::string>(text);
            return (long long)_session->get()->Send(temp.data(), temp.size());
        }

        //! Send data to the client with timeout (synchronous)
        /*!
            \param buffer - Buffer to send
            \param timeout - Timeout
            \return Size of sent data
        */
        long long Send(array<Byte>^ buffer, TimeSpan^ timeout) { return Send(buffer, 0, buffer->Length, timeout); }
        //! Send data to the client with timeout (synchronous)
        /*!
            \param buffer - Buffer to send
            \param offset - Buffer offset
            \param size - Buffer size
            \param timeout - Timeout
            \return Size of sent data
        */
        long long Send(array<Byte>^ buffer, long long offset, long long size, TimeSpan^ timeout)
        {
            pin_ptr<Byte> ptr = &buffer[buffer->GetLowerBound(0) + (int)offset];
            return (long long)_session->get()->Send(ptr, size, CppCommon::Timespan::nanoseconds(100 * timeout->Ticks));
        }
        //! Send text to the client with timeout (synchronous)
        /*!
            \param text - Text string to send
            \param timeout - Timeout
            \return Size of sent text
        */
        long long Send(String^ text, TimeSpan^ timeout)
        {
            std::string temp = marshal_as<std::string>(text);
            return (long long)_session->get()->Send(temp.data(), temp.size(), CppCommon::Timespan::nanoseconds(100 * timeout->Ticks));
        }

        //! Send data to the client (asynchronous)
        /*!
            \param buffer - Buffer to send
            \return 'true' if the data was successfully sent, 'false' if the session is not connected
        */
        bool SendAsync(array<Byte>^ buffer) { return SendAsync(buffer, 0, buffer->Length); }
        //! Send data to the client (asynchronous)
        /*!
            \param buffer - Buffer to send
            \param offset - Buffer offset
            \param size - Buffer size
            \return 'true' if the data was successfully sent, 'false' if the session is not connected
        */
        bool SendAsync(array<Byte>^ buffer, long long offset, long long size)
        {
            pin_ptr<Byte> ptr = &buffer[buffer->GetLowerBound(0) + (int)offset];
            return _session->get()->SendAsync(ptr, size);
        }
        //! Send text to the client (asynchronous)
        /*!
            \param text - Text string to send
            \return 'true' if the text was successfully sent, 'false' if the session is not connected
        */
        bool SendAsync(String^ text)
        {
            std::string temp = marshal_as<std::string>(text);
            return _session->get()->SendAsync(temp.data(), temp.size());
        }

        //! Receive data from the client (synchronous)
        /*!
            \param buffer - Buffer to receive
            \return Size of received data
        */
        long long Receive(array<Byte>^ buffer) { return Receive(buffer, 0, buffer->Length); }
        //! Receive data from the client (synchronous)
        /*!
            \param buffer - Buffer to receive
            \param offset - Buffer offset
            \param size - Buffer size
            \return Size of received data
        */
        long long Receive(array<Byte>^ buffer, long long offset, long long size)
        {
            pin_ptr<Byte> ptr = &buffer[buffer->GetLowerBound(0) + (int)offset];
            return (long long)_session->get()->Receive(ptr, size);
        }
        //! Receive text from the client (synchronous)
        /*!
            \param size - Text size to receive
            \return Received text
        */
        String^ Receive(long long size)
        {
            return marshal_as<String^>(_session->get()->Receive(size));
        }

        //! Receive data from the client with timeout (synchronous)
        /*!
            \param buffer - Buffer to receive
            \param timeout - Timeout
            \return Size of received data
        */
        long long Receive(array<Byte>^ buffer, TimeSpan^ timeout) { return Receive(buffer, 0, buffer->Length, timeout); }
        //! Receive data from the client with timeout (synchronous)
        /*!
            \param buffer - Buffer to receive
            \param offset - Buffer offset
            \param size - Buffer size
            \param timeout - Timeout
            \return Size of received data
        */
        long long Receive(array<Byte>^ buffer, long long offset, long long size, TimeSpan^ timeout)
        {
            pin_ptr<Byte> ptr = &buffer[buffer->GetLowerBound(0) + (int)offset];
            return (long long)_session->get()->Receive(ptr, size, CppCommon::Timespan::nanoseconds(100 * timeout->Ticks));
        }
        //! Receive text from the client with timeout (synchronous)
        /*!
            \param size - Text size to receive
            \param timeout - Timeout
            \return Received text
        */
        String^ Receive(long long size, TimeSpan^ timeout)
        {
            return marshal_as<String^>(_session->get()->Receive(size, CppCommon::Timespan::nanoseconds(100 * timeout->Ticks)));
        }

        //! Receive data from the client (asynchronous)
        void ReceiveAsync() { return _session->get()->ReceiveAsync(); }

        //! Setup option: receive buffer limit
        /*!
            The session will be disconnected if the receive buffer limit is met.
            Default is unlimited.

            \param limit - Receive buffer limit
        */
        void SetupReceiveBufferLimit(long limit) { return _session->get()->SetupReceiveBufferLimit(limit); }
        //! Setup option: receive buffer size
        /*!
            This option will setup SO_RCVBUF if the OS support this feature.

            \param size - Receive buffer size
        */
        void SetupReceiveBufferSize(long size) { return _session->get()->SetupReceiveBufferSize(size); }
        //! Setup option: send buffer limit
        /*!
            The session will be disconnected if the send buffer limit is met.
            Default is unlimited.

            \param limit - Send buffer limit
        */
        void SetupSendBufferLimit(long limit) { return _session->get()->SetupSendBufferLimit(limit); }
        //! Setup option: send buffer size
        /*!
            This option will setup SO_SNDBUF if the OS support this feature.

            \param size - Send buffer size
        */
        void SetupSendBufferSize(long size) { return _session->get()->SetupSendBufferSize(size); }

    protected:
        //! Handle client connected notification
        virtual void OnConnected() {}
        //! Handle client disconnected notification
        virtual void OnDisconnected() {}

        //! Handle buffer received notification
        /*!
            Notification is called when another part of buffer was received from the client.

            \param buffer - Received buffer
            \param size - Received buffer size
        */
        virtual void OnReceived(array<Byte>^ buffer, long long size) {}
        //! Handle buffer sending notification
        /*!
            \param size - Size of send buffer
            \return Allow send flag
        */
        virtual bool OnSending(long long size) { return true; }
        //! Handle buffer sent notification
        /*!
            Notification is called when another part of buffer was sent to the client.

            This handler could be used to send another buffer to the client
            for instance when the pending size is zero.

            \param sent - Size of sent buffer
            \param pending - Size of pending buffer
        */
        virtual void OnSent(long long sent, long long pending) {}

        //! Handle empty send buffer notification
        /*!
            Notification is called when the send buffer is empty and ready
            for a new data to send.

            This handler could be used to send another buffer to the client.
        */
        virtual void OnEmpty() {}

        //! Handle error notification
        /*!
            \param error - Error code
            \param category - Error category
            \param message - Error message
        */
        virtual void OnError(int error, String^ category, String^ message) {}

    internal:
        void InternalOnConnected() { OnConnected(); }
        void InternalOnDisconnected() { OnDisconnected(); }
        void InternalOnReceived(array<Byte>^ buffer, long long size) { OnReceived(buffer, size); }
        bool InternalOnSending(long long sent) { return OnSending(sent); }
        void InternalOnSent(long long sent, long long pending) { OnSent(sent, pending); }
        void InternalOnEmpty() { OnEmpty(); }
        void InternalOnError(int error, String^ category, String^ message) { OnError(error, category, message); }

    protected:
        !TcpSession() { _session.Release(); }

    internal:
        TcpServer^ _server;
        Embedded<std::shared_ptr<TcpSessionEx>> _session;
    };

    //! TCP server
    /*!
        TCP server is used to connect, disconnect and manage TCP sessions.
    */
    public ref class TcpServer
    {
    public:
        //! Initialize TCP server with a given service, port number and protocol type
        /*!
            \param service - Service
            \param port - Port number
            \param protocol - Protocol type
        */
        TcpServer(Service^ service, int port, CSharpServer::InternetProtocol protocol);
        //! Initialize TCP server with a given service, address and port number
        /*!
            \param service - Service
            \param address - Server address
            \param port - Port number
        */
        TcpServer(Service^ service, String^ address, int port);
        //! Initialize TCP server with a given service and TCP endpoint
        /*!
            \param service - Service
            \param endpoint - Server TCP endpoint
        */
        TcpServer(Service^ service, TcpEndpoint^ endpoint);
        ~TcpServer() { this->!TcpServer(); }

        //! Get the server Id
        property String^ Id { String^ get() { return marshal_as<String^>(_server->get()->id().string()); } }

        //! Get the service
        property Service^ Service { CSharpServer::Service^ get() { return _service; } }

        //! Get the server address
        property String^ Address { String^ get() { return marshal_as<String^>(_server->get()->address()); } }
        //! Get the server port number
        property int Port { int get() { return _server->get()->port(); } }

        //! Get the number of sessions connected to the server
        property long long ConnectedSessions { long long get() { return (long long)_server->get()->connected_sessions(); } }
        //! Get the number of bytes pending sent by the server
        property long long BytesPending { long long get() { return (long long)_server->get()->bytes_pending(); } }
        //! Get the number of bytes sent by the server
        property long long BytesSent { long long get() { return (long long)_server->get()->bytes_sent(); } }
        //! Get the number of bytes received by the server
        property long long BytesReceived { long long get() { return (long long)_server->get()->bytes_received(); } }

        //! Get the option: keep alive
        property bool OptionKeepAlive { bool get() { return _server->get()->option_keep_alive(); } }
        //! Get the option: no delay
        property bool OptionNoDelay { bool get() { return _server->get()->option_no_delay(); } }
        //! Get the option: reuse address
        property bool OptionReuseAddress { bool get() { return _server->get()->option_reuse_address(); } }
        //! Get the option: reuse port
        property bool OptionReusePort { bool get() { return _server->get()->option_reuse_port(); } }

        //! Is the server started?
        property bool IsStarted { bool get() { return _server->get()->IsStarted(); } }

        //! Start the server
        /*!
            \return 'true' if the server was successfully started, 'false' if the server failed to start
        */
        bool Start() { return _server->get()->Start(); }
        //! Stop the server
        /*!
            \return 'true' if the server was successfully stopped, 'false' if the server is already stopped
        */
        bool Stop() { return _server->get()->Stop(); }
        //! Restart the server
        /*!
            \return 'true' if the server was successfully restarted, 'false' if the server failed to restart
        */
        bool Restart() { return _server->get()->Restart(); }

        //! Multicast data to all connected sessions
        /*!
            \param buffer - Buffer to multicast
            \return 'true' if the data was successfully multicasted, 'false' if the data was not multicasted
        */
        bool Multicast(array<Byte>^ buffer) { return Multicast(buffer, 0, buffer->Length); }
        //! Multicast data to all connected clients
        /*!
            \param buffer - Buffer to multicast
            \param offset - Buffer offset
            \param size - Buffer size
            \return 'true' if the data was successfully multicasted, 'false' if the data was not multicasted
        */
        bool Multicast(array<Byte>^ buffer, long long offset, long long size)
        {
            pin_ptr<Byte> ptr = &buffer[buffer->GetLowerBound(0) + (int)offset];
            return _server->get()->Multicast(ptr, size);
        }
        //! Multicast text to all connected clients
        /*!
            \param text - Text string to multicast
            \return 'true' if the text was successfully multicasted, 'false' if the text was not multicasted
        */
        bool Multicast(String^ text)
        {
            std::string temp = marshal_as<std::string>(text);
            return _server->get()->Multicast(temp.data(), temp.size());
        }

        //! Disconnect all connected sessions
        /*!
            \return 'true' if all sessions were successfully disconnected, 'false' if the server is not started
        */
        bool DisconnectAll() { return _server->get()->DisconnectAll(); }

        //! Find a session with a given Id
        /*!
            \param id - Session Id
            \return Session with a given Id or null if the session it not connected
        */
        TcpSession^ FindSession(String^ id)
        {
            std::string temp = marshal_as<std::string>(id);
            auto session = _server->get()->FindSession(CppCommon::UUID(temp));
            return session ? std::dynamic_pointer_cast<TcpSessionEx>(session)->root : nullptr;
        }

        //! Setup option: keep alive
        /*!
            This option will setup SO_KEEPALIVE if the OS support this feature.

            \param enable - Enable/disable option
        */
        void SetupKeepAlive(bool enable) { return _server->get()->SetupKeepAlive(enable); }
        //! Setup option: no delay
        /*!
            This option will enable/disable Nagle's algorithm for TCP protocol.

            https://en.wikipedia.org/wiki/Nagle%27s_algorithm

            \param enable - Enable/disable option
        */
        void SetupNoDelay(bool enable) { return _server->get()->SetupNoDelay(enable); }
        //! Setup option: reuse address
        /*!
            This option will enable/disable SO_REUSEADDR if the OS support this feature.

            \param enable - Enable/disable option
        */
        void SetupReuseAddress(bool enable) { return _server->get()->SetupReuseAddress(enable); }
        //! Setup option: reuse port
        /*!
            This option will enable/disable SO_REUSEPORT if the OS support this feature.

            \param enable - Enable/disable option
        */
        void SetupReusePort(bool enable) { return _server->get()->SetupReusePort(enable); }

    protected:
        //! Create TCP session factory method
        /*!
            \return TCP session
        */
        virtual TcpSession^ CreateSession() { return gcnew TcpSession(this); }

    protected:
        //! Handle server started notification
        virtual void OnStarted() {}
        //! Handle server stopped notification
        virtual void OnStopped() {}

        //! Handle session connected notification
        /*!
            \param session - Connected session
        */
        virtual void OnConnected(TcpSession^ session) {}
        //! Handle session disconnected notification
        /*!
            \param session - Disconnected session
        */
        virtual void OnDisconnected(TcpSession^ session) {}

        //! Handle error notification
        /*!
            \param error - Error code
            \param category - Error category
            \param message - Error message
        */
        virtual void OnError(int error, String^ category, String^ message) {}

    internal:
        TcpSession^ InternalCreateSession() { return CreateSession(); }
        void InternalOnStarted() { OnStarted(); }
        void InternalOnStopped() { OnStopped(); }
        void InternalOnConnected(TcpSession^ session) { OnConnected(session); }
        void InternalOnDisconnected(TcpSession^ session) { OnDisconnected(session); }
        void InternalOnError(int error, String^ category, String^ message) { OnError(error, category, message); }

    protected:
        !TcpServer() { _server.Release(); }

    internal:
        CSharpServer::Service^ _service;
        Embedded<std::shared_ptr<TcpServerEx>> _server;
    };

}
