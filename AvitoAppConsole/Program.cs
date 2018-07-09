using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System.Collections.ObjectModel;
using tessnet2;
using System.Drawing;
using System.Net;

namespace AvitoAppConsole
{
    class Program
    {
        public static Program current;
        public static ChromeDriver Driver;
        private Actions driverActions;

        private string defCity = "kaliningrad";

        private string _avitoPath = "https://www.avito.ru/{0}/avtomobili";

        private bool _needMinPrice = false;
        private bool _needMaxPrice = false;
        private int _minPrice = 0;
        private int _maxPrice = 100000;
        private string _minPriceStr = "pmin";
        private string _maxPriceStr = "pmax";

        private string listStr = "view=list";

        private StreamWriter streamWriter = null;
        //private Tesseract tesseract = null;

        private int pagesCount = 0;

        private string dataFolder = "data\\";

        private List<InfoContainer> _infoContainers = new List<InfoContainer>();

        static void Main(string[] args)
        {

            PhantomJSOptions options = new PhantomJSOptions();
            //options.AddAdditionalCapability(OpenQA.Selenium.Remote.CapabilityType.Proxy,
            //    "http://88.247.131.221:8080");
            //var proxy = new Proxy();
            //proxy.HttpProxy = "https://31.28.6.185:8080";
            //options.Proxy = proxy;
            //options.BrowserVersion = "10.0.1";

            //

            Driver = new ChromeDriver();
            Driver.Manage().Window.Maximize();

            current = new Program();
            //Driver.Navigate().GoToUrl();
        }

        public Program()
        {
            streamWriter = new StreamWriter("log.txt");
            driverActions = new Actions(Driver);
            //tesseract = new Tesseract();
            //tesseract.SetVariable("tessedit_char_whitelist", "0123456789");
            this.Logic();
        }

        private void Logic()
        {
            //Driver.Navigate().GoToUrl(ConstructPath());
            //
            //GetScreenshot();

            //this.Login();
            this.Parse();
            

            //var scr = Driver.GetScreenshot();
            //scr.SaveAsFile("test.jpg", ScreenshotImageFormat.Jpeg);
            //Driver.Close();
        }

        private void CloseProgram()
        {
            Driver.Quit();
            streamWriter.Close();
        }

        private void GetScreenshot()
        {
            //var scr = Driver.GetScreenshot();
            //streamWriter.WriteLine(Driver.Url);
            //scr.SaveAsFile("test.jpg", ScreenshotImageFormat.Jpeg);
            CloseProgram();
        }

        private void GetPhone(IWebElement src)
        {
            var client = new WebClient();

            var pic = client.DownloadData(src.GetAttribute("src"));

            File.WriteAllBytes("tmp_phone", pic);

            //var image = Image.FromFile("tmp_phone");
        }

        private void GetScreenshot(string fileName)
        {
            return;
            var scr = Driver.GetScreenshot();
            //streamWriter.WriteLine(Driver.Url);
            scr.SaveAsFile(fileName, ScreenshotImageFormat.Jpeg);
        }

        private void InitScreenshot()
        {
            return;
            pageScr = Driver.GetScreenshot();
            pageScr.SaveAsFile("tmp_page.jpg", ScreenshotImageFormat.Jpeg);
            pageBmp = (Bitmap)Image.FromFile("tmp_page.jpg");
        }

        private Screenshot pageScr = null;
        private Bitmap pageBmp = null;
        private string GetNumber(Point loc, Size size)
        {
            Bitmap bmp = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(pageBmp, loc.X, loc.Y, size.Width, size.Height);

            //List<tessnet2.Word> number = tesseract.DoOCR(bmp, Rectangle.Empty);
            string result = string.Empty;
            //foreach (var word in number)
            //{
            //    result += word.Text;
            //}

            return result;
        }

