﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Path = System.IO.Path;
using EngLibrary;
using System.IO.Ports;
using System.Security.Cryptography;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Serialization;
using Xceed.Wpf.Toolkit.Primitives;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Formatting = Newtonsoft.Json.Formatting;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;


namespace SMC_GUI
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {   
        
        public List<Item> items = new List<Item>();
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
        public Config Config;
        public Loger loger;
        public FileStream fileStream;

        public MainWindow()
        {
            Config = ReadConfig(filepath: "Config.json");
            InitializeComponent();
            NewWindow_DataPassed(Dialog());
        }

        private void NewWindow_DataPassed( string data)
        {
            ReadJson(filepath:data);
            ReadJsonRecipe(filepath: "Files/SurvivalRecipeList.json");
            DisplaySystemParameters();
            ResizeWindow();
            ModsName();
            loger = new Loger();
            fileStream = loger.CreateLogFile(filepath:"Log/Log.txt");
        }

        private string GetDropButton()
        {
            return MyButton.Content.ToString();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Console.WriteLine(items.Count);
            foreach (var VARIABLE in CheckBoxes)
            {
                VARIABLE.IsEnabled = true; 
                VARIABLE.IsChecked = false;
                
            }
            // Получаем доступ к ListBox
            ListBox listBox = sender as ListBox;

            // Проверяем, был ли выбран элемент
            if (listBox.SelectedItem is ListBoxItem selectedListBoxItem)
            {
                if (Icons != null)
                {
                    Grid.Children.Remove(Icons);
                }

                DrawingBrush drawingBrush = new DrawingBrush();
                drawingBrush.Drawing = CreateDrawing(Brushes.Green);

                // Присваиваем DrawingBrush выбранному элементу
                selectedListBoxItem.Background = drawingBrush;

                // Получение тега из выбранного элемента
                var tag = selectedListBoxItem.Tag.ToString();

                // Попытка получить значение из словаря по тегу
                if (XMLAtlasDict.TryGetValue(tag, out string value))
                {
                    var size =XMLAtlas.Resource.Group.Size;
                    
                    Icons = new Image();

                    Icons = LocationIcon(SplitString(value,0),
                        SplitString(value,1), IconMap,
                        SplitString(size,0),
                        SplitString(size,1));
                }
                else
                {
                    loger.Log(fileStream, $"Key '{tag}' not found in the dictionary.",
                        "ERROR");

                    MessageBox.Show("LogError", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }

                RichTextBox.Document.Blocks.Clear();

                
                // Проверяем, пуст ли список items
                if (items.Count == 0)
                {
                    // Создаем новый элемент
                    Item item = new Item
                    {
                        comment = selectedListBoxItem.Content.ToString(),
                        itemId = selectedListBoxItem.Tag.ToString(),
                        ingredientList = new List<Ingredient>(),
                        craftTime = 1,
                        quantity = 1
                    };
                    string info = $"ItemId: {selectedListBoxItem.Tag}\n" +
                                  $"Quantity: {1}\n" +
                                  $"CraftTime: {1}\n" +
                                  $"IngridientList:\n";

                    AddParagraphToRichTextBox(info, item);
                    items.Add(item);
                    return;
                }

                // Поиск существующего элемента

                Item existingItem = items.FirstOrDefault(variable => variable.itemId == selectedListBoxItem.Tag);
                Console.WriteLine(items.Contains(existingItem));
                if (existingItem != null)
                {
                    string ingredientsString = "";
                    foreach (var item in existingItem.ingredientList)
                    {
                        ingredientsString += ViewTemplate(item.quantity.ToString(), item.itemId, item.name);
                    }
                     
                    string info = $"ItemId: {existingItem.itemId}\n" +
                                  $"Quantity: {existingItem.quantity}\n" +
                                  $"CraftTime: {existingItem.craftTime}\n"+
                                  $"{ingredientsString}";
                                  

                    AddParagraphToRichTextBox(info, existingItem);
                }
                else
                {
                    // Создаем новый элемент
                    Item item = new Item
                    {
                        comment = selectedListBoxItem.Content.ToString(),
                        itemId = selectedListBoxItem.Tag.ToString(),
                        ingredientList = new List<Ingredient>(),
                        craftTime = 1,
                        quantity = 1
                    };
                    string info = $"ItemId: {selectedListBoxItem.Tag}\n" +
                                  $"Quantity: {1}\n" +
                                  $"CraftTime: {1}\n" +
                                  $"IngridientList:\n";

                    AddParagraphToRichTextBox(info, item);
                    items.Add(item);
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
            try
            {
                // Сериализация всей коллекции items
                
                string json = JsonConvert.SerializeObject(items, Formatting.Indented);
                
                // Укажите корректный путь к файлу, например "data.json"
                string filePath = "data.json";
                File.WriteAllText(filePath, json);
                var message = MessageBox.Show($"Данные успешно сохранены в файл: {filePath}");
                if (message == MessageBoxResult.OK)
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                
                loger.Log(fileStream, $"Произошла ошибка при сохранении данных: {ex.Message}",
                    "ERROR");

                MessageBox.Show("LogError", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
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
                this.Close();
            }

            // Проверка на существование файла
            if (!File.Exists(filepath))
            {
                loger.Log(fileStream, $"Файл не найден по пути: {filepath}",
                    "ERROR");

                MessageBox.Show("LogError", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }

            // Инициализация ридера
            Reader reader = new Reader();

            // Чтение JSON и преобразование в список рецептов
            List<Recipe> recipes = reader.ReadJsonToList<Recipe>(filepath);

            // Создание ScrollViewer
            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Width = 400, // Пример ширины
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
                    Margin = new Thickness(0, 0, 5, 0), // Отступ между элементами
                    IsEnabled = false
                };

                checkBox.Checked += CheckBox_Checked;
                checkBox.Unchecked += CheckBox_Unchecked;

                CheckBoxes.Add(checkBox);



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
                this.Close();
            }

            foreach (var fileName in filesToSearch)
            {
                string[] files = Directory.GetFiles(filepath,fileName , SearchOption.AllDirectories);

                if (files.Length > 0)
                {
                   
                    foreach (string file in files)
                    {
                        filePathsList.Add(file);
                        string message = $"Файл: {fileName} найден";
                        MessageList.Add(message, true);
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
                        Description = reader.ReadJson<Description>(file);
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
                         XMLAtlas = reader.SerializerXML<MyGUI>(file);
                        foreach (var index in XMLAtlas.Resource.Group.Indices)
                        {
                            XMLAtlasDict.Add(index.Name,index.Frame.Point);
                        }
                        break;
                    case "inventoryDescriptions.json":
                        InventoryDescriptions.description = reader.ReadJsonAnNormal<string,string>(file);
                        break;
                }
            }

            CreateMessageBox();
            CreateNames();
            
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
                Margin = new Thickness(729, 90, 0, 0)
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

            Console.WriteLine($"Screen Width: {screenWidth}");
            Console.WriteLine($"Screen Height: {screenHeight}");
            Console.WriteLine($"Menu Height: {menuHeight}");
            Console.WriteLine($"Caption Height: {captionHeight}");
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
                MessageBox.Show(text, "Status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                text += Em;
                MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            item.TypeDropdawun = GetDropButton();

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
            CheckBox checkBox = sender as CheckBox;
            List<Paragraph> paragraphsToRemove = new List<Paragraph>(); // Список для хранения параграфов, которые нужно удалить

            foreach (var block in RichTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    bool isParagraphTagMatched = paragraph.Tag == checkBox.Tag as Ingredient;

                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is TaggedRun taggedRun)
                        {
                            Item item = taggedRun.Tag as Item; // Предполагаем, что это корректный каст

                            if (item != null && item.ingredientList.Count > 0)
                            {
                                Ingredient ingredientToRemove = checkBox.Tag as Ingredient;
                                item.ingredientList.Remove(ingredientToRemove); // Удаляем ингредиент из списка

                                Console.WriteLine(item.ingredientList.Count);
                                foreach (var ingredient in item.ingredientList)
                                {
                                    Console.WriteLine(ingredient.name);
                                }
                            }
                        }
                    }

                    // Если тег параграфа совпадает с тегом чекбокса, добавляем параграф в список на удаление
                    if (isParagraphTagMatched)
                    {
                        Console.WriteLine(paragraph);
                        paragraphsToRemove.Add(paragraph);
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

        public string Dialog()
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            // Получаем путь к Roaming AppData
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string subDirectory = "Axolot Games\\Scrap Mechanic"; // Замените на нужное имя подкаталога
            string initialDirectory = Path.Combine(appDataPath, subDirectory);

            // Проверяем, существует ли указанная директория
            if (Directory.Exists(initialDirectory))
            {
                // Устанавливаем начальную директорию
                dialog.InitialDirectory = initialDirectory;
            }
            else
            {
                MessageBox.Show($"Подкаталог '{subDirectory}' не найден в '{appDataPath}'.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                dialog.InitialDirectory = appDataPath; // Если подкаталог не найден, используем LocalApplicationData
            }

            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }

            return null;
        }

        public Config ReadConfig(string filepath)
        {
            Reader reader = new Reader();
            Config config = reader.ReadJson<Config>(filepath);
            return config;
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

    public class ItemHideout
    {
        public string comment { get; set; }
        public string itemId { get; set; }
        public int quantity { get; set; }
        public int craftTime { get; set; }
        public List<IngredientHideout> ingredientList { get; set; }
    }

    public class Ingredient
    {
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

    public class IngredientHideout
    {
        public int quantity { get; set; }
        public string itemId { get; set; }
        public string name { get; set; }
        public TextBox textBox { get; set; }
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
        public string Comment { get; set; }
        public enum Mode
        {
            Auto,      // Автоматический режим
            Manually   // Ручной режим
        }
        public Mode OperationMode { get; set; }
        public string OutputFilePath { get; set; }

        public bool UsInputFilePath { get; set; }
        public string InputFilePath { get; set; }
    }

}
