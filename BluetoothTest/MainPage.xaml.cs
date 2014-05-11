///////////////////////////////////PASTE INTO ARDUINO.CC/////////////////////////////////////////
//Setup is simple, and boilerplate:

//#include "foneastrapins.h"
//void setup(void) {
//  //set up the HW UART to communicate with the BT module
//  Serial.begin(38400);

//  // Provide power to BT
//  pinMode(BT_PWR_PIN,OUTPUT);
//  digitalWrite(BT_PWR_PIN,HIGH);

//  // Turn on the red light
//  pinMode(RED_LED_PIN,OUTPUT);
//  digitalWrite(RED_LED_PIN, HIGH);
//}

///////////////////////////////////////////////////////ADDTL CODE/////////////////////////////////

//Programming the Arduino is an exercise in Simplicity
//It’s straight C/C++, and really easy C++ at that!

//Need two functions at minimum:
//setup() and loop()

//Do Input/Output with the Serial object
//    Serial.print(“Hello!”)
//    if( Serial.available() > 0 )
//      char key = Serial.read()

//Output beeps asynchronously with tone()
//    tone( BUZZER_PIN, 1000, 100 );
//    delay( 100 );

//////////////////////////////////////////////////////////////////////////////////////////////////


//////////////////////////////////////////////////////BBT/////////////////////////////////////////
//Once you turn the Bluetooth on, it’s just “available”
//No “initialization” step, other than ensuring that ID_CAP_NETWORKING and ID_CAP_PROXIMITY are both checked

//Use the PeerFinder class to search nearby devices
//Used to find not only other BT devices, but also specific applications
//Operates not only on BT, but also NFC and Wifi
//We’re using only BT, so we’ll make sure to specify that

//Once you’ve got a Peer, open a StreamSocket
//The most basic TCP/Bluetooth communication method
//Sends a continuous stream of data
//This is as opposed to Datagram-based communication (e.g. UDP)


//Working with Data Streams can be tricky
//This idealized “stream” of data is chunked up into packets
//These packets are then sent to the BT radio and transmitted
//This is done transparently by the operating system
//The receiving end is notified at the packet level
//There is no way of knowing how much data to wait for
//Unless we invent a protocol to give us this knowledge!

//Protocols are easy and neat
//Example protocol:
//First 4 bytes of every message is an integer with size of following data
//Next N bytes are the data, where N is the value of the original integer
//Interpreting this data is completely up to your application
//Another protocol:
//Print out data willy-nilly, know it’s done with a newline
//Read it in, appending to a buffer until you read in a newline



//First, we tell PeerFinder to find all paired BT devices
//   PeerFinder.AlternateIdentities["Bluetooth:PAIRED"] = ""

//Next, get a list of all peers that satisfy those criteria:
//   var peers = await PeerFinder.FindAllPeersAsync();

//Find the peer we want:
//   PeerInformation peerInfo = peers.FirstOrDefault(x =>
//                                   x.DisplayName.Contains("FoneAstra"));
//Connect to that peer if not null
//   s = new StreamSocket();
//   await s.ConnectAsync(peerInfo.HostName, "1");

//And now we have a socket connected!


//To send/receive data, we use Data{Writer,Reader}
//We build these out of the StreamSocket
//   DataWriter output = new DataWriter(sock.OutputStream);
//   DataReader input = new DataReader(sock.InputStream);

//To read data in, first Load it, then Read it:
//   await input.LoadAsync(4);
//   input.ReadBytes(byteBuffer);
//   int length = BitConverter.ToInt32(byteBuffer, 0);

//To write data out, Write it then Store it:
//   byte[] data = { 100, 253, 5, 67 };
//   output.WriteBytes(data);
//   output.StoreAsync();


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BluetoothTest.Resources;
using Windows.Networking;
using Windows.Networking.Sockets;
using System.Threading.Tasks;
using Windows.Networking.Proximity;
using Windows.Storage.Streams;
using System.Diagnostics;


namespace BluetoothTest
{
    public partial class MainPage : PhoneApplicationPage
    {
        // This is the bluetooth stream socket that we will communicate over
        private StreamSocket s = null;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            ReadData(SetupBluetoothLink());
        }

        private async Task<bool> SetupBluetoothLink()
        {
            // Tell PeerFinder that we're a pair to anyone that has been paried with us over BT
            PeerFinder.AlternateIdentities["Bluetooth:PAIRED"] = "";

            // Find all peers
            var devices = await PeerFinder.FindAllPeersAsync();

            // If there are no peers, then complain
            if (devices.Count == 0)
            {
                MessageBox.Show("No bluetooth devices are paired, please pair your FoneAstra");
                
                // Neat little line to open the bluetooth settings
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
                return false;
            }

            // Convert peers to array from strange datatype return from PeerFinder.FindAllPeersAsync()
            PeerInformation[] peers = devices.ToArray();

            // Find paired peer that is the FoneAstra
            PeerInformation peerInfo = devices.FirstOrDefault(c => c.DisplayName.Contains("FoneAstra"));

            // If that doesn't exist, complain!
            if (peerInfo == null)
            {
                MessageBox.Show("No paired FoneAstra was found, please pair your FoneAstra");
                await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-bluetooth:"));
                return false;
            }

            // Otherwise, create our StreamSocket and connect it!
            s = new StreamSocket();
            await s.ConnectAsync(peerInfo.HostName, "1");
            return true;
        }

        // Read in bytes one at a time until we get a newline, then return the whole line
        private async Task<string> readLine(DataReader input)
        {
            string line = "";
            char a = ' ';
            // Keep looping as long as we haven't hit a newline OR line is empty
            while ((a != '\n' && a != '\r') || line.Length == 0 )
            {
                // Wait until we have 1 byte available to read
                await input.LoadAsync(1);
                // Read that one byte, typecasting it as a char
                a = (char)input.ReadByte();

                // If the char is a newline or a carriage return, then don't add it on to the line and quit out
                if( a != '\n' && a != '\r' )
                    line += a;
            }

            // Return the string we've built
            return line;
        }

        private async void ReadData(Task<bool> setupOK)
        {
            // Wait for the setup function to finish, when it does, it returns a boolean
            // If the boolean is false, then something failed and we shouldn't attempt to read data
            if (!await setupOK)
                return;

            // Construct a dataReader so we can read junk in
            DataReader input = new DataReader(s.InputStream);

            // Loop forever
            while (true)
            {
                // Read a line from the input, once again using await to translate a "Task<xyz>" to an "xyz"
                string line = (await readLine(input));

                // Append that line to our TextOutput
                this.textOutput.Text += line + "\n";
            }
        }
    }
}