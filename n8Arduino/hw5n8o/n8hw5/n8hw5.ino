#include "foneastrapins.h"
#include <SoftwareSerial.h>

// Creates a SoftwareSerial so you can talk to the 
SoftwareSerial swSerial(SW_UART_RX_PIN, SW_UART_TX_PIN); // RX, TX

void setup(void) {
  //set up the HW UART to communicate with the BT module
  Serial.begin(38400);
  swSerial.begin(38400);

  // Provide power to BT
  pinMode(BT_PWR_PIN,OUTPUT);
  digitalWrite(BT_PWR_PIN,HIGH);

  // Turn on the red light
  pinMode(RED_LED_PIN,OUTPUT);
  digitalWrite(RED_LED_PIN, HIGH);
}


void printBattery(void) {
  // Get the battery level from a builtin ADC
  unsigned short battLvlADC = analogRead(BATT_LVL_PIN);
  
  // Convert that ADC value to a rough estimation of volt
  float battLvlVoltage = 0.0032 * battLvlADC;
  float key;
  
  // Print out the raw ADC value, and the voltage, followed by a Newline
  Serial.print("raw batt lvl: ");
  Serial.print(battLvlADC); 
  Serial.print(" voltage at BATT_LVL: ");
  Serial.print(battLvlVoltage);
  Serial.println();
  
  // Do the same for the swSerial, connected to the PC
  swSerial.print("raw batt lvl: ");
  swSerial.print(battLvlADC); 
  swSerial.print(" voltage at BATT_LVL: ");
  swSerial.print(battLvlVoltage);
  swSerial.println();
  
  
  if( Serial.available() > 0 ){
  Serial.print("\n Serial Available! \n");
  key = Serial.read();
  swSerial.print(key);
  Serial.print(key);
  }

}

void buzzdot(void){
  //buzz for a short time
  tone( BUZZER_PIN, 500, 100 );
  delay( 100 );
}

void buzzdash(void){
  //buzz for a short time
  tone( BUZZER_PIN, 500, 500 );
  delay( 100 );
}

void morse(void){
  //char myChar;
  if(Serial.available() > 0){
   //Citation: http://arduino.cc/en/Reference/sizeof
   //Citation: http://arduino.cc/en/Reference/StreamRead
      
   for(int i=0; i < /*sizeof(myChar)*/Serial.available(); i++){
     char myChar = Serial.read();
     swSerial.print(myChar);
     Serial.print(myChar);
     if(myChar == '.'){
       buzzdot();
     }else if(myChar == '-'){
       buzzdash();
       }else if(myChar == ' '){
         delay(1200);
       }else{
         Serial.print(myChar);
         Serial.print(" is unknown character, please use dot . or dash - only \n");
         swSerial.print(myChar);
         swSerial.print(" is unknown character, please use dot . or dash - only \n");
         swSerial.println();
       }
       delay(600);
     }
   }
 }

void loop(void) {
  morse();
}


