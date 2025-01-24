using EngLibrary;
using System;
using System.Collections.Generic;
using System.IO;

namespace SMC_GUI
{
    internal class Compare
    {
        public Tuple<List<Item>, List<Item>> CompareJson(string filepath, Loger loger,
            FileStream fileStream, OpenFileDialog openFileDialog, Config Config,
            List<Item> items, List<Item> itemsHideout) //merge 
        {
            Reader reader = new Reader();
            string[] files = Directory.GetFiles(filepath);

            // Очистите списки перед заполнением, чтобы избежать дублирования данных
            items = new List<Item>();
            itemsHideout = new List<Item>();

            foreach (var file in files)
            {
                try
                {
                    string name = Path.GetFileName(file);
                    int startIndex = name.IndexOf('_') + 1; // +1 чтобы пропустить "_" символ
                    int endIndex = name.IndexOf('_', startIndex); // Найдем следующий символ "_"

                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        string result = name.Substring(startIndex, endIndex - startIndex);

                        // Проверяем результат
                        if (result == "craftbot")
                        {
                            var readItems = reader.ReadJson<List<Item>>(
                            file, loger, fileStream, "ERROR",
                                openFileDialog, Config.LogFilePath);
                            if (readItems != null && readItems.Count > 0)
                            {
                                items.AddRange(readItems);
                            }
                        }

                        if (result == "hideout")
                        {
                            var readItemsHideout = reader.ReadJson<List<Item>>(
                            file, loger, fileStream, "ERROR",
                                openFileDialog, Config.LogFilePath);
                            if (readItemsHideout != null && readItemsHideout.Count > 0)
                            {
                                itemsHideout.AddRange(readItemsHideout);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    loger.Log(fileStream, $"Ошибка при обработке файла {file}: {ex.Message}", "ERROR");
                    openFileDialog.OpenLogDirectory(Config.LogFilePath);
                    Environment.Exit(1);
                }
            }

            return Tuple.Create(items, itemsHideout);
        }
    }
}