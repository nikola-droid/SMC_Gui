using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Path = System.IO.Path;
using EngLibrary;
using System.Xml.Serialization;
using Formatting = Newtonsoft.Json.Formatting;
using SMC_GUI;



namespace SMC_GUI
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isProgrammaticChange = false;
        public List<Item> items = new List<Item>();
        public List<Item> itemsHideout = new List<Item>();
        public Description Description = new Description
        {
            name = "Error",
            creatorId ="Error",
            description = "Error",
            fileId = "Error",
            localId = "Error",
            type = "Error"
        };
        public Dictionary<string, string> XMLAtlasDict = new Dictionary<string, string>();
        public inventoryDescriptions InventoryDescriptions = new inventoryDescriptions();
        public BitmapImage IconMap;
        public Image Icons;
        public MyGUI XMLAtlas;
        public Dictionary<string, bool> MessageList = new Dictionary<string, bool>();
        public List<CheckBox> CheckBoxes = new List<CheckBox>();
        public List<TextBox> TextBoxes = new List<TextBox>();
        public Dictionary<CheckBox, TextBox> pairs = new Dictionary<CheckBox, TextBox>();
        public Config Config;
        public EngLibrary.OpenFileDialog openFileDialog;
        public Loger loger;
        public Folders folder;
        public FileStream fileStream;

        public string LogFileName;
        public string LogFilePath;

        public ListBox listBox;

        

        public MainWindow()
        {
            loger = new Loger();
            folder = new Folders();
            openFileDialog = new EngLibrary.OpenFileDialog();

            Settings secondWindow = new Settings();
            secondWindow.ConfigUpdated += SecondWindow_ConfigUpdated;
            bool? result = secondWindow.ShowDialog();
            if (result == true)
            {
                Console.WriteLine($"Добро пожаловать в SMC_GUI_{Config.OperationMode}");
            }
            else
            {
                this.Close();
            }

            bool? Folder = folder.CreateFolder(AppDomain.CurrentDomain.BaseDirectory + "Log");
            LogFileName = Path.Combine(loger.GenerateLogFileName());
            LogFilePath = Path.Combine(Config.LogFilePath, LogFileName);
            loger.DestroyLogFile(Config.LogFilePath, Config.TimeSpan, loger, fileStream,
                "ERROR", openFileDialog);
            fileStream = loger.CreateLogFile(Path.Combine(Config.LogFilePath, LogFileName));

            Console.WriteLine("Все настройки применины");

            switch (Config.OperationMode)
            {
                case Config.Mode.Editor:
                    if (Config.UsInputFilePath)
                    {
                        InitializeComponent();
                        NewWindow_DataPassed(openFileDialog.DialogOpenFolder(subDirectoryPath: Config.SubDirectoryPath,
                            mode: "MessageBox", loger, fileStream,
                            Config.LogFilePath, "ERROR"));
                    }
                    else
                    {
                        InitializeComponent();
                        NewWindow_DataPassed(openFileDialog.DialogOpenFolder(subDirectoryPath: Config.SubDirectoryPath,
                            mode: "MessageBox", loger, fileStream,
                            Config.LogFilePath, "ERROR"));
                    }

                    break;
                case Config.Mode.Compare:
                    try
                    {
                        Compare compare = new Compare();
                        compare.CompareJson(Config.InputDirectory, loger, fileStream,openFileDialog,
                            Config, items, itemsHideout);

                        folder.CreateFolder(AppDomain.CurrentDomain.BaseDirectory + "Output");
                        // Сохраняем данные в файлы
                        Save("Output/craftbot.json", items);
                        Save("Output/hideout.json", itemsHideout);

                        openFileDialog.OpenLogDirectory(Path.GetDirectoryName("Output / hideout.json"));
                        Environment.Exit(1);
                    }
                    catch (Exception ex)
                    {
                        loger.Log(fileStream, ex.Message, "ERROR");
                        MessageBox.Show("LogError", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        openFileDialog.OpenLogDirectory(Config.LogFilePath);
                        Environment.Exit(1);
                    }
                    break;
                case Config.Mode.Auto:
                    MessageBox.Show("Извините пока что этот режим не готов", "Information",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Environment.Exit(1);
                    break;
                case Config.Mode.EditCraft:
                    InitializeComponent();
                    if (Config.UsInputFilePath)
                    {
                        ReadCompleateCraftJson(Config.InputFilePath);
                    }
                    else
                    {
                        ReadCompleateCraftJson(openFileDialog.DialogOpenFile(
                            mode: "MessageBox", loger, fileStream, Config.LogFilePath,
                            "ERROR"));
                    }
                    break;
            }
        }

        private void NewWindow_DataPassed( string data)
        {
            ReadJson(filepath:data);
            ReadJsonRecipe(filepath: "Files/SurvivalRecipeList.json");
            DisplaySystemParameters();
            ResizeWindow();
            ModsName();
            Console.WriteLine("вызов чтения json");

        }

        private void SecondWindow_ConfigUpdated(Config config)
        {
            // Обработка полученного объекта Config
            //MessageBox.Show($"Received Setting1: {config.OperationMode}");
            //MessageBox.Show($"Received Setting1: {config.TimeSpan}");
            Config = config;
            Console.WriteLine("Конфиг прочитан");
        }
        private string GetDropButton()
        {
            return MyButton.Content.ToString();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var VARIABLE in CheckBoxes)
            {
                VARIABLE.IsEnabled = true;
                _isProgrammaticChange = true;
                VARIABLE.IsChecked = false;
                
            }

            foreach (var VARIABLE in TextBoxes)
            {
                VARIABLE.Text = "1";
            }
            // Получаем доступ к ListBox
            listBox = sender as ListBox;

            // Проверяем, был ли выбран элемент
            if (listBox.SelectedItem is ListBoxItem selectedListBoxItem)
            {
                if (Icons != null)
                {
                    Grid.Children.Remove(Icons);
                }

                Item itemFind = items.FirstOrDefault(i => i.itemId.Equals(selectedListBoxItem.Tag.ToString(),
                    StringComparison.OrdinalIgnoreCase));
                DrawingBrush drawingBrush = new DrawingBrush(); // Создаем DrawingBrush один раз
                if (itemFind != null) // Проверяем, найден ли элемент
                {
                    // Проверяем ингредиенты
                    if (itemFind.ingredientList.Count > 0)
                    {
                        drawingBrush.Drawing = CreateDrawing(Brushes.Green);
                    }
                }
                else
                {
                    // Если элемент не найден, задаем другой цвет 
                    drawingBrush.Drawing = CreateDrawing(Brushes.Yellow); 
                }

                // Присваиваем DrawingBrush выбранному элементу
                selectedListBoxItem.Background = drawingBrush;


                // Получение тега из выбранного элемента
                var tag = selectedListBoxItem.Tag.ToString();

                if (Config.OperationMode == Config.Mode.EditCraft)
                {
                    RichTextBox.Document.Blocks.Clear();
                    Item existingItem = items.FirstOrDefault(variable =>
                        variable.itemId == selectedListBoxItem.Tag);
                    if (existingItem != null)
                    {
                        string ingredientsString = "";
                        foreach (var item in existingItem.ingredientList)
                        {
                            ingredientsString += ViewTemplate(item.quantity.ToString(), item.itemId, item.comment);
                        }

                        string info = $"Type: {existingItem.TypeDropdawun}\n" +
                                      $"ItemId: {existingItem.itemId}\n" +
                                      $"Quantity: {existingItem.quantity}\n" +
                                      $"CraftTime: {existingItem.craftTime}\n" +
                                      $"{ingredientsString}";


                        AddParagraphToRichTextBox(info, existingItem);
                    }
                    else
                    {
                        loger.Log(fileStream,"Данные о предмете не найдены","ERROR");
                        MessageBox.Show("LogError", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        openFileDialog.OpenLogDirectory(Config.LogFilePath);
                        Environment.Exit(1);
                    }

                }
                else
                {
                    // Попытка получить значение из словаря по тегу
                    if (XMLAtlasDict.TryGetValue(tag, out string value))
                    {
                        var size = XMLAtlas.Resource.Group.Size;

                        Icons = new Image();

                        Icons = LocationIcon(SplitString(value, 0),
                            SplitString(value, 1), IconMap,
                            SplitString(size, 0),
                            SplitString(size, 1));
                    }
                    else
                    {
                        loger.Log(fileStream, $"Ключ '{tag}' не найден в словаре.",
                            "ERROR");

                        MessageBox.Show("LogError", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        openFileDialog.OpenLogDirectory(Config.LogFilePath);
                        Environment.Exit(1);
                    }

                    RichTextBox.Document.Blocks.Clear();

                    // Поиск существующего элемента

                    Item existingItem = items.FirstOrDefault(variable =>
                        variable.itemId == selectedListBoxItem.Tag);
                    
                    if (existingItem != null)
                    {
                        string ingredientsString = "";
                        foreach (var item in existingItem.ingredientList)
                        {
                            ingredientsString += ViewTemplate(item.quantity.ToString(), item.itemId, item.name);
                        }

                        string info = $"Type: {existingItem.TypeDropdawun}\n" +
                                      $"ItemId: {existingItem.itemId}\n" +
                                      $"Quantity: {existingItem.quantity}\n" +
                                      $"CraftTime: {existingItem.craftTime}\n" +
                                      $"IngridientList:\n" +
                                      $"{ingredientsString}";


                        AddParagraphToRichTextBox(info, existingItem);
                        Console.WriteLine($"Объект уже инициализирован:" +
                                          $"Тип объекта {existingItem.TypeDropdawun}" +
                                          $"Количество ингридиентов: {existingItem.ingredientList.Count}");
                    }
                    else
                    {
                        // Создаем новый элемент
                        Item item = new Item
                        {
                            TypeDropdawun = "None",
                            comment = selectedListBoxItem.Content.ToString(),
                            itemId = selectedListBoxItem.Tag.ToString(),
                            ingredientList = new List<Ingredient>(),
                            craftTime = 1,
                            quantity = 1
                        };
                        string info = $"Type: None\n" +
                                      $"ItemId: {selectedListBoxItem.Tag}\n" +
                                      $"Quantity: {1}\n" +
                                      $"CraftTime: {1}\n" +
                                      $"IngridientList:\n";

                        AddParagraphToRichTextBox(info, item);
                        items.Add(item);
                        Console.WriteLine($"Инициализирован новый предмет: {item.comment}" +
                                          $"Тип объекта {item.TypeDropdawun}");
                    }
                }
            }
        }

        private void AddParagraphToRichTextBox(string info, Item item)
        {

            Paragraph paragraph = new Paragraph();
            TaggedRun taggedRun = new TaggedRun(info, item, paragraph);
            paragraph.Inlines.Add(taggedRun);
            RichTextBox.Document.Blocks.Add(paragraph);
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_ClickSave(object sender, RoutedEventArgs e)
        {
            RestructList();
        }

        #region ReadJson and Create: CheckBox,Ingredient, CreateMessageBox, CreateNames()
        private void ReadJsonRecipe(string filepath)
        {
            // Проверка на пустую строку
            if (string.IsNullOrWhiteSpace(filepath))
            {
                loger.Log(fileStream, $"Путь к файлу не может быть пустым или нулевым + {nameof(filepath)}",
                    "ERROR");

                MessageBox.Show("LogError", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                openFileDialog.OpenLogDirectory(Config.LogFilePath);
                Environment.Exit(1);
            }

            // Проверка на существование файла
            if (!File.Exists(filepath))
            {
                loger.Log(fileStream, $"Файл не найден по пути: {filepath}",
                    "ERROR");

                MessageBox.Show("LogError", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                openFileDialog.OpenLogDirectory(Config.LogFilePath);
                Environment.Exit(1);
            }

            // Инициализация ридера
            Reader reader = new Reader();

            // Чтение JSON и преобразование в список рецептов
            List<Recipe> recipes = reader.ReadJsonToList<Recipe>(filepath,
                loger, fileStream, "ERROR",openFileDialog ,Config.LogFilePath);

            recipes.Sort((x, y) => string.Compare(x.title, y.title));

            // Создание ScrollViewer
            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Width = 580, // Пример ширины
                Height = 864 // Пример высоты
            };

            // Создание StackPanel для размещения CheckBox
            StackPanel stackPanel = new StackPanel();

            // Создание CheckBox для каждого рецепта
            foreach (var recipe in recipes)
            {
                // Создаем контейнер для CheckBox и TextBox
                StackPanel itemContainer = new StackPanel
                {
                    Orientation = Orientation.Horizontal, // Устанавливаем горизонтальную ориентацию
                    Margin = new Thickness(0, 0, 0, 5) // Задаем отступ
                };
                // Создаем TextBox для количества
                TextBox quantityTextBox = new TextBox
                {
                    Width = 50, // Ширина текстового поля
                    Margin = new Thickness(5, 0, 0, 0), // Отступ слева
                    Text = "1" // Устанавливаем начальное значение
                };

                // Создаем ингредиент
                Ingredient ingredient = new Ingredient( int.Parse(quantityTextBox.Text), // берем значение из TextBox
                     recipe.itemId,
                    recipe.title,
                     quantityTextBox,
                     recipe.title);
                
            // Создаем CheckBox
                CheckBox checkBox = new CheckBox
                {
                    Content = recipe.title,  // Предполагается, что у Recipe есть свойство title
                    Tag = ingredient,        // Установите тег в объект Ingredient
                    Margin = new Thickness(0, 0, 5, 10), // Отступ между элементами
                    IsEnabled = false,
                    Foreground = new SolidColorBrush(Colors.White) // Установка цвета текста
                };

                checkBox.Checked += CheckBox_Checked;
                checkBox.Unchecked += CheckBox_Unchecked;

                CheckBoxes.Add(checkBox);
                TextBoxes.Add(quantityTextBox);
                pairs.Add(checkBox, quantityTextBox);



                // Добавление CheckBox и TextBox на контейнер
                itemContainer.Children.Add(checkBox);
                itemContainer.Children.Add(quantityTextBox);

                // Добавление контейнера со CheckBox и TextBox на основной StackPanel
                stackPanel.Children.Add(itemContainer);
            }

            // Добавление StackPanel в ScrollViewer
            scrollViewer.Content = stackPanel;

            CanvasRecipe.Children.Add(scrollViewer); // Добавляем ScrollViewer на Canvas
        }
        private void ReadJson(string filepath)
        {
            List<string> filePathsList = new List<string>();
            string[] filesToSearch = { "description.json",
                "IconMap.png",
                "IconMap.xml",
                "inventoryDescriptions.json"};

            if (string.IsNullOrWhiteSpace(filepath))
            {
                loger.Log(fileStream, $"Путь к файлу не может быть пустым или нулевым {nameof(filepath)}",
                    "ERROR");

                MessageBox.Show("LogError", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                openFileDialog.OpenLogDirectory(Config.LogFilePath);
                Environment.Exit(1);
            }

            foreach (var fileName in filesToSearch)
            {
                string[] files = Directory.GetFiles(filepath,fileName,
                    SearchOption.AllDirectories);

                if (files.Length > 0)
                {
                   
                    foreach (string file in files)
                    {
                        filePathsList.Add(file);
                        string message = $"Файл: {fileName} найден";
                        MessageList.Add(message + file, true);
                    }
                }
                else
                {
                    string message = $"Файл для {fileName} не найдены.";
                    MessageList.Add(message, false);
                }
            }
            
            Reader reader = new Reader();

            foreach (var file in filePathsList) 
            {
                switch (Path.GetFileName(file))
                {
                    case "description.json":
                        Description = reader.ReadJson<Description>(file, loger,
                            fileStream, "ERROR", openFileDialog, Config.LogFilePath);
                        if (string.IsNullOrEmpty(Description?.name))
                        {
                            Description.name = "Error";
                        }
                        break;
                    case "IconMap.png":
                        try
                        {
                            IconMap = new BitmapImage(new Uri(file, UriKind.RelativeOrAbsolute));
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            MessageBox.Show("У вас нет доступа к этому пути." +
                                            " Пожалуйста, проверьте права доступа: " +
                                            ex.Message);
                        }
                        break;
                    case "IconMap.xml":
                         XMLAtlas = reader.SerializerXML<MyGUI>(file, loger,
                             fileStream, "ERROR", openFileDialog,Config.LogFilePath);
                        foreach (var index in XMLAtlas.Resource.Group.Indices)
                        {
                            if (!XMLAtlasDict.ContainsKey(index.Name))
                            {
                                XMLAtlasDict.Add(index.Name, index.Frame.Point);
                            }
                            
                        }
                        break;
                    case "inventoryDescriptions.json":
                        InventoryDescriptions.description =
                            reader.ReadJsonAnNormal<string,string>(
                                file, loger, fileStream, "ERROR",
                                openFileDialog, Config.LogFilePath);
                        break;
                }
            }

            CreateMessageBox();
            CreateNames();
            
        }

        private void ReadCompleateCraftJson(string filepath)
        {
            Reader reader = new Reader();
            items = reader.ReadJson<List<Item>>(filepath, loger, fileStream, "ERROR",
                openFileDialog, Config.LogFilePath);
            foreach (var item in items)
            {
                DrawingBrush drawingBrush = new DrawingBrush(); // Создаем DrawingBrush один раз
                if (item.comment == null)
                {

                    drawingBrush.Drawing = CreateDrawing(Brushes.DarkRed);

                    ListBox.Items.Add(new ListBoxItem
                    {
                        Content = item.comment ?? item.itemId,
                        Tag = item.itemId,
                        FontSize = 14,
                        Width = 420,
                        Height = 50,
                        Background = drawingBrush,
                    });
                }
                else
                {
                    drawingBrush.Drawing = CreateDrawing(Brushes.Green);

                    ListBox.Items.Add(new ListBoxItem
                    {
                        Content = item.comment ?? item.itemId,
                        Tag = item.itemId,
                        FontSize = 14,
                        Width = 420,
                        Height = 50,
                        Background = drawingBrush,
                    });
                }
            }
        }
        
        #endregion
        
        private Image LocationIcon(int offsetX, int offsetY, BitmapImage IconMap, int width, int height)
        {
            // offsetX   координата X (в пикселях)
            // offsetY   координата Y (в пикселях)
            //int width    ширина обрезаемой области
            //int height   высота обрезаемой области
            CroppedBitmap croppedBitmap = new CroppedBitmap(IconMap, new Int32Rect(offsetX, offsetY, width, height));

            Image Icons = new Image
            {
                Source = croppedBitmap,
                Width = 140,
                Height = 140,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(630, 100, 0, 0)
            };
            Grid.Children.Add(Icons);
            return Icons;
        }

        private void CreateNames()
        {
            foreach (var item in InventoryDescriptions.description)
            {

                ListBox.Items.Add(new ListBoxItem
                {
                    Content = item.Value, // Выводим имя элемента в ListBox
                    Tag = item.Key
                });
            }
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

        public void ResizeWindow()
        {
            this.Width = SystemParameters.PrimaryScreenWidth * 0.8; // Устанавливаем ширину окна на 80% ширины экрана
            this.Height = SystemParameters.PrimaryScreenHeight * 0.8; // Устанавливаем высоту окна на 80% высоты экрана
        }

        public int SplitString(string value, int index)
        {
            string[] parts = value.Split(' ');

            // Преобразуем части в числа и присваиваем переменным
            int firstVariable = int.Parse(parts[index]); // Первое число

            return firstVariable;
        }

        public void ModsName()
        {
           ModName.Text = Description.name;
        }

        public void CreateMessageBox()
        {
            string text = "";
            
            int i = MessageList.Count;
            int m = 0;
            string Em = "";
            foreach (var message in MessageList)
            {
                if(message.Value == true)
                {
                    m++;
                }
                else
                {
                    m--;
                    Em += message.Key;
                }
                
            }

            if (m == i)
            {
                text += "Все файлы найдены";
                //MessageBox.Show(text, "Status", MessageBoxButton.OK, MessageBoxImage.Information);
                Console.WriteLine($"Все файлы найдены");
            }
            else
            {
                text += Em;
                MessageBox.Show(text, "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Console.WriteLine($"Возможны проблемы: {text}");
            }

            
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            
            CheckBox checkBox = sender as CheckBox;

            foreach (var block in RichTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is TaggedRun taggedRun)
                        {
                            Item item = taggedRun.Tag; // selectedItem<Item>

                            // Получаем ингредиент из тега CheckBox
                            
                             Ingredient ingredient = new Ingredient(checkBox.Tag as Ingredient);

                            ingredient.quantity = Int32.Parse(ingredient.textBox.Text);

                            if (ingredient.quantity <= 0 )
                            {
                                ingredient.quantity = 1;
                            }
                            
                            // Добавляем ингредиент в список элемента
                            item.ingredientList.Add(ingredient);
                            _isProgrammaticChange = false;
                            Console.WriteLine(item.ingredientList.Count);
                            // Создаем новый параграф для добавления информации об ингредиенте
                            Paragraph newParagraph = new Paragraph();

                            newParagraph.Tag = ingredient;

                            // Добавляем информацию об ингредиенте в новый параграф
                            newParagraph.Inlines.Add(new Run(ViewTemplate(ingredient.quantity.ToString(),
                                                                            ingredient.itemId, ingredient.name)));

                            // Добавляем новый параграф в RichTextBox
                            RichTextBox.Document.Blocks.Add(newParagraph);
                        }
                        break; // Можно убрать, если вы хотите обрабатывать все inline элементы
                    }
                    break; // Можно убрать, если вы хотите обрабатывать все блоки
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isProgrammaticChange)
            {
                return;
            }
            CheckBox checkBox = sender as CheckBox;
            Ingredient ingredientToRemove = checkBox.Tag as Ingredient;

            if (ingredientToRemove == null)
            {
                Console.WriteLine("Ingredient from CheckBox null.");
                return;
            }

            List<Paragraph> paragraphsToRemove = new List<Paragraph>(); // Список для хранения параграфов, которые нужно удалить

            foreach (var block in RichTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    Ingredient ingredientInParagraph = paragraph.Tag as Ingredient;

                    // Проверка на совпадение тегов параграфа и чекбокса
                    if (ingredientInParagraph != null && ingredientInParagraph.itemId == ingredientToRemove.itemId)
                    {
                        paragraphsToRemove.Add(paragraph);
                    }

                    // Обработка инлайн-элементов
                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is TaggedRun taggedRun)
                        {
                            Item item = taggedRun.Tag as Item;

                            if (item != null && item.ingredientList.Count > 0)
                            {
                                // Удаляем ингредиент из списка
                                item.ingredientList.RemoveAll(ingredient => ingredient.itemId == ingredientToRemove.itemId);

                                // Выводим информацию о текущем состоянии ingredientList
                                Console.WriteLine(item.ingredientList.Count);
                                foreach (var ingredient in item.ingredientList)
                                {
                                    Console.WriteLine(ingredient.name);
                                }
                            }
                        }
                    }
                }
            }

            // После завершения перебора удаляем все собранные параграфы
            foreach (var paragraph in paragraphsToRemove)
            {
                RichTextBox.Document.Blocks.Remove(paragraph);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedMenuItem = sender as MenuItem;
            if (selectedMenuItem != null)
            {
                // Получаем заголовок выбранного пункта меню
                string selectedOption = selectedMenuItem.Header.ToString();
                MyButton.Content = selectedOption;
            }
        }

        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            // Открываем контекстное меню программно
            ContextMenu.IsOpen = true;
        }

        private GeometryDrawing CreateDrawing(Brush brush)
        {
            // Создание прямоугольника с заданным цветом
            var drawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 100, 100)), // Простая прямоугольная форма
                Brush = brush // Используем переданный цвет
            };

            return drawing;
        }

        public string ViewTemplate(string ingredientQuantity,string ingredientID,string name)
        {
            string template = $"Ingredient Quantity: {ingredientQuantity}\n\n" +
                              $"Ingredient ID: {ingredientID}\n\n" +
                              $"Name: {name} \n\n";

            return template;
        }

       

        public void Save( string output, List<Item> data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            string filePath = output;

            try
            {
                File.WriteAllText(filePath, json);
                MessageBox.Show($"Данные успешно сохранены в файл: {filePath}");
                
            }
            catch (Exception ex)
            {

                loger.Log(fileStream, $"Произошла ошибка при сохранении данных: {ex.Message}",
                    "ERROR");

                MessageBox.Show("LogError", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                openFileDialog.OpenLogDirectory(Config.LogFilePath);
                Environment.Exit(1);
            }


        }

        public void RestructList()
        {
            // Создаем временный список для хранения элементов для удаления
            List<Item> itemsToRemove = new List<Item>();

            foreach (var item in items)
            {
                if (item.ingredientList.Count == 0 && item.ingredientList == null)
                {
                    itemsToRemove.Add(item);
                }
                switch (item.TypeDropdawun)
                {
                    case "Craftbot":
                        break;
                    case "Hideout":
                        itemsHideout.Add(item);
                        itemsToRemove.Add(item); // Добавляем элемент в список для удаления
                        break;
                }
            }

            // Удаляем элементы после завершения итерации
            foreach (var item in itemsToRemove)
            {
                items.Remove(item);
            }

            // Показ уведомления с количеством элементов
            MessageBox.Show($"Craftbot: {items.Count}\nHideout: {itemsHideout.Count}");

            folder.CreateFolder(AppDomain.CurrentDomain.BaseDirectory +Path.GetDirectoryName(Config.OutputDirectory));

            string filenamecraftbot = Description.name.Replace(" ","") + ".json";
            string pathcraftbot =  Config.OutputDirectory + filenamecraftbot;

            if (items.Count > 0 )
            {
                Save(pathcraftbot, items);
            }

            if (itemsHideout.Count > 0)
            {
                Save(Config.OutputFilePath + filenamecraftbot, itemsHideout);
            }

            openFileDialog.OpenLogDirectory(Path.GetDirectoryName(Config.OutputFilePath +
                                                                  filenamecraftbot));
            Environment.Exit(1);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string info = "";
            Item item = null; // Инициализируем item как null
            string ingredientsString = "";
            CheckBox checkBox = sender as CheckBox;

            // Список для хранения Paragraph, которые необходимо удалить
            List<Paragraph> paragraphsToRemove = new List<Paragraph>();

            foreach (var block in RichTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is TaggedRun taggedRun)
                        {
                            item = taggedRun.Tag as Item; // Предполагается, что Tag возвращает Item


                            foreach (var ingredient in item.ingredientList)
                            {
                                ingredientsString += ViewTemplate(ingredient.quantity.ToString(),
                                    ingredient.itemId, ingredient.comment);
                            }

                            if (item != null) // Проверяем, что item не null
                            {
                                item.TypeDropdawun = GetDropButton(); // Обновляем TypeDropdown
                                paragraphsToRemove.Add(paragraph); // Добавляем paragraph в список

                                // Формируем строку с информацией о элементе
                                info += $"Type: {item.TypeDropdawun}\n" +
                                        $"ItemId: {item.itemId}\n" + // Измените это на ваш идентификатор элемента
                                        $"Quantity: {item.quantity}\n" +
                                        $"CraftTime: {item.craftTime}\n" +
                                        $"IngredientList:\n"+
                                        $"{ingredientsString}"; // Предполагаем, что это перечисление
                            }

                            
                        }
                    }
                }
            }
            

            // Удаляем все собранные Paragraph после завершения итерации
            foreach (var paragraph in paragraphsToRemove)
            {
                RichTextBox.Document.Blocks.Clear();
            }

            // Добавляем новую информацию в RichTextBox
            if (item != null) // Проверяем, что item не null для добавления информации
            {
                AddParagraphToRichTextBox(info, item);
                
            }
            
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTextBox = sender as TextBox;
            var searchText = searchTextBox.Text.ToLower(); // Приводим к нижнему регистру для нечувствительного поиска

            foreach (CheckBox recipe in pairs.Keys) 
            {
                if (recipe is CheckBox checkBox)
                {
                    var isVisible = string.IsNullOrEmpty(searchText) || checkBox.Content.ToString().ToLower().Contains(searchText);
                    checkBox.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                    pairs.TryGetValue(checkBox, out TextBox textbox);
                    textbox.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

    }





    public class Item
    {
        public string comment { get; set; }
        public string itemId { get; set; }
        public int quantity { get; set; }
        public int craftTime { get; set; }
        public List<Ingredient> ingredientList { get; set; }
        [JsonIgnore]
        public string TypeDropdawun { get; set; }
    }

    public class Ingredient
    {
        public Ingredient() { }
        public int quantity { get; set; }
        public string itemId { get; set; }
        [JsonIgnore]
        public string name { get; set; }
        [JsonIgnore]
        public TextBox textBox { get; set; }
        
        public string comment { get; set; }

        public Ingredient(Ingredient ingredient)
        {
            quantity = ingredient.quantity;
            itemId = ingredient.itemId;
            name = ingredient.name;
            textBox = ingredient.textBox;
            comment = ingredient.comment;
        }

        public Ingredient(int quantity1, string itemId1, string name1, TextBox textBox1, string comment1)
        {
            quantity = quantity1;
            itemId = itemId1;
            name = name1;
            textBox = textBox1;
            comment = comment1;
        }
    }

    public class TaggedRun : Run
    {
        public Item Tag { get; set; }
        public Paragraph Paragraph { get; set; }

        public TaggedRun(string text, Item Item, Paragraph paragraph) : base(text)
        {
            this.Tag = Item;
            this.Paragraph = paragraph;
        }
    }

    public class Description
    {
        public string  creatorId { get; set; }
        public string description { get; set; }
        public string fileId { get; set; }
        public string localId { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    public class inventoryDescriptions
    {
        public Dictionary<string, string> description { get; set; }
    }

    [XmlRoot("MyGUI")]
    public class MyGUI
    {
        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlElement("Resource")]
        public Resource Resource { get; set; }
    }

    public class Resource
    {
        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("Group")]
        public Group Group { get; set; }
    }

    public class Group
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("texture")]
        public string Texture { get; set; }

        [XmlAttribute("size")]
        public string Size { get; set; }

        [XmlElement("Index")]
        public List<Index> Indices { get; set; }  
    }

    public class Index
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("Frame")]
        public Frame Frame { get; set; }
    }

    public class Frame
    {
        [XmlAttribute("point")]
        public string Point { get; set; }
    }

    public class Recipe
    {
        public string itemId { get; set; }
        public string title { get; set; }
    }

    public class Config
    {
        public enum Mode
        {
            Auto,      // Автоматический режим
            Editor,   // Ручной режим
            Compare,  //Объединение многих json в 1
            EditCraft
        }
        [JsonProperty("Mode")]
        public Mode OperationMode { get; set; }
        public string OutputFilePath { get; set; }
        public bool UsInputFilePath { get; set; }
        public string InputFilePath { get; set; }
        public string LogFilePath { get; set; }
        public string SubDirectoryPath { get; set; }
        public string OutputDirectory { get; set; }
        public string InputDirectory { get; set; }
        public TimeSpan TimeSpan { get; set; }
    }

}
