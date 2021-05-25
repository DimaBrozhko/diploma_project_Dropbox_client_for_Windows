using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Security.Cryptography;


using Dropbox.Api;
using Dropbox.Api.Files;
using System.Text;
using System.Data.SQLite;

namespace my_dropbox_app
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //NameScope ns = new NameScope();

        #region ************************************************** Global variables
        DropboxClient client;
        public string path_to_local_download_folder = @"D:\test\";

        List<string> parts_of_path = new List<string>();
        int current_index_of_path = -1;

        List<Files_info> Files_info_list = new List<Files_info>();

        public string new_folder_name = "";

        public int reserve_number = 0;

        List<string> folders_to_upload = new List<string>();
        List<string> files_to_upload = new List<string>();

        int uploading_file_count = 0;
        public window_login.User current_user;

        SQLiteConnection sqlite_connection;
        SQLiteCommand sqlite_command;
        #endregion
        public MainWindow(window_login.User _user)
        {
            InitializeComponent();
            current_user = _user;
            if (!File.Exists("users_settings/users_keys.db"))
            {
                SQLiteConnection.CreateFile("users_settings/users_keys.db");
                
            }
            sqlite_connection = new SQLiteConnection(@"Data Source=users_settings/users_keys.db; Version=3");
            string create_tabe = "CREATE TABLE IF NOT EXISTS keys (" +
                                 "id_key INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL," +
                                 "login VARCHAR(50)," +
                                 "local_path VARCHAR(150)," +
                                 "server_path VARCHAR(150)," +
                                 "key BLOB," +
                                 "iv BLOB)";
            sqlite_command = new SQLiteCommand(create_tabe, sqlite_connection);
            sqlite_connection.Open();
            sqlite_command.ExecuteNonQuery();
            sqlite_connection.Close();
        }



        #region ************************************************** Classes

        //public class Users_keys
        //{
        //    public string login { get; set; }
        //    public string password { get; set; }
        //    public string file_path { get; set; }
        //    public byte[] key { get; set; }
        //    public byte[] iv { get; set; }

        //    public Users_keys(string _login, string _password, string _file_path, byte[] _key, byte[] _iv)
        //    {
        //        login = _login;
        //        password = _password;
        //        file_path = _file_path;
        //        key = _key;
        //        iv = _iv;
        //    }
        //}

        public class Files_info
        {
            public string file_name { get; set; }
            public string full_path { get; set; }
            public DateTime client_modified { get; set; }
            //public DateTime server_modified { get; set; }
            public ulong file_size { get; set; }
            public string file_size_text { get; set; }
            public string is_folder { get; set; }
            public Files_info(string _file_name, string _full_path, DateTime _client_modified,/* DateTime _server_modified,*/ ulong _file_size)
            {
                file_name = _file_name;
                full_path = _full_path;
                //DateTime date = new DateTime(_client_modified.Year, _client_modified.Month, _client_modified.Day, _client_modified.Hour - 3, _client_modified.Minute, _client_modified.Second);
                client_modified = _client_modified.AddHours(3);
                //client_modified.Hour -= 
                //server_modified = _server_modified;
                is_folder = "";
                file_size = _file_size;
                file_size_text = calculate_size(file_size);
            }

            public Files_info(string _file_name, string _full_path)
            {
                file_name = _file_name;
                full_path = _full_path;
                is_folder = "Папка";
            }

            public string calculate_size(ulong file_size)
            {
                int kb = 1024;
                int mb = kb * 1024;

                int size = Convert.ToInt32(file_size);

                string size_text;
                // b
                if (size < kb)
                {
                    size_text = $"{size} Б";
                }
                // kb
                else if (size >= kb & size < mb)
                {
                    double result = Math.Round((double)(size / kb));
                    size_text = $"{result} Кб";
                }
                // mb
                else
                {
                    double result = Math.Round((double)(size / mb));
                    size_text = $"{result} Мб";
                }
                return size_text;
            }
        }
        public class Account_info
        {
            public string account_type { get; set; }
            public string account_email { get; set; }
            public string account_email_verified { get; set; }
            public string account_language { get; set; }
            public string account_name { get; set; }
            public Account_info(Dropbox.Api.Users.FullAccount _account)
            {
                account_type = to_define_account_type(_account.AccountType);
                account_email = _account.Email;
                account_email_verified = check_account_email_verification(_account.EmailVerified);
                account_language = _account.Locale.ToUpper();
                account_name = _account.Name.DisplayName;
            }
            public string to_define_account_type(Dropbox.Api.UsersCommon.AccountType _account_type)
            {
                if (_account_type.IsBasic)
                {
                    return "Базовый";
                }
                else if (_account_type.IsBusiness)
                {
                    return "Для бизнеса";
                }
                else if (_account_type.IsPro)
                {
                    return "Профессиональный";
                }
                else return null;
            }
            public string check_account_email_verification(bool verification)
            {
                if (verification)
                {
                    return "Email подтвержден";
                }
                else
                {
                    return "Email не подтвержден";
                }
            }
        }

        //public class User
        //{
        //    public string login { get; set; }
        //    public string password { get; set; }
        //    public string user_token { get; set; }
        //    public string download_folder { get; set; }

        //    public User (string _login, string _password)
        //    {
        //        login = _login;
        //        password = _password;
        //    }
        //}

        #endregion
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            start();
        }

        // user initialization
        public async Task start()
        {
            client = new DropboxClient(current_user.user_token);
            await list_folder(client, "");
            await get_account_info();

        }

        #region ************************************************** Operations with user
        public async Task<Dropbox.Api.Users.FullAccount> get_account_info()
        {
            var account_info = new Account_info(await client.Users.GetCurrentAccountAsync());
            return null;
        }
        #endregion

        private async Task<ListFolderResult> list_folder(DropboxClient client, string path)
        {
            listView1.ItemsSource = null;
            Files_info_list.Clear();
            var list = await client.Files.ListFolderAsync(path);
            foreach (var item in list.Entries.Where(i => i.IsFolder))
            {
                var folder = item.AsFolder;
                Files_info_list.Add(new Files_info(folder.Name, folder.PathDisplay));
            }
            foreach (var item in list.Entries.Where(i => i.IsFile))
            {
                var file = item.AsFile;
                Files_info_list.Add(new Files_info(file.Name, file.PathDisplay, file.ClientModified, /*file.ServerModified,*/ file.Size));
            }
            listView1.ItemsSource = Files_info_list;
            return null;
        }

        public string get_current_path()
        {
            string path = "";
            for (int i = 0; i < current_index_of_path + 1; i++)
            {
                path += "/";
                path += parts_of_path[i];
            }
            return path;
        }

        public async void open_folder(string folder)
        {
            string current_path;
            int cou = parts_of_path.Count;
            for (int i = 0; i < cou - current_index_of_path - 1; i++)
            {
                parts_of_path.RemoveAt(parts_of_path.Count - 1);
            }
            parts_of_path.Add(folder);
            current_index_of_path++;
            current_path = get_current_path();
            display_current_path();
            await list_folder(client, current_path);
        }

        public void display_current_path()
        {
            string current_path = get_current_path();
            textBlock_current_path.Text = $"Текущий путь: {current_path}";
        }
       
        private void listView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listView1.SelectedItem != null)
            {
                if (((Files_info)listView1.SelectedItem).is_folder == "Папка")
                {
                    string next_folder = ((Files_info)listView1.SelectedItem).file_name;
                    open_folder(next_folder);
                }
            }
        }

        #region ************************************************** Folder navigations button
        
        private async void button_backward_Click(object sender, RoutedEventArgs e)
        {
            if (current_index_of_path != -1)
            {
                current_index_of_path--;
                string current_path = get_current_path();
                await list_folder(client, current_path);
                display_current_path();
            }
        }
                   
        private async void button_forward_Click(object sender, RoutedEventArgs e)
        {
            if (current_index_of_path != parts_of_path.Count - 1)
            {
                current_index_of_path++;
                string current_path = get_current_path();
                await list_folder(client, current_path);
                display_current_path();
            }
        }
        #endregion

        #region ************************************************** ListView context menu
        public async void menu_click_download_file(object sender, RoutedEventArgs e)
        {
            string path, file_name;
            if (listView1.SelectedItem != null)
            {
                path = ((Files_info)listView1.SelectedItem).full_path;
                file_name = ((Files_info)listView1.SelectedItem).file_name;
                if (((Files_info)listView1.SelectedItem).is_folder == "Папка")
                {
                    //MessageBox.Show($"{path}\r{path_to_local_download_folder}");
                    await download_folder(file_name, path, path_to_local_download_folder);
                }
                else
                {
                    try
                    {
                        add_progress_bar(file_name);
                        await Task.Run(() => download_file(path, file_name, null));
                    }
                    catch (ArgumentException)
                    {
                        MessageBox.Show("Вы пытаетесь скачать файл который уже загружается.\r" +
                            "Вы не можете загружать одновременно один и тот же файл несколько раз", "Ошибка");
                    }
                }
            }
        }
        public async Task<FileMetadata> download_folder(string _file_name, string _server_path, string _local_path)
        {
            var folder = await client.Files.ListFolderAsync(_server_path);
            string local_path = _local_path + "\\" + _file_name;
            Directory.CreateDirectory(local_path);
            foreach (var file in folder.Entries)
            {
                if (file.IsFolder)
                {
                    await download_folder(file.Name, _server_path + $"/{file.Name}/", local_path);
                }
                else if (file.IsFile)
                {
                    add_progress_bar(file.Name);
                    await Task.Run(() => download_file(file.PathDisplay, file.Name, null));
                }
            }
            return null;
        }
        public async void menu_click_create_folder(object sender, RoutedEventArgs e)
        {
            var window = new window_create_folder_input();
            window.Owner = this;
            window.ShowDialog();
            if (new_folder_name != "")
            {
                string new_folder_path = $"{get_current_path()}/{new_folder_name}";
                CreateFolderArg folder_arg;
                try
                {
                    folder_arg = new CreateFolderArg(new_folder_path, false);
                    var folder = await client.Files.CreateFolderV2Async(folder_arg);
                    var m = folder.Metadata;
                    MessageBox.Show($"Папка создана успешно.\r" +
                        $"Имя новой папки: {m.Name}");
                    textBox_history.Text = $"Успешно создана папка: {m.Name}\r{textBox_history.Text}";
                    await list_folder(client, get_current_path());
                }
                catch (Exception ex)
                {
                    folder_arg = new CreateFolderArg(new_folder_path, true);
                    var folder = await client.Files.CreateFolderV2Async(folder_arg);
                    var m = folder.Metadata;
                    MessageBox.Show($"Папка с таким именем уже существует.\r" +
                        $"Новая папка создана с именем: {m.Name}");
                    textBox_history.Text = $"Успешно создана папка: {m.Name}\r{textBox_history.Text}";
                    await list_folder(client, get_current_path());
                }
            }
            
            //try
            //{
            //    var folderArg = new CreateFolderArg(path);
            //    var folder = await client.Files.CreateFolderV2Async(folderArg);
            //    return folder.Metadata;
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("-----------------" + e.Message);
            //    return null;
            //}
        }

        public async void menu_click_delete(object sender, RoutedEventArgs e)
        {
            string path;
            if (listView1.SelectedItem != null)
            {
                bool is_folder = ((Files_info)listView1.SelectedItem).is_folder == "Папка" ? true : false;
                path = ((Files_info)listView1.SelectedItem).full_path;
                if (is_folder)
                {
                    var file_name = ((Files_info)listView1.SelectedItem).file_name;
                    MessageBoxResult result = MessageBox.Show($"Подтвердите удаление следующей папки:\r" +
                                                              $"{file_name}", "Удаление папки", MessageBoxButton.YesNo);
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            try
                            {
                                var delete = await client.Files.DeleteV2Async(path);
                                textBox_history.Text = $"Успешное удаление папки: {file_name}\r{textBox_history.Text}";
                                await list_folder(client, get_current_path());
                            }
                            catch (Dropbox.Api.ApiException<Dropbox.Api.Files.DeleteError> ex)
                            {
                                MessageBox.Show("Вы пытаетесь удалить папку которая в облаке уже удалена", "Ошибка удаления папки");
                                await list_folder(client, get_current_path());
                            }
                            break;
                        case MessageBoxResult.No:
                            break;
                    }
                }
                else
                {
                    var file_name = ((Files_info)listView1.SelectedItem).file_name;
                    MessageBoxResult result = MessageBox.Show($"Подтвердите удаление следующего файла:\r" +
                                                              $"{file_name}", "Удаление файла", MessageBoxButton.YesNo);
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            try
                            {
                                var delete = await client.Files.DeleteV2Async(path);
                                textBox_history.Text = $"Успешное удаление файла: {file_name}\r{textBox_history.Text}";
                                await list_folder(client, get_current_path());
                            }
                            catch (Dropbox.Api.ApiException<Dropbox.Api.Files.DeleteError> ex)
                            {
                                MessageBox.Show("Вы пытаетесь удалить файл который в облаке уже удален", "Ошибка удаления файла");
                                await list_folder(client, get_current_path());
                            }
                            break;
                        case MessageBoxResult.No:
                            break;
                    }
                }
            }
        }
        #endregion
        // uncompleted folder download

        // uncompleted method
        

        public void add_progress_bar(string _raw_file_name)
        {
            string file_name = _raw_file_name.Replace('.', 'd').Replace(' ', 's').Replace('=', 'e')
                .Replace(',', 'c').Replace('(', 'l').Replace(')', 'r').Replace('-', 't');
            //ser = file_name;
            ProgressBar progress_bar = new ProgressBar();
            progress_bar.Height = 25;
            progress_bar.SetValue(Grid.ColumnSpanProperty, 4);
            progress_bar.SetValue(Grid.RowProperty, 1);
            progress_bar.SetValue(Grid.ColumnProperty, 0);

            Label label_file_name = new Label();
            label_file_name.SetValue(Grid.ColumnProperty, 0);
            label_file_name.SetValue(Grid.RowProperty, 0);
            label_file_name.SetValue(Grid.ColumnSpanProperty, 5);
            label_file_name.Content = $"{_raw_file_name}";

            Label label_percent = new Label();
            label_percent.SetValue(Grid.ColumnProperty, 4);
            label_percent.SetValue(Grid.RowProperty, 1);
            label_percent.Content = "0%";

            Grid grid_progress = new Grid();
            //grid_progress.Background = Brushes.Red;
            grid_progress.ColumnDefinitions.Add(new ColumnDefinition());
            grid_progress.ColumnDefinitions.Add(new ColumnDefinition());
            grid_progress.ColumnDefinitions.Add(new ColumnDefinition());
            grid_progress.ColumnDefinitions.Add(new ColumnDefinition());
            grid_progress.ColumnDefinitions.Add(new ColumnDefinition());
            grid_progress.RowDefinitions.Add(new RowDefinition());
            grid_progress.RowDefinitions.Add(new RowDefinition());

            grid_progress.Children.Add(progress_bar);
            grid_progress.Children.Add(label_percent);
            grid_progress.Children.Add(label_file_name);

            try
            {
                //Console.WriteLine($"register {file_name}");
                this.RegisterName($"progress_bar_{file_name}", progress_bar);
                this.RegisterName($"label_file_name_{file_name}", label_file_name);
                this.RegisterName($"label_percent_{file_name}", label_percent);
                this.RegisterName($"grid_progress_{file_name}", grid_progress);
                //MessageBox.Show($"grid_progress_{file_name}");
            }
            catch (Exception ex)
            {
                this.UnregisterName($"progress_bar_{file_name}");
                this.UnregisterName($"label_file_name_{file_name}");
                this.UnregisterName($"label_percent_{file_name}");
                this.UnregisterName($"grid_progress_{file_name}");
                this.RegisterName($"progress_bar_{file_name}{reserve_number}", progress_bar);
                this.RegisterName($"label_file_name_{file_name}{reserve_number}", label_file_name);
                this.RegisterName($"label_percent_{file_name}{reserve_number}", label_percent);
                this.RegisterName($"grid_progress_{file_name}{reserve_number}", grid_progress);
                reserve_number++;
            }

            stackPanel_progress.Children.Add(grid_progress);
        }

        public async void download_file(string _server_path, string _raw_file_name, string _local_path)
        {
            string file_name = _raw_file_name.Replace('.', 'd').Replace(' ', 's').Replace('=', 'e')
                .Replace(',', 'c').Replace('(', 'l').Replace(')', 'r').Replace('-', 't');
            //bool f = (Grid)this.FindName($"grid_progress_{file_name}") != null ? true : false;
            //MessageBox.Show($"start download {f}");
            var response = await client.Files.DownloadAsync(_server_path);
            ulong fileSize = response.Response.Size;
            const int bufferSize = 1024 * 1024 * 500;
            var buffer = new byte[bufferSize];
            string local_path = "";
            using (var content = await response.GetContentAsStreamAsync())
            {
                //using (var file = new FileStream(path_to_local_download_folder + _raw_file_name, FileMode.Create))
                if (_local_path == null)
                {
                    //MessageBox.Show("llll");
                    local_path = path_to_local_download_folder + _raw_file_name;
                }
                else
                {
                    local_path = _local_path;
                }

                //MessageBox.Show($"local path   {local_path}\rserver path    {_server_path}\rraw      {_raw_file_name}");
                using (var file = new FileStream(local_path, FileMode.Create))
                {
                    var length = content.Read(buffer, 0, bufferSize);
                    while (length > 0)
                    {
                        file.Write(buffer, 0, length);
                        var percentage = 100 * (ulong)file.Length / fileSize;
                        Application.Current.Dispatcher.Invoke(new Action(() => {
                            update_progress((int)percentage, file_name, _raw_file_name);
                        }));

                        length = content.Read(buffer, 0, bufferSize);
                    }
                }
                decrypt_file(local_path, _server_path);
            }
            Application.Current.Dispatcher.Invoke(new Action(() => { file_download_complete(file_name, _raw_file_name); }));
        }
        public void file_download_complete(string _file_name, string _raw_file_name)
        {
            Grid grid = (Grid)this.FindName($"grid_progress_{_file_name}");
            stackPanel_progress.Children.Remove(grid);

            this.UnregisterName($"grid_progress_{_file_name}");
            this.UnregisterName($"progress_bar_{_file_name}");
            this.UnregisterName($"label_percent_{_file_name}");
            this.UnregisterName($"label_file_name_{_file_name}");

            textBox_history.Text = $"Файл скачан успешно: {_raw_file_name}\r{textBox_history.Text}";
        }
        public void update_progress(int _percent, string _file_name, string _raw_file_name)
        {
            //Console.WriteLine($"update {_file_name}");
            var progress_bar = (ProgressBar)this.FindName($"progress_bar_{_file_name}");
            var label_file_name = (Label)this.FindName($"label_file_name_{_file_name}");
            var label_percent = (Label)this.FindName($"label_percent_{_file_name}");
            progress_bar.Value = _percent;
            label_file_name.Content = _raw_file_name;
            label_percent.Content = $"{_percent}%";
        }



        private void menu_click_upload_file(object sender, RoutedEventArgs e)
        {
            var current_path = get_current_path();
            var dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.ShowDialog();
            List<string> names = new List<string>();
            List<string> paths = new List<string>();
            foreach (var path in dialog.FileNames)
            {
                paths.Add(path);
            }
            foreach (var name in dialog.SafeFileNames)
            {
                names.Add(name);
            }
            string path_to_enc_file = "";
            for (int i = 0; i < names.Count; i++)
            {
                //MessageBox.Show($"{current_path}/{names[i]}\r{paths[i]}");
                
                path_to_enc_file = encrypt_file($"{current_path}/{names[i]}", paths[i]);
                //MessageBox.Show($"start upload file {names[i]} to server path {current_path}/{names[i]} from local path {path_to_enc_file}");
                //save_key();
                upload_file($"{current_path}/{names[i]}", path_to_enc_file, paths[i]);
            }
        }

        public async void upload_file(string remotePath, string Path_to_enc_file, string local_path)
        {
            uploading_file_count++;
            const int ChunkSize = 4096 * 1024;
            using (var fileStream = File.Open(Path_to_enc_file, FileMode.Open))
            {
                if (fileStream.Length <= ChunkSize)
                {
                    //File f = new File();
                    //var attr = File.GetLastWriteTime(localPath);
                    //MessageBox.Show(attr.ToString());
                    Console.WriteLine(remotePath);
                    await this.client.Files.UploadAsync(remotePath, body: fileStream, mode: WriteMode.Overwrite.Instance/*, clientModified: attr*/);
                }
                else
                {
                    //MessageBox.Show("big");
                    await this.ChunkUpload(remotePath, fileStream, (int)ChunkSize);
                }
            }
            uploading_file_count--;
            File.Delete(Path_to_enc_file);
            textBox_history.Text = $"Файл загружен успешно: {local_path} Осталось файлов: {uploading_file_count}\r" + textBox_history.Text;
        }


        public async Task ChunkUpload(String path, FileStream stream, int chunkSize)
        {
            ulong numChunks = (ulong)Math.Ceiling((double)stream.Length / chunkSize);
            byte[] buffer = new byte[chunkSize];
            string sessionId = null;
            for (ulong idx = 0; idx < numChunks; idx++)
            {
                Console.WriteLine($"{idx}   {numChunks}");
                var byteRead = stream.Read(buffer, 0, chunkSize);

                using (var memStream = new MemoryStream(buffer, 0, byteRead))
                {
                    if (idx == 0)
                    {
                        UploadSessionStartArg arg = new UploadSessionStartArg();
                        var result = await this.client.Files.UploadSessionStartAsync(arg, memStream);
                        sessionId = result.SessionId;
                    }
                    else
                    {
                        var cursor = new UploadSessionCursor(sessionId, (ulong)chunkSize * idx);

                        if (idx == numChunks - 1)
                        {
                            FileMetadata fileMetadata = await this.client.Files.UploadSessionFinishAsync(cursor, new CommitInfo(path), memStream);
                            Console.WriteLine(fileMetadata.PathDisplay);
                        }
                        else
                        {
                            await this.client.Files.UploadSessionAppendV2Async(cursor, false, memStream);
                        }
                    }
                }
            }
        }

        private void menu_click_upload_folder(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            List<string> names = new List<string>();
            List<string> paths = new List<string>();
            string root_folder_name = "";
            string root_folder_path = "";
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                root_folder_path = dialog.FileName;
                int last = root_folder_path.LastIndexOf('\\');
                root_folder_name = extract_file_name_from_path(root_folder_path);
                folders_to_upload.Clear();
                files_to_upload.Clear();
                find_items_to_upload(root_folder_path);

                //string s = folders_to_upload[0];



                Regex reg = new Regex($"{root_folder_name}");
                Match match;
                
                string fol = "";
                // made from full local path to folder, path to create folder in server
                for (int i = 0; i < folders_to_upload.Count; i++)
                {
                    match = reg.Match(folders_to_upload[i]);
                    folders_to_upload[i] = $"{get_current_path()}/" + folders_to_upload[i]
                        .Substring(match.Index, folders_to_upload[i].Length - match.Index)
                        .Replace('\\', '/');
                }
                //for (int i = 0; i < files_to_upload.Count; i++)
                //{
                //    MessageBox.Show("asdasdasdasd          " + files_to_upload[i]);
                //    match = reg.Match(files_to_upload[i]);
                //    files_to_upload[i] = files_to_upload[i]
                //        .Substring(match.Index, files_to_upload[i].Length - match.Index)
                //        .Replace('\\', '/');
                //}
                upload_folder(folders_to_upload, root_folder_name);
                //foreach (string folder in files_to_upload)
                //{
                //    fol += $"{folder}\r";
                //}
                //MessageBox.Show(fol);
            }

        }
        public async void upload_folder(List<string> _folders, string _root_folder_name)
        {
            //var arg = new CreateFolderBatchArg(_folders);
            var id = await client.Files.CreateFolderBatchAsync(_folders);
            //var s = id.
            //while (!id.IsComplete)
            //{
            //    MessageBox.Show("");
            //    Console.WriteLine("------------");
            //}
            //MessageBox.Show("complete");
            Regex reg = new Regex($"{_root_folder_name}");
            Match match;
            string local_path = "";
            string server_path = "";
            string path_to_enc_file = "";
            for (int i = 0; i < files_to_upload.Count; i++)
            {
                local_path = files_to_upload[i];
                //MessageBox.Show("asdasdasdasd          " + files_to_upload[i]);
                match = reg.Match(files_to_upload[i]);
                server_path = $"{get_current_path()}/" + files_to_upload[i]
                    .Substring(match.Index, files_to_upload[i].Length - match.Index)
                    .Replace('\\', '/');
                //MessageBox.Show($"local_path: {local_path}\rserver_path: {server_path}");
                path_to_enc_file = encrypt_file(server_path, local_path);
                upload_file(server_path,path_to_enc_file, local_path);
            }
            //var s = await client.Files.CreateFolderBatchCheckAsync(id.);
        }

        public string extract_file_name_from_path(string _path)
        {
            int last = _path.LastIndexOf('\\');
            return _path.Substring(last + 1, _path.Length - last - 1);
        }

        
        
        public void find_items_to_upload(/*string _server_path,*/ string _local_path)
        {
            //await client.Files.CreateFolderV2Async(arg);
            //Thread.Sleep(50000);
            //string folders = "", files = "";
            foreach (var file in Directory.GetFileSystemEntries(_local_path))
            {
                if (Directory.Exists(file))
                {
                    folders_to_upload.Add(file);
                    find_items_to_upload(/*$"{_server_path}/{extract_file_name_from_path(file)}", */file);
                }
                else if (File.Exists(file))
                {
                    files_to_upload.Add(file);
                    //upload_file(_server_path, file);
                }
                
            }
            
        }


        private void create_folder_for_sync(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            string local_path = "";
            string server_path = "";
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                local_path = dialog.FileName;
                int index = dialog.FileName.LastIndexOf("\\");
                string folder_name = dialog.FileName.Substring(index + 1, dialog.FileName.Length - index - 1);
                server_path = $"{get_current_path()}/{folder_name}";

                string json = File.ReadAllText("users_settings/users.json");
                List<window_login.User> users = JsonConvert.DeserializeObject<List<window_login.User>>(json);
                foreach (window_login.User user in users)
                {
                    if (user.login == current_user.login & user.password == user.password)
                    {
                        user.path_local_sync = local_path;
                        user.path_server_sync = server_path;
                        user.sync_folder_name = folder_name;
                        current_user = user;
                        break;
                    }
                }
                json = JsonConvert.SerializeObject(users, Formatting.Indented);
                File.WriteAllText("users_settings/users.json", json);
                
            }
        }
        List<string> server_files_to_sync = new List<string>();
        List<string> local_files_to_sync = new List<string>();
         
        public void find_local_files_to_sync(string _path)
        {
            foreach (var file in Directory.GetFileSystemEntries(_path))
            {
                if (Directory.Exists(file))
                {
                    find_local_files_to_sync(file);
                }
                else if (File.Exists(file))
                {
                    local_files_to_sync.Add(file);
                }
            }
        }

        public void find_server_files_to_sync(string _server_path)
        {
            var files = client.Files.ListFolderAsync(_server_path);
            foreach (var file in files.Result.Entries)
            {
                if (file.IsFolder)
                {
                    find_server_files_to_sync(file.PathDisplay);
                }
                else if (file.IsFile)
                {
                    server_files_to_sync.Add(file.PathDisplay);
                }
            }
        }

        public string list_to_string(List<string> _list)
        {
            string str = "";
            foreach (string element in _list)
            {
                str += $"{element}\r";
            }
            return str;
        }

        private async void start_sync(object sender, RoutedEventArgs e)
        {
            local_files_to_sync.Clear();
            server_files_to_sync.Clear();
            find_local_files_to_sync(current_user.path_local_sync);
            find_server_files_to_sync(current_user.path_server_sync);
            
            List<string> local_file_names = new List<string>();
            List<string> server_file_names = new List<string>();
            string sync_folder = current_user.sync_folder_name;
            string s = "";
            int index = 0;
            foreach (string file in local_files_to_sync)
            {
                index = file.IndexOf($@"\{sync_folder}\");
                s = file.Substring(index + 2 + sync_folder.Length, file.Length - 2 - index - sync_folder.Length).Replace('\\', '/');
                local_file_names.Add(s);
            }
            foreach (string file in server_files_to_sync)
            {
                //MessageBox.Show(file);
                index = file.IndexOf($@"/{sync_folder}/");
                s = file.Substring(index + 2 + sync_folder.Length, file.Length - 2 - index - sync_folder.Length);
                server_file_names.Add(s);
            }
            List<string> to_update = new List<string>();
            List<string> to_upload = new List<string>();
            List<string> to_download = new List<string>();
            //MessageBox.Show($"server files\r{list_to_string(server_file_names)}\r" +
            //    $"\r\rlocal_files\r{list_to_string(local_file_names)}");
            string str = "";
            for (int loc = 0; loc < local_file_names.Count; loc++)
            {
                str = local_file_names[loc];
                if (server_file_names.Contains(str))
                {
                    to_update.Add(str);
                    server_file_names.Remove(str);
                    //local_file_names.Remove(str);
                }
                else
                {
                    to_upload.Add(str);

                    //local_file_names.Remove(str);
                }
                //to_download.Add(server_file_names[loc]);
            }
            to_download = server_file_names;
            MessageBox.Show($"to update\r{list_to_string(to_update)}\r\rto upload\r" +
                $"{list_to_string(to_upload)}\r\rto download\r{list_to_string(to_download)}");
            string local_path = "", server_path = "";
            string local_hash = "", server_hash = "";
            // update files
            string path_to_enc_file = "";
            long local_size = 0, server_size = 0;
            foreach (string file in to_update)
            {
                local_path = $"{current_user.path_local_sync}\\{file}".Replace('/', '\\');
                server_path = $"{current_user.path_server_sync}/{file}".Replace('\\', '/');
                local_size = (new FileInfo(local_path)).Length;
                server_size = (long)client.Files.GetMetadataAsync(server_path).Result.AsFile.Size;
                //MessageBox.Show($"serv size {server_size}\rlocal size {local_size}");
                server_hash = client.Files.GetMetadataAsync(server_path).Result.AsFile.ContentHash;
                local_hash = get_hash(local_path);
                //MessageBox.Show($"file   {file}\rlocal hash   {local_hash}\rserve hash   {server_hash}\r{local_size}-{server_size}");
                if ((local_size != 0) & (server_size != 0))
                {
                    if (local_hash != server_hash)
                    {
                        //MessageBox.Show("upl");
                        //path_to_enc_file = encrypt_file(local_path);
                        upload_file(server_path, path_to_enc_file, local_path);
                    }
                }
            }
            // upload files to server
            foreach (string file in to_upload)
            {
                local_path = $"{current_user.path_local_sync}\\{file}".Replace('/', '\\');
                server_path = $"{current_user.path_server_sync}/{file}".Replace('\\', '/');
                //path_to_enc_file = encrypt_file(local_path);
                //MessageBox.Show($"need upload\r{file}\r{local_path}\r{server_path}");
                upload_file(server_path, path_to_enc_file, local_path);
            }
            foreach (string file in to_download)
            {
                local_path = $"{current_user.path_local_sync}\\{file}".Replace('/', '\\');
                server_path = $"{current_user.path_server_sync}/{file}".Replace('\\', '/');

                add_progress_bar(extract_file_name_from_path(local_path));
                await Task.Run(() => download_file(server_path, extract_file_name_from_path(local_path), local_path));

            }

            //MessageBox.Show($"{list_to_string(local_file_names)}\r\r\r{list_to_string(server_file_names)}");
        }



        public string get_hash(string _path_to_file)
        {
            using (FileStream stream = new FileStream(_path_to_file, FileMode.Open, FileAccess.Read))
            {
                int buffer_size = 4 * 1024 * 1024;
                byte[] block;
                byte[] hash_block;
                long left_read = stream.Length;
                int size = 0;
                List<byte[]> hash_block_list = new List<byte[]>();
                bool continue_read = true;
                while (continue_read)
                {
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        if (left_read > buffer_size)
                        {
                            block = new byte[buffer_size];
                            stream.Read(block, 0, buffer_size);
                            continue_read = true;
                            left_read -= buffer_size;
                        }
                        else
                        {
                            block = new byte[left_read];
                            stream.Read(block, 0, (int)left_read);
                            continue_read = false;
                        }
                        hash_block = sha256.ComputeHash(block);
                        hash_block_list.Add(hash_block);
                        size += 32;
                    }
                }
                byte[] all_hash_block = new byte[size];
                int offset = 0;
                foreach (byte[] mas in hash_block_list)
                {
                    mas.CopyTo(all_hash_block, offset);
                    offset += 32;
                }
                string result;
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] mas = sha.ComputeHash(all_hash_block);
                    result = ToHex(mas);
                }
                return result;

            }
        }

        const string HEX_DIGITS = "0123456789abcdef";


        public static string ToHex(byte[] data)
        {
            var r = new System.Text.StringBuilder();
            foreach (byte b in data)
            {
                r.Append(HEX_DIGITS[(b >> 4)]);
                r.Append(HEX_DIGITS[(b & 0xF)]);
            }
            return r.ToString();
        }


        private string encrypt_file(string _server_path, string _local_path)
        {
            byte[] original_bytes = File.ReadAllBytes(_local_path);
            string original = Encoding.UTF8.GetString(original_bytes);
            byte[] encrypted_original = { };
            byte[] key = { }, iv = { };
            using (Aes aes = Aes.Create())
            {
                key = aes.Key;
                iv = aes.IV;
                ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);
                using (MemoryStream memory_stream = new MemoryStream())
                {
                    using (CryptoStream crypto_stream = new CryptoStream(memory_stream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter stream_writer = new StreamWriter(crypto_stream))
                        {
                            stream_writer.Write(original);
                        }
                        encrypted_original = memory_stream.ToArray();
                    }
                }
            }
            //string json = File.ReadAllText("users_settings/users_keys.json");
            //List<Users_keys> list = new List<Users_keys>();
            //Users_keys new_key = new Users_keys(current_user.login, current_user.password, _path, key, iv);
            //MessageBox.Show($"{new_key.login}\r{new_key.password}\r{new_key.file_path}\r{new_key.key}\r{new_key.iv}");
            //if (json.Length != 0)
            //{
            //    list = JsonConvert.DeserializeObject<List<Users_keys>>(json);
            //    foreach (Users_keys user_key in list)
            //    {
            //        if ((user_key.login == current_user.login) & (user_key.password == current_user.password) & (user_key.file_path == _path))
            //        {
            //            list.Remove(user_key);
            //            break;
            //        }
            //    }
            //    list.Add(new_key);
            //}
            //else
            //{
            //    list.Add(new_key);
            //}

            
            //json = JsonConvert.SerializeObject(list, Formatting.Indented);
            //File.WriteAllText("users_settings/users_keys.json", json);
            //Console.Write($"temp_{_path}");
            int index = _local_path.LastIndexOf('.');
            string extension = _local_path.Substring(index, _local_path.Length - index);
            string path_without_extension = _local_path.Substring(0, _local_path.Length - extension.Length);
            string encrypted_file_path = $"{path_without_extension}_temp{extension}";
            //MessageBox.Show(encrypted_file_path);
            File.WriteAllBytes(encrypted_file_path, encrypted_original);
            
            sqlite_command.CommandText = $"SELECT count(id_key) FROM keys WHERE local_path='{_local_path}'";
            sqlite_connection.Open();
            object count = sqlite_command.ExecuteScalar();
            sqlite_connection.Close();
            //MessageBox.Show($"111111\r\r\r{count.ToString()}");
            if (count.ToString() == "0")
            {
                MessageBox.Show("0");
                save_key(_server_path, _local_path, key, iv);
            }
            else
            {
                MessageBox.Show("not 0");
                sqlite_connection.Open();
                sqlite_command.CommandText = $"DELETE FROM keys WHERE local_path='{_local_path}'";
                sqlite_command.ExecuteNonQuery();
                sqlite_connection.Close();
                save_key(_server_path, _local_path, key, iv);
            }
            return encrypted_file_path;
        }

        public void save_key(string _server_path, string _local_path, byte[] _key, byte[] _iv)
        {
            sqlite_command.CommandText = $"INSERT INTO keys('login', 'local_path', 'server_path', 'key', 'iv') VALUES (@login, @local, @server, @key, @iv)";
            sqlite_command.Parameters.AddWithValue("@login", current_user.login);
            sqlite_command.Parameters.AddWithValue("@local", _local_path);
            sqlite_command.Parameters.AddWithValue("@server", _server_path);
            sqlite_command.Parameters.AddWithValue("@key", _key);
            sqlite_command.Parameters.AddWithValue("@iv", _iv);
            sqlite_connection.Open();
            sqlite_command.ExecuteNonQuery();
            sqlite_connection.Close();
        }

        public void decrypt_file(string _path_to_encrypt_file, string _server_path)
        {
            byte[] encrypted_original = File.ReadAllBytes(_path_to_encrypt_file);
            string orig = "";
            //string json = File.ReadAllText("users_settings/users_keys.json");
            //List<Users_keys> keys = new List<Users_keys>();
            //keys = JsonConvert.DeserializeObject<List<Users_keys>>(json);
            //int index_temp = _path_to_temp_file.LastIndexOf('_');
            //int index_extension = _path_to_temp_file.LastIndexOf('.');
            //string extension = _path_to_temp_file.Substring(index_extension, _path_to_temp_file.Length - index_extension);
            //string result_file_path = _path_to_temp_file.Substring(0, _path_to_temp_file.Length - index_temp) + extension;
            //MessageBox.Show($"decrypt\rtemp path{_path_to_temp_file}\rresult path{result_file_path}");
            //foreach (Users_keys key in keys)
            //{
            //if ((key.login == current_user.login) & (key.password == current_user.password) & (key.file_path == _path_to_encrypt_file))
            //{
            //Users_keys current_key = key;
            //MessageBox.Show($"{current_user.login}\r{current_key.login}\r\r{current_user.passw}");


            using (Aes aes = Aes.Create())
            {
                sqlite_command.CommandText = $"SELECT key, iv FROM keys WHERE server_path='{_server_path}'";
                sqlite_connection.Open();
                SQLiteDataReader reader = sqlite_command.ExecuteReader();
                while (reader.Read())
                {
                    aes.Key = (byte[])reader["key"];
                    aes.IV = (byte[])reader["iv"];
                }
                sqlite_connection.Close();
                //aes.Key = current_key.key;
                //aes.IV = current_key.iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (MemoryStream memory_stream = new MemoryStream(encrypted_original))
                {
                    using (CryptoStream crypto_stream = new CryptoStream(memory_stream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader stream_reader = new StreamReader(crypto_stream))
                        {
                            orig = stream_reader.ReadToEnd();
                        }
                    }
                }
            }
            byte[] s = Encoding.UTF8.GetBytes(orig);
            File.Delete(_path_to_encrypt_file);
            File.WriteAllBytes(_path_to_encrypt_file, s);
            //break;
            //}
            //}
        }

        private void encryptфывы_file(object sender, RoutedEventArgs e)
        {
            byte[] key = { }, iv = { };
            byte[] original_bytes = File.ReadAllBytes("milky-way-nasa.jpg");
            string original = Encoding.Default.GetString(original_bytes);
            byte[] encrypted_original = { };
            using (Aes aes = Aes.Create())
            {
                key = aes.Key;
                iv = aes.IV;
                ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);
                using (MemoryStream memory_stream = new MemoryStream())
                {
                    using (CryptoStream crypto_stream = new CryptoStream(memory_stream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter stream_writer = new StreamWriter(crypto_stream))
                        {
                            stream_writer.Write(original);
                        }
                        encrypted_original = memory_stream.ToArray();
                    }
                }

            }
            //File.WriteAllBytes($"temp{=}", encrypted_original);
            encrypted_original = null;
            encrypted_original = File.ReadAllBytes("temp");
            //MessageBox.Show($"{Encoding.Default.GetString(encrypted_original)}");

            string orig = "";
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
                using (MemoryStream memory_stream = new MemoryStream(encrypted_original))
                {
                    using (CryptoStream crypto_stream = new CryptoStream(memory_stream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader stream_reader = new StreamReader(crypto_stream))
                        {
                            orig = stream_reader.ReadToEnd();
                        }
                    }
                }
            }
            //MessageBox.Show(orig);
            byte[] s = Encoding.Default.GetBytes(orig);
            File.WriteAllBytes("milky-way-nasasasasa.jpg", s);
            MessageBox.Show("complete");
        }
    }


}
