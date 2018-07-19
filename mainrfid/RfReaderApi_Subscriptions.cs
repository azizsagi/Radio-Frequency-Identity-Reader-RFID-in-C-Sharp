using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Siemens.Simatic.RfReader.ReaderApi;


namespace Siemens.Simatic.RfReader
{
	/// <summary>
    /// This class implements the IRfReaderApi interface which provides
    /// a simple wrapper layer around the XML USER protocol.
    /// Currently we transform the simple .NET commands into XML USER API 
    /// commands and send them via TCP/IP to the reader
	/// </summary>
	public partial class RfReaderApi : IRfReaderApi
	{
		/// <summary>Shortcut for notification channel</summary>
		public const string TER = "EVENT";
		/// <summary>Shortcut for alarm channel</summary>
		public const string ALARM = "ALARM";
        /// <summary>Shortcut for all available channels</summary>
		public const string ALL = "ALL";
		
		/// <summary>The notification channel type we want to use</summary>
		public enum ChannelType {
			/// <summary>Specifies an RFID notification channel</summary>
			Notification = 1,
			/// <summary>Specifies an alarm channel</summary>
			Alarm = 2
		};


		/// <summary>
		/// 
		/// </summary>
        /// <param name="iPAddress">IP Address of listener, to which channel send data</param>
        ///  <param name="port">Port of listener, to which channel send data</param>
        /// <param name="bufferTag">If true: buffers data during offline. Listener must acknowledge data</param>
		/// <returns></returns>
        public void Subscribe(string typeOfChannel, IPAddress iPAddress, int port, bool bufferTag)
                             
		{
            // Validate current state of Reader API
			if (!RfReaderApi.IsValid)
			{
				throw new RfReaderApiInvalidModeException();
			}

            // Tssting only: Setup listener, but do not send commands to reader
            if (RfReaderApi.testFlag_ListenOnly)
            {
                SubscribeChannelListener(typeOfChannel, iPAddress, port, bufferTag);
            }
	        else
            {
                PerfTiming perfTimer1 = new PerfTiming();
			    perfTimer1.Start();

      
                // Security check for preconditions: Are we connected at all?
                if (!this.CommandChannel.IsConnected())
                {
                    throw new RfReaderApiException(RfReaderApiException.ResultCode_System, RfReaderApiException.Error_NoConnection);
                }
           
                 // setup channel Listener for alarms
                if (RfReaderApi.TER == typeOfChannel || RfReaderApi.ALARM == typeOfChannel || RfReaderApi.ALL == typeOfChannel)
                {
                    // Tssting only: send subscribe command, but don't listen
                    if (false == RfReaderApi.testFlag_SubscribeOnly)
                    {
                        if (IPAddress.None != iPAddress)
                        {

                            SubscribeChannelListener(typeOfChannel, iPAddress, port, bufferTag);
                        }
                        else
                        {
                            UnSubscribeChannelListener(typeOfChannel);
                        }
                    }
                }
                else 
                {
                    throw new RfReaderApiException(RfReaderApiException.ResultCode_System, RfReaderApiException.Error_InvalidParameter);
                }
              
			    List<ParameterDesc> subscribeParams = new List<ParameterDesc>();
                subscribeParams.Add(new ParameterDesc("type", typeOfChannel));
                if (IPAddress.None != iPAddress)
                {
                    subscribeParams.Add(new ParameterDesc("iPAddress", iPAddress.ToString()));
                    subscribeParams.Add(new ParameterDesc("port", port.ToString()));
                }
                subscribeParams.Add(new ParameterDesc("bufferTag",bufferTag.ToString()));

                Command rpCommand = new Command("subscribe", subscribeParams);
                string commandID = "";
                string replyMsg = this.CommandChannel.execute(rpCommand.getXmlCommand(ref commandID), commandID);
			    CommandReply rpReply = CommandReply.DecodeXmlCommand(replyMsg);
			    if (rpReply == null)
			    {
				    // There is no reply from our connected reader
				    throw new RfReaderApiException(RfReaderApiException.ResultCode_System,
					    RfReaderApiException.Error_NoReply);
			    }
			    if (rpReply.ResultCode != 0)
			    {
				    // forward errors as exceptions
				    throw new RfReaderApiException(rpReply.ResultCode, rpReply.Error,
					    rpReply.Cause);
			    }

       	        if (RfReaderApi.showPerformanceInfo)
		        {
			        ProvideInformation(this, "Subscribe took " + perfTimer1.End().ToString("##.###") + "s");
		        }
            }
		}

