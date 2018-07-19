using System;

namespace Siemens.Simatic.RfReader
{
    /// <summary>
    /// This class include information about IO Events
    /// </summary>
    public class RfIoEvent
    {
        /// <summary>
        /// Create anew instance of Io Events
        /// </summary>
        public RfIoEvent()
        {
        }

     #region Property
        /// <summary>
        /// The ioName
        /// </summary>
        public string IoName
        {
            get { return ioName; }
            set { ioName = value; }
        }
        private string ioName = "";

        /// <summary>
        /// The event type 
        /// </summary>
        public string IoEvent
        {
            get { return ioEvent; }
            set { ioEvent = value; }
        }
        private string ioEvent = "";

        /// <summary>
        /// The time stamp in UTC
        /// </summary>
        public DateTime IoTimeStamp
        {
            get { return ioTimeStamp; }
            set { ioTimeStamp = value; }
        }
        private DateTime ioTimeStamp = DateTime.MinValue;

     #endregion Property

    }
}
