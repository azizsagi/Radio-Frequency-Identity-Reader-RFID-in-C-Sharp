using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using Siemens.Simatic.RfReader;
using System.Threading;

namespace Siemens.Simatic.RfReader.ReaderApi
{
	/// <summary>
	/// This class encapsulates the communication between the API
	/// and an underlying reader service application over sockets via TCP.
	/// </summary>
    internal class CommandChannel
    {
		/// <summary>
		/// Create a new command channel object
		/// </summary>
		public CommandChannel()
		{
		}

		/// <summary>
		/// Contains the name of the targeted object
		/// </summary>
		public string TargetName
		{
			get { return this.targetName; }
			set { this.targetName = value; }
		}
		private string targetName = null;

		/// <summary>This is our buffer size</summary>
		private const int SendReceiveBufferSize = 16500;
        byte[] buffer = new byte[CommandChannel.SendReceiveBufferSize];

		/// <summary>
		/// This target address our commands are sent to.
		/// </summary>
		public IPAddress IPAddress
		{
			get { return this.ipAddress; }
			set { this.ipAddress = value; }
		}
		private IPAddress ipAddress = new IPAddress(new byte[4] { 127, 0, 0, 1 });

		/// <summary>
		/// The port we use for communication
		/// </summary>
		public int Port
		{
			get { return this.ipPort;  }
			set { this.ipPort = value; }
		}
        private int ipPort = 10001;

        /// <summary>
        /// Flag indicating if alarms and reports has to be acknowledged
        /// </summary>
        public bool AckAsyncData
        {
            get { return this.ackAsyncData; }
            set { this.ackAsyncData = value; }
        }
        private bool ackAsyncData = false;

		/// <summary>
		/// A TCP client to access the command channel port of the target
		/// </summary>
		public TcpClient RfTcpClient
		{
			get {
				// Create a new TCP client if none available
				if (null == this.tcpClient)
				{
					this.tcpClient = new TcpClient();
				}
				return this.tcpClient;
			}
		}
		private TcpClient tcpClient = null;

		/// <summary>
		/// The network stream we use as a means of communication
		/// </summary>
		public NetworkStream RfNetworkStream
		{
			get {
				// Return a stream if there is a valid TCP client
				// connection
				if (null == this.networkStream)
				{
					if (null != this.RfTcpClient)
					{
						this.networkStream = this.RfTcpClient.GetStream();
					}
				}
				return this.networkStream;
			}
		}
		private NetworkStream networkStream = null;

		/// <summary>
		///  This member encapsulates a unique consequtive
		///  number used as an identifier for messages sent via
		/// the command channel
		/// </summary>
		public static int NewCommandID
		{
			get	{
				return CommandChannel.commandID++;
			}
		}
		private static int commandID = 1;

		/// <summary>
		/// Check whether a connection to an underlying reader service application
		/// is available
		/// </summary>
		/// <returns>Returns true if there is a working connection available.</returns>
		public bool IsConnected()
		{
			return isConnected;
		}
        private bool isConnected = false;

        /// <summary>Our internal thread
        /// watching for incoming alarams and notifications on the command channel</summary>
        private Thread commandListenerThread = null;
        private bool stopcommandListenerThread = false;

		/// <summary>
		/// Whenever we are destructed we make sure that the connection is closed
		/// </summary>
		~CommandChannel()
        {
            closeConnection();
        }

		/// <summary>
		/// Close the TCP connection
		/// </summary>
        public void closeConnection()
        {

            // wait until commandListnerThread is stopped
            stopcommandListenerThread = true;

            for (int i = 0; i < 3 && (null != commandListenerThread); i++)
            {
                Thread.Sleep(100);
            }

            // force stopping of commandListnerThread
            lock (this.communicationLock) 
            {
                if (null != commandListenerThread)
                {
                    commandListenerThread.Abort();
                    commandListenerThread = null;
                }
            }
            // The network stream retrieved from a TCP client is only valid
            // after connect has been called on the TCP client and
            // will not (!) automatically be closed when the TCP client closes.
            if (this.networkStream != null)
            {
                this.networkStream.Close();
                this.networkStream = null;
            }

            // Close Connection
            if (this.tcpClient != null)
            {
                this.tcpClient.Close();
                this.tcpClient = null;
            }

            this.isConnected = false;
        }

		/// <summary>
		/// Try to connect using the internal values for ipaddress and port
		/// that were set before.
		/// </summary>
		/// <returns>true for success</returns>
		public bool connect(bool ackData)
		{
            return connect(this.IPAddress, this.Port, ackData);
		}

