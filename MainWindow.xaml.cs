using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Diagnostics;

namespace AcceptXboxLive
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            cmbExtension.SelectedIndex = 0;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Select input video",
                Filter = "Video files|*.mp4;*.avi;*.mkv;*.mov;*.webm|All files|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                txtInput.Text = ofd.FileName;
            }
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            string input = txtInput.Text.Trim();
            if (!File.Exists(input))
            {
                MessageBox.Show("Input file not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string ext = ((System.Windows.Controls.ComboBoxItem)cmbExtension.SelectedItem).Content.ToString();

            var sfd = new SaveFileDialog
            {
                Title = "Save output file",
                Filter = $"{ext.ToUpper()} File|*.{ext}|All Files|*.*",
                FileName = Path.GetFileNameWithoutExtension(input) + "." + ext
            };
            if (sfd.ShowDialog() != true) return;

            string output = sfd.FileName;

            // Main ffmpeg command (-y to always overwrite)
            string args = $"-y -i \"{input}\" " +
                          $"-c:v mpeg4 -vf \"scale={txtWidth.Text}:{txtHeight.Text},setsar={txtSAR.Text}\" " +
                          $"-q:v {txtQuality.Text} -af \"volume={txtVolume.Text}dB\" " +
                          $"-r {txtFramerate.Text} -c:a ac3 -b:a {txtAudioBitrate.Text} -ar {txtAudioRate.Text} " +
                          $"-bsf:v noise={txtVBSF.Text} -bsf:a noise={txtABSF.Text} -f avi \"{output}\"";

            // Run main ffmpeg and wait until it finishes
            RunAndWait("ffmpeg.exe", args);

            // Optional re-encode for web/Discord-friendly output
            if (chkReencodeFriendly.IsChecked == true)
            {
                string friendlyOutput = Path.Combine(
                    Path.GetDirectoryName(output),
                    Path.GetFileNameWithoutExtension(output) + "_friendly.mp4");

                string reencodeArgs = $"-y -i \"{output}\" -c:v libx264 -preset fast -profile:v high -level 4.0 " +
                                      $"-r 30 -pix_fmt yuv420p -c:a aac -b:a 128k \"{friendlyOutput}\"";

                RunAndWait("ffmpeg.exe", reencodeArgs);
                output = friendlyOutput;
            }

            // Optionally play output
            if (chkPlayAfter.IsChecked == true && File.Exists(output))
            {
                RunAndWait("ffplay.exe", $"\"{output}\"");
            }
        }

        private void RunAndWait(string exe, string args)
        {
            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        WorkingDirectory = Environment.CurrentDirectory
                    }
                };
                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to run process: " + ex.Message);
            }
        }
    }
}