		/// <summary>
		/// 
		/// </summary>
        /// 
        public void Unsubscribe(string typeOfChannel)
		{
			PerfTiming perfTimer1 = new PerfTiming();
			perfTimer1.Start();


            // Validate current state of Reader API
			if (!RfReaderApi.IsValid)
			{
				throw new RfReaderApiInvalidModeException();
			}

            if (RfReaderApi.TER == typeOfChannel || RfReaderApi.ALARM == typeOfChannel || RfReaderApi.ALL == typeOfChannel)
            {
                UnSubscribeChannelListener(typeOfChannel);
            }
            else
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_System, RfReaderApiException.Error_InvalidParameter);
            }

			// Send unsubscribe command to ReaderService
			// We do this deliberately after turning down our channels in order not
			// to prevent channel shutdowns if exception during unsubscribe commands occur 
			// Do a security check whether we are connected at all
			if (!this.CommandChannel.IsConnected())
			{
				throw new RfReaderApiException(RfReaderApiException.ResultCode_System, RfReaderApiException.Error_NoConnection);
			}
			List<ParameterDesc> unsubscribeParams = new List<ParameterDesc>();
            unsubscribeParams.Add(new ParameterDesc("type", typeOfChannel));

            Command rpCommand = new Command("unsubscribe", unsubscribeParams);
            string commandID = "";
            string replyMsg = this.CommandChannel.execute(rpCommand.getXmlCommand(ref commandID), commandID);
            CommandReply rpReply = CommandReply.DecodeXmlCommand(replyMsg);
			if (rpReply == null)
			{
				// There is no reply from our connected reader
				throw new RfReaderApiException(RfReaderApiException.ResultCode_System,
					RfReaderApiException.Error_NoReply);
			}
			if (rpReply.ResultCode != 0)
			{
				// forward errors as exceptions
				throw new RfReaderApiException(rpReply.ResultCode, rpReply.Error,
					rpReply.Cause);
			}

