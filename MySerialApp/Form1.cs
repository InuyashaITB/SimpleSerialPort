using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MySerialApp
{
    public partial class Form1 : Form
    {
        private ArrayList Logs = new ArrayList();
        private SerialPort serialPort;
        private Timer sendTimer;
        private DateTime LastLogTime = DateTime.MinValue;

        private List<byte> TXData = new List<byte>();
        private List<byte> RXData = new List<byte>();

        private int _SentCount = 0;
        private int SentCount { get { return _SentCount; } set { _SentCount = value; labelTXCount.Text = $"Sent {_SentCount} Bytes"; } }

        private int _RXCount = 0;
        private int RXCount { get { return _RXCount; } set { _RXCount = value; labelRXCount.Text = $"Received {_RXCount} Bytes"; } }

        public Form1()
        {
            InitializeComponent();
            cbCOMPort.Items.AddRange(SerialPort.GetPortNames());
            sendTimer = new Timer();
            sendTimer.Interval = (int)numericUpDown1.Value;
            sendTimer.Tick += SendTimer_Tick;
        }

        private async void SendTimer_Tick(object sender, EventArgs e)
        {
            var logItem = new LogItem();
            logItem.Timestamp = DateTime.Now;
            logItem.TX = string.Join(",", TXData);

            if (TXData.Count > 0)
            {
                serialPort.Write(TXData.ToArray(), 0, TXData.Count);
                SentCount += TXData.Count;
                await Task.Delay((int)((int)numericUpDown1.Value * 0.5));
            }

            if (RXData != null && RXData.Count > 0)
                logItem.RX = string.Join(",", RXData);
            else
                logItem.RX = string.Empty;
            RXData.Clear();

            if (logItem.TX.Length > 0 || logItem.RX.Length > 0)
            {
                Logs.Add(logItem);
                dataGridView1.RowCount++;
            }
        }

        private void cbCOMPort_SelectedValueChanged(object sender, EventArgs e)
        {
            string port = cbCOMPort.SelectedItem as string;

            try
            {
                serialPort = new SerialPort(port, 9600, Parity.Odd, 7);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.ErrorReceived += SerialPort_ErrorReceived;
                serialPort.Open();
                button1.Enabled = true;
            }
            catch (Exception E)
            {
                button1.Enabled = false;
                MessageBox.Show($"Failed to open {port} because:\r\n{E}");
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            MessageBox.Show($"SerialPort Error Received... {e.EventType}");
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[serialPort.BytesToRead];
            serialPort.Read(buffer, 0, serialPort.BytesToRead);
            RXCount += buffer.Length;

            RXData.AddRange(buffer);

            if (!sendTimer.Enabled)
            {
                LogItem logItem = new LogItem();
                logItem.Timestamp = DateTime.Now;
                logItem.RX = string.Join(",", RXData);
                logItem.TX = string.Empty;
                Logs.Add(logItem);
                dataGridView1.RowCount++;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (sendTimer.Enabled)
                sendTimer.Stop();
            else
                sendTimer.Start();

            button1.Text = sendTimer.Enabled ? "Stop" : "Start";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string[] split = textBox1.Text.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

                TXData.Clear();
                foreach (string v in split) {
                    byte value = (byte)((byte)int.Parse(v, System.Globalization.NumberStyles.HexNumber) & 0xFF);
                    if (value == 0x00)
                        continue;
                    TXData.Add(value);
                }
                textBox1.BackColor = Color.White;
            }
            catch { textBox1.BackColor = Color.IndianRed; }
        }

        private void dataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            // If this is the row for new records, no values are needed.
            if (e.RowIndex == this.dataGridView1.RowCount - 1) return;

            switch (this.dataGridView1.Columns[e.ColumnIndex].Name)
            {
                case nameof(LogItem.Timestamp):
                {
                    DateTime timestamp = ((LogItem)Logs[e.RowIndex]).Timestamp;
                    e.Value = $"{timestamp.Hour}h:{timestamp.Minute}m:{timestamp.Second}s:{timestamp.Millisecond}ms";
                    break;
                }

                case nameof(LogItem.RX):
                    e.Value = ((LogItem)Logs[e.RowIndex]).RX;
                    break;

                case nameof(LogItem.TX):
                    e.Value = ((LogItem)Logs[e.RowIndex]).TX;
                    break;
            }
        }
    }
}