        private void Login()
        {
            Driver.Navigate().GoToUrl("https://www.avito.ru/");

            var allDivA = Driver.FindElements(By.XPath(".//div/a"));
            bool isLogined = true;
            foreach (var button in allDivA)
            {
                if (button.Text == "Вход и регистрация")
                {
                    driverActions.MoveToElement(button);
                    driverActions.Click();
                    isLogined = false;
                }
            }

            if (isLogined)
            {
                this.GetScreenshot();
                return;
            }

            var allDivInputs = Driver.FindElements(By.XPath(".//div/input"));
            bool isLogin = false;
            bool isPasswrod = false;
            foreach (var inp in allDivInputs)
            {
                if (!isLogin && inp.GetAttribute("placeholder") == "Телефон или электронная почта")
                {
                    driverActions.MoveToElement(inp);
                    driverActions.Click();
                    driverActions.SendKeys("89814506291");
                    isLogin = true;
                }
                if (!isPasswrod && inp.GetAttribute("placeholder") == "Пароль")
                {
                    driverActions.MoveToElement(inp);
                    driverActions.Click();
                    driverActions.SendKeys("918171Ss");
                    isPasswrod = true;
                    if (isLogin)
                    {
                        driverActions.SendKeys(Keys.Enter);
                        break;
                    }
                }
                if (isLogin && isPasswrod)
                {
                    driverActions.SendKeys(Keys.Enter);
                    break;
                }
            }

            this.GetScreenshot();
        }

        private void Parse()
        {
            //GetScreenshot();
            List<string> paths = new List<string>();

            Driver.Navigate().GoToUrl(this.ConstructPath());
            this.FindPagesCount();

            int curIndex = 1;

            string dir = dataFolder;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string pathToFile = dir + "\\" + defCity + ".csv";
            if (File.Exists(pathToFile))
            {
                var streamReader = new StreamReader(pathToFile);
                string tmp = streamReader.ReadLine();
                while (!string.IsNullOrEmpty(tmp))
                {
                    _infoContainers.Add(new InfoContainer(tmp));
                    tmp = streamReader.ReadLine();
                }
                streamReader.Close();
            }
            streamWriter = new StreamWriter(pathToFile);

            while (curIndex <= pagesCount)
            {
                if (curIndex != 1)
                {
                    //Driver.Manage().Cookies.DeleteAllCookies();
                    string tmpPath = this.ConstructPath();
                    this.AddArgumentAtPath(ref tmpPath,
                        "p", curIndex.ToString());
                    Driver.Navigate().GoToUrl(tmpPath);
                }

                List<IWebElement> listElement = new List<IWebElement>();
                try
                {
                    //listElement = Driver.FindElements(By.XPath(".//h3/a"));
                    listElement.AddRange(Driver.FindElements(By.ClassName("js-catalog_before-ads")));
                    listElement.AddRange(Driver.FindElements(By.ClassName("js-catalog_after-ads")));
                    //listElement = Driver.FindElementsByXPath(".//div[@id='my_div']//a")
                }
                catch (Exception e)
                {
                    //Console.WriteLine(Driver.Url);
                    this.GetScreenshot();
                    return;
                }
                if (listElement != null)
                {
                    //if (listElement.Count == 0)
                    //{
                    //    break;
                    //}
                    var list = new List<IWebElement>();
                    foreach (var el in listElement)
                    {
                        list.AddRange(el.FindElements(By.ClassName("item")));
                    }
                    foreach (var l in list)
                    {
                        var titleButton = l.FindElement(By.XPath(".//h3/a"));
                        string path = titleButton.GetAttribute("href");

                        if (_infoContainers.Find((item) => { return item.path == path; }) == null)
                        {
                            _infoContainers.Add(new InfoContainer()
                            {
                                path = path,
                                title = titleButton.Text,
                                price = l.FindElement(By.ClassName("price")).Text,
                                description = l.FindElement(By.ClassName("specific-params")).Text,
                            });

                            //this.WriteToLog(_infoContainers[_infoContainers.Count - 1].ToString());
                        }

                    }
                }
                curIndex++;
            }

            foreach (var cont in _infoContainers)
            {
                this.WriteToLog(cont.ToString());
            }

            foreach (var cont in _infoContainers)
            {
                try
                {
                    this.ParseTargetPage(cont);
                }
                catch (Exception)
                {
                    break;
                }
            }

            streamWriter.Close();
            streamWriter = new StreamWriter(pathToFile);

            foreach (var cont in _infoContainers)
            {
                this.WriteToLog(cont.ToString());
            }

            this.CloseProgram();
        }

