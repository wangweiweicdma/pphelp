using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Net;

namespace UUWiseCSWrapper
{
    public partial class Form1 : Form
    {
        static int SoftID = 0;
        static string SoftKey = "";
        bool isLogin = false;
        int codeType = 1004;
        int m_codeID;
        //static UUWiseHelperLib.CaptchaRecognizerClass c = new UUWiseHelperLib.CaptchaRecognizerClass();

        public Form1()
        {
            InitializeComponent();

           
        }

        private bool CheckDll(string softid, string softkey, string dllPath)
        {
            String[] URLs = { "http://v.uuwise.com/service/verify.aspx",
                             "http://v.uuwise.net/service/verify.aspx",
                             "http://v.uudama.com/service/verify.aspx",
                             "http://v.uudati.cn/service/verify.aspx",
                             "http://v.taskok.com/service/verify.aspx"};
            bool isPass = false;
            foreach (String url in URLs)
            {
                try
                {
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    request.Method = "POST";
                    //设置http请求的超时时间为10s
                    request.Timeout = 10 * 1000;
                    request.UserAgent = "VersionClient";
                    request.ContentType = "application/x-www-form-urlencoded";
                    //构造post数据流
                    //String strPostData = "SID=2097&dllkey=";
                    String strPostData = "SID=" + softid + "&dllkey=";
                    String strDllKey = GetFileMD5(dllPath);
                    strPostData += strDllKey + "&key=" + MD5Encoding(softid + strDllKey.ToUpper());

                    byte[] data = Encoding.ASCII.GetBytes(strPostData);
                    request.ContentLength = data.Length;
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }

                    //获取返回结果
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        Encoding responseEncoding = Encoding.GetEncoding(response.CharacterSet);
                        using (Stream stream = response.GetResponseStream())
                        {
                            using (StreamReader objReader = new StreamReader(stream, responseEncoding))
                            {
                                String strRes = objReader.ReadToEnd();
                                if (strRes.Equals(MD5Encoding(softid + softkey.ToUpper())))
                                {
                                    isPass = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            if (isPass)
                richTextBox1.Text += "DLL校验成功！请登录打码。";
            else
                richTextBox1.Text += "DLL校验失败！";
            return isPass;
        }

        /// <summary>
        /// 登陆测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLogin_Click(object sender, EventArgs e)
        {
            /*	优优云DLL 文件MD5值校验
	         *  用处：近期有不法份子采用替换优优云官方dll文件的方式，极大的破坏了开发者的利益
	         *  用户使用替换过的DLL打码，导致开发者分成变成别人的，利益受损，
	         *  所以建议所有开发者在软件里面增加校验官方MD5值的函数
	         *  如何获取文件的MD5值，通过下面的GetFileMD5(文件)函数即返回文件MD5
	         */

            string DLLPath = System.Environment.CurrentDirectory + "\\UUWiseHelper.dll";
            string Md5 = GetFileMD5(DLLPath);
            //string AuthMD5 = "79dd7e248b7ec70e2ececa19b51c39c6";//作者在编写软件时内置的比对用DLLMD5值，不一致时将禁止登录,具体需要各位自己先获取使用的DLL的MD5验证字符串
            // if (Md5 != AuthMD5)
            //{
            //    MessageBox.Show("此软件使用的是UU云1.1.0.9动态链接库版DLL，与您目前软件内DLL版本不符，请前往http://www.uuwise.com下载更换此版本DLL");
            //     return;
            // }


            if (!CheckSid())
                return;
            string u = tbxUsername.Text.Trim();
            string p = tbxPasswd.Text.Trim();
            int res = Wrapper.uu_login(u, p);
            if (res > 0)
                isLogin = true;
            DelegateSetRtbText("登录返回结果:" + res.ToString() + "," + (isLogin ? "登陆成功" : "登录失败"));
        }

        /// <summary>
        /// 识别测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSltCodeImg_Click(object sender, EventArgs e)
        {
            if (!CheckSid())
                return;


            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "选择验证码图片";
                dlg.Filter = "图片文件 (*.jpg)|*.jpg|所有文件 (*.*)|*.*";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    tbxCodeImgPath.Text = dlg.FileName;
                    pictureBox2.Image = Image.FromFile(dlg.FileName);
                }
            }
        }

        /// <summary>
        /// MD5 加密字符串
        /// </summary>
        /// <param name="rawPass">源字符串</param>
        /// <returns>加密后字符串</returns>
        public static string MD5Encoding(string rawPass)
        {
            // 创建MD5类的默认实例：MD5CryptoServiceProvider
            MD5 md5 = MD5.Create();
            byte[] bs = Encoding.UTF8.GetBytes(rawPass);
            byte[] hs = md5.ComputeHash(bs);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hs)
            {
                // 以十六进制格式格式化
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        bool CheckSid()
        {
            if (SoftID <= 0)
            {
                MessageBox.Show("请先设置软件id");
                return false;

            }
            if (string.IsNullOrEmpty(SoftKey))
            {
                MessageBox.Show("请先设置软件key");
                return false;
            }
            return true;
        }

        private void btnSetSoftInfo_Click(object sender, EventArgs e)
        {
            try
            {
                SoftID = int.Parse(txtSoftID.Text);
                SoftKey = txtSoftKey.Text;
            }
            catch
            {
                MessageBox.Show("无效的软件id");
                return;
            }
            Wrapper.uu_setSoftInfo(SoftID, SoftKey);
            DelegateSetRtbText("软件信息设置成功");
        }

        private void btnGetScore_Click(object sender, EventArgs e)
        {
            int score = Wrapper.uu_getScore(tbxUsername.Text, tbxPasswd.Text);
            DelegateSetRtbText("当前可用题分:" + score.ToString());
        }

        private void btnUserReg_Click(object sender, EventArgs e)
        {
            string userName = tbxUsername.Text;
            string pass = tbxPasswd.Text;
            if (string.IsNullOrEmpty(userName))
            {
                MessageBox.Show("用户名不能为空");
                return;
            }
            if (string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("对不起，请输入密码");
                return;
            }
            if (string.IsNullOrEmpty(txtSoftID.Text))
            {
                MessageBox.Show("请输入软件id");
                return;
            }
            if (string.IsNullOrEmpty(txtSoftKey.Text))
            {
                MessageBox.Show("请输入软件key");
                return;
            }
            int sid = int.Parse(txtSoftID.Text);
            string key = txtSoftKey.Text;

            int result = Wrapper.uu_reguser(userName, pass, sid, key);

            MessageBox.Show("返回结果:" + result.ToString());
        }

        private void btnPay_Click(object sender, EventArgs e)
        {
            string cardNum = txtCardNum.Text;
            string userName = txtPayUserName.Text;
            if (string.IsNullOrEmpty(cardNum))
            {
                MessageBox.Show("充值卡不能为空");
                return;
            }
            if (string.IsNullOrEmpty(userName))
            {
                MessageBox.Show("对不起，请输入要充值的用户");
                return;
            }
            if (string.IsNullOrEmpty(txtSoftID.Text))
            {
                MessageBox.Show("请输入软件id");
                return;
            }
            if (string.IsNullOrEmpty(txtSoftKey.Text))
            {
                MessageBox.Show("请输入软件key");
                return;
            }
            int sid = int.Parse(txtSoftID.Text);
            string key = txtSoftKey.Text;
            int result = Wrapper.uu_pay(userName, cardNum, sid, key);

            MessageBox.Show("返回结果:" + result.ToString());

        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    DirectoryInfo dir = new DirectoryInfo(dlg.SelectedPath);
                    richTextBox1.BeginInvoke(new EventHandler(delegate
                    {
                        richTextBox1.Text += string.Format("当前路径:{0}\r\n", dir.FullName);
                    }));
                    //tbxImgPath.Text = dlg.SelectedPath;
                }
            }
        }

        void DelegateSetRtbText(string text)
        {
            DelegateSetRtbText(text, true);
        }


        void DelegateSetRtbText(string text, bool isNewLine)
        {
            this.richTextBox1.BeginInvoke(new EventHandler(delegate
            {
                if (isNewLine)
                    richTextBox1.Text += string.Format("\r\n{0}: {1}", DateTime.Now.ToString("HH:mm:ss"), text);
                else
                    richTextBox1.Text += string.Format("{0}", text);
                richTextBox1.Select(richTextBox1.TextLength, richTextBox1.TextLength);
                richTextBox1.ScrollToCaret();
            }));
        }

        void SetLabelText(Label lbl)
        {
            lbl.Invoke(new EventHandler(delegate
            {
                lbl.Text = (int.Parse(lbl.Text) + 1).ToString();
            }));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //下面是软件id对应的dll校验key。在开发者后台-我的软件里面可以查的到。
            string strCheckKey = "06517CB8-BF07-4261-B8D5-E67C9BC7BAF4".ToUpper();

            if (!isLogin)
            {
                MessageBox.Show("请先登录优优云.");
                return;
            }

            int.TryParse(textBox3.Text, out codeType);

            Image img = null;
            if (rb1.Checked)
            {
                img = pictureBox1.Image;
            }
            else if (rb2.Checked)
            {
                if (string.IsNullOrEmpty(tbxCodeImgPath.Text))
                {
                    MessageBox.Show("请先选择图片");
                    return;
                }
                img = Image.FromFile(tbxCodeImgPath.Text);
            }

            if (!checkBox1.Checked)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                if (rb4.Checked && rb2.Checked)
                {
                    //新版本dll需要预先分配50个字节的空间，否则dll会崩溃！！！！
                    StringBuilder result = new StringBuilder(50);
                    int codeId = Wrapper.uu_recognizeByCodeTypeAndPath(tbxCodeImgPath.Text, codeType, result);
                    string resultCode = CheckResult(result.ToString(), Convert.ToInt32(txtSoftID.Text.Trim()), codeId, strCheckKey);

                    m_codeID = codeId;
                    DelegateSetRtbText(string.Format("Code ID:{0}, 识别结果:{1}", codeId, resultCode.ToString()));
                }
                else if (rb3.Checked)
                {
                    //新版本dll需要预先分配50个字节的空间，否则dll会崩溃！！！！
                    StringBuilder result = new StringBuilder(50);
                    int codeId = Wrapper.uu_recognizeByCodeTypeAndUrl(this.textBox2.Text, string.Empty, codeType, string.Empty, result);
                    string resultCode = CheckResult(result.ToString(), Convert.ToInt32(txtSoftID.Text.Trim()), codeId, strCheckKey);
                    m_codeID = codeId;
                    DelegateSetRtbText(string.Format("Code ID:{0}, 识别结果:{1}", codeId, resultCode.ToString()));
                }
                else
                {
                    MemoryStream ms = new MemoryStream();
                    img.Save(ms, ImageFormat.Jpeg);
                    byte[] buffer = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(buffer, 0, buffer.Length);
                    ms.Flush();
                    //新版本dll需要预先分配50个字节的空间，否则dll会崩溃！！！！
                    StringBuilder res = new StringBuilder(50);
                    int codeId = Wrapper.uu_recognizeByCodeTypeAndBytes(buffer, buffer.Length, codeType, res);
                    string resultCode = CheckResult(res.ToString(), Convert.ToInt32(txtSoftID.Text.Trim()), codeId, strCheckKey);
                    m_codeID = codeId;
                    DelegateSetRtbText(string.Format("Code ID:{0}, 识别结果:{1}", codeId, resultCode.ToString()));
                    ms.Close();
                    ms.Dispose();
                    //img.Dispose();
                }
                sw.Stop();
                DelegateSetRtbText(string.Format(", 本次识别耗时: {0}毫秒, {1}秒", sw.ElapsedMilliseconds, (sw.ElapsedMilliseconds / (float)1000).ToString("F2")), false);
            }
            else
            {
                int threads = 20;
                int.TryParse(textBox1.Text, out threads);
                Thread t = new Thread(new ParameterizedThreadStart(ThreadsTest));
                t.IsBackground = true;
                t.Start(new object[] { 
                    threads,
                    img,
                    rb3.Checked,
                    textBox2.Text
                });
            }


        }

