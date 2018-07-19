using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using Siemens.Simatic.RfReader.ReaderApi;
using Siemens.Simatic.RfReader;
using System.IO;
using System.Threading;

namespace MainRFID
{
    public partial class Form1 : Form
    {
        private string source = "Source_1";
        private string deviceType = "SIMATIC_RF670R";
        private string tagID = "300833B2DDD9014035050005";
        private string tagData = "0";
        private string tagDataMask = "0";
        private string tagField = String.Empty;
        private uint tagAddress = 0;
        private uint tagBank = 3;
        private uint tagLength = 2;
        private string tagPassword = String.Empty;
        

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

            // Now access the RFReader API and start a new device
            RfReaderInitData initData = new RfReaderInitData();
            
            initData.Address = ipAddress.Text;
            initData.Port = Convert.ToInt32(port.Text);
            initData.AckData = true;

           try
           {
               RfReaderApi.Current.StartTcpIPConnection(initData);
               result.Text = "Connection successful...\r\n";
               

           }
           catch (RfReaderApiException rfidException)
           {
               result.Text = "ERROR: {0} - {1}, cause: {2}\r\n"+rfidException.ResultCode + " "+ rfidException.Error + " " +rfidException.Cause;
           }

         

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string[] sources = null;
            // Call API
            try
            {
                sources = RfReaderApi.Current.GetAllSources();
                result.Text += "OK: getAllSources\r\n";
            }
            catch (RfReaderApiException rfidException)
            {
                result.Text = string.Format("ERROR: {0} - {1}, cause: {2}\r\n",
                    rfidException.ResultCode, rfidException.Error,
                    rfidException.Cause);
            }
            // Display sources
            if (null != sources)
            {
                source = sources[0];
                result.Text += " > " + sources.Length.ToString() + " sources received\r\n";
               
                for (int index = 0; index < sources.Length; index++)
                {
                    result.Text += "  >" + sources[index] + "\r\n";
                }

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Call API
            try
            {
                string[] versions = new string[3];
                versions[0] = "V1.0";
                // At the moment we have only one valid version. 
                // Further versions are dummies and needed here for testing purposes only.
                versions[1] = String.Empty;
                versions[2] = "V5.w34";


               
                
                    
                    RfConfigID rfConfigID = RfReaderApi.Current.HostGreetings("SIMATIC_RF670R", ref versions,"Default");
                    // Display result status
                    result.Text += string.Format("OK: HostGreetings readerMode: {0}\r\n","Default");
                    result.Text += string.Format(" > version: {0}, configType: {1}, configID: {2}\r\n",
                                        versions[0], rfConfigID.ConfigType, rfConfigID.ConfigID);
                    deviceType = "SIMATIC_RF670R";
                
            }
            catch (RfReaderApiException rfidException)
            {
                result.Text += string.Format("ERROR: {0} - {1}, cause: {2}\r\n",
                    rfidException.ResultCode, rfidException.Error,
                    rfidException.Cause);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            RfTag[] tagIDs = null;

            try
            {

                
                uint duration = 0;

                tagIDs = RfReaderApi.Current.ReadTagIDs(source, duration);
                result.Text += string.Format(" OK: Get Tag IDs source = {0} duration ={1}\r\n",source, 0);

            }
            catch (RfReaderApiException rfidException)
            {
                result.Text += string.Format("ERROR: {0} - {1}, cause: {2}\r\n",
                    rfidException.ResultCode, rfidException.Error,
                    rfidException.Cause);
                return;
            }


            if (null != tagIDs && 0 < tagIDs.Length)
            {
                tagID = tagIDs[1].TagID;
            }

            Queue<string> displayTagData = DisplayTagResult(tagIDs);
            while (0 < displayTagData.Count)
            {
                result.Text += displayTagData.Dequeue();
            }
        }


        private Queue<string> DisplayTagResult(RfTag[] tagIDs)
        {
            Queue<string> displayOutputQueue = new Queue<string>();

            if (null != tagIDs && tagIDs.Length > 0)
            {
                for (int index = 0; index < tagIDs.Length; index++)
                {
                    bool showTagAttrib = false;

                    // TagID
                    displayOutputQueue.Enqueue(string.Format(" > tagID: {0}\r\n", tagIDs[index].TagID));

                    // Success Flag
                    string showSuccessFlag = String.Empty;
                    // show only if success = false
                    if (!tagIDs[index].SuccessFlag)
                    {
                        showSuccessFlag = string.Format("success: {0}, ", tagIDs[index].SuccessFlag.ToString());
                        showTagAttrib = true;
                    }

                    // Timestamp
                    string showTimeStamp = String.Empty;
                    if (DateTime.MinValue != tagIDs[index].TagTimeStamp)
                    {
                        showTimeStamp = string.Format("time: {0}, ", tagIDs[index].TagTimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"));
                        showTagAttrib = true;
                    }

                    // Antenna
                    string showAntenna = String.Empty;
                    if (String.Empty != tagIDs[index].TagAntenna)
                    {
                        showAntenna = string.Format("antenna: {0}, ", tagIDs[index].TagAntenna);
                        showTagAttrib = true;
                    }

                    // RSSI
                    string showRSSI = String.Empty;
                    if (String.Empty != tagIDs[index].TagRSSI)
                    {
                        showRSSI = string.Format("rssi: {0}, ", tagIDs[index].TagRSSI);
                        showTagAttrib = true;
                    }

                    // Event
                    string showEvent = String.Empty;
                    if (String.Empty != tagIDs[index].TagEvent)
                    {
                        showEvent = string.Format("event: {0}, ", tagIDs[index].TagEvent);
                        showTagAttrib = true;
                    }

                    // Show attributes of TagID

                    if (showTagAttrib)
                    {
                        displayOutputQueue.Enqueue(string.Format("      {0}{1}{2}{3}{4}\r\n", showSuccessFlag, showEvent, showTimeStamp, showAntenna, showRSSI));
                    }

                    // TagFields
                    if (null != tagIDs[index].TagFields && 0 < tagIDs[index].TagFields.Length)
                    {
                        for (int J = 0; J < tagIDs[index].TagFields.Length; J++)
                        {
                            bool showFieldData = true;

                            // Field Data
                            string showData = "data: ----- ";
                            if (String.Empty != tagIDs[index].TagFields[J].TagFieldData)
                            {
                                showData = string.Format("data: {0}, ", tagIDs[index].TagFields[J].TagFieldData);
                                tagData = tagIDs[0].TagFields[0].TagFieldData;
                            }


                            // Field Name
                            string showName = String.Empty;
                            if (String.Empty != tagIDs[index].TagFields[J].TagFieldName)
                            {
                                showName = string.Format("fieldName: {0}, ", tagIDs[index].TagFields[J].TagFieldName);
                                tagField = tagIDs[0].TagFields[0].TagFieldName;
                                showFieldData = true;
                            }

                            // Field Address
                            string showAddress = String.Empty;
                            if (String.Empty != tagIDs[index].TagFields[J].TagFieldAddress)
                            {
                                showAddress = string.Format("address: {0}, ", tagIDs[index].TagFields[J].TagFieldAddress);
                                try
                                {
                                    tagAddress = Convert.ToUInt32(tagIDs[0].TagFields[0].TagFieldAddress, 10);
                                }
                                catch (Exception)
                                {
                                    // invalid address, reset global variable to zero
                                    tagAddress = 0;
                                }

                                showFieldData = true;
                            }

                            // Field Bank
                            string showBank = String.Empty;
                            if (String.Empty != tagIDs[index].TagFields[J].TagFieldBank
                                && "-1" != tagIDs[index].TagFields[J].TagFieldBank)
                            {

                                showBank = string.Format("bank: {0}, ", tagIDs[index].TagFields[J].TagFieldBank);
                                try
                                {
                                    tagBank = Convert.ToUInt32(tagIDs[0].TagFields[0].TagFieldBank, 10);
                                }
                                catch (Exception)
                                {
                                    // invalid tagBank. reset global variable to zero
                                    tagBank = 0;
                                }

                                showFieldData = true;
                            }

                            // Field Length
                            string showLength = String.Empty;
                            if (String.Empty != tagIDs[index].TagFields[J].TagFieldLength)
                            {
                                showLength = string.Format("length: {0}, ", tagIDs[index].TagFields[J].TagFieldLength);
                                try
                                {
                                    tagLength = Convert.ToUInt32(tagIDs[0].TagFields[0].TagFieldLength, 10);
                                }
                                catch (Exception)
                                {

                                    tagLength = 0;
                                }
                                // invalid tagBank. reset global variable to zero
                                showFieldData = true;
                            }
                            // Show Field Data
                            if (showFieldData)
                            {
                                displayOutputQueue.Enqueue(string.Format("   >> {0}{1}{2}{3}{4}\r\n", showData, showBank, showAddress, showLength, showName));
                            }
                        }
                    }
                }
            }
            else
            {
                displayOutputQueue.Enqueue(" > No Tag\r\n");
            }
            return (displayOutputQueue);
        }

    }
}
