#pragma once

#include "Service.h"

namespace CSharpServer {

    //! TCP Endpoint
    public ref class TcpEndpoint
    {
    internal:
        //! Initialize empty TCP endpoint
        TcpEndpoint() {}

    public:
        //! Initialize TCP endpoint with a given port number and protocol
        /*!
            \param port - Port number
            \param protocol - Protocol type
        */
        TcpEndpoint(int port, CSharpServer::InternetProtocol protocol);
        //! Initialize TCP endpoint with a given address and port number
        /*!
            \param address - Address
            \param port - Port number
        */
        TcpEndpoint(String^ address, int port);
        //! Initialize TCP endpoint with another endpoint instance
        /*!
            \param endpoint - Endpoint
        */
        TcpEndpoint(TcpEndpoint^ endpoint);
        ~TcpEndpoint() { this->!TcpEndpoint(); }

    protected:
        !TcpEndpoint() { _endpoint.Release(); };

    internal:
        Embedded<asio::ip::tcp::endpoint> _endpoint;
    };

    //! UDP Endpoint
    public ref class UdpEndpoint
    {
    internal:
        //! Initialize empty UDP endpoint
        UdpEndpoint() {}

    public:
        //! Initialize UDP endpoint with a given port number and protocol
        /*!
            \param port - Port number
            \param protocol - Protocol type
        */
        UdpEndpoint(int port, CSharpServer::InternetProtocol protocol);
        //! Initialize UDP endpoint with a given address and port number
        /*!
            \param address - Address
            \param port - Port number
        */
        UdpEndpoint(String^ address, int port);
        //! Initialize UDP endpoint with another endpoint instance
        /*!
            \param endpoint - Endpoint
        */
        UdpEndpoint(UdpEndpoint^ endpoint);
        ~UdpEndpoint() { this->!UdpEndpoint(); }

    protected:
        !UdpEndpoint() { _endpoint.Release(); };

    internal:
        Embedded<asio::ip::udp::endpoint> _endpoint;
    };

}