		/// <summary>
		/// Connect to a socket for communication and send an initial handshake message
		/// </summary>
		/// <param name="addr">The IP address to connect to.</param>
		/// <param name="port">The port number to be used.</param>
		/// <returns>true for success</returns>
        public bool connect(IPAddress addr, int port, bool ackData)
        {
            bool fResult = false;
            // set acknowleging.
            this.ackAsyncData = ackData;
            try
            {
				if (RfReaderApi.showDebugInfo)
				{
					RfReaderApi.CurrentApi.ProvideInformation("CmdChn", InformationType.Debug, "CmdChn connect " + addr.ToString() + "," + port.ToString());
				}
				RfTcpClient.Connect(addr, port);

                this.isConnected = true;

                fResult = this.isConnected;
            }
            catch(System.Net.Sockets.SocketException ex)
            {
                if (RfReaderApi.showDebugInfo)
                {
                    RfReaderApi.CurrentApi.ProvideInformation("CommandChannel", "Socket Error " + ex.ErrorCode.ToString());
                    RfReaderApi.CurrentApi.ProvideInformation("CommandChannel", ex);
                }
				// Check if we got an "already connected" exception
				// In that case everything is ok in spite of an exception
				if (ex.ErrorCode == 10056)
				{
					this.isConnected = true;
					fResult = true;
				}
			}
            catch(System.Exception ex)
            {
				RfReaderApi.CurrentApi.ProvideInformation("CommandChannel", ex);
			}

            if (fResult)
            {
                // Start the event listener thread
                if (null == this.commandListenerThread)
                {
                    stopcommandListenerThread = false; 
                    commandListenerThread = new Thread(new ThreadStart(this.WatchEventOnCommandChannel));
                    // Make sure this thread does not prevent the application from shutting down
                    commandListenerThread.IsBackground = true;
                    commandListenerThread.Start();
                }
            }
            return fResult;
        }

	

		/// <summary>
		/// Send a handshake message to prove the connection is working. 
		/// </summary>
		/// <returns>Returns true if the handshake was accepted.</returns>
        private bool handshake()
        {
            bool fHandshakeResult = false;

            // send heartbeat to reader
            Command rpCommand = new Command("heartBeat");
            string replyMsg = execute(rpCommand.getXmlCommand(), rpCommand.CommandID);
            CommandReply rpReply = CommandReply.DecodeXmlCommand(replyMsg);

            if (rpReply != null && rpReply.ResultCode == 0)
            {
					fHandshakeResult = true;
			}
			return fHandshakeResult;
        }

		/// <summary>
		/// Send a message that the connection is to be ended. But don't wait for an answer
		/// </summary>
		public void goodbye()
		{
			if (this.isConnected)
			{
                Command rpCommand = new Command("hostGoodbye");
				string replyMsg = this.execute(rpCommand.getXmlCommand(),rpCommand.CommandID,0);
			}
		}


	    /// <summary>
		/// Execute a given command by sending it via the socket and wait for a reply.
		/// </summary>
		/// <param name="command">The command as an XML string</param>
        /// <param name="commandID">Id of command. Reply for this command must have same id</param>
        /// <returns>The reply to the given command as an XML string</returns>
        public string execute(string command, string commandID)
        {
            return execute(command, commandID, RfReaderApi.CommandTimeout);
        }

