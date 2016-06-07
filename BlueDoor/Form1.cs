using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// 全局键盘事件监听相关
using 壁纸管家;
using System.Runtime.InteropServices;   //调用WINDOWS API函数时要用到
using Microsoft.Win32;  //写入注册表时要用到

namespace BlueDoor
{
    public partial class Form1 : Form
    {
        private string Status, BlueToothReceivedData;
        private KeyboardHook k_hook;

        //Create a serial port for bluetooth
        private SerialPort BluetoothConnection = new SerialPort();

        public Form1()
        {
            InitializeComponent();

            // 全局键盘事件监听相关
            k_hook = new KeyboardHook();
            k_hook.KeyDownEvent += new KeyEventHandler(hook_KeyDown);//钩住键按下
            k_hook.Start();//安装键盘钩子

            //Get all port list for selection
            //获得所有的端口列表，并显示在列表内
            cbPort.Items.Clear();
            string[] Ports = SerialPort.GetPortNames();

            for (int i = 0; i < Ports.Length; i++)
            {
                string s = Ports[i].ToUpper();
                Regex reg = new Regex("[^COM\\d]", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                s = reg.Replace(s, "");

                cbPort.Items.Add(s);
            }
            if (Ports.Length >= 1) cbPort.SelectedIndex = 0;
        }

        //3.判断输入键值（实现KeyDown事件）
        private void hook_KeyDown(object sender, KeyEventArgs e)
        {
            //判断按下的键（Alt + Z）,开锁
            if (e.KeyValue == (int)Keys.X && (int)Control.ModifierKeys == (int)Keys.Alt)
            {
                //System.Windows.Forms.MessageBox.Show("按下了指定快捷键组合");
                if (!BluetoothConnection.IsOpen)
                    bluetoothConnect(); 

                while (!BluetoothConnection.IsOpen) ;
                Thread.Sleep(500);       
                BlueToothDataSend("*unlock#");
                showIconTips("开锁");
            }

            //判断按下的键（Alt + X）松锁
            if (e.KeyValue == (int)Keys.C && (int)Control.ModifierKeys == (int)Keys.Alt)
            {
                //System.Windows.Forms.MessageBox.Show("按下了指定快捷键组合");
                if (!BluetoothConnection.IsOpen)
                    bluetoothConnect();

                while (!BluetoothConnection.IsOpen) ;
                Thread.Sleep(200);
                BlueToothDataSend("*lock#");
                showIconTips("松锁");
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            timer2.Enabled = false;
            bluetoothConnect();
        }

        private void BlueToothDataReceived(object o, SerialDataReceivedEventArgs e)
        {
            // 无需处理收到的数据
        }

        private void BlueToothDataSend(string data)
        {
            
            if (BluetoothConnection.IsOpen)
            {
                timer2.Enabled = false;  // 关闭自动断开定时器
                BluetoothConnection.Write(data);  // 发送字符串
                timer2.Enabled = true;   // 重开定时器
            }
            else
            {
                Status = "端口被关闭，消息发送失败";
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            bluetoothDisConnect();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            BlueToothDataSend(txtMessage.Text);
        }

        private void btnUnlock_Click(object sender, EventArgs e)
        {
            BlueToothDataSend("*unlock#");
        }

        private void btnLock_Click(object sender, EventArgs e)
        {
            BlueToothDataSend("*lock#");
        }

        private void bluetoothConnect()
        {
            if (!BluetoothConnection.IsOpen)
            {
                //Start

                Status = "正在连接蓝牙设备";
                showIconTips("正在连接蓝牙设备");
                BluetoothConnection = new SerialPort();
                btnConnect.Enabled = false;
                BluetoothConnection.PortName = cbPort.SelectedItem.ToString();
                BluetoothConnection.Open();
                BluetoothConnection.ReadTimeout = 10000;
                BluetoothConnection.DataReceived += new SerialDataReceivedEventHandler(BlueToothDataReceived);
                Status = "蓝牙连接成功";
                showIconTips("蓝牙连接成功");
            }
        }

        private void bluetoothDisConnect()
        {
            //Stop
            Status = "正在断开蓝牙设备";
            showIconTips("正在断开蓝牙设备");
            BluetoothConnection.Close();
            //BluetoothConnection.Dispose();
            //BluetoothConnection = null;
            btnConnect.Enabled = true;
            Status = "蓝牙断开成功";
            showIconTips("蓝牙断开成功");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.Visible = false;
            this.notifyIcon1.Visible = true;
            showIconTips("锁锁还在后台运行哦");  // 托盘图标显示消息
            e.Cancel = true;  // 不关闭程序
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = true;
                this.ShowInTaskbar = true;  //显示在系统任务栏 
                this.WindowState = FormWindowState.Normal;  //还原窗体 
                //this.notifyIcon1.Visible = false;  //托盘图标隐藏 
            }
        }

        private void ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            //点击"是(YES)"退出程序
            if (MessageBox.Show("确定要退出程序?", "提示",
                        System.Windows.Forms.MessageBoxButtons.YesNo,
                        System.Windows.Forms.MessageBoxIcon.Warning)
                == System.Windows.Forms.DialogResult.Yes)
            {
                notifyIcon1.Visible = false;   //设置图标不可见
                this.Close();                  //关闭窗体
                this.Dispose();                //释放资源
                Application.Exit();            //关闭应用程序窗体
                k_hook.Stop();                 //卸载键盘钩子
            }
        }

        private void ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = true;
                this.ShowInTaskbar = true;  //显示在系统任务栏 
                this.WindowState = FormWindowState.Normal;  //还原窗体 
                //this.notifyIcon1.Visible = false;  //托盘图标隐藏 
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            txtState.Text = Status;
            //txtBlueToothMessage.Text = BlueToothReceivedData;
        }

        private void timer2_Tick(object sender, EventArgs e)  // 自动断开定时器，间隔1分钟
        {
            if (BluetoothConnection.IsOpen)
                bluetoothDisConnect();
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("蓝牙门锁电脑控制端-仲恺D446专用！\n" + 
                            "                                                             ——by邵国际");
        }

        private void 开锁ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!BluetoothConnection.IsOpen)
                bluetoothConnect();

            while (!BluetoothConnection.IsOpen) ;
            Thread.Sleep(500);
            BlueToothDataSend("*unlock#");
            showIconTips("开锁");
        }

        private void 松锁ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!BluetoothConnection.IsOpen)
                bluetoothConnect();

            while (!BluetoothConnection.IsOpen) ;
            Thread.Sleep(200);
            BlueToothDataSend("*lock#");
            showIconTips("松锁");
        }

        private void showIconTips(string tip)  // 托盘图标显示消息
        {
            if (this.WindowState == FormWindowState.Minimized)  // 窗口最小化
                notifyIcon1.ShowBalloonTip(100, "提示", tip, ToolTipIcon.None);  // 托盘图标显示消息
        }
    }
}