        private void ParseTargetPage(InfoContainer container)
        {
            Driver.Navigate().GoToUrl(container.path);
            var nameTmp = Driver.FindElement(By.ClassName("seller-info-value"));
            container.name = nameTmp.Text;

            var phoneTmp = Driver.FindElement(By.ClassName("item-phone-number"));
            //phoneTmp = phoneTmp.FindElement(By.XPath("//div/a"));

            phoneTmp.Click();
            //driverActions.MoveToElement(phoneTmp).Perform();
            //Thread.Sleep(2000);
            //driverActions.ClickAndHold();
            //Thread.Sleep(300);
            //driverActions.Release();
            //phoneTmp.Click();
            Thread.Sleep(2000);

            phoneTmp = phoneTmp.FindElement(By.XPath("//img"));

            container.number = phoneTmp.GetAttribute("src");
        }

        private void FindPagesCount()
        {
            var divAElements = Driver.FindElements(By.XPath(".//div/a"));
            if (divAElements.Count == 0)
            {
                GetScreenshot();
                return;
            }
            foreach (var el in divAElements)
            {
                if (el.Text == "Последняя")
                {
                    string url = el.GetAttribute("href");
                    int pInd = url.IndexOf("p=");
                    string pStr = "";
                    for (int i = pInd + 2; i < url.Length; i++)
                    {
                        if (url[i] == '&')
                        {
                            break;
                        }
                        pStr += url[i];
                    }

                    this.pagesCount = Math.Min(Convert.ToInt32(pStr), 3);
                }
            }
        }

        private string ConstructPath()
        {
            string result = string.Format(_avitoPath, defCity);
            isFirstArgument = true;
            if (_needMinPrice)
            {
                AddArgumentAtPath(ref result, _minPriceStr, _minPrice.ToString());
            }
            if (_needMaxPrice)
            {
                AddArgumentAtPath(ref result, _maxPriceStr, _maxPrice.ToString());
            }
            //AddArgumentAtPath(ref result, listStr);
            AddArgumentAtPath(ref result, "radius", "0");
            AddArgumentAtPath(ref result, "user", "1");

            return result;
        }

        private bool isFirstArgument = true;
        private void AddArgumentAtPath(ref string path, string arg, string value)
        {
            path += isFirstArgument ? "?" : "&";
            isFirstArgument = false;
            path += arg + "=" + value;
        }
        private void AddArgumentAtPath(ref string path, string fullArg)
        {
            path += isFirstArgument ? "?" : "&";
            isFirstArgument = false;
            path += fullArg;
        }

        private void WriteToLog(string text)
        {
            streamWriter.WriteLine(text);
        }

    }

    public class InfoContainer
    {
        public string path;
        public string title;
        public string number;
        public string price;
        public string description;
        public string name;

        public override string ToString()
        {
            string result = string.Empty;

            result = path + ";" + title + ";" + number + ";" + price + ";" +
                description + ";" + name;
            result = result.Replace(",", "_");
            result = result.Replace(" ", "_");
            result = result.Replace("__", "_");

            return result;
        }

        public InfoContainer()
        {

        }

        public InfoContainer(string target)
        {
            List<string> strings = new List<string>();
            int i = 0;
            string col = string.Empty;
            while (i < target.Length)
            {
                if (target[i] != ';')
                {
                    col += target[i];
                }
                else
                {
                    //string copy = col;
                    strings.Add(col);
                    col = string.Empty;
                }
                i++;
            }

            if (strings.Count == 0)
            {
                return;
            }

            while (strings.Count < 6)
            {
                strings.Add("");
            }
            this.path = strings[0];
            this.title = strings[1];
            this.number = strings[2];
            this.price = strings[3];
            this.description = strings[4];
            this.name = strings[5];
        }
    }
}
