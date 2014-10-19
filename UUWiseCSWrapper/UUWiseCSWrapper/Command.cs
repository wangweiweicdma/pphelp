using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace UUWiseCSWrapper
{
    class Command
    {

        private  const byte MOSE_CLICK = 110;
        private  const byte KEY_DOWN = 120;
        private  const byte CONTROL_START = 210;
        private  const byte CONTROL_STOP = 220;
        private  const byte TYPE_UNKNOWN = 230;


        private  const int CMD_LEN = 64;
        private  const int START_CODE = 0x5566eeff;
        private  const int END_CODE = 0x6655ffEE;

        private  byte []  mCmdBuffer ;
        public Command(byte []temp)
        {
            mCmdBuffer = new byte[CMD_LEN];
            for (int i = 0; i < CMD_LEN; i++)
            {
                mCmdBuffer[i] = temp[i];
            }
        }
        
        public Command(){
            mCmdBuffer = new byte[CMD_LEN];
        }

        public byte[] createKeyEvent(byte keydown)
        {
            setStartCode();

            int index = 4;
            mCmdBuffer[index ++] = KEY_DOWN ; 
            mCmdBuffer[index ++ ] = keydown;


            setEndCode();

            return mCmdBuffer;;
        }

        private void setStartCode(){
             mCmdBuffer[0] = 0x55;
            mCmdBuffer[1] = 0x66;
            mCmdBuffer[2] = 0xFF;
            mCmdBuffer[3] = 0xEE;
        }

        private void setEndCode(){
            mCmdBuffer[CMD_LEN - 4] = 0x55;
            mCmdBuffer[CMD_LEN - 3] = 0x66;
            mCmdBuffer[CMD_LEN - 2] = 0xFF;
            mCmdBuffer[CMD_LEN - 1] = 0xEE;
        }


        public byte[] createMouseEventClick(uint  x, uint  y)
        {

            setStartCode();
            int index = 4;
            mCmdBuffer[index++] = MOSE_CLICK;
            mCmdBuffer[index++] =  (byte)((x & 0xFF000000) >> 24);

            mCmdBuffer[index++] = (byte)((x & 0x00FF0000) >> 16);
            mCmdBuffer[index++] = (byte)((x & 0x0000FF00) >> 8);
            mCmdBuffer[index++] = (byte)((x & 0x000000FF) );


            mCmdBuffer[index++] = (byte)((y & 0xFF000000) >> 24);
            mCmdBuffer[index++] = (byte)((y & 0x00FF0000) >> 16);
            mCmdBuffer[index++] = (byte)((y & 0x0000FF00) >> 8);
            mCmdBuffer[index++] = (byte)((y & 0x000000FF));

            setEndCode();

            return mCmdBuffer;
        }

        public Boolean checkCMD()
        {
            int index = 0;
            if (mCmdBuffer[index++] != 0x55)
            {
                return false;
            }
            if (mCmdBuffer[index++] != 0x66)
            {
                return false;
            }
            if (mCmdBuffer[index++] != 0xEE)
            {
                return false;
            }
            if (mCmdBuffer[index++] != 0xFF)
            {
                return false;
            }

            index = CMD_LEN - 4;
            if (mCmdBuffer[index++] != 0x66)
            {
                return false;
            }
            if (mCmdBuffer[index++] != 0x55)
            {
                return false;
            }
            if (mCmdBuffer[index++] != 0xFF)
            {
                return false;
            }
            
            if (mCmdBuffer[index++] != 0xEE)
            {
                return false;
            }

            return true;
        }

        public int getCommandType()
        {
            return mCmdBuffer[4];
        }


        public int getMoseX()
        {
            int value = 0;
            int index = 5;
            value += (mCmdBuffer[index++] << 24);
            value += (mCmdBuffer[index++] << 16); 
            value += (mCmdBuffer[index++] << 8);
            value += (mCmdBuffer[index++] );

            return value;
        }

        public int getMoseY()
        {
            int value = 0;
            int index = 9;
            value += (mCmdBuffer[index++] << 24);
            value += (mCmdBuffer[index++] << 16);
            value += (mCmdBuffer[index++] << 8);
            value += (mCmdBuffer[index++]);

            return value;
        }

        public byte getKeyCode()
        {
            return mCmdBuffer[5];
        }


        public void sendBraodcast( )
        {
            Console.WriteLine("send cmd start  " );
            byte[] data = new byte[1024];//定义一个数组用来做数据的缓冲区
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("192.168.1.255"), 9050);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // string welcome = "Hello,are you there?";

            server.SendTo(mCmdBuffer, mCmdBuffer.Length, SocketFlags.None, ipep);//将数据发送到指定的终结点


            Console.WriteLine("send cmd ok :");

        }


        public void exec()
        {
            if (getCommandType() == KEY_DOWN)
            {
                Console.WriteLine(" create key event from net ");
                Wrapper.keybd_event(getKeyCode(), 0, 0, 0);
            }
            else if (getCommandType() == MOSE_CLICK)
            {
                // for mose click ;
            }
            


        }

    }
}
