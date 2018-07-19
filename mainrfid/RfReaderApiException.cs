using System;
using System.Collections;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// This is the common exception that is thrown whenever
	/// there occur errors while using the reader API.
	/// Do not forget to fortify your implementations by catching
	/// this exception when using the interface.
	/// </summary>
	public class RfReaderApiException : System.Exception
	{
		/// <summary>This is the result code for API internal system errors</summary>
		public const int ResultCode_System = -1;
		/// <summary>This is the result code for internal reader errors</summary>
		public const int ResultCode_Reader = -2;

		/// <summary>Internal system error</summary>
		public const string Error_Internal = "ERROR_INTERNAL";
		/// <summary>This is the error code if no connection to the reader could be established.</summary>
		public const string Error_NoConnection = "ERROR_NO_CONNECTION";
		/// <summary>This is the error code if an issued command is not answered by the reader.</summary>
		public const string Error_NoReply = "ERROR_NO_REPLY";
		/// <summary>This is the error code if we received an invalid reply to a command from the reader.</summary>
		public const string Error_InvalidReply = "ERROR_INVALID_REPLY";
		/// <summary>This is the error code if we try to invoke functions while working in an invalid mode.</summary>
		public const string Error_InvalidMode = "ERROR_INVALID_MODE";
        /// <summary>This is the error code if we try to invoke functions with wrong parameters.</summary>
        public const string Error_InvalidParameter = "ERROR_INVALID_PARAMETER";
        /// <summary>This is the error code if we got reply from the reader with missing parameters.</summary>
        public const string Error_MissingParameter = "ERROR_MISSING_PARAMETER";


		/// <summary>
		/// Create an empty RfReaderApiException
		/// </summary>
		public RfReaderApiException() { }
		/// <summary>
		/// Create an RfReaderApiException
		/// </summary>
		/// <param name="resultCode">The error id (a number).</param>
		/// <param name="error">The error code (a string)</param>
		public RfReaderApiException(int resultCode, string error)
		{
			this.m_resultCode = resultCode;
			this.m_error = error;
		}
		/// <summary>
		/// Create an RfReaderApiException based on another
		/// existing exception used as an inner exception
		/// </summary>
		/// <param name="message">The exception message to be displayed.</param>
		/// <param name="innerException">A contained inner exception as a primary cause.</param>
		public RfReaderApiException(string message, System.Exception innerException)
			:
			base(message, innerException)
		{
            this.ResultCode = ResultCode_System;
            this.Error = message;
            this.Cause = innerException.ToString();
		}

		/// <summary>
		/// Create an RfReaderApiException
		/// </summary>
		/// <param name="resultCode">The error id as a number</param>
		/// <param name="error">The error code as a text</param>
		/// <param name="cause">The reason for the error</param>
		public RfReaderApiException(int resultCode, string error, string cause)
		{
			this.m_resultCode = resultCode;
			this.m_error = error;
			this.m_cause = cause;
		}

		/// <summary>
		/// The error id 
		/// </summary>
		public int ResultCode
		{
			get { return m_resultCode; }
			set { m_resultCode = value; }
		}
		private int m_resultCode = 0;

		/// <summary>
		/// The error code
		/// </summary>
		public string Error
		{
			get { return m_error; }
			set { m_error = value; }
		}
		private string m_error = "";

		/// <summary>
		/// Additional descriptions for an error
		/// </summary>
		public string Cause
		{
			get { return m_cause; }
			set { m_cause = value; }
		}
		private string m_cause = "";
	}

	/// <summary>
	/// This exception occurs whenever the reader API is in an invalid mode.
	/// Normally the cause is that an underlying reader service is reconfigured.
	/// During reconfiguration, internal data may be in an inconsistent state and
	/// as a consequence calls to the interface might fail. That is why the interface
	/// blocks all calls but StartReader and StopReader during reconfiguration.
	/// </summary>
	public class RfReaderApiInvalidModeException : RfReaderApiException
	{
		/// <summary>
		/// Create a new invalid mode exception
		/// </summary>
		public RfReaderApiInvalidModeException()
			: base(ResultCode_System, Error_InvalidMode, 
			"During reconfiguration of the reader service the behavior of the interface is undefined and thus calls are blocked.")
		{
		}

	};
}
