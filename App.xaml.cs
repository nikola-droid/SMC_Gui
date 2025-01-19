using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace SMC_GUI
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Проверка аргументов командной строки
            if (e.Args.Length > 0 && e.Args[0] == "-dev")
            {
                AllocConsole(); // Открытие консольного окна
                Console.WriteLine("Приложение запущено в режиме разработчика.");
                // Вы можете добавить больше логики для режима разработчика
            }

        }
    }
}
