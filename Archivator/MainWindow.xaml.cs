using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

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
    public class ListItem
    {
        // полное имя файла. Иначе говоря абсолютный путь. Изменять нельзя.
        public string FullName { get; }
        // Обычное имя файла. Отображается в столбце "Имя"
        public string Name { get; set; }
        // Иконка изображения
        public ImageSource Image { get; set; }
        // Дата последнего изменения
        public string ChangeData { get; set; }
        // Расширение файла
        public string Type { get; set; }
        // Размер файла
        public string Size { get; set; }

        public ListItem(FileInfo Inf, Icon Ico)
        {
            // Конструктор принимающий на вход класс FileInfo из пространства имён System.IO.
            // Из него получаем абсолютный путь.
            FullName = Inf.FullName;
      
            // Имя файла с расширением
            Name = Inf.Name;
            // Дату последнего изменения
            ChangeData = Inf.LastAccessTimeUtc.ToString();
            // Расширение
            Type = Inf.Extension;

            // И размер файла
            // Но т.к он даётся в битах, нужно немного преобразовать строку.
            // Недоделано. Надо размеры до ГБ сделать.
            if(Inf.Length >= 1024)
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
        // метод получения папки с загрузками
        // Способ не идеальный так как берёт первый попавшийся диск
        // Отсортированы они по алфавиту, поэтому если папка с загрузками пользователя
        // находится на диске D, но при этом есть диск A,B,C, то поведение непредсказуемо.
        // Вообще exception может выбить. Надо бы обработать, но мне лень
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
            // 
            return BBld.ToString();
        }

        // объявляю здесь потому что 
        ObservableCollection<ListItem> Entry;

        public MainWindow()
        {
            InitializeComponent();
            /* Test List
            List<ListItem> LSD = new List<ListItem>();
            LSD.Add(new ListItem(new FileInfo(@"C:\Users\mixap\Downloads\witch_hunter_0.3.21-pc_0.zip"), System.Drawing.Icon.ExtractAssociatedIcon(@"C:\Users\mixap\Downloads\witch_hunter_0.3.21-pc_0.zip")));
            LSD.Add(new ListItem(new FileInfo(@"C:\Users\mixap\Downloads\witch_hunter_0.3.21-pc_0.zip"), System.Drawing.Icon.ExtractAssociatedIcon(@"C:\Users\mixap\Downloads\witch_hunter_0.3.21-pc_0.zip")));
            Explorer.ItemsSource = LSD;
            */
            // Первое заполнение ListView происходит при инициализации MainWindow (запуске приложения)
            // ObservableCollection это тот же List только реализующий интерфейсы INotifyCollectionChanged, INotifyPropertyChanged .
            // WPF использует их ( интерфейсы ) для обновления значений в ListView

            Entry = new ObservableCollection<ListItem>();
            
            // получаем список абсолютных путей файлов в папке "Загрузки"
            var Dirs = Directory.GetFiles(GetDownloadDir());
            // для каждого пути
            foreach (var path in Dirs)
            {
                // добавляем новый элемент в список
                Entry.Add(new ListItem(new FileInfo(path), System.Drawing.Icon.ExtractAssociatedIcon(path)));
            }
                // Выводим файлы в ListView
            Explorer.ItemsSource = Entry;
        }

        // Кнопки управления (которые справа сверху) Объяснять что они делают не надо
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


        // TextBox (который справа от сдоровой стрелки) должен принимать на вход путь к папке или к файлу
        // и либо вывести список файлов в папке либо архивировать ( возможно отдельное окно сделать под это надо)
        // НЕ ДОДЕЛАНО. Exceptions | ввод вместе с файлом
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Когда TextBox находится в фокусе, мы ждём пока нажмут Enter.
            if(e.Key == Key.Enter)
            {
                // здесь обновляем treeView
                Entry.Clear();
                
                // Берём текст из TextBox-а и опять составляем лист всех файлов
                var Dirs = Directory.GetFiles(Navigator.Text);
                foreach (var path in Dirs)
                {
                    Entry.Add(new ListItem(new FileInfo(path), System.Drawing.Icon.ExtractAssociatedIcon(path)));
                }
                Explorer.ItemsSource = Entry;
            }
        }

        // Ивент вызывается когда DragAndDrop-ают какой-либо объект
        //TODO: застваить Drop работать постоянно
        private void Explorer_Drop(object sender, DragEventArgs e)
        {
            // Абсолютный путь к файлу или к папке ( директории )
            string[] buffer = e.Data.GetData("FileName") as string[];
        }

        // Double click по файлу из ListView. Должен вызывать начало архивации
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListItem Src = (e.Source as ListViewItem).Content as ListItem;
            // Src - это вся необходимая о файле инфа
            // Можно открыть файл на чтение через 
            // var FileStr = File.OpenRead(Src.FullName);
        }

        //нажатие на большую стрелку. По образу и подобию WinRar должен открывать Explorer в корневой папке.
        //Реализуется через Directory. Там есть готовый метод
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // я менял его стиль, надо было проверить
            MessageBox.Show("Работает, успокойся");
        }

        // не знаю насколько этот способ хорош но...
        private void ListViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            /* Это ломает Binding
            var ob = sender as ListViewItem;
            if(mem == null)
            {
                mem = ob;
            }
            else
            {
                mem.Background = new SolidColorBrush(Colors.Transparent);
                mem = ob;
            }
            ob.Background = Resources["FadedBlue"] as System.Windows.Media.Brush;
            */
        }
    }
}
