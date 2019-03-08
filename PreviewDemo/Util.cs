using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PreviewDemo.plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace PreviewDemo
{
    class Util
    {

        //输出log
        public static void WriteMessage(string msg)
        {
            using (FileStream fs = new FileStream("./log.txt", FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.BaseStream.Seek(0, SeekOrigin.End);
                    sw.WriteLine("{0}\n", msg, DateTime.Now);
                    sw.Flush();
                }
            }
        }


        /// <summary>
        /// 32位MD5加密
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string MD5Encrypt32(string password)
        {
            string cl = password;
            string pwd = "";
            MD5 md5 = MD5.Create(); //实例化一个md5对像
                                    // 加密后是一个字节类型的数组，这里要注意编码UTF8/Unicode等的选择　
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
            // 通过使用循环，将字节类型的数组转换为字符串，此字符串是常规字符格式化所得
            for (int i = 0; i < s.Length; i++)
            {
                // 将得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符 
                pwd = pwd + s[i].ToString("X");
            }
            return pwd;
        }


        //get方法调用接口获取json文件内容
        public static string LoginFunction(string server, string username, string password)
        {
            string serviceAddress = server + "?action=login" + "&username=" + username + "&password=" + password;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        //检测升级
        public static int CheckUpdate(string server)
        {
            string serviceAddress = server + "?action=checkVersion";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceAddress);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            Console.WriteLine(retString);
            if (retString == "0")
            {
                //有升级
                return 1;

                //Dialog dialog = new Dialog("发现新版本");
                //dialog.ShowDialog();
                //MessageBox.Show("发现新版本","提示");
            }
            else
            {
                return 0;
            }
        }

        //读取本地配置
        public static JObject LoadConfig()
        {
            string fileName = "./localConfig.ini";
            //读取文件内容
            StreamReader sr = new StreamReader(fileName, System.Text.Encoding.Default);
            String ls_input = sr.ReadToEnd().TrimStart();
            sr.Close();
            return (JObject)JsonConvert.DeserializeObject(ls_input);
        }

    }
}
