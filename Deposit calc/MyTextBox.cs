using System;
using System.Windows.Forms;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class MyTextBox : TextBox
    {
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x302 && Clipboard.ContainsText())
            {
                try
                {
                    if (this.Name == "textBox3")
                    {
                        int i = int.Parse(Clipboard.GetText(), NumberStyles.Integer);
                        if (i > 0)
                            this.Text = i.ToString();
                    }
                    else
                    {
                        double i = double.Parse(Clipboard.GetText(), NumberStyles.Float);
                        if (i > 0)
                            this.Text = i.ToString();
                    }
                }
                catch (Exception) { return; }

                return;
            }

            base.WndProc(ref m);
        }
    }
}