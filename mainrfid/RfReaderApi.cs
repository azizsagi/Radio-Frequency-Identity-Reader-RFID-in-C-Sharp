using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using Siemens.Simatic.RfReader.ReaderApi;
using System.Globalization;


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
    #region Constants and Definitions

        ///// <summary>This is the first reader type that is supported.</summary>
        //public const string SupportedReaderType1 = "SIMATIC_RF670R";

        ///// <summary>This is the second reader type that is supported.</summary>
        //private const string SupportedReaderType2 = "RFxxx";
        
        /// <summary>This is the first reader type that is supported.</summary> 
        private const string SupportedXmlAPIVersion = "V1.0";

        /// <summary>
		/// The max. size of a data block in bytes. 
        /// Max. data block which could be transfered through API functions at one time
        /// Bigger blocks should be splitted in smaller pieces
		/// </summary>
        private const uint maxSupportedDataLength = 4096;

        /// <summary>
        /// As this interface is bound to be extended we provide an
        /// explicit version number instead of just use the assembly version.
        /// </summary>
        private const string apiVersion = "V02.00.00.00_01.01.00.01";

        /// <summary>Provide additional information for performance measuring</summary>
		internal static bool showPerformanceInfo = false;

		/// <summary>Provide additional information for debugging purposes</summary>
		internal static bool showDebugInfo = false;

        /// <summary>Flags for tessting purposes</summary>
        internal static bool testFlag_ListenOnly = false;

        /// <summary>Flags for tessting purposes</summary>
        internal static bool testFlag_SubscribeOnly = false;


        #endregion Constants and Definitions

        #region Properties

        /// <summary>Remember reconfiguration state</summary>
        internal static bool IsValid
		{
			get {
				return !RfReaderApi.isReconfiguring;
			}
		}
		private static bool isReconfiguring = false;

        /// <summary>
		/// defines the maximum timeout for getting an answer from reader service in seconds
		/// </summary>
		internal static double CommandTimeout
		{
			set { RfReaderApi.commandTimeout = value ;}
            get { return RfReaderApi.commandTimeout; }
		}
		private static double commandTimeout = 2;

        /// <summary>
		/// defines the timeout for special commands like getConfiguration in seconds
		/// </summary>
		internal static double CommandLongTimeout
		{
			set { RfReaderApi.commandLongTimeout = value ;}
            get { return RfReaderApi.commandLongTimeout; }
		}
		private static double commandLongTimeout = 20;

		/// <summary>
		/// The command channel to communicate with the reader service
		/// </summary>
		internal CommandChannel CommandChannel
		{
			get
			{
				return this.commandChannel;
			}
		}
		private CommandChannel commandChannel = new CommandChannel();

		/// <summary>
		/// Provide additional information 
		/// </summary>
		/// <param name="perfInfo">Show performance information</param>
		/// <param name="debugInfo">Show debugging information</param>
		public static void ExtendInformation(bool perfInfo, bool debugInfo)
		{
			RfReaderApi.showPerformanceInfo = perfInfo;
			RfReaderApi.showDebugInfo = debugInfo;
		}

        /// <summary>
        /// Flags for testing puposes only.
        /// !*! Don't use this function in releas !*!
        /// No support and subject to change without any notice!
        /// </summary>
        /// <param name="listenOnly">Listen for Alarm and TER without sending comands</param>
        /// <param name="subscribeOnly">Subscribe for Alarm and TER setup listener</param>
        public static void ExtendTestFlags(bool listenOnly, bool subscribeOnly)
        {
            RfReaderApi.testFlag_ListenOnly = listenOnly;
            RfReaderApi.testFlag_SubscribeOnly = subscribeOnly;
        }

        /// <summary>
        /// ReaderInitData. This data will be updated 
        /// at StartTcpIpConnection() called and will be reset at 
        /// StopTcpIpConnection() called.
        /// </summary>

        public static RfReaderInitData ReaderInitData
        {
            get { return RfReaderApi.readerInitData; }
            set { RfReaderApi.readerInitData = value; }
        }
        private static RfReaderInitData readerInitData = null;

		/// <summary>
		/// Return an instance of the RFReader API.
		/// Creates a new instance if there is none available yet
		/// and reuses an existing instance.
		/// </summary>
		public static IRfReaderApi Current
		{
			get
			{
				// Create a new instance if there is none available
				if (null == RfReaderApi.theRfReaderApi)
				{
					try
					{
						RfReaderApi.theRfReaderApi = new RfReaderApi();
					}
					catch
					{
						RfReaderApi.theRfReaderApi = null;
					}
				}
				// ... or reuse existing instance
				return RfReaderApi.theRfReaderApi;
			}
		}
        /// <summary>
        /// Internal cache for the current instance of the reader API 
        /// </summary>
        private static IRfReaderApi theRfReaderApi = null;

        /// <summary>
        /// Return the current fully accessible instance of the API
        /// </summary>
        internal static RfReaderApi CurrentApi
        {
            get
            {
                return (RfReaderApi)RfReaderApi.Current;
            }
        }
    #endregion Properties

    #region Information distribution
        /// <summary>
		/// Event to notify any client about internal status or additional information
		/// </summary>
		public event InformationHandler Information;

		/// <summary>
		/// This is a way to provide information to clients of our API
		/// </summary>
		/// <param name="sender">The source for a message</param>
		/// <param name="message">The information itself</param>
		public void ProvideInformation(object sender, string message)
		{
			if (null != this.Information)
			{
				this.Information(sender, new InformationArgs(message));
			}
		}

		/// <summary>
		/// This is a way to provide information to clients of our API
		/// </summary>
		/// <param name="sender">The source for a message</param>
		/// <param name="type">The information'S type, e.g. error, warning, info</param>
		/// <param name="message">The information itself</param>
		public void ProvideInformation(object sender, InformationType type, string message)
		{
			if (null != this.Information)
			{
				this.Information(sender, new InformationArgs(type, message));
			}
		}

		/// <summary>
		/// This is a way to provide exception information to clients of our API
		/// </summary>
		/// <param name="sender">The source for a message</param>
		/// <param name="ex">An exception that has occured</param>
		public void ProvideInformation(object sender, System.Exception ex)
		{
			if (null != this.Information)
			{
				this.Information(sender, new InformationArgs(ex));
			}
		}

		/// <summary>
		///  The handler for information events.
		/// Currently it only passes the information through to client applications
		/// </summary>
		/// <param name="sender">The source for a message</param>
		/// <param name="args">The information itself wrapped in an event args structure</param>
		public void OnInformationHandler(object sender, InformationArgs args)
		{
			// Just pass the information on to a client
			if (null != this.Information)
			{
				this.Information(sender, args);
			}
		}
    #endregion Information distribution

    #region IRfReaderApi Members

        ///// <summary>Supported reader types </summary>
        //public string[] SupportedReaderTypes
        //{
        //    get
        //    {
        //        string[] supportedReaderType = new string[1];
        //        supportedReaderType[0] = SupportedReaderType1;
        //        return supportedReaderType;
        //    }
        //}


		/// <summary>
		/// Return the current version of this interface
		/// </summary>
		public string Version
		{
			get {
				return apiVersion; 
			}
		}

		/// <summary>
		/// Start or restart the reader.
		/// Using the given initialization data the reader is connected and configured.
		/// In all cases the connection will be closed and reopened with the
		/// newly configured data.
		/// </summary>
		/// <param name="initData"></param>
		public void StartTcpIPConnection(RfReaderInitData initData)
		{
			// Do a plausibility check on incoming paramters
            if (null == initData)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_System,
                    RfReaderApiException.Error_MissingParameter,
                    "RfReaderInitData is null");
            }
            //if ( initData.Type != RfReaderApi.SupportedReaderType1)
            //{
            //    throw new RfReaderApiException(RfReaderApiException.ResultCode_System,
            //        RfReaderApiException.Error_InvalidParameter,
            //        "Reader type is not supported");
            //}

            PerfTiming perfTimer1 = new PerfTiming();
            perfTimer1.Start();

            RfReaderApi.readerInitData = initData;

    #region ReadConfigFile
            //---------------------------------------------------
			// Get parameter from configuration file
            string configFileName = "unknown";

			try
			{
				configFileName = Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName + ".config";
				ToolHelp.Configuration.AppSettingsReader appSettings = new ToolHelp.Configuration.AppSettingsReader(configFileName);
                
                // read timeout from configuration file
                CommandTimeout = Double.Parse((string)appSettings["CommandTimeout"]);
                CommandLongTimeout = Double.Parse((string)appSettings["CommandLongTimeout"]);
 			}
			catch
			{
                if (RfReaderApi.showDebugInfo)
                {
                    ProvideInformation(this, InformationType.Debug, "Error reading Configuration: " + configFileName);
                }
			}
			if (RfReaderApi.showDebugInfo)
			{
				ProvideInformation(this, InformationType.Debug, 
                     string.Format("Using CommandTimeout = {0}s; CommancLongTimeout = {1}s",
                     CommandTimeout.ToString(),
                     CommandLongTimeout.ToString ()));
			}
    #endregion ReadConfigFile

            ProvideInformation(this, InformationType.Info, "Starting...");

            try
            {
                // If there is still an existing communication to a reader service
                // we stop it and close the connection
                if (this.CommandChannel.TargetName != null)
                {
                    // Only break connection
                    StopTcpIpConnection();
                }

                // Create an instance of the new device
                // CHECK: Do we need to support multiple devices and better use a map than a single instance?
                if (this.CommandChannel.TargetName == null)
                {
                    // For now we ignore the reader type because we only support a single type
                    this.CommandChannel.TargetName = "SIEMENS_RFID_READER";
                }

                // Default to local host IP address to avoid problems with multiple network cards
                // and with broken connections.
                IPAddress address = IPAddress.Parse("127.0.0.1");

                // Get IP address from init data if given
                if (null != initData.Address && initData.Address.Length > 0)
                {
                    address = IPAddress.Parse(initData.Address);
                }

                // Remember ip address and port but do not connect here.
                this.CommandChannel.IPAddress = address;
                this.CommandChannel.Port = initData.Port;

                // If we are not yet connected...
                if (!this.CommandChannel.IsConnected())
                {
                    // ... try to connect using the command channel's internal settings
                    if (this.CommandChannel.connect(initData.AckData))
                    {
                        if (RfReaderApi.showDebugInfo)
                        {
                            // Initialize the command channel to dispatch commands
                            ProvideInformation(this, InformationType.Debug,
                                string.Format("Set address: {0}, port: {1}", address.ToString(), initData.Port));
                        }
                    }
                    else
                    {
                        throw new RfReaderApiException(RfReaderApiException.ResultCode_System, RfReaderApiException.Error_NoConnection);
                    }
                }
            }
            catch (RfReaderApiException ex)
            {
                throw ex;
            }
            catch (System.Exception ex)
            {
                throw new RfReaderApiException(RfReaderApiException.Error_Internal, ex);
            }


            if (RfReaderApi.showPerformanceInfo)
            {
                ProvideInformation(this, "Start reader took " + perfTimer1.End().ToString("##.###") + "s");
            }
		}

		/// <summary>
		/// This is an internally used method to stop the communication
		///  and close the connection to a reader 
		/// </summary>
        public void StopTcpIpConnection()
		{
            PerfTiming perfTimer1 = new PerfTiming();
            perfTimer1.Start();

            try
			{
                // Do we have a reader at all?
			    if (this.CommandChannel.IsConnected())
			    {
					if (null != this.CommandChannel.TargetName)
					{
						// First make sure that there are no longer subscriptions pending.
						// We have to skip checking for the invalid API mode and errors here
                        //try 
                        //{
                        //    this.Unsubscribe(RfReaderApi.ALL);
                        //} 
                        //catch (System.Exception) {};

                        try
                        {
                            // Before we end the communication inform the reader about it
                            this.CommandChannel.goodbye();
                        }
                        catch (System.Exception ) { };

						this.CommandChannel.TargetName = null;
					}
				}
                // Now close the existing connection
				this.CommandChannel.closeConnection();
            }
			catch (System.Exception ex)
			{
				throw new RfReaderApiException(RfReaderApiException.Error_Internal, ex);
			}
            RfReaderApi.readerInitData = null;

            if (RfReaderApi.showPerformanceInfo)
            {
                ProvideInformation(this, "End reader took " + perfTimer1.End().ToString("##.###") + "s");
            }
		}

        /// <summary>
        /// Should be first command to the reader
        /// Checks reader type and version of communication protocol
        /// </summary>
        /// <param name="readerType">requested type of reader</param>
        /// <param name="version">Requested versions of XML API </param>
        /// <param name="readerMode">
        /// behaviour of reader:
        /// DEFAULT: reader will work with settings defined in configuration 
        /// STOPP:   reader stopps reading. 
        ///          Antennas are powered down, no tags were read. 
        ///          All tagprocessing commands will return with error. No events are send.
        /// NOREPORT:reader works with settings defined in configuration.
        ///          No reports (Alarm, TagEventReports...) will  be send.
        /// </param>
        /// <returns>The used version of the XML-API</returns>
        public RfConfigID HostGreetings(string readerType, ref string[] version, string readerMode)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> hostGreetingParams = new List<ParameterDesc>();
            hostGreetingParams.Add(new ParameterDesc("readerType", readerType));
            if ("" != readerMode)
            {
                hostGreetingParams.Add(new ParameterDesc("readerMode", readerMode));
            }
            
            hostGreetingParams.Add(new ParameterDesc("supportedVersions", true,true));
            for (int i = 0; i < version.Length; i++)
            {
               hostGreetingParams.Add(new ParameterDesc("version", version[i])); 
            }

           // hostGreetingParams.Add(new ParameterDesc("version", version2,false,true));

            CommandReply rpReply = executeCommand("hostGreetings", hostGreetingParams);

            string replyVersion = "";
            string replyCompatible = "";
            string replyConfigVersion = "";
            bool noVersion = true;
            bool noCompatible = true;
            bool noConfigVersion = true;

            foreach (ParameterDesc param in rpReply.Parameters)
            {
                if (param.key == "version")
                {
                    replyVersion = (string)param.value;
                    noVersion = false;
                }
                if (param.key == "configType")
                {
                    replyCompatible = (string)param.value;
                    noCompatible = false;
                }
                if (param.key == "configID")
                {
                    replyConfigVersion = (string)param.value;
                    noConfigVersion = false;
                }
            }
            if (noVersion || noCompatible || noConfigVersion)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                    RfReaderApiException.Error_MissingParameter,
                    "Command hostGreetings returnd without parameter: version or configID or configType");
            }
            version = new string [1];
            version[0] = replyVersion;
            RfConfigID rfConfigID = new RfConfigID();
            rfConfigID.ConfigType = replyCompatible;
            rfConfigID.ConfigID = replyConfigVersion;
            return rfConfigID;
        }

        /// <summary>
        /// Should be the last command to the reader
        /// <param name="readerMode">
        /// behaviour of reader:
        /// DEFAULT: reader will work with settings defined in configuration 
        /// STOPP:   reader stopps reading. 
        ///          Antennas are powered down, no tags were read. 
        ///          All tagprocessing commands will return with error. No events are send.
        /// NOREPORT:reader works with settings defined in configuration.
        ///          No reports (Alarm, TagEventReports...) will  be send.
        /// </param>

        /// </summary>
        public  void HostGoodbye(string readerMode)
        {
            // Validate current state of Reader API
            validateAPI();

            if ("" != readerMode)
            {
                List<ParameterDesc> hostGoodByeParams = new List<ParameterDesc>();
                hostGoodByeParams.Add(new ParameterDesc("readerMode", readerMode));
                executeCommand("hostGoodbye", hostGoodByeParams);
            }
            else
            {
                executeCommand("hostGoodbye", null);
            }
        }

        /// <summary>
        /// Reader leave stop mode.
        /// </summary>
        public void StartReader()
        {
            // Validate current state of Reader API
            validateAPI();

            executeCommand("startReader", null);
        }

        /// <summary>
        /// Reader enter stop mode.
        ///          - Reader stops reading. 
        ///          - Antennas are powered down, no tags were read. 
        ///          - All tagprocessing commands will return with error. No events are send.
        /// </summary>
        public void StopReader()
        {
            // Validate current state of Reader API
            validateAPI();

            executeCommand("stopReader", null);
        }
        
	    /// <summary>
        /// Checks if reader is present
        /// </summary>
        public void HeartBeat()
        {
            // Validate current state of Reader API
            validateAPI();
            
            executeCommand("heartBeat",null);
        }

        /// <summary>
        /// Set IP address configuration of reader
        /// </summary>
        /// <param name="iPAddress">IP Address of reader</param>
        /// <param name="subNetMask">Subnet Mask of reader</param>
        /// <param name="gateway">IP Address of gateway</param>
        /// <param name="dHCPEnable">If True, use DHCP for IP Address</param>
        public void SetIPConfig(IPAddress iPAddress, IPAddress subNetMask, IPAddress gateway, bool dHCPEnable)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> setIPConfigParams = new List<ParameterDesc>();
            setIPConfigParams.Add(new ParameterDesc("iPAddress", iPAddress.ToString()));
            setIPConfigParams.Add(new ParameterDesc("subNetMask", subNetMask.ToString()));
            setIPConfigParams.Add(new ParameterDesc("dHCPEnable", dHCPEnable.ToString()));
            setIPConfigParams.Add(new ParameterDesc("gateway", gateway.ToString()));

            executeCommand("setIPConfig", setIPConfigParams);
        }
         /// <summary>
        /// Transfer the given configuration file to the reader
        /// </summary>
        /// <param name="fileName">name of the configuration file to transfer</param>
        /// <returns>The configuration identifier</returns>
        public RfConfigID SetConfiguraton(string fileName)
        {
            // Validate current state of Reader API
            validateAPI();

            StringBuilder configData = new StringBuilder(200000);
            configData.Append ("<![CDATA[");

            try 
	        {	        
        		configData.Append (File.ReadAllText(fileName,Encoding.UTF8).ToString());
	        }
	        catch (Exception)
	        {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_System, RfReaderApiException.Error_InvalidParameter + "file name: " +fileName);
	        }         
            configData.Append ("]]>");
            
            List<ParameterDesc> setConfigParams = new List<ParameterDesc>();
			setConfigParams.Add(new ParameterDesc("configData", configData.ToString()));

            CommandReply  rpReply = executeCommand("setConfiguration", setConfigParams,CommandLongTimeout);

            string configID = "";
            bool noConfigID = true;
            foreach (ParameterDesc param in rpReply.Parameters)
            {
                if (param.key == "configID")
                {
                    configID = (string)param.value;
                    noConfigID = false;
                }
            }
           
            if (noConfigID)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                    RfReaderApiException.Error_MissingParameter,
                    "Command SetConfiguration returnd without parameter configID");
            }
            RfConfigID configurationID = new RfConfigID();
            configurationID.ConfigID = configID;
            return configurationID;
        }

        /// <summary>
        /// Get the configuration from the reader
        /// </summary>
        /// <param name="fileName">name of file where the configuration data should be stored</param>
        /// <returns>The configuration identifier</returns>
        public RfConfigID GetConfiguraton(string fileName)
        {
            // Validate current state of Reader API
            validateAPI();

            CommandReply rpReply = executeCommand("getConfiguration", null);
   
            // Get returned config Data 
            string configID = "";
            bool noConfigID = true;
            bool noConfigData = true;

            foreach (ParameterDesc param in rpReply.Parameters)
            {
                if (param.key == "configData")
                {
                    try
                    {
                        File.WriteAllText(fileName, (string)param.value,Encoding.UTF8);
                        noConfigData = false;
                    }
                    catch (Exception)
                    {
                        throw new RfReaderApiException(RfReaderApiException.ResultCode_System, RfReaderApiException.Error_InvalidParameter + "file name: " + fileName);
                    }
                }
                else if (param.key == "configID")
                {
                    configID = (string)param.value;
                    noConfigID = false;
                }
            }
            if (noConfigID || noConfigData)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                    RfReaderApiException.Error_MissingParameter,
                    "Command GetConfiguration returnd without parameter configID, or configData");
            }
           
            RfConfigID configurationID = new RfConfigID();
            configurationID.ConfigID = configID;
            return configurationID;
        }

        /// <summary>
        /// Save the active configuration permanent into the reader
        /// </summary>
        /// <returns>The configuration identifier</returns>
        public RfConfigID SaveConfiguraton()
        {
            // Validate current state of Reader API
            validateAPI();

            CommandReply rpReply = executeCommand("saveConfiguration", null);
   
            // Get returned config Data 
            string configID = "";
            bool noConfigID = true;

            foreach (ParameterDesc param in rpReply.Parameters)
            {
                if (param.key == "configID")
                {
                    configID = (string)param.value;
                    noConfigID = false;
                }
            }
            if (noConfigID)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                    RfReaderApiException.Error_MissingParameter,
                    "Command SaveConfiguration returnd without parameter configID");
            }

            RfConfigID configurationID = new RfConfigID();
            configurationID.ConfigID = configID;
            return configurationID;
        }
 
        /// <summary>
        /// Get the ID of the saved configuration of the reader 
        /// </summary>
        /// <returns>The configuration identifier </returns>
        public RfConfigID GetConfigVersion()
        {
            // Validate current state of Reader API
            validateAPI();

            CommandReply rpReply = executeCommand("getConfigVersion", null);
                    // Get returned config Data 
            string configID = "";
            string replyCompatible = "";
            bool noConfigID = true;
            bool noCompatible = true;

            foreach (ParameterDesc param in rpReply.Parameters)
            {
                if (param.key == "configID")
                {
                    configID = (string)param.value;
                    noConfigID = false;
                }
                if (param.key == "configType")
                {
                    replyCompatible = (string)param.value;
                    noCompatible = false;
                }
            }
            if (noConfigID || noCompatible)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                    RfReaderApiException.Error_MissingParameter,
                    "Command GetConfigVersion returnd without parameter configID or configType");
            }

            RfConfigID configurationID = new RfConfigID();
            configurationID.ConfigID = configID;
            configurationID.ConfigType = replyCompatible;
            return configurationID;
        }

        /// <summary>
        /// Set the clock of the reader 
        /// </summary>
        /// <param name="utcTime">Time stamp in UTC</param>
        public void SetTime(DateTime utcTime)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> setTimeParams = new List<ParameterDesc>();
			setTimeParams.Add(new ParameterDesc("utcTime", utcTime.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")));

            CommandReply rpReply = executeCommand("setTime", setTimeParams);
        }

        /// <summary>
        /// Get the time of the reader 
        /// </summary>
        /// <returns>Time stamp in UTC</returns>
        public DateTime GetTime()
        {
            // Validate current state of Reader API
            validateAPI();

            CommandReply rpReply = executeCommand("getTime", null);
            // Get returned config Data 
            string utcTime = "";
            bool noUtcTime = true;

            foreach (ParameterDesc param in rpReply.Parameters)
            {
                if (param.key == "utcTime")
                {
                    utcTime = (string)param.value;
                    noUtcTime = false;
                }
            }
            if (noUtcTime)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                    RfReaderApiException.Error_MissingParameter,
                    "Command GetTime returnd without parameter utcTime");
            }
            try
            {
                return DateTime.ParseExact(utcTime, "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz",null);
            }
            catch (Exception)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_System,
                    RfReaderApiException.Error_InvalidReply,
                    string.Format("Command GetTime returnd with undecodable parameter utcTime: {0}",utcTime));
            }

        }

        /// <summary>
        /// Set the IO of the reader 
        /// </summary>
        /// <param name="outPortValue">
        /// Bitfield mask indicating wich OUT port is set. Bit 0 = Out port 0
        /// 0 = corrosponding OUT Port is set to LOW level
        /// 1 = corrosponding OUT Port is set to HIGH level
        /// X = corrosponding OUT Port will not be changed
        /// </param>
        public void SetIO(string outPortValue)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> setIoPortParams = new List<ParameterDesc>();
            setIoPortParams.Add(new ParameterDesc("outValue", outPortValue));
            
            CommandReply rpReply = executeCommand("setIO", setIoPortParams);
        }

        /// <summary>
        /// get the IO values of the reader 
        /// </summary>
        /// <returns>All states of all IOs</returns>
        public RfIoPort GetIO()
        {
            // Validate current state of Reader API
            validateAPI();

            CommandReply rpReply = executeCommand("getIO", null);

            string inPortValue = string.Empty;
            string outPortValue = string.Empty;

            foreach (ParameterDesc param in rpReply.Parameters)
            {
                if (param.key == "inValue")
                {
                    inPortValue = param.value.ToString();
                }
                if (param.key == "outValue")
                {
                    outPortValue = param.value.ToString();
                }
            }
           RfIoPort ioPort = new RfIoPort(inPortValue.Length,outPortValue.Length);

            try
            {
                if (0 < inPortValue.Length)
                    ioPort.InPortValue = Convert.ToUInt16(inPortValue, 2);
                if (0 < outPortValue.Length)
                    ioPort.OutPortValue = Convert.ToUInt16(outPortValue, 2);
            }
            catch (Exception)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                    RfReaderApiException.Error_InvalidParameter,
                    "Command GetIO returnd with invalid value for parameter inValue");
            }


            return ioPort;
        }

        /// <summary>
        /// reset the reader to factory defaults
        /// </summary>
        /// <param name="type">
        /// type of reset: 
        ///     FACTORY = factory default
        ///     SW = software reset
        ///     HW = hardware reset
        /// </param>
        public void ResetReader(string type)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> resetParams = new List<ParameterDesc>();
            resetParams.Add(new ParameterDesc("resetType", type));

            CommandReply rpReply = executeCommand("resetReader", resetParams, CommandLongTimeout);
        }

		/// <summary>
		/// Return a map of key/value pairs that specify the current status of our
		/// associated reader.
		/// </summary>
        public Hashtable GetReaderStatus()
		{
		    // Validate current state of Reader API
            validateAPI();

			CommandReply rpReply = executeCommand("getReaderStatus", null);

			Hashtable readerStatusTable = new Hashtable();

            int incr = 0;
			// Get returned parameter
			foreach (ParameterDesc param in rpReply.Parameters)
			{
                if ("version" == param.key)
                {
                    string key = "subVersion_" + incr.ToString();
                    readerStatusTable[key] = param.value;
                    incr++;
                }
                else
                {
                    readerStatusTable[param.key] = param.value;
                }
			}
	
			return readerStatusTable;
		}

        /// <summary>
        /// Returns all source names of the reader
        /// </summary>
        /// <return>A vector containing all source names</return>
        public string[] GetAllSources()
        {
            // Validate current state of Reader API
            validateAPI();

			CommandReply rpReply = executeCommand("getAllSources", null);

			string[] sources = new string[rpReply.Parameters.Count];
            int incr = 0;

			// Get returned parameter
			foreach (ParameterDesc param in rpReply.Parameters)
			{
                if ("sourceName" == param.key)
                {
                    sources[incr] = param.value.ToString();
                    incr++;
                }
			}

            if (incr != rpReply.Parameters.Count)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                   RfReaderApiException.Error_MissingParameter,
                   "Got more parameter than source available");
            }
            return sources;
        }

        /// <summary>
        /// Sends an binary command to the reader, and gets a binary reply from the reader
        /// </summary>
        /// <param name="inputByteCommand">binary command</param>
        /// <return>binary data from reader</return>
        public byte[] SendCommand(byte[] inputByteCommand)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> sendCommandParams = new List<ParameterDesc>();
            // data in HexString
            string data = GetHexEncodedString(inputByteCommand, (uint)inputByteCommand.Length);

            sendCommandParams.Add(new ParameterDesc("byteCommand", data));

            CommandReply rpReply = executeCommand("sendCommand", sendCommandParams);
            bool noData = true;
            byte[] buffer = null;

            foreach (ParameterDesc param in rpReply.Parameters)
            {
                if (param.key == "byteReply")
                {
                    // Extract returned tag memory
                    try
                    {
                        buffer = GetBufferFromHexEncodedString((string)param.value);
                    }
                    catch (System.Exception ex)
                    {
                        throw new RfReaderApiException(RfReaderApiException.Error_InvalidReply, ex);
                    }
                    noData = false;
                }
            }
            if (noData)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                    RfReaderApiException.Error_MissingParameter,
                    "Command readTagData returnd without parameter data");
            }
            return buffer;
        }

        /// <summary>
        /// Set the air protocol configuration from the reader
        /// </summary>
        /// <param name="initialQ">initialQ value. Valid values: 0..15</param>
        /// <param name="commProfile">id of communication profile. Valid values: 0..11</param>
        /// <param name="channelList">
        /// List of used channel number, separated with colon. E.g. "103.109,114,115"
        /// </param>
        /// <param name="retry">number of command retries on air protocol. Valid values 0..255</param>
        /// <param name="tagIdLength">
        /// Fixed length of TagID. Valid values 0,16,32,48...496. 0 = no fix TagId length
        /// </param>
        /// <param name="writeBoost">
        /// Antenna power boost on air protocol write commands. Valid values 0...12
        /// </param>
        public void SetProtocolConfig(  uint initialQ,
                                        uint commProfile,
                                        string channelList,
                                        uint retry,
                                        uint tagIdLength,
                                        uint writeBoost)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> setProtocolConfigParams = new List<ParameterDesc>();
            setProtocolConfigParams.Add(new ParameterDesc("initialQ", initialQ.ToString()));
            setProtocolConfigParams.Add(new ParameterDesc("profile", commProfile.ToString()));
            setProtocolConfigParams.Add(new ParameterDesc("channels", channelList));
            setProtocolConfigParams.Add(new ParameterDesc("retry", retry.ToString()));
            setProtocolConfigParams.Add(new ParameterDesc("idLength", tagIdLength.ToString()));
            setProtocolConfigParams.Add(new ParameterDesc("writeBoost", writeBoost.ToString()));

            CommandReply rpReply = executeCommand("setProtocolConfig", setProtocolConfigParams);
        }

        /// <summary>
        /// Get the air protocol configuration from the reader
        /// </summary>
        /// <returns>initialQ value</returns>
        public Hashtable GetProtocolConfig()
        {
            // Validate current state of Reader API
            validateAPI();

            CommandReply rpReply = executeCommand("getProtocolConfig", null);

            Hashtable ProtocolConfigTable = new Hashtable();

            // Get returned parameter
            foreach (ParameterDesc param in rpReply.Parameters)
            {
                ProtocolConfigTable[param.key] = param.value;
            }

            if (6 != ProtocolConfigTable.Count)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                    RfReaderApiException.Error_MissingParameter,
                    "Command getProtocolConfig returnd with wrong number of parameters");
            }
            return ProtocolConfigTable;
        }

        /// <summary>
        /// Set the configuration of the reader antennas
        /// </summary>
        /// <param name="readerAntennas">
        /// The configuration structure with all parameters of all antennas
        /// </param>
        /// <param name="antennaMask">
        /// Bitmask defining which antenna will be set
        /// Bit 0 = Antenna01
        /// Bit 1 = Antenna02 ...
        /// </param>
        public void SetAntennaConfig(RfAntennas readerAntennas, UInt16 antennaMask)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> setAntennaConfigParams = new List<ParameterDesc>();
            UInt16 tempMask = 1;
            // antenna 01
            if (0 < (antennaMask & tempMask))
            {
                setAntennaConfigParams.Add(new ParameterDesc("antenna", true, true));
                setAntennaConfigParams.Add(new ParameterDesc("antennaName", readerAntennas.RfAntenna1.name));
                setAntennaConfigParams.Add(new ParameterDesc("power", readerAntennas.RfAntenna1.Power.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("cableLoss", readerAntennas.RfAntenna1.CableLoss.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("gain", readerAntennas.RfAntenna1.Gain.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("rSSIThreshold", readerAntennas.RfAntenna1.RSSIThreshold.ToString(), false, true));
            }
            // antenna 02
            tempMask = 2;
            if (0 < (antennaMask & tempMask))
            {
                setAntennaConfigParams.Add(new ParameterDesc("antenna", true, true));
                setAntennaConfigParams.Add(new ParameterDesc("antennaName", readerAntennas.RfAntenna2.name));
                setAntennaConfigParams.Add(new ParameterDesc("power", readerAntennas.RfAntenna2.Power.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("cableLoss", readerAntennas.RfAntenna2.CableLoss.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("gain", readerAntennas.RfAntenna2.Gain.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("rSSIThreshold", readerAntennas.RfAntenna2.RSSIThreshold.ToString(), false, true));
            }
            // antenna 03
            tempMask = 4;
            if (0 < (antennaMask & tempMask))
            {
                setAntennaConfigParams.Add(new ParameterDesc("antenna", true, true));
                setAntennaConfigParams.Add(new ParameterDesc("antennaName", readerAntennas.RfAntenna3.name));
                setAntennaConfigParams.Add(new ParameterDesc("power", readerAntennas.RfAntenna3.Power.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("cableLoss", readerAntennas.RfAntenna3.CableLoss.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("gain", readerAntennas.RfAntenna3.Gain.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("rSSIThreshold", readerAntennas.RfAntenna3.RSSIThreshold.ToString(), false, true));
            }
            // antenna 04
            tempMask = 8;
            if (0 < (antennaMask & tempMask))
            {
                setAntennaConfigParams.Add(new ParameterDesc("antenna", true, true));
                setAntennaConfigParams.Add(new ParameterDesc("antennaName", readerAntennas.RfAntenna4.name));
                setAntennaConfigParams.Add(new ParameterDesc("power", readerAntennas.RfAntenna4.Power.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("cableLoss", readerAntennas.RfAntenna4.CableLoss.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("gain", readerAntennas.RfAntenna4.Gain.ToString()));
                setAntennaConfigParams.Add(new ParameterDesc("rSSIThreshold", readerAntennas.RfAntenna4.RSSIThreshold.ToString(), false, true));
            }

            CommandReply rpReply = executeCommand("setAntennaConfig", setAntennaConfigParams);
        }

        /// <summary>
        /// Get the configuration of the reader antennas
        /// </summary>
        /// <returns>The configuration structure with all parameters of all antennas</returns>
        public RfAntennas GetAntennaConfig()
        {
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            // Validate current state of Reader API
            validateAPI();

            CommandReply rpReply = executeCommand("getAntennaConfig", null);

            RfAntennas readerAntennas = new RfAntennas();

            bool noGain = true;
            bool noPower = true;
            bool noCableLoss = true;
            bool noRssi = true;
            int antennaIndex = -1;

            foreach (ParameterDesc param in rpReply.Parameters)
            {
                if (param.isContainer)
                {
                    if ((noCableLoss || noPower || noGain || noRssi) && 0 <= antennaIndex)
                    {
                        throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                            RfReaderApiException.Error_MissingParameter,
                            "Command getAntennaConfig returnd without parameter noCableLoss or noPower or noGain");
                    }
                    antennaIndex++;
                    noGain = true;
                    noPower = true;
                    noCableLoss = true;
                    noRssi = true;
                }
                // check if we got parameter without start XML <antenna>
                // or if we got more than specified antenna blocks
                if (antennaIndex == readerAntennas.RfAntennaArray.Length || 0 > antennaIndex )
                {
                    throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                     RfReaderApiException.Error_InvalidParameter,
                         "Command getAntennaConfig returned with invalid antenna");
                }
                if (param.key == "antennaName")
                {
                    if (readerAntennas.RfAntennaArray[antennaIndex].Name != param.value.ToString())
                    {
                        throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                            RfReaderApiException.Error_InvalidParameter,
                                "Command getAntennaConfig returned with invalid Antenna name ");
                    }
                }
                if (param.key == "gain")
                {
                    try
                    {
                        readerAntennas.RfAntennaArray[antennaIndex].Gain = float.Parse((string)param.value, nfi);
                        noGain = false;
                    }
                    catch (Exception)
                    {
                         throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                          RfReaderApiException.Error_InvalidParameter,
                          "Command getAntennaConfig returned with invalid value for parameter gain");
                    }

                }
                if (param.key == "power")
                {
                    try
                    {
                        readerAntennas.RfAntennaArray[antennaIndex].Power = Convert.ToUInt16(param.value);
                    }
                    catch (Exception)
                    {
                        readerAntennas.RfAntennaArray[antennaIndex].Gain = 0;
                        ProvideInformation(this, "power not readable, because of hex value -> we display a zero");

                    }
                    noPower = false;
                }
                if (param.key == "cableLoss")
                {
                    try
                    {
                        readerAntennas.RfAntennaArray[antennaIndex].CableLoss = float.Parse((string)param.value,nfi);
                        noCableLoss = false;
                    }
                    catch (Exception)
                    {
                        throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                         RfReaderApiException.Error_InvalidParameter,
                         "Command getAntennaConfig returned with invalid value for parameter cableLoss");
                    }

                }
                if (param.key == "rSSIThreshold")
                {
                    try
                    {
                        readerAntennas.RfAntennaArray[antennaIndex].RSSIThreshold = Convert.ToUInt16(param.value);
                        noRssi = false;
                    }
                    catch (Exception)
                    {
                        throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                         RfReaderApiException.Error_InvalidParameter,
                         "Command getAntennaConfig returned with invalid value for parameter rSSIThreshold");
                    }

                }
            }
            if (noCableLoss || noPower || noGain || noRssi)
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_Reader,
                    RfReaderApiException.Error_MissingParameter,
                    "Command getAntennaConfig returnd without parameter gain, power, cableLoss, or rSSIThreshold");
            }
            return readerAntennas;
        }

        /// <summary>
        /// Trigger the specified source for one reading cycle
        /// Recognize tags will be deliverd via Tag event report (TER)
        /// </summary>
        /// <param name="sourceName">Specifiy the source to be triggered</param>
        public void TriggerSource(string sourceName)
        {
            TriggerSource(sourceName, "");
        }

        /// <summary>
        /// Trigger the specified source for one reading cycle
        /// Recognize tags will be deliverd via Tag event report (TER)
        /// </summary>
        /// <param name="sourceName">Specifiy the source to be triggered</param>
        /// <param name="mode">Trigger mode: Single, Start, Stop</param>
        public void TriggerSource(string sourceName, string mode)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> triggerSourceParams = new List<ParameterDesc>();
            triggerSourceParams.Add(new ParameterDesc("sourceName", sourceName));
            if ("" != mode)
            {
                triggerSourceParams.Add(new ParameterDesc("triggerMode", mode));
            }
            CommandReply rpReply = executeCommand("triggerSource", triggerSourceParams);
        }

        /// <summary>
        /// Perform inventory, that is get the IDs of all tags
        /// currently visible at the requested source for our reader
        /// </summary>
        /// <param name="sourceName">Specifiy the source read</param>
        /// <param name="duration">time duration for reading tags</param>
        /// <returns>Returns an array of all tags read.
        /// In case no tags are within the field null is returned.
        /// </returns>
        public RfTag[] ReadTagIDs(string sourceName, uint duration)
		{
            // Validate current state of Reader API
            validateAPI();
            double commandTimeout = CommandTimeout;
            List<ParameterDesc> getTagIDConfigParams = new List<ParameterDesc>();
            if ("" != sourceName)
            {
                getTagIDConfigParams.Add(new ParameterDesc("sourceName", sourceName));
            }
            if (0 != duration)
            {
                getTagIDConfigParams.Add(new ParameterDesc("duration", duration.ToString()));
                commandTimeout += duration / 1000;
            }

            CommandReply rpReply = executeCommand("readTagIDs", getTagIDConfigParams,commandTimeout);

            return rpReply.TagData;
		}

		/// <summary>
		/// Set a new tagID for a tag within the field.
		/// </summary>
        /// <param name="source">
        /// Reference to the source where tag is to be changed
        /// If empty, first source is used
        /// </param>
		/// <param name="currentTagID">
        /// The current ID of the tag to be changed
        /// If empty, first tag in fild is used<
        /// /param>
		/// <param name="newTagID">The new ID for the tag</param>
        /// <param name="idLength">Length of the TagID</param>
		/// <param name="password">
        /// An access password if necessary
        /// If empty, open access to tag is used
        /// </param>
        /// <returns>Be aware, if no tag Id could be written, an exception is thrown!
        /// E.g. if no tag was in field!
        /// </returns>
		public void WriteTagID(string source, string currentTagID, string newTagID, uint idLength, string password)
		{
            // Validate current state of Reader API
            validateAPI();

     		List<ParameterDesc> setTagIdParams = new List<ParameterDesc>();
            // source is optional
            if (! ("" == source))
            {
                setTagIdParams.Add(new ParameterDesc("sourceName", source));
            }
            // currentTagID is optional
            if (!("" == currentTagID))
            {
                setTagIdParams.Add(new ParameterDesc("tagID", currentTagID));
            }
			setTagIdParams.Add(new ParameterDesc("newID", newTagID));
            // tidLength is optional
            if (!(0 == idLength))
            {
                setTagIdParams.Add(new ParameterDesc("idLength", idLength));
            }
            // password is optional
            if (!("" == password))
            {
                setTagIdParams.Add(new ParameterDesc("password", password));
            }
            CommandReply rpReply = executeCommand("writeTagID", setTagIdParams);
		}

		/// <summary>
		/// Helper function to transform an arbitrary string into a hex encoded representation
		/// </summary>
		/// <param name="password">The string to be encoded</param>
		/// <returns>A bytewise translation to hex as string</returns>
        //private string GetHexEncodedString(string password)
        //{
        //    StringBuilder encodedPassword = new StringBuilder();
        //    for (int index = 0; index < password.Length; index++)
        //    {
				
        //        encodedPassword.Append(((byte)password[index]).ToString("X2"));
        //    }

        //    return encodedPassword.ToString();
        //}

		private string GetHexEncodedString(byte[] buffer, uint dataLength)
		{
			StringBuilder encodedPassword = new StringBuilder();
			for (uint index = 0; index < dataLength; index++)
			{

				encodedPassword.Append(buffer[index].ToString("X2"));
			}

			return encodedPassword.ToString();
		}

		private byte[] GetBufferFromHexEncodedString(string hexEncodedString)
		{
			byte [] buffer = null;

			if (null != hexEncodedString && hexEncodedString.Length >= 2)
			{
				int bufferLength = hexEncodedString.Length / 2;
				buffer = new byte[bufferLength];
				for (int index = 0; index < bufferLength; index++)
				{
					buffer[index] = byte.Parse(hexEncodedString.Substring(index * 2, 2),
						System.Globalization.NumberStyles.HexNumber);
				}
			}
			return buffer;
		}

        /// <summary>
        /// Read specific tag data
        /// </summary>
        /// <param name="source">Reference to the source where tag is to be changed</param>
        /// <param name="tagID">The ID of the tag whose memory should be read.</param>
        /// <param name="password">An access password if necessary.</param>
        /// <param name="tagFields">Array of requested tagFields. 
        /// One tag field is limited to a lenght of 4096 Bytes</param>
        /// <returns>Returns an array of all read tags.
        /// In case of errors or invalid memory areas null is returned.</returns>
        public RfTag[] ReadTagMemory(string source, string tagID, string password, RfTagField[] tagFields)
		{
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> readTagDataParams = new List<ParameterDesc>();
            // source 
            readTagDataParams.Add(new ParameterDesc("sourceName", source));

            // currentTagID is optional
            if (!("" == tagID))
            {
                readTagDataParams.Add(new ParameterDesc("tagID", tagID));
            }
            // password is optional
            if (!("" == password))
            {
                readTagDataParams.Add(new ParameterDesc("password", password));
            }
            for (int i = 0; i < tagFields.Length; i++)
            {
                readTagDataParams.Add(new ParameterDesc("tagField", true, true));
                 // tag bank is optional
                if (!("" == tagFields[i].TagFieldBank))
                {
                    readTagDataParams.Add(new ParameterDesc("bank", tagFields[i].TagFieldBank));
                }
                // start address in Bytes
                readTagDataParams.Add(new ParameterDesc("startAddress", tagFields[i].TagFieldAddress));
                // dataLength in Bytes
                readTagDataParams.Add(new ParameterDesc("dataLength", tagFields[i].TagFieldLength,false,true));
            }

            CommandReply rpReply = executeCommand("readTagMemory", readTagDataParams);

            return rpReply.TagData;
		}

        /// <summary>
        /// Write specific tag memory
        /// </summary>
        /// <param name="source">Reference to the source where tag is to be changed</param>
        /// <param name="tagID">The ID of the tag whose memory should be read.</param>
        /// <param name="password">An access password if necessary.</param>
        /// <param name="tagFields">Array of requested tagFields. 
        /// One tag field is limited to a lenght of 4096 Bytes</param>
        /// <returns>Returns an array of all read tags.
        /// In case of errors or invalid memory areas null is returned.</returns>
        public RfTag[] WriteTagMemory(string source, string tagID, string password, RfTagField[] tagFields)
		{
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> setTagMemoryParams = new List<ParameterDesc>();
            // source 
            setTagMemoryParams.Add(new ParameterDesc("sourceName", source));
            // currentTagID is optional
            if (!("" == tagID))
            {
                setTagMemoryParams.Add(new ParameterDesc("tagID", tagID));
            }      
            // password is optional
            if (!("" == password))
            {
                setTagMemoryParams.Add(new ParameterDesc("password", password));
            }
            for (int i = 0; i < tagFields.Length; i++)
            {
                setTagMemoryParams.Add(new ParameterDesc("tagField", true, true));
                // tag bank is optional
                if (!("" == tagFields[i].TagFieldBank))
                {
                    setTagMemoryParams.Add(new ParameterDesc("bank", tagFields[i].TagFieldBank));
                }
                // start address in Bytes
                setTagMemoryParams.Add(new ParameterDesc("startAddress", tagFields[i].TagFieldAddress));
                // dataLength in Bytes
                setTagMemoryParams.Add(new ParameterDesc("dataLength", tagFields[i].TagFieldLength));
                // data in HexString
                setTagMemoryParams.Add(new ParameterDesc("data", tagFields[i].TagFieldData, false, true));
            }

            CommandReply rpReply = executeCommand("writeTagMemory", setTagMemoryParams);

            return rpReply.TagData;
        }

        /// <summary>
        /// Read specific tag data
        /// </summary>
        /// <param name="source">Reference to the source where tag is to be changed</param>
        /// <param name="tagID">The ID of the tag whose memory should be read.</param>
        /// <param name="password">An access password if necessary.</param>
        /// <param name="tagFields">Array of requested tagFields. 
        /// One tag field is limited to a lenght of 512 Bytes</param>
        /// <returns>Returns an array of all read tags.
        /// In case of errors or invalid memory areas null is returned.</returns>
        public RfTag[] ReadTagField(string source, string tagID, string password, RfTagField[] tagFields)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> readTagFieldParams = new List<ParameterDesc>();
            // source is optional
            readTagFieldParams.Add(new ParameterDesc("sourceName", source));
            // currentTagID is optional
            if (!("" == tagID))
            {
                readTagFieldParams.Add(new ParameterDesc("tagID", tagID));
            }
            // password is optional
            if (!("" == password))
            {
                readTagFieldParams.Add(new ParameterDesc("password", password));
            }
            for (int i = 0; i < tagFields.Length; i++)
            {
                readTagFieldParams.Add(new ParameterDesc("tagField", true, true));
                // data in HexString
                readTagFieldParams.Add(new ParameterDesc("fieldName", tagFields[i].TagFieldName, false, true));
            }

            CommandReply rpReply = executeCommand("readTagField", readTagFieldParams);

            return rpReply.TagData;
        }

        /// <summary>
        /// Write specific tag memory
        /// </summary>
        /// <param name="source">Reference to the source where tag is to be changed</param>
        /// <param name="tagID">The ID of the tag whose memory should be read.</param>
        /// <param name="password">An access password if necessary.</param>
        /// <param name="tagFields">Array of requested tagFields. 
        /// One tag field is limited to a lenght of 512 Bytes</param>
        /// <returns>Returns an array of all read tags.
        /// In case of errors or invalid memory areas null is returned.</returns>
        public RfTag[] WriteTagField(string source, string tagID, string fieldName, string password, RfTagField[] tagFields)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> writeTagFieldParams = new List<ParameterDesc>();
            // source is optional
            writeTagFieldParams.Add(new ParameterDesc("sourceName", source));
            // currentTagID is optional
            if (!("" == tagID))
            {
                writeTagFieldParams.Add(new ParameterDesc("tagID", tagID));
            }
            // password is optional
            if (!("" == password))
            {
                writeTagFieldParams.Add(new ParameterDesc("password", password));
            }
            for (int i = 0; i < tagFields.Length; i++)
            {
                writeTagFieldParams.Add(new ParameterDesc("tagField", true, true));
                writeTagFieldParams.Add(new ParameterDesc("fieldName", tagFields[i].TagFieldName));
                writeTagFieldParams.Add(new ParameterDesc("data", tagFields[i].TagFieldData, false, true));  
            }
            CommandReply rpReply = executeCommand("writeTagField", writeTagFieldParams);

            return rpReply.TagData;
        }

		/// <summary>
		/// Kill an EPC Class1 Gen2 tag
		/// </summary>
        /// <param name="source">
        /// Reference to the source where tag is to be changed
        /// If empty, first source is used
        /// </param>
		/// <param name="currentTagID">The tag ID for the tag to be killed.</param>
		/// <param name="password">A kill password if necessary</param>
        /// <param name="recomissioningFlags">
        /// Recommissioning flags: bool string with max 3 Bit
        /// Bit 0: disable block permalocking
        /// Bit 1: render USER memory inaccessible
        /// Bit 2: Unlock EPC TID and USER bank
        /// </param>
        /// <returns>Returns an array of data bytes read from the tag.
        /// In case of errors or invalid memory areas null is returned.</returns>
        public RfTag[] KillTag(string source, string currentTagID, string password)
		{
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> killTagParams = new List<ParameterDesc>();
            // source is optional
            if (!("" == source))
            {
                killTagParams.Add(new ParameterDesc("sourceName", source));
            }
            // currentTagID is optional
            if (!("" == currentTagID))
            {
                killTagParams.Add(new ParameterDesc("tagID", currentTagID));
            }
            killTagParams.Add(new ParameterDesc("password", password));
        
            CommandReply rpReply = executeCommand("killTag", killTagParams);

            return rpReply.TagData;
        }

		/// <summary>
		/// Lock a tag bank on EPC Class1 Gen2 tag
		/// </summary>
        /// <param name="source">
        /// Reference to the source where tag is to be changed
        /// If empty, first source is used
        /// </param>
        /// <param name="tagID">The ID for the tag to be locked.</param>
        /// <param name="action">The lock action according to EPC specifications.</param>
        /// <param name="mask">The lock mask according to EPC specifications.</param>
		/// <param name="password">An access password if necessary.</param>
        /// <returns>Returns an array of data bytes read from the tag.
        /// In case of errors or invalid memory areas null is returned.</returns>
        public RfTag[] LockTagBank(string source, string tagID, uint action, uint mask, string password)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> lockTagParams = new List<ParameterDesc>();
            // source is optional
            if (!("" == source))
            {
                lockTagParams.Add(new ParameterDesc("sourceName", source));
            }
            // currentTagID is optional
            if (!("" == tagID))
            {
                lockTagParams.Add(new ParameterDesc("tagID", tagID));
            }
            lockTagParams.Add(new ParameterDesc("lockAction", Convert.ToString(action, 2)));
            lockTagParams.Add(new ParameterDesc("lockMask", Convert.ToString(mask, 2)));
            lockTagParams.Add(new ParameterDesc("password", password));

            CommandReply rpReply = executeCommand("lockTagBank", lockTagParams);

            return rpReply.TagData;
        }
    #region NXP Commands
        /// <summary>
		/// Set read protect for  for a NXP tag within the field.
		/// </summary>
        /// <param name="source">
        /// Reference to the source where tag is to be changed
        /// If empty, first source is used
        /// </param>
		/// <param name="currentTagID">
        /// The current ID of the tag to be changed
        /// If empty, first tag in fild is used<
        /// /param>
		/// <param name="password">
        /// An access password if necessary
        /// If empty, open access to tag is used
        /// </param>
        /// <returns>Be aware, if no tag Id could be protected, an exception is thrown!
        /// E.g. if no tag was in field!
        /// </returns>
        public RfTag[] NXP_SetReadProtect(string source, string TagID, string password)
		{
            // Validate current state of Reader API
            validateAPI();

     		List<ParameterDesc> nXP_SetReadProtectParams = new List<ParameterDesc>();
            // source
            nXP_SetReadProtectParams.Add(new ParameterDesc("sourceName", source));
            // currentTagID is optional
            if (!("" == TagID))
            {
                nXP_SetReadProtectParams.Add(new ParameterDesc("tagID", TagID));
            }
            // password is optional
            if (!("" == password))
            {
                nXP_SetReadProtectParams.Add(new ParameterDesc("password", password));
            }
            CommandReply rpReply = executeCommand("nXP_SetReadProtect", nXP_SetReadProtectParams);

            return rpReply.TagData;
		}

        /// <summary>
        /// Reset read protect for  for a NXP tag within the field.
        /// </summary>
        /// <param name="source">
        /// Reference to the source where tag is to be changed
        /// If empty, first source is used
        /// </param>
        /// <param name="password">
        /// The access password </param>
        /// <returns>Be aware, if no tag Id could be unprotected, an exception is thrown!
        /// E.g. if no tag was in field!
        /// </returns>
        public RfTag[] NXP_ResetReadProtect(string source, string password)
        {
            // Validate current state of Reader API
            validateAPI();

            List<ParameterDesc> nXP_ResetReadProtectParams = new List<ParameterDesc>();
            // source
            nXP_ResetReadProtectParams.Add(new ParameterDesc("sourceName", source));

            // password is optional
            if (!("" == password))
            {
                nXP_ResetReadProtectParams.Add(new ParameterDesc("password", password));
            }
            CommandReply rpReply = executeCommand("nXP_ResetReadProtect", nXP_ResetReadProtectParams);

            return rpReply.TagData;
        }

    
    #endregion NXP Commands

    #endregion

        #region helper functinons

        private void validateAPI()
        {
            // do we have a valid API ?
            if (!RfReaderApi.IsValid)
            {
                throw new RfReaderApiInvalidModeException();
            }
            // Do a security check whether we are connected at all
            if (!this.CommandChannel.IsConnected())
            {
                throw new RfReaderApiException(RfReaderApiException.ResultCode_System, RfReaderApiException.Error_NoConnection);
            }
        }

        private CommandReply executeCommand(string commandName, List<ParameterDesc> configParams)
        {
            return executeCommand(commandName, configParams, RfReaderApi.CommandTimeout);
        }

        private CommandReply executeCommand(string commandName, List<ParameterDesc> configParams, double commandTimeout)
        {
            PerfTiming perfTimer1 = new PerfTiming();
            perfTimer1.Start();

            Command rpCommand = new Command(commandName, configParams);
            string replyMsg = this.CommandChannel.execute(rpCommand.getXmlCommand(), rpCommand.CommandID, commandTimeout);
            CommandReply rpReply = CommandReply.DecodeXmlCommand(replyMsg);
            if (rpReply == null)
            {
                // There is no reply from our connected reader
                throw new RfReaderApiException(RfReaderApiException.ResultCode_System, RfReaderApiException.Error_NoReply);
            }

            if (rpReply.ResultCode != 0)
            {
                // forward errors as exceptions
                throw new RfReaderApiException(rpReply.ResultCode, rpReply.Error,
                    rpReply.Cause);
            }

            if (RfReaderApi.showPerformanceInfo)
            {
                ProvideInformation(this, commandName + " took: " + perfTimer1.End().ToString("##.###") + "s");
            }
            return rpReply;
        }

        /// <summary>
        /// deliever the collected messsages to the differen listeners
        /// </summary>
        /// <param name="debugData">queue with debug messages</param>
        /// <param name="alarmData">queue with received alarm messages</param>
        /// <param name="reportData">queue with received report messages</param>
        /// <returns></returns>
        protected internal void deliverMessages(Queue<string> debugData, Queue<RfAlarm[]> alarmData, Queue<RfReport[]> reportData)
        {
            while (null != debugData && 0 < debugData.Count)
            {
                RfReaderApi.CurrentApi.ProvideInformation("CommandChannel", InformationType.Debug, debugData.Dequeue());
            }
            while (null != alarmData && 0 < alarmData.Count)
            {
                // Notify clients about received tag events
                if (null != RfReaderApi.CurrentApi.Alarms)
                {
                    // Create argumnets and fire notification
                    RfAlarmArgs alarmArgs = new RfAlarmArgs(alarmData.Dequeue());
                    RfReaderApi.CurrentApi.Alarms(this, alarmArgs);
                }
            }
            while (null != reportData && 0 < reportData.Count)
            {
                RfReport[] tagEventList = reportData.Dequeue();
                // Notify clients about received tag events
                if (null != RfReaderApi.CurrentApi.TagEventNotifications && null != tagEventList)
                {
                    // Create argumnets and fire notification
                    RfNotificationArgs notifyArgs = new RfNotificationArgs(tagEventList);
                    this.TagEventNotifications(this, notifyArgs);
                }
            }
        }
    #endregion	helper functinons
   };    
}

