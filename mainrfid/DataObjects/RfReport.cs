using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Siemens.Simatic.RfReader.ReaderApi.XmlBinding;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// This class gives all information about reports send from the reader
	/// </summary>
	public partial class RfReport
	{
		/// <summary>
		/// Create anew instance of an report
		/// </summary>
		public RfReport()
		{
		}

        /// <summary>
        /// The type of report we got
        /// </summary>
        public string ReportType
        {
            get { return reportType; }
            set { reportType = value; }
        }
        private string reportType = "";

        /// <summary>
        /// The source where the tag is read
        /// </summary>
        public string SourceName
        {
            get { return sourceName; }
            set { sourceName = value; }
        }
        private string sourceName = "";

        /// <summary>
        /// The tag datas
        /// </summary>
        public RfTag[] Tag;

        /// <summary>
        /// The IO data
        /// </summary>
        public RfIoEvent[] IoEvent;
 

        internal static RfReport[] GetListFromReport(string report)
        {
            RfReport[] tagEventList = null;

            try
            {
                XmlParser xmlParser = new XmlParser();

                tagEventList = xmlParser.ParseReport(report);
            }
            catch (Exception ex)
            {
                RfReaderApi.CurrentApi.ProvideInformation("CmdReply", ex);
            }

            return tagEventList;
        }
    }
}
