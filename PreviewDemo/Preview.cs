using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Text;
using System.IO;

using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using PreviewDemo.plugin;

namespace PreviewDemo
{
    /// <summary>
    /// Form1 的摘要说明。
    /// </summary>
    public class Preview : System.Windows.Forms.Form
    {
        private uint iLastErr = 0;
        private Int32 m_lUserID = -1;
        private bool m_bInitSDK = false;
        private bool m_bTalk = false;
        private Int32 m_lRealHandle = -1;
        private int lVoiceComHandle = -1;
        private string str;
        private string UserDir = "./用户/";//用户资料文件夹
        private JObject config = null;//配置
        private bool is_FullScreen = false;
        private bool m_bRecord = false;
        private Dictionary<string, int> winSize = new Dictionary<string, int>();
        System.Timers.Timer t = new System.Timers.Timer(1000);   //实例化Timer类，设置间隔时间为10000毫秒；   
        CHCNetSDK.VOICEDATACALLBACKV30 VoiceData;




        CHCNetSDK.REALDATACALLBACK RealData = null;
        public CHCNetSDK.NET_DVR_PTZPOS m_struPtzCfg;
        private System.Windows.Forms.PictureBox RealPlayWnd;
        /*private Button PtzGet;
        private Button PtzSet;*/
        /*private ComboBox comboBox1;
        private TextBox textBoxPanPos;
        private TextBox textBoxTiltPos;
        private TextBox textBoxZoomPos;*/
        private TreeView treeView1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem menuRecord;
        private ToolStripMenuItem menuCapture;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem menuSound;
        private ToolStripMenuItem menuReset;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem menuFullScreen;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem userFolder;
        private ToolStripMenuItem about;
        private Label label1;
        private IContainer components;

        //private GroupBox groupBox1;

