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

		private const string CMD_ARGUMENTS = "a -y -r -ms=off -mx9 -mtm=off -mtr=off \"{0}\" \"{1}\"";

		private string _src;

		private string _target;

		private bool _isCompressing;

		private Process _process;

		private volatile bool _isExit;

		public MainWindow() {
			InitializeComponent();
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

		private void Btn_Compress_Click(object sender, RoutedEventArgs e) {
			if (string.IsNullOrWhiteSpace(_src))
				return;
			if (string.IsNullOrWhiteSpace(_target))
				return;

			Btn_Rename.IsEnabled = false;
			Btn_Compress.IsEnabled = false;
			_isExit = false;

			Task.Factory.StartNew(() => {
				DirectoryInfo di = new DirectoryInfo(_src);
				FileInfo[] files = di.GetFiles("*.*");
				var size = files.Count();

				Dispatcher.BeginInvoke(new Action(() => {
					ProgressBar.Maximum = size;
					ProgressBar.Value = 0;
				}));

				int i = 0;
				foreach (FileInfo fi in files) {
					if (_isExit)
						break;
					string tar = _target + "\\" + System.IO.Path.GetFileNameWithoutExtension(fi.Name) + ".7z";
					_process = new Process();
					_process.StartInfo.CreateNoWindow = true;
					_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					_process.StartInfo.FileName = CMD;
					_process.StartInfo.Arguments = string.Format(CMD_ARGUMENTS, tar, fi.FullName);
					_process.EnableRaisingEvents = true;
					//_process.Exited += new EventHandler(proc_Exited);
					_process.Start();
					_process.WaitForExit();
					_process.Dispose();

					i++;

					Dispatcher.BeginInvoke(new Action(() => {
						ProgressBar.Value = i;
					}));
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
	}
}
