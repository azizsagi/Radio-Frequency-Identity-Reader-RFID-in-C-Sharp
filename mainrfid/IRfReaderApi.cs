using System;
using System.Collections;
using System.Net;



namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// Helper class to contain the initialization data for a reader.
	/// </summary>
	public class RfReaderInitData
	{
		/// <summary> This is the default port for communication channel</summary>
		private const int DefaultPort = 10001;

        ///// <summary>
        ///// The reader's type (RF670R).
        ///// </summary>
        //public string Type
        //{
        //    get { return this.m_readerType; }
        //    set { this.m_readerType = value; }
        //}
        //private string m_readerType = "";

	
		/// <summary>
		/// The IP address for the reader. (For future use!)
		/// </summary>
		/// <remarks>
		/// Currently, this setting is ignored as we are working only on one device
		/// that hosts our reader service. So there is not yet a need to distinguish
		/// which reader service to use. We always use the local reader service
		/// </remarks>
		public string Address
		{
			get { return this.m_readerAddress; }
			set { this.m_readerAddress = value; }
		}
		private string m_readerAddress = "";

		/// <summary>
		/// The socket port used for the command channel
		/// </summary>
		public int Port
		{
			get { return this.m_readerPort; }
			set { this.m_readerPort = value; }
		}
		private int m_readerPort = DefaultPort;

        /// <summary>
        /// The socket port used for the command channel
        /// </summary>
        public bool AckData
        {
            get { return this.ackData; }
            set { this.ackData = value; }
        }
        private bool ackData = false;
	
		/// <summary>
		/// Additional initialization data as name/value pairs.
		/// This data is intended for future use whenever other reader devices
		/// might need additional configuration information.
		/// </summary>
		public Hashtable AdditionalInitData
		{
			get { return this.m_additionalInitData; }
			set { this.m_additionalInitData = value; }
		}
		private Hashtable m_additionalInitData = null;

		/// <summary>
		/// Create RFID reader initialization data
		/// for an RF610M device with a default name
		/// </summary>
		public RfReaderInitData()
		{
 //           this.m_readerType = RfReaderApi.SupportedReaderType1;
		}

	
        ///// <summary>
        ///// Create RFID reader initialization data
        ///// </summary>
        ///// <param name="type">The reader's type (SIMATIC_RF670R).</param>
        //public RfReaderInitData(string type)
        //{
        //    this.m_readerType = type;
        //}


		/// <summary>
		/// Create RFID reader initialization data
		/// </summary>
        /// <param name="type">The reader's type (RF660, RF610M, RRF310M).</param>
		/// <param name="address">The IP address for the reader</param>
		/// <param name="port">The socket used for the command channel </param>
		public RfReaderInitData(string type, string address, int port)
		{
//			this.m_readerType = type;
			this.m_readerAddress = address;
			this.m_readerPort = port;
		}
	}

	/// <summary>
	/// This is the simple RFID reader interface that
	/// allows access to tag data and functions without
	/// having to deal with EPC or implementation details
	/// </summary>
	public interface IRfReaderApi
	{
        ///// <summary>Supported reader types </summary>
        //string [] SupportedReaderTypes { get ; }

 
		/// <summary>
		/// The current version of the API.
		/// This will help you identify which features are available
		/// </summary>
		string Version { get ; }

		/// <summary>
		/// Initiate the communication to an RFID reader,
		/// that is to the reader service application that manages the
		/// physical reader.
		/// </summary>
		/// <param name="initData">Initialization data needed to set up the connection</param>
		void StartTcpIPConnection(RfReaderInitData initData);

		/// <summary>
		/// Stop the communication to the reader service application and terminate 
		/// and unload the service.
		/// </summary>
		void StopTcpIpConnection();

        /// <summary>
        /// Should be first command to reader
        /// Checks reader type and version of communication protocol
        /// Todo; Only for testing purposes, remove when publishing this API
        /// </summary>
        /// <param name="readerType">Type of supported reader</param>
        /// <param name="version1">requested versions of XML API</param>
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
        RfConfigID HostGreetings(string readerType, ref string[] version, string readerMode);

        /// <summary>
        /// Should be the last command to the reader
        /// <param name="readerMode">
        /// behaviour of reader:
        /// Default: reader will work with settings defined in configuration 
        /// Stop:    reader will go into stop mode:
        ///          - Reader stops reading. 
        ///          - Antennas are powered down, no tags were read. 
        ///          - All tagprocessing commands will return with error. No events are send.
        /// Run:     Reader 
        ///          No reports (Alarm, TagEventReports...) will  be send.
        /// </param>
        /// </summary>
        void HostGoodbye(string readerMode);

        /// <summary>
        /// Reader leave stop mode.
        /// </summary>
        void StartReader();

        /// <summary>
        /// Reader enter stop mode.
        ///          - Reader stops reading. 
        ///          - Antennas are powered down, no tags were read. 
        ///          - All tagprocessing commands will return with error. No events are send.
        /// </summary>
        void StopReader();

        /// <summary>
        /// Checks if reader is present
        /// </summary>
        void HeartBeat();

        /// <summary>
        /// Set IP address configuration of reader
        /// </summary>
        /// <param name="iPAddress">IP Address of reader</param>
        /// <param name="subNetMask">Subnet Mask of reader</param>
        /// <param name="gateway">IP Address of gateway</param>
        /// <param name="dHCPEnable">If True, use DHCP for IP Address</param>
         void SetIPConfig(IPAddress iPAddress, IPAddress subNetMask, IPAddress gateway, bool dHCPEnable);

        /// <summary>
        /// Transfer the given configuration file to the reader
        /// </summary>
        /// <param name="fileName">name of the configuration file to transfer</param>
        /// <returns>The configuration identifier of the saved configuration</returns>
        RfConfigID SetConfiguraton(string fileName);

        /// <summary>
        /// Get the configuration from the reader
        /// </summary>
        /// <param name="fileName">name of file where the configuration data should be stored</param>
        /// <returns>The configuration identifier</returns>
        RfConfigID GetConfiguraton(string fileName);

        /// <summary>
        /// Save the active configuration permanent into the reader
        /// </summary>
        /// <returns>The configuration identifier</returns>
        RfConfigID SaveConfiguraton();

        /// <summary>
        /// Get the ID of the saved configuration of the reader 
        /// </summary>
        /// <returns>The configuration identifier</returns>
        RfConfigID GetConfigVersion();
        
        /// <summary>
        /// Set the clock of the reader 
        /// </summary>
        /// <param name="utcTime">Time stamp in UTC</param>
        void SetTime(DateTime utcTime);

        /// <summary>
        /// Get the time of the reader 
        /// </summary>
        /// <returns>Time stamp in UTC</returns>
        DateTime GetTime();

        /// <summary>
        /// Set the out ports of the reader 
        /// </summary>
        /// Bitfield mask indicating wich OUT port is set. Bit 0 = Out port 0
        /// 0 = corrosponding OUT Port is set to LOW level
        /// 1 = corrosponding OUT Port is set to HIGH level
        /// X = corrosponding OUT Port will not be changed
        /// </param>
        void SetIO(string outPortValue);

        /// <summary>
        /// Get the status of the reader ports
        /// </summary>
        /// <returns>state of all inputs and outputs of the reader</returns>
        RfIoPort GetIO();

        /// <summary>
        /// reset the reader to factory defaults
        /// </summary>
        /// <param name="type">
        /// type of reset: 
        ///     FACTORY = factory default
        ///     SW = software reset
        ///     HW = hardware reset
        /// </param>
        void ResetReader(string type);

        /// <summary>
		/// The current status of the reader.
		/// </summary>
		/// <return>A hashtable of all status values provided by the reader as name/value pairs.</return>
        Hashtable GetReaderStatus();

         /// <summary>
        /// Returns all source names of the reader
        /// </summary>
        /// <return>A vector containing all source names</return>
        string[] GetAllSources();
    
        /// <summary>
        /// Sends an binary command to the reader, and gets a binary reply from the reader
        /// </summary>
        /// <param name="inputByteCommand">binary command</param>
        /// <return>binary data from reader</return>
        byte[] SendCommand(byte[] inputByteCommand);

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
        void SetProtocolConfig( uint initialQ,
                                uint commProfile, 
                                string channelList, 
                                uint retry,
                                uint tagIdLength, 
                                uint writeBoost);

        /// <summary>
        /// Get the air protocol configuration from the reader
        /// </summary>
        /// <returns>A hashtable of all protocol config values provided by the reader as name/value pairs.</returns>
        Hashtable GetProtocolConfig();

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
        void SetAntennaConfig(RfAntennas readerAntennas, UInt16 antennaMask);

        /// <summary>
        /// Get the configuration of the reader antennas
        /// </summary>
        /// <returns>The configuration structure with all parameters of all antennas</returns>
        RfAntennas GetAntennaConfig();

        /// <summary>
        /// Trigger the specified source for one reading cycle
        /// Recognize tags will be deliverd via Tag event report (TER)
        /// </summary>
        /// <param name="sourceName">Specifiy the source to be triggered</param>
        /// <param name="mode">Trigger mode: Single, Start, Stop</param>

        void TriggerSource(string sourceName, string mode);

        /// <summary>
        /// Trigger the specified source for one reading cycle
        /// Recognize tags will be deliverd via Tag event report (TER)
        /// </summary>
        /// <param name="sourceName">Specifiy the source to be triggered</param>
        void TriggerSource(string sourceName);
        
		/// <summary>
		/// Perform inventory, that is get the IDs of all tags
		/// currently visible at the requested source for our reader
		/// </summary>
        /// <param name="sourceName">Specifiy the source read</param>
        /// <param name="duration">time duration for reading tags</param>
		/// <returns>Returns an array of all tags read.
		/// In case no tags are within the field null is returned.
		/// </returns>
        RfTag[] ReadTagIDs(string sourceName, uint duration);

		/// <summary>
		/// Set a new tagID for a tag within the field.
		/// </summary>
        /// <param name="source">Reference to the source where tag is to be changed</param>
		/// <param name="currentTagID">The current ID of the tag to be changed</param>
		/// <param name="newTagID">The new ID for the tag</param>
        /// <param name="idLength">Length of the TagID</param>
		/// <param name="password">An access password if necessary.</param>
		void WriteTagID(string source, string currentTagID, string newTagID, uint idLength, string password);

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
        RfTag [] ReadTagMemory(string source, string tagID, string password, RfTagField[] tagFields);
		
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
        RfTag[] WriteTagMemory(string source, string tagID, string password, RfTagField[] tagFields);

        /// <summary>
        /// Read specific tag data. Referenced through tagfield name.
        /// </summary>
        /// <param name="source">Reference to the source where tag is to be changed</param>
        /// <param name="tagID">The ID of the tag whose memory should be read.</param>
        /// <param name="password">An access password if necessary.</param>
        /// <param name="tagFields">Array of requested tagFields. 
        /// One tag field is limited to a lenght of 512 Bytes</param>
        /// <returns>Returns an array of all read tags.
        /// In case of errors or invalid memory areas null is returned.</returns>
        RfTag[] ReadTagField(string source, string tagID, string password, RfTagField[] tagFields);

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
        RfTag[] WriteTagField(string source, string tagID, string fieldName, string password, RfTagField[] tagFields);

		/// <summary>
		/// Kill an EPC Class1 Gen2 tag
		/// </summary>
        /// <param name="source">Reference to the source where tag is to be changed</param>
		/// <param name="tagID">The tag ID for the tag to be killed.</param>
		/// <param name="password">A kill password if necessary</param>
        /// <param name="recomissioningFlags">
        /// Recommissioning flags
        /// Bit 0: disable block permalocking
        /// Bit 1: render USER memory inaccessible
        /// Bit 2: Unlock EPC TID and USER bank
        /// </param>
        /// <returns>Returns an array of all tags read.
        /// In case no tags are within the field null is returned.</returns>
        RfTag[] KillTag(string source, string TagID, string password);

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
        /// <returns>Returns an array of all tags read.
        /// In case no tags are within the field null is returned.</returns>
        RfTag[] LockTagBank(string source, string tagID, uint action, uint mask, string password);
	
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
        /// <returns>Be aware, if no tag Id could be written, an exception is thrown!
        /// E.g. if no tag was in field!
        /// </returns>
        RfTag[] NXP_SetReadProtect(string source, string TagID, string password);

        /// <summary>
        /// Reset read protect for  for a NXP tag within the field.
        /// </summary>
        /// <param name="source">
        /// Reference to the source where tag is to be changed
        /// If empty, first source is used
        /// </param> 
        /// <param name="password">
        /// The access password 
        /// </param>
        /// <returns>Be aware, if no tag Id could be written, an exception is thrown!
        /// E.g. if no tag was in field!
        /// </returns>
        RfTag[] NXP_ResetReadProtect(string source, string password);

		/// <summary>
		/// Event to notify any client about internal status or additional information
		/// This is just there for diagnostic reasons. From a functional point of view
		/// there is no need to deal with these events.
		/// </summary> 
		event InformationHandler Information;

		/// <summary>
        /// !!!!!!!!!! Do not use this function. !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		/// !!!!!!!!!! function only for SIEMENS internal testing purposes !!!!!!!!!!!
        /// Subscribe to a asynchronous Channel and start delivering of tag events or Alarms via
		/// the TagEventNotifications event or Alarms on this interface
		/// </summary>
        /// <param name="channels">Indicate the type of channel. TER = TagEvents, ALARM = Alarm, ALL = all types</param>
		/// <param name="ipAddress">The IP Addrsss of notification channel.</param>
		/// <param name="ipPort">Specifies the IP Port </param>
        /// <param name="bufferTag">Specifies whether tags should stored when connections is disturbed </param>
		void Subscribe( string channels, IPAddress ipAddress, int ipPort, bool bufferTag);

		/// <summary>
        /// !!!!!!!!!! Do not use this function. !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        /// !!!!!!!!!! function only for SIEMENS internal testing purposes !!!!!!!!!!!
        /// End notifications via TagEventNotifications by unsubscribing from an underlying
		/// notification channel.
		/// </summary>
        /// <param name="channels">Indicate the type of channel. TER = TagEvents, ALARM = Alarm, ALL = all types</param>
        void Unsubscribe(string channels);

		/// <summary>
		/// event to distribute the incoming notifications about changed RFID states
		/// </summary>
		event RfNotificationHandler TagEventNotifications;

		/// <summary>
		/// event to distribute RFID alarms
		/// </summary>
		event RfAlarmHandler Alarms;

	};
}
