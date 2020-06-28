using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoNETBlock
{
    public partial class Form1 : Form
    {
        private const int WM_MOUSEHOVER = 0x2a1;
        private const int WM_MOUSELEAVE = 0x2a3;
        private const int WM_MOUSEMOVE = 0x200; // 窗体客户区内移动
        private const int WM_NCMOUSEMOVE = 0xa0; // 窗体内非客户区移动（非客户区为：窗体的标题栏及窗 ）
        private const int TME_HOVER = 0x1;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int VK_LBUTTON = 0x01;

        private bool hasRule = false;
        public bool HasRule 
        { 
            get { return hasRule; } 
            set 
            {
                button1.Text = value ? "删除规则" : "添加规则";
                hasRule = value; 
            } 
        }

        Helper helper = new Helper();

        public Form1()
        {
            CheckFirewallState();

            InitializeComponent();
            this.Location = new Point(0, Screen.PrimaryScreen.WorkingArea.Height - this.Size.Height);

            HasRule = helper.hasSetRule();
        }

        protected bool isMouseEnter()
        {
            var wPoint = this.Location;
            //var wPoint2 = this.PointToScreen(new Point(this.ClientSize.Width - this.Size.Width, this.ClientSize.Height - this.Size.Height)); // 客户区坐标
            var mPoint = Control.MousePosition;
            if (mPoint.X < wPoint.X + 5 ||
                mPoint.X > wPoint.X + this.Size.Width - 5 ||
                mPoint.Y < wPoint.Y + 5 ||
                mPoint.Y > wPoint.Y + this.Size.Height - 5
                )
            {
                return false;
            }
            return true;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0xA3://禁止双击最大化
                    break;
                case WM_MOUSEMOVE:
                case WM_NCMOUSEMOVE:
                    var wPoint = this.PointToScreen(new Point(0, this.ClientSize.Height - this.Size.Height)); // 客户区坐标
                    var mPoint = Control.MousePosition;
                    if (isMouseEnter())
                    {
                        this.Opacity = 1;
                    }
                    else
                    {
                        this.Opacity = 0.6;
                    }
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void CheckFirewallState()
        {
            var state = helper.IsFireWallOpen();
            if(state[0] && state[1] && state[2])
            {
                return;
            }
            var sb = new StringBuilder("防火墙未全部开启，可能导致本工具无法生效:\r\n");
            if (!state[0]) sb.AppendLine("[域网络未开启]");
            if (!state[1]) sb.AppendLine("[专用网络未开启]");
            if (!state[2]) sb.AppendLine("[公用网络未开启]");
            MessageBox.Show(sb.ToString(), "请检查防火墙状态", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string GetHearthstonePath()
        {
            OpenFileDialog dialog = new OpenFileDialog() { Title = "请选择炉石的启动文件(Hearthstone.exe)", CheckFileExists = true, Filter = "炉石启动程序|Hearthstone.exe" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            return null;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(HasRule)
            {
                try
                {
                    helper.RemoveHearthstoneNETBlockRule();
                    HasRule = false;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            var appPath = GetHearthstonePath();
            if(!string.IsNullOrEmpty(appPath))
            {
                try
                {
                    if(helper.CreateHearthstoneNETBlockRule(appPath))
                    {
                        MessageBox.Show("防火墙规则添加成功", "成功");
                        HasRule = true;
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Task task = new Task(() =>
            {
                var btn = button2;
                var text = btn.Text;
                try
                {
                    helper.SetHearthstoneBlock(true);
                    btn.BeginInvoke((Action)(() => {
                        btn.Enabled = false;
                        btn.Text = "正在处理中...";
                    }));
                    Thread.Sleep(4000);
                    helper.SetHearthstoneBlock(false);

                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    // Thread.Sleep(2000); // 等待炉石反应时间
                    btn.BeginInvoke((Action)(() => {
                        btn.Enabled = true;
                        btn.Text = text;
                    }));
                }
            });
            task.Start();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = checkBox1.Checked;
        }
    }
}
