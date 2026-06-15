using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Calculator
{
    public partial class Form1 : Form
    {
        private const string ErrorText = "Ошибка";

        private TextBox _displayTextBox;
        private Label _statusLabel;

        private decimal _storedValue;
        private string _pendingOperation = "";
        private bool _isNewInput = true;
        private bool _isDarkTheme;

        private readonly string _saveFolderPath;
        private readonly string _saveFilePath;

        public Form1()
        {
            InitializeComponent();

            _saveFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Calculator");

            _saveFilePath = Path.Combine(_saveFolderPath, "saved_result.txt");

            CreateCalculator();
        }

        private void CreateCalculator()
        {
            ConfigureForm();
            CreateDisplay();
            CreateButtons();
            ApplyTheme();
        }

        private void ConfigureForm()
        {
            Text = "Калькулятор";
            Width = 330;
            Height = 500;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
        }

        private void CreateDisplay()
        {
            _displayTextBox = new TextBox();
            _displayTextBox.Left = 20;
            _displayTextBox.Top = 20;
            _displayTextBox.Width = 270;
            _displayTextBox.Height = 40;
            _displayTextBox.ReadOnly = true;
            _displayTextBox.Text = "0";
            _displayTextBox.TextAlign = HorizontalAlignment.Right;
            _displayTextBox.Font = new Font("Segoe UI", 18);
            Controls.Add(_displayTextBox);

            _statusLabel = new Label();
            _statusLabel.Left = 20;
            _statusLabel.Top = 65;
            _statusLabel.Width = 270;
            _statusLabel.Height = 20;
            _statusLabel.Text = "";
            _statusLabel.Font = new Font("Segoe UI", 9);
            Controls.Add(_statusLabel);
        }

        private void CreateButtons()
        {
            string decimalSeparator = GetDecimalSeparator();

            string[,] buttonTexts =
            {
                { "7", "8", "9", "/" },
                { "4", "5", "6", "*" },
                { "1", "2", "3", "-" },
                { "0", decimalSeparator, "=", "+" },
                { "C", "Сохр", "Загр", "Тема" }
            };

            int startX = 20;
            int startY = 95;
            int buttonWidth = 60;
            int buttonHeight = 50;
            int gap = 10;

            for (int row = 0; row < buttonTexts.GetLength(0); row++)
            {
                for (int col = 0; col < buttonTexts.GetLength(1); col++)
                {
                    Button button = CreateButton(
                        buttonTexts[row, col],
                        startX + col * (buttonWidth + gap),
                        startY + row * (buttonHeight + gap),
                        buttonWidth,
                        buttonHeight);

                    AddClickHandler(button, decimalSeparator);
                    Controls.Add(button);
                }
            }
        }

        private Button CreateButton(string text, int left, int top, int width, int height)
        {
            Button button = new Button();
            button.Text = text;
            button.Left = left;
            button.Top = top;
            button.Width = width;
            button.Height = height;
            button.Font = new Font("Segoe UI", 11);
            button.UseVisualStyleBackColor = false;

            return button;
        }

        private void AddClickHandler(Button button, string decimalSeparator)
        {
            if (IsDigitButton(button.Text) || button.Text == decimalSeparator)
            {
                button.Click += DigitButton_Click;
            }
            else if (IsOperationButton(button.Text))
            {
                button.Click += OperationButton_Click;
            }
            else if (button.Text == "=")
            {
                button.Click += EqualsButton_Click;
            }
            else if (button.Text == "C")
            {
                button.Click += ClearButton_Click;
            }
            else if (button.Text == "Сохр")
            {
                button.Click += SaveButton_Click;
            }
            else if (button.Text == "Загр")
            {
                button.Click += LoadButton_Click;
            }
            else if (button.Text == "Тема")
            {
                button.Click += ThemeButton_Click;
            }
        }

        private void DigitButton_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
            {
                return;
            }

            AddInputSymbol(button.Text);
        }

        private void OperationButton_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;

            if (button == null)
            {
                return;
            }

            SetOperation(button.Text);
        }

        private void EqualsButton_Click(object sender, EventArgs e)
        {
            if (_pendingOperation == "")
            {
                return;
            }

            if (!TryGetDisplayValue(out decimal secondNumber))
            {
                ShowError("Некорректное число");
                return;
            }

            if (Calculate(secondNumber))
            {
                _pendingOperation = "";
                _isNewInput = true;
                _statusLabel.Text = "";
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            ClearCalculator();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveResult();
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            LoadResult();
        }

        private void ThemeButton_Click(object sender, EventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme();
        }

        private void AddInputSymbol(string symbol)
        {
            string decimalSeparator = GetDecimalSeparator();

            if (_displayTextBox.Text == ErrorText)
            {
                ClearCalculator();
            }

            if (symbol == decimalSeparator)
            {
                AddDecimalSeparator(decimalSeparator);
                return;
            }

            if (_isNewInput || _displayTextBox.Text == "0")
            {
                _displayTextBox.Text = symbol;
                _isNewInput = false;
            }
            else
            {
                _displayTextBox.Text += symbol;
            }

            _statusLabel.Text = "";
        }

        private void AddDecimalSeparator(string decimalSeparator)
        {
            if (_isNewInput)
            {
                _displayTextBox.Text = "0" + decimalSeparator;
                _isNewInput = false;
                _statusLabel.Text = "";
                return;
            }

            if (!_displayTextBox.Text.Contains(decimalSeparator))
            {
                _displayTextBox.Text += decimalSeparator;
                _statusLabel.Text = "";
            }
        }

        private void SetOperation(string operation)
        {
            if (!TryGetDisplayValue(out decimal currentValue))
            {
                ShowError("Некорректное число");
                return;
            }

            if (_pendingOperation != "" && !_isNewInput)
            {
                if (!Calculate(currentValue))
                {
                    return;
                }
            }
            else
            {
                _storedValue = currentValue;
            }

            _pendingOperation = operation;
            _isNewInput = true;
            _statusLabel.Text = FormatNumber(_storedValue) + " " + _pendingOperation;
        }

        private bool Calculate(decimal secondNumber)
        {
            decimal result;

            switch (_pendingOperation)
            {
                case "+":
                    result = _storedValue + secondNumber;
                    break;

                case "-":
                    result = _storedValue - secondNumber;
                    break;

                case "*":
                    result = _storedValue * secondNumber;
                    break;

                case "/":
                    if (secondNumber == 0)
                    {
                        ShowError("Деление на ноль невозможно");
                        return false;
                    }

                    result = _storedValue / secondNumber;
                    break;

                default:
                    return false;
            }

            _storedValue = result;
            _displayTextBox.Text = FormatNumber(result);

            return true;
        }

        private void ClearCalculator()
        {
            _storedValue = 0;
            _pendingOperation = "";
            _isNewInput = true;

            _displayTextBox.Text = "0";
            _statusLabel.Text = "";
        }

        private void SaveResult()
        {
            if (!TryGetDisplayValue(out decimal value))
            {
                _statusLabel.Text = "Сохранять нечего";
                return;
            }

            try
            {
                Directory.CreateDirectory(_saveFolderPath);

                string textToSave = value.ToString(CultureInfo.InvariantCulture);
                File.WriteAllText(_saveFilePath, textToSave);

                _statusLabel.Text = "Результат сохранён";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                _statusLabel.Text = "Не удалось сохранить результат";
            }
        }

        private void LoadResult()
        {
            try
            {
                if (!File.Exists(_saveFilePath))
                {
                    _statusLabel.Text = "Сохранённый результат не найден";
                    return;
                }

                string savedText = File.ReadAllText(_saveFilePath).Trim();

                if (!TryParseSavedValue(savedText, out decimal savedValue))
                {
                    _statusLabel.Text = "Сохранённый файл содержит некорректные данные";
                    return;
                }

                _storedValue = savedValue;
                _pendingOperation = "";
                _isNewInput = true;

                _displayTextBox.Text = FormatNumber(savedValue);
                _statusLabel.Text = "Результат загружен";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                _statusLabel.Text = "Не удалось загрузить результат";
            }
        }

        private bool TryParseSavedValue(string text, out decimal value)
        {
            if (decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value))
            {
                return true;
            }

            return decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out value);
        }

        private bool TryGetDisplayValue(out decimal value)
        {
            return decimal.TryParse(
                _displayTextBox.Text,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out value);
        }

        private void ShowError(string message)
        {
            _displayTextBox.Text = ErrorText;
            _statusLabel.Text = message;

            _storedValue = 0;
            _pendingOperation = "";
            _isNewInput = true;
        }

        private void ApplyTheme()
        {
            Color formBackColor;
            Color textColor;
            Color buttonBackColor;
            Color displayBackColor;

            if (_isDarkTheme)
            {
                formBackColor = Color.FromArgb(40, 40, 40);
                textColor = Color.White;
                buttonBackColor = Color.FromArgb(65, 65, 65);
                displayBackColor = Color.FromArgb(25, 25, 25);
            }
            else
            {
                formBackColor = SystemColors.Control;
                textColor = Color.Black;
                buttonBackColor = SystemColors.Control;
                displayBackColor = Color.White;
            }

            BackColor = formBackColor;
            ForeColor = textColor;

            foreach (Control control in Controls)
            {
                ApplyThemeToControl(control, textColor, buttonBackColor, displayBackColor);
            }
        }

        private void ApplyThemeToControl(
            Control control,
            Color textColor,
            Color buttonBackColor,
            Color displayBackColor)
        {
            control.ForeColor = textColor;

            if (control is Button)
            {
                control.BackColor = buttonBackColor;
            }
            else if (control is TextBox)
            {
                control.BackColor = displayBackColor;
            }
            else if (control is Label)
            {
                control.BackColor = BackColor;
            }
        }

        private string FormatNumber(decimal number)
        {
            if (number == decimal.Truncate(number))
            {
                return number.ToString("0", CultureInfo.CurrentCulture);
            }

            return number.ToString("0.##########", CultureInfo.CurrentCulture);
        }

        private string GetDecimalSeparator()
        {
            return CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        }

        private bool IsDigitButton(string text)
        {
            return text.Length == 1 && char.IsDigit(text[0]);
        }

        private bool IsOperationButton(string text)
        {
            return text == "+" || text == "-" || text == "*" || text == "/";
        }
    }
}