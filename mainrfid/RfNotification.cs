using System;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// The argument class for RFID notifications
	/// </summary>
	public class RfNotificationArgs : EventArgs
	{
		private RfReport[] reportList = null;

		/// <summary>
		/// A list of tag events
		/// </summary>
		public RfReport[] ReportList
		{
			get { return reportList; }
			set { reportList = value; }
		}

		/// <summary>
		/// Creates a notification argument class containing a list of tag events
		/// </summary>
		/// <param name="eventList"></param>
		public RfNotificationArgs(RfReport[] reportList)
		{
			this.reportList = reportList;
		}
	}

	/// <summary>
	/// The delegate to provide tag event notifications
	/// </summary>
	/// <param name="sender">The sending object from within the RFID runtime</param>
	/// <param name="args">Additional parameters such as the tag events delivered.</param>
	public delegate void RfNotificationHandler(object sender, RfNotificationArgs args);

	/// <summary>
	/// 
	/// </summary>
	public class RfAlarmArgs : EventArgs
	{
 
		/// <summary>The raw alarm message</summary>
        private string alarmMsg;
        /// <summary>
        /// The raw message text of the alarm
        /// </summary>
        public string AlarmMessage
        {
            get { return this.alarmMsg; }
        }

        /// <summary>
		/// A list of tag events
		/// </summary>
		public RfAlarm[] Alarms
		{
			get { return this.alarmList; }
			set { this.alarmList = value; }
		}
		private RfAlarm[] alarmList = null;

		/// <summary>
		/// 
		/// </summary>
		public RfAlarmArgs(RfAlarm[] alarmList)
		{
			this.alarmList = alarmList;
		}

		/// <summary>
		/// Create alarm event arguments
		/// </summary>
		public RfAlarmArgs(string msg)
		{
			this.alarmMsg = msg;
		}
	}

	/// <summary>
	/// The delegate to provide RFID alarms
	/// </summary>
	/// <param name="sender">The sending object from within the RFID runtime</param>
	/// <param name="args">Additional parameters</param>
	public delegate void RfAlarmHandler(object sender, RfAlarmArgs args);
}