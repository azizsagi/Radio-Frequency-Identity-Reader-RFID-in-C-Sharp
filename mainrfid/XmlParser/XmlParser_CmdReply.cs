using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Collections.ObjectModel;

namespace Siemens.Simatic.RfReader.ReaderApi.XmlBinding
{

  	/// <summary>
	/// This class parses the replys got from our reader service.
	/// </summary>
    internal partial class XmlParser
    {
		private bool m_IsValidReply = false;
		private bool m_IsHeaderRead = false;

		/// <summary>
		/// Creates an instance of the reply message parser
		/// </summary>
		internal XmlParser()
		{ }

		internal CommandReply ParseReply(string replyMsg)
		{
	        Collection<string> paramName = new Collection<string>();

		    CommandReply result = new CommandReply();

			StringReader strReader = new StringReader(replyMsg);

			XmlTextReader replyReader = new XmlTextReader(strReader);

            while ("" != replyMsg && replyReader.Read())
			{
				switch (replyReader.NodeType)
				{
					case XmlNodeType.Element:
						if (replyReader.Name == "returnValue")
						{
							switch (result.CommandName)
							{
                                case "readTagIDs":
                                case "readTagMemory":
                                case "writeTagMemory":
								case "readTagField":
                                case "writeTagField":
                                case "killTag":
                                case "lockTagBank":    
                                case "nXP_SetReadProtect":
                                case "nXP_ResetReadProtect":
                                case "nXP_Calibrate":
                                    ParseForTagData(replyReader, ref result);
                                    break;

                                case "hostGreetings":
                                    paramName.Add("version");
                                    paramName.Add("configType");
                                    paramName.Add("configID");
                                    ParseReplyParam(replyReader, ref result, paramName);
                                    break;

                                case "setConfiguration":
                                    paramName.Add("configID");
                                    ParseReplyParam(replyReader, ref result, paramName);
                                    break;

                                case "getConfigVersion":
                                    paramName.Add("configID");
                                    paramName.Add("configType");
                                    ParseReplyParam(replyReader, ref result, paramName);
                                    break;

                                case "getConfiguration":
                                    paramName.Add("configID");
                                    paramName.Add("configData");
                                    ParseReplyParam(replyReader, ref result,paramName);
									break;

                                case "getAllSources":
                                    paramName.Add("sourceName");
                                    ParseReplyParam(replyReader, ref result, paramName);
                                    break;

                                case "getTime":
                                    paramName.Add("utcTime");
                                    ParseReplyParam(replyReader, ref result,paramName);
									break;

                                case "getIO":
                                    paramName.Add("inValue");
                                    paramName.Add("outValue");
                                    ParseReplyParam(replyReader, ref result,paramName);
									break;

                                case "getReaderStatus":
                                    paramName.Add("readerType");
                                    paramName.Add("mLFB");
                                    paramName.Add("hWVersion");
                                    paramName.Add("fWVersion");
                                    paramName.Add("readerMode");
                                    paramName.Add("version");
                                    ParseReplyParam(replyReader, ref result,paramName);
									break;
                                    
                                case "getProtocolConfig":
                                    paramName.Add("initialQ"); 
                                    paramName.Add("profile");
                                    paramName.Add("channels");
                                    paramName.Add("session");
                                    paramName.Add("rSSIThreshold");
                                    paramName.Add("retry");
                                    paramName.Add("idLength");
                                    paramName.Add("writeMode");
                                    paramName.Add("writeBoost");
                                    ParseReplyParam(replyReader, ref result, paramName);
                                    break;

                                case "getAntennaConfig":
                                    ParseAntennaReplyParam(replyReader, ref result);
                                    break;

                               case "sendCommand":
                                   paramName.Add("byteReply");
                                    ParseReplyParam(replyReader, ref result, paramName);
                                    break;
                                    
     
								// This is an unknown result so we just ignore it
								default:
									break;

							}

							// Here we are: Now the interesting part begins which is the real
							// return value.
							while (replyReader.Read() &&
									!(replyReader.Name == "returnValue" && replyReader.NodeType == XmlNodeType.EndElement) )
							{
								if (replyReader.Name == "value")
								{
									if (replyReader.Read())
										if (XmlNodeType.Text == replyReader.NodeType)
										{
											ParameterDesc param = new ParameterDesc("value", replyReader.Value);
											result.Parameters.Add(param);
										}
								}
							}
						}
						else if (replyReader.Name == "error")
						{
							// Read in extended error information
							while (replyReader.Read() &&
									!(replyReader.Name == "error" && replyReader.NodeType == XmlNodeType.EndElement))
							{
								if (replyReader.Name == "name" && replyReader.NodeType == XmlNodeType.Element)
								{
									if (replyReader.Read() && XmlNodeType.Text == replyReader.NodeType)
									{
										result.Error = replyReader.Value.ToString();
									}
								}
								else if (replyReader.Name == "cause" && replyReader.NodeType == XmlNodeType.Element)
								{
									if (replyReader.Read() && XmlNodeType.Text == replyReader.NodeType)
									{
										result.Cause = replyReader.Value.ToString();
									}
								}
							}
						}
						else if (this.m_IsHeaderRead)
						{
							result.CommandName = replyReader.Name;
						}
						else if (this.m_IsValidReply)
						{
							switch (replyReader.Name)
							{
								case "id":
									if (replyReader.Read())
										if (XmlNodeType.Text == replyReader.NodeType)
											result.CommandID = replyReader.Value.ToString();
									break;

								case "resultCode":
									if (replyReader.Read())
										if (XmlNodeType.Text == replyReader.NodeType)
											result.ResultCode = Int32.Parse(replyReader.Value);
									break;

								default:
									break;
							}
						}
						else if (replyReader.Name == "reply")
						{
							this.m_IsValidReply = true;
						}
						break;

					case XmlNodeType.EndElement:
						switch (replyReader.Name)
						{
							case "resultCode":
								this.m_IsHeaderRead = true;
								break;

							default:
								break;
						}
						break;

					default:
						break;
				}
			}

			return result;
		}

