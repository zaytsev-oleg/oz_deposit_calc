using System;
using System.Windows.Forms;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class MyDataGridViewColumn : DataGridViewColumn
    {
        public MyDataGridViewColumn() : base(new MyDataGridViewTextBoxCell())
        {

        }
    }
}