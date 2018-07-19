using System;
using System.Collections;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// This class gives all information about a single alarm
	/// </summary>
	public partial class RfAlarm
	{
		/// <summary>
		/// Create a new instance of an Alarm
		/// </summary>
		public RfAlarm()
		{
		}

        /// <summary>
        /// The alarm's number
        /// </summary>
        public string ErrorNumber
        {
            get { return errorNumber; }
            set { errorNumber = value; }
        }

        private string errorNumber = "";

        /// <summary>
        /// The alarm's number
        /// </summary>
        public string ErrorText
        {
            get { return errorText; }
            set { errorText = value; }
        }

        private string errorText = "";

		/// <summary>
		/// The alarm's time stamp
		/// </summary>
		public string UtcTime
		{
            get { return utcTime; }
            set { utcTime = value; }
        }

        private string utcTime = "";
    
        /// <summary>
        /// 
        /// </summary>
        public Hashtable AdditionalAlarmData
        {
            get { return this.additionalAlarmData; }
            set { this.additionalAlarmData = value; }
        }
        private Hashtable additionalAlarmData = null;

	}

}
