using System;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// This class gives all information about a single tag field
	/// </summary>
    public class RfTagField
    {
        /// <summary>
        /// Create anew instance of a TagField
        /// </summary>
        public RfTagField()
        {
        }

        /// <summary>
        /// The tag field's name
        /// </summary>
        public string TagFieldName
        {
            get { return this.tagFieldName; }
            set { this.tagFieldName = value; }
        }
        private string tagFieldName = "";

        /// <summary>
        /// The tag field's Bank
        /// </summary>
        public string TagFieldBank
        {
            get { return this.tagFieldBank; }
            set { this.tagFieldBank = value; }
        }
        private string tagFieldBank = "";

        /// <summary>
        /// The tag field's Address
        /// </summary>
        public string TagFieldAddress
        {
            get { return this.tagFieldAddress; }
            set { this.tagFieldAddress = value; }
        }
        private string tagFieldAddress = "";

        /// <summary>
        /// The tag field's Length
        /// </summary>
        public string TagFieldLength
        {
            get { return this.tagFieldLength; }
            set { this.tagFieldLength = value; }
        }
        private string tagFieldLength = "";

        /// <summary>
        /// The tag field's Value
        /// </summary>
        public string TagFieldData
        {
            get { return this.tagFieldData; }
            set { this.tagFieldData = value; }
        }
        private string tagFieldData = "";
    }

}
