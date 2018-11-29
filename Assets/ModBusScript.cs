using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using KMHelper;
using System;

public class ModBusScript : MonoBehaviour {

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

    private int digit = 0; //Digit currently writing
    private char[] CorrectAns = new char[17]; //Answer correct.
    private char[] YourAns = new char[17]; //Variable answer modify by you
    private int Function = 0;
    private int FindAdressWord04Result = 0;
    // Use this for initialization
    void Start ()
    {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;
	}
	
    void Activate()
    {
        Init();
        _lightsOn = true;
    }

    void Init()
    {
        Debug.LogFormat("[ ModBus #{0} ]", _moduleId);

        //Init the variable.
        digit = 0;
        InitTab();
        FindCorrectAns();
        UpdateScreenText();
    }

    private void FindCorrectAns ()
    {
        FindSlaveAdress();
        FindFunction();
        Function = 06;
        if(Function == 04)
        {
            FindAdressWord04();
            FindNumberOfWord04();
        }
        else if(Function == 06)
        {
            FindAdressInRegister06();
            FindValue06();
        }
        else
        {
            Debug.LogFormat("[ ModBus #{0} Erreur Function N°{1}]", _moduleId, Function);
        }
    }

    private void UpdateScreenText ()
    {
        Screen.text = new string(YourAns);
        //Conv all char in a string for the screentext
    }

    private void Awake()
    {
        btnSend.OnInteract += delegate ()
        {
            //When btnSend is clicked
            SendPress();
            return false;
        };
        for (int i = 0; i < 16; i++)
        {
            int j = i;
            btn[i].OnInteract += delegate ()
            {
                //When 1 btn is clicked (not send), use j parameter.
                HandlePress(j);
                return false;
            };
        }
    }

    void SendPress()
    {
        if (!_lightsOn || _isSolved) return;

        if (CorrectAns.SequenceEqual(YourAns))
        {
            //You win
            GetComponent<KMBombModule>().HandlePass();
            _isSolved = true;
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
        }
    }

    void HandlePress(int num)
    {
        if (!_lightsOn || _isSolved) return;

        YourAns[digit] = IntToASCII(num); //Need to convert, NumToASCII(num) -> ASCII
        Debug.LogFormat("[ ModBus #{0} - AnsDigit N°{1} -> {2} ]", _moduleId, digit, YourAns[digit]);
        UpdateScreenText();

        if (digit % 3 == 1) // If is a '.' digit (for display)
        {
            digit++;
        }

        if (digit >= 16) //Test if you are at the last digit
        {
            digit = 0; 
        }
        else
        {
            digit++; 
        }
    }


    private char IntToASCII(int num)
    {
        //Debug.LogFormat("IntToASCII IN -> {0}", num);
        if (num<=9) //If is an number, convert to ASCII number
        {
            num = num + 48;
        }
        else
        {
            num = num + 55; //A = 10 so 10 + 55 = 65 -> 'A'
        }
        //Debug.LogFormat("IntToASCII OUT -> {0}", (char)num);
        return (char)num;
    }

    private void InitTab()
    {
        for(int i=0; i<17; i++)
        {
            if((i%3 == 0)||(i%3==1))
            {
                CorrectAns[i] = '_';
                YourAns[i] = '_';
            }
            else
            {
                CorrectAns[i] = '.';
                YourAns[i] = '.';
            }
        }
    }

    private void FindSlaveAdress()
    {
        
        string SlaveAdress = BitConverter.ToString(new byte[] { Convert.ToByte(Info.GetSerialNumber().ElementAt(0)) });
        CorrectAns[0] = SlaveAdress.ToCharArray()[0];
        CorrectAns[1] = SlaveAdress.ToCharArray()[1];
        //Debug.LogFormat("[ ModBus #{0} - Slave Adress {1}]", _moduleId, SlaveAdress);
        Debug.LogFormat("[ ModBus #{0} - Partiel Correct Answer {1}]", _moduleId, new string(CorrectAns));
    }

    private void FindFunction()
    {
        Function = Info.GetSerialNumber().ElementAt(5) % 2;
        Function = Function * 2 + 4;
        CorrectAns[3] = '0';
        CorrectAns[4] = (char)(Function + 48);
        Debug.LogFormat("[ ModBus #{0} - Partiel Correct Answer {1}]", _moduleId, new string(CorrectAns));
    }