        public Preview(string config)
        {
            //
            // Windows 窗体设计器支持所必需的
            //
            InitializeComponent();

            //初始化定时器
            t.Elapsed += new System.Timers.ElapsedEventHandler(theout); //到达时间的时候执行事件；   
            t.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；   
            t.SynchronizingObject = this;

            //双击全屏事件
            this.RealPlayWnd.DoubleClick += new System.EventHandler(this.RealPlayWnd_Click);


            //存储尺寸
            Rectangle rect = new Rectangle();
            rect = Screen.GetWorkingArea(this);
            winSize.Add("full_width", rect.Width);
            winSize.Add("full_height", rect.Height);
            winSize.Add("origin_width", this.Width);
            winSize.Add("origin_height", this.Height);
            //解析配置
            JObject json = (JObject)JsonConvert.DeserializeObject(config);
            this.config = json;
            //读取本地配置
            int is_first_startup = int.Parse(Util.LoadConfig()["first_startup"].ToString());
            if (is_first_startup == 1)
            {
                //第一次启动
                MessageBox.Show("在播放区域点击右键，打开功能菜单；如发现问题请联系技术组", "提示");
                //修改配置
                JToken t = Util.LoadConfig();
                t["first_startup"] = 0;
                string content = t.ToString();
                System.IO.File.WriteAllText(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\\localConfig.ini", content, Encoding.UTF8);
            }

            //Console.WriteLine((JObject)JsonConvert.DeserializeObject(json.ToString()));
            /*
             * 远程配置内容示例
             {
              "code": 1,//状态码
              "grade": "[1,1,1]",//登录账户年级
              "configuration": {
                "id": "1",//索引，无实际意义
                "grade_1": "{\"ip\":\"172.16.0.\",\"first\":201,\"num\":25}",//高一，ip前三段，第一个班级第四段，班级数
                "grade_2": "{\"ip\":\"172.16.1.\",\"first\":201,\"num\":25}",//高二
                "grade_3": "{\"ip\":\"172.16.1.\",\"first\":141,\"num\":25}",//高三
                "remote_notice": "{\"code\":0}",//推送通知
                "has_update": "{\"code\":1,\"url\":\"www.baidu.com\"}"//软件升级
              }
            }
              
             */

            //初始化树
            TreeNode grade_root1 = null, grade_root2 = null, grade_root3 = null;
            for (int grade = 0; grade < 3; grade++)
            {
                //获取对应年级配置
                JObject grade_config = (JObject)JsonConvert.DeserializeObject(this.config["configuration"]["grade_" + (grade + 1)].ToString());
                //班级长度
                int length = int.Parse(grade_config["num"].ToString());
                //申请临时数组
                TreeNode[] arr = new TreeNode[length];
                //生成班级节点
                for (int i = 0; i < length; i++)
                {
                    string num = (i + 1).ToString();
                    TreeNode treeNode = new TreeNode(num + "班")
                    {
                        Name = "class",
                        Tag = grade_config["ip"].ToString()
                        + (int.Parse(grade_config["first"].ToString()) + i),
                        Text = num + "班"
                    };
                    arr[i] = treeNode;
                }
                switch (grade)
                {
                    case 0:
                        grade_root1 = new TreeNode("高一", arr);
                        this.treeView1.Nodes.Add(grade_root1);
                        break;
                    case 1:
                        grade_root2 = new TreeNode("高二", arr);
                        this.treeView1.Nodes.Add(grade_root2);
                        break;
                    case 2:
                        grade_root3 = new TreeNode("高三", arr);
                        this.treeView1.Nodes.Add(grade_root3);
                        break;

                }

            }

            //杂项设置
            //this.treeView1.ExpandAll();
            //隐藏年级
            char[] g = json["grade"].ToString().ToCharArray();
            if (g[0] == '0')
            {
                grade_root1.Remove();
            }
            if (g[1] == '0')
            {
                grade_root2.Remove();
            }
            if (g[2] == '0')
            {
                grade_root3.Remove();
            }


            m_bInitSDK = CHCNetSDK.NET_DVR_Init();
            if (m_bInitSDK == false)
            {
                MessageBox.Show("NET_DVR_Init error!");
                return;
            }
            else
            {
                //保存SDK日志 To save the SDK log
                CHCNetSDK.NET_DVR_SetLogToFile(3, "C:\\SdkLog\\", true);
            }
            //
            // TODO: 在 InitializeComponent 调用后添加任何构造函数代码
            //
        }

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (m_lRealHandle >= 0)
            {
                CHCNetSDK.NET_DVR_StopRealPlay(m_lRealHandle);
            }
            if (m_lUserID >= 0)
            {
                CHCNetSDK.NET_DVR_Logout(m_lUserID);
            }
            if (m_bInitSDK == true)
            {
                CHCNetSDK.NET_DVR_Cleanup();
            }
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码
        /// <summary>
        /// 设计器支持所需的方法 - 不要使用代码编辑器修改
        /// 此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Preview));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuRecord = new System.Windows.Forms.ToolStripMenuItem();
            this.menuCapture = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.menuSound = new System.Windows.Forms.ToolStripMenuItem();
            this.menuReset = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.menuFullScreen = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.userFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.about = new System.Windows.Forms.ToolStripMenuItem();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.RealPlayWnd = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RealPlayWnd)).BeginInit();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuRecord,
            this.menuCapture,
            this.toolStripSeparator2,
            this.menuSound,
            this.menuReset,
            this.toolStripSeparator3,
            this.menuFullScreen,
            this.toolStripSeparator1,
            this.userFolder,
            this.about});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(154, 190);
            // 
            // menuRecord
            // 
            this.menuRecord.Enabled = false;
            this.menuRecord.Name = "menuRecord";
            this.menuRecord.Size = new System.Drawing.Size(153, 24);
            this.menuRecord.Text = "录像";
            this.menuRecord.Click += new System.EventHandler(this.menuRecord_Click);
            // 
            // menuCapture
            // 
            this.menuCapture.Enabled = false;
            this.menuCapture.Name = "menuCapture";
            this.menuCapture.Size = new System.Drawing.Size(153, 24);
            this.menuCapture.Text = "截图";
            this.menuCapture.Click += new System.EventHandler(this.menuCapture_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(150, 6);
            // 
            // menuSound
            // 
            this.menuSound.Enabled = false;
            this.menuSound.Name = "menuSound";
            this.menuSound.Size = new System.Drawing.Size(153, 24);
            this.menuSound.Text = "关闭声音";
            this.menuSound.Click += new System.EventHandler(this.menuSound_Click);
            // 
            // menuReset
            // 
            this.menuReset.Enabled = false;
            this.menuReset.Name = "menuReset";
            this.menuReset.Size = new System.Drawing.Size(153, 24);
            this.menuReset.Text = "重置镜头";
            this.menuReset.Visible = false;
            this.menuReset.Click += new System.EventHandler(this.menuReset_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(150, 6);
            // 
            // menuFullScreen
            // 
            this.menuFullScreen.Enabled = false;
            this.menuFullScreen.Name = "menuFullScreen";
            this.menuFullScreen.Size = new System.Drawing.Size(153, 24);
            this.menuFullScreen.Text = "全屏";
            this.menuFullScreen.Click += new System.EventHandler(this.menuFullScreen_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(150, 6);
            // 
            // userFolder
            // 
            this.userFolder.Name = "userFolder";
            this.userFolder.Size = new System.Drawing.Size(153, 24);
            this.userFolder.Text = "用户文件夹";
            this.userFolder.Click += new System.EventHandler(this.userFolder_Click);
            // 
            // about
            // 
            this.about.Name = "about";
            this.about.Size = new System.Drawing.Size(153, 24);
            this.about.Text = "关于";
            this.about.Click += new System.EventHandler(this.about_Click);
            // 
            // treeView1
            // 
            this.treeView1.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.treeView1.Location = new System.Drawing.Point(12, 12);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(234, 652);
            this.treeView1.TabIndex = 33;
            this.treeView1.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.TreeView1_NodeMouseDoubleClick);
            // 
            // RealPlayWnd
            // 
            this.RealPlayWnd.BackColor = System.Drawing.SystemColors.WindowText;
            this.RealPlayWnd.ContextMenuStrip = this.contextMenuStrip1;
            this.RealPlayWnd.Location = new System.Drawing.Point(263, 12);
            this.RealPlayWnd.Name = "RealPlayWnd";
            this.RealPlayWnd.Size = new System.Drawing.Size(920, 652);
            this.RealPlayWnd.TabIndex = 4;
            this.RealPlayWnd.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.label1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(1070, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 27);
            this.label1.TabIndex = 34;
            this.label1.Text = "正在录像";
            this.label1.Visible = false;
            // 
            // Preview
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(9, 23);
            this.ClientSize = new System.Drawing.Size(1195, 676);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.RealPlayWnd);
            this.Font = new System.Drawing.Font("微软雅黑", 10.28571F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Preview";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "来宾高级中学录播系统 ";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.RealPlayWnd)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(new Login());

        }

        public void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            if (dwBufSize > 0)
            {
                byte[] sData = new byte[dwBufSize];
                Marshal.Copy(pBuffer, sData, 0, (Int32)dwBufSize);
                string str = "实时流数据.ps";
                FileStream fs = new FileStream(str, FileMode.Create);
                int iLen = (int)dwBufSize;
                fs.Write(sData, 0, iLen);
                fs.Close();
            }
        }

        public void VoiceDataCallBack(int lVoiceComHandle, IntPtr pRecvDataBuffer, uint dwBufSize, byte byAudioFlag, System.IntPtr pUser)
        {
            byte[] sString = new byte[dwBufSize];
            Marshal.Copy(pRecvDataBuffer, sString, 0, (Int32)dwBufSize);

            /*
             * if (byAudioFlag == 0)
            {
                //将缓冲区里的音频数据写入文件 save the data into a file
                string str = "audio.pcm";
                FileStream fs = new FileStream(str, FileMode.Create);
                int iLen = (int)dwBufSize;
                fs.Write(sString, 0, iLen);
                fs.Close();
            }
            if (byAudioFlag == 1)
            {
                //将缓冲区里的音频数据写入文件 save the data into a file
                string str = "video.pcm";
                FileStream fs = new FileStream(str, FileMode.Create);
                int iLen = (int)dwBufSize;
                fs.Write(sString, 0, iLen);
                fs.Close();
            }
            */

        }

        void TreeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode selected = treeView1.SelectedNode;
            if (selected != null && selected.Name == "class")
            {
                //鼠标等待
                this.Cursor = Cursors.WaitCursor;
                if (selected.Parent.Text == "高二")
                {
                    //鼠标恢复
                    this.Cursor = Cursors.Arrow;
                    MessageBox.Show("不支持高二设备", "错误");
                }
                else
                {
                    if (m_bRecord)
                    {
                        //鼠标恢复
                        this.Cursor = Cursors.Arrow;
                        MessageBox.Show("当前正在录像，请先停止", "提示");
                    }
                    else
                    {
                        Play(selected);
                    }

                }
                //Console.WriteLine(selected.Tag);
            }
        }
        private void Play(TreeNode selected)
        {
            //1、登录
            //string DVRIPAddress = "172.16.1." + (140 + Convert.ToInt16(selected.Tag)); //设备IP地址或者域名
            string DVRIPAddress = selected.Tag.ToString();//Int16 DVRPortNumber = Int16.Parse(textBoxPort.Text);//设备服务端口号
            Int16 DVRPortNumber = 8000;
            string DVRUserName = "admin";//设备登录用户名
            string DVRPassword = "12345";//设备登录密码

            CHCNetSDK.NET_DVR_DEVICEINFO_V30 DeviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V30();
            //登录设备 Login the device
            m_lUserID = CHCNetSDK.NET_DVR_Login_V30(DVRIPAddress, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo);
            if (m_lUserID < 0)
            {
                //鼠标恢复
                this.Cursor = Cursors.Arrow;
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "错误码 = " + iLastErr + ",无法连接到ip：" + DVRIPAddress+"。\n原因可能为：录播摄像头断电或网络线路故障"; //登录失败，输出错误号
                MessageBox.Show(str);
                return;
            }
            else
            {
                //登录成功
                Btn_enable(1);
                //2、播放
                //关闭预览与语音
                if (m_lRealHandle > 0 || m_bTalk)
                {
                    //停止语音对讲 Stop two-way talk
                    CHCNetSDK.NET_DVR_StopVoiceCom(lVoiceComHandle);
                    CHCNetSDK.NET_DVR_StopRealPlay(m_lRealHandle);
                    m_lRealHandle = -1;
                    m_bTalk = false;
                }
                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();
                lpPreviewInfo.hPlayWnd = RealPlayWnd.Handle;//预览窗口
                lpPreviewInfo.lChannel = 1;//预te览的设备通道
                lpPreviewInfo.dwStreamType = 0;//码流类型：0-主码流，1-子码流，2-码流3，3-码流4，以此类推
                lpPreviewInfo.dwLinkMode = 0;//连接方式：0- TCP方式，1- UDP方式，2- 多播方式，3- RTP方式，4-RTP/RTSP，5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = true; //0- 非阻塞取流，1- 阻塞取流
                lpPreviewInfo.dwDisplayBufNum = 1; //播放库播放缓冲区最大缓冲帧数
                lpPreviewInfo.byProtoType = 0;
                lpPreviewInfo.byPreviewMode = 0;

                if (RealData == null)
                {
                    RealData = new CHCNetSDK.REALDATACALLBACK(RealDataCallBack);//预览实时流回调函数
                }

                IntPtr pUser = new IntPtr();//用户数据

                //打开预览 Start live view 
                m_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, null/*RealData*/, pUser);
                if (m_lRealHandle < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "预览失败, error code= " + iLastErr; //预览失败，输出错误号
                    //鼠标恢复
                    this.Cursor = Cursors.Arrow;
                    MessageBox.Show(str);
                    return;
                }
                else
                {
                    //鼠标恢复
                    this.Cursor = Cursors.Arrow;
                    //预览成功
                    //3、开启声音
                    //开始语音对讲 Start two-way talk
                    VoiceData = new CHCNetSDK.VOICEDATACALLBACKV30(VoiceDataCallBack);//预览实时流回调函数

                    lVoiceComHandle = CHCNetSDK.NET_DVR_StartVoiceCom_V30(m_lUserID, 1, true, VoiceData, IntPtr.Zero);
                    //bNeedCBNoEncData [in]需要回调的语音数据类型：0- 编码后的语音数据，1- 编码前的PCM原始数据

                    if (lVoiceComHandle < 0)
                    {
                        iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                        str = "声音开启错误, 进入静音模式。\n此为电脑系统环境问题，无法修复";
                        MessageBox.Show(str);
                    }
                    m_bTalk = true;
                }
            }
        }

        private void Debug(string msg)
        {
            MessageBox.Show(msg);
        }

        //点击关闭按钮
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dr = MessageBox.Show("是否退出?", "提示:", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

            if (dr == DialogResult.OK)   //如果单击“是”按钮
            {
                e.Cancel = false;
                //停止预览 Stop live view 
                if (m_lRealHandle >= 0)
                {
                    CHCNetSDK.NET_DVR_StopRealPlay(m_lRealHandle);
                    m_lRealHandle = -1;
                }

                //注销登录 Logout the device
                if (m_lUserID >= 0)
                {
                    CHCNetSDK.NET_DVR_Logout(m_lUserID);
                    m_lUserID = -1;
                }
                CHCNetSDK.NET_DVR_Cleanup();
                //File.Delete("audio.pcm");
                //File.Delete("video.pcm");
                //彻底退出
                System.Environment.Exit(0);
            }
            else if (dr == DialogResult.Cancel)
            {
                e.Cancel = true;                  //不执行操作
            }
        }
        //控制按钮是否可用
        private void Btn_enable(int action)
        {
            if (action > 0)
            {
                this.menuRecord.Enabled = true;
                this.menuSound.Enabled = true;
                this.menuCapture.Enabled = true;
                this.menuReset.Enabled = true;
                this.menuFullScreen.Enabled = true;
            }
            else
            {
                this.menuRecord.Enabled = false;
                this.menuSound.Enabled = false;
                this.menuCapture.Enabled = false;
                this.menuReset.Enabled = false;
                this.menuFullScreen.Enabled = false;

            }
        }

        private void RealPlayWnd_Click(object sender, EventArgs e)
        {
            FullScreenFun();
        }

        private void menuCapture_Click(object sender, EventArgs e)
        {
            string sJpegPicFileName;
            //图片保存路径和文件名 the path and file name to save
            sJpegPicFileName = DateTime.Now.ToUniversalTime().ToString().Replace("/", "-").Replace(":", "-") + ".jpg";
            int lChannel = 1; //通道号 Channel number

            CHCNetSDK.NET_DVR_JPEGPARA lpJpegPara = new CHCNetSDK.NET_DVR_JPEGPARA();
            lpJpegPara.wPicQuality = 0; //图像质量 Image quality
            lpJpegPara.wPicSize = 0xff; //抓图分辨率 Picture size: 2- 4CIF，0xff- Auto(使用当前码流分辨率)，抓图分辨率需要设备支持，更多取值请参考SDK文档

            //JPEG抓图 Capture a JPEG picture
            if (!CHCNetSDK.NET_DVR_CaptureJPEGPicture(m_lUserID, lChannel, ref lpJpegPara, UserDir + sJpegPicFileName))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "发生错误, error code= " + iLastErr;
                MessageBox.Show(str);
                return;
            }
            else
            {
                str = "截图成功，文件名为: " + sJpegPicFileName + "，请打开用户文件夹查看";
                MessageBox.Show(str);
            }
            return;
        }

        private void menuSound_Click(object sender, EventArgs e)
        {
            if (m_bTalk)
            {
                CHCNetSDK.NET_DVR_StopVoiceCom(lVoiceComHandle);
                menuSound.Text = "开启声音";
                m_bTalk = false;
            }
            else
            {
                CHCNetSDK.VOICEDATACALLBACKV30 VoiceData = new CHCNetSDK.VOICEDATACALLBACKV30(VoiceDataCallBack);//预览实时流回调函数
                lVoiceComHandle = CHCNetSDK.NET_DVR_StartVoiceCom_V30(m_lUserID, 1, true, VoiceData, IntPtr.Zero);
                //bNeedCBNoEncData [in]需要回调的语音数据类型：0- 编码后的语音数据，1- 编码前的PCM原始数据
                menuSound.Text = "关闭声音";
                if (lVoiceComHandle < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "声音开启错误, 进入静音模式";
                    MessageBox.Show(str);
                }
                m_bTalk = true;
            }
        }

        private void menuReset_Click(object sender, EventArgs e)
        {
            if (m_lRealHandle > 0)
            {
                //预览已开启，可以重置角度
                CHCNetSDK.NET_DVR_PTZPreset(m_lRealHandle, 39, 8);
            }
        }

        private void menuFullScreen_Click(object sender, EventArgs e)
        {
            FullScreenFun();
        }

        //全屏函数
        private void FullScreenFun()
        {
            if (is_FullScreen)
            {
                //让程序退出全屏
                this.Height = winSize["origin_height"];
                this.Width = winSize["origin_width"];
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                RealPlayWnd.Dock = DockStyle.None;
                this.WindowState = FormWindowState.Normal;
                this.SetDesktopLocation(winSize["full_width"] / 2 - winSize["origin_width"] / 2, winSize["full_height"] / 2 - winSize["origin_height"] / 2);
                //显示控件
                treeView1.Visible = true;
                is_FullScreen = false;
                menuFullScreen.Text = "全屏";
            }
            else
            {
                //让程序全屏
                this.Height = winSize["full_height"];
                this.Width = winSize["full_width"];
                this.SetDesktopLocation(0, 0);
                this.FormBorderStyle = FormBorderStyle.None;
                RealPlayWnd.Dock = DockStyle.Fill;
                this.WindowState = FormWindowState.Maximized;
                //隐藏控件
                treeView1.Visible = false;
                is_FullScreen = true;
                menuFullScreen.Text = "退出全屏";

            }
        }

        private void menuRecord_Click(object sender, EventArgs e)
        {
            //录像保存路径和文件名 the path and file name to save
            string sVideoFileName;
            sVideoFileName = UserDir + "录像" + DateTime.Now.ToUniversalTime().ToString().Replace("/", "-").Replace(":", "-") + ".mp4";

            if (m_bRecord == false)
            {
                //强制I帧 Make one key frame
                int lChannel = 1; //通道号 Channel number
                CHCNetSDK.NET_DVR_MakeKeyFrame(m_lUserID, lChannel);

                //开始录像 Start recording
                if (!CHCNetSDK.NET_DVR_SaveRealData(m_lRealHandle, sVideoFileName))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_SaveRealData failed, error code= " + iLastErr;
                    return;
                }
                else
                {
                    menuRecord.Text = "停止录像";
                    m_bRecord = true;
                    label1.Visible = true;
                    t.Enabled = true;

                }
            }
            else
            {
                //停止录像 Stop recording
                if (!CHCNetSDK.NET_DVR_StopSaveRealData(m_lRealHandle))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_StopSaveRealData failed, error code= " + iLastErr;
                    return;
                }
                else
                {
                    str = "NET_DVR_StopSaveRealData succ and the saved file is " + sVideoFileName;
                    menuRecord.Text = "录像";
                    label1.Visible = false;
                    m_bRecord = false;
                    Record record = new Record();
                    record.Show();
                    t.Enabled = false;
                    label1.Visible = false;
                }
            }
            return;
        }

        public void theout(object source, System.Timers.ElapsedEventArgs e)
        {
            label1.Visible = !label1.Visible;
        }

        private void userFolder_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.StartupPath + UserDir);
        }

        private void about_Click(object sender, EventArgs e)
        {
            Dialog about = new Dialog();
            about.ShowDialog();
        }
    }
}
