using System;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// This class include information about all of the readers antennas 
	/// </summary>
    public class RfAntennas
    {
        /// <summary>
        /// Create anew instance of Antennas
        /// </summary>
        public RfAntennas()
        {
            rfAntenna1.name = "Antenna01";
            rfAntenna2.name = "Antenna02";
            rfAntenna3.name = "Antenna03";
            rfAntenna4.name = "Antenna04";
            RfAntennaArray[0] = rfAntenna1;
            RfAntennaArray[1] = rfAntenna2;
            RfAntennaArray[2] = rfAntenna3;
            RfAntennaArray[3] = rfAntenna4;
        }
        
        public RfAntenna[] RfAntennaArray = new RfAntenna[4];
        /// <summary>
        /// Information of antenna 1
        /// </summary>
        public RfAntenna RfAntenna1
        {
            get { return this.rfAntenna1; }
        }
        private RfAntenna rfAntenna1 = new RfAntenna();
        
        /// <summary>
        /// Information of antenna 2
        /// </summary>
        public RfAntenna RfAntenna2
        {
            get { return this.rfAntenna2; }
        }
        private RfAntenna rfAntenna2 = new RfAntenna();

        /// <summary>
        /// Information of antenna 3
        /// </summary>
        public RfAntenna RfAntenna3
        {
            get { return this.rfAntenna3; }
        }
        private RfAntenna rfAntenna3 = new RfAntenna();

        /// <summary>
        /// Information of antenna 4
        /// </summary>
        public RfAntenna RfAntenna4
        {
            get { return this.rfAntenna4; }
        }
        private RfAntenna rfAntenna4 = new RfAntenna();
    }
        
    /// <summary>
	/// This class stores information about a single reader antenna 
	/// </summary>
    public class RfAntenna
    {
         /// <summary>
        /// Create anew instance of a single Antenna
        /// </summary>
        public RfAntenna()
        {
        }
        /// <summary>
        /// The antenna name
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }
        internal string name = "";

        /// <summary>
        /// The antenna power in dbm
        /// </summary>
        public UInt16 Power
        {
            get { return this.power; }
            set { this.power = value; }
        }
        private UInt16 power = 0;

        /// <summary>
        /// The cable loss of the antenna in dbm
        /// </summary>
        public float CableLoss
        {
            get { return this.cableLoss; }
            set { this.cableLoss = value; }
        }
        private float cableLoss = 0;

        /// <summary>
        /// The antenna gain in dbm
        /// </summary>
        public float Gain
        {
            get { return this.gain; }
            set { this.gain = value; }
        }
        private float gain = 0;

        /// <summary>
        /// The RSSI threshold of the antenna in dbm
        /// </summary>
        public UInt16 RSSIThreshold
        {
            get { return this.rssiThreshold; }
            set { this.rssiThreshold = value; }
        }
        private UInt16 rssiThreshold = 0;

    }
}
