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
        
        internal RfReport[] ParseReport(string report)
		{
            RfReport[] reportList = null;

			StringReader strReader = new StringReader(report);

			XmlTextReader reportReader = new XmlTextReader(strReader);

            // check strart elemen = <report>
            if (reportReader.Read() &&
                  "report" == reportReader.Name && reportReader.NodeType == XmlNodeType.Element)
            {
                // loop until we get end elemen </report>
                while (reportReader.Read() &&
                  !("report" == reportReader.Name  && reportReader.NodeType == XmlNodeType.EndElement))
                {
                    // ---------------------------------------------------------------------------------------
                    // tag event reports
                    // check start element <ter>
                    if (reportReader.Read() &&
                         "ter" == reportReader.Name && reportReader.NodeType == XmlNodeType.Element)
                    {
                        // loop until end element </ter>
                        while (reportReader.Read() &&
                                !("ter" == reportReader.Name  && reportReader.NodeType == XmlNodeType.EndElement))
                        {
                            // check start element <source>
                            if ("source" == reportReader.Name  && reportReader.NodeType == XmlNodeType.Element)
                            {
                                // event report with Tags found, create source container
                                reportList = AddReportStructure(reportList);
                                int reportListIndex = reportList.Length - 1;

                                // get sourceName
                                if (reportReader.Read() && 
                                    "sourceName" == reportReader.Name && reportReader.NodeType == XmlNodeType.Element)
                                {
                                    if (reportReader.Read())
                                    {
                                        if (XmlNodeType.Text == reportReader.NodeType)
                                        {
                                            // Store source name
                                            reportList[reportListIndex].SourceName = reportReader.Value;
                                            reportList[reportListIndex].ReportType = "ter";
                                            // get all tags from this source
                                            reportList[reportListIndex].Tag = ParseReportForTags(reportReader, "source");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // ---------------------------------------------------------------------------------------
                    // RSSI event reports
                    else if ("rssier" == reportReader.Name && reportReader.NodeType == XmlNodeType.Element)
                    {
                        // event report with Tags found, create tag event list container
                        reportList = AddReportStructure(reportList);
                        int reportIndex = reportList.Length - 1;
                        reportList[reportIndex].ReportType = "rssier";

                        // get all tags from this event report
                        reportList[reportIndex].Tag = ParseReportForTags(reportReader, "rssier");
                    }
                    else if ("ioer" == reportReader.Name && reportReader.NodeType == XmlNodeType.Element)
                    {
                        // event report with IOs found, create io list container
                        reportList = AddReportStructure(reportList);
                        int reportIndex = reportList.Length - 1;
                        reportList[reportIndex].ReportType = "ioer";

                        // get all io events from this event report
                        reportList[reportIndex].IoEvent = ParseReadReportIO(reportReader, "ioer");
                    }
                }
            }
            return reportList;
		}

	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reportReader"></param>
        /// <param name="endXMLTag"></param>
        protected RfTag [] ParseReportForTags(XmlTextReader reportReader, string endXMLTag)
		{
            RfTag [] rfTag = null;
            // loop until end element <source> 
            while (reportReader.Read() &&
                !(reportReader.Name == endXMLTag && reportReader.NodeType == XmlNodeType.EndElement))
            {   // check for start element <tag>
                if ("tag" == reportReader.Name  && reportReader.NodeType == XmlNodeType.Element)
                {
                    rfTag = AddTagStructure(rfTag);
                    int tagIndex = rfTag.Length - 1;
                    if ("rssier" == endXMLTag)
                    { // for RSSI Events we have to init TagEvent
                        rfTag[tagIndex].TagEvent = "RSSI";
                    }

    		        while (reportReader.Read() &&
					        !(reportReader.Name == "tag" && reportReader.NodeType == XmlNodeType.EndElement))
			        {
				        if (reportReader.NodeType == XmlNodeType.Element)
				        {
					        if (reportReader.Name == "tagID")
					        {
						        if (reportReader.Read() && XmlNodeType.Text == reportReader.NodeType)
						        {
                                    rfTag[tagIndex].TagID = reportReader.Value;
						        }
					        }
                            else if (reportReader.Name == "success")
                            {
                                if (reportReader.Read() && XmlNodeType.Text == reportReader.NodeType)
                                {
                                    rfTag[tagIndex].SuccessFlag = bool.Parse(reportReader.Value);
                                }
                            }
					        else if (reportReader.Name == "event")
					        {
    	            	        if (reportReader.Read() && XmlNodeType.Text == reportReader.NodeType)
                                {
                                    rfTag[tagIndex].TagEvent = reportReader.Value;
							    }
						    }
    	                    else if (reportReader.Name == "utcTime")
					        {
    	            	        if (reportReader.Read() && XmlNodeType.Text == reportReader.NodeType)
						        {
                                    try
                                    {
                                        rfTag[tagIndex].TagTimeStamp = DateTime.ParseExact(reportReader.Value, "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz", null);
                                    }
                                    catch (Exception)
                                    {
                                        rfTag[tagIndex].TagTimeStamp = DateTime.MinValue;
                                    }
						        }
                            }
                            else if (reportReader.Name == "antennaName")
                            {
    	            	        if (reportReader.Read() && XmlNodeType.Text == reportReader.NodeType)
                                {
                                    rfTag[tagIndex].TagAntenna = reportReader.Value;
                                }
                            }
        	                else if (reportReader.Name == "rSSI")
					        {
    	            	        if (reportReader.Read() && XmlNodeType.Text == reportReader.NodeType)
						        {
                                    rfTag[tagIndex].TagRSSI = reportReader.Value;
						        }
					        }
                            else if (reportReader.Name == "tagField")
                            {
                                // add one TagField
                                rfTag[tagIndex].TagFields = AddFieldStructure(rfTag[tagIndex].TagFields);
                                
                                // fill in Values in new created TagField
                                int fieldIndex = rfTag[tagIndex].TagFields.Length - 1;
                                rfTag[tagIndex].TagFields[fieldIndex] = ParseReportForTagFields(reportReader, "tagField");
                            }
                        }
				    }
			    }
            }
            return rfTag;
   		}

        		/// <summary>
		/// 
		/// </summary>
		/// <param name="reportReader"></param>
        /// <param name="endXMLTag"></param>
        protected RfIoEvent[] ParseReadReportIO(XmlTextReader reportReader, string endXMLTag)
		{

            RfIoEvent[] rfIoEvent = null;
            // loop until end element 
            while (reportReader.Read() &&
                !(reportReader.Name == endXMLTag && reportReader.NodeType == XmlNodeType.EndElement))
            {
                // check for start element <io>
                if ("io" == reportReader.Name  && reportReader.NodeType == XmlNodeType.Element)
                {
                    rfIoEvent = AddIoEventStructure(rfIoEvent);
                    int ioEventIndex = rfIoEvent.Length - 1;

    		        while (reportReader.Read() &&
					        !(reportReader.Name == "io" && reportReader.NodeType == XmlNodeType.EndElement))
			        {
				        if (reportReader.NodeType == XmlNodeType.Element)
				        {
					        if (reportReader.Name == "ioName")
					        {
						        if (reportReader.Read() && XmlNodeType.Text == reportReader.NodeType)
						        {
                                    rfIoEvent[ioEventIndex].IoName = reportReader.Value;
						        }
					        }
                            else if (reportReader.Name == "ioEvent")
                            {
                                if (reportReader.Read() && XmlNodeType.Text == reportReader.NodeType)
                                {
                                    rfIoEvent[ioEventIndex].IoEvent = reportReader.Value;
                                }
                            }
					        else if (reportReader.Name == "utcTime")
					        {
    	            	        if (reportReader.Read() && XmlNodeType.Text == reportReader.NodeType)
						        {
                                    try
                                    {
                                        rfIoEvent[ioEventIndex].IoTimeStamp = DateTime.ParseExact(reportReader.Value, "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz", null);
                                    }
                                    catch (Exception)
                                    {
                                        rfIoEvent[ioEventIndex].IoTimeStamp = DateTime.MinValue;
                                    }
						        }
                            }
                        }
				    }
			    }
            }
            return rfIoEvent;
   		}
        

        /// <summary>
		/// 
		/// </summary>
		/// <param name="reportReader"></param>
        /// <param name="endXMLTag"></param>
        protected RfTagField ParseReportForTagFields(XmlTextReader reportReader, string endXMLTag)
        {
            RfTagField newTagField = new RfTagField();

            while (reportReader.Read() &&
                !(reportReader.Name == endXMLTag && reportReader.NodeType == XmlNodeType.EndElement))
            {
                if (reportReader.NodeType == XmlNodeType.Element)
                {
                    if (reportReader.Name == "bank")
                    {
                        if (reportReader.Read())
                        {
                            if (XmlNodeType.Text == reportReader.NodeType)
                            {
                                newTagField.TagFieldBank = reportReader.Value;
                            }
                        }
                    }
                    else if (reportReader.Name == "startAddress")
                    {
                        if (reportReader.Read())
                        {
                            if (XmlNodeType.Text == reportReader.NodeType)
                            {
                                newTagField.TagFieldAddress = reportReader.Value;
                            }
                        }
                    }
                    else if (reportReader.Name == "fieldName")
                    {
                        if (reportReader.Read())
                        {
                            if (XmlNodeType.Text == reportReader.NodeType)
                            {
                                newTagField.TagFieldName = reportReader.Value;
                            }
                        }
                    }
                    else if (reportReader.Name == "dataLength")
                    {
                        if (reportReader.Read())
                        {
                            if (XmlNodeType.Text == reportReader.NodeType)
                            {
                                newTagField.TagFieldLength = reportReader.Value;
                            }
                        }
                    }
                    else if (reportReader.Name == "data")
                    {
                        if (reportReader.Read())
                        {
                            if (XmlNodeType.Text == reportReader.NodeType)
                            {
                                newTagField.TagFieldData = reportReader.Value;
                            }
                        }
                    }
                }
            }
            return newTagField;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tagEventList"></param>
        protected RfReport[] AddReportStructure(RfReport[] reportList)
        {
            RfReport[] newTagEventList = null;

            // for the first time we have to init the list
            if (null == reportList)
            {
                newTagEventList = new RfReport[1];
                newTagEventList[0] = new RfReport();
            }
            else
            {
                // Create a new list one element longer than the old one
                newTagEventList = new RfReport[reportList.Length + 1];
                for (int pos = 0; pos < reportList.Length; pos++)
                {
                    newTagEventList[pos] = reportList[pos];
                }

                // Yippieh! Create a new tagevent
                newTagEventList[reportList.Length] = new RfReport();
            }
            return newTagEventList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rfTag"></param>
        protected RfTag [] AddTagStructure(RfTag[] rfTag)
        {
            RfTag[] newRfReaderTag = null;

            // for the first time we have to init the list
            if (null == rfTag)
            {
                newRfReaderTag = new RfTag[1];
                newRfReaderTag[0] = new RfTag();
            }
            else
            {
                // Create a new list one element longer than the old one
                newRfReaderTag = new RfTag[rfTag.Length + 1];
                for (int pos = 0; pos < rfTag.Length; pos++)
                {
                    newRfReaderTag[pos] = rfTag[pos];
                }
                // Yippieh! Create a new tagFieldList
                newRfReaderTag[rfTag.Length] = new RfTag();
            }
            return newRfReaderTag;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rfTagfield"></param>
        protected RfTagField[] AddFieldStructure(RfTagField[] rfTagfield)
        {
            RfTagField[] newRfTagfield = null;

            // for the first time we have to init the list
            if (null == rfTagfield)
            {
                // first tag field
                newRfTagfield = new RfTagField[1];
                newRfTagfield[0] = new RfTagField();
            }
            else            
            {
                // Create a new list one element longer than the old one
                newRfTagfield = new RfTagField[rfTagfield.Length + 1];
                for (int pos = 0; pos < rfTagfield.Length; pos++)
                {
                    newRfTagfield[pos] = rfTagfield[pos];
                }
                // Yippieh! Create a new tagFieldList
                newRfTagfield[rfTagfield.Length] = new RfTagField();
            }
            return newRfTagfield;
        }

               /// <summary>
        /// 
        /// </summary>
        /// <param name="rfTag"></param>
        protected RfIoEvent[] AddIoEventStructure(RfIoEvent[] rfIoEvent)
        {
            RfIoEvent[] newRfIoEvent = null;

            // for the first time we have to init the list
            if (null == rfIoEvent)
            {
                newRfIoEvent = new RfIoEvent[1];
                newRfIoEvent[0] = new RfIoEvent();
            }
            else
            {
                // Create a new list one element longer than the old one
                newRfIoEvent = new RfIoEvent[rfIoEvent.Length + 1];
                for (int pos = 0; pos < rfIoEvent.Length; pos++)
                {
                    newRfIoEvent[pos] = rfIoEvent[pos];
                }
                // Yippieh! Create a new tagFieldList
                newRfIoEvent[rfIoEvent.Length] = new RfIoEvent();
            }
            return newRfIoEvent;
        }
        
	}	// class
}
