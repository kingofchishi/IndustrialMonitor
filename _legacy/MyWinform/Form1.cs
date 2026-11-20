using System.Threading.Tasks;

namespace MyWinform
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            if(btn_test2.Enabled == false)
            {
                btn_test2.Text = "禁用";
                btn_test2.BackColor = SystemColors.Control;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            btn_test.Text = "什么玩意";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            label1.Text = "OK";
        }

        private void btn_test_Click(object sender, EventArgs e)
        {
            label1.Text = "OK";
            btn_test2.Enabled = true;

        }
        private async Task btn_test2_Click(object sender, EventArgs e)
        {
            float f = await FuncAsync(label1.Text);
        }

        async Task<float> FuncAsync(string str)
        {
            await Task.Run<string>(
            ()=>
            {
                Invoke(() =>
                {
                    label1.Text = "异步发力了";

                });
                return "1";
            }
                );
            return 1.5f;
        }
        void Func(Task<string> str)
        {

        }
    }
}