        void ThreadsTest(object param)
        {
            object[] parameter = (object[])param;
            int threads = Convert.ToInt32(parameter[0].ToString());
            Image img = parameter[1] as Image;
            bool isUrlCaptcha = Convert.ToBoolean(parameter[2]);
            string captchaUrl = parameter[3].ToString();

            MemoryStream ms = new MemoryStream();
            if (!isUrlCaptcha) img.Save(ms, ImageFormat.Jpeg);

            for (int i = 0; i < threads; i++)
            {
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    int codeId = 0;
                    //新版本dll需要预先分配50个字节的空间，否则dll会崩溃！！！！
                    StringBuilder result = new StringBuilder(50);
                    if (!isUrlCaptcha)
                    {
                        byte[] buffer = new byte[ms.Length];
                        ms.Position = 0;
                        ms.Read(buffer, 0, buffer.Length);
                        ms.Flush();
                        codeId = Wrapper.uu_recognizeByCodeTypeAndBytes(buffer, buffer.Length, codeType, result);
                        //ms.Close();
                        //ms.Dispose();
                    }
                    else
                    {
                        codeId = Wrapper.uu_recognizeByCodeTypeAndUrl(captchaUrl, string.Empty, codeType, string.Empty, result);
                    }
                    sw.Stop();
                    richTextBox1.BeginInvoke(new EventHandler(delegate
                    {
                        //SetLabelText(label11);
                        string text = string.Format("\r\n{2}: Code ID:{0}, 识别结果:{1}", codeId.ToString(), result, DateTime.Now.ToString("HH:mm:ss"));
                        richTextBox1.Text += text;
                        richTextBox1.Text += string.Format(", 本次识别耗时: {0}毫秒, {1}秒", sw.ElapsedMilliseconds, (sw.ElapsedMilliseconds / (float)1000).ToString("F2"));
                        richTextBox1.Select(richTextBox1.Text.Length - text.Length, text.Length);
                        richTextBox1.SelectionColor = Color.Black;
                        richTextBox1.Select(richTextBox1.TextLength, 0);
                        richTextBox1.ScrollToCaret();
                    }));
                }));
                t.IsBackground = true;
                t.Start();

