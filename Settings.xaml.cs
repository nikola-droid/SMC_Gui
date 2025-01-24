using EngLibrary;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SMC_GUI
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Config Config;
        public event Action<Config> ConfigUpdated;
        public Settings()
        {
            InitializeComponent();
            DisplaySystemParameters();

            try
            {
                string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json");

                // Проверка наличия файла
                if (!File.Exists(jsonFilePath))
                {
                    throw new FileNotFoundException($"Файл конфигурации не найден: {jsonFilePath}");
                }

                // Чтение конфигурации
                Config = ReadConfig(filepath: jsonFilePath);
            }
            catch (Exception ex)
            {
                // Логируйте или обрабатывайте ошибку
                Console.WriteLine($"Ошибка при чтении конфигурации: {ex.Message}");
            }

            TextBox.Text = Config.OperationMode.ToString();
            TextBox_Time.Text = Config.TimeSpan.ToString();
        }



        private void DisplaySystemParameters()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double menuHeight = SystemParameters.MenuHeight;
            double captionHeight = SystemParameters.CaptionHeight;

            //Console.WriteLine($"Screen Width: {screenWidth}");
            //Console.WriteLine($"Screen Height: {screenHeight}");
            //Console.WriteLine($"Menu Height: {menuHeight}");
            //Console.WriteLine($"Caption Height: {captionHeight}");
        }

        public Config ReadConfig(string filepath)
        {
            Reader reader = new Reader();
            Config config = reader.ReadJson<Config>(filepath);
            return config;
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedMenuItem = sender as MenuItem;
            if (selectedMenuItem != null)
            {
                // Получаем заголовок выбранного пункта меню
                string selectedOption = selectedMenuItem.Header.ToString();
                TextBox.Text = selectedOption;
                // Пробуем преобразовать строку к перечислению
                if (Enum.TryParse(selectedOption, out Config.Mode selectedMode))
                {
                    Config.OperationMode = selectedMode;
                }
                else
                {
                    // Устанавливаем значение по умолчанию, если преобразование не удалось
                    Config.OperationMode = Config.Mode.Editor; // Значение по умолчанию
                }
            }

        }

        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu.IsOpen = true;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            UpdateTimeSpan();
            // Вызываем событие и передаем объект Config
            ConfigUpdated?.Invoke(Config);
            this.DialogResult = true;
            // Закрываем окно
            this.Close();
        }

        private void UpdateTimeSpan()
        {
            string timeText = TextBox_Time.Text.Trim(); // Убираем лишние пробелы

            if (TimeSpan.TryParse(timeText, out TimeSpan parsedTimeSpan))
            {
                Config.TimeSpan = parsedTimeSpan;
            }
            else
            {
                // Обработка случая, когда строка не является корректным TimeSpan
                MessageBox.Show("Пожалуйста, введите корректное значение времени в формате 'hh:mm:ss' или 'd.hh:mm:ss'.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                Config.TimeSpan = TimeSpan.Zero; // Можно установить значение по умолчанию или оставить предыдущие настройки
            }
        }
    }
}
