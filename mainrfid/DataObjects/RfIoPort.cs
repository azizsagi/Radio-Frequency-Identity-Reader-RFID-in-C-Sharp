using System;
using System.Collections;

namespace Siemens.Simatic.RfReader
{
	/// <summary>
	/// This class gives all information about the IO ports of the reader
	/// </summary>
    public class RfIoPort
    {
        private const int maxOutPorts = 8;
        private const int maxInPorts = 8;

        /// <summary>
        /// Create anew instance of IO Ports
        /// </summary>
        /// <param name="InportCount"> number of Inports </param>
        /// <param name="OutportCount"> number of Outports </param>
        public RfIoPort(int InportCount, int OutportCount)
        {
            // adjust range of Ports
            if (InportCount > maxInPorts)
                InportCount = maxInPorts;
            if (0 > InportCount)
                InportCount = 0;
            if (OutportCount > maxOutPorts)
                OutportCount = maxOutPorts;
            if (0 > OutportCount)
                OutportCount = 0;

            inPortBitArray = new BitArray(InportCount);
            inPortBitArray.SetAll(false);
            outPortBitArray = new BitArray(OutportCount);
            outPortBitArray.SetAll(false);
            containsPorts = (0 != InportCount || 0 !=  OutportCount); 
        }

        /// <summary>
        /// indicate if this object contains any port at all
        /// false = object contains neither input nor output ports
        /// </summary>
        public bool ContainsPorts
        {
            get { return this.containsPorts; }
        }
        private bool containsPorts = true;

        /// <summary>
        /// Stores the single values of each In port
        /// index 0 = Out Port 0
        /// </summary>
        public BitArray InPortBitArray
        {
            get { return this.inPortBitArray; }
            set { this.inPortBitArray = value; }
        }
        private BitArray inPortBitArray;

        /// <summary>
        /// 16 Bit value, representing the values of the IN ports 
        /// Bit 0 = In Port 0
        /// </summary>
        public UInt16 InPortValue
        {
            get 
            {
                UInt16 ioValue = 0;
                for (int i = 0; i < this.inPortBitArray.Count; i++)
                {
                    if (inPortBitArray.Get(i))
                    {
                        ioValue += ((UInt16)(1 << i));
                    }
                }
                return ioValue; 
            }
            set
            {
                for (int i = 0; i < this.inPortBitArray.Count; i++)
                {
                    bool bitValue = false;
                    if (0 < (value & (1 << i)))
                    {
                        bitValue = true;
                    }
                    inPortBitArray.Set(i, bitValue);
                }
            }
        }

        /// <summary>
        /// Stores the single values of each OUT port
        /// index 0 = Out Port 0
        /// </summary>
        public BitArray OutPortBitArray
        {
            get { return this.outPortBitArray; }
            set { this.outPortBitArray = value; }
        }
        private BitArray outPortBitArray;

        /// <summary>
        /// 16 Bit value, representing the values of the OUT ports 
        /// Bit 0 = Out Port 0
        /// </summary>
        public UInt16 OutPortValue
        {
            get 
            {
                UInt16 ioValue = 0;
                for (int i = 0; i < this.outPortBitArray.Count; i++)
                {
                    if (outPortBitArray.Get(i))
                    {
                        ioValue += ((UInt16)(1 << i));
                    }
                }
                return ioValue; 
            }
            set
            {
                for (UInt16 i = 0; i < this.outPortBitArray.Count; i++)
                {
                    bool bitValue = false;
                    if (0 < (value & (1 << i)))
                    {
                        bitValue = true;
                    }
                    outPortBitArray.Set(i, bitValue);
                }
            }
        }
    }

}