			if (RfReaderApi.showPerformanceInfo)
			{
				ProvideInformation(this, "Unsubscribe took " + perfTimer1.End().ToString("##.###") + "s");
			}
		}

        private void SubscribeChannelListener(string channelName, IPAddress ipAddress, int port, bool bufferTag)
         {

		    // Check whether channel is already subscribed
		    if (this.channelListeners.ContainsKey(channelName))
            {
                // Make sure a working listener is stopped
                this.stopChannelListeners[channelName] = true;

                // Turn down listener
                if (this.channelListeners[channelName] != null)
                {
                    this.channelListeners[channelName].StopListening();
                    this.channelListeners[channelName].Connected -=
                                new RfNotificationListener.TcpListenerEventDlgt(OnConnected);
                    this.channelListeners[channelName] = null;

                    this.channelListeners.Remove(channelName);
                }
            }

		    try
		    {
			    // Create a new listener.
                this.channelListeners[channelName] = new RfNotificationListener(channelName, ipAddress, port, bufferTag, 100);
			    this.channelListeners[channelName].Connected +=
						    new RfNotificationListener.TcpListenerEventDlgt(OnConnected);
			    this.channelListeners[channelName].StartListening();
			    this.stopChannelListeners[channelName] = false;
		    }
		    catch(System.Exception ex)
		    {
			    ProvideInformation(this, ex);

			    // Cleanup channel listener entry on failure
			    this.channelListeners[channelName] = null;
		    }


		    // Do a security check if we have a valid listener
		    if (this.channelListeners[channelName] == null)
		    {
			    // There is something wrong with setting up a listener
			    throw new RfReaderApiException(RfReaderApiException.ResultCode_System,
				    RfReaderApiException.Error_Internal);
		    }
        }

        private void UnSubscribeChannelListener(string channelName)
        {
             // Check whether channel is already subscribed
            if (this.channelListeners.ContainsKey(channelName))
            {
                // Make sure a working listener is stopped
                this.stopChannelListeners[channelName] = true;

                // Turn down listener
                if (this.channelListeners[channelName] != null)
                {
                    this.channelListeners[channelName].StopListening();
                    this.channelListeners[channelName].Connected -=
                                new RfNotificationListener.TcpListenerEventDlgt(OnConnected);
                    this.channelListeners[channelName] = null;

                    this.channelListeners.Remove(channelName);
                }
            }
            if (channelName == RfReaderApi.ALL)
            {
                UnSubscribeChannelListener(RfReaderApi.ALARM);
                UnSubscribeChannelListener(RfReaderApi.TER);
            }

        }

	
		/// <summary>
		/// Gets called whenever a connection socket was accepted by the internal listener
		/// </summary>
		/// <param name="sender"> 
		/// The <see cref="RfNotificationListener"/> instance
		/// </param>
		/// <param name="e">
		/// A <see cref="TcpListenerEventArgs"/> instance holding the socket connection
		/// </param>
		void OnConnected(object sender, TcpListenerEventArgs e)
		{
            string reply = "";
            bool secondTry = false;

            // implementation of nonblocking mode:
            // wait on receive for 500 ms. 
            // if we doesn't implement timeout, our application will not shutdown.
            e.Socket.ReceiveTimeout = 500;

			while (!this.stopChannelListeners[e.ChannelName])
			{
				try
				{
                    // listen for data only if
                    //    - data is available
                    // or - we doesn't have to decode a message
                    if (e.Socket.Available > 0)
					{
						byte[] receiveBuffer = new byte[e.Socket.Available];
						int bytesReceived = e.Socket.Receive(receiveBuffer);

                        if (0 < bytesReceived)
                        {
                            // Decode incoming information and create streams without breaks.
                            reply = reply + Encoding.ASCII.GetString(receiveBuffer, 0, bytesReceived);
                            reply = reply.Replace("\0", "");
                            if (RfReaderApi.showDebugInfo)
                            {
                                RfReaderApi.CurrentApi.ProvideInformation("NC", InformationType.Debug, "----async nac: all----" + reply);
                            }
                        }
                    }
                    if(0 < reply.Length)
					{
                       secondTry = ! (decodeMessage(ref reply, secondTry, sender, e));
					}
				}
				catch (SocketException /*ex */)
				{
					// No data available
                    // empty message
                    reply = "";
				}
                // give other task time to run
                Thread.Sleep(1); 
			}	// end of while

			e.CloseConnection();

			// Reenable socket receives
			this.stopChannelListeners[e.ChannelName] = false;
		}

        /// <summary>
		/// Find the first message and delivere it and if necessary acknowledge message 
        /// Cut the reply string 
        /// Returns false if message is not complete and we have to wait for remaining input
		/// </summary>
		/// <param name="incomeMessage"></param>
        /// <param name="sender"> 
        /// The <see cref="RfNotificationListener"/> instance
        /// </param>
        /// <param name="e">
        /// A <see cref="TcpListenerEventArgs"/> instance holding the socket connection
        /// </param>

        /// <returns></returns>

        bool decodeMessage(ref string reply, bool secondTry, object sender, TcpListenerEventArgs e)
        {
            string id = "";
            string   commandXMLTag ="";
            Queue<string> debugData = new Queue<string> ();
            Queue<RfAlarm[]> alarmData = new Queue<RfAlarm[]> ();
            Queue<RfReport[]> reportData = new Queue<RfReport[]> ();

            // if we got a message check if we have to acknowledge it.
            if (   (decodeAsyncData(ref reply, ref id,ref  commandXMLTag, ref debugData, ref alarmData,ref reportData)) 
                && e.ackMessage)
            {
                string handshakeReply = "<reply><id>" + id + "</id><resultCode>0</resultCode><" + commandXMLTag + "></reply>";
                byte[] bytes = Encoding.ASCII.GetBytes(handshakeReply);
                int sentBytes = e.Socket.Send(bytes);
                if (RfReaderApi.showDebugInfo)
                {
                    debugData.Enqueue("----aync nac: Ack----" + handshakeReply);
                }
            }

            RfReaderApi.CurrentApi.deliverMessages(debugData, alarmData, reportData);
            
            if (secondTry)
            {
                reply = "";
                return true;
            }
            if (0 < reply.Length)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Decode the message for asynchron data (Alarm and TER. Deliver async data to connected listener
        /// Cut the reply string 
        /// find message id and return it to caller
        /// find type of command and return it to caller
        /// Returns false if message is not complete and we have to wait for remaining input
        /// </summary>
        /// <param name="reply"></param>
        /// <param name="id">message id, extracted from XML message </param> 
        /// <param name="commandXMLTag">type of command, extracted from XML message</param>
        /// <param name="debugData">queue with debug messages</param>
        /// <param name="alarmData">queue with received alarm messages</param>
        /// <param name="reportData">queue with received report messages</param>
        /// <returns></returns>

        public bool decodeAsyncData(ref string reply, ref string id, ref string commandXMLTag,
                                                ref Queue<string> debugData,  
                                                ref Queue<RfAlarm[]> alarmData,
                                                ref Queue<RfReport[]> reportData)
        {
            const string notificationType = "report";
            const string alarmType = "alarm";
            const string idType = "id";

            string startXMLTag = "";
            string endXML_Tag = "";
            int startIndex = 0;
            int startIndex_Notification = 0;
            int startIndex_Alarm = 0;
            int endIndex = 0;

            startIndex_Notification = reply.IndexOf("<" + notificationType + ">");
            startIndex_Alarm = reply.IndexOf("<" + alarmType + ">");

            // no decodeable message        
            if (0 > startIndex_Notification && 0 > startIndex_Alarm)
            {
                reply = "";
                return false;
            }
            // check for different types
            if (                      0 <= startIndex_Notification
                && (    startIndex_Alarm > startIndex_Notification
                    ||  0                > startIndex_Alarm ))
            {
                startIndex = startIndex_Notification;
                startXMLTag = "<" + notificationType + ">";
                endXML_Tag = "</" + notificationType + ">";
            }
            else 
            {
                startIndex = startIndex_Alarm;
                startXMLTag = "<" + alarmType + ">";
                endXML_Tag = "</" + alarmType + ">";
            }
           
            // throw away all unnecessary data
            reply = reply.Substring(startIndex);

            // search end of message
            if (0 <= (endIndex = reply.IndexOf(endXML_Tag)))
            {
                // calculate end of first message position
                // -> endIndes = length of message
                endIndex += endXML_Tag.Length;

                // There are cases when more than a single message is delivered
                // in a single XML string. We first have to split the XML string into
                // separate messages to prevent XML parsing exceptions
                string msgToParse = reply.Substring(0, endIndex);
                if (RfReaderApi.showDebugInfo)
                {
                    debugData.Enqueue("----async: one----" + msgToParse);
                }

                // Todo: own function
                // Distinguish between alarms and notifications 
                if (startXMLTag == "<" + notificationType + ">")
                {
                    // create an event list for notifications
                    RfReport[] tagEventList = RfReport.GetListFromReport(msgToParse);
                    reportData.Enqueue(tagEventList);

                }
                else
                {
                    // ...and create an alarm list
                    RfAlarm[] alarmList = RfAlarm.GetAlarmsFromMessage(msgToParse);
                    alarmData.Enqueue(alarmList);

                }
                // get message id
                int startIndex1 = msgToParse.IndexOf("<" + idType + ">") + 2 + idType.Length;
                int endIndex1 = msgToParse.IndexOf("</" + idType + ">");
                id = msgToParse.Substring(startIndex1, endIndex1 - startIndex1);

                // get command 
                startIndex1 = msgToParse.IndexOf("</" + idType + ">") + 2 + idType.Length;
                startIndex1 = msgToParse.IndexOf("<", startIndex1) + 1;
                endIndex1 = msgToParse.IndexOf(">", startIndex1);
                commandXMLTag = msgToParse.Substring(startIndex1, endIndex1 - startIndex1);

                // Is there a further message available?
                if (reply.Length > endIndex)
                {
                    reply = reply.Substring(endIndex);
                }
                else
                {
                    reply = "";
                }
                return true;
            }
            return false;
        }


		/// <summary>
		/// Send alarms to clients 
		/// </summary>
		/// <param name="sender">THe originating object</param>
		/// <param name="alarmArgs">The list of alarms as AlarmArgs</param>
		internal void SendAlarms(object sender, RfAlarmArgs alarmArgs)
		{
			// Notify clients about received tag events
			if (null != this.Alarms)
			{
				this.Alarms(sender, alarmArgs);
			}
		}

		/// <summary>
		/// This is our map for all listeners against the
		/// RFID runtime. Both notification channels and alarm channels
		/// will be included
		/// </summary>
		protected Dictionary<string, RfNotificationListener> channelListeners =
			 new Dictionary<string, RfNotificationListener>();

		/// <summary>
		/// This is our flag to stop listening on a certain socket
		/// </summary>
		protected Dictionary<string, bool> stopChannelListeners =
			 new Dictionary<string, bool>();

		/// <summary>
		/// The proxy channels for our listeners in filter mode
		/// </summary>
		internal Dictionary<string, CommandChannel> proxyChannels =
			new Dictionary<string, CommandChannel>();

		/// <summary>
		/// 
		/// </summary>
		public event RfNotificationHandler TagEventNotifications;

		/// <summary>
		/// 
		/// </summary>
		public event RfAlarmHandler Alarms;

    #region Helper
		/// <summary>
		/// Retrieve IP address and port from given connection string
		/// </summary>
		/// <param name="connectionString"></param>
		/// <param name="ipAddr"></param>
		/// <param name="port"></param>
		/// <returns></returns>
		protected bool GetAddressAndPort(string connectionString, out string ipAddr, out int port)
		{
			ipAddr = "";
			port = 0;
			bool fResult = false;

			if (null == connectionString || connectionString.Length < 3)	// at least "a:2"
			{
				return fResult;
			}

			try
			{
				// Search deliminators
				int ipPos = connectionString.LastIndexOf("//");
				int portPos = connectionString.LastIndexOf(":");
				int endOfPortPos = connectionString.LastIndexOf("?");

				// Do we have a valid format
				if (ipPos > -1 && portPos > -1)
				{
					// Get Address
					ipAddr = connectionString.Substring(ipPos + 2, portPos - ipPos - 2);

					if (endOfPortPos > -1)
					{
						string proxyPortString = connectionString.Substring(portPos + 1, endOfPortPos - portPos - 1);
						port = Int32.Parse(proxyPortString);
					}
					else
					{
						string proxyPortString = connectionString.Substring(portPos + 1, connectionString.Length - portPos - 1);
						port = Int32.Parse(proxyPortString);
					}

					fResult = true;
				}
			}
			catch (System.Exception)
			{
				// Conversion failed, return false
			}

			return fResult;
		}

    #endregion Helper
	};
}
