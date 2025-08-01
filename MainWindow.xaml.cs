using FTD2XX_NET;
using System;
using System.Threading;
using System.Windows;

namespace A4_MotherBoard_Tester_WPF
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        FTDI Globle_Ftdi = new FTDI();
        int iTxtFeedBack_Count = 1;

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

        private void fn_Ftdi_Write(byte command, uint DATA)
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
                Purge();
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

        private void fn_Ftdi_PageRead()
        {

        }

        private void fn_Ftdi_PageWrite(byte command, uint DATA)
        {
            command = (byte)(command << 8); // 將命令轉換為 8 位元格式
            fn_Ftdi_Write(command, DATA);
        }
    }
}
