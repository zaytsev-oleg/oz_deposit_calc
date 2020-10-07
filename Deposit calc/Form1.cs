using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        private double amount { get; set; } // Автоматически реализуемые свойства
        private double interest { get; set; }
        private int term { get; set; }
        private bool method { get; set; }

        private string[] rData;

        private bool addMoneyFlag { get; set; }
        private DateTime[] adjustmentDate;
        private double[] adjustmentAmount;

        private bool calcStatus { get; set; }
        private CultureInfo culture { get; set; }
        private RegionInfo region { get; set; }

        public Form1()
        {
            addMoneyFlag = true;

            culture = CultureInfo.CurrentCulture;
            region = RegionInfo.CurrentRegion;

            InitializeComponent();

            string s = culture.DateTimeFormat.ShortDatePattern.ToLowerInvariant();

            if (culture.Name == "ru-RU")
                s = s.Replace("d", "д").Replace("m", "м").Replace("y", "г");

            this.Column5.HeaderText = "Дата (" + s + ")";
            this.Column6.HeaderText = "Сумма (руб) (пример: " + (100.5).ToString(culture) + ")";

            this.ClientSize = this.MinimumSize;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Тестовые начальные значения
            dataGridView2.Rows.Add(4);
            dataGridView2[0, 0].Value = "15.01.2015";
            dataGridView2[1, 0].Value = "800";
            dataGridView2[0, 1].Value = "15.01.2015";
            dataGridView2[1, 1].Value = "200";
            dataGridView2[0, 2].Value = "15.01.2015";
            dataGridView2[1, 2].Value = "500";
            dataGridView2[0, 3].Value = "15.02.2015";
            dataGridView2[1, 3].Value = "500";

            textBox1.Text = "1000";
            textBox2.Text = "12";
            textBox3.Text = "12";
        }

        public void MyDefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            string s = culture.DateTimeFormat.DateSeparator;
            dataGridView2[0, e.Row.Index].Value = "00" + s + "00" + s + "0000";
        }

        public void MyTextInputHandle(object sender, KeyPressEventArgs e, string separator, int n = 1)
        {
            TextBox textBox = sender as TextBox;

            if (n > 1 && textBox.TextLength == (2 * separator.Length + 8) && (!char.IsControl(e.KeyChar))
                && (textBox.SelectedText.Length == 0))
                e.Handled = true;

            if (separator.Contains(e.KeyChar) && textBox.Name != textBox3.Name)
            {
                Func<char, bool> method = (f) => {
                    if (f == e.KeyChar)
                        return true;
                    else
                        return false;
                };

                int i = separator.Count(method);
                int j = textBox.Text.Count(method);
                int k = textBox.SelectedText.Count(method);

                if (n*i > (j - k))
                    return;
                else
                    e.Handled = true;
            }
            else if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar))
                return;
            else
                e.Handled = true;
        }

        public void MyEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            MyEditingControl control = e.Control as MyEditingControl;
            DataGridView dataGrid = (sender as DataGridView);

            string dateSeparator = culture.DateTimeFormat.DateSeparator;
            string decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;

            if (!control.IsHandleCreated)
                control.KeyPress += (@object, myEventArgs) => {

                    if (dataGrid.CurrentCell.ColumnIndex == 0)
                    {
                        MyTextInputHandle(@object, myEventArgs, dateSeparator, 2);
                    }
                    else if (dataGrid.CurrentCell.ColumnIndex == 1)
                    {
                        MyTextInputHandle(@object, myEventArgs, decimalSeparator);
                    }

                };

            if (control.TextLength == 0 && dataGrid.CurrentCell.ColumnIndex == 0)
                control.Text = "00" + dateSeparator + "00" + dateSeparator + "0000";
        }

        public void MyTextBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            string decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
            MyTextInputHandle(sender, e, decimalSeparator);
        }

        public void FillFinalResults(int days, double finalAmount, double receivedInterest, double adjSum = 0, double effectiveRate = 0)
        {

            rData = new string[5];
            rData[0] = textBox1.Text;
            rData[1] = textBox2.Text;
            rData[2] = dateTimePicker1.Value.Date.ToShortDateString();
            rData[3] = textBox3.Text;

            if (method)
                rData[4] = "Да";
            else 
                rData[4] = "Нет";

            textBox7.Text = days.ToString(); // Количество дней
            textBox4.Text = finalAmount.ToString("N"); // Сумма к выплате
            textBox5.Text = receivedInterest.ToString("N"); // Получено процентов

            if (adjSum == 0) // Сумма пополнений
                textBox8.Text = "-";
            else
                textBox8.Text = adjSum.ToString("N");

            if (effectiveRate == 0) // Эффективная ставка
                textBox6.Text = "-";
            else
                textBox6.Text = effectiveRate.ToString();

            if (!button3.Enabled) // Включаем кнопку Сохранить расчёт
                button3.Enabled = true;
        }

        // Перегрузка метода Calculate()
        public void Calculate(double amount, double interest, int term, bool method, DateTime[] adjustmentDate, double[] adjustmentAmount)
        {
            try
            {
                double r = interest / (100 * 365);
                double[] interest_array = new double[term];

                DateTime date = dateTimePicker1.Value.Date, dateL = date, dateR;
                double span;

                int k = 0, buffer = 0;

                dataGridView1.Rows.Clear();
                dataGridView1.Rows.Add(term + adjustmentDate.GetLength(0));

                for (int i = 0; i < term; i++)
                {
                    dateR = date.AddMonths(i + 1);

                    // Определяем количество пополнений в i-ом месяца
                    for (int j = k; j < adjustmentDate.Length; j++)
                    {
                        if (adjustmentDate[j] >= dateL && adjustmentDate[j] < dateR)
                        {
                            dataGridView1[1, i + k].Value = adjustmentDate[j].ToShortDateString();
                            // dataGridView1[2, i + k].Value = "пополнение вклада";
                            dataGridView1[3, i + k].Value = "+" + adjustmentAmount[j].ToString("N");
                            k++;
                        }
                        else
                            break;
                    }

                    if (k > buffer)
                    {
                        double temp = 0;

                        for (int j = buffer; j < k; j++)
                        {
                            temp = adjustmentAmount[j];

                            while ((j + 1 < k) && (adjustmentDate[j] == adjustmentDate[j + 1]))
                            {
                                temp += adjustmentAmount[j + 1];
                                j++;
                            }

                            span = (adjustmentDate[j] - dateL).Days;

                            if (method)
                            {
                                interest_array[i] += span * r * amount;
                            }
                            else
                            {
                                interest_array[i] += span * r * this.amount;
                                this.amount += temp;
                            }

                            amount += temp;

                            dateL = adjustmentDate[j];
                        }
                    }

                    span = (dateR - dateL).Days;

                    if (method)
                        interest_array[i] += span * r * amount;
                    else
                        interest_array[i] += span * r * this.amount;

                    amount += interest_array[i];

                    dataGridView1[0, i + k].Value = i + 1;
                    dataGridView1[1, i + k].Value = dateR.ToShortDateString();
                    dataGridView1[2, i + k].Value = (Math.Round(interest_array[i], 2)).ToString("N");
                    dataGridView1[3, i + k].Value = (Math.Round(amount, 2)).ToString("N");

                    dateL = dateR;
                    buffer = k;
                }

                int days = (dateL - date).Days;
                double finalAmount = Math.Round(amount, 2);
                double receivedInterest = Math.Round(interest_array.Sum(), 2);
                double adjSum = Math.Round(adjustmentAmount.Sum(), 2);

                FillFinalResults(days, finalAmount, receivedInterest, adjSum);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\nПожалуйста, проверьте корректность данных.", "Ошибка");
            }
        }

        public void Calculate(double amount, double interest, int term, bool method)
        {
            try
            {
                double r = interest / (100*365);
                double[] interest_array = new double[term];

                DateTime date = dateTimePicker1.Value.Date, dateL = date, dateR;
                double span;

                dataGridView1.Rows.Clear();
                dataGridView1.Rows.Add(term);

                for (int i = 0; i < term; i++)
                {
                    dataGridView1[0, i].Value = i+1;

                    dateR = date.AddMonths(i+1);
                    dataGridView1[1, i].Value = dateR.ToShortDateString();

                    span = (dateR - dateL).Days;

                    if (method)
                        interest_array[i] = span * r * amount;
                    else
                        interest_array[i] = span * r * this.amount;

                    amount += interest_array[i];

                    dataGridView1[2, i].Value = (Math.Round(interest_array[i], 2)).ToString("N");
                    dataGridView1[3, i].Value = (Math.Round(amount, 2)).ToString("N");

                    dateL = dateR;
                }

                int days = (dateL - date).Days;
                double finalAmount = Math.Round(amount, 2);
                double receivedInterest = Math.Round(interest_array.Sum(), 2);
                double effectiveRate = Math.Round((interest_array.Sum() * 100d * 365d) / (days * this.amount), 2);

                FillFinalResults(days, finalAmount, receivedInterest, effectiveRate: effectiveRate);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\nПожалуйста, проверьте корректность данных.", "Ошибка");
            }
        }

        public bool ValidateTextbox() // метод-предикат
        {
            StringBuilder errorMessage = new StringBuilder();
            int i = 0, j = 0;
            bool outcome = true;
            double value;
            string[] label = new string[] { label1.Text, label2.Text, label4.Text };

            textBox1.Text = textBox1.Text.Trim();
            textBox2.Text = textBox2.Text.Trim();
            textBox3.Text = textBox3.Text.Trim();
            
            foreach (string field in (new string[] { textBox1.Text, textBox2.Text, textBox3.Text }))
                {
                    try
                    {
                        if (field.Length == 0)
                            throw new Exception("отсутствует значение.\n");

                        if (j == 2)
                            value = int.Parse(field, NumberStyles.Integer);
                        else
                            value = double.Parse(field, NumberStyles.Float);

                        if (value <= 0)
                            throw new Exception("значение должно быть строго больше нуля.\n");

                        if (j == 2)
                        {
                            try
                            {
                                dateTimePicker1.Value.Date.AddMonths((int)value);
                            }
                            catch (Exception)
                            {
                                throw new Exception("превышено максимальное значение типа System.DateTime, " + DateTime.MaxValue.ToShortDateString() + ".\n" +
                                                    "Срок вклада не может превышать " + 
                                                    ((DateTime.MaxValue.Year - dateTimePicker1.Value.Date.Year) * 12 +
                                                    DateTime.MaxValue.Month - dateTimePicker1.Value.Date.Month).ToString() + " мес. " + "от указанной даты оформления, " + dateTimePicker1.Value.Date.ToShortDateString() + ".\n");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        errorMessage.Append((++i).ToString() + ". " + label[j] + ": ");

                        switch (e.GetType().ToString())
                        {
                            case "System.FormatException":
                                errorMessage.AppendLine("неверный формат числа.\n");
                                break;

                            case "System.OverflowException":
                                {
                                    if (j < 2)
                                    {
                                        errorMessage.AppendLine("значение не может быть обработано вещественным типом System.Double.");
                                        errorMessage.AppendLine("Значение типа должно быть строго больше нуля, в промежутке (0; " + double.MaxValue.ToString() + "].\n");
                                    }
                                    else
                                    {
                                        errorMessage.AppendLine("значение не может быть обработано целочисленным типом System.Int32.");
                                        errorMessage.AppendLine("Значение типа должно быть строго больше нуля, в промежутке (0; " + int.MaxValue.ToString() + "].\n");
                                    }
                                    break;
                                }

                            default:
                                errorMessage.AppendLine(e.Message);
                                break;
                        }

                        outcome = false;
                    }

                    j++;
                }

            if (outcome)
            {
                amount = double.Parse(textBox1.Text, NumberStyles.Float);
                interest = double.Parse(textBox2.Text, NumberStyles.Float);
                term = int.Parse(textBox3.Text, NumberStyles.Integer);

                //MessageBox.Show("Поздравляем, проверка прошла успешно.", "Информация");
            }
            else
            {
                errorMessage.AppendLine("Пример заполнения с учётом региональных настроек ОС, " + culture.Name + ".\n");
                errorMessage.AppendLine("Сумма вклада (руб) = " + (1000.5).ToString(culture));
                errorMessage.AppendLine("Годовая проц. ставка (%) = " + (12.3).ToString(culture));
                errorMessage.AppendLine("Дата оформления вклада = " + dateTimePicker1.Value.Date.ToShortDateString());
                errorMessage.AppendLine("Срок вклада = 12");

                MessageBox.Show(errorMessage.ToString(), "Ошибка");
            }

            return outcome;
        }

        public bool ValidateAdjustments(out DateTime[] adjustmentDate, out double[] adjustmentAmount) // метод-предикат
        {
            string s;
            int n = dataGridView2.Rows.Count;
            string separator = culture.DateTimeFormat.DateSeparator;

            adjustmentDate = new DateTime[n - 1]; // -1 из-за пустой строки ввода (*)
            adjustmentAmount = new double[n - 1];

            Regex rgx = new Regex(@"^[0-9]{1,2}\" + separator + @"[0-9]{1,2}\" + separator + "[0-9]{4}$");
            DateTime date;
            double adj;

            Func<DataGridViewTextBoxCell, bool> markRed = (cell) => { // Используем лямбда-оператор () => {} для делегата markRed
                if (cell.Selected)
                cell.Selected = false;
                
                cell.Style = new DataGridViewCellStyle() { BackColor = Color.Red };
                return false;
            };
            
            bool outcome = true;

            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (row.IsNewRow)
                    break;

                foreach (DataGridViewTextBoxCell cell in row.Cells)
                {
                    cell.Style = new DataGridViewCellStyle() { BackColor = Color.White };

                    if (cell.Value == null || cell.Value.ToString().Trim().Length == 0) // Проверяем, заполнено ли поле
                    {
                        outcome = markRed(cell);
                        continue; // Пропустить итерацию цикла foreach, если поле не заполнено
                    }
                    else
                        s = cell.Value.ToString();
                                                    
                    if (cell.ColumnIndex == 0) // Колонка "Дата (дд.мм.гггг)"
                    {
                        // Проверка значение на соответствие формату дд.мм.гггг
                        // Проверка корректности даты и её соответствия сроку вклада

                        if (!rgx.IsMatch(s) || !DateTime.TryParse(s, out date) ||
                            !(date >= dateTimePicker1.Value.Date && date < dateTimePicker1.Value.Date.AddMonths(term)))
                            outcome = markRed(cell);
                        else
                            adjustmentDate[row.Index] = date;
                    }
                    else // Колонка "Сумма (руб)"
                    {
                        if (!double.TryParse(s, NumberStyles.Float, culture, out adj) || adj <= 0)
                            outcome = markRed(cell);
                        else
                            adjustmentAmount[row.Index] = adj;
                    }
                }
            }

            if (outcome)
            {
                // Сортируем массивы adjustmentDate и adjustmentAmount. Сортировка выбором (Selection sort).
                for (int i = 0; i < adjustmentDate.Length - 1; i++)
                {
                    int k = i;

                    for (int j = i + 1; j < adjustmentDate.Length; j++)
                    {
                        if (adjustmentDate[k] > adjustmentDate[j])
                            k = j;
                    }

                    if (k != i)
                    {
                        DateTime temp_date = adjustmentDate[k];
                        adjustmentDate[k] = adjustmentDate[i];
                        adjustmentDate[i] = temp_date;

                        double temp_amount = adjustmentAmount[k];
                        adjustmentAmount[k] = adjustmentAmount[i];
                        adjustmentAmount[i] = temp_amount;
                    }
                }
            }
            else
                MessageBox.Show("Таблица Пополнение вклада заполнена неверно.\nПожалуйста, исправьте.", "Ошибка");

            return outcome;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // MessageBox.Show("К сожалению, эта кнопка пока недоступна.");
            // Form2 Form2 = new Form2();
            // Form2.Show();

            this.ClientSize = (addMoneyFlag) ? this.MaximumSize : this.MinimumSize; // тернарный оператор

            if (addMoneyFlag == false)
            {
                checkBox1.Checked = false;
                button2.Text = "Пополнение вклада >>>";
            }
            else
            {
                button2.Text = "Пополнение вклада <<<";
            }

            this.addMoneyFlag = !(this.addMoneyFlag);
        }

        private void button1_Click(object sender, EventArgs e)
        {

            method = radioButton1.Checked;

            if (calcStatus = ValidateTextbox())
            { 
                if (checkBox1.Checked && dataGridView2.RowCount > 1)
                {
                    if (calcStatus = ValidateAdjustments(out adjustmentDate, out adjustmentAmount))
                    {
                        Calculate(amount, interest, term, method, adjustmentDate, adjustmentAmount);
                    }
                }
                else
                {
                    Calculate(amount, interest, term, method);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!calcStatus &&
            MessageBox.Show("Текущий расчёт не был произведён из-за наличия ошибок в исходных данных.\n" +
                            "Сохранить последний успешный расчёт?", "Информация", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            string directory = Directory.GetCurrentDirectory();
            DirectoryInfo dir = new DirectoryInfo(directory);

            DirectoryInfo[] listDir = dir.GetDirectories("Reports");

            if (listDir.Length == 0)
                dir = dir.CreateSubdirectory("Reports");
            else
                dir = listDir[0];

            StringBuilder timeStamp = new StringBuilder();

            timeStamp.Append(DateTime.Now.Year);
            timeStamp.Append(DateTime.Now.Month);
            timeStamp.Append(DateTime.Now.Day + "_");
            timeStamp.Append(DateTime.Now.Hour);
            timeStamp.Append(DateTime.Now.Minute);
            timeStamp.Append(DateTime.Now.Second);
            timeStamp.Append(DateTime.Now.Millisecond);

            FileInfo file = new FileInfo(@"Reports\report_" + timeStamp.ToString() + ".txt");
            StreamWriter stream = file.CreateText();
            
            // Кодировка Юникод UTF-8
            stream.WriteLine("Расчёт создан программой Кредитный калькулятор.");
            stream.WriteLine(new string('-', 20));
            stream.Write(stream.NewLine);

            stream.WriteLine("Данные по вкладу");
            stream.WriteLine("{0, -30}\t{1}", label1.Text, rData[0]);
            stream.WriteLine("{0, -30}\t{1}", label2.Text, rData[1]);
            stream.WriteLine("{0, -30}\t{1}", label3.Text, rData[2]);
            stream.WriteLine("{0, -30}\t{1}", label4.Text, rData[3]);
            stream.WriteLine("{0, -30}\t{1}", groupBox2.Text, rData[4]);

            stream.WriteLine();
            stream.WriteLine("Расчётная информация");
            stream.WriteLine("{0, -30}\t{1}", "Количество дней", textBox7.Text);
            stream.WriteLine("{0, -30}\t{1}", "Сумма к выплате (руб)", textBox4.Text);
            stream.WriteLine("{0, -30}\t{1}", "Сумма пополнений (руб)", textBox8.Text);
            stream.WriteLine("{0, -30}\t{1}", "Получено процентов (руб)", textBox5.Text);
            stream.WriteLine("{0, -30}\t{1}", "Эффективная проц. ставка (%)", textBox6.Text);

            stream.WriteLine();
            stream.WriteLine("Детализация денежных потоков");
            stream.WriteLine("{0, -5}\t{1, -15}\t{2, -20}\t{3, -20}", "Месяц", "Дата начисления", "Начисленные проценты", "Общая сумма");

            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                string[] s = new string[4];

                for (int j = 0; j < dataGridView1.ColumnCount; j++)
                {
                    if (dataGridView1[j, i].Value == null)
                        s[j] = "";
                    else
                        s[j] = dataGridView1[j, i].Value.ToString();
                }
                stream.WriteLine("{0, -5}\t{1, -15}\t{2, -20}\t{3, -20}", s);
            }

            stream.Write(stream.NewLine);
            stream.WriteLine(new string('-', 20));
            stream.Write("Дата создания расчёта: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ".");

            stream.Close();
            MessageBox.Show("Последний успешный расчёт сохранён в файл:\n..\\Reports\\" + file.Name, "Информация");
        }
    }
}