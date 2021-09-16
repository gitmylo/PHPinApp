using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Net;
using System.Security.Policy;
using System.Windows;
using System.Windows.Media.Imaging;
using IniParser.Model;
using IniParser.Parser;

namespace PHPinApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            string configFile = "config.ini";
            if (!File.Exists(configFile)) setupConfigEmpty(configFile);//avoid overwriting
            string configIni = File.ReadAllText(configFile);
            IniData config = new IniDataParser().Parse(configIni);
            config = insertPlaceholders(config);
            setConfig(config);
            setupPhp(config);
        }

        private void setupServer(IniData config)
        {
            String launchCommand = config["PHP"]["exe"] + " " + config["PHP"]["launchcommand"];
            if (config["PHP"]["debugmode"] == "1")
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = config["PHP"]["exe"];
                info.Arguments = config["PHP"]["launchcommand"];
                phpProc = Process.Start(info);
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                Window.Closed += OnProcessExit;
            }
            else
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = config["PHP"]["exe"];
                info.Arguments = config["PHP"]["launchcommand"];
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                phpProc = Process.Start(info);
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                Window.Closed += OnProcessExit;
            }
            
            setupBrowser(config);
        }

        private static Process phpProc;
        static void OnProcessExit (object sender, EventArgs e)//i am trying to make it always end it, but currently if it gets ended in task manager, it's not going to close the php server
        {
            phpProc.Kill();
        }

        private static float dlprogress = 0;
        private IniData insertPlaceholders(IniData config)
        {
            foreach (SectionData section in config.Sections)
            {
                if (section.SectionName != "Placeholders")
                {
                    foreach (KeyData key in section.Keys)
                    {
                        config[section.SectionName][key.KeyName] = config[section.SectionName][key.KeyName].Replace(config["Placeholders"]["appdir"], Path.GetDirectoryName(Application.ResourceAssembly.Location));
                    }
                }
            }
            return config;
        }

        private void setupPhp(IniData config)
        {
            if (!Directory.Exists(config["PHP"]["downloaddir"]))
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadProgressChanged += (sender, args) =>
                    {
                        Window.Title = config["Window"]["dltitle"] + " (" + args.ProgressPercentage + ")";
                    };
                    wc.DownloadFileCompleted += (sender, args) =>
                    {
                        Window.Title = config["Window"]["unziptitle"];

                        Directory.CreateDirectory(config["PHP"]["downloaddir"]);
                        System.IO.Compression.ZipFile.ExtractToDirectory("PHP.zip", config["PHP"]["downloaddir"]);

                        Window.Title = config["Window"]["starttitle"];
                        setupServer(config);
                    };
                    wc.Headers.Add("user-agent", config["PHP"]["dluseragent"]);
                    wc.DownloadFileAsync(new Uri(config["PHP"]["downloadurl"]), "PHP.zip");
                }
            }
            else
            {
                setupServer(config);
            }
        }

        private void setupBrowser(IniData config)
        {
            Browser.TitleChanged += (sender, args) =>
            {
                Window.Title = (string)args.NewValue;
            };
            
            Browser.Load(config["Browser"]["loadurl"]);
        }

        private void setConfig(IniData config)
        {
            Window.Title = config["Window"]["starttitle"];
            
            Window.Width = int.Parse(config["Screen"]["defaultwidth"]);
            Window.Height = int.Parse(config["Screen"]["defaultheight"]);

            if (config["Screen"]["usemax"] == "1")
            {
                Window.MaxWidth = int.Parse(config["Screen"]["maxwidth"]);
                Window.MaxHeight = int.Parse(config["Screen"]["maxheight"]);
            }

            if (config["Screen"]["usemin"] == "1")
            {
                Window.MinWidth = int.Parse(config["Screen"]["minwidth"]);
                Window.MinHeight = int.Parse(config["Screen"]["minheight"]);
            }
            
            Window.Icon = new BitmapImage(new Uri(Path.GetDirectoryName(Application.ResourceAssembly.Location) + "/" + config["Window"]["icon"]));
        }

        private void setupConfigEmpty(string configFile)
        {
            IniData config = new IniData();

            config["Placeholders"]["appdir"] = "%appdir%";
            
            config["Window"]["starttitle"] = "Loading... please wait";
            config["Window"]["dltitle"] = "Downloading php files...";
            config["Window"]["unziptitle"] = "unzipping php files...";
            config["Window"]["icon"] = "icon.ico";

            config["Screen"]["defaultwidth"] = "525";
            config["Screen"]["defaultheight"] = "350";
            
            config["Screen"]["usemax"] = "0";
            config["Screen"]["maxwidth"] = "525";
            config["Screen"]["maxheight"] = "350";
            
            config["Screen"]["usemin"] = "0";
            config["Screen"]["minwidth"] = "525";
            config["Screen"]["minheight"] = "350";

            config["Browser"]["loadurl"] = "localhost:8001";

            config["PHP"]["exe"] = "%appdir%/php/php.exe";
            config["PHP"]["launchcommand"] = "-S localhost:8001 -t %appdir%/htdocs";
            config["PHP"]["debugmode"] = "1";
            config["PHP"]["downloadurl"] = "https://windows.php.net/downloads/releases/php-8.0.10-Win32-vs16-x64.zip";
            config["PHP"]["downloaddir"] = "%appdir%/php";
            config["PHP"]["dluseragent"] = "PHPinApp";

            File.WriteAllText(configFile, config.ToString());
        }
    }
}