    private void FindAdressWord04()
    {
        int firstNumber = Info.GetSerialNumber().ElementAt(2);
        int secondNumber = Info.GetSerialNumber().ElementAt(3);
        if(firstNumber<58) //if is a number
        {
            firstNumber -= 48;
        }
        if (secondNumber < 58) //if is a number
        {
            firstNumber -= 48;
        }

        FindAdressWord04Result = firstNumber * secondNumber * 100;

        string SlaveAdress = BitConverter.ToString(new byte[] { Convert.ToByte(FindAdressWord04Result / 256)});
        string SlaveAdress2 = BitConverter.ToString(new byte[] { Convert.ToByte(FindAdressWord04Result % 256)});
        CorrectAns[6] = SlaveAdress.ToCharArray()[0];
        CorrectAns[7] = SlaveAdress.ToCharArray()[1];

        CorrectAns[9] = SlaveAdress2.ToCharArray()[0];
        CorrectAns[10] = SlaveAdress2.ToCharArray()[1];

        Debug.LogFormat("[ ModBus #{0} - Partiel Correct Answer {1}]", _moduleId, new string(CorrectAns));
    }

    private void FindNumberOfWord04()
    {
        int FindNumberOfWord04result;
        FindNumberOfWord04result = Info.GetSerialNumber().ElementAt(0) * Function + Info.GetSerialNumber().ElementAt(4);

        string SlaveAdress = BitConverter.ToString(new byte[] { Convert.ToByte(FindNumberOfWord04result / 256) });
        string SlaveAdress2 = BitConverter.ToString(new byte[] { Convert.ToByte(FindNumberOfWord04result % 256) });

        CorrectAns[12] = SlaveAdress.ToCharArray()[0];
        CorrectAns[13] = SlaveAdress.ToCharArray()[1];

        CorrectAns[15] = SlaveAdress2.ToCharArray()[0];
        CorrectAns[16] = SlaveAdress2.ToCharArray()[1];

        Debug.LogFormat("[ ModBus #{0} - Partiel Correct Answer {1}]", _moduleId, new string(CorrectAns));
    }

    private void FindAdressInRegister06()
    {
        byte firstRegister = 0x00; 
        byte secondRegister = 0x00;
        byte[] temp;

        temp = new byte[] { Convert.ToByte(Info.GetSerialNumber().ElementAt(3)) };
        firstRegister = temp[0];

        Debug.LogFormat("[ ModBus #{0} - firstRegister -> {1}]", _moduleId, firstRegister);

        secondRegister = (byte)(firstRegister ^ 0xFF);
        secondRegister = (byte)(secondRegister % 100);

        Debug.LogFormat("[ ModBus #{0} - secondeRegister -> {1}]", _moduleId, secondRegister);

        CorrectAns[6] = (char)(firstRegister/10+48);
        CorrectAns[7] = (char)(firstRegister % 10 + 48);

        CorrectAns[9] = (char)(secondRegister / 10 + 48);
        CorrectAns[10] = (char)(secondRegister % 10 + 48);

        Debug.LogFormat("[ ModBus #{0} - Partiel Correct Answer {1}]", _moduleId, new string(CorrectAns));
    }

    private void FindValue06()
    {
        int result;
        result = (Info.GetSerialNumber().ElementAt(0) - Function) * Info.GetSerialNumber().ElementAt(4); 

        Debug.LogFormat("[ ModBus #{0} - Test {1}]", _moduleId, result);

        CorrectAns[12] = (char)(result % 10000 /1000 + 48);
        CorrectAns[13] = (char)(result % 1000 / 100 + 48);

        CorrectAns[15] = (char)(result % 100 / 10 + 48);
        CorrectAns[16] = (char)(result % 10 + 48);


        Debug.LogFormat("[ ModBus #{0} - Partiel Correct Answer {1}]", _moduleId, new string(CorrectAns));
    }
}

/*USEFULL FUNCTION
 * Info.GetSerialNumber().ElementAt(0); //Get number from serialNumber. (return char)
 * (char)122 Convert like a savage an int in char
 * 
 * 
 * 
 * */
