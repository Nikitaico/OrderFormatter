using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace OrderStructurer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private TextBox txtArea;
        private Button btnProcess;
        private Button btnClear;
        private Button btnCopyAll;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu; // Заменено на современный ContextMenuStrip

        public MainForm()
        {
            // --- НАСТРОЙКА ОКНА ---
            this.Text = "Структуризатор заказов";
            this.Size = new Size(570, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // --- ВЕРХНЯЯ ПАНЕЛЬ ДЛЯ КНОПОК ---
            Panel controlPanel = new Panel();
            controlPanel.Dock = DockStyle.Top;
            controlPanel.Height = 50;
            this.Controls.Add(controlPanel);

            // Кнопка обработки (Слева)
            btnProcess = new Button();
            btnProcess.Text = "Обработать";
            btnProcess.Location = new Point(10, 10);
            btnProcess.Size = new Size(170, 30);
            btnProcess.BackColor = Color.FromArgb(76, 175, 80); // #4CAF50
            btnProcess.ForeColor = Color.White;
            btnProcess.Font = new Font("Arial", 10, FontStyle.Bold);
            btnProcess.FlatStyle = FlatStyle.Flat;
            btnProcess.Click += BtnProcess_Click;
            controlPanel.Controls.Add(btnProcess);

            // Кнопка очистки (По центру)
            btnClear = new Button();
            btnClear.Text = "Очистить";
            btnClear.Location = new Point(190, 10);
            btnClear.Size = new Size(170, 30);
            btnClear.BackColor = Color.FromArgb(117, 117, 117); // #757575
            btnClear.ForeColor = Color.White;
            btnClear.Font = new Font("Arial", 10, FontStyle.Bold);
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.Click += BtnClear_Click;
            controlPanel.Controls.Add(btnClear);

            // Кнопка копирования всего текста (Справа)
            btnCopyAll = new Button();
            btnCopyAll.Text = "Скопировать всё";
            btnCopyAll.Location = new Point(370, 10);
            btnCopyAll.Size = new Size(170, 30);
            btnCopyAll.BackColor = Color.FromArgb(33, 150, 243); // #2196F3
            btnCopyAll.ForeColor = Color.White;
            btnCopyAll.Font = new Font("Arial", 10, FontStyle.Bold);
            btnCopyAll.FlatStyle = FlatStyle.Flat;
            btnCopyAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopyAll.Click += BtnCopyAll_Click;
            controlPanel.Controls.Add(btnCopyAll);

            // --- ТЕКСТОВОЕ ПОЛЕ (Снизу) ---
            Label lbl = new Label();
            lbl.Text = "Вставьте текст с номерами заказов:";
            lbl.Location = new Point(10, 55);
            lbl.Size = new Size(300, 20);
            lbl.Font = new Font("Arial", 10);
            this.Controls.Add(lbl);

            txtArea = new TextBox();
            txtArea.Multiline = true;
            txtArea.ScrollBars = ScrollBars.Vertical;
            txtArea.Font = new Font("Arial", 10);
            txtArea.Location = new Point(10, 80);
            txtArea.Size = new Size(535, 350);
            txtArea.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(txtArea);

            // --- ПОСТОЯННЫЙ ТРЕЙ ---
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Показать окно", null, ShowWindow);
            trayMenu.Items.Add("Выход", null, ExitApp);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Структуризатор заказов";
            
            // Генерируем заглушку-иконку
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(73, 109, 137));
                g.FillRectangle(Brushes.White, 4, 4, 8, 8);
            }
            trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            
            // Восстановление окна по двойному клику на трей
            trayIcon.DoubleClick += ShowWindow;

            // Перехватываем закрытие окна (крестик) -> скрываем в трей
            this.FormClosing += MainForm_FormClosing;
        }

        // --- ЛОГИКА ОБРАБОТКИ ТЕКСТА ---
        private void BtnProcess_Click(object sender, EventArgs e)
        {
            string inputText = txtArea.Text.Trim();
            if (string.IsNullOrEmpty(inputText))
            {
                MessageBox.Show("Поле ввода пусто", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MatchCollection matches = Regex.Matches(inputText, @"\d+");
            List<string> validOrders = new List<string>();
            List<string> errors = new List<string>();

            foreach (Match match in matches)
            {
                string num = match.Value;
                if (num.Length == 6)
                {
                    validOrders.Add($"ПР{num}_VLH");
                }
                else
                {
                    errors.Add($"{num} (цифр: {num.Length})");
                }
            }

            List<string> lines = new List<string>();
            List<string> currentLineOrders = new List<string>();

            foreach (string order in validOrders)
            {
                int potentialLen = currentLineOrders.Count == 0 
                    ? order.Length 
                    : string.Join(",", currentLineOrders).Length + 1 + order.Length;

                if (potentialLen <= 255)
                {
                    currentLineOrders.Add(order);
                }
                else
                {
                    if (currentLineOrders.Count > 0)
                    {
                        lines.Add(string.Join(",", currentLineOrders));
                    }
                    currentLineOrders = new List<string> { order };
                }
            }

            if (currentLineOrders.Count > 0)
            {
                lines.Add(string.Join(",", currentLineOrders));
            }

            string ordersOutput = string.Join(Environment.NewLine, lines);
            string resultText = ordersOutput;

            if (errors.Count > 0)
            {
                if (!string.IsNullOrEmpty(resultText))
                {
                    resultText += Environment.NewLine + Environment.NewLine;
                }
                resultText += "Ошибки (не 6 цифр):" + Environment.NewLine + string.Join(Environment.NewLine, errors);
            }

            txtArea.Text = resultText;

            if (!string.IsNullOrEmpty(ordersOutput))
            {
                Clipboard.SetText(ordersOutput);
            }
        }

        // --- ОЧИСТКА ПОЛЯ ---
        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtArea.Clear();
            txtArea.Focus();
        }

        // --- КОПИРОВАНИЕ ВСЕГО ПОЛЯ ---
        private void BtnCopyAll_Click(object sender, EventArgs e)
        {
            string allText = txtArea.Text.Trim();
            if (!string.IsNullOrEmpty(allText))
            {
                Clipboard.SetText(allText);
            }
            else
            {
                MessageBox.Show("Текстовое поле пустое, нечего копировать.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // --- ПОВЕДЕНИЕ ТРЕЯ ---
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void ShowWindow(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void ExitApp(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Exit();
        }
    }
}