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
    /// Form1 ��ժҪ˵����
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
        private string UserDir = "./�û�/";//�û������ļ���
        private JObject config = null;//����
        private bool is_FullScreen = false;
        private bool m_bRecord = false;
        private Dictionary<string, int> winSize = new Dictionary<string, int>();
        System.Timers.Timer t = new System.Timers.Timer(1000);   //ʵ����Timer�࣬���ü��ʱ��Ϊ10000���룻   
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
            // Windows ���������֧���������
            //
            InitializeComponent();

            //��ʼ����ʱ��
            t.Elapsed += new System.Timers.ElapsedEventHandler(theout); //����ʱ���ʱ��ִ���¼���   
            t.AutoReset = true;   //������ִ��һ�Σ�false������һֱִ��(true)��   
            t.SynchronizingObject = this;

            //˫��ȫ���¼�
            this.RealPlayWnd.DoubleClick += new System.EventHandler(this.RealPlayWnd_Click);


            //�洢�ߴ�
            Rectangle rect = new Rectangle();
            rect = Screen.GetWorkingArea(this);
            winSize.Add("full_width", rect.Width);
            winSize.Add("full_height", rect.Height);
            winSize.Add("origin_width", this.Width);
            winSize.Add("origin_height", this.Height);
            //��������
            JObject json = (JObject)JsonConvert.DeserializeObject(config);
            this.config = json;
            //��ȡ��������
            int is_first_startup = int.Parse(Util.LoadConfig()["first_startup"].ToString());
            if (is_first_startup == 1)
            {
                //��һ������
                MessageBox.Show("�ڲ����������Ҽ����򿪹��ܲ˵����緢����������ϵ������", "��ʾ");
                //�޸�����
                JToken t = Util.LoadConfig();
                t["first_startup"] = 0;
                string content = t.ToString();
                System.IO.File.WriteAllText(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\\localConfig.ini", content, Encoding.UTF8);
            }

            //Console.WriteLine((JObject)JsonConvert.DeserializeObject(json.ToString()));
            /*
             * Զ����������ʾ��
             {
              "code": 1,//״̬��
              "grade": "[1,1,1]",//��¼�˻��꼶
              "configuration": {
                "id": "1",//��������ʵ������
                "grade_1": "{\"ip\":\"172.16.0.\",\"first\":201,\"num\":25}",//��һ��ipǰ���Σ���һ���༶���ĶΣ��༶��
                "grade_2": "{\"ip\":\"172.16.1.\",\"first\":201,\"num\":25}",//�߶�
                "grade_3": "{\"ip\":\"172.16.1.\",\"first\":141,\"num\":25}",//����
                "remote_notice": "{\"code\":0}",//����֪ͨ
                "has_update": "{\"code\":1,\"url\":\"www.baidu.com\"}"//�������
              }
            }
              
             */

            //��ʼ����
            TreeNode grade_root1 = null, grade_root2 = null, grade_root3 = null;
            for (int grade = 0; grade < 3; grade++)
            {
                //��ȡ��Ӧ�꼶����
                JObject grade_config = (JObject)JsonConvert.DeserializeObject(this.config["configuration"]["grade_" + (grade + 1)].ToString());
                //�༶����
                int length = int.Parse(grade_config["num"].ToString());
                //������ʱ����
                TreeNode[] arr = new TreeNode[length];
                //���ɰ༶�ڵ�
                for (int i = 0; i < length; i++)
                {
                    string num = (i + 1).ToString();
                    TreeNode treeNode = new TreeNode(num + "��")
                    {
                        Name = "class",
                        Tag = grade_config["ip"].ToString()
                        + (int.Parse(grade_config["first"].ToString()) + i),
                        Text = num + "��"
                    };
                    arr[i] = treeNode;
                }
                switch (grade)
                {
                    case 0:
                        grade_root1 = new TreeNode("��һ", arr);
                        this.treeView1.Nodes.Add(grade_root1);
                        break;
                    case 1:
                        grade_root2 = new TreeNode("�߶�", arr);
                        this.treeView1.Nodes.Add(grade_root2);
                        break;
                    case 2:
                        grade_root3 = new TreeNode("����", arr);
                        this.treeView1.Nodes.Add(grade_root3);
                        break;

                }

            }

            //��������
            //this.treeView1.ExpandAll();
            //�����꼶
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
                //����SDK��־ To save the SDK log
                CHCNetSDK.NET_DVR_SetLogToFile(3, "C:\\SdkLog\\", true);
            }
            //
            // TODO: �� InitializeComponent ���ú�����κι��캯������
            //
        }

        /// <summary>
        /// ������������ʹ�õ���Դ��
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

        #region Windows ������������ɵĴ���
        /// <summary>
        /// �����֧������ķ��� - ��Ҫʹ�ô���༭���޸�
        /// �˷��������ݡ�
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
            this.menuRecord.Text = "¼��";
            this.menuRecord.Click += new System.EventHandler(this.menuRecord_Click);
            // 
            // menuCapture
            // 
            this.menuCapture.Enabled = false;
            this.menuCapture.Name = "menuCapture";
            this.menuCapture.Size = new System.Drawing.Size(153, 24);
            this.menuCapture.Text = "��ͼ";
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
            this.menuSound.Text = "�ر�����";
            this.menuSound.Click += new System.EventHandler(this.menuSound_Click);
            // 
            // menuReset
            // 
            this.menuReset.Enabled = false;
            this.menuReset.Name = "menuReset";
            this.menuReset.Size = new System.Drawing.Size(153, 24);
            this.menuReset.Text = "���þ�ͷ";
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
            this.menuFullScreen.Text = "ȫ��";
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
            this.userFolder.Text = "�û��ļ���";
            this.userFolder.Click += new System.EventHandler(this.userFolder_Click);
            // 
            // about
            // 
            this.about.Name = "about";
            this.about.Size = new System.Drawing.Size(153, 24);
            this.about.Text = "����";
            this.about.Click += new System.EventHandler(this.about_Click);
            // 
            // treeView1
            // 
            this.treeView1.Font = new System.Drawing.Font("΢���ź�", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
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
            this.label1.Font = new System.Drawing.Font("΢���ź�", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(1070, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 27);
            this.label1.TabIndex = 34;
            this.label1.Text = "����¼��";
            this.label1.Visible = false;
            // 
            // Preview
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(9, 23);
            this.ClientSize = new System.Drawing.Size(1195, 676);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.RealPlayWnd);
            this.Font = new System.Drawing.Font("΢���ź�", 10.28571F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Preview";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "�����߼���ѧ¼��ϵͳ ";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.RealPlayWnd)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        /// <summary>
        /// Ӧ�ó��������ڵ㡣
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
                string str = "ʵʱ������.ps";
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
                //�������������Ƶ����д���ļ� save the data into a file
                string str = "audio.pcm";
                FileStream fs = new FileStream(str, FileMode.Create);
                int iLen = (int)dwBufSize;
                fs.Write(sString, 0, iLen);
                fs.Close();
            }
            if (byAudioFlag == 1)
            {
                //�������������Ƶ����д���ļ� save the data into a file
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
                //���ȴ�
                this.Cursor = Cursors.WaitCursor;
                if (selected.Parent.Text == "�߶�")
                {
                    //���ָ�
                    this.Cursor = Cursors.Arrow;
                    MessageBox.Show("��֧�ָ߶��豸", "����");
                }
                else
                {
                    if (m_bRecord)
                    {
                        //���ָ�
                        this.Cursor = Cursors.Arrow;
                        MessageBox.Show("��ǰ����¼������ֹͣ", "��ʾ");
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
            //1����¼
            //string DVRIPAddress = "172.16.1." + (140 + Convert.ToInt16(selected.Tag)); //�豸IP��ַ��������
            string DVRIPAddress = selected.Tag.ToString();//Int16 DVRPortNumber = Int16.Parse(textBoxPort.Text);//�豸����˿ں�
            Int16 DVRPortNumber = 8000;
            string DVRUserName = "admin";//�豸��¼�û���
            string DVRPassword = "12345";//�豸��¼����

            CHCNetSDK.NET_DVR_DEVICEINFO_V30 DeviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V30();
            //��¼�豸 Login the device
            m_lUserID = CHCNetSDK.NET_DVR_Login_V30(DVRIPAddress, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo);
            if (m_lUserID < 0)
            {
                //���ָ�
                this.Cursor = Cursors.Arrow;
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "������ = " + iLastErr + ",�޷����ӵ�ip��" + DVRIPAddress+"��\nԭ�����Ϊ��¼������ͷ�ϵ��������·����"; //��¼ʧ�ܣ���������
                MessageBox.Show(str);
                return;
            }
            else
            {
                //��¼�ɹ�
                Btn_enable(1);
                //2������
                //�ر�Ԥ��������
                if (m_lRealHandle > 0 || m_bTalk)
                {
                    //ֹͣ�����Խ� Stop two-way talk
                    CHCNetSDK.NET_DVR_StopVoiceCom(lVoiceComHandle);
                    CHCNetSDK.NET_DVR_StopRealPlay(m_lRealHandle);
                    m_lRealHandle = -1;
                    m_bTalk = false;
                }
                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();
                lpPreviewInfo.hPlayWnd = RealPlayWnd.Handle;//Ԥ������
                lpPreviewInfo.lChannel = 1;//Ԥte�����豸ͨ��
                lpPreviewInfo.dwStreamType = 0;//�������ͣ�0-��������1-��������2-����3��3-����4���Դ�����
                lpPreviewInfo.dwLinkMode = 0;//���ӷ�ʽ��0- TCP��ʽ��1- UDP��ʽ��2- �ಥ��ʽ��3- RTP��ʽ��4-RTP/RTSP��5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = true; //0- ������ȡ����1- ����ȡ��
                lpPreviewInfo.dwDisplayBufNum = 1; //���ſⲥ�Ż�������󻺳�֡��
                lpPreviewInfo.byProtoType = 0;
                lpPreviewInfo.byPreviewMode = 0;

                if (RealData == null)
                {
                    RealData = new CHCNetSDK.REALDATACALLBACK(RealDataCallBack);//Ԥ��ʵʱ���ص�����
                }

                IntPtr pUser = new IntPtr();//�û�����

                //��Ԥ�� Start live view 
                m_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, null/*RealData*/, pUser);
                if (m_lRealHandle < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "Ԥ��ʧ��, error code= " + iLastErr; //Ԥ��ʧ�ܣ���������
                    //���ָ�
                    this.Cursor = Cursors.Arrow;
                    MessageBox.Show(str);
                    return;
                }
                else
                {
                    //���ָ�
                    this.Cursor = Cursors.Arrow;
                    //Ԥ���ɹ�
                    //3����������
                    //��ʼ�����Խ� Start two-way talk
                    VoiceData = new CHCNetSDK.VOICEDATACALLBACKV30(VoiceDataCallBack);//Ԥ��ʵʱ���ص�����

                    lVoiceComHandle = CHCNetSDK.NET_DVR_StartVoiceCom_V30(m_lUserID, 1, true, VoiceData, IntPtr.Zero);
                    //bNeedCBNoEncData [in]��Ҫ�ص��������������ͣ�0- �������������ݣ�1- ����ǰ��PCMԭʼ����

                    if (lVoiceComHandle < 0)
                    {
                        iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                        str = "������������, ���뾲��ģʽ��\n��Ϊ����ϵͳ�������⣬�޷��޸�";
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

        //����رհ�ť
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult dr = MessageBox.Show("�Ƿ��˳�?", "��ʾ:", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

            if (dr == DialogResult.OK)   //����������ǡ���ť
            {
                e.Cancel = false;
                //ֹͣԤ�� Stop live view 
                if (m_lRealHandle >= 0)
                {
                    CHCNetSDK.NET_DVR_StopRealPlay(m_lRealHandle);
                    m_lRealHandle = -1;
                }

                //ע����¼ Logout the device
                if (m_lUserID >= 0)
                {
                    CHCNetSDK.NET_DVR_Logout(m_lUserID);
                    m_lUserID = -1;
                }
                CHCNetSDK.NET_DVR_Cleanup();
                //File.Delete("audio.pcm");
                //File.Delete("video.pcm");
                //�����˳�
                System.Environment.Exit(0);
            }
            else if (dr == DialogResult.Cancel)
            {
                e.Cancel = true;                  //��ִ�в���
            }
        }
        //���ư�ť�Ƿ����
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
            //ͼƬ����·�����ļ��� the path and file name to save
            sJpegPicFileName = DateTime.Now.ToUniversalTime().ToString().Replace("/", "-").Replace(":", "-") + ".jpg";
            int lChannel = 1; //ͨ���� Channel number

            CHCNetSDK.NET_DVR_JPEGPARA lpJpegPara = new CHCNetSDK.NET_DVR_JPEGPARA();
            lpJpegPara.wPicQuality = 0; //ͼ������ Image quality
            lpJpegPara.wPicSize = 0xff; //ץͼ�ֱ��� Picture size: 2- 4CIF��0xff- Auto(ʹ�õ�ǰ�����ֱ���)��ץͼ�ֱ�����Ҫ�豸֧�֣�����ȡֵ��ο�SDK�ĵ�

            //JPEGץͼ Capture a JPEG picture
            if (!CHCNetSDK.NET_DVR_CaptureJPEGPicture(m_lUserID, lChannel, ref lpJpegPara, UserDir + sJpegPicFileName))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                str = "��������, error code= " + iLastErr;
                MessageBox.Show(str);
                return;
            }
            else
            {
                str = "��ͼ�ɹ����ļ���Ϊ: " + sJpegPicFileName + "������û��ļ��в鿴";
                MessageBox.Show(str);
            }
            return;
        }

        private void menuSound_Click(object sender, EventArgs e)
        {
            if (m_bTalk)
            {
                CHCNetSDK.NET_DVR_StopVoiceCom(lVoiceComHandle);
                menuSound.Text = "��������";
                m_bTalk = false;
            }
            else
            {
                CHCNetSDK.VOICEDATACALLBACKV30 VoiceData = new CHCNetSDK.VOICEDATACALLBACKV30(VoiceDataCallBack);//Ԥ��ʵʱ���ص�����
                lVoiceComHandle = CHCNetSDK.NET_DVR_StartVoiceCom_V30(m_lUserID, 1, true, VoiceData, IntPtr.Zero);
                //bNeedCBNoEncData [in]��Ҫ�ص��������������ͣ�0- �������������ݣ�1- ����ǰ��PCMԭʼ����
                menuSound.Text = "�ر�����";
                if (lVoiceComHandle < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "������������, ���뾲��ģʽ";
                    MessageBox.Show(str);
                }
                m_bTalk = true;
            }
        }

        private void menuReset_Click(object sender, EventArgs e)
        {
            if (m_lRealHandle > 0)
            {
                //Ԥ���ѿ������������ýǶ�
                CHCNetSDK.NET_DVR_PTZPreset(m_lRealHandle, 39, 8);
            }
        }

        private void menuFullScreen_Click(object sender, EventArgs e)
        {
            FullScreenFun();
        }

        //ȫ������
        private void FullScreenFun()
        {
            if (is_FullScreen)
            {
                //�ó����˳�ȫ��
                this.Height = winSize["origin_height"];
                this.Width = winSize["origin_width"];
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                RealPlayWnd.Dock = DockStyle.None;
                this.WindowState = FormWindowState.Normal;
                this.SetDesktopLocation(winSize["full_width"] / 2 - winSize["origin_width"] / 2, winSize["full_height"] / 2 - winSize["origin_height"] / 2);
                //��ʾ�ؼ�
                treeView1.Visible = true;
                is_FullScreen = false;
                menuFullScreen.Text = "ȫ��";
            }
            else
            {
                //�ó���ȫ��
                this.Height = winSize["full_height"];
                this.Width = winSize["full_width"];
                this.SetDesktopLocation(0, 0);
                this.FormBorderStyle = FormBorderStyle.None;
                RealPlayWnd.Dock = DockStyle.Fill;
                this.WindowState = FormWindowState.Maximized;
                //���ؿؼ�
                treeView1.Visible = false;
                is_FullScreen = true;
                menuFullScreen.Text = "�˳�ȫ��";

            }
        }

        private void menuRecord_Click(object sender, EventArgs e)
        {
            //¼�񱣴�·�����ļ��� the path and file name to save
            string sVideoFileName;
            sVideoFileName = UserDir + "¼��" + DateTime.Now.ToUniversalTime().ToString().Replace("/", "-").Replace(":", "-") + ".mp4";

            if (m_bRecord == false)
            {
                //ǿ��I֡ Make one key frame
                int lChannel = 1; //ͨ���� Channel number
                CHCNetSDK.NET_DVR_MakeKeyFrame(m_lUserID, lChannel);

                //��ʼ¼�� Start recording
                if (!CHCNetSDK.NET_DVR_SaveRealData(m_lRealHandle, sVideoFileName))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_SaveRealData failed, error code= " + iLastErr;
                    return;
                }
                else
                {
                    menuRecord.Text = "ֹͣ¼��";
                    m_bRecord = true;
                    label1.Visible = true;
                    t.Enabled = true;

                }
            }
            else
            {
                //ֹͣ¼�� Stop recording
                if (!CHCNetSDK.NET_DVR_StopSaveRealData(m_lRealHandle))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "NET_DVR_StopSaveRealData failed, error code= " + iLastErr;
                    return;
                }
                else
                {
                    str = "NET_DVR_StopSaveRealData succ and the saved file is " + sVideoFileName;
                    menuRecord.Text = "¼��";
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
