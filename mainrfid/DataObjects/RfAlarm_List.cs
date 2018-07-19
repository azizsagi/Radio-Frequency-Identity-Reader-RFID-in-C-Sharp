using System;
using System.Text;
using Siemens.Simatic.RfReader.ReaderApi.XmlBinding;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// This class gives all information about a single alarm
	/// </summary>
	public partial class RfAlarm
	{
		/// <summary>
		/// Retrieve list of alarms from message
		/// </summary>
		internal static RfAlarm[] GetAlarmsFromMessage(string message)
		{
			RfAlarm[] alarmList = null;

			try
			{
				XmlParser xmlParser = new XmlParser();

				alarmList = xmlParser.ParseAlarms(message);
			}
			catch (Exception ex)
			{
				RfReaderApi.CurrentApi.ProvideInformation("CmdReply", ex);
			}

			return alarmList;
		}
	}
}
