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



////First, we tell PeerFinder to find all paired BT devices
////   PeerFinder.AlternateIdentities["Bluetooth:PAIRED"] = ""

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

//Use Arduino Pro (3.3V 8MHz) w/ ATmega328

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

        /*
        private async void WriteData(Task<bool> setupOK)
        {

        }
        */

        private async void ReadData(Task<bool> setupOK)
        {
            // Wait for the setup function to finish, when it does, it returns a boolean
            // If the boolean is false, then something failed and we shouldn't attempt to read data
            if (!await setupOK)
                return;

            // Construct a dataReader so we can read junk in
            DataReader input = new DataReader(s.InputStream);

            DataWriter output = new DataWriter(s.OutputStream);

            
            this.n8button.Click += async (object sender, RoutedEventArgs e) =>
                {
                   // String myString3;
                    //output.WriteString(this.n8textbox.Text);
                    

                    
                    String myString = this.n8textbox.Text;
                    String myString2 = "";// = new String "";
                    //Array myChar = new char[1];
                    //char[myString.Length] myArray;= myString.ToCharArray;
                    //Array myArray = myString.ToArray();
                    //myString.
                    for (int i = 0; i < myString.Length; i++)
                    {
                        String myString3 = myString.Substring(i,1);//CopyTo(i,myChar[0],1,1);
                        //string myString2 = (string)myArray.GetValue(i);
                        //citation: http://www.google.com/imgres?imgurl=&imgrefurl=http%3A%2F%2Fcommons.wikimedia.org%2Fwiki%2FFile%3AInternational_Morse_Code.svg&h=0&w=0&tbnid=UGJ6vAUjTb5jQM&zoom=1&tbnh=255&tbnw=198&docid=o7ACXGZOzqYIBM&tbm=isch&ei=b0dxU9KPEIr9oASg1oD4CQ&ved=0CAUQsCUoAQ
                        //citation: http://msdn.microsoft.com/en-us/library/06tc147t.aspx
                        switch (myString3)
                        {
                            case " ":
                                myString2 += " " + " "; // add a pause + " " between characters
                                break;
                            case ".":
                                myString2 += "." + " ";
                                break;
                            case "-":
                                myString2 += "-" + " ";
                                break;
                            case "a":
                            case "A":
                                myString2 = ".-" + " ";
                                break;
                            case "b":
                            case "B":
                                myString2 += "-..." + " ";
                                break;
                            case "c":
                            case "C":
                                myString2 += "-.-." + " ";
                                break;
                            case "d":
                            case "D":
                                myString2 += "-.." + " ";
                                break;
                            case "e":
                            case "E":
                                myString2 += "." + " ";
                                break;
                            case "f":
                            case "F":
                                myString2 += "..-." + " ";
                                break;
                            case "g":
                            case "G":
                                myString2 += "--." + " ";
                                break;
                            case "h":
                            case "H":
                                myString2 += "...." + " ";
                                break;
                            case "i":
                            case "I":
                                myString2 += ".." + " ";
                                break;
                            case "j":
                            case "J":
                                myString2 += ".---" + " ";
                                break;
                            case "k":
                            case "K":
                                myString2 += "-.-" + " ";
                                break;
                            case "l":
                            case "L":
                                myString2 += ".-.." + " ";
                                break;
                            case "m":
                            case "M":
                                myString2 += "--" + " ";
                                break;
                            case "n":
                            case "N":
                                myString2 += "-." + " ";
                                break;
                            case "o":
                            case "O":
                                myString2 += "---" + " ";
                                break;
                            case "p":
                            case "P":
                                myString2 += ".--." + " ";
                                break;
                            case "q":
                            case "Q":
                                myString2 += "--.-" + " ";
                                break;
                            case "r":
                            case "R":
                                myString2 += ".-." + " ";
                                break;
                            case "s":
                            case "S":
                                myString2 += "..." + " ";
                                break;
                            case "t":
                            case "T":
                                myString2 += "-" + " ";
                                break;
                            case "u":
                            case "U":
                                myString2 += "..-" + " ";
                                break;
                            case "v":
                            case "V":
                                myString2 += "...-" + " ";
                                break;
                            case "w":
                            case "W":
                                myString2 += ".--" + " ";
                                break;
                            case "x":
                            case "X":
                                myString2 += "-..-" + " ";
                                break;
                            case "y":
                            case "Y":
                                myString2 += "-.--" + " ";
                                break;
                            case "z":
                            case "Z":
                                myString2 += "--.." + " ";
                                break;
                            case "1":
                                myString2 += ".----" + " ";
                                break;
                            case "2":
                                myString2 += "..---" + " ";
                                break;
                            case "3":
                                myString2 += "...--" + " ";
                                break;
                            case "4":
                                myString2 += "....-" + " ";
                                break;
                            case "5":
                                myString2 += "....." + " ";
                                break;
                            case "6":
                                myString2 += "-...." + " ";
                                break;
                            case "7":
                                myString2 += "--..." + " ";
                                break;
                            case "8":
                                myString2 += "---.." + " ";
                                break;
                            case "9":
                                myString2 += "----." + " ";
                                break;
                            case "10":
                                myString2 += "-----" + " ";
                                break;
                            default:
                                //Console.WriteLine("Default case");
                                break;
                        }

                    }
                    output.WriteString(myString2);
                    await output.StoreAsync();
                    this.n8textbox.Text = "";
                    await Task.Delay(1500);
                };

            

            // Loop forever
            while (true)
            {
                // Read a line from the input, once again using await to translate a "Task<xyz>" to an "xyz"
                string line = (await readLine(input));

                // Append that line to our TextOutput
                this.textOutput.Text += line + "\n";
            }
        }

        private void n8textbox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        /*
        public bool n8button_Click { get; set;
            //if(n8button_Click == true)
            //{

            //}
        }*/
    }
}