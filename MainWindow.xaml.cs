using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace A4_MotherBoard_Tester_WPF
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        FTDI Globle_Ftdi = new FTDI();
        int iTxtFeedBack_Count = 1;
        bool window_isLoaded = false;
        PromCtrlParameters Class_promCtrlParameters = new PromCtrlParameters();
        uint uint_PromCtrl_AddressVar = 0;
        public MainWindow()
        {
            InitializeComponent();


        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txt_FtdiFeedBackMessage.Clear();
            txt_FtdiFeedBackMessage.AppendText(iTxtFeedBack_Count.ToString() + ". " + System.DateTime.Now + "->" + fn_Ftdi_Init(out int FtdiCount));
            if (FtdiCount > 0)
            {
                fn_FeedBackMessage(fn_Ftdi_Connect(FtdiCount, 2));
                fn_FeedBackMessage(fn_Ftdi_Setting(FtdiCount));
                fn_IfConnectedWillMakeSound();
            }

            fn_RadBtn_PWprest();
            fn_Init_register();


            cob_PromCtrl_17to16.SelectedIndex = Properties.Settings.Default.LastSelected_17to16;
            cob_PromCtrl_19.SelectedIndex = Properties.Settings.Default.LastSelected_19;
            cob_PromCtrl_22to20.SelectedIndex = Properties.Settings.Default.LastSelected_22to20;
            cob_PromCtrl_23.SelectedIndex = Properties.Settings.Default.LastSelected_23;
            cob_PromCtrl_25to24.SelectedIndex = Properties.Settings.Default.LastSelected_25to24;

            window_isLoaded = true; // 確保在設定完 ComboBox 之後才設為 true

            fn_GivePromCtrlParameters();
            #region 清空所有txt_xx_Read內的text

            txt_40_Read.Clear();
            txt_30_Read.Clear();
            txt_20_Read.Clear();
            txt_10_Read.Clear();

            txt_41_Read.Clear();
            txt_31_Read.Clear();
            txt_21_Read.Clear();
            txt_11_Read.Clear();

            txt_42_Read.Clear();
            txt_32_Read.Clear();
            txt_22_Read.Clear();
            txt_12_Read.Clear();

            txt_43_Read.Clear();
            txt_33_Read.Clear();
            txt_23_Read.Clear();
            txt_13_Read.Clear();
            #endregion

        }



        #region Ftdi functions
        private string fn_Ftdi_Init(out int FindFtdiCount)
        {
            // 取得裝置數量
            uint ftdiDeviceCount = 0;
            FindFtdiCount = (int)ftdiDeviceCount;
            FTDI.FT_STATUS status = Globle_Ftdi.GetNumberOfDevices(ref ftdiDeviceCount);

            if (status != FTDI.FT_STATUS.FT_OK || ftdiDeviceCount == 0)
            {
                Globle_Ftdi.GetDescription(out string ftdiDescription);
                MessageBox.Show("無法取得 FTDI 裝置數量" + ftdiDescription);
                return "找不到 FTDI 裝置";
            }

            FindFtdiCount = (int)ftdiDeviceCount;
            return $"找到 {ftdiDeviceCount} 個裝置";
        }

        private string fn_Ftdi_Connect(int count, int Index)
        {
            FTDI.FT_DEVICE_INFO_NODE[] deviceList = new FTDI.FT_DEVICE_INFO_NODE[count];
            FTDI.FT_STATUS status = Globle_Ftdi.GetDeviceList(deviceList);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                Globle_Ftdi.GetDescription(out string ftdiDescription);
                MessageBox.Show("無法取得裝置清單，請檢查連接或驅動程式" + ftdiDescription);
                return ("無法取得裝置清單");
            }

            if (Globle_Ftdi.IsOpen != true)
            {
                if (Globle_Ftdi.OpenByIndex((uint)Index) != FTDI.FT_STATUS.FT_OK)
                {
                    Globle_Ftdi.GetDescription(out string ftdiDescription);
                    MessageBox.Show("無法開啟 FTDI 裝置，請檢查連接或驅動程式。" + ftdiDescription);
                }
            }
            status = Globle_Ftdi.GetDeviceList(deviceList);


            if (status != FTDI.FT_STATUS.FT_OK || !Globle_Ftdi.IsOpen)
            {

                Globle_Ftdi.GetDescription(out string ftdiDescription);
                MessageBox.Show("無法開啟裝置: " + ftdiDescription);
                return ("無法開啟裝置" + ftdiDescription);
            }
            else if (status == FTDI.FT_STATUS.FT_OK && Globle_Ftdi.IsOpen)
            {
                Globle_Ftdi.GetDescription(out string ftdiDescription);
                fn_FeedBackMessage("裝置已開啟: " + ftdiDescription);
                return ("裝置已開啟" + ftdiDescription);
            }
            else
            {
                return (status.ToString());
            }
        }

        private string fn_Ftdi_Setting(int count)
        {
            Globle_Ftdi.SetLatency(0);
            Globle_Ftdi.SetBaudRate(115200);
            Globle_Ftdi.SetDataCharacteristics(8, 1, 2); // 8-1-Even // 0-4=None,Odd,Even,Mark,Space
            Globle_Ftdi.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0x11, 0x13); // 無 flow control

            FTDI.FT_DEVICE_INFO_NODE[] deviceList = new FTDI.FT_DEVICE_INFO_NODE[count];
            FTDI.FT_STATUS status = Globle_Ftdi.GetDeviceList(deviceList);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                return ("無法取得裝置清單");
            }
            else
            {
                return ("裝置設定成功 "
                    + Environment.NewLine + "     SetLatency = 0"
                    + Environment.NewLine + "     SetBaudRate = 115200"
                    + Environment.NewLine + "     SetDataCharacteristics =  DataBits = 8 , StopBits = 1 ,Parity = 2"
                    + Environment.NewLine + "     SetFlowControl = No flow control"
                    );
            }
        }


        private void fn_singleRead(ulong CMD_Page, ulong Address)
        {

            byte[] Send = new byte[5];
            // 命令
            Send[0] = (byte)(0x00 & 0xFF);
            // data 拆成 5 個 7-bit，加上最高位 1
            Send[1] = 0; // bit 27~21 (7bit)
            Send[2] = 0; // bit 20~14 (7bit)
            Send[3] = (byte)((CMD_Page & 0x7F));  // bit 13~7 (7bit)
            Send[4] = (byte)((Address & 0x7F));         // bit 6~0  (7bit)

            if (Send != null && Send.Length >= 5)
            {
                uint bytesWritten = 0;
                if (Globle_Ftdi.Write(Send, Send.Length, ref bytesWritten) != FTDI.FT_STATUS.FT_OK)
                {
                    MessageBox.Show("寫入失敗");
                    return;
                }

            }


        }
        private void fn_Ftdi_Write(byte command, ulong DATA)
        {
            #region 新手範例城市
            //uint cmd = 0x0A;
            //string CMD_bitString = Convert.ToString(cmd, 2).PadLeft(6, '0');// 轉換成binary字串，並補足6位
            //CMD_bitString = "00" + CMD_bitString; // 前面補兩個0，總長度為8位

            //uint data = 0x87654321; // 假設這是要寫入的資料
            //string data_bitString = Convert.ToString(data, 2).PadLeft(32, '0');//轉換成binary字串，並補足32位

            //// 將 data_bitString 分割成 1 個字一組
            //char[] data_bitStringToCharArray = data_bitString.ToCharArray();

            ////組合data到模板中
            //string bit2nd = "1000" + data_bitStringToCharArray[0] + data_bitStringToCharArray[1] + data_bitStringToCharArray[2] + data_bitStringToCharArray[3];

            //string bit3rd = "1" +
            //    data_bitStringToCharArray[4] + data_bitStringToCharArray[5] + data_bitStringToCharArray[6] + data_bitStringToCharArray[7] +
            //                data_bitStringToCharArray[8] + data_bitStringToCharArray[9] + data_bitStringToCharArray[10];

            //string bit4th = "1" + data_bitStringToCharArray[11] + data_bitStringToCharArray[12] + data_bitStringToCharArray[13] + data_bitStringToCharArray[14] +
            //                data_bitStringToCharArray[15] + data_bitStringToCharArray[16] + data_bitStringToCharArray[17];

            //string bit5th = "1" + data_bitStringToCharArray[18] + data_bitStringToCharArray[19] + data_bitStringToCharArray[20] + data_bitStringToCharArray[21] +
            //    data_bitStringToCharArray[22] + data_bitStringToCharArray[23] + data_bitStringToCharArray[24];

            //string bit6th = "1" + data_bitStringToCharArray[25] + data_bitStringToCharArray[26] + data_bitStringToCharArray[27] + data_bitStringToCharArray[28] +
            //    data_bitStringToCharArray[29] + data_bitStringToCharArray[30] + data_bitStringToCharArray[31];

            //// 將每個 bit 字串轉換為 byte , 同時組合成完整的 Send 陣列
            //Send[0] = Convert.ToByte(CMD_bitString, 2); // 命令
            //Send[1] = Convert.ToByte(bit2nd, 2);        // bit 31~28
            //Send[2] = Convert.ToByte(bit3rd, 2);        // bit 27~21
            //Send[3] = Convert.ToByte(bit4th, 2);        // bit 20~14
            //Send[4] = Convert.ToByte(bit5th, 2);        // bit 13~7
            //Send[5] = Convert.ToByte(bit6th, 2);        // bit 6~0

            //Send[1] = (byte)(((DATA >> 28) & 0x7F) | 0x80); // 取 bit 31~28（4 位）
            //Send[2] = (byte)(((DATA >> 21) & 0x7F) | 0x80); // 取 bit 27~21（7 位）
            //Send[3] = (byte)(((DATA >> 14) & 0x7F) | 0x80); // 取 bit 20~14（7 位）
            //Send[4] = (byte)(((DATA >> 7) & 0x7F) | 0x80); // 取 bit 13~7（7 位）
            //Send[5] = (byte)((DATA & 0x7F) | 0x80);         // 取 bit 6~0（7 位）
            //Send[0] = command;  
            #endregion

            if (cob_PromCtrl_23.SelectedIndex == 0)//32K
            {
                byte[] Send = new byte[6];
                // 命令
                Send[0] = (byte)(command & 0xFF);
                // data 拆成 5 個 7-bit，加上最高位 1
                Send[1] = (byte)(((DATA >> 28) & 0x0F) | 0x80); // bit 31~28 (4bit)
                Send[2] = (byte)(((DATA >> 21) & 0x7F) | 0x80); // bit 27~21 (7bit)
                Send[3] = (byte)(((DATA >> 14) & 0x7F) | 0x80); // bit 20~14 (7bit)
                Send[4] = (byte)(((DATA >> 7) & 0x7F) | 0x80);  // bit 13~7 (7bit)
                Send[5] = (byte)((DATA & 0x7F) | 0x80);         // bit 6~0  (7bit)
                string allbyte = Convert.ToString(Send[0], 2) + "," + Convert.ToString(Send[1], 2) + "," + Convert.ToString(Send[2], 2) + "," + Convert.ToString(Send[3], 2) + "," + Convert.ToString(Send[4], 2) + "," + Convert.ToString(Send[5], 2);
                if (Send != null && Send.Length >= 6)
                {
                    uint bytesWritten = 0;
                    if (Globle_Ftdi.Write(Send, Send.Length, ref bytesWritten) != FTDI.FT_STATUS.FT_OK)
                    {
                        MessageBox.Show("寫入失敗");
                        return;
                    }
                }
            }

            else if (cob_PromCtrl_23.SelectedIndex == 1)//2K
            {
                byte[] Send = new byte[5];
                Send[0] = (byte)((command & 0xFF) | 0x40);
                Send[1] = (byte)(((DATA >> 28) & 0x07) | 0x80);
                Send[2] = (byte)(((DATA >> 21) & 0x7F) | 0x80);
                Send[3] = (byte)(((DATA >> 14) & 0x7F) | 0x80);
                Send[4] = (byte)(((DATA >> 7) & 0x7F) | 0x80);

                if (Send != null && Send.Length >= 5)
                {
                    uint bytesWritten = 0;
                    if (Globle_Ftdi.Write(Send, Send.Length, ref bytesWritten) != FTDI.FT_STATUS.FT_OK)
                    {
                        MessageBox.Show("寫入失敗");
                        return;
                    }
                }
            }
        }

        private void fn_Ftdi_Read(out uint returnData, out byte address)
        {
            byte[] recv = new byte[6];
            uint bytesRead = 0;
            Globle_Ftdi.SetTimeouts(100, 100);
            FTDI.FT_STATUS status = Globle_Ftdi.Read(recv, 6, ref bytesRead);

            if (status != FTD2XX_NET.FTDI.FT_STATUS.FT_OK || bytesRead < 6)
            {
                MessageBox.Show("讀取失敗或資料不足");
                returnData = 0;
                address = 0;
                return;
            }

            address = recv[0];

            // 把標誌位 (bit7) 清掉，只保留低 7 bit
            uint part1 = (uint)(recv[1] & 0x0F); // 4bit
            uint part2 = (uint)(recv[2] & 0x7F); // 7bit
            uint part3 = (uint)(recv[3] & 0x7F); // 7bit
            uint part4 = (uint)(recv[4] & 0x7F); // 7bit
            uint part5 = (uint)(recv[5] & 0x7F); // 7bit

            // 組回原本的 32bit DATA
            uint data = (part1 << 28) |
                        (part2 << 21) |
                        (part3 << 14) |
                        (part4 << 7) |
                        (part5);

            returnData = data;


        }

        /// <summary>
        /// 0 : OK , 1 : Busy , 2 : ERR , 3 : ERR+BUSY
        /// </summary>
        /// <returns></returns>
        private uint fn_Ftdi_IsBusy()
        {
            bJumpOutError = false;
            fn_Ftdi_Write(0x00, 0x09);
            Thread.Sleep(10); // 等待讀取完成
            fn_Ftdi_Read(out uint Data, out byte add);
            uint last2Bits = Data & 0b11;
            return last2Bits;
        }

        private uint fn_Ftdi_IsBusy(int CMD_Page)
        {
            ulong data = 0x09;
            if (CMD_Page == 0) data = 0x009;
            else if (CMD_Page == 1) data = 0x109;
            else if (CMD_Page == 2) data = 0x209;
            else if (CMD_Page == 3) data = 0x309;
            fn_Ftdi_Write(0x00, data);
            Thread.Sleep(10); // 等待讀取完成
            fn_Ftdi_Read(out uint Data, out byte add);
            uint last2Bits = Data & 0b11;
            return last2Bits;
            //if (last2Bits == 0)//1000000//OK
            //{
            //    return 0;
            //}
            //else if (last2Bits == 1)//1000001//Busy
            //{
            //    return 1;
            //}
            //else if (Data == 2)//1000010//ERR
            //{
            //    return 2;
            //}
            //else  if (Data == 3)//ERR+BUSY
            //{
            //    return 3;
            //}

        }

        private void fn_IfConnectedWillMakeSound()
        {
            fn_Ftdi_Write(0x01, 0x20000);
            Thread.Sleep(100);

            fn_Ftdi_Write(0x01, 0x00000);
            Thread.Sleep(50);

            fn_Ftdi_Write(0x01, 0x20000);
            Thread.Sleep(50);

            fn_Ftdi_Write(0x01, 0x00000);
            Thread.Sleep(50);

            fn_Ftdi_Write(0x01, 0x20000);
            Thread.Sleep(50);
            fn_Ftdi_Write(0x01, 0x00000);
            Thread.Sleep(50);
        }

        private bool Purge()
        {
            FTDI.FT_STATUS status = Globle_Ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
            if (status != FTDI.FT_STATUS.FT_OK)
            {
                MessageBox.Show("清除緩衝區失敗: " + status.ToString());
                return false;
            }
            return true;
        }
        #endregion


        private void fn_FeedBackMessage(string message)
        {
            iTxtFeedBack_Count++;
            txt_FtdiFeedBackMessage.AppendText(Environment.NewLine
                + iTxtFeedBack_Count.ToString()
                + ". "
                + System.DateTime.Now
                + "->"
                + message);
        }



        private void btn_BuzzerOn_Click(object sender, RoutedEventArgs e)
        {
            fn_Ftdi_Write(0x01, 0x20000);
        }

        private void btn_BuzzerOff_Click(object sender, RoutedEventArgs e)
        {
            fn_Ftdi_Write(0x01, 0x00000);
        }

        private void btn_SINGLE_READ_Click(object sender, RoutedEventArgs e)
        {
            fn_Ftdi_Write(0x00, 0x09);
            Thread.Sleep(50); // 等待讀取完成
            fn_Ftdi_Read(out uint Data, out byte add);
            string dataBinary = Convert.ToString(Data, 2);
            txt_ReadBackMessage.AppendText(Environment.NewLine
                + ". "
                + System.DateTime.Now
                + "->"
                + "讀取地址: " + add.ToString()
                + ", 資料: " + dataBinary);
        }


        bool bJumpOutError = false;
        private bool fn_Ftdi_PageWrite(uint PROM_WRITE_Data1, uint PROM_WRITE_Data2, uint PROM_WRITE_Data3, uint PROM_WRITE_Data4)
        {
            string strFull_promCtrlParameters =
               Class_promCtrlParameters.Bit31_26 +
                  Class_promCtrlParameters.Bit25_24_I2C_clock_frequency +
                  Class_promCtrlParameters.Bit23_Type +
                  Class_promCtrlParameters.Bit22_20_ID +
                  Class_promCtrlParameters.Bit19_RDWR +
                  Class_promCtrlParameters.Bit18 +
                  Class_promCtrlParameters.Bit17_16_ByteSize +
                  Class_promCtrlParameters.Bit15_0_Address;

            // 將二進制字串轉換為16進制uint
            uint promCtrlParameters = Convert.ToUInt32(strFull_promCtrlParameters, 2);

            bool AllPageDone = false;
            int nowPage = 0;
            // if (Rad_ReadPage0.IsChecked == true) nowPage = 0;
           // else if (Rad_ReadPage1.IsChecked == true) nowPage = 1;
            //else if (Rad_ReadPage2.IsChecked == true) nowPage = 2;
           // else if (Rad_ReadPage3.IsChecked == true) nowPage = 3;


            switch (nowPage)
            {
                case 0:
                    while (true)
                    {
                        if (fn_Ftdi_IsBusy(0) == 0)
                        {
                            fn_Ftdi_Write(0x0A, PROM_WRITE_Data1);// PROM_WRITE1
                            fn_Ftdi_Write(0x0B, PROM_WRITE_Data2);// PROM_WRITE2
                            fn_Ftdi_Write(0x0C, PROM_WRITE_Data3);// PROM_WRITE3
                            fn_Ftdi_Write(0x0D, PROM_WRITE_Data4);// PROM_WRITE4

                            fn_Ftdi_Write(0x09, promCtrlParameters); // PROM_control
                            break;
                        }
                    }
                    break;

                case 1:
                    while (true)
                    {
                        if (fn_Ftdi_IsBusy(1) == 0)
                        {
                            fn_Ftdi_Write(0x0A, PROM_WRITE_Data1);// PROM_WRITE1
                            fn_Ftdi_Write(0x0B, PROM_WRITE_Data2);// PROM_WRITE2
                            fn_Ftdi_Write(0x0C, PROM_WRITE_Data3);// PROM_WRITE3
                            fn_Ftdi_Write(0x0D, PROM_WRITE_Data4);// PROM_WRITE4

                            fn_Ftdi_Write(0x09, promCtrlParameters); // PROM_control
                            break;
                        }
                    }
                    break;

                case 2:
                    while (true)
                    {
                        if (fn_Ftdi_IsBusy(2) == 0)
                        {
                            fn_Ftdi_Write(0x0A, PROM_WRITE_Data1);// PROM_WRITE1
                            fn_Ftdi_Write(0x0B, PROM_WRITE_Data2);// PROM_WRITE2
                            fn_Ftdi_Write(0x0C, PROM_WRITE_Data3);// PROM_WRITE3
                            fn_Ftdi_Write(0x0D, PROM_WRITE_Data4);// PROM_WRITE4

                            fn_Ftdi_Write(0x09, promCtrlParameters); // PROM_control
                            break;
                        }
                    }
                    break;

                case 3:
                    while (true)
                    {
                        if (fn_Ftdi_IsBusy(3) == 0)
                        {
                            fn_Ftdi_Write(0x0A, PROM_WRITE_Data1);// PROM_WRITE1
                            fn_Ftdi_Write(0x0B, PROM_WRITE_Data2);// PROM_WRITE2
                            fn_Ftdi_Write(0x0C, PROM_WRITE_Data3);// PROM_WRITE3
                            fn_Ftdi_Write(0x0D, PROM_WRITE_Data4);// PROM_WRITE4

                            fn_Ftdi_Write(0x09, promCtrlParameters); // PROM_control
                            break;
                        }
                    }
                    break;

            }

            return true;
        }

        private void btn_PageWrite_Click(object sender, RoutedEventArgs e)
        {
            //fn_Ftdi_PageWrite(0x1234, 0x1234, 0x1234, 0x1234);

        }

        private void btn_PageRead_Click(object sender, RoutedEventArgs e)
        {
            #region TXT.Clear

            txt_40_Read.Clear();
            txt_30_Read.Clear();
            txt_20_Read.Clear();
            txt_10_Read.Clear();

            txt_41_Read.Clear();
            txt_31_Read.Clear();
            txt_21_Read.Clear();
            txt_11_Read.Clear();

            txt_42_Read.Clear();
            txt_32_Read.Clear();
            txt_22_Read.Clear();
            txt_12_Read.Clear();

            txt_43_Read.Clear();
            txt_33_Read.Clear();
            txt_23_Read.Clear();
            txt_13_Read.Clear();
            #endregion

            string str_addre = Class_promCtrlParameters.Bit31_26
                + Class_promCtrlParameters.Bit25_24_I2C_clock_frequency
                + Class_promCtrlParameters.Bit23_Type
                + Class_promCtrlParameters.Bit22_20_ID
                + "1" + Class_promCtrlParameters.Bit18
                + Class_promCtrlParameters.Bit17_16_ByteSize
                + Class_promCtrlParameters.Bit15_0_Address;
            uint Address = Convert.ToUInt32(str_addre, 2);
            int nowPage = 0;
            int ReadByte = 0;

            if (cob_PromCtrl_17to16.SelectedIndex == 0) ReadByte = 8;//
            else if (cob_PromCtrl_17to16.SelectedIndex == 1) ReadByte = 16;
            else if (cob_PromCtrl_17to16.SelectedIndex == 2) ReadByte = 32;
            else if (cob_PromCtrl_17to16.SelectedIndex == 3) ReadByte = 64;



            ulong Data_A = 0x00A + (ulong)(nowPage * 0x100);
            ulong Data_B = 0x00B + (ulong)(nowPage * 0x100);
            ulong Data_C = 0x00C + (ulong)(nowPage * 0x100);
            ulong Data_D = 0x00D + (ulong)(nowPage * 0x100);

            int retry = 0;

            if (ReadByte == 8)
            {
                while (true)
                {
                    if (fn_Ftdi_IsBusy(0) == 0 && retry < 10)
                    {
                        fn_Ftdi_Write(0x09, Address);
                        while (true)
                        {
                            if (fn_Ftdi_IsBusy(0) == 0 && retry < 10)
                            {
                                fn_Ftdi_Write(0x00, Data_A); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData0, out byte PageData0_);
                                fn_Ftdi_Write(0x00, Data_B); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData1, out byte PageData1_);
                                for (int i = 0; i < 4; i++)
                                {
                                    for (int j = 0; j < 4; j++)
                                    {
                                        fn_LetUINull(i, j);
                                    }
                                }
                                fn_GivePageReadUiValue(PageData0, 0, nowPage);
                                fn_GivePageReadUiValue(PageData1, 1, nowPage);
                                break;
                            }
                            else
                            {
                                retry++;
                                Thread.Sleep(100);
                            }
                        }
                        break;
                    }
                    else
                    {
                        retry++;
                        Thread.Sleep(100);
                    }
                }
            }

            if (ReadByte == 16)
            {
                while (true)
                {
                    if (fn_Ftdi_IsBusy(0) == 0 && retry < 10)
                    {
                        fn_Ftdi_Write(0x09, Address);
                        while (true)
                        {
                            if (fn_Ftdi_IsBusy(0) == 0 && retry < 10)
                            {
                                fn_Ftdi_Write(0x00, Data_A); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData0, out byte PageData0_);
                                fn_Ftdi_Write(0x00, Data_B); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData1, out byte PageData1_);
                                fn_Ftdi_Write(0x00, Data_C); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData2, out byte PageData2_);
                                fn_Ftdi_Write(0x00, Data_D); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData3, out byte PageData3_);
                                for (int i = 0; i < 4; i++)
                                {
                                    for (int j = 0; j < 4; j++)
                                    {
                                        fn_LetUINull(i, j);
                                    }
                                }
                                fn_GivePageReadUiValue(PageData0, 0, nowPage);
                                fn_GivePageReadUiValue(PageData1, 1, nowPage);
                                fn_GivePageReadUiValue(PageData2, 2, nowPage);
                                fn_GivePageReadUiValue(PageData3, 3, nowPage);
                                break;
                            }
                            else
                            {
                                retry++;
                                Thread.Sleep(100);
                            }
                        }
                        break;
                    }
                    else
                    {
                        retry++;
                        Thread.Sleep(100);
                    }
                }
            }

            if (ReadByte == 32)
            {
                while (true)
                {
                    nowPage = 0;
                    if (fn_Ftdi_IsBusy(nowPage) == 0 && retry < 10)
                    {
                        fn_Ftdi_Write(0x09, Address);
                        while (true)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    fn_LetUINull(i, j);
                                }
                            }

                            if (fn_Ftdi_IsBusy(nowPage) == 0 && retry < 10)
                            {
                                fn_Ftdi_Write(0x00, Data_A); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData0, out byte PageData0_);
                                fn_Ftdi_Write(0x00, Data_B); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData1, out byte PageData1_);
                                fn_Ftdi_Write(0x00, Data_C); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData2, out byte PageData2_);
                                fn_Ftdi_Write(0x00, Data_D); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData3, out byte PageData3_);

                                fn_GivePageReadUiValue(PageData0, 0, nowPage);
                                fn_GivePageReadUiValue(PageData1, 1, nowPage);
                                fn_GivePageReadUiValue(PageData2, 2, nowPage);
                                fn_GivePageReadUiValue(PageData3, 3, nowPage);

                                nowPage = 1;
                                Data_A = 0x00A + (ulong)(nowPage * 0x100);
                                Data_B = 0x00B + (ulong)(nowPage * 0x100);
                                Data_C = 0x00C + (ulong)(nowPage * 0x100);
                                Data_D = 0x00D + (ulong)(nowPage * 0x100);

                                fn_Ftdi_Write(0x00, Data_A); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData0_1, out byte PageData0_1_1);
                                fn_Ftdi_Write(0x00, Data_B); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData1_1, out byte PageData1_1_1);
                                fn_Ftdi_Write(0x00, Data_C); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData2_1, out byte PageData2_1_1);
                                fn_Ftdi_Write(0x00, Data_D); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData3_1, out byte PageData3_1_1);

                                fn_GivePageReadUiValue(PageData0_1, 0, nowPage);
                                fn_GivePageReadUiValue(PageData1_1, 1, nowPage);
                                fn_GivePageReadUiValue(PageData2_1, 2, nowPage);
                                fn_GivePageReadUiValue(PageData3_1, 3, nowPage);

                                break;

                            }
                            else
                            {
                                retry++;
                                Thread.Sleep(100);
                            }
                        }
                        break;
                    }
                    else
                    {
                        retry++;
                        Thread.Sleep(100);
                    }
                }





            }

            if (ReadByte == 64)
            {
                while (true)
                {
                    nowPage = 0;
                    if (fn_Ftdi_IsBusy(nowPage) == 0 && retry < 10)
                    {
                        fn_Ftdi_Write(0x09, Address);
                        while (true)
                        {                        
                            if (fn_Ftdi_IsBusy(nowPage) == 0 && retry < 10)
                            {
                                fn_Ftdi_Write(0x00, Data_A); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData0, out byte PageData0_);
                                fn_Ftdi_Write(0x00, Data_B); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData1, out byte PageData1_);
                                fn_Ftdi_Write(0x00, Data_C); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData2, out byte PageData2_);
                                fn_Ftdi_Write(0x00, Data_D); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData3, out byte PageData3_);

                                fn_GivePageReadUiValue(PageData0, 0, nowPage);
                                fn_GivePageReadUiValue(PageData1, 1, nowPage);
                                fn_GivePageReadUiValue(PageData2, 2, nowPage);
                                fn_GivePageReadUiValue(PageData3, 3, nowPage);

                                nowPage = 1;
                                Data_A = 0x00A + (ulong)(nowPage * 0x100);
                                Data_B = 0x00B + (ulong)(nowPage * 0x100);
                                Data_C = 0x00C + (ulong)(nowPage * 0x100);
                                Data_D = 0x00D + (ulong)(nowPage * 0x100);

                                fn_Ftdi_Write(0x00, Data_A); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData0_1, out byte PageData0_1_1);
                                fn_Ftdi_Write(0x00, Data_B); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData1_1, out byte PageData1_1_1);
                                fn_Ftdi_Write(0x00, Data_C); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData2_1, out byte PageData2_1_1);
                                fn_Ftdi_Write(0x00, Data_D); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData3_1, out byte PageData3_1_1);

                                fn_GivePageReadUiValue(PageData0_1, 0, nowPage);
                                fn_GivePageReadUiValue(PageData1_1, 1, nowPage);
                                fn_GivePageReadUiValue(PageData2_1, 2, nowPage);
                                fn_GivePageReadUiValue(PageData3_1, 3, nowPage);


                                nowPage = 2;
                                Data_A = 0x00A + (ulong)(nowPage * 0x100);
                                Data_B = 0x00B + (ulong)(nowPage * 0x100);
                                Data_C = 0x00C + (ulong)(nowPage * 0x100);
                                Data_D = 0x00D + (ulong)(nowPage * 0x100);


                                fn_Ftdi_Write(0x00, Data_A); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData0_2, out byte PageData0__2);
                                fn_Ftdi_Write(0x00, Data_B); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData1_2, out byte PageData1__2);
                                fn_Ftdi_Write(0x00, Data_C); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData2_2, out byte PageData2__2);
                                fn_Ftdi_Write(0x00, Data_D); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData3_2, out byte PageData3__2);

                                fn_GivePageReadUiValue(PageData0_2, 0, nowPage);
                                fn_GivePageReadUiValue(PageData1_2, 1, nowPage);
                                fn_GivePageReadUiValue(PageData2_2, 2, nowPage);
                                fn_GivePageReadUiValue(PageData3_2, 3, nowPage);

                                nowPage = 3;
                                Data_A = 0x00A + (ulong)(nowPage * 0x100);
                                Data_B = 0x00B + (ulong)(nowPage * 0x100);
                                Data_C = 0x00C + (ulong)(nowPage * 0x100);
                                Data_D = 0x00D + (ulong)(nowPage * 0x100);

                                fn_Ftdi_Write(0x00, Data_A); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData0_1_3, out byte PageData0_1_1_3);
                                fn_Ftdi_Write(0x00, Data_B); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData1_1_3, out byte PageData1_1_1_3);
                                fn_Ftdi_Write(0x00, Data_C); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData2_1_3, out byte PageData2_1_1_3);
                                fn_Ftdi_Write(0x00, Data_D); //SINGLE_READ
                                fn_Ftdi_Read(out uint PageData3_1_3, out byte PageData3_1_1_3);

                                fn_GivePageReadUiValue(PageData0_1_3, 0, nowPage);
                                fn_GivePageReadUiValue(PageData1_1_3, 1, nowPage);
                                fn_GivePageReadUiValue(PageData2_1_3, 2, nowPage);
                                fn_GivePageReadUiValue(PageData3_1_3, 3, nowPage);
                                break;

                            }
                            else
                            {
                                retry++;
                                Thread.Sleep(100);
                            }
                        }
                        break;
                    }
                    else
                    {
                        retry++;
                        Thread.Sleep(100);
                    }
                }





            }
        }
        private void btn_BusyCheck_Click(object sender, RoutedEventArgs e)
        {
            fn_Ftdi_IsBusy();
        }

        private void fn_GiveTxtVal_16to2(uint Page0, uint Page1, uint Page2, uint Page3)
        {
            List<TextBox> lst_PageWrite_TextBox = new List<TextBox>
            { txt_40, txt_30,txt_20, txt_10,
            txt_41, txt_31,txt_21, txt_11,
            txt_42, txt_32,txt_22, txt_12,
            txt_43, txt_33,txt_23, txt_13,};

            //txt_Page0_16x.Text = "0x" + Page0.ToString("X8");
            //txt_Page1_16x.Text = "0x" + Page1.ToString("X8");
            //txt_Page2_16x.Text = "0x" + Page2.ToString("X8");
            //txt_Page3_16x.Text = "0x" + Page3.ToString("X8");
            int is32or16 = 32;
            if (cob_PromCtrl_23.SelectedIndex == 1) is32or16 = 16; // 如果選擇的是16位元模式

            string Bin0 = Convert.ToString(Page0, 2).PadLeft(is32or16, '0');
            string Bin1 = Convert.ToString(Page1, 2).PadLeft(is32or16, '0');
            string Bin2 = Convert.ToString(Page2, 2).PadLeft(is32or16, '0');
            string Bin3 = Convert.ToString(Page3, 2).PadLeft(is32or16, '0');
            string b = Bin0 + Bin1 + Bin2 + Bin3;

            if (b.Length > 64)
            {
                for (int i = 0; i < 16; i++)
                {
                    lst_PageWrite_TextBox[i].Text = b.Substring(i * 8, 8); // 取出每4位二進位數字
                }
            }
            else
            {
                for (int i = 0; i < 16; i++)
                {
                    lst_PageWrite_TextBox[i].Text = b.Substring(i * 4, 4); // 取出每4位二進位數字
                }
            }


        }

        private void fn_GivePageReadUiValue(uint pageData, int WhichPROM, int WhitchPage)
        {
            TextBox[][] textBoxes = new TextBox[][]
            {
                new TextBox[] { txt_10_Read ,txt_20_Read,txt_30_Read , txt_40_Read },
                new TextBox[] { txt_11_Read ,txt_21_Read,txt_31_Read , txt_41_Read },
                new TextBox[] { txt_12_Read ,txt_22_Read,txt_32_Read , txt_42_Read },
                new TextBox[] { txt_13_Read ,txt_23_Read,txt_33_Read , txt_43_Read }
            };
            textBoxes[WhitchPage][WhichPROM].Text = Convert.ToString(pageData, 16);
        }

        private void fn_LetUINull(int WhichPROM, int WhitchPage)
        {
            TextBox[][] textBoxes = new TextBox[][]
            {
                new TextBox[] { txt_10_Read ,txt_20_Read,txt_30_Read , txt_40_Read },
                new TextBox[] { txt_11_Read ,txt_21_Read,txt_31_Read , txt_41_Read },
                new TextBox[] { txt_12_Read ,txt_22_Read,txt_32_Read , txt_42_Read },
                new TextBox[] { txt_13_Read ,txt_23_Read,txt_33_Read , txt_43_Read }
            };
            textBoxes[WhitchPage][WhichPROM].Text = "null";
        }

        private void fn_RadBtn_PWprest()
        {
            txt_BigData.Clear();
            if (cob_PromCtrl_23.SelectedIndex == 0)
            {
                if (RadBtn_PWpreste1.IsChecked == true)
                {

                    txt_BigData.Text = "1111 1111 2222 2222 3333 3333 4444 4444 5555 5555 6666 6666 7777 7777 8888 8888 9999 9999 0000 0000 AAAA AAAA" +
                        " BBBB BBBB CCCC CCCC DDDD DDDD EEEE EEEE FFFF FFFF";
                    //一個TXT中可以放一個32位元的16進制數字

                }
                else if (RadBtn_PWpreste2.IsChecked == true)
                {
                    txt_BigData.Text = "9999 9999 0000 0000 AAAA AAAA" +
                        " BBBB BBBB CCCC CCCC DDDD DDDD EEEE EEEE FFFF FFFF 1111 1111 2222 2222 3333 3333 4444 4444 5555 5555 6666 6666 7777 7777 8888 8888";
                }
                else if (RadBtn_PWpreste3.IsChecked == true)
                {
                    txt_BigData.Text = "AAAA AAAA BBBB BBBB CCCC CCCC DDDD DDDD1111 1111 2222 2222 3333 3333 4444 4444 5555 5555 6666 6666 7777 7777 8888 8888 9999 9999 0000 0000  EEEE EEEE FFFF FFFF";
                }
                else if (RadBtn_PWpreste4.IsChecked == true)
                {
                    txt_BigData.Text = "5555 5555 6666 6666 1111 1111 2222 2222 3333 3333 4444 4444 7777 7777 8888 8888 DDDD DDDD EEEE EEEE FFFF FFFF 9999 9999 0000 0000 AAAA AAAA" +
                               " BBBB BBBB CCCC CCCC ";
                }
            }
            else if (cob_PromCtrl_23.SelectedIndex == 1)
            {
                if (RadBtn_PWpreste1.IsChecked == true)
                {
                    txt_BigData.Text = "1111 1111 2222 2222 3333 3333 4444 4444 5555 5555 6666 6666 7777 7777 8888 8888 9999 9999 0000 0000 AAAA AAAA" +
                        " BBBB BBBB CCCC CCCC DDDD DDDD EEEE EEEE FFFF FFFF";
                    //一個TXT中可以放一個32位元的16進制數字
                }
                else if (RadBtn_PWpreste2.IsChecked == true)
                {
                    txt_BigData.Text = "9999 9999 0000 0000 AAAA AAAA" +
                        " BBBB BBBB CCCC CCCC DDDD DDDD EEEE EEEE FFFF FFFF 1111 1111 2222 2222 3333 3333 4444 4444 5555 5555 6666 6666 7777 7777 8888 8888";
                }
                else if (RadBtn_PWpreste3.IsChecked == true)
                {
                    txt_BigData.Text = "AAAA AAAA BBBB BBBB CCCC CCCC DDDD DDDD1111 1111 2222 2222 3333 3333 4444 4444 5555 5555 6666 6666 7777 7777 8888 8888 9999 9999 0000 0000  EEEE EEEE FFFF FFFF";
                }
                else if (RadBtn_PWpreste4.IsChecked == true)
                {
                    txt_BigData.Text = "5555 5555 6666 6666 1111 1111 2222 2222 3333 3333 4444 4444 7777 7777 8888 8888 DDDD DDDD EEEE EEEE FFFF FFFF 9999 9999 0000 0000 AAAA AAAA" +
                               " BBBB BBBB CCCC CCCC ";
                }
            }

            TextBox[] txts = new TextBox[16]
               {
                txt_10,txt_20, txt_30, txt_40,
                txt_11,txt_21, txt_31, txt_41,
                txt_12,txt_22, txt_32, txt_42,
                txt_13,txt_23, txt_33, txt_43
               };
            string big = txt_BigData.Text.Replace(" ", "");
            for (int i = 0; i < 16; i++)
            {
                string part = big.Substring(i * 8, 8);
                txts[i].Text = part;
            }
        }

        private void RadBtn_PWprest_Click(object sender, RoutedEventArgs e)
        {
            fn_RadBtn_PWprest();
        }

        private void btn_PageWrite_2_Click(object sender, RoutedEventArgs e)
        {
            fn_GivePromCtrlParameters();
            // 組合 PROM_control 的值
            string str_addre = Class_promCtrlParameters.Bit31_26 + Class_promCtrlParameters.Bit25_24_I2C_clock_frequency + Class_promCtrlParameters.Bit23_Type + Class_promCtrlParameters.Bit22_20_ID +
                     "0" +
                     Class_promCtrlParameters.Bit18 + Class_promCtrlParameters.Bit17_16_ByteSize + Class_promCtrlParameters.Bit15_0_Address;
            int RetryConunt = 0;
            int nowPage = 0;
            while (true)
            {
                if (nowPage == 0 && RetryConunt < 5)
                {
                    if (fn_Ftdi_IsBusy(0) == 0)
                    {
                        fn_Ftdi_Write(0x0A, Convert.ToUInt64(txt_10.Text, 16));
                        fn_Ftdi_Write(0x0B, Convert.ToUInt64(txt_20.Text, 16));
                        if (cob_PromCtrl_17to16.SelectedIndex > 0)
                        {
                            fn_Ftdi_Write(0x0C, Convert.ToUInt64(txt_30.Text, 16));
                            fn_Ftdi_Write(0x0D, Convert.ToUInt64(txt_40.Text, 16));
                        }

                        nowPage++;
                    }
                    else
                    {
                        Thread.Sleep(100);
                        RetryConunt++;
                    }
                }

                else if (nowPage == 1 && RetryConunt < 5 && cob_PromCtrl_17to16.SelectedIndex > 1)
                {
                    if (fn_Ftdi_IsBusy(1) == 0)
                    {
                        fn_Ftdi_Write(0x0A, Convert.ToUInt64(txt_11.Text, 16));
                        fn_Ftdi_Write(0x0B, Convert.ToUInt64(txt_21.Text, 16));
                        fn_Ftdi_Write(0x0C, Convert.ToUInt64(txt_31.Text, 16));
                        fn_Ftdi_Write(0x0D, Convert.ToUInt64(txt_41.Text, 16));
                        // 轉換為 uint後PROM_control
                        //  fn_Ftdi_Write(0x09, Convert.ToUInt32(str_addre, 2));
                        nowPage++;
                    }
                    else
                    {
                        Thread.Sleep(100);
                        RetryConunt++;
                    }
                }

                else if (nowPage == 2 && RetryConunt < 5 && cob_PromCtrl_17to16.SelectedIndex > 2)
                {
                    if (fn_Ftdi_IsBusy(2) == 0)
                    {
                        fn_Ftdi_Write(0x0A, Convert.ToUInt64(txt_12.Text, 16));
                        fn_Ftdi_Write(0x0B, Convert.ToUInt64(txt_22.Text, 16));
                        fn_Ftdi_Write(0x0C, Convert.ToUInt64(txt_32.Text, 16));
                        fn_Ftdi_Write(0x0D, Convert.ToUInt64(txt_42.Text, 16));
                        // 轉換為 uint後PROM_control
                        //  fn_Ftdi_Write(0x09, Convert.ToUInt32(str_addre, 2));
                        nowPage++;
                    }
                    else
                    {
                        Thread.Sleep(100);
                        RetryConunt++;
                    }
                }

                else if (nowPage == 3 && RetryConunt < 5 && cob_PromCtrl_17to16.SelectedIndex > 2)
                {
                    if (fn_Ftdi_IsBusy(3) == 0)
                    {
                        fn_Ftdi_Write(0x0A, Convert.ToUInt64(txt_13.Text, 16));
                        fn_Ftdi_Write(0x0B, Convert.ToUInt64(txt_23.Text, 16));
                        fn_Ftdi_Write(0x0C, Convert.ToUInt64(txt_33.Text, 16));
                        fn_Ftdi_Write(0x0D, Convert.ToUInt64(txt_43.Text, 16));
                        // 轉換為 uint後PROM_control

                        nowPage++;
                    }
                    else
                    {
                        Thread.Sleep(100);
                        RetryConunt++;
                    }
                }

                else if (RetryConunt >= 5)
                {
                    MessageBox.Show("寫入失敗，請重新嘗試");
                    break;
                }

                else
                {
                    fn_Ftdi_Write(0x09, Convert.ToUInt32(str_addre, 2));
                    MessageBox.Show("全部寫入完成");
                    break;
                }
            }



        }

        private void txt_Page0_16To2(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //try
            //{
            //    if (e.Key == Key.Enter)
            //    {
            //        fn_GiveTxtVal_16to2(
            //                          Convert.ToUInt32(txt_Page0_16x.Text.Substring(2), 16),
            //                          Convert.ToUInt32(txt_Page1_16x.Text.Substring(2), 16),
            //                          Convert.ToUInt32(txt_Page2_16x.Text.Substring(2), 16),
            //                          Convert.ToUInt32(txt_Page3_16x.Text.Substring(2), 16));
            //    }


            //}
            //catch (Exception ex)
            //{

            //}
        }

        private void txt_Page0_2To16(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            List<List<string>> allNames = new List<List<string>>
              {
                     new List<string> { "txt_40", "txt_30", "txt_20", "txt_10" },
                     new List<string> { "txt_41", "txt_31", "txt_21", "txt_11" },
                     new List<string> { "txt_42", "txt_32", "txt_22", "txt_12" },
                    new List<string> { "txt_43", "txt_33", "txt_23", "txt_13" }
               };

            List<string> hexTextBoxes = new List<string>    {
        "txt_Page0_16x",
        "txt_Page1_16x",
        "txt_Page2_16x",
        "txt_Page3_16x"    };

            for (int i = 0; i < allNames.Count; i++)
            {
                string binary = "";

                foreach (string name in allNames[i])
                {
                    var txtBox = this.FindName(name) as TextBox;
                    if (txtBox == null) return;

                    string value = txtBox.Text.Trim();

                    // 自動補足4位，不管原本長度多少
                    if (value.Length > 4) value = value.Substring(0, 4);
                    value = value.PadLeft(4, '0');

                    // 只判斷是不是全部0或1
                    if (!Regex.IsMatch(value, "^[01]+$"))
                    {
                        // 如果非二進制字串，仍然繼續下一組，或你也可以顯示錯誤
                        value = "0000";  // 直接用0代替
                    }

                    binary += value;
                }

                // 一律補足16位（安全保險）
                binary = binary.PadLeft(16, '0');

                uint hexValue = Convert.ToUInt32(binary, 2);
                string hexString = "0x" + hexValue.ToString("X4");

                var resultBox = this.FindName(hexTextBoxes[i]) as TextBox;
                if (resultBox != null)
                {
                    resultBox.Text = hexString;
                }
            }
        }

        private void fn_Init_register()
        {
            txt_10.KeyDown += txt_Page0_2To16;
            txt_11.KeyDown += txt_Page0_2To16;
            txt_12.KeyDown += txt_Page0_2To16;
            txt_13.KeyDown += txt_Page0_2To16;

            txt_20.KeyDown += txt_Page0_2To16;
            txt_21.KeyDown += txt_Page0_2To16;
            txt_22.KeyDown += txt_Page0_2To16;
            txt_23.KeyDown += txt_Page0_2To16;

            txt_30.KeyDown += txt_Page0_2To16;
            txt_31.KeyDown += txt_Page0_2To16;
            txt_32.KeyDown += txt_Page0_2To16;
            txt_33.KeyDown += txt_Page0_2To16;

            txt_40.KeyDown += txt_Page0_2To16;
            txt_41.KeyDown += txt_Page0_2To16;
            txt_42.KeyDown += txt_Page0_2To16;
            txt_43.KeyDown += txt_Page0_2To16;

        }

        private void cob_PromCtrl_combox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (window_isLoaded)
            {
                Properties.Settings.Default.LastSelected_17to16 = cob_PromCtrl_17to16.SelectedIndex;
                Properties.Settings.Default.LastSelected_19 = cob_PromCtrl_19.SelectedIndex;
                Properties.Settings.Default.LastSelected_22to20 = cob_PromCtrl_22to20.SelectedIndex;
                Properties.Settings.Default.LastSelected_23 = cob_PromCtrl_23.SelectedIndex;
                Properties.Settings.Default.LastSelected_25to24 = cob_PromCtrl_25to24.SelectedIndex;
                Properties.Settings.Default.Save();
                fn_GivePromCtrlParameters();


            }
        }

        private void txt_PromCtrl_15to0_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (window_isLoaded)
            {
                Properties.Settings.Default.LastInputAddrese = txt_PromCtrl_15to0.Text.Trim();
                Properties.Settings.Default.Save();
                fn_GivePromCtrlParameters();
            }


        }
        private void fn_GivePromCtrlParameters()//這個好用
        {

            if (window_isLoaded)
            {
                try
                {
                    string strFor15_0_binaryString = "";

                    if (txt_PromCtrl_15to0.Text.Length < 16)
                    {//16進制
                     //將16進制轉為2進制
                        strFor15_0_binaryString = Convert.ToString(Convert.ToUInt16(txt_PromCtrl_15to0.Text, 16), 2).PadLeft(16, '0');
                    }
                    else
                    {//2進制
                        strFor15_0_binaryString = txt_PromCtrl_15to0.Text.Trim();
                    }

                    ComboBoxItem selectedItem1716 = (ComboBoxItem)cob_PromCtrl_17to16.SelectedItem;
                    ComboBoxItem selectedItem19 = (ComboBoxItem)cob_PromCtrl_19.SelectedItem;
                    ComboBoxItem selectedItem2220 = (ComboBoxItem)cob_PromCtrl_22to20.SelectedItem;
                    ComboBoxItem selectedItem23 = (ComboBoxItem)cob_PromCtrl_23.SelectedItem;
                    ComboBoxItem selectedItem2524 = (ComboBoxItem)cob_PromCtrl_25to24.SelectedItem;

                    Class_promCtrlParameters = new PromCtrlParameters
                    {
                        Bit15_0_Address = strFor15_0_binaryString, // 16 bits, default to 0
                        Bit17_16_ByteSize = selectedItem1716.Tag.ToString(), // 2 bits, default to 0

                        Bit19_RDWR = selectedItem19.Tag.ToString(), // 2 bits, default to 0
                        Bit22_20_ID = selectedItem2220.Tag.ToString(), // 3 bits, default to 0
                        Bit23_Type = selectedItem23.Tag.ToString(), // 1 bit, default to 0
                        Bit25_24_I2C_clock_frequency = selectedItem2524.Tag.ToString(), // 2 bits, default to 0
                    };

                    txt_PromCtrl_bin.Text =
                        Class_promCtrlParameters.Bit31_26 +
                        Class_promCtrlParameters.Bit25_24_I2C_clock_frequency +
                        Class_promCtrlParameters.Bit23_Type +
                        Class_promCtrlParameters.Bit22_20_ID +
                        Class_promCtrlParameters.Bit19_RDWR +
                        Class_promCtrlParameters.Bit18 +
                        Class_promCtrlParameters.Bit17_16_ByteSize +
                        Class_promCtrlParameters.Bit15_0_Address;

                    // 將二進制字串轉換為16進制uint

                    uint promCtrlParameters = Convert.ToUInt32(txt_PromCtrl_bin.Text, 2);
                    txt_PromCtrl_hex.Text = "0x" + promCtrlParameters.ToString("X8");
                    uint_PromCtrl_AddressVar = Convert.ToUInt32(txt_PromCtrl_bin.Text, 2);

                    // 將二進制字串轉換為DEx進制uint
                    txt_PromCtrl_dec.Text = promCtrlParameters.ToString("D");

                }
                catch (Exception ex)
                {

                }
            }
        }


        class PromCtrlParameters
        {
            public string Bit15_0_Address { get; set; }
            public string Bit17_16_ByteSize { get; set; }
            public string Bit18 = "0";
            public string Bit19_RDWR { get; set; }
            public string Bit22_20_ID { get; set; }
            public string Bit23_Type { get; set; }
            public string Bit25_24_I2C_clock_frequency { get; set; }
            public string Bit31_26 = "000000"; // 6 bits, default to 0
        }

        private void RadBtn_ReadP1_Click(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
        }

        private void btn_TrunBigToSmall_Click(object sender, RoutedEventArgs e)
        {


        }

        private void txt_BigData_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    TextBox[] txts = new TextBox[16]
              {
                txt_10,txt_20, txt_30, txt_40,
                txt_11,txt_21, txt_31, txt_41,
                txt_12,txt_22, txt_32, txt_42,
                txt_13,txt_23, txt_33, txt_43
              };
                    string big = txt_BigData.Text.Replace(" ", "");
                    for (int i = 0; i < 16; i++)
                    {
                        string part = big.Substring(i * 8, 8);
                        txts[i].Text = part;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
