using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace Covariant_Script_Shell
{
    public partial class Form1 : Form
    {
        private Process CsProcess = null;
        private Thread Worker1 = null;
        private Thread Worker2 = null;
        private int CharBuffSize = 1;
        private string LineBuff = "";
        internal ProgramSettings Settings { get; set; } = new ProgramSettings();
        public Form1()
        {
            InitializeComponent();
            Settings.InitDefault();
            ReadRegistry();
            StartProcess();
        }

        private void ReadRegistry()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(Configs.Names.CsRegistry);
            Object bin_path = key.GetValue(Configs.RegistryKey.BinPath);
            Object ipt_path = key.GetValue(Configs.RegistryKey.ImportPath);
            Object log_path = key.GetValue(Configs.RegistryKey.LogPath);
            Object font_size = key.GetValue(Configs.RegistryKey.FontSize);
            Object tab_width = key.GetValue(Configs.RegistryKey.TabWidth);
            Object time_over = key.GetValue(Configs.RegistryKey.TimeOver);
            Object encoding = key.GetValue(Configs.RegistryKey.Encoding);
            if (bin_path == null || ipt_path == null || log_path == null || font_size == null || tab_width == null || time_over == null || encoding == null)
            {
                key.Close();
                Settings.InitDefault();
                SaveRegistry();
            }
            else
            {
                Settings.program_path = bin_path.ToString();
                Settings.import_path = ipt_path.ToString();
                Settings.log_path = log_path.ToString();
                Settings.font_size = int.Parse(font_size.ToString());
                Settings.tab_width = int.Parse(tab_width.ToString());
                Settings.time_over = int.Parse(time_over.ToString());
                Settings.encoding = bool.Parse(encoding.ToString());
                key.Close();
            }
        }

        private void SaveRegistry()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(Configs.Names.CsRegistry);
            key.SetValue(Configs.RegistryKey.BinPath, Settings.program_path);
            key.SetValue(Configs.RegistryKey.ImportPath, Settings.import_path);
            key.SetValue(Configs.RegistryKey.LogPath, Settings.log_path);
            key.SetValue(Configs.RegistryKey.FontSize, Settings.font_size);
            key.SetValue(Configs.RegistryKey.TabWidth, Settings.tab_width);
            key.SetValue(Configs.RegistryKey.TimeOver, Settings.time_over);
            key.SetValue(Configs.RegistryKey.Encoding, Settings.encoding);
        }

        private void DownloadCompoents()
        {
            try
            {
                Process.Start(Settings.program_path + Configs.Names.CsInstBin);
            }
            catch (Exception)
            {
                MessageBox.Show("未找到Covariant Script安装程序", "Covariant Script Shell", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void StdOutWorker()
        {
            while (CsProcess != null && !CsProcess.HasExited)
            {
                char[] bs = new char[CharBuffSize];
                if (CsProcess.StandardOutput.Read(bs, 0, CharBuffSize) > 0)
                    BeginInvoke(new Action(() => { textBox1.AppendText(new string(bs)); }));
                Thread.Sleep(1);
            }
        }

        private void StdErrWorker()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            while (CsProcess != null && !CsProcess.HasExited)
            {
                char[] bs = new char[CharBuffSize];
                if (CsProcess.StandardError.Read(bs, 0, CharBuffSize) > 0)
                    BeginInvoke(new Action(() => { textBox1.AppendText(new string(bs)); }));
            }
        }

        void StartProcess()
        {
            if (CsProcess != null && !CsProcess.HasExited)
            {
                MessageBox.Show("不能同时运行两个进程", "Covariant Script Shell", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Settings.program_path + Configs.Names.CsBin,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                Arguments = ""
            };
            try
            {
                CsProcess = new Process
                {
                    StartInfo = psi
                };
                CsProcess.Start();
                Worker1 = new Thread(new ThreadStart(StdOutWorker));
                Worker2 = new Thread(new ThreadStart(StdErrWorker));
                Worker1.Start();
                Worker2.Start();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                CsProcess = null;
                if (MessageBox.Show("缺少必要组件，是否下载？", "Covariant Script Shell", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                    DownloadCompoents();
            }
        }

        void KillProcess()
        {
            if (CsProcess != null && !CsProcess.HasExited)
            {
                Worker1.Abort();
                Worker2.Abort();
                CsProcess.Kill();
            }
        }

        private void EnterLine()
        {
            if (CsProcess == null || CsProcess.HasExited)
                StartProcess();
            CsProcess.StandardInput.WriteLine(LineBuff);
            LineBuff = "";
        }

        private bool keyValid(char key)
        {
            return key != '\b';
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (keyValid(e.KeyChar))
            {
                LineBuff += e.KeyChar;
                textBox1.AppendText(e.KeyChar.ToString());
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    EnterLine();
                    textBox1.AppendText("\r\n");
                    break;
                case Keys.Back:
                    if (LineBuff.Length > 0)
                    {
                        LineBuff = LineBuff.Substring(0, LineBuff.Length - 1);
                        textBox1.Text = textBox1.Text.Substring(0, textBox1.TextLength - 1);
                        textBox1.Select(textBox1.TextLength, 0);
                        textBox1.ScrollToCaret();
                    }
                    break;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            KillProcess();
        }

        private void rEBOOTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartProcess();
        }

        private void sTOPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            KillProcess();
            textBox1.AppendText("\r\n[Process exit with code " + CsProcess.ExitCode + "]\r\n");
        }

        private void cLEARToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            Process.Start(Configs.Urls.WebSite);
        }
    }
}