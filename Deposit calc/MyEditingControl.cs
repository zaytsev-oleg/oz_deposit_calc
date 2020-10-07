using System;
using System.Windows.Forms;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    class MyEditingControl : DataGridViewTextBoxEditingControl
    {
        protected override void WndProc(ref Message m)
        {
            string separator = CultureInfo.CurrentCulture.DateTimeFormat.DateSeparator;
            Regex rgx = new Regex(@"^[0-9]{1,2}\" + separator + @"[0-9]{1,2}\" + separator + "[0-9]{4}$");

            if (m.Msg == 0x302 && Clipboard.ContainsText())
            {
                int column = this.EditingControlDataGridView.CurrentCell.ColumnIndex;
                string s = Clipboard.GetText();

                try
                {
                    if (column == 0)
                    {
                        if (!rgx.IsMatch(s))
                            throw new Exception();

                        DateTime date = DateTime.Parse(s);
                        this.Text = date.ToShortDateString();
                    }
                    else if (column == 1)
                    {
                        double adj = double.Parse(Clipboard.GetText(), NumberStyles.Float);
                        if (adj > 0)
                            this.Text = adj.ToString();
                    }
                }
                catch (Exception) { return; }

                return;
            }

            base.WndProc(ref m);
        }
    }
}