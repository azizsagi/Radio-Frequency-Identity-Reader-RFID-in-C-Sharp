using System;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// Used to qualify information conveyed for later filtering
	/// </summary>
	public enum InformationType
	{
		/// <summary>Unspecified information</summary>
		Unspecified = 0,
		/// <summary>Information needed for debugging only.</summary>
		Debug,
		/// <summary>General information.</summary>
		Info,
		/// <summary>Just to notify users.</summary>
		Notice,
		/// <summary>A warning.</summary>
		Warning,
		/// <summary>An error.</summary>
		Error,
		/// <summary>A critical error.</summary>
		Critical,
		/// <summary>A very critical error.</summary>
		Alert,
		/// <summary>Better leave the building - now!</summary>
		Emergency
	}

	/// <summary>
	/// The argument class for information provision
	/// </summary>
	public class InformationArgs : EventArgs
	{
		private InformationType type = InformationType.Unspecified;

		/// <summary>
		/// The category this information belongs to, e.g. Debug, Notice, Error,...
		/// </summary>
		public InformationType Type
		{
			get { return type; }
			set { type = value; }
		}

		private string message = "";

		/// <summary>
		/// Access the stored message
		/// </summary>
		public string Message
		{
			get { return message; }
			set { message = value; }
		}

		/// <summary>
		/// Initialize an information structure
		/// </summary>
		/// <param name="type">The severity level of this information.</param>
		/// <param name="message">The message to be used</param>
		public InformationArgs(InformationType type, string message)
		{
			this.type = type;
			this.message = message;
			NewLine();
		}

		/// <summary>
		/// Initialize an information structure
		/// </summary>
		/// <param name="message">The message to be used</param>
		public InformationArgs(string message)
		{
			this.message = message;
			this.type = InformationType.Info;
			NewLine();
		}

		/// <summary>
		/// Initialize an information structure
		/// </summary>
		/// <param name="message">The message to be used</param>
		/// <param name="fSingleLine">Allow suppression of newline at the end of the message.</param>
		public InformationArgs(string message, bool fSingleLine)
		{
			this.message = message;
			if (!fSingleLine)
			{
				NewLine();
			}
		}

		/// <summary>
		/// Initialize an information structure
		/// </summary>
		/// <param name="type">The severity level of this information.</param>
		/// <param name="message">The message to be used</param>
		/// <param name="fSingleLine">Allow suppression of newline at the end of the message.</param>
		public InformationArgs(InformationType type, string message, bool fSingleLine)
		{
			this.type = type;
			this.message = message;
			if (!fSingleLine)
			{
				NewLine();
			}
		}

		/// <summary>
		/// Add a new line
		/// </summary>
		public void NewLine()
		{
			this.message += "\r\n";
		}

		/// <summary>
		/// Initialize an information structure with an exception
		/// </summary>
		/// <param name="ex">The exception whose information is to be passed on.</param>
		public InformationArgs(System.Exception ex)
		{
			this.message = "Exception: " + ex.ToString();
			this.type = InformationType.Error;
			NewLine();
		}
	}

	/// <summary>
	/// The delegate to provide information.
	/// </summary>
	/// <param name="sender">The object sending the information.</param>
	/// <param name="args">Additional arguments to be delivered.</param>
	public delegate void InformationHandler(object sender, InformationArgs args);
}