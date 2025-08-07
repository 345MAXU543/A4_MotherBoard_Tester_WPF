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

            fn_GiveTxtVal_16to2(0x1234, 0x4321, 0x5678, 0x8756);
            fn_Init_register();


            cob_PromCtrl_17to16.SelectedIndex = Properties.Settings.Default.LastSelected_17to16;
            cob_PromCtrl_19.SelectedIndex = Properties.Settings.Default.LastSelected_19;
            cob_PromCtrl_22to20.SelectedIndex = Properties.Settings.Default.LastSelected_22to20;
            cob_PromCtrl_23.SelectedIndex = Properties.Settings.Default.LastSelected_23;
            cob_PromCtrl_25to24.SelectedIndex = Properties.Settings.Default.LastSelected_25to24;

            window_isLoaded = true; // 確保在設定完 ComboBox 之後才設為 true

            fn_GivePromCtrlParameters();
            #region 清空所有txt_xx_Read內的text
            txt_Page0_16x_Read.Clear();
            txt_Page1_16x_Read.Clear();
            txt_Page2_16x_Read.Clear();
            txt_Page3_16x_Read.Clear();
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
                return ("無法取得裝置清單");
            }

            if (Globle_Ftdi.IsOpen != true)
            {
                if (Globle_Ftdi.OpenByIndex((uint)Index) != FTDI.FT_STATUS.FT_OK)
                {
                    MessageBox.Show("無法開啟 FTDI 裝置，請檢查連接或驅動程式。");
                }
            }
            status = Globle_Ftdi.GetDeviceList(deviceList);


            if (status != FTDI.FT_STATUS.FT_OK || !Globle_Ftdi.IsOpen)
            {
                Globle_Ftdi.GetDescription(out string ftdiDescription);
                return ("無法開啟裝置" + ftdiDescription);
            }
            else if (status == FTDI.FT_STATUS.FT_OK && Globle_Ftdi.IsOpen)
            {
                Globle_Ftdi.GetDescription(out string ftdiDescription);
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

        private void fn_Ftdi_Write(byte command, ulong DATA)
        {
            byte[] Send = new byte[6];
            Send[0] = (byte)(((DATA >> 28) & 0x7F) | 0x80); // 取 bit 31~28（4 位）
            Send[1] = (byte)(((DATA >> 21) & 0x7F) | 0x80); // 取 bit 27~21（7 位）
            Send[2] = (byte)(((DATA >> 14) & 0x7F) | 0x80); // 取 bit 20~14（7 位）
            Send[3] = (byte)(((DATA >> 7) & 0x7F) | 0x80); // 取 bit 13~7（7 位）
            Send[4] = (byte)((DATA & 0x7F) | 0x80);         // 取 bit 6~0（7 位）
            Send[5] = command;                              // 命令放最後

            if (Send != null && Send.Length >= 6)
            {
                // 清空佇列
                //Purge();
                uint bytesWritten = 0;
                if (Globle_Ftdi.Write(Send, Send.Length, ref bytesWritten) != FTDI.FT_STATUS.FT_OK)
                {
                    MessageBox.Show("寫入失敗");
                }
            }
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



        private void fn_Ftdi_Read(out uint returnData, out byte address)
        {
            byte[] recv = new byte[6];
            uint bytesRead = 0;
            Globle_Ftdi.SetTimeouts(100, 100);
            FTDI.FT_STATUS status = Globle_Ftdi.Read(recv, 6, ref bytesRead);

            if (status != FTD2XX_NET.FTDI.FT_STATUS.FT_OK || bytesRead < 6)
            {
                MessageBox.Show("讀取失敗或資料不足");
            }

            returnData = 0;
            address = 0;

            byte[] afterTransfer = new byte[4];
            byte high, low;

            // 解碼 AfterTransfer[0]
            high = (byte)(recv[0] & 0x0F);                    // bit 3~0
            low = (byte)((recv[1] >> 3) & 0x0F);              // bit 6~3
            afterTransfer[0] = (byte)((high << 4) | low);     // bits 31~24

            // 解碼 AfterTransfer[1]
            high = (byte)(recv[1] & 0x07);                    // bit 2~0
            low = (byte)((recv[2] >> 2) & 0x1F);              // bit 6~2
            afterTransfer[1] = (byte)((high << 5) | low);     // bits 23~16

            // 解碼 AfterTransfer[2]
            high = (byte)(recv[2] & 0x03);                    // bit 1~0
            low = (byte)((recv[3] >> 1) & 0x3F);              // bit 6~1
            afterTransfer[2] = (byte)((high << 6) | low);     // bits 15~8

            // 解碼 AfterTransfer[3]
            high = (byte)(recv[3] & 0x01);                    // bit 0
            low = (byte)(recv[4] & 0x7F);                     // bit 6~0
            afterTransfer[3] = (byte)((high << 7) | low);     // bits 7~0

            // 組合成 uint
            returnData =
                ((uint)afterTransfer[0] << 24) |
                ((uint)afterTransfer[1] << 16) |
                ((uint)afterTransfer[2] << 8) |
                afterTransfer[3];

            // 第6個 byte 是 address
            address = recv[5];

        }

        /// <summary>
        /// 0 : OK , 1 : Busy , 2 : ERR , 3 : Other Error
        /// </summary>
        /// <returns></returns>
        private int fn_Ftdi_IsBusy()
        {
            fn_Ftdi_Write(0x00, 0x09);
            Thread.Sleep(50); // 等待讀取完成
            fn_Ftdi_Read(out uint Data, out byte add);
            if (Data == 64)//1000000//OK
            {
                return 0;
            }
            else if (Data == 65)//1000001//Busy
            {
                return 1;
            }
            else if (Data == 66)//1000010//ERR
            {
                return 2;
            }
            else//其他錯誤
            {
                return 3;
            }

            #region UI

            if (Data == 64)
            {
                lb_BusyCheckRes.Background
                    = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#FF02F502");
                lb_BusyCheckRes.Content = "Not busy !!!";
            }
            else if (Data == 65)
            {
                lb_BusyCheckRes.Background
                    = System.Windows.Media.Brushes.Yellow;
                lb_BusyCheckRes.Content = "Busy !!!";
            }
            else if (Data == 66)
            {
                lb_BusyCheckRes.Background
                    = System.Windows.Media.Brushes.Red;
                lb_BusyCheckRes.Content = "Error !!!";
            }
            else
            {
                lb_BusyCheckRes.Background
                    = System.Windows.Media.Brushes.Gray;
                lb_BusyCheckRes.Content = "Unknown status !!!";
            }

            #endregion
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


        /// <summary>
        /// PageRead 函數
        /// </summary>
        /// <param name="ID">
        ///ID = 1 : 不使用
        ///ID = 2 : 主機板
        ///ID = 3 : 不使用
        ///ID = 4 : 晶片鎖 ENC#1 EEPROM 24LC64
        ///ID = 5 : 晶片鎖 ENC#2 EEPROM 24LC64
        ///ID = 6 : 晶片鎖 LC#1 EEPROM 24LC64
        ///ID = 7 : 晶片鎖 LC#2 EEPROM 24LC64</param>
        /// <param name="ReadSize">ReadSize 只能是 8, 16, 32, 64</param>
        /// <param name="StartAddress">開始讀取的記憶體位置</param>
        /// <param name="PageData">Feed back data (OUTPUT)</param>
        /// <param name="PageAddress">Address of the feed back data (OUTPUT)</param>
        private void fn_Ftdi_PageRead( uint read_address,out uint PageData, out byte PageAddress)
        {
            


            //0x0230000 ->  ID = 2  , 3 = 64byte讀取資料大小, 從0000開始讀取
            //uint Address = 0x0230000;

            // uint Address = StartAddress;
            // Address = ID * 0x100000 + Address;
            // Address = ReadSize * 0x10000 + Address;
            //Address = fn_SetBitToOne(Address, 23);
           string str_addre =
                     Class_promCtrlParameters.Bit31_26 +
                     Class_promCtrlParameters.Bit25_24_I2C_clock_frequency +
                     Class_promCtrlParameters.Bit23_Type +
                     Class_promCtrlParameters.Bit22_20_ID +
                     "1" +
                     Class_promCtrlParameters.Bit18 +
                     Class_promCtrlParameters.Bit17_16_ByteSize +
                     Class_promCtrlParameters.Bit15_0_Address;

            uint Address = Convert.ToUInt32(str_addre, 2);


            if (fn_Ftdi_IsBusy() == 0)
            {
                fn_Ftdi_Write(0x09, Address); //PROM_CTRL
                Thread.Sleep(500);
                if (fn_Ftdi_IsBusy() == 0)
                {
                    fn_Ftdi_Write(0x00, read_address); //SINGLE_READ
                    Thread.Sleep(500);
                }
            }
            fn_Ftdi_Read(out PageData, out PageAddress);
        }

        public static uint fn_SetBitToZero(uint value, int index)
        {
            return value & ~(1u << index);
        }

        public static uint fn_SetBitToOne(uint value, int index)
        {
            return value | (1u << index);
        }

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

            fn_Ftdi_Write(0x0A, PROM_WRITE_Data1);// PROM_WRITE1
            fn_Ftdi_Write(0x0B, PROM_WRITE_Data2);// PROM_WRITE2
            fn_Ftdi_Write(0x0C, PROM_WRITE_Data3);// PROM_WRITE3
            fn_Ftdi_Write(0x0D, PROM_WRITE_Data4);// PROM_WRITE4
            int timeout = 10000; // 5秒超時
            while (true)
            {
                if (fn_Ftdi_IsBusy() == 0)
                {
                    break;
                }
            }



            //uint Address = 0x0230000;
            //Address = fn_SetBitToZero(Address, 23);//這是韌體改版前的RD_WR
            fn_Ftdi_Write(0x09, uint_PromCtrl_AddressVar); // PROM_control


            var startTime = Environment.TickCount;
            var timeoutMs = 10000; // 設定超時時間為 5 秒
            while (true)
            {
                if (fn_Ftdi_IsBusy() == 0)
                {
                    break;
                }
                if (Environment.TickCount - startTime > timeoutMs)
                {
                    return false; // Timeout 發生
                }

                Thread.Sleep(1); // 加這個避免 CPU 100% 占用
            }
            return true;


            //uint returnData_A;
            //byte address_A;
            //uint returnData_B;
            //byte address_B;
            //uint returnData_C;
            //byte address_C;
            //uint returnData_D;
            //byte address_D;

            ////這邊回應的是DEC(十進制)
            //fn_Ftdi_PageRead(2, 64, 0x0000, 0x0A, out uint PageDataA, out byte PageAddressA);
            //fn_Ftdi_PageRead(2, 64, 0x0004, 0x0B, out uint PageDataB, out byte PageAddressB);
            //fn_Ftdi_PageRead(2, 64, 0x0000, 0x0C, out uint PageDataC, out byte PageAddressC);
            //fn_Ftdi_PageRead(2, 64, 0x0000, 0x0D, out uint PageDataD, out byte PageAddressD);

        }

        private void btn_PageWrite_Click(object sender, RoutedEventArgs e)
        {
            fn_Ftdi_PageWrite(0x1234, 0x1234, 0x1234, 0x1234);

        }

        private void btn_PageRead_Click(object sender, RoutedEventArgs e)
        {
            if(RadBtn_ReadP1.IsChecked == true)
                {
                fn_Ftdi_PageRead(0x0A, out uint PageData, out byte PageAddress);
                fn_GivePageReadUiValue(PageData, 0);
            }
            else if (RadBtn_ReadP2.IsChecked == true)
            {
                fn_Ftdi_PageRead(0x0B, out uint PageData, out byte PageAddress);
                fn_GivePageReadUiValue(PageData, 1);
            }
            else if (RadBtn_ReadP3.IsChecked == true)
            {
                fn_Ftdi_PageRead(0x0C, out uint PageData, out byte PageAddress);
                fn_GivePageReadUiValue(PageData, 2);
            }
            else if (RadBtn_ReadP4.IsChecked == true)
            {
                fn_Ftdi_PageRead(0x0D, out uint PageData, out byte PageAddress);
                fn_GivePageReadUiValue(PageData, 3);
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

            txt_Page0_16x.Text = "0x" + Page0.ToString("X4");
            txt_Page1_16x.Text = "0x" + Page1.ToString("X4");
            txt_Page2_16x.Text = "0x" + Page2.ToString("X4");
            txt_Page3_16x.Text = "0x" + Page3.ToString("X4");
            string Bin0 = Convert.ToString(Page0, 2).PadLeft(16, '0');
            string Bin1 = Convert.ToString(Page1, 2).PadLeft(16, '0');
            string Bin2 = Convert.ToString(Page2, 2).PadLeft(16, '0');
            string Bin3 = Convert.ToString(Page3, 2).PadLeft(16, '0');
            string b = Bin0 + Bin1 + Bin2 + Bin3;

            if (b.Length > 64)
            {

            }
            else
            {
                for (int i = 0; i < 16; i++)
                {
                    lst_PageWrite_TextBox[i].Text = b.Substring(i * 4, 4); // 取出每4位二進位數字
                }
            }


        }

        private void fn_GivePageReadUiValue(uint pageData , int WhitchPage)
        {
            //清空所有txt_xx_Read內的text
            #region 清空所有txt_xx_Read內的text
            txt_Page0_16x_Read.Clear();
            txt_Page1_16x_Read.Clear();
            txt_Page2_16x_Read.Clear();
            txt_Page3_16x_Read.Clear();
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


            // 將 pageData 轉換為 2 進制字串
            string binaryString = Convert.ToString(pageData, 2).PadLeft(16, '0');
            // 將二進制字串分割成 4 位一組
            string[] binaryGroups = new string[4];
            for (int i = 0; i < 4; i++)
            {
                binaryGroups[i] = binaryString.Substring(i * 4, 4);
            }

            if (WhitchPage == 0)
            {
                txt_Page0_16x_Read.Text = "0x" + pageData.ToString("X4");
                txt_40_Read.Text = binaryGroups[0];
                txt_30_Read.Text = binaryGroups[1];
                txt_20_Read.Text = binaryGroups[2];
                txt_10_Read.Text = binaryGroups[3];
            }
            else if (WhitchPage == 1)
            {
                txt_Page1_16x_Read.Text = "0x" + pageData.ToString("X4");
                txt_41_Read.Text = binaryGroups[0];
                txt_31_Read.Text = binaryGroups[1];
                txt_21_Read.Text = binaryGroups[2];
                txt_11_Read.Text = binaryGroups[3];
            }
            else if (WhitchPage == 2)
            {
                txt_Page2_16x_Read.Text = "0x" + pageData.ToString("X4");
                txt_42_Read.Text = binaryGroups[0];
                txt_32_Read.Text = binaryGroups[1];
                txt_22_Read.Text = binaryGroups[2];
                txt_12_Read.Text = binaryGroups[3];

            }
            else if (WhitchPage == 3)
            {
                txt_Page3_16x_Read.Text = "0x" + pageData.ToString("X4");
                txt_43_Read.Text = binaryGroups[0];
                txt_33_Read.Text = binaryGroups[1];
                txt_23_Read.Text = binaryGroups[2];
                txt_13_Read.Text = binaryGroups[3];
            }
        }

        private void RadBtn_PWprest_Click(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (RadBtn_PWpreste1.IsChecked == true)
            {
                fn_GiveTxtVal_16to2(0x1234, 0x4321, 0x5678, 0x8756);
            }
            else if (RadBtn_PWpreste2.IsChecked == true)
            {
                fn_GiveTxtVal_16to2(0x4321, 0x1234, 0x8756, 0x5678);
            }
            else if (RadBtn_PWpreste3.IsChecked == true)
            {
                fn_GiveTxtVal_16to2(0x5678, 0x8756, 0x1234, 0x4321);
            }
            else if (RadBtn_PWpreste4.IsChecked == true)
            {
                fn_GiveTxtVal_16to2(0x8756, 0x5678, 0x4321, 0x1234);
            }

        }

        private void btn_PageWrite_2_Click(object sender, RoutedEventArgs e)
        {
            fn_GivePromCtrlParameters();
            uint d1 = Convert.ToUInt32(txt_Page0_16x.Text.Substring(2), 16);
            uint d2 = Convert.ToUInt32(txt_Page1_16x.Text.Substring(2), 16);
            uint d3 = Convert.ToUInt32(txt_Page2_16x.Text.Substring(2), 16);
            uint d4 = Convert.ToUInt32(txt_Page3_16x.Text.Substring(2), 16);

            if (!fn_Ftdi_PageWrite(d1, d2, d3, d4))
            {
                MessageBox.Show("寫入失敗，請檢查連接或裝置狀態。");
            }
        }



        private void txt_Page0_16To2(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    fn_GiveTxtVal_16to2(
                                      Convert.ToUInt32(txt_Page0_16x.Text.Substring(2), 16),
                                      Convert.ToUInt32(txt_Page1_16x.Text.Substring(2), 16),
                                      Convert.ToUInt32(txt_Page2_16x.Text.Substring(2), 16),
                                      Convert.ToUInt32(txt_Page3_16x.Text.Substring(2), 16));
                }


            }
            catch (Exception ex)
            {

            }
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


    }
}
