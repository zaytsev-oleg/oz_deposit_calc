using System;
using System.Windows.Forms;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class MyDataGridViewTextBoxCell : DataGridViewTextBoxCell
    {
        public override Type EditType
        {
            get
            {
                return typeof(MyEditingControl);
            }
        }
    }
}