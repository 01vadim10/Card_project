using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

using System.IO.Ports;

namespace WindowsFormsApplication1
{
    public partial class MifareReader : Form
    {
        private Thread thread_query; //Создает поток для работы со считывателем
        //private volatile bool _shouldStop;

        public MifareReader()
        {
            InitializeComponent();
        }

        private void rtxtDataArea_TextChanged(object sender, EventArgs e)
        {

        }

        private void MifareReader_Load(object sender, EventArgs e)
        {
            updatePorts();
        }

        //Обновляет доступные порты
        private void updatePorts()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                cmbPortName.Items.Add(port);
            }
        }

       
        private SerialPort ComPort = new SerialPort(); //Initialise ComPort Variable as SerialPort

        //Открывает Com порт, создает соединение между считывателем и программой
        private void connect()
        {
            bool error = false;

            // Check if all settings have been selected
  
            if (cmbPortName.SelectedIndex != -1 & String.Compare(txtKeyA.Text, "") != 0 &
                String.Compare(txtKeyB.Text, "") != 0 & String.Compare(txtMark.Text, "") != 0)  //if yes than Set The Port's settings
            {
                ComPort.PortName = cmbPortName.Text;
                ComPort.BaudRate = 115200; //convert Text to Integer
                ComPort.Parity = System.IO.Ports.Parity.None; //convert Text to Parity
                ComPort.DataBits = 8; //convert Text to Integer
                ComPort.StopBits = System.IO.Ports.StopBits.One; //convert Text to stop bits
  
                try //always try to use this try and catch method to open your port.
                //if there is an error your program will not display a message instead of freezing.
                {
                    //Open Port
                    ComPort.Open();
                }
                catch (UnauthorizedAccessException) { error = true; }
                catch (System.IO.IOException) { error = true; }
                catch (ArgumentException) { error = true; }

                if (error) MessageBox.Show(this, "Could not open the COM port. Most likely it is already in use, has been removed, or is unavailable.",
                                            "COM Port unavailable", MessageBoxButtons.OK, MessageBoxIcon.Stop);
               
            }
            else 
            {
                MessageBox.Show("Please select all the COM Serial Port Settings and Key A, Key B, ", "Serial Port Interface",
                                 MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            //if the port is open, Change the Connect button to disconnect, enable the send button.
            //and disable the groupBox to prevent changing configuration of an open port.
            if (ComPort.IsOpen)
            {
                btnConnect.Text = "Disconnect";
                groupBox1.Enabled = false;
                Thread_query_start();
            }
         }

        //Запускает поток запросов для работы со считывателем
        public void Thread_query_start()
        {
            //thread_query.SetApartmentState(ApartmentState.STA);
            thread_query = new Thread(Thread_query);
            thread_query.Start();
        }

        //Останавливает поток запросов работы со считывателем
        public void Thread_query_stop()
        {
            thread_query.Abort();
            thread_query.Join();
        }

         // Call this function to close the port.
         private void disconnect()
         {
             
             ComPort.Close();
             Thread_query_stop();
             txtCID.Text = "";
             txtID.Text = "";
             btnConnect.Text = "Connect";
             groupBox1.Enabled = true;
         }

        
         //whenever the connect button is clicked, it will check if the port is already open, call the disconnect function.
         // if the port is closed, call the connect function.
         private void btnConnect_Click(object sender, EventArgs e)
         {
            if (ComPort.IsOpen)
             {
                disconnect();
             }
             else
             {
                connect();
                
             }
         }
         //Override String.ToString("X") - add 0 to 0x1 = 0x01
         //public override string ToString(string var)
         //{
         //    string res = null;
         //    if (String.Compare(var, "Xx") == 0)
         //        res = this.ToString().Length > 1 ? this.ToString() : "0" + this.ToString();
         //    return res;
         //}
  
         // a function to send data to the serial port
        private void sendData(string txtSend)
        {
            bool error = false;
  
            try
            {
                // Convert the user's string of hex digits (example: E1 FF 1B) to a byte array
                byte[] data = HexStringToByteArray(txtSend);
  
                // Send the binary data out the port
                ComPort.Write(data, 0, data.Length);
            }
            catch (FormatException) { error = true; } 
            // Inform the user if the hex string was not properly formatted
            catch (ArgumentException) { error = true; }
  
            if (error) MessageBox.Show(this, "Not properly formatted hex string: " + txtSend + "\n",
                                        "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
        //Convert a string of hex digits (example: E1 FF 1B) to a byte array.
        //The string containing the hex digits (with or without spaces)
        //Returns an array of bytes. </returns>
        private byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }
  
        //Converts an array of bytes into a formatted string of hex digits (example: E1 FF 1B)
        //The array of bytes to be translated into a string of hex digits.
        //Returns a well formatted string of hex digits with spacing.
        private string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0').PadRight(3, ' '));
            return sb.ToString().ToUpper();
        }

        //This event will be raised when the form is closing.
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ComPort.IsOpen) ComPort.Close(); //close the port if open when exiting the application.
        }
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string receiveData = serialPort1.ReadExisting(); //read all avaliable data in the receiving buffer
        }

        //Подсчет контрольной суммы CRC
        private byte calcCRC(string var)
        {
            byte res = 0;
            byte[] data = HexStringToByteArray(var);

            foreach(byte b in data)
                res ^= b;

            return res;
        }

        //Записываем полученный CID в поле программы для отображения
        private void ChangeTextCID(string sText)
        {
            txtCID.Text = sText;
        }

        //Записываем полученный ID в поле программы для отображения
        private void ChangeTextID(string sText)
        {
            this.txtID.Text = sText;
        }
        public delegate void TXTChangeCID(string sText);
        public delegate void TXTChangeID(string sText);

        //Формирует запросы: search - поиск карточки; authA - авторизация карточки; authB - авторизация искомого сектора; rSector - чтение заданного сектора
        //Здесь command - запрос, который мы хотим отправить(search, authA, auhtB, rSector); sectorNum - номер считываемого блока (0x00 - по умолчанию),
        //byteRead - количество считываемых байт
        private string TransferQuery(string command, /*byte blockNum = 0x00, */byte sectorNum = 0xff, byte byteRead = 0x03)
        {
            const string bLn = "02";
            const string eLn = "03";
            const string bSect = "90005709000101";
            string result = null;

            switch (command)
            {
                case "search":
                    result = bLn + "E0003D0400000200" + calcCRC("E0003D0400000200").ToString("X") + eLn;
                    break;
                case "authA":
                    result = bLn + "80 00 54 14 00 01 00 40" + txtKeyA.Text + calcCRC("80 00 54 14 00 01 00 40" + txtKeyA.Text).ToString("X") + eLn;
                    break;
                case "authB":
                    //result = bLn + "80 00 54 14 00 01 00 40" + txtKeyB.Text + calcCRC("80 00 54 14 00 01 00 40" + txtKeyB.Text).ToString("X") + eLn;
                    string query = "80 00 54 14 00 01" + sectorNum.ToString("X") + "40" + txtKeyB.Text;
                                string qCrc = calcCRC(query).ToString("X");
                                string authSectorCRC = qCrc.Length > 1 ? qCrc : "0" + qCrc;
                                result = bLn + query + authSectorCRC + eLn;
                     break;
                case "rSector":
                    if ((sectorNum < 0xff) && (byteRead > 0))
                    {
                        qCrc = calcCRC(bSect + (sectorNum.ToString("X").Length > 1 ? sectorNum.ToString("X") : "0" + sectorNum.ToString("X")) + "00" +
                                 (byteRead.ToString("X").Length > 1 ? byteRead.ToString("X") : "0" + byteRead.ToString("X")) + "01 01 01").ToString("X");
                        result = bLn + bSect + (sectorNum.ToString("X").Length > 1 ? sectorNum.ToString("X") : "0" + sectorNum.ToString("X")) + "00" +
                                 (byteRead.ToString("X").Length > 1 ? byteRead.ToString("X") : "0" + byteRead.ToString("X")) + "01 01 01" + qCrc + eLn;
                    }
                    break;
            }

            return result;
        }

        //Поиск карточки и запись CID
        private bool SearchCardQuery()
        {
            
            bool res = false;

            sendData(TransferQuery("search"));
            System.Threading.Thread.Sleep(500);
            string rcvData = ReadData();

            if (!CompareData(rcvData, "02E0000104E503") && !String.IsNullOrEmpty(rcvData))
                res = GetCIDtoForm(rcvData);

            return res;
        }

        //Запись CID в поле программы и файл
        private bool GetCIDtoForm(string data)
        {
            bool res = false;
            TXTChangeCID tCID = new TXTChangeCID(ChangeTextCID);

            if (data.Length >= (22 + 14))
            {
                string cid = data.Substring(22, 14);
                if (!CompareData(txtCID.Text, cid))
                {
                    txtCID.BeginInvoke(tCID, new object[] { cid });
                    AppendData(cid);
                    res = true;
                }
            }

            return res;
        }

        //Запись CID в поле программы и файл
        private bool GetIDtoForm(string data)
        {
            bool res = false;
            TXTChangeID tID = new TXTChangeID(ChangeTextID);

            if (data.Length >= (10 + 32/*16*/))
            {
                string id = data.Substring(10, 32/*16*/);
                txtID.BeginInvoke(tID, new object[] { id });
                AppendData(id);
                res = true;
            }

            return res;
        }

        //Чтение данных из порта ComPort
        private string ReadData()
        {
            string receiveData = null;
            if (ComPort.IsOpen && (ComPort.BytesToRead > 0))
            {
                byte[] answer = new byte[(int)ComPort.BytesToRead];     //  Читаем буфер для аналаза ответа на команду Z (управление)

                ComPort.Read(answer, 0, ComPort.BytesToRead);
                receiveData = BitConverter.ToString(answer).Replace("-", "");
            }

            return receiveData;
        }


        //Сравнение двух строк, результат true - строки совпали; false - строки различны
        private bool CompareData(string cmprData, string etalon)
        {
            bool res = false;

            if (String.Compare(cmprData, etalon) == 0)
                res = true;

            return res;
        }

        //Первичная авторизация карточки для чтения 0 и 1 секторов
        private bool AuthQueryA()
        {
            bool res = false;

            sendData(TransferQuery("authA"));
            System.Threading.Thread.Sleep(100);
            string rcvData = ReadData();
            if (!CompareData(rcvData, "02 80 00 01 00 81 03"))
            {
                res = true;
            }

            return res;
        }

        //Авторизация искомого сектора из которого мы хотим получить данные
        private bool AuthQueryB(byte blockNum)
        {
            bool res = false;

            sendData(TransferQuery("authB", blockNum));
            System.Threading.Thread.Sleep(100);
            string rcvData = ReadData();
            if (!CompareData(rcvData, "02 80 00 01 00 81 03"))
            {
                res = true;
            }

            return res;
        }

        //Посылает данные для чтения сектора данных и записывает полученные данные, где sectorNum - блок с которого начнется чтение данных, bRead - кол-во считываемых байт
        private string ReadSector(byte sectorNum = 0xff, byte bRead = 0x03)
        {
            sendData(TransferQuery("rSector", sectorNum, bRead));
            System.Threading.Thread.Sleep(300);
            string rcvData = ReadData();
            
            return rcvData;
        }

        //Посылка запроса и чтение 0-го сектора
        private string ReadSector_0()
        {
            string rcv = ReadSector(0x00);
            if (rcv.Length >= (10 + 48*2))
                return rcv.Substring(10, 48 * 2);
            else return "";
        }

        //Посылка запроса и чтение 1-го сектора
        private string ReadSector_1()
        {
            string rcv = ReadSector(0x04);
            if (rcv.Length >= (10 + 48*2))
                return rcv.Substring(10, 48 * 2);
            else return "";
        }

        //Посылка запроса и чтение искомого сектора
        private string ReadSector_Num(byte sectorNum)
        {
            string rcv = ReadSector(sectorNum, 0x01);
            if (rcv.Length >= (10 + 32/*16*/))
                return rcv;
            else return "";
        }

        //Поиск искомой метки в 0-м и 1-м секторах. Возвращает номер вхожедения первого символа искомой метки если метка была найдена в каком-либо секторе, иначе возвращает -1
        private int SearchMark()
        {
            byte[] txtSearchMark = HexStringToByteArray(txtMark.Text);
            Array.Reverse(txtSearchMark);
            string str = BitConverter.ToString(txtSearchMark, 0).Replace("-", "");
            string rSector0 = ReadSector_0();
            string rSector1 = ReadSector_1();
            int index = -1;

            if (!CompareData(rSector0, ""))
            {
                index = rSector0.IndexOf(str);
                if (index > -1)
                    index = (index / 4) * 2;
            }
            if (!CompareData(rSector1, "") && (index == -1))
            {
                index = rSector1.IndexOf(str);
                if (index > -1)
                    index = ((index / 4) + 16) * 2;
            }
            return index;
        }

        //Дописывает CID и ID карточки в файл
        private void AppendData(string sText)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\MIFARECARD_CID_ID.txt", true))
            {
                file.WriteLine(sText);
            }
        }

        //Подает звуковой сигнал со считывателя
        private void Beeper()
        {
            sendData("02 B0 00 26 07 00 04 01 00 01 00 01 94 03");

            System.Threading.Thread.Sleep(100);
            ComPort.DiscardInBuffer();
        }

        //Основной метод, который постоянно работает в потоке пока существует соединение по Com-порту
        private void Thread_query()
        {
            //Пока поток существует, выполнять
            while (thread_query.IsAlive)
            {
                if (SearchCardQuery())                                                              //Поиск карточки
                {
                    if (AuthQueryA())                                                               //Авторизация карточки
                    {
                        int num = SearchMark();                                                     //Поиск метки в 0 и 1 секторах
                        if (num > -1)
                        {
                            if (AuthQueryB(Convert.ToByte(num)))                                    //Авторизация искомого сектора
                            {
                                string receiveData = ReadSector_Num(Convert.ToByte(num * 2));       //Чтение данных искомого сектора
                                if (GetIDtoForm(receiveData))                                       //Запись Id в поле программы и файл
                                    Beeper();                                                       //Подать звуковой сигнал об окончании считывания карточки
                            }
                        }
                    }
                }
            }
        }



        
    }
}
