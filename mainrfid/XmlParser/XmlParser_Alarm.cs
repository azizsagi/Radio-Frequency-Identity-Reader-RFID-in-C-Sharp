using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using Siemens.Simatic.RfReader;

namespace Siemens.Simatic.RfReader.ReaderApi.XmlBinding
{
	/// <summary>
	/// This class parses the replys got from our reader service.
	/// </summary>
	internal partial class XmlParser
	{
		private int alarmCount = 0;
		private int alarmIndex = 0;

		internal RfAlarm[] ParseAlarms(string alarmMsg)
		{
			RfAlarm[] alarmList = null;

			StringReader strReader = new StringReader(alarmMsg);

			XmlTextReader alarmReader = new XmlTextReader(strReader);

			this.alarmCount = 0;
			this.alarmIndex = 0;

            //RfReaderInfo currentReaderInfo = new RfReaderInfo();

            while (alarmReader.Read())
            {
                switch (alarmReader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (alarmReader.Name)
                        {
                            case "error":
                                RfAlarm[] oldAlarmList = alarmList;
                                // Create a new list one element longer than the old one
                                alarmList = new RfAlarm[this.alarmIndex + 1];
                                for (int pos = 0; pos < this.alarmIndex; pos++)
                                {
                                    alarmList[pos] = oldAlarmList[pos];
                                }

                                // Yippieh! Create a new alarm 
                                alarmList[this.alarmIndex] = new RfAlarm();
                                alarmList[this.alarmIndex].AdditionalAlarmData = new Hashtable();
                                break;

                            case "utcTime":
                                if (alarmReader.Read())
                                {
                                    if (XmlNodeType.Text == alarmReader.NodeType)
                                    {
                                        alarmList[this.alarmIndex].UtcTime = alarmReader.Value;
                                    }
                                }
                                break;

                            case "errorNumber":
                                if (alarmReader.Read())
                                {
                                    if (XmlNodeType.Text == alarmReader.NodeType)
                                    {
                                        alarmList[this.alarmIndex].ErrorNumber = alarmReader.Value;
                                    }
                                }
                                break;

                            case "errorText":
                                if (alarmReader.Read())
                                {
                                    if (XmlNodeType.Text == alarmReader.NodeType)
                                    {
                                        alarmList[this.alarmIndex].ErrorText = alarmReader.Value;
                                    }
                                }
                                break;

                            default :
                                string Name = alarmReader.Name;
                                if (alarmReader.Read())
                                {
                                    if (XmlNodeType.Text == alarmReader.NodeType)
                                    {
                                        alarmList[this.alarmIndex].AdditionalAlarmData[Name] = alarmReader.Value;
                                    }
                                }
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (alarmReader.Name == "error")
                        {
                            this.alarmIndex++;
                            this.alarmCount++;
                        }
                        break;

                    default:
                        break;
                }
            }

			return alarmList;
		}
	}
}