                //Thread.Sleep(1000);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);
            this.Dispose();
            this.Close();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.uuwise.com/price.html");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://www.uuwise.com");
            }
            catch (Exception)
            {
                Clipboard.SetText("http://www.uuwise.com");
                MessageBox.Show("网址已经成功复制，请粘贴到浏览器内浏览。");
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Wrapper.uu_SysCallOneParam(1, 1);
        }

        private void reportErrBtn_Click(object sender, EventArgs e)
        {
            int iResult = Wrapper.uu_reportError(m_codeID);
            if (0 == iResult)
                DelegateSetRtbText("报错成功！");
            else
                DelegateSetRtbText(string.Format("报错失败！错误代码为:{0}", iResult));
        }

        #region 根据路径获取文件MD5
        /// <summary>
        /// 获取文件MD5校验值
        /// </summary>
        /// <param name="filePath">校验文件路径</param>
        /// <returns>MD5校验字符串</returns>
        private string GetFileMD5(string filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] md5byte = md5.ComputeHash(fs);
            int i, j;
            StringBuilder sb = new StringBuilder(16);
            foreach (byte b in md5byte)
            {
                i = Convert.ToInt32(b);
                j = i >> 4;
                sb.Append(Convert.ToString(j, 16));
                j = ((i << 4) & 0x00ff) >> 4;
                sb.Append(Convert.ToString(j, 16));
            }
            return sb.ToString();
        }
        #endregion

        private void checkDllBtn_Click(object sender, EventArgs e)
        {
            string strSoftID = txtSoftID.Text.Trim();
            int softId = int.Parse(strSoftID);
            string softKey = txtSoftKey.Text.Trim();
            Guid guid = Guid.NewGuid();
            string strGuid = guid.ToString().Replace("-", "").Substring(0, 32).ToUpper();
            string DLLPath = System.Environment.CurrentDirectory + "\\UUWiseHelper.dll";
            //string DLLPath = "E:\\work\\UUWiseHelper 新版http协议\\输出目录\\UUWiseHelper.dll";
            string strDllMd5 = GetFileMD5(DLLPath);
            CRC32 objCrc32 = new CRC32();
            string strDllCrc = String.Format("{0:X}", objCrc32.FileCRC(DLLPath));
            //CRC不足8位，则前面补0，补足8位
            int crcLen = strDllCrc.Length;
            if (crcLen < 8)
            {
                int miss = 8 - crcLen;
                for (int i = 0; i < miss; ++i)
                {
                    strDllCrc = "0" + strDllCrc;
                }
            }
            //下面是软件id对应的dll校验key。在开发者后台-我的软件里面可以查的到。
            string strCheckKey = "06517CB8-BF07-4261-B8D5-E67C9BC7BAF4".ToUpper();
            string yuanshiInfo = strSoftID + strCheckKey + strGuid + strDllMd5.ToUpper() + strDllCrc.ToUpper();
            richTextBox1.Text += yuanshiInfo + "\n";
            string localInfo = MD5Encoding(yuanshiInfo);
            StringBuilder checkResult = new StringBuilder();
            Wrapper.uu_CheckApiSign(softId, softKey, strGuid, strDllMd5, strDllCrc, checkResult);
            string strCheckResult = checkResult.ToString();
            if (localInfo.Equals(strCheckResult))
                richTextBox1.Text += "Dll校验成功！\n";
            else
                richTextBox1.Text += "Dll校验失败！服务器返回信息为" + strCheckResult + "本地校验信息为" + localInfo + "\n";
        }

        public string CheckResult(string result, int softId, int codeId, string checkKey)
        {
            //对验证码结果进行校验，防止dll被替换
            if (string.IsNullOrEmpty(result))
                return result;
            else
            {
                if (result[0] == '-')
                    //服务器返回的是错误代码
                    return result;

                string[] modelReult = result.Split('_');
                //解析出服务器返回的校验结果
                string strServerKey = modelReult[0];
                string strCodeResult = modelReult[1];
                //本地计算校验结果
                string localInfo = softId.ToString() + checkKey + codeId.ToString() + strCodeResult.ToUpper();
                string strLocalKey = MD5Encoding(localInfo).ToUpper();
                //相等则校验通过
                if (strServerKey.Equals(strLocalKey))
                    return strCodeResult;
                return "结果校验不正确";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SocketServer.startService();

        }

        private void button6_Click(object sender, EventArgs e)
        {
            SocketClient.sendText(" pp help test ");
        }

        private void button7_Click(object sender, EventArgs e)
        {


            Thread keythread = new Thread(Form1.SendKeyTest);
            keythread.Start();
         

        }


        public static void SendKeyTest()
        {
            Console.WriteLine("send key test 1");
            Thread.Sleep(3000);
            Console.WriteLine("send key test 2");
            Wrapper.keybd_event(13, 0, 0, 0);


        }

        private void button8_Click(object sender, EventArgs e)
        {
            Thread mousethread = new Thread(Form1.SendMouseTest);
            mousethread.Start();
         
        }

        public static void SendMouseTest()
        {
            Console.WriteLine("send mouse test 1");
            Thread.Sleep(3000);
            Console.WriteLine("send mouse test 2");
            Wrapper.mouse_event(0x8000 | 0x0001, 100, 100, 0, 0);


        }

        private void button9_Click(object sender, EventArgs e)
        {
            initKeyHook();
        }


        private void initKeyHook()
        {
            kh = new KeyboardHook();
            kh.SetHook();
            kh.OnKeyDownEvent += kh_OnKeyDownEvent;
        }


        KeyboardHook kh;

        void kh_OnKeyDownEvent(object sender, KeyEventArgs e)
        {
           // if (e.KeyData == (Keys.S | Keys.Control)) { this.Show(); }//Ctrl+S显示窗口
           // if (e.KeyData == (Keys.H | Keys.Control)) { this.Hide(); }//Ctrl+H隐藏窗口
           // if (e.KeyData == (Keys.C | Keys.Control)) { this.Close(); }//Ctrl+C 关闭窗口 
           // if (e.KeyData == (Keys.A | Keys.Control | Keys.Alt)) { this.Text = "你发现了什么？"; }//Ctrl+Alt+A


            Command cmd = new Command();
            int value = (int)e.KeyCode;
            cmd.createKeyEvent((byte)value);
            cmd.sendBraodcast();

            Console.WriteLine("key down test " + e.KeyCode);
           
            
        }

    }
}
