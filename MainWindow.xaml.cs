using System;
using System.Windows;
using System.IO.Ports;
using System.Text;
using System.Windows.Controls;

namespace Solar
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// Hardware Setup: A solar-panel (with a diode in series with it) is used to power 2 active-low LEDs 
    /// (which perform A to D conversions). A voltage is taken from the supercapacitor, used as a battery to power the circuit.
    /// 
    /// This code simluates a solar-power plant, which displays the solar-panel and battery voltage, battery and LED currents. 
    /// Raw data is sent and received from the serial port, via an Arduino running a Serial Transfer Protocol.
    /// Values can be sent to the Arduino in packet or bit form. 
    /// As the LEDs are active low, sending a packet with two 0s in the position of the first-two bits will turn the LEDs on.
    /// 
    /// </summary>
    
    public partial class MainWindow : Window
    {

        // A boolean used to store the port's status 
        private bool PortOpen = false;
        private string package;

        public int checksumError = 0;
        // Variables to track whether a packet is lost
        private int prevPacketNum = -1;     
        private int newPacketNum = 0;
        private int lostPacketCount = 0;
        private int packetRollOver = 0;

        // StringBuilder creates an imutable string, which can't be changed 
        // The binary values shown in the following string are ASCII values, 0 is (0x30)
        // 111100 = 292; packet math = (49 * 4) + (2 * 48) = 292
        // The Checksum value for sending only 0s is 288 - ###000000288
        private StringBuilder stringBuilderSend = new StringBuilder("###111100292");

        SerialPort serialPort = new SerialPort();
        SolarCalc solarCalc = new SolarCalc();

        public StringBuilder StringBuilderSend { get => stringBuilderSend; set => stringBuilderSend = value; }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // creating an array of strings with the port names
            string[] ports = SerialPort.GetPortNames();
            serialPort.BaudRate = 115200;                        // setting the BaudRate based on the Arduino's BaudRate 
            serialPort.ReceivedBytesThreshold = 1;               // triggered after a byte is received
            serialPort.DataReceived += SerialPort_DataReceived;  // event handler which stores received data

            foreach (string port in ports)                       // ports is an array of strings
            {
                Combo_Box.Items.Add(port);                       // each port is added to the Combo_Box
            }
            Combo_Box.SelectedIndex = 0;                         // can be set to a specific value for debugging purposes
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                package = serialPort.ReadLine();

                //Txt_Received.Text = package;                // Serial port is on a different thread than the UI
                if (Txt_Received.Dispatcher.CheckAccess())    // Txt_Received has an associated Dispatcher due to threading
                {
                    // if it can be accessed, package is sent to UpdateUI method to update the thread
                    UpdateUI(package);
                }
                else
                {
                    // invokes access to this UI and then Accesses the method
                    Txt_Received.Dispatcher.Invoke(() => { UpdateUI(package); });
                }
            }
            // times out if the Arduino isn't sending code or if code is sent at the wrong Baud rate 
            catch (TimeoutException) { }
        }

        private void UpdateUI(string packet)
        {
            // A check-box is used to display the raw-data or eliminate it on-screen
            if (checkBoxHistory.IsChecked == true)
            {

                Txt_Received.Text = packet + Txt_Received.Text;

            }
            else
            {
                Txt_Received.Text = packet;
            }
            packetLength.Text = packet.Length.ToString();       // converts the packet to a string
           
            int calcChecksum = 0;                               // the checksum is calculated based on the bytes of data received

            if (packet.Length > 41)                             // has the whole packet been received?
            {
                    // if a header is not received, a bad packet has been received
                    if (packet.Substring(0, 3) == "###")
                    {   
                        // searches for values after the third character and limits values to three characters following that
                        packetNum.Text = packet.Substring(3, 3);    // index increments the amount necessary for the packet, from the third value for three values
                        newPacketNum = Convert.ToInt32(packetNum.Text);
                        Analog0.Text = packet.Substring(6, 4);      // parses from the sixth value for four values
                        Analog1.Text = packet.Substring(10, 4);     // each analog output parses 4 values from the value needed
                        Analog2.Text = packet.Substring(14, 4);
                        Analog3.Text = packet.Substring(18, 4);
                        Analog4.Text = packet.Substring(22, 4);
                        Analog5.Text = packet.Substring(26, 4);
                        binaryNum.Text = packet.Substring(30, 8);    // eight values long, begins at position 30
                        RecChkSum.Text = packet.Substring(38, 3);    // three bytes long, begins at position 38
                                                                     // examines the changing packet values and adds them together
                        for (int i = 3; i < 38; i++)
                        {
                            calcChecksum += (byte)packet[i];         // ints are cast to a byte
                        }
                        CalcChksum.Text = Convert.ToString(value: calcChecksum);
                        calcChecksum %= 1000;                        // truncates the checksum to a three-digit value

                        int recChecksum = Convert.ToInt32(packet.Substring(38, 3)); // parses for the received checksum
                        if (recChecksum == calcChecksum)
                        {
                            DisplaySolarData(packet);
                        }
                        else
                        {
                        checksumError++;                           // keeps track of how many checksum errors occur
                        txtChecksumError.Text = checksumError.ToString();
                        }
                        if (prevPacketNum > -1)
                        { 
                            packetRollOver++;
                            packetRollover.Text = packetRollOver.ToString();
                        if (prevPacketNum != 999)                 // if the old packet does not equal 999, an error has occured
                        {
                            lostPacketCount++;                   // displays how many packets are lost
                            lostPacket.Text = lostPacketCount.ToString();
                        }
                        } // end of if (prevPacketNum > -1)
                    } // end of if (packet.Substring)
                    prevPacketNum = newPacketNum;
            }   // end of if (packetLength)
        } 
        
        private void DisplaySolarData(string validPacket)
        {
            // valid packet data is taken from the analog ports, converted to an integer, and parsed based on position placement
            int AN0 = Convert.ToInt32(validPacket.Substring(6, 4));
            int AN1 = Convert.ToInt32(validPacket.Substring(10, 4));
            int AN2 = Convert.ToInt32(validPacket.Substring(14, 4));
            int AN3 = Convert.ToInt32(validPacket.Substring(18, 4));
            int AN4 = Convert.ToInt32(validPacket.Substring(22, 4));

            // values are changed based on calculations performed in the SolarCalc class
            Solartxt.Text = solarCalc.GetSolarVoltage(AN0);
            BatteryVolt.Text = solarCalc.GetBatteryVoltage(AN2);
            BattCurr.Text = solarCalc.GetBatteryCurrent(AN1, AN2);
            LED1Curr.Text = solarCalc.GetLEDCurrent(AN1, AN4);
            LED2Curr.Text = solarCalc.GetLEDCurrent(AN1, AN3);
        }

        private void Btn_Open_Close_Click(object sender, RoutedEventArgs e)
        {
            // when the code first runs, the port is not open, so the following will execute:
            if (!PortOpen)
            {
                serialPort.PortName = Combo_Box.Text;   // the port name is taken from the Combo_Box
                serialPort.Open();
                Btn_Open_Close.Content = "Close";       // the port is now open, button status is updated
                PortOpen = true;
            }
            else
            {
                serialPort.Close();
                Btn_Open_Close.Content = "Open";        // the port is closed, button status is updated
                PortOpen = false;
            }
        }
        // Btn_Clear_Click resets the output
        private void Btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            Txt_Received.Text = "";
        }
        //  SendBtn_Click operates as a method to send the packet
        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            SendPacket();
        }

        private void SendPacket()
        {
            try
            {   
                // the transmit checksum 
                int txChksum = 0;                  
                // calculates the value of the checksum (starts at 3 to skip ###) by adding each value in the textbox
                for (int i = 3; i < 9; i++)
                {   // casts the textbox values to a byte (as it may be in unicode 16 form)
                    txChksum += (byte)StringBuilderSend[i];
                }
                // truncates the checksum, sends 3 digits
                txChksum %= 1000;                          
                stringBuilderSend.Remove(9, 3);                             // 3 bytes are removed at index 9
                stringBuilderSend.Insert(9, txChksum.ToString());           // a checksum is inserted at index 9, converted to a string, and inserted in the string builder
                Txt_Send.Text = stringBuilderSend.ToString();               // the contents of our text-box are converted to a string string builder

                string messageOut = Txt_Send.Text;                          // sending the item in the textbox
                messageOut += "\r\n";                                       // adds a return line-feed to the string
                // sends a byte array, message is encoded in Unicode 8 which ensures that we are only working with bytes
                byte[] messageBytes = Encoding.UTF8.GetBytes(messageOut);   
                // sends the packet to the serial port (buffer, offset or starting point, and length of message)
                serialPort.Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);                                // if the port is closed a message-box appears
            }

        }

        // When the bit buttons are clicked, they reference the ButtonClicked method, their index is passed to it.
        private void BtnBit0_Click(object sender, RoutedEventArgs e)
        {
            ButtonClicked(0);

            // An image of the sun appears when the LED is turned on for ports 0 and 1
            if (btnBit0.Content.ToString() == "1")
            {
                Sunshine.Visibility = Visibility.Collapsed;
            }
            else
            {
                Sunshine.Visibility = Visibility.Visible;
            }
        }

        private void BtnBit1_Click(object sender, RoutedEventArgs e)
        {
            ButtonClicked(1);

            if (btnBit1.Content.ToString() == "1")
            {
                Sunshine.Visibility = Visibility.Collapsed;
            }
            else
            {
                Sunshine.Visibility = Visibility.Visible;
            }
        }

        private void BtnBit2_Click(object sender, RoutedEventArgs e)
        {
            ButtonClicked(2);
        }

        private void BtnBit3_Click(object sender, RoutedEventArgs e)
        {
            ButtonClicked(3);
        }

        private void BtnBit4_Click(object sender, RoutedEventArgs e)
        {
            ButtonClicked(4);
        }

        private void BtnBit5_Click(object sender, RoutedEventArgs e)
        {
            ButtonClicked(5);
        }

        private void ButtonClicked(int i)
        {
            // Creating a button array to group the buttons together
            Button[] btnBit = new Button[] { btnBit0, btnBit1, btnBit2, btnBit3, btnBit4, btnBit5 };
            if (btnBit[i].Content.ToString() == "0")        // if the content of the button is a 0, execute the below code 
            {                                               // the button content is used as a Boolean to toggle the value
                btnBit[i].Content = "1";
                stringBuilderSend[i + 3] = '1';             // removes one bit of string in stringBuilderSend at (index + 3), then replaces this char
            }
            else
            {
                btnBit[i].Content = "0";
                stringBuilderSend[i + 3] = '0';
            }
            SendPacket();                                   // SendPacket is called each time a button is clicked
        } // end of ButtonClicked();
    } // end of partial class Main Window : Window
} // end of namespace 
