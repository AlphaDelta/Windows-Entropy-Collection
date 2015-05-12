using Gma.UserActivityMonitor;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Agent
{
    static class Program
    {
        static volatile bool weregoingdownbuddy = false, itsbeenapleasuretoprocesswithyou = false, tumbling = false;
        static volatile int total = 0, entropyindex = 0, bindex = 0, tumble = 0;

        [STAThread]
        static void Main()
        {
            TcpListener tcp = null;
            try
            {
                tcp = new TcpListener(IPAddress.Any, 65535);
                tcp.Start();
            }
            catch { MessageBox.Show("The port 65535 cannot be binded to, make sure it is open and another process is not already using it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            bool free = false;
            
            List<int> Intermediate = new List<int>(256);
            int[] Proc = new int[0];

            ISAAC isaac = new ISAAC();
            isaac.Isaac();

            MouseEventHandler dMove = delegate(object sender, MouseEventArgs e)
            {
                total += 2;

                Intermediate.Add(e.X);
                Intermediate.Add(e.Y);
            };
            MouseEventHandler dDown = delegate(object sender, MouseEventArgs e)
            {
                total++;

                if (e.Button == MouseButtons.Left) Intermediate.Add(0x00);
                else if (e.Button == MouseButtons.Middle) Intermediate.Add(0x01);
                else if (e.Button == MouseButtons.Right) Intermediate.Add(0x02);
                else Intermediate.Add(0x03);
            };
            MouseEventHandler dWheel = delegate(object sender, MouseEventArgs e)
            {
                total++;

                Intermediate.Add(e.Delta);
            };
            KeyEventHandler dKey = delegate(object sender, KeyEventArgs e)
            {
                total++;

                Intermediate.Add(e.KeyValue);
            };

            HookManager.MouseMove += dMove;
            HookManager.MouseDown += dDown;
            HookManager.MouseWheel += dWheel;
            HookManager.KeyDown += dKey;

            NotifyIcon ico = new NotifyIcon();
            ico.Icon = Properties.Resources.wec;

            ContextMenu cm = new ContextMenu();

            MenuItem cm1 = new MenuItem("Exit...");
            cm1.DefaultItem = true;
            cm1.Click += delegate { free = true; };
            cm.MenuItems.Add(cm1);

            ico.ContextMenu = cm;

            ico.Text = "Windows Entropy Collection";
            ico.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if(e.Button == MouseButtons.Left)
                    ico.ShowBalloonTip(3, "Stats", "Entropy collected: " + total + "b\r\nTumbled " + tumble + " times", ToolTipIcon.Info);
            };
            ico.Visible = true;

            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += delegate
            {
                while (!weregoingdownbuddy)
                {
                    total += 3;
                    Intermediate.Add(Cursor.Position.X);
                    Intermediate.Add(Cursor.Position.Y);
                    Intermediate.Add((int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);

                    MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(memStatus))
                    {
                        total += 3;
                        Intermediate.Add((int)memStatus.ullAvailPhys);
                        Intermediate.Add((int)memStatus.ullAvailVirtual);
                        Intermediate.Add((int)memStatus.ullAvailPageFile);
                    }
                    memStatus = null;

                    tumbling = true;
                    Proc = Intermediate.ToArray();
                    Intermediate.Clear();
                    for (int i = 0; i < Proc.Length; i++) isaac.mem[i % ISAAC.SIZE] = isaac.mem[i % ISAAC.SIZE] ^ Proc[i];
                    isaac.Isaac();
                    tumbling = false;

                    entropyindex = 0;
                    bindex = 0;
                    tumble++;
                    for (int i = 0; i < 10; i++) { if (weregoingdownbuddy) break; Thread.Sleep(100); } //Check for exit every 100ms for 10 seconds until the next tumble
                }

                itsbeenapleasuretoprocesswithyou = true;
            };
            bg.RunWorkerAsync();

            BackgroundWorker serve = new BackgroundWorker();
            serve.DoWork += delegate
            {
                while (!weregoingdownbuddy)
                {
                    if (tcp.Pending() == true)
                    {
                        TcpClient client = tcp.AcceptTcpClient();
                        NetworkStream st = client.GetStream();

                        st.WriteByte(0x06); //ACK (Acknowledgment)

                        try
                        {
                            if (tumble > 2) //Make sure we have tumbled data available and to an acceptable degree, otherwise we would be feeding possibly predictable data
                            {
                                int wait = 100;
                                while (tumbling) { Thread.Sleep(10); }
                                while (client.Available < 1) { wait--; if (wait < 1) break; Thread.Sleep(10); } //Wait for the client to send us the amount of entropy to release

                                if (wait > 0) //If we havent timed out release the requested amount of entropy, if that amount of entropy isn't available just release whatever we have
                                {
                                    int num = (int)st.ReadByte();
                                    int max = ISAAC.SIZE * 4;
                                    for (int i = 0; i < num && i < max; i++)
                                    {
                                        byte b = 0;
                                        if (bindex == 0) { b = (byte)((isaac.rsl[entropyindex] & 0xFF000000) >> 24); bindex++; }
                                        else if (bindex == 1) { b = (byte)((isaac.rsl[entropyindex] & 0x00FF0000) >> 16); bindex++; }
                                        else if (bindex == 2) { b = (byte)((isaac.rsl[entropyindex] & 0x0000FF00) >> 8); bindex++; }
                                        else if (bindex == 3) { b = (byte)((isaac.rsl[entropyindex] & 0x000000FF)); bindex = 0; entropyindex++; }
                                        st.WriteByte(b);
                                    }
                                }
                            }

                            st.WriteByte(0x04); //EOT (End of Transmission)
                        }
                        catch { }
                        st.Close();
                    }

                    Thread.Sleep(10);
                }
            };
            serve.RunWorkerAsync();

            while (!free)
            {
                Application.DoEvents();

                Thread.Sleep(5);
            }

            HookManager.MouseMove -= dMove;
            HookManager.MouseDown -= dDown;
            HookManager.MouseWheel -= dWheel;
            HookManager.KeyDown -= dKey;

            weregoingdownbuddy = true;
            while (!itsbeenapleasuretoprocesswithyou) { Thread.Sleep(100); }

            ico.Dispose();
            tcp.Stop();
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }


        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
    }
}
