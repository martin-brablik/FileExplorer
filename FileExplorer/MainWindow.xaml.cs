using System;
using System.Linq;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        private readonly Button[] _goBtns, _cutBtns, _copyBtns, _pasteBtns, _renameBtns, _delBtns;

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
            _cutBtns = new Button[] { Cut1, Cut2 };
            _copyBtns = new Button[] { Copy1, Copy2 };
            _pasteBtns = new Button[] { Paste1, Paste2 };
            _renameBtns = new Button[] { Rename1, Rename2 };
            _delBtns = new Button[] { Delete1, Delete2 };

            _currentLocations = Enumerable.Repeat<string>(Path.GetFullPath(Environment.GetEnvironmentVariable("userprofile")), 2).ToArray<string>();

            _paths[0].Text = _currentLocations[0];
            _paths[1].Text = _currentLocations[1];

            UpdateListBox(_listBoxes[0], _currentLocations[0]);
            UpdateListBox(_listBoxes[1], _currentLocations[1]);
        }

        private void ChangePasteButtons(bool enable)
        {
            _pasteBtns[0].IsEnabled = enable;
            _pasteBtns[1].IsEnabled = enable;
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
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                path += Path.DirectorySeparatorChar;
            try
            {
                if (Directory.Exists(Path.GetFullPath(path))) 
                    UpdateListBox(_listBoxes[index], Path.GetFullPath(path));
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
                _paths[index].Text = _currentLocations[index];
                return;
            }
            _currentLocations[index] = Path.GetFullPath(path);
            _paths[index].Text = Path.GetFullPath(path);
        }

        private void UpdateListBox(ListBox lb, string path)
        {
            string[] directories = Array.Empty<string>();
            string[] files = Array.Empty<string>();
            try
            {
                directories = Directory.GetDirectories(path);
                files = Directory.GetFiles(path);
            }
            catch(Exception ex)
            {
                throw ex;
            }
            lb.Items.Clear();
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
                ListBoxItem lbItem = new ListBoxItem { Content = Path.GetFileName(files[i]), Foreground = Brushes.Black };
                lbItem.PreviewMouseDoubleClick += LbItem_DoubleClick;
                lbItem.PreviewKeyDown += LbItem_KeyboardShortcut;
                lb.Items.Add(lbItem);
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
            Button operationButton = GetButtonFromKey(e.Key, listbox, out shortcutFunction);

            if (shortcutFunction != null && (_isControlPressed || e.Key == Key.Delete))
                shortcutFunction(operationButton, e);
        }

        private Button GetButtonFromKey(Key key, ListBox side, out ButtonCallback callback)
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
                    return sideIndex == 0 ? _copyBtns[0] : _copyBtns[1];
                case Key.X:
                    callback = Cut_Click;
                    return sideIndex == 0 ? _cutBtns[0] : _cutBtns[1];
                case Key.V:
                    callback = Paste_Click;
                    return sideIndex == 0 ? _pasteBtns[1] : _pasteBtns[0];
                case Key.Delete:
                    callback = Delete_Click;
                    return sideIndex == 0 ? _delBtns[0] : _delBtns[1];
                default:
                    callback = null;
                    return null;
            }    
        }

        private void Path_TextChanged(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
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
            var sideIndex = GetSideIndex<Button>((Button)sender, _cutBtns);
            var selectedItem = _listBoxes[sideIndex].SelectedItem as ListBoxItem;
            if (selectedItem != null)
                FileOperations.MoveFiles(new string[] { Path.Combine(_currentLocations[sideIndex], selectedItem.Content.ToString()) });
            ChangePasteButtons(true);
            CurrentPasteMode = FileOperations.PasteMode.CUT;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var sideIndex = GetSideIndex<Button>((Button)sender, _copyBtns);
            var selectedItem = _listBoxes[sideIndex].SelectedItem as ListBoxItem;
            if (selectedItem != null)
                FileOperations.CopyFiles(new string[]  { Path.Combine(_currentLocations[sideIndex], selectedItem.Content.ToString()) });
            ChangePasteButtons(true);
            CurrentPasteMode = FileOperations.PasteMode.COPY;
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            var sideIndex = GetSideIndex<Button>((Button)sender, _renameBtns);
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
            var sideIndex = GetSideIndex<Button>((Button)sender, _delBtns);
            var selectedItem = _listBoxes[sideIndex].SelectedItem as ListBoxItem;
            if (selectedItem != null)
                FileOperations.DeleteFile(Path.Combine(_currentLocations[sideIndex], selectedItem.Content.ToString()));
            UpdateListBox(_listBoxes[sideIndex], _currentLocations[sideIndex]);
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            var sideIndex = GetSideIndex<Button>((Button)sender, _pasteBtns);
            FileOperations.PasteFile(CurrentPasteMode, _currentLocations[sideIndex]);
            if (!Clipboard.ContainsFileDropList() || CurrentPasteMode == FileOperations.PasteMode.CUT)
            {
                ChangePasteButtons(false);
                Clipboard.Clear();
            }  
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
            var lbItem = e.Data.GetData(DataFormats.FileDrop) as ListBoxItem;
            if(lbItem == null) return;
            string srcPath = Path.Combine(_currentLocations[srcIndex], lbItem.Content.ToString());
            string dstPath = Path.Combine(_currentLocations[dstIndex], lbItem.Content.ToString());
            if (srcPath.Equals(dstPath)) return;
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        DragDrop.DoDragDrop((ListBox)sender, new DataObject(DataFormats.FileDrop, selectedItem), DragDropEffects.Copy);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}
