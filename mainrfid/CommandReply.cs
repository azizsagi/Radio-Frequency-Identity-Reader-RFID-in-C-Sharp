using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using Siemens.Simatic.RfReader;

namespace Siemens.Simatic.RfReader.ReaderApi
{
	/// <summary>
	/// This structure is used to wrap a parameter passed from the simple API to
	/// the reader service.
	/// </summary>
	internal struct ParameterDesc
	{
		public ParameterDesc(string key, object value) :
			this(key, value, false, false, false)
		{
		}

		public ParameterDesc(string key, object value, bool isContainer) :
			this(key, value, isContainer, false, false)
		{
		}

		public ParameterDesc(string key, object value, bool isContainer, bool isLastChild) :
			this(key, value, isContainer, isLastChild, false)
		{
		}

		public ParameterDesc(string key, object value, bool isContainer, bool isLastChild, bool isEmpty)
		{
			this.key = key;
			this.value = value;
			this.isContainer = isContainer;
			this.isLastChild = isLastChild;
			this.isEmpty = isEmpty;
		}

		public string key;
		public object value;
		public bool isContainer;
		public bool isEmpty;
		public bool isLastChild;
	}

	internal class Command
	{
		public Command()
		{
			m_commandID = CommandChannel.NewCommandID.ToString();
		}

//// todo: delete, is obsolet
//        public Command(string targetName, string groupName, string commandName)
//            : this( commandName)
//        {
//        }

//// todo: delete, is obsolet
//        public Command(string targetName, string groupName, string commandName,
//            List<ParameterDesc> parameters)
//            : this(commandName, parameters)
//        {
//        }

        public Command(string commandName)
            : this(commandName, null)
        {
        }

        public Command(string commandName, List<ParameterDesc> parameters)
		{
			m_commandName = commandName;
			m_commandID = CommandChannel.NewCommandID.ToString();
			m_parameters = parameters;
		}


		private string m_commandName = "";
		public string CommandName
		{
			get { return this.m_commandName; }
			set { this.m_commandName = value; }
		}

		private string m_commandID = "";
		public string CommandID
		{
		  get { return m_commandID; }
		  set { m_commandID = value; }
		}

      	private List<ParameterDesc> m_parameters = new List<ParameterDesc>();
		public List<ParameterDesc> Parameters
		{
		  get { return m_parameters; }
		  set { m_parameters = value; }
		}

        public string getXmlCommand()
        {
            string commandId = "";
            return getXmlCommand(ref  commandId);
        }
		public string getXmlCommand(ref string commandId)
		{
            commandId = this.CommandID;
			StringBuilder msg = new StringBuilder();
            msg.Append("<frame>");
            msg.Append("<cmd>");
			msg.Append("<id>");
            msg.Append(commandId);
			msg.Append("</id>");

			msg.Append("<");
			msg.Append(this.CommandName);
			msg.Append(">");

			if (null != this.Parameters)
			{
				IEnumerator<ParameterDesc> paramEnum = this.Parameters.GetEnumerator();
				AddParameters(paramEnum, msg);
			}

			msg.Append("</");
			msg.Append(this.CommandName);
			msg.Append(">");

	
			msg.Append("</cmd>");
            msg.Append("</frame>");

			return msg.ToString();
		}

		private void AddParameters(IEnumerator<ParameterDesc> paramEnum, StringBuilder msg)
		{
			while (paramEnum.MoveNext())
			{
				ParameterDesc param = paramEnum.Current;
				if (param.isContainer)
				{
					// Containing parameter need to add a bracket around contained parameters
					msg.Append("<");
					msg.Append(param.key);
					msg.Append(">");

					// Enable empty lists by checking for end of container before
					// processing children
					if (! param.isEmpty)
					{
						// Now add the contained list
						AddParameters(paramEnum, msg);
					}

					msg.Append("</");
					msg.Append(param.key);
					msg.Append(">");
				}
				else
				{
					// Simple parameter are just added
					AddXmlParam(msg, param);
				}
				// If this is the last child of a container, break out of the loop
				// in order to close the container
				if (param.isLastChild)
				{
					break;
				}
			}
		}

		private void AddXmlParam(StringBuilder msg, ParameterDesc param)
		{
			msg.Append("<");
			msg.Append(param.key);
			msg.Append(">");
			msg.Append(param.value);
			msg.Append("</");
			msg.Append(param.key);
			msg.Append(">");
		}
	}

    internal class CommandReply
    {
		private string m_commandName = "";
		public string CommandName
		{
			get { return this.m_commandName; }
			set { this.m_commandName = value; }
		}

		private string m_commandID = "";
		public string CommandID
		{
			get { return m_commandID; }
			set { m_commandID = value; }
		}
        private int m_resultCode = 0;
		public int ResultCode
		{
		  get { return m_resultCode; }
		  set { m_resultCode = value; }
		}
		private string m_error = "";
		public string Error
		{
			get { return m_error; }
			set { m_error = value; }
		}
	
		private string m_cause = "";
		public string Cause
		{
			get { return m_cause; }
			set { m_cause = value; }
		}

		private List<ParameterDesc> m_parameters = new List<ParameterDesc>();
		public List<ParameterDesc> Parameters
		{
			get { return m_parameters; }
			set { m_parameters = value; }
		}

        private RfTag [] m_rfReaderTag = null;
        public RfTag [] TagData
        {
            get { return m_rfReaderTag; }
            set { m_rfReaderTag = value; }
        }

		public CommandReply()
		{ }

		public static CommandReply DecodeXmlCommand(string replyMessage)
		{
			return CommandReply.DecodeXmlCommand(replyMessage, "");
		}

		public static CommandReply DecodeXmlCommand(string replyMessage, Command cmd)
		{
			return CommandReply.DecodeXmlCommand(replyMessage, cmd.CommandID);
		}

		public static CommandReply DecodeXmlCommand(string replyMessage, string commandID)
		{
			CommandReply result = null;
			try{
				XmlBinding.XmlParser xmlParser = new XmlBinding.XmlParser();

				result = xmlParser.ParseReply(replyMessage);
			}
			catch(Exception ex)
			{
				RfReaderApi.CurrentApi.ProvideInformation("CmdReply", ex);
			}

			return result;
		}
	}
}
