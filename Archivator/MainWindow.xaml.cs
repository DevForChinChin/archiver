using System;
using System.Text;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using Microsoft.WindowsAPICodePack.Shell;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Archivator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    // А название Archiver или Archivator чё бы не ?
    // Я всё в ReadMe.md хотел описать, но похоже мой загрузчик в гит сломался

    class Binary
    {
        public static string ToBinConvert(char arg)
        {
            string ret;
            ret = Convert.ToString(arg, 2); //обычная функция получения бинарного кода
            while (ret.Length < 8)
            {
                ret = '0' + ret; //дополнение длины до 8
            }
            return ret;

        }
        public static string ToBinConvert(string arg)
        {
            string ret = "";
            for (byte i = 0; i < arg.Length; i++)
            {
                ret += ToBinConvert(arg[i]);
            }
            return ret;
        }

        public static char FromBinConvert(string arg)
        {
            int bt = 0;
            for (byte i = 0; i < 8; i++)
            {
                if (arg[i] == '1')
                {
                    bt += (int)(Math.Pow(2, 7 - i));
                }
            }
            return (char)(bt);
        }
    }


    // расширение для Icon. Перевод в ImageSource. Нужно для отображения иконок в столбце "Имя"
    public static class ExtensionDefine
    {
        /// <summary>
        /// Конвертирует icon в ImageSource
        /// </summary>
        /// <param name="icon">Icon, которую нужно конвертировать</param>
        /// <returns></returns>
        public static ImageSource ToImageSource(this Icon icon)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
           icon.ToBitmap().GetHbitmap(),
           IntPtr.Zero,
           Int32Rect.Empty,
           BitmapSizeOptions.FromEmptyOptions());

        }
    }

    // элемент ListView
    /// <summary>
    /// Класс, представляющий элемент ListView
    /// </summary>
    public class ListItem
    {
        // Обычное имя файла. Отображается в столбце "Имя"
        /// <summary>
        ///Имя файла с расширением
        /// </summary>
        public string Name { get; set; }
        // Иконка изображения
        /// <summary>
        ///Иконка, ассоциирующаяся с файлом
        /// </summary>
        public ImageSource Image { get; set; }
        /// <summary>
        ///Дата последнего изменения
        /// </summary>
        public DateTime ChangeData { get; set; }
        /// <summary>
        ///Расширение файла
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        ///Размер файла
        /// </summary>
        public string Size { get; set; }

        // Конструктор, принимающий на вход класс FileInfo из пространства имён System.IO.
        #region Устаревшие конструкторы
        /// <summary>
        /// Конструктор, принимающий информацию о файле и Иконку файла
        /// </summary>
        /// <param name="Inf">информация</param>
        /// <param name="Ico">Иконка файла</param>
        public ListItem(FileInfo Inf, Icon Ico)
        {
            // Имя файла с расширением
            Name = Inf.Name;
            // Дату последнего изменения
            ChangeData = Inf.LastWriteTime;
            // Расширение
            Type = Inf.Extension;

            // И размер файла
            // Но т.к он даётся в битах, нужно немного преобразовать строку.
            // Недоделано. Надо размеры до ГБ сделать.
            if (Inf.Length >= 1024)
            {
                // в одном килобайте 1024 байт.
                Size = (Inf.Length / 1024).ToString() + " KB";
            }
            else
            {
                //Собственно если размер меньше 1024 байт
                Size = Inf.Length.ToString() + " B";
            }

            // Ну и из класса System.Drawing берём класс Image, отвечающий за Иконку приложения
            Image = Ico.ToImageSource();
        }

        /// <summary>
        /// Конструктор, принимающий на вход информацию о файле, который берёт ассоциированную с этим файлом иконку
        /// </summary>
        /// <param name="info"></param>
        public ListItem(FileInfo info)
        {

            // Имя файла с расширением
            Name = info.Name;
            // Дату последнего изменения
            ChangeData = info.LastWriteTime;
            // Расширение
            Type = info.Extension;

            // И размер файла
            // Но т.к он даётся в битах, нужно немного преобразовать строку.
            // Недоделано. Надо размеры до ГБ сделать.
            if (info.Length >= 1024)
            {
                // в одном килобайте 1024 байт.
                Size = (info.Length / 1024).ToString() + " KB";
            }
            else
            {
                //Собственно если размер меньше 1024 байт
                Size = info.Length.ToString() + " B";
            }

            // AssociatedIcon по полному имени файла, конвертируется в ImageSource
            Image = Icon.ExtractAssociatedIcon(info.FullName).ToImageSource();
        }
        #endregion

        /// <summary>
        /// ListItem из экземпляра ShellObject
        /// </summary>
        /// <param name="info"></param>
        public ListItem(ShellObject info)
        {
            Name = info.Name;

            Image = info.Thumbnail.SmallBitmapSource;

            // проверить ChangeData на null
            ChangeData = info.Properties.System.DateModified.Value.Value;

            Type = info.Properties.System.ItemType.Value;

            var inf = info.Properties.System.Size.Value;

            if (inf == null) //данных нет
            {
                Size = inf.ToString();
            }
            else if (inf >= 1024*1024*1024) //гигабайт 
            {
                Size = (inf.Value/(1024*1024*1024)).ToString() + " GB";
            }
            else if (inf >= 1024) //пропускаем мегабайты для наглядности и проверяем килобайт
            {
                // в одном килобайте 1024 байт.
                Size = (inf / 1024).ToString() + " KB";
            }
            else //помянем обладателей файлы в терабайты. Считаем что все остальное считать в байтах
            {
                Size = inf.ToString() + " B";
            }
            // Size = info.Properties.System.Size.Value.ToString();
        }

        public override string ToString()
        {
            //↓         Плохо сформулированный бред       ↓\\
            // GridViewColumn имеет свойство DisplayMemberBinding, которая неведомым ( пока не читал ) колдунством
            // с помощью Binding даже без подключения пространства имён в видимость xaml сопоставляет свойства класса
            // из коллекции ListView.ItemSource колонну.
            // если что-то пойдёт не так, то значение в колонне будет равно .ToString() от класса
            // Правда это уже не актуально из-за Icon.
            return "BINDING ERROR";
        }

    }

    public partial class MainWindow : Window
    {
        ObservableCollection<ListItem> Entry;
        string CurrentDirectory;

        //тест для многопоточности
        Dispatcher Test;
        #region Вспомогательные функции

        /// <summary>
        /// Возвращает путь к папке "Загрузки" первого по алфавиту диска.
        /// Если не найдена, то возвращает первый по алфавиту логический диск
        /// </summary>
        /// <returns></returns>
        [Obsolete("Используй ShellObject.FromParsingName(SLSID key)", false)]
        private string GetDownloadDir()
        {
            // Так как относительно много возьни со строками используем StringBuilder
            StringBuilder BBld = new StringBuilder();

            // Получаем список имеющихся жд и выбираем первый из них.
            BBld.Append(Directory.GetLogicalDrives()[0]);
            // Папка с загрузками находится по пути C:\Users\**USERNAME**\Downloads
            BBld.Append(@"Users\");
            BBld.Append(Environment.UserName);
            BBld.Append(@"\Downloads");

            if (Directory.Exists(BBld.ToString()))
            {
                return BBld.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Обновляет ListView
        /// </summary>
        /// <param name="Path">Полный путь к директории, которую отображает Explorer (ListItem)</param>
        private void UpdateListView(string Path)
        {
            /* И ОПЯТЬ КОММЕНТАРИИ НЕ МОГУ СВЕРНУТЬ
             Проверяет есть ли вообще такая директория
             Способ не идеален. Directory.Exists не поддерживает пути которые являются ссылками или shell команды. Пример: C:\Users\mixap\ Папка загрузки, документы и так далее
             Костыль с shell
             */
            System.Diagnostics.Stopwatch TestWatch = new System.Diagnostics.Stopwatch();
            TestWatch.Start();
            if (Directory.Exists(Path) || Path.Contains("shell:"))
            {
                // значение не попадает в ожидаемый диапозон
                try
                {
                    var TargetFolder = (ShellFolder)ShellObject.FromParsingName(Path);
                    CurrentDirectory = TargetFolder.ParsingName;
                    Navigator.Text = CurrentDirectory;
                    Entry.Clear();
                    foreach (var item in TargetFolder)
                    {
                        // работай ок ?
                        // через Dispatcher.InvokeAsync асинхронно добавляем элементы к ObservableCollection
                        Test.InvokeAsync(() =>
                        {
                            Entry.Add(new ListItem(item));
                        });
                    }
                    // вот Dispose для всей папки. Разницы между вызовом Dispose для каждого объекта и только для Target Folder ВРОДЕ нет.
                    TargetFolder.Dispose();
                }
                catch (ShellException e)
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка \n Код ошибки:{e.HResult}  Подробности:{e.Message}");
                }
            }
            else
            {   // сообщение об ошибке
                //Надо поменять стиль MessageBox
                MessageBox.Show("Директория не найдена");
            }
            TestWatch.Stop();
            System.Diagnostics.Trace.WriteLine($"Всего времени прошло : {TestWatch.ElapsedMilliseconds} мс");
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Entry = new ObservableCollection<ListItem>();

            //UpdateListView(@"shell:Downloads"); // не используем потому что нужно инициировать CurrentDirectory
            var Dir = (ShellFolder)ShellObject.FromParsingName("shell:Downloads");
            CurrentDirectory = Dir.ParsingName;
            Navigator.Text = CurrentDirectory;
            foreach (var file in Dir)
            {
                Entry.Add(new ListItem(file));
            }
            Test = Application.Current.Dispatcher;
            Explorer.ItemsSource = Entry;
        }


        #region кнопки Свернуть, Развернуть на весь экран, Закрыть

        private void FullSizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            { WindowState = WindowState.Maximized; }
            else WindowState = WindowState.Normal;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        #endregion

        // TextBox (который справа от здоровой стрелки, далее Navigator) должен принимать на вход путь к папке или к файлу
        // и либо вывести список файлов в папке либо архивировать ( возможно отдельное окно сделать под это надо)
        // НЕ ДОДЕЛАНО. Exceptions | ввод вместе с файлом
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {

            #region Комменты не сворачиваются ПОЧЕМУ-ТО. Не удаляю потому-что вся эта дичь с SLSID ключами всё ещё под вопросом
            /* 
             * На всякий случай не удаляю. Я думаю надо вообще 2 версии сделать
             * Одна будет через SLSID ключи, другая "По старинке" и проблему иконок решить путём "Не можем настоящую поставим дефолтную"
            // Когда TextBox находится в фокусе, мы ждём пока нажмут Enter.
            if (e.Key == Key.Enter)
            {
                // Берём текст из TextBox-а и опять составляем лист всех файлов
                if(Directory.Exists(Navigator.Text))
                {
                    // здесь обновляем treeView. Нужно добавить поддержку директорий. Короче конструктор с DirectoryInfo 
                    Entry.Clear();
                    var Dirs = Directory.GetFiles(Navigator.Text);
                    foreach (var path in Dirs)
                    {
                        Entry.Add(new ListItem(new FileInfo(path)));
                    }
                    Explorer.ItemsSource = Entry;
                }
                else
                {
                    MessageBox.Show($"Директория \"{Navigator.Text}\" не найдена или не существует");
                }
            }
            */
            #endregion

            // не уверен насчёт этого метода
            if (e.Key == Key.Enter)
            {
                // Исключение надо ловить
                UpdateListView(Navigator.Text);
            }
        }

        // Ивент вызывается когда DragAndDrop-ают какой-либо объект
        //TODO: застваить Drop работать постоянно
        private void Explorer_Drop(object sender, DragEventArgs e)
        {
            // Абсолютный путь к файлу или к папке ( директории )
            // почему оно имеет тип string[] ???
            string[] buffer = e.Data.GetData("FileName") as string[];
        }

        // Double click по файлу из ListView. Должен вызывать начало архивации
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListItem Src = (e.Source as ListViewItem).Content as ListItem;
            /* "адрес" логического диска выглядит так - "C:\" 
               и при этом чтобы получить путь до объекта нам нужно прибавить к его имени - "\"
               получается строка "C:\\NAME" которая проходит проверку Directory.Exists,
               но не проходит в ShellObject.FromParsingName
            */
            if (CurrentDirectory[CurrentDirectory.Length - 1] == '\\')
            {
                UpdateListView(CurrentDirectory + Src.Name);
            }
            else
            {
                UpdateListView(CurrentDirectory + @"\" + Src.Name);
            }
            // Src - это вся необходимая о файле инфа
            // Можно открыть файл на чтение через 
            // var FileStr = File.OpenRead(Src.FullName);
        }

        //нажатие на большую стрелку. По образу и подобию WinRar должен открывать Explorer в корневой папке.
        //Реализуется через Directory. Там есть готовый метод
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateListView(Directory.GetParent(CurrentDirectory).FullName);
        }

    }
}