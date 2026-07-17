Звонок прозвенел, все садимся, достаем двойные листочки... Шучу! Листочки нам сегодня не понадобятся, а вот светлая голова и открытый VS Code — очень даже.

Тема нашего сегодняшнего урока: **«Миграция десктопного приложения с Python (Tkinter) на C# (.NET Framework 4.8) в среде Visual Studio Code»**.

Мы разберем, как подготовить рабочее место, переписать код с сохранением всего функционала (включая работу в системном трее Windows), и собрать готовый файл `.exe`. Поехали!

---

## Часть 1. Подготовка верстака (Настройка VS Code)

Поскольку мы работаем в Windows 10/11 и хотим целиться в классический **.NET Framework 4.8**, нам понадобится сделать три простых шага.

### Шаг 1. Установка окружения в систему

1. Скачайте и установите **.NET Framework 4.8 Developer Pack** (именно пакет разработчика, а не просто рантайм). Он позволит компилировать программы под эту версию.
```
https://dotnet.microsoft.com/ru-ru/download/dotnet-framework/thank-you/net48-developer-pack-offline-installer
```
```
https://dotnet.microsoft.com/ru-ru/download/dotnet-framework/thank-you/net48-developer-pack-rus
```

NDP48-DevPack-ENU.exe

NDP48-DevPack-RUS.exe

2. Скачайте и установите **.NET Core SDK** (актуальной версии, например 8.0). Он нужен, чтобы в VS Code работал современный удобный инструмент командной строки `dotnet`.

```
https://dotnet.microsoft.com/ru-ru/download/dotnet/8.0
```

dotnet-sdk-8.0.423-win-x64.exe

### Шаг 2. Настройка расширений в VS Code

Открываем VS Code, переходим во вкладку **Extensions (Расширения)** `Ctrl+Shift+X` и устанавливаем:

* **C# Dev Kit** (официальный пакет от Microsoft) — он подтянет всё необходимое для подсветки кода, подсказок и сборки.

### Шаг 3. Создание проекта

Открываем терминал в VS Code (`Ctrl+``) и пишем команды:

```bash
# Создаем шаблон приложения Windows Forms
dotnet new winforms

# Переходим в папку проекта
cd OrderFormatter

```

Теперь самое главное! Откройте файл **`OrderFormatter.csproj`** в VS Code. По умолчанию современный SDK создаст проект под .NET 6/8. Перепишем его, чтобы он компилировал под **.NET 4.8**.

Замените содержимое файла `.csproj` на этот чистый код:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>

```

---

## Часть 2. Перенос логики: с Python на C#

А теперь — магия программирования. Давайте сравним наши инструменты:

* Вместо `tkinter.Tk` у нас будет класс `Form`.


* Вместо `tk.Text` — элемент `TextBox` с включенным режимом `Multiline`.


* Вместо библиотеки `pystray` — встроенный компонент `NotifyIcon`.


* **Важная деталь:** В Python нам приходилось вручную прописывать костыли для горячих клавиш (Ctrl+C, Ctrl+V) для русской раскладки. В C# Windows Forms текстовое поле *нативно* и идеально обрабатывает эти комбинации из коробки. Код станет чище!



Откройте файл **`Program.cs`**, удалите оттуда всё и вставьте наш новый, переработанный урок-код:

```csharp
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
        private Button btnCopyAll;
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;

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
            btnProcess.Text = "Обработать и копировать";
            btnProcess.Location = new Point(10, 10);
            btnProcess.Size = new Size(190, 30);
            btnProcess.BackColor = Color.FromArgb(76, 175, 80); // #4CAF50
            btnProcess.ForeColor = Color.White;
            btnProcess.Font = new Font("Arial", 10, FontStyle.Bold);
            btnProcess.FlatStyle = FlatStyle.Flat;
            btnProcess.Click += BtnProcess_Click;
            controlPanel.Controls.Add(btnProcess);

            // Кнопка копирования всего текста (Справа)
            btnCopyAll = new Button();
            btnCopyAll.Text = "Скопировать всё";
            btnCopyAll.Location = new Point(355, 10);
            btnCopyAll.Size = new Size(190, 30);
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
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Показать окно", ShowWindow);
            trayMenu.MenuItems.Add("Выход", ExitApp);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Структуризатор заказов";
            
            // Генерируем заглушку-иконку (как в Python-скрипте)
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(73, 109, 137));
                g.FillRectangle(Brushes.White, 4, 4, 8, 8);
            }
            trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
            trayIcon.ContextMenu = trayMenu;
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

            // Поиск последовательностей цифр
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

            // Разделение на строки до 255 символов
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

            // Копируем результат в буфер обмена
            if (!string.IsNullOrEmpty(ordersOutput))
            {
                Clipboard.SetText(ordersOutput);
            }
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
            // Если пользователь нажал на крестик — отменяем закрытие и прячем окно
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
            // Полный выход из приложения
            trayIcon.Visible = false;
            trayIcon.Dispose();
            Application.Exit();
        }
    }
}

```

---

## Часть 3. Сборка заветного .exe файла

Код написан, остался финальный аккорд — компиляция.

1. Снова переходим в терминал VS Code.
2. Вводим команду сборки релиз-версии:
```bash
dotnet build -c Release

```


3. После успешной сборки (вы увидите зеленую надпись "Build succeeded") перейдите по пути внутри папки вашего проекта:
`👉 папка_проекта\bin\Release\net48\`

Там вы найдете готовый файл **`OrderStructurer.exe`**.

Этот файл полностью автономен, весит совсем немного, запускается мгновенно на любой Windows 10/11 без необходимости устанавливать Python, библиотеки pystray или Tkinter! При закрытии на крестик он тихо уходит в трей, а при двойном клике по иконке или через контекстное меню — возвращается на экран.

Урок окончен! Домашнее задание: запустить, проверить работу и радоваться скорости работы на C#. Все свободны!