		/// <summary>
		/// Execute a given command by sending it via the socket and wait for a reply.
		/// </summary>
		/// <param name="command">The command as an XML string</param>
        /// <param name="commandID">Id of command. Reply for this command must have same id</param>
        /// <param name="commandTimeout">Timeout in seconds. Max time waiting for a reply</param>
		/// <returns>The reply to the given command as an XML string</returns>
        public string execute(string command, string commandID, double commandTimeout)
        {
            string commandReply = "";
            string reply = "";
            bool timeout = false;
            Queue<string> debugData = new Queue<string>();
            Queue<RfAlarm[]> alarmData = new Queue<RfAlarm[]>();
            Queue<RfReport[]> reportData = new Queue<RfReport[]>();

			try
			{
                if (RfReaderApi.showDebugInfo)
                {
                    RfReaderApi.CurrentApi.ProvideInformation("CommandChannel", InformationType.Debug, "----msg----" + command);
                }

                // Make sure there is only one thread active if we send a command
                // and wait for an answer
                int bytesRead = 0;


                lock (this.communicationLock)
                {
                    // first check for old data
                    readAsyncData(ref debugData, ref alarmData, ref reportData);

                    // Create the network stream 
                    NetworkStream stream = this.RfNetworkStream;

                    // We have to encode the data for sending it over the network
                    byte[] bytes = Encoding.ASCII.GetBytes(command);
                    stream.Write(bytes, 0, bytes.Length);

                    // setup timer
                    PerfTiming perfTimer1 = new PerfTiming();
                    perfTimer1.Start(); 

                    // Wait for receiving a answer but only during a defined time period
                    while (0 == commandReply.Length && (!timeout))
                    {
                        try 
	                    {
                            System.Array.Clear(buffer, 0, buffer.Length);
                            stream.ReadTimeout = 100;
                            bytesRead = stream.Read(buffer, /*off*/0, CommandChannel.SendReceiveBufferSize);
	                    }
	                    catch (IOException /*ex */)
                        {
                            // No data available->empty message
                            bytesRead = 0;
                        }
                        if (bytesRead > 0)
                        {
                            bytesRead = 0;
                            // add read Data to reply message
                            reply = reply + GetReply(buffer);
                            if (RfReaderApi.showDebugInfo)
                            {
                                debugData.Enqueue("----reply----" + reply);
                            }
                            commandReply = FilterForCommmandReply(ref reply, commandID, stream, ref debugData, ref alarmData, ref reportData);
                        }
 
                        // check if we have time to try an other read
                        if (commandTimeout < perfTimer1.End())
                        {
                            timeout  = true;
                        }
                    }
                }
			}
			catch (Exception ex)
			{
                if (RfReaderApi.showDebugInfo)
                {
                    debugData.Enqueue(ex.ToString());
                }
                throw new RfReaderApiException(RfReaderApiException.ResultCode_System,
                   RfReaderApiException.Error_NoConnection);
			}
            finally
            {
                RfReaderApi.CurrentApi.deliverMessages(debugData, alarmData, reportData);
            }   
            if (timeout)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_System,
                    RfReaderApiException.Error_NoReply);
            }
			return commandReply ;
        }
       

        /// <summary>
        /// Read data from socket
        /// Deliver Alarm and TER to connected listener
        /// </summary>
        /// <param name="debugData">queue with debug messages</param>
        /// <param name="alarmData">queue with received alarm messages</param>
        /// <param name="reportData">queue with received report messages</param>
        /// <returns></returns>
        protected void readAsyncData(ref Queue<string> debugData,
                                      ref Queue<RfAlarm[]> alarmData,
                                      ref Queue<RfReport[]> reportData)
        {
            lock (this.communicationLock)
            {
                // Create the network stream 
                NetworkStream stream = this.RfNetworkStream;
                int bytesRead = 0;

                if (stream.DataAvailable)
                {
                    // Receive the answer 
                    System.Array.Clear(buffer, 0, buffer.Length);

                    try
                    {
                        stream.ReadTimeout = 1;
                        bytesRead = stream.Read(buffer, /*off*/0, CommandChannel.SendReceiveBufferSize);
                    }
                    catch (IOException /*ex */)
                    {
                        // No data available -> empty message
                        bytesRead = 0;
                    }
                    if (0 < bytesRead)
                    {
                        string reply = GetReply(buffer);
                        if (RfReaderApi.showDebugInfo)
                        {
                            debugData.Enqueue("----async cmd: all----" + reply);
                        }
                        // we have to throw away old commamd reply, so we let check for invalid commandID
                        // but we have to get Alarms and TER
                        string dummyCommandID = "x";
                        FilterForCommmandReply(ref reply, dummyCommandID, stream,
                                                ref debugData, ref alarmData, ref reportData);
                    }
                }
            }
        }

		/// <summary>
		/// Return a message string out of a received buffer.
		/// Make sure there are no invalid characters within the string
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		protected string GetReply(byte[] buffer)
		{
			string reply = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
			if (null != reply)
			{
				reply = reply.Replace("\0", "");
			}
			return reply;
		}

        /// <summary>
        /// Check received data for valid command reply.
        /// Filter received data for async data (Alarm, TER).
        /// and delivere it to connected listener
        /// </summary>
        /// <param name="reply">received data</param>
        /// <param name="commandID">command id, must be identical in command reply </param>
        /// <param name="debugData">queue with debug messages</param>
        /// <param name="alarmData">queue with received alarm messages</param>
        /// <param name="reportData">queue with received report messages</param>
        /// <returns>potental first part of a message, which needs further data</returns>
        protected string FilterForCommmandReply(ref string reply, string commandID, NetworkStream stream, 
                                                ref Queue<string> debugData,
                                                ref Queue<RfAlarm[]> alarmData,
                                                ref Queue<RfReport[]> reportData)
        {
            int startIndex = 0;
            int endIndex = 0;
            bool furtherMessages = true;
            string replyCommand = "";
            string startXMLTag = "";

            // find Alarms or notifications
            while (furtherMessages)
            {
                // do we have a start XML Tag?
                if (    0 <= (startIndex = reply.IndexOf("<" ))
                    && startIndex < (endIndex = reply.IndexOf(">",startIndex))+startIndex)
                {
                    startXMLTag = reply.Substring(startIndex+1, endIndex - startIndex -1);
                    // check for valid XML Tag
                    if (   "reply"  != startXMLTag
                        && "report" != startXMLTag
                        && "alarm"  != startXMLTag)
                    {
                        // unknown start XML Tag -> throw away
                        reply = reply.Substring(endIndex + 1);
                    }
                    // find end XML Tag
                    else if (0 <= (endIndex = reply.IndexOf("</" + startXMLTag + ">")))
                    {
                        // command reply found
                        if ("reply" == startXMLTag)
                        {
                            // save reply
                            replyCommand = reply.Substring(startIndex, endIndex + 8 - startIndex);
                            // if command ID doesnt fit, its not a valid commmand reply -> throw it away
                            checkCommandID(ref replyCommand, ref commandID);
                            // cut message 
                            reply = reply.Substring(endIndex + 8);
                        }
                        // Async data found
                        else
                        {
                            string id = "";
                            string command = "";
                            RfReaderApi.CurrentApi.decodeAsyncData(ref reply, ref id, ref command, 
                                                                    ref debugData, 
                                                                    ref alarmData, 
                                                                    ref reportData);

                            if (this.ackAsyncData)
                            {
                                string handshakeReply = "<frame><reply><id>" + id + "</id><resultCode>0</resultCode><" + command + "></reply></frame>";
                                byte[] bytes = Encoding.ASCII.GetBytes(handshakeReply);
                                stream.Write(bytes,0,bytes.Length);
                                if (RfReaderApi.showDebugInfo)
                                {
                                    debugData.Enqueue("----async cmd: ack----" + handshakeReply);
                                }
                            }
                        }
                    }
                    // no valid end Element found -> maybe we need more data return with rest of message
                    else
                    {
                        furtherMessages = false;
                    }
                }
                // no valid XML Tag found  -> throw away message
                else
                {
                    furtherMessages = false;
                    reply = "";
                }
            }
            return replyCommand;
        }

        /// <summary>
        /// Check "id" of command reply message
        /// If id doesnt fit, delete complete reply message
        /// </summary>
        /// <param name="reply">received data</param>
        /// <param name="commandID">id of sended command, must be identical in reply message</param>
        /// <returns></returns>
        private void checkCommandID(ref string commandMessage, ref string commandID)
        {
            bool idOk = false;
            int startIdIndex = commandMessage.IndexOf("<id>");
            if (0 <= startIdIndex)
            {
                startIdIndex += 4;
                int endIdIndex = commandMessage.IndexOf("</id>");
                if (0 <= endIdIndex)
                {
                    if (commandID == "" || commandID == commandMessage.Substring(startIdIndex, endIdIndex - startIdIndex))
                    {
                        idOk = true;
                    }
                }
            }
            if (!idOk)
            {
                // wrong ID, throw away reply
                commandMessage = "";
            }
        }

        /// <summary>
        /// Thread watching for alarms and notification
        /// </summary>
        /// <returns></returns>
        private void WatchEventOnCommandChannel()
        {
            try
            {
                while (false == stopcommandListenerThread)
                {
                    Thread.Sleep(100);
                    if (this.isConnected)
                    {
                        Queue <string> debugData = new Queue<string> ();
                        Queue<RfAlarm[]> alarmData = new Queue<RfAlarm[]> ();
                        Queue<RfReport[]> reportData = new Queue<RfReport[]> ();

                        readAsyncData(ref debugData, ref alarmData, ref reportData);
                        RfReaderApi.CurrentApi.deliverMessages(debugData, alarmData, reportData);
                    }
                }
            }
            catch (Exception ex)
            {
				RfReaderApi.CurrentApi.ProvideInformation("CommandChannel", ex);
            }
            lock (this.communicationLock)
            {
                commandListenerThread = null;
            }
        }

	    /// <summary>We use an internal object for locking</summary>
	    private Object communicationLock = new Object();
    }

}
