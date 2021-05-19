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

namespace my_dropbox_app
{
    /// <summary>
    /// Логика взаимодействия для window_create_folder_input.xaml
    /// </summary>
    public partial class window_create_folder_input : Window
    {
        public window_create_folder_input()
        {
            InitializeComponent();
        }

        private void to_confirm_create(object sender, RoutedEventArgs e)
        {
            MainWindow window = this.Owner as MainWindow;
            string text = textBox1.Text;
            List<bool> bool_list = new List<bool>();
            List<string> string_list = new List<string>();
            string_list.Add("<");
            string_list.Add(">");
            string_list.Add("\\");
            string_list.Add("/");
            string_list.Add(":");
            string_list.Add("?");
            string_list.Add("\"");
            string_list.Add("|");
            string output_message = "";

            foreach (string s in string_list)
            {
                if (text.Contains(s))
                {
                    bool_list.Add(true);
                }
                else
                {
                    bool_list.Add(false);
                }
            }
            if (window != null)
            {
                if (textBox1.Text != "")
                {
                    if (bool_list.Contains(true))
                    {
                        for (int j = 0; j < bool_list.Count; j++)
                        {
                            if (bool_list[j])
                            {
                                output_message += $"{string_list[j]}\r";
                            }
                        }
                        MessageBox.Show($"В имени папки присутствуют следующие недопустимые символы: \r" +
                            $"{output_message}", "Ошибка создания папки");
                    }
                    else
                    {
                        window.new_folder_name = textBox1.Text;
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Вы не ввели имя новой папки. Имя не может быть пустым.", "Предупреждение");
                }
            }
        }

        private void cancel_create(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
