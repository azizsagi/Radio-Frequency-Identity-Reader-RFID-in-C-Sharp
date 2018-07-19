using System;

namespace Siemens.Simatic.RfReader
{
    /// <summary>
    /// This class include information about a single tag
    /// </summary>
    public class RfTag
    {
        /// <summary>
        /// Create anew instance of a Tag
        /// </summary>
        public RfTag()
        {
        }

     #region Property
        /// <summary>
        /// The tagID
        /// </summary>
        public string TagID
        {
            get { return tagID; }
            set { tagID = value; }
        }
        private string tagID = "";

        /// <summary>
        /// The success flag
        /// </summary>
        public bool SuccessFlag
        {
            get { return successFlag; }
            set { successFlag = value; }
        }
        private bool successFlag = true;


        /// <summary>
        /// The event type 
        /// </summary>
        public string TagEvent
        {
            get { return tagEvent; }
            set { tagEvent = value; }
        }
        private string tagEvent = "";

        /// <summary>
        /// The time stamp in UTC
        /// </summary>
        public DateTime TagTimeStamp
        {
            get { return tagTimeStamp; }
            set { tagTimeStamp = value; }
        }
        private DateTime tagTimeStamp = DateTime.MinValue;

        /// <summary>
        /// The antenna from which the tag was written
        /// </summary>
        public string TagAntenna
        {
            get { return tagAntenna; }
            set { tagAntenna = value; }
        }
        private string tagAntenna = "";

        /// <summary>
        /// the RSSI value of the tag event
        /// </summary>
        public string TagRSSI
        {
            get { return tagRSSI; }
            set { tagRSSI = value; }
        }
        private string tagRSSI = "";

        /// <summary>
        /// The tagfields of the tag
        /// </summary>
        public RfTagField[] TagFields
        {
            get { return tagFields; }
            set { tagFields = value; }
        }
        private RfTagField[] tagFields = null;

     #endregion Property

    }
}
