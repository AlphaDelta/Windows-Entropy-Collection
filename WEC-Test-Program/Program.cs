using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TestProgram
{
    class Program
    {
        static void Main()
        {
            Console.CursorVisible = false;

            int[] table = new int[256];
            for (int i = 0; i < 256; i++) { table[i] = 0; }

            do
            {
                TcpClient tcp = new TcpClient("127.0.0.1", 65535);
                Console.WriteLine("Listening on localhost:65535");

                while (tcp.Available < 1) Thread.Sleep(100);
                while (tcp.Available > 0)
                {
                    int bt = tcp.GetStream().ReadByte();
                    if (bt == 0x06)
                    {
                        Console.Write("Recieved: ");
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("ACK");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("Recieved: 0x" + bt.ToString("X2"));
                        return;
                    }
                }


                tcp.GetStream().WriteByte(0xFF);
                Console.Write("Wrote: ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("0xFF");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(" and now waiting...");
                Console.ResetColor();

                while (tcp.Available < 1) Thread.Sleep(100);
                Console.WriteLine("Reading " + tcp.Available + " bytes   ");
                int i = 0;
                while (tcp.Available > 0)
                {
                    int b = tcp.GetStream().ReadByte();
                    if (b == 0x04)
                    {
                        //Console.BackgroundColor = ConsoleColor.Red;
                        //Console.ForegroundColor = ConsoleColor.White;
                        //Console.Write("XX");
                        //Console.ResetColor();
                        //Console.Write(" ");
                    }
                    else
                    {
                        table[b]++;
                        //Console.ForegroundColor = ConsoleColor.White;
                        //Console.Write(b.ToString("X2"));
                        //Console.Write(" ");
                    }
                    //if (i % 8 == 7) Console.WriteLine();
                    i++;
                }

                int rnum = 0;
                for (int num = 0; num < 256; num++)
                {
                    int num2 = (table[num] > 0xFF ? 0xFF : table[num]);
                    //if (num2 < 1) continue;

                    Console.BackgroundColor = (num2 >= 0xFF ? ConsoleColor.Red : ConsoleColor.Black);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(num.ToString("X2"));
                    Console.ForegroundColor = (num2 >= 0xFF ? ConsoleColor.Black : (num2 > 0xBD ? ConsoleColor.Magenta : (num2 >= 0x7E ? ConsoleColor.Red : (num2 >= 0x3F ? ConsoleColor.DarkRed : ConsoleColor.DarkGray))));
                    Console.Write(" " + (num == 0x04 ? "XX" : num2.ToString("X2")));
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write(" ");

                    if (rnum % 8 == 7) Console.WriteLine();
                    rnum++;
                }

                tcp.Close();
                Console.ResetColor();
                Console.WriteLine("\r\nClosed localhost:65535");
                Console.SetCursorPosition(0, 0);
            } while (Console.ReadKey().Key != ConsoleKey.Escape);

            Console.ReadKey();
        }
    }
}
