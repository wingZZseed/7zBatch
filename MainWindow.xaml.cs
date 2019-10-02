using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace _7zBatch {
	/// <summary>
	/// MainWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MainWindow : Window {

		private const string PATTERN = "^\\d{4}\\s\\-\\s";

		private const string CMD = "7z.exe";

		private const string CMD_ARGUMENTS_DEFAULT = "a -y -r -mx9";

		private const string CMD_ARGUMENTS = " \"{0}\" \"{1}\"";

		private const string CMD_ARGUMENTS_SOLID = " -ms=off";

		private const string CMD_ARGUMENTS_TIMESTAMP = " -mtm=off";

		private const string CMD_ARGUMENTS_ATTR = " -mtr=off";

		private Dictionary<string, string> method = new Dictionary<string, string>() {
			{ " -m0=LZMA", "LZMA" },
			{ "", "LZMA2" }
		};

		private string _src;

		private string _target;

		private Process _process;

		private volatile bool _isExit;

		public MainWindow() {
			InitializeComponent();

			Cmb_Method.ItemsSource = method;
			Cmb_Method.SelectedValuePath = "Key";
			Cmb_Method.DisplayMemberPath = "Value";
			Cmb_Method.SelectedIndex = 1;

			RefreshCmd();
			
		}

		private void Btn_Browse_Src_Click(object sender, RoutedEventArgs e) {
			CommonOpenFileDialog dialog = new CommonOpenFileDialog { IsFolderPicker = true };
			if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
				return;
			Txt_Src.Text = _src = dialog.FileName;
		}

		private void Btn_Browse_Target_Click(object sender, RoutedEventArgs e) {
			CommonOpenFileDialog dialog = new CommonOpenFileDialog { IsFolderPicker = true };
			if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
				return;
			Txt_Target.Text = _target = dialog.FileName;
		}

		private void Btn_Rename_Click(object sender, RoutedEventArgs e) {
			if (string.IsNullOrWhiteSpace(_src))
				return;
			DirectoryInfo di = new DirectoryInfo(_src);
			FileInfo[] files = di.GetFiles("*.*");
			foreach (FileInfo fi in files) {
				string name = Regex.Replace(fi.Name, PATTERN, "");
				Console.WriteLine(fi.DirectoryName);
				if (!name.Equals(fi.Name))
					fi.MoveTo(fi.DirectoryName + "\\" + name);
			}
		}

		private void Comproess(string cmd_arguments) {
			_process = new Process();
			_process.StartInfo.CreateNoWindow = true;
			_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			_process.StartInfo.FileName = CMD;
			_process.StartInfo.Arguments = cmd_arguments;
			_process.EnableRaisingEvents = true;
			_process.Start();
			_process.WaitForExit();
			_process.Dispose();
		}

		private void Btn_Compress_Click(object sender, RoutedEventArgs e) {
			if (string.IsNullOrWhiteSpace(_src))
				return;
			if (string.IsNullOrWhiteSpace(_target))
				return;

			Btn_Rename.IsEnabled = false;
			Btn_Compress.IsEnabled = false;
			_isExit = false;

			var cmd_arguments = Txt_Cmd.Text + CMD_ARGUMENTS;

			Task.Factory.StartNew(() => {
				DirectoryInfo di = new DirectoryInfo(_src);

				var dirs = di.GetDirectories();
				var files = di.GetFiles();
				var size = files.Length + dirs.Length;

				Dispatcher.BeginInvoke(new Action(() => {
					ProgressBar.Maximum = size;
					ProgressBar.Value = 0;
				}));

				int i = 0;

				if (dirs.Length > 0) {
					foreach (var d in dirs) {
						if (_isExit)
							break;

						string archive = _target + "\\" + d.Name + ".7z";
						string fs = d.FullName + "\\*";

						Comproess(string.Format(cmd_arguments, archive, fs));

						i++;
						Dispatcher.BeginInvoke(new Action(() => {
							ProgressBar.Value = i;
						}));
					}
				}

				if (files.Length > 0) {
					foreach (FileInfo fi in files) {
						if (_isExit)
							break;

						string archive = _target + "\\" + System.IO.Path.GetFileNameWithoutExtension(fi.Name) + ".7z";

						Comproess(string.Format(cmd_arguments, archive, fi.FullName));

						i++;
						Dispatcher.BeginInvoke(new Action(() => {
							ProgressBar.Value = i;
						}));
					}
				}

				_process = null;

				var flag = _isExit;
				_isExit = false;

				Dispatcher.BeginInvoke(new Action(() => {
					if (flag)
						MessageBox.Show("Stop!");
					else
						MessageBox.Show("Compress Completed!");
					ResetUI();
				}));
			});
		}

		private void ResetUI() {
			Btn_Rename.IsEnabled = true;
			Btn_Compress.IsEnabled = true;
			ProgressBar.Value = 0;
		}

		private void proc_Exited(object sender, EventArgs e) {
			Dispatcher.BeginInvoke(new Action(() => {
				MessageBox.Show("Exit!");
				ResetUI();
			}));
		}

		private void Btn_Stop_Click(object sender, RoutedEventArgs e) {
			_isExit = true;

			try {
				if (_process != null && !_process.HasExited) {
					_process.Kill();
				}
			} catch (Exception ex) {
				MessageBox.Show(ex.Message);
			}
		}

		private void RefreshCmd() {
			var cmd = CMD_ARGUMENTS_DEFAULT;

			cmd += Cmb_Method.SelectedValue;

			if (!Chk_Soild.IsChecked.GetValueOrDefault(false))
				cmd += CMD_ARGUMENTS_SOLID;
			if (!Chk_Attr.IsChecked.GetValueOrDefault(false))
				cmd += CMD_ARGUMENTS_ATTR;
			if (!Chk_Timestamp.IsChecked.GetValueOrDefault(false))
				cmd += CMD_ARGUMENTS_TIMESTAMP;

			Txt_Cmd.Text = cmd;
		}

		private void Chk_Soild_Checked(object sender, RoutedEventArgs e) {
			RefreshCmd();
		}

		private void Chk_Attr_Checked(object sender, RoutedEventArgs e) {
			RefreshCmd();
		}

		private void Chk_Timestamp_Checked(object sender, RoutedEventArgs e) {
			RefreshCmd();
		}

		private void Cmb_Method_Changed(object sender, RoutedEventArgs e) {
			RefreshCmd();
		}

	}
}
