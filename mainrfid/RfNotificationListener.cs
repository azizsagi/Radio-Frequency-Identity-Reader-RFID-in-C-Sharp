using System;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// An instance of this class is 
	/// sent when a connection was accepted
	/// </summary>
	public class TcpListenerEventArgs : EventArgs
	{
		/// <summary>
		/// Creates a new TcpListenerEventArgs instance
		/// </summary>
		/// <param name="socket">
		/// Accepted socket connection
		/// </param>
		/// <param name="channelName">
		/// The channel that produced this event
		/// </param>
		public TcpListenerEventArgs(Socket socket, string channelName, bool ackMessage)
		{
			this.m_Socket = socket;
			this.m_channelName = channelName;
            this.m_ackMessage = ackMessage;
		}

		/// <summary>
		/// Gets the socket connection
		/// </summary>
		public Socket Socket
		{
			get { return m_Socket; }
		}

		/// <summary>
		///  Socket connection
		/// </summary>
		protected Socket m_Socket;

		/// <summary>
		/// The channel that sent this event
		/// </summary>
		public string ChannelName
		{
			get { return this.m_channelName; }
		}
		/// <summary>
		///  The channel this socket belongs to
		/// </summary>
		protected string m_channelName;

        /// <summary>
        /// flag, indicating if message must be acknowledged
        /// </summary>
        public bool ackMessage
        {
            get { return this.m_ackMessage; }
        }
        /// <summary>
        ///  The channel this socket belongs to
        /// </summary>
        protected bool m_ackMessage;

		/// <summary>
		/// Closes the socket connection
		/// </summary>
		public void CloseConnection()
		{
			m_Socket.Shutdown(SocketShutdown.Both);
			m_Socket.Close();
			m_Socket = null;
		}
	}

	/// <summary>
	/// This class repeatedly listens 
	/// on a IP-Port for connection attempts.
	/// </summary>
	public class RfNotificationListener
	{
		/// <summary>
		/// Delegate to add Connected event-handler
		/// </summary>
		public delegate void TcpListenerEventDlgt(object sender, TcpListenerEventArgs e);

		/// <summary>
		/// This event is fired when a socket connection has been accepted
		/// </summary>
		public event TcpListenerEventDlgt Connected;

		/// <summary>
		/// Local address
		/// </summary>
		internal IPEndPoint localEndpoint;

		/// <summary>
		/// Socket to listen repeatedly for incoming connection attempts
		/// </summary>
		internal Socket listener = null;

		/// <summary>
		/// Specifies the number of incoming connections that can be queued for acceptance
		/// </summary>
		internal int pendingConnectionQueueSize;

        /// <summary>
        /// Indicates if messages have to be acknowledged
        /// </summary>
        internal bool ackMessage = false;

		/// <summary>
		/// The channel's name we listen to.
		/// Returns an empty string for unnamed channels
		/// </summary>
		public string ChannelName
		{
			get { return this.channelName; }
		}

		/// <summary>
		/// The channel'S name we are about to listen to
		/// </summary>
		internal string channelName = "";

		/// <overloads>
		/// Initializes a new instance of the NonBlockingTcpListener class.
		/// </overloads>
		/// <summary>
		/// Creates a listener using the local IP address.
		/// The service provider will assign an port 10002.
		/// You can discover what local network address and port number has been assigned by using the 
		/// <see cref="RfNotificationListener.IpAddress"/> and 
		/// <see cref="RfNotificationListener.Port"/> properties.
		/// The default maximum number of pending connections is 100.
		/// </summary>
		public RfNotificationListener(string channelName) : this(channelName, 10002)
		{
		}

		/// <summary>
		/// Creates a listener using the local IP address and the specified port number.
		/// You can discover what local network address has been assigned by using the 
		/// <see cref="RfNotificationListener.IpAddress"/> and 
		/// <see cref="RfNotificationListener.Port"/> properties.
		/// The default maximum number of pending connections is 100.
		/// </summary>
		/// <param name="channelName">The channel's name</param>
		/// <param name="port">
		/// Number of the port
		/// </param>
		public RfNotificationListener(string channelName, int port) : this(channelName, port,false)
		{
		}

        /// <summary>
        /// Creates a listener using the local IP address the specified port number and the 
        /// type of acknowledgement
        /// You can discover what local network address has been assigned by using the 
        /// <see cref="RfNotificationListener.IpAddress"/> and 
        /// <see cref="RfNotificationListener.Port"/> properties.
        /// The default maximum number of pending connections is 100.
        /// </summary>
        /// <param name="channelName">The channel's name</param>
        /// <param name="port">
        /// Number of the port
        /// </param>
        public RfNotificationListener(string channelName, int port, bool ack)
            : this(channelName, port, ack, 100)
        {
        }

		/// <summary>
		/// Creates a listener using the local IP address,
		/// the specified port number and the maximum number of pending connections.
		/// If you do not care which local port is used, you can use 0 for the port number. 		
		/// You can discover what local network address has been assigned by using the 
		/// <see cref="RfNotificationListener.IpAddress"/> and 
		/// <see cref="RfNotificationListener.Port"/> properties.
		/// The default maximum number of pending connections is 100.
        /// Default is no acknowledgement.
		/// </summary>
		/// <param name="channelName">The channel's name</param>
		/// <param name="port">Number of the port</param>
        /// <param name="ack">true=acknowladge Message</param>
		/// <param name="maxPendingConnections">
		/// Maximum number of pending connection attempts
		/// </param>
		public RfNotificationListener(string channelName, int port, bool ack, int maxPendingConnections)
            :this(channelName, IPAddress.Parse("127.0.0.1"),port, ack, 100)
        {
        }

            	/// <summary>
		/// Creates a listener using the local IP address,
		/// the specified port number and the maximum number of pending connections.
		/// If you do not care which local port is used, you can use 0 for the port number. 		
		/// You can discover what local network address has been assigned by using the 
		/// <see cref="RfNotificationListener.IpAddress"/> and 
		/// <see cref="RfNotificationListener.Port"/> properties.
		/// The default maximum number of pending connections is 100.
        /// Default is no acknowledgement.
		/// </summary>
		/// <param name="channelName">The channel's name</param>
		/// <param name="port">Number of the port</param>
        /// <param name="ack">true=acknowladge Message</param>
		/// <param name="maxPendingConnections">
		/// Maximum number of pending connection attempts
		/// </param>
		public RfNotificationListener(string channelName, IPAddress ipAddress, int port, bool ack, int maxPendingConnections)
		{
			this.channelName = channelName;

			localEndpoint = new IPEndPoint(ipAddress, port);

			if(0 == maxPendingConnections)
			{
				maxPendingConnections = 100;
			}

			pendingConnectionQueueSize = maxPendingConnections;

            ackMessage = ack;
		}

		/// <summary>
		/// Gets/Sets the local IP address to listen on.
		/// <para>
		/// <b>NOTE:</b> 
		/// If the Listener is already listening, <see cref="StopListening"/> will be called.
		/// All pending connections will be closed too.
		/// The listener is started afterwards automatically with the new setting.
		/// </para>
		/// </summary>
		public string IpAddress
		{
			get
			{
				if(IsListening)
				{
					return ((IPEndPoint)listener.LocalEndPoint).Address.ToString();
				}
				else
				{
					return localEndpoint.Address.ToString();
				}

			}

			set
			{
				// do not unnecessarily stop listening
				if(value.Equals(IpAddress)) 
					return;

				bool restart = false;
				if(IsListening)
				{
					StopListening();
					restart = true;
				}
				localEndpoint.Address = IPAddress.Parse(value);

				if(restart == true)
				{
					StartListening();
				}
			}
		}

		/// <summary>
		/// Gets/Sets the Port to listen on.
		/// <para>
		/// <b>NOTE:</b> If the Listener is already listening, <see cref="StopListening"/> will be called.
		/// All pending connections will be closed too.
		/// The listener is started afterwards automatically with the new setting.
		/// </para>
		/// </summary>
		public int Port
		{
			get
			{
				if(IsListening)
				{
					return ((IPEndPoint)listener.LocalEndPoint).Port;
				}
				else
				{
					return localEndpoint.Port;
				}
			}

			set
			{
				// do not unnecessarily stop listening
				if(value == Port) 
					return;

				bool restart = false;
				if(IsListening)
				{
					StopListening();
					restart = true;
				}

				localEndpoint.Port = value;

				if(restart == true)
				{
					StartListening();
				}
			}
		}

		/// <summary>
		/// Gets/sets pendingConnectionQueueSize. The default is 100.
		/// <para>
		/// <b>NOTE:</b> If the Listener is already listening, <see cref="StopListening"/> will be called.
		/// All pending connections will be closed too. 
		/// The listener is started afterwards automatically with the new setting.
		/// </para>
		/// </summary>
		public int PendingConnectionQueueSize
		{
			get 
			{ 
				return pendingConnectionQueueSize; 
			}
			set
			{
				// do not unnecessarily stop listening
				if(value == PendingConnectionQueueSize) 
					return;

				bool restart = false;
				if(IsListening)
				{
					StopListening();
					restart = true;
				}

				pendingConnectionQueueSize = value; 

				if(restart == true)
				{
					StartListening();
				}
			}
		}

		/// <summary>
		/// Returns true if the listener is already listening
		/// </summary>
		public bool IsListening
		{
			get
			{
				return (null != listener);
			}
		}

		/// <summary>
		/// Begins listening for incoming connections.  This method returns immediately.
		/// Incoming connections are reported using the Connected event.
		/// </summary>
		public void StartListening()
		{
			if(IsListening) return;

			listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			// Seems to be a problem on Win CE to mix non-blocking mode with blocking calls
			//listener.Blocking = false;	// Make sure we are non-blocking
			listener.Bind(localEndpoint);
			listener.Listen(pendingConnectionQueueSize);
			listener.BeginAccept(new AsyncCallback(AcceptConnection), null);
		}

		/// <summary>
		/// Shuts down the listener.
		/// </summary>
		public void StopListening()
		{
			if(!IsListening) return;

			// Make sure we're not accepting a connection.
			lock (this)
			{
				listener.Close();
				listener = null;
			}
		}

		/// <summary>
		/// Accepts the connection and invokes any Connected event handlers.
		/// </summary>
		/// <param name="res">
		/// See <see cref="System.Net.Sockets.Socket.BeginAccept"/> for details 
		/// </param>
		protected void AcceptConnection(IAsyncResult res)
		{
			Socket connection = null;

			// Make sure listener doesn't go null on us.
			lock (this)
			{
				// Do we have to stop 
				if(!IsListening)
					return;

				// Accept any incoming connection... 
				connection = listener.EndAccept(res);
				// Prepare for new connections

				listener.BeginAccept(new AsyncCallback(AcceptConnection), null);
			}

			// Now handle the newly created socket
			OnConnected(new TcpListenerEventArgs(connection, this.ChannelName, this.ackMessage));
		}

		/// <summary>
		/// Fire the Connected event if it exists.
		/// </summary>
		/// <param name="e">
		/// See <see cref="TcpListenerEventArgs"/> for details 
		/// </param>
		protected virtual void OnConnected(TcpListenerEventArgs e)
		{
			if(Connected != null)
			{
				try
				{
					Connected(this, e);
				}
				catch (Exception ex)
				{
					string strex = ex.Message;
				}
			}
			// Do not close the connection directly but simply wait for the called
			// handler to return.
			// As this method is a callback of an accepted socket we already have
			// a thread of our own.
		}
	}
}
