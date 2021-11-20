using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace FileExplorer
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static FileOperations.PasteMode CurrentPasteMode { get; private set; }

        private readonly BitmapImage _disabledPasteImage;
        private readonly BitmapImage _enabledPasteImage;

        private readonly ListBox[] _listBoxes;
        private readonly TextBox[] _paths;
        private readonly Button[] _goBtns, CutBtns, CopyBtns, PasteBtns, RenameBtns, DelBtns;

        private readonly string[] _currentLocations;

        private bool _isControlPressed;
        private int _currentSideIndex;

        private delegate void ButtonCallback(object sender, RoutedEventArgs e);
        private int GetSideIndex<T>(T obj, T[] array) => Array.IndexOf<T>(array, obj);

        public MainWindow()
        {
            InitializeComponent();

            _disabledPasteImage = new BitmapImage(new Uri("Resources/paste_nobg_disabled.png", UriKind.Relative));
            _enabledPasteImage = new BitmapImage(new Uri("Resources/paste_nobg.png", UriKind.Relative));

            _listBoxes = new ListBox[] { List1, List2 };
            _paths = new TextBox[] { Path1, Path2 };
            _goBtns = new Button[] { Go1, Go2 };
            CutBtns = new Button[] { Cut1, Cut2 };
            CopyBtns = new Button[] { Copy1, Copy2 };
            PasteBtns = new Button[] { Paste1, Paste2 };
            RenameBtns = new Button[] { Rename1, Rename2 };
            DelBtns = new Button[] { Delete1, Delete2 };

            _currentLocations = Enumerable.Repeat<string>(Path.GetFullPath(Environment.GetEnvironmentVariable("userprofile")), 2).ToArray<string>();

            _paths[0].Text = _currentLocations[0];
            _paths[1].Text = _currentLocations[1];

            UpdateListBox(List1, _currentLocations[0]);
            UpdateListBox(List2, _currentLocations[1]);
        }

        private void ChangePasteButtons(bool enable)
        {
            Paste1.IsEnabled = enable;
            Paste2.IsEnabled = enable;
            BitmapImage source = enable ? _enabledPasteImage : _disabledPasteImage;
            Paste1_Icon.Source = source;
            Paste2_Icon.Source = source;
        }

        private void ChangeDirectory(string path, int index)
        {
            if (!FileOperations.IsValidPath(path))
                return;
            if (path.EndsWith(@":\.."))
            {
                _currentLocations[index] = "\\";
                UpdateListBoxRoot(_listBoxes[index]);
                _paths[index].Text = "";
                return;
            }
            if (Directory.Exists(Path.GetFullPath(path)))
            {
                _currentLocations[index] = Path.GetFullPath(path);
                UpdateListBox(_listBoxes[index], Path.GetFullPath(path));
                _paths[index].Text = Path.GetFullPath(path);
            }
        }

        private void UpdateListBox(ListBox lb, string path)
        {
            try
            {
                lb.Items.Clear();
                string[] directories = Directory.GetDirectories(path);
                string[] files = Directory.GetFiles(path);
                for (int i = 0; i <= directories.Length; i++)
                {
                    ListBoxItem lbItem;
                    lbItem = i == 0 ? new ListBoxItem { Content = "..", Foreground = Brushes.Blue } :
                        new ListBoxItem { Content = Path.GetFileName(directories[i - 1]), Foreground = Brushes.Blue };
                    lbItem.PreviewMouseDoubleClick += LbItem_DoubleClick;
                    lbItem.PreviewKeyDown += LbItem_KeyboardShortcut;
                    lb.Items.Add(lbItem);
                }
                for (int i = 0; i < files.Length; i++)
                {
                    var lbItem = new ListBoxItem { Content = Path.GetFileName(files[i]), Foreground = Brushes.Black };
                    lbItem.PreviewMouseDoubleClick += LbItem_DoubleClick;
                    lbItem.PreviewKeyDown += LbItem_KeyboardShortcut;
                    lb.Items.Add(lbItem);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message,"File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateListBoxRoot(ListBox lb)
        {
            try
            {
                lb.Items.Clear();
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in allDrives)
                {
                    ListBoxItem lbItem = new ListBoxItem { Content = drive.Name, Foreground = Brushes.Blue };
                    lbItem.PreviewMouseDoubleClick += LbItem_DoubleClick;
                    lb.Items.Add(lbItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Ctrl_Down(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                _isControlPressed = true;
        }
        private void Ctrl_Up(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                _isControlPressed = false;
        }

        private void LbItem_KeyboardShortcut(object sender, KeyEventArgs e)
        {
            ButtonCallback shortcutFunction;
            var lbItem = sender as ListBoxItem;
            var listbox = GetParentOfType<ListBox>(lbItem);
            object operationButton = GetButtonFromKey(e.Key, listbox, out shortcutFunction);

            if (shortcutFunction != null && (_isControlPressed || e.Key == Key.Delete))
                shortcutFunction(operationButton, e);
        }

        private object GetButtonFromKey(Key key, ListBox side, out ButtonCallback callback)
        {
            var sideIndex = GetSideIndex<ListBox>(side, _listBoxes);
            if (sideIndex == -1)
            {
                callback = null;
                return null;
            }    
            switch(key)
            {
                case Key.C:
                    callback = Copy_Click;
                    return sideIndex == 0 ? Copy1 : Copy2;
                case Key.X:
                    callback = Cut_Click;
                    return sideIndex == 0 ? Cut1 : Cut2;
                case Key.V:
                    callback = Paste_Click;
                    return sideIndex == 0 ? Paste2 : Paste1;
                case Key.Delete:
                    callback = Delete_Click;
                    return sideIndex == 0 ? Delete1 : Delete2;
                default:
                    callback = null;
                    return callback;
            }    
        }

        private void Path_TextChanged(object sender, KeyEventArgs e)
        {
            Go_Click(_goBtns[Array.IndexOf<TextBox>(_paths, (TextBox)sender)], e);
        }

        private void LbItem_DoubleClick(object sender, MouseEventArgs e)
        {
            var lbItem = sender as ListBoxItem;
            var listbox = GetParentOfType<ListBox>(lbItem);
            var sideIndex = GetSideIndex<ListBox>(listbox, _listBoxes);
            if (lbItem.Foreground.Equals(Brushes.Blue))
                ChangeDirectory(Path.Combine(_currentLocations[sideIndex], lbItem.Content.ToString()), sideIndex);
            else
                FileOperations.ExecuteFile(Path.Combine(_currentLocations[sideIndex], lbItem.Content.ToString()));
        }

        private void Go_Click(object sender, RoutedEventArgs e)
        {
            var sideIndex = GetSideIndex<Button>((Button)sender, _goBtns);
            if (FileOperations.IsAbsolutePath(_paths[sideIndex].Text))
                ChangeDirectory(_paths[sideIndex].Text, sideIndex);
            else
                ChangeDirectory(Path.Combine(_currentLocations[sideIndex], _paths[sideIndex].Text), sideIndex);
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            var sideIndex = GetSideIndex<Button>((Button)sender, CutBtns);
            var selectedItem = _listBoxes[sideIndex].SelectedItem as ListBoxItem;
            if (selectedItem != null)
                FileOperations.MoveFiles(new string[] { Path.Combine(_currentLocations[sideIndex], selectedItem.Content.ToString()) });
            ChangePasteButtons(true);
            CurrentPasteMode = FileOperations.PasteMode.CUT;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var sideIndex = GetSideIndex<Button>((Button)sender, CopyBtns);
            var selectedItem = _listBoxes[sideIndex].SelectedItem as ListBoxItem;
            if (selectedItem != null)
                FileOperations.CopyFiles(new string[] { Path.Combine(_currentLocations[sideIndex], selectedItem.Content.ToString()) });
            ChangePasteButtons(true);
            CurrentPasteMode = FileOperations.PasteMode.COPY;
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            var sideIndex = GetSideIndex<Button>((Button)sender, RenameBtns);
            var selectedItem = _listBoxes[sideIndex].SelectedItem as ListBoxItem;
            RenameBox.IsEnabled = true;
            RenameBox.Focus();
            if (selectedItem != null)
                RenameBox.Text = selectedItem.Content.ToString();
            RenameBox.SelectAll();
            _currentSideIndex = sideIndex;
        }

        private void RenameBox_TextChanged(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Window_MouseDown(sender, null);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (RenameBox.IsEnabled)
            {
                var selectedItem = _listBoxes[_currentSideIndex].SelectedItem as ListBoxItem;
                if (selectedItem != null)
                    FileOperations.RenameFile(Path.Combine(_currentLocations[_currentSideIndex], selectedItem.Content.ToString()), RenameBox.Text);
                RenameBox.Text = "";
                RenameBox.IsEnabled = false;
                UpdateListBox(_listBoxes[_currentSideIndex], _currentLocations[_currentSideIndex]);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var sideIndex = GetSideIndex<Button>((Button)sender, DelBtns);
            var selectedItem = _listBoxes[sideIndex].SelectedItem as ListBoxItem;
            if (selectedItem != null)
                FileOperations.DeleteFile(Path.Combine(_currentLocations[sideIndex], selectedItem.Content.ToString()));
            UpdateListBox(_listBoxes[sideIndex], _currentLocations[sideIndex]);
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            var sideIndex = GetSideIndex<Button>((Button)sender, PasteBtns);
            FileOperations.PasteFile(CurrentPasteMode, _currentLocations[sideIndex]);
            if (!Clipboard.ContainsFileDropList() || CurrentPasteMode == FileOperations.PasteMode.CUT)
                ChangePasteButtons(false);
            UpdateListBox(_listBoxes[0], _currentLocations[0]);
            UpdateListBox(_listBoxes[1], _currentLocations[1]);
        }

        private T GetParentOfType<T>(DependencyObject element) where T : DependencyObject
        {
            Type type = typeof(T);
            if (element == null) return null;
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (parent == null && ((FrameworkElement)element).Parent is DependencyObject)
                parent = ((FrameworkElement)element).Parent;
            if (parent == null) return null;
            else if (parent.GetType() == type || parent.GetType().IsSubclassOf(type))
                return parent as T;
            return GetParentOfType<T>(parent);
        }

        private void List_Drop(object sender, DragEventArgs e)
        {
            var srcIndex = sender == List1 ? 1 : 0;
            var dstIndex = sender == List1 ? 0 : 1;
            ListBoxItem item = e.Data.GetData(DataFormats.FileDrop) as ListBoxItem;
            string srcPath = Path.Combine(_currentLocations[srcIndex], item.Content.ToString());
            string dstPath = Path.Combine(_currentLocations[dstIndex], item.Content.ToString());
            if (item != null)
            {
                try
                {
                    if (File.Exists(srcPath))
                        File.Move(srcPath, dstPath);
                    else if (Directory.Exists(srcPath))
                    {
                        FileOperations.CopyDirectory(srcPath, Path.GetDirectoryName(dstPath));
                        Directory.Delete(srcPath, true);
                    }
                    UpdateListBox(List1, _currentLocations[0]);
                    UpdateListBox(List2, _currentLocations[1]);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void List_MouseMove(object sender, MouseEventArgs e)
        {
            Point mPos = e.GetPosition(null);
            if(e.LeftButton == MouseButtonState.Pressed && Math.Abs(mPos.X) > SystemParameters.MinimumHorizontalDragDistance && Math.Abs(mPos.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var srcListIndex = GetSideIndex<ListBox>((ListBox)sender, _listBoxes);
                try
                {
                    ListBoxItem selectedItem = _listBoxes[srcListIndex].SelectedItem as ListBoxItem;
                    if(selectedItem != null)
                        DragDrop.DoDragDrop(this, new DataObject(DataFormats.FileDrop, selectedItem), DragDropEffects.Copy);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}
