using System;


namespace Siemens.Simatic.RfReader
{
 	/// <summary>
	/// This class gives all information about the configuration 
	/// </summary>
	public class RfConfigID
	{
		/// <summary>
		/// Create anew instance of configuration description
		/// </summary>
        public RfConfigID()
		{
            configID = "";
            configType = "";
		}

        /// <summary>
        /// The name (or id) of the stored configuration 
        /// </summary>
        public string ConfigID
        {
            get { return configID; }
            set { configID = value; }
        }
        private string configID;

        /// <summary>
        /// The name (or id) of the stored configuration 
        /// </summary>
        public string ConfigType
        {
            get { return configType; }
            set { configType = value; }
        }
        private string configType;
	}
}