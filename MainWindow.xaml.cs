using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Serialization;


namespace SMC_GUI
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<Item> items;
        public Description Description;
        public Dictionary<string, string> XMLAtlasDict = new Dictionary<string, string>();
        public inventoryDescriptions InventoryDescriptions = new inventoryDescriptions();
        public BitmapImage IconMap;
        public Image Icons;
        public MyGUI XMLAtlas;
        public MainWindow()
        {
            InitializeComponent();
            Settings newWindow = new Settings();
            newWindow.DataPassed += NewWindow_DataPassed;
            newWindow.ShowDialog();


            
            
        }

        private void NewWindow_DataPassed(object sender, string data)
        {
            ReadJson(filepath:data);
            ReadJsonRecipe(filepath: "Files/SurvivalRecipeList.json");
            DisplaySystemParameters();
            ResizeWindow();
            ModsName();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            // Получаем доступ к ListBox
            ListBox listBox = sender as ListBox;

            // Проверяем, был ли выбран элемент
            if (listBox.SelectedItem is ListBoxItem selectedListBoxItem)
            {
                if (Icons != null)
                {
                    Grid.Children.Remove(Icons);
                }
               
                // Получение тега из выбранного элемента
                var tag = selectedListBoxItem.Tag.ToString();

                // Попытка получить значение из словаря по тегу
                if (XMLAtlasDict.TryGetValue(tag, out string value))
                {
                    var size =XMLAtlas.Resource.Group.Size;
                    

                    // Выводим значения переменных
                    Icons = new Image();
                    Icons = LocationIcon(SplitString(value,0),
                        SplitString(value,1), IconMap,
                        SplitString(size,0), SplitString(size,1));
                }
                else
                {
                    throw new NullReferenceException($"Key '{tag}' not found in the dictionary.");
                }

                RichTextBox.Document.Blocks.Clear();

                Item item = new Item();

                item.craftTime = 0;
                item.itemId = tag;
                item.quantity = 1;
                item.ingredientList = new List<Ingredient>();

                string[] Ingridient = new[]
                {
                    "Quantity: ",
                    "itemId: "
                };

                string info = $"ItemId: {item.itemId}\n" +
                              $"Quantity: {item.quantity}\n" +
                              $"CraftTime: {item.craftTime}\n" +
                              $"IngridientList:\n {Ingridient[1]}";


                
                Paragraph paragraph = new Paragraph();

                TaggedRun taggedRun = new TaggedRun(info, item, paragraph);

                paragraph.Inlines.Add(taggedRun);

                RichTextBox.Document.Blocks.Add(paragraph);
                
            }
        }

        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_ClickReload(object sender, RoutedEventArgs e)
        {
            foreach (var block in RichTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is TaggedRun taggedRun)
                        {

                            Item item = taggedRun.Tag; // здесь будет ваш selectedItem

                            item.ingredientList.RemoveAt(item.ingredientList.Count - 1);

                            RemoveLastTwoLines(taggedRun.Paragraph);

                            Console.WriteLine(item.ingredientList.Count);
                        }
                        break;
                    }
                    break;
                }
            }
        }



        private void Button_ClickUpload(object sender, RoutedEventArgs e)
        {
            foreach (var block in RichTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is TaggedRun taggedRun)
                        {
                            
                            Item item = taggedRun.Tag; // здесь будет ваш selectedItem
                            
                            Ingredient ingredient = new Ingredient
                            {
                                    itemId = "test", 
                                    quantity = 1 
                            };
                            item.ingredientList.Add(ingredient);


                            
                            taggedRun.Paragraph.Inlines.Add(new Run("Ingredient Quantity:  \n Ingredient ID: "));

                            RichTextBox.Document.Blocks.Clear();

                            RichTextBox.Document.Blocks.Add(taggedRun.Paragraph);

                            Console.WriteLine(item.ingredientList.Count);
                        }
                        break;
                    }
                    break;
                }
            }
            
            

        }

        private void Button_ClickSave(object sender, RoutedEventArgs e)
        {

        }

        private void ReadJsonRecipe(string filepath)
        {
            if (string.IsNullOrWhiteSpace(filepath))
            {
                throw new ArgumentException("Путь к файлу не может быть пустым или нулевым", nameof(filepath));
            }

            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException($"Файл не найден по пути: {filepath}");
            }

            
            Reader reader = new Reader();

            // Чтение и разбиение файла на строки
             List<Recipe>txt = reader.ReadJsonToList<Recipe>(filepath);

            StringBuilder stringBuilder = new StringBuilder();

            
            foreach (var line in txt)
            {
                string name = $"{line.title} - \n";
                string id = $"{line.itemId} \n";
                stringBuilder.AppendLine( name + id);
            }

            
            TextTxt.Text = stringBuilder.ToString();
        }

        private void ReadJson(string filepath)
        {
            List<string> filePathsList = new List<string>();
            string[] filesToSearch = { "description.json", "IconMap.png", "IconMap.xml", "inventoryDescriptions.json"};

            if (string.IsNullOrWhiteSpace(filepath))
            {
                throw new ArgumentException("Путь к файлу не может быть пустым или нулевым", nameof(filepath));
            }

            foreach (var fileName in filesToSearch)
            {
                string[] files = Directory.GetFiles(filepath,fileName , SearchOption.AllDirectories);

                if (files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        filePathsList.Add(file);
                    }
                }
                else
                {
                    Console.WriteLine($"Файлы для {fileName} не найдены.");
                }
            }
            
            Reader reader = new Reader();

            foreach (var file in filePathsList) 
            {
                switch (Path.GetFileName(file))
                {
                    case "description.json":
                        Description = reader.ReadJson<Description>(file);
                        break;
                    case "IconMap.png":
                        try
                        {
                            IconMap = new BitmapImage(new Uri(file, UriKind.RelativeOrAbsolute));
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            MessageBox.Show("У вас нет доступа к этому пути. Пожалуйста, проверьте права доступа: " +
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

            CreateNames();
        }

        private void RemoveLastTwoLines(Paragraph paragraph)
        {
            // Получаем список всех Inlines в Paragraph
            var inlines = paragraph.Inlines.ToList();

            // Строка - это текстовые элементы, которые должны быть удалены
            List<Inline> linesToRemove = new List<Inline>();

            // Находим последние две строки
            for (int i = inlines.Count - 1; i >= 0; i--)
            {
                if (inlines[i] is Run run)
                {
                    // Считаем количество переноса строки
                    var runText = run.Text;
                    var lineEndings = runText.Split(new[] { "rn", "r", "n" }, StringSplitOptions.None);

                    // Добавляем последние две строки в список для удаления
                    if (lineEndings.Length > 1 || (lineEndings.Length == 1 && linesToRemove.Count < 2))
                    {
                        for (int j = lineEndings.Length - 1; j >= 0; j--)
                        {
                            linesToRemove.Add(run);

                            if (linesToRemove.Count >= 2)
                                break;
                        }
                    }
                }

                if (linesToRemove.Count >= 2)
                    break;
            }

            // Удаляем последние две строки из Paragraph
            foreach (var inline in linesToRemove)
            {
                paragraph.Inlines.Remove(inline);
            }
        }

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
    }

    

    public class Item
    {
        public string itemId { get; set; }
        public int quantity { get; set; }
        public int craftTime { get; set; }
        public List<Ingredient> ingredientList { get; set; }
    }

    public class Ingredient
    {
        public int quantity { get; set; }
        public string itemId { get; set; }
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
}
