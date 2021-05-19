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

using Dropbox.Api;

namespace my_dropbox_app
{
    /// <summary>
    /// Логика взаимодействия для window_input_token.xaml
    /// </summary>
    public partial class window_input_token : Window
    {

        public window_input_token()
        {
            InitializeComponent();
        }

        private void cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void confirm(object sender, RoutedEventArgs e)
        {
            try
            {
                string token = textBox1.Text.Trim(' ');
                DropboxClient client = new DropboxClient(token);
                window_login window = this.Owner as window_login;
                window.global_token = token;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка токена");
            }
        }
    }
}
