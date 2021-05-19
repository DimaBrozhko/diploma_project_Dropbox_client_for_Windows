using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace my_dropbox_app
{
    /// <summary>
    /// Логика взаимодействия для window_login.xaml
    /// </summary>
    public partial class window_login : Window
    {
        public string global_token = "";
        public class User
        {
            public string login { get; set; }
            public string password { get; set; }
            public string user_token { get; set; }
            public string default_download_folder { get; set; }
            public string path_local_sync { get; set; }
            public string path_server_sync { get; set; }
            public string sync_folder_name { get; set; }
            public User(string _login, string _password, string _user_token, string _local_path, string _server_path, string _sync_folder_name)
            {
                login = _login;
                password = _password;
                user_token = _user_token;
                path_local_sync = _local_path;
                path_server_sync = _server_path;
                sync_folder_name = _sync_folder_name;
            }
        }
        public window_login()
        {
            InitializeComponent();
        }

        private void registration(object sender, RoutedEventArgs e)
        {
            string new_user_login = textBox_login.Text;
            string new_user_passwors = textBox_password.Text;
            Regex reg = new Regex(@"^[\d\w]{5,50}$");
            if (reg.IsMatch(new_user_login))
            {
                if (reg.IsMatch(new_user_passwors))
                {
                    if (!File.Exists("users_settings/users.json"))
                    {
                        
                        using (File.Create("users_settings/users.json"))
                        {
                            MessageBox.Show("Файл с настройками пользователей не найден.\r" +
                            "Либо он был удален, либо это первый запуск приложения.\r" +
                            "Создан новый файл с настройками пользователей", "Новый файл с настройками");
                        }
                        textBox_login.Text = "";
                        textBox_password.Text = "";
                    }

                    List<User> users = new List<User>();
                    var json = File.ReadAllText("users_settings/users.json");
                    if (json.Length == 0)
                    {
                        var wnd = new window_input_token();
                        wnd.Owner = this;
                        wnd.ShowDialog();

                        if (global_token != "")
                        {
                            users.Add(new User(new_user_login, new_user_passwors, global_token, "", "", ""));
                            json = JsonConvert.SerializeObject(users, Formatting.Indented);
                            File.WriteAllText("users_settings/users.json", json);
                            global_token = "";
                            MessageBox.Show($"Пользователь {new_user_login} успешно создан", "Пользователь создан");
                            textBox_login.Text = "";
                            textBox_password.Text = "";
                        }
                        else
                        {
                            MessageBox.Show("Вы не подтвердили токен.", "Ошибка регистрации");
                        }
                    }
                    else
                    {
                        users = JsonConvert.DeserializeObject<List<User>>(json);
                        bool user_exist = false;
                        foreach (User user in users)
                        {
                            if (user.login == new_user_login)
                            {
                                user_exist = true;
                            }
                        }
                        if (user_exist)
                        {
                            MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка регистрации");
                        }
                        else
                        {
                            var wnd = new window_input_token();
                            wnd.Owner = this;
                            wnd.ShowDialog();
                            if (global_token != "")
                            {
                                users.Add(new User(new_user_login, new_user_passwors, global_token, "", "", ""));
                                json = JsonConvert.SerializeObject(users, Formatting.Indented);
                                File.WriteAllText("users_settings/users.json", json);
                                global_token = "";
                                MessageBox.Show($"Пользователь {new_user_login} успешно создан", "Пользователь создан");
                                textBox_login.Text = "";
                                textBox_password.Text = "";
                            }
                            else
                            {
                                MessageBox.Show("Вы не подтвердили токен.", "Ошибка регистрации");
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("В логине и пароле могут содержаться только буквы русского и латинского алфавитов, цифры, знак подчеркивания." +
                    " Необходимая длина логина или пароля - от 5 до 50 символов", "Некорректный пароль");
                }
            }
            else
            {
                MessageBox.Show("В логине и пароле могут содержаться только буквы русского и латинского алфавитов, цифры, знак подчеркивания." +
                    " Необходимая длина логина или пароля - от 5 до 50 символов", "Некорректный логин");
            }
        }

        private void autentification(object sender, RoutedEventArgs e)
        {
            
            if (File.Exists("users_settings/users.json"))
            {
                string user_login = textBox_login.Text;
                string user_password = textBox_password.Text;
                List<User> users = new List<User>();
                string json = File.ReadAllText("users_settings/users.json");
                bool user_exist = false;
                int user_index = 0;
                if (json.Length == 0)
                {
                    MessageBox.Show("В системе не зарегистрировано ни одного пользователя", "Ошибка аутентификации");
                }
                else
                {
                    users = JsonConvert.DeserializeObject<List<User>>(json);
                    for (int i = 0; i < users.Count; i++)
                    {
                        if ((users[i].login == user_login) & (users[i].password == user_password))
                        {
                            user_exist = true;
                            user_index = i;
                            break;
                        }
                    }
                    if (user_exist)
                    {
                        User current_user = new User(users[user_index].login, users[user_index].password, users[user_index].user_token,
                            users[user_index].path_local_sync, users[user_index].path_server_sync, users[user_index].sync_folder_name);
                        MainWindow wnd = new MainWindow(current_user);
                        wnd.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Пользователь с такими логином и паролем не найден.", "Ошибка аутентификации");
                    }
                }
            }
            else
            {
                MessageBox.Show("Отсутствует файл с настройками.\rВозможно, это первый запуск программы и нет зарегистрированных пользователей", "Ошибка авторизации");
            }
            
        }
    }
}
