using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using KMHelper;
using System;

public class ModBusScript : MonoBehaviour
{

    public class ModSettingsJSON
    {
        public int countdownTime;
        public string note;
    }

    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Info;
    public KMModSettings modSettings;
    public KMSelectable[] btn;
    public KMSelectable btnSend;
    public TextMesh Screen;

    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;
    private bool _isSolved = false, _lightsOn = false;
    //_isSolved = true; at the end of module
    //_lightsOn = true; in Activate()

    private string correctAns = ""; //Answer correct.
    private string yourAns = ""; //Variable answer modify by you
    private string screen_text = "__.__.__.__.__.__";

    // Use this for initialization
    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
    }

    private void Activate()
    {
        Init();
        _lightsOn = true;
    }

    private void Init()
    {
        Debug.LogFormat("[ ModBus #{0} ]", _moduleId);

        // Find correct answer
        Find_Correct_Answer();
    }

    private void Find_Correct_Answer()
    {
        // Slave ID
        Find_Correct_SlaveId();

        // Function
        Find_Correct_Function();

        // If function 04
        if(correctAns[3] == '4'){
            Debug.LogFormat("[ ModBus #{0} - Find_Correct_Answer - function 4 detected]", _moduleId);

            // Address function 04
            Find_Correct_Function_04_Address();

            // Number of word to read function 04
            Find_Correct_Function_04_Number_of_Word();

        } else {  // If function 06
            Debug.LogFormat("[ ModBus #{0} - Find_Correct_Answer - function 6 detected]", _moduleId);

            // Address function 06
            Find_Correct_Function_06_Address();

            // Value function 06
            Find_Correct_Function_06_Value();
        }
        Debug.LogFormat("[ ModBus #{0} - Find_Correct_Answer - Correct Answer is {1}]", _moduleId, correctAns);
    }

    // For SlaveID :
    // - Convert 1st number/letter from ASCII to hexadecimal
    private void Find_Correct_SlaveId()
    {
        // Convert 1st digit to decimal
        int Digit1 = Info.GetSerialNumber().ElementAt(0);

        // Convert decimal to string hexa
        string slaveId = Digit1.ToString("X2");
            
        correctAns = slaveId;
        Debug.LogFormat("[ ModBus #{0} - Find_Correct_SlaveId - first digit : {1} - slaveId : {2}]", _moduleId, Digit1, slaveId);
    }

    // For Function :
    // - Convert 6th digit from ASCII to decimal
    // - Take the rest of modulo 3
    // - Multiply by 7
    // - Take modulo 2
    // - if 0 -> 06, if 1 -> 04
    private void Find_Correct_Function()
    {
        // Convert 6th digit from ASCII to decimal
        int Digit6 = Info.GetSerialNumber().ElementAt(5);
        string function = "";

        // Take modulo 3
        int temp = Digit6 % 3;

        // Multiply by 7
        temp *= 7;

        // Take modulo 2
        temp %= 3;

        // if 0 -> 06, if 1 -> 04
        if(temp == 0){
            function = "06";
        } else {
            function = "04";
        }
        correctAns += function;
        Debug.LogFormat("[ ModBus #{0} - Find_Correct_Function - 6th digit : {1} - function : {2}]", _moduleId, Digit6, function);
    }

    // For Function 04 - address:
    // - take 3rd digit
    // - if it's a letter, convert it from ASCII to decimal
    // - take 4th digit
    // - if it's a letter, convert it from ASCII to decimal
    // - Multiply the 2 previous number
    // - Convert to hexa
    private void Find_Correct_Function_04_Address()
    {
        // take 3rd digit
        int Digit3 = Info.GetSerialNumber().ElementAt(2);

        // if it's a number, the conversion from ascii is not automatic
        if(Digit3 >= '0' && Digit3 <= '9')
        {
            // Soustract by ASCII code of 0, 48
            Digit3 -= 48;
        }

        // take 4th digit
        int Digit4 = Info.GetSerialNumber().ElementAt(3);

        // if it's a number, the conversion from ascii is not automatic
        if (Digit4 >= '0' && Digit4 <= '9')
        {
            // Soustract by ASCII code of 0, 48
            Digit4 -= 48;
        }

        // Multiply the 2 previous number
        int Result_Mult = Digit3 * Digit4;

        // Convert to hexa
        string address = Result_Mult.ToString("X4");

        correctAns += address;
        Debug.LogFormat("[ ModBus #{0} - Find_Correct_Function_04_Address - digit 3 : {1} - digit 4 : {2} - address : {3}]", _moduleId, Digit3, Digit4, address);

    }

    // For Function 04 - number of word:
    // - take 5th digit
    // - Convert to ASCII to binary
    // - Shift 6 times to the left, it's new 5th digit
    // - take 6th digit
    // - Convert to ASCII to binary
    // - Make OR Bitwise with new 5th and 6th
    private void Find_Correct_Function_04_Number_of_Word()
    {
        // take 5th digit
        int Digit5 = Info.GetSerialNumber().ElementAt(4);

        // Shift 6 times to the left, it's new 5th digit
        Digit5 <<= 6;

        // take 6th digit
        int Digit6 = Info.GetSerialNumber().ElementAt(5);

        // Make OR Bitwise with new 5th and 6th
        int NumberOfWord = Digit5 | Digit6;

        string str_NumberOfWord = NumberOfWord.ToString("X4");
        correctAns += str_NumberOfWord;
        Debug.LogFormat("[ ModBus #{0} - Find_Correct_Function_04_Number_of_Word - digit 5 : {1} - digit 6 : {2} - number of word : {3}]", _moduleId, Digit5, Digit6, str_NumberOfWord);
    }

    // For Function 06 - address:
    // - Take 4th digit
    // - Convert from ASCII to binary
    // - Make not in 16 bits
    // - Convert to hexa
    private void Find_Correct_Function_06_Address()
    {
        // Take 4th digit
        int Digit4 = Info.GetSerialNumber().ElementAt(3);

        // Make not in 16 bits
        int Result_Not = Digit4 ^ 0xFFFF;

        // Convert to hexa
        string address = Result_Not.ToString("X4");

        correctAns += address;
        Debug.LogFormat("[ ModBus #{0} - Find_Correct_Function_06_Address - digit 4 : {1} - number of word : {2}]", _moduleId, Digit4, address);
    }

    // For Function 06 - value:
    // - Add every number together
    // - Convert it to hexadecimal
    // - troncate if necessary
    private void Find_Correct_Function_06_Value()
    {
        // Make the sum of all number
        int sum = 0;
        int actualSerialDigit; 
        // For all serial number
        for(int i=0; i<6; i++)
        {
            actualSerialDigit = Info.GetSerialNumber().ElementAt(i);
            // if it's a number
            if (actualSerialDigit >= '0' && actualSerialDigit <= '9')
            {
                // Soustract by ASCII code of 0, 48
                actualSerialDigit -= 48;
                sum += actualSerialDigit;
            }
        }

        // Convert it to hexadecimal
        string value = sum.ToString("X4");
        correctAns += value;

        Debug.LogFormat("[ ModBus #{0} - Find_Correct_Function_06_Value - value : {1}]", _moduleId, value);
    }
    
    // UpdateScreenText :
    // - Update screen text with yourAns
    private void UpdateScreenText()
    {
        if(yourAns != "")
        {
            // Remplace in screen text our value
            System.Text.StringBuilder sb = new System.Text.StringBuilder(screen_text);
            //print((yourAns.Length + 1) / 3.0));
            sb[yourAns.Length + Convert.ToInt32((yourAns.Length - 1) / 2) - 1] = yourAns[yourAns.Length - 1]; //Convert.ToInt32(yourAns.Length/3) is to jump through "."
            screen_text = sb.ToString();
        } else {
            // Reset screen text
            screen_text = "__.__.__.__.__.__";
        }
        

        Screen.text = screen_text;
    }

    private void Awake()
    {
        btnSend.OnInteract += delegate ()
        {
            //When btnSend is clicked
            Send_Clicked();
            return false;
        };
        for (int i = 0; i < 16; i++)
        {
            int j = i;
            btn[i].OnInteract += delegate ()
            {
                //When 1 btn is clicked (not send), use j parameter.
                Button_Cliked(j);
                return false;
            };
        }
    }

    void Send_Clicked()
    {
        if (!_lightsOn || _isSolved) return;

        if (correctAns == yourAns)
        {
            //You win
            GetComponent<KMBombModule>().HandlePass();
            _isSolved = true;
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
            yourAns = "";
            UpdateScreenText();
        }
    }

    void Button_Cliked(int num)
    {
        if (!_lightsOn || _isSolved) return;

        Debug.LogFormat("[ ModBus #{0} - HandlePress - num : {1} ]", _moduleId, num);

        string value = num.ToString("X");

        if (yourAns.Length <= 11) {
            yourAns += value;
            UpdateScreenText();
        } else { // If it's complete
            // pass
        }
    }
}

// Need to be test :
// 5 exemple of path function 04
// 5 exemple of path function 06






