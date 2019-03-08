using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PreviewDemo.plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PreviewDemo
{
    public partial class Login : Form
    {
        public static int userGrade = -1;
        public static string url = null;//升级包下载地址
        public static string server = null;//服务端入口

        public Login()
        {
            InitializeComponent();
            //初始化相关配置
            //skinEngine1.SkinFile = "./Debug/Skins/MacOS.ssk";
            server = Util.LoadConfig()["server"].ToString();
            //读取账号密码
            JToken t= Util.LoadConfig();
            userName.Text = t["username"].ToString();
            passWord.Text = t["password"].ToString();

            //异步检测升级
            Task task1 = new Task(() =>
            {
                string serviceAddress = server+"?action=checkVersion";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "GET";
                request.ContentType = "text/html;charset=UTF-8";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
                JObject json = (JObject)JsonConvert.DeserializeObject(retString);
                if (json["code"].ToString() == "1")
                {
                    //有升级
                    this.Invoke((MethodInvoker)(() =>
                    {
                        update_tip.Visible = true;
                        url = json["url"].ToString();
                    }));
                }

            });
            task1.Start();

            //通知
            Task task2 = new Task(() =>
            {
                string serviceAddress = server + "?action=checkNotice";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
                request.Method = "GET";
                request.ContentType = "text/html;charset=UTF-8";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
                JObject json = (JObject)JsonConvert.DeserializeObject(retString);
                Console.WriteLine(json);
                if (json["code"].ToString() == "1")
                {
                    Notice notice = new Notice(json["content"].ToString());
                    notice.ShowDialog();
                }

            });
            task2.Start();

        }

        private void loginBtn_Click(object sender, EventArgs e)
        {

            string username = userName.Text;
            string password = passWord.Text;
            //WriteMessage(GetFunction(username, MD5Encrypt32(password)));
            if (username == "" || password == "")
            {
                MessageBox.Show("账号或密码不能为空", "错误");
                return;
            }
            //保存账号密码
            JToken t = Util.LoadConfig();
            t["username"] = username;
            t["password"] = password;
            System.IO.File.WriteAllText("localConfig.ini", t.ToString(), Encoding.UTF8);
            //提交验证
            string res = Util.LoginFunction(server, username, Util.MD5Encrypt32(password));
            JObject json = (JObject)JsonConvert.DeserializeObject(res);

            if (json["code"].ToString() == "-1")
            {
                MessageBox.Show("账号密码错误", "错误");
            }
            else
            {
                //隐藏登录框并显示主界面

                this.Visible = false;
                Preview preview = new Preview(res);
                preview.ShowDialog();


            }
            //隐藏登录框并显示界面
            //this.Visible = false;
            //Preview preview = new Preview();
            //preview.ShowDialog();
        }

        private void label4_Click(object sender, EventArgs e)
        {
            Notice notice = new Notice("账户为管理员统一管理\n账号：中文姓名，密码：手机号\n如仍无法登录请联系技术组");
            notice.ShowDialog();

        }

        private void label5_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(url);
        }
    }
}