        private void ParseForTagData(XmlTextReader replyReader, ref CommandReply result)
        {
             result.TagData =  ParseReportForTags(replyReader, "returnValue");
        }

        private void ParseAntennaReplyParam(XmlTextReader replyReader, ref CommandReply result)
        {
            // do until end XML tag </returnValue> is found
            while (replyReader.Read() &&
                    !(replyReader.Name == "returnValue" && replyReader.NodeType == XmlNodeType.EndElement))
            {
                // start XML tag <antenna>: we get an paramblock for antennas
                if (replyReader.Name == "antenna" && replyReader.NodeType == XmlNodeType.Element)
                {
                    ParameterDesc container = new ParameterDesc("antenna", true, true);
                    result.Parameters.Add(container);
                    // do until end XML tag </antenna> is found
                    while (replyReader.Read() &&
					    !(replyReader.Name == "antenna" && replyReader.NodeType == XmlNodeType.EndElement))
			        {
                        if (replyReader.NodeType == XmlNodeType.Element)
                        {
                            string paramName = replyReader.Name;
                            if ( replyReader.Read() && (XmlNodeType.Text == replyReader.NodeType ))
                            {
                                ParameterDesc param = new ParameterDesc(paramName, replyReader.Value);
                                result.Parameters.Add(param);
                            }
                        }
			        }
                }
            }
        }
 
        private void ParseReplyParam(XmlTextReader replyReader, ref CommandReply result, Collection<string> Name)
		{
            while (replyReader.Read() &&
					!(replyReader.Name == "returnValue" && replyReader.NodeType == XmlNodeType.EndElement))
			{
                if (replyReader.NodeType == XmlNodeType.Element)
                {
                    foreach (string paramName in Name)
	                {
                        if (replyReader.Name == paramName)
                        {
                            if ( replyReader.Read() 
                                && (XmlNodeType.Text == replyReader.NodeType || XmlNodeType.CDATA == replyReader.NodeType))
                            {
                                ParameterDesc param = new ParameterDesc(paramName, replyReader.Value);
                                result.Parameters.Add(param);
                            }
                        } 
	                }
                }
			}
		}
    }
}
