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
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;

namespace AvitoAppConsole
{
    class Program
    {
        public static Program current;
        public static ChromeDriver Driver;
        private Actions driverActions;

        private string defCity = "kaliningrad";

        private string _avitoPath = "https://m.avito.ru/{0}/avtomobili";

        private bool _needMinPrice = false;
        private bool _needMaxPrice = false;
        private int _minPrice = 0;
        private int _maxPrice = 100000;
        private string _minPriceStr = "pmin";
        private string _maxPriceStr = "pmax";

        private string listStr = "view=list";

        private StreamWriter streamWriter = null;
        private StreamWriter logWriter = null;
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
            logWriter = new StreamWriter("log.txt");
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
            //this.Parse();
            this.ParseMobVer();

            //var cont = this.ParseTargetPage("https://m.avito.ru/kaliningrad/avtomobili/bmw_x3_2008_1537953362");

            //var cont = new InfoContainer() { path = "https://m.avito.ru/kaliningrad/avtomobili/bmw_x3_2008_1537953362" };

            //this.ParseTargetPage(cont);


            //var scr = Driver.GetScreenshot();
            //scr.SaveAsFile("test.jpg", ScreenshotImageFormat.Jpeg);
            //Driver.Close();
        }

        private void CloseProgram()
        {
            Driver.Quit();
            streamWriter.Close();
            this.logWriter.Close();
        }

        private void SaveToFile()
        {
            foreach (var cont in _infoContainers)
            {
                this.WriteToLog(cont.ToString());
            }
        }

        private void GetScreenshot()
        {
            //var scr = Driver.GetScreenshot();
            //streamWriter.WriteLine(Driver.Url);
            //scr.SaveAsFile("test.jpg", ScreenshotImageFormat.Jpeg);
            CloseProgram();
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

            bool isLogined = false;
            while (!isLogined)
            {
                var allDivA = Driver.FindElements(By.XPath(".//div/a"));

                isLogined = true;
                foreach (var button in allDivA)
                {
                    if (button.Text == "Вход и регистрация")
                    {
                        //driverActions.Click();
                        isLogined = false;
                        break;
                    }
                }
                Thread.Sleep(2000);
            }
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

                        //if (_infoContainers.Find((item) => { return item.path == path; }) == null)
                        //{
                        //    _infoContainers.Add(new InfoContainer()
                        //    {
                        //        path = path,
                        //        title = titleButton.Text,
                        //        price = l.FindElement(By.ClassName("price")).Text,
                        //        description = l.FindElement(By.ClassName("specific-params")).Text,
                        //    });

                        //    //this.WriteToLog(_infoContainers[_infoContainers.Count - 1].ToString());
                        //}

                        if (_infoContainers.Find((item) => { return item.url == path; }) == null)
                        {
                            _infoContainers.Add(this.ParseTargetPage(path));
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

        private void ParseMobVer()
        {
            Driver.Navigate().GoToUrl(this.ConstructPath());

            int curIndex = 1;
            var start = DateTime.Now;
            int totalNew = 0;

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

            try
            {
                while (true)
                {
                    string url = this.ConstructPath();
                    if (curIndex != 1)
                    {
                        this.AddArgumentAtPath(ref url, "p", curIndex.ToString());
                    }
                    Driver.Navigate().GoToUrl(url);

                    if ((curIndex != 1 && !Driver.Url.Contains("&p=")) || curIndex == 2)
                    {
                        break;
                    }

                    var list = Driver.FindElements(By.ClassName("b-item-wrapper"));
                    foreach (var el in list)
                    {
                        string locUrl = el.FindElement(By.TagName("a")).GetAttribute("href");

                        if (_infoContainers.Find((elem) => { return elem.url == locUrl; }) == null)
                        {
                            _infoContainers.Add(new InfoContainer() { url = locUrl });
                            _infoContainers[_infoContainers.Count - 1].price =
                                el.FindElement(By.ClassName("item-price-value")).Text;
                        }
                    }

                    foreach (var info in _infoContainers)
                    {
                        if (info.title == string.Empty)
                        {
                            info.UpdateContainer(this.ParseTargetPage(info.url));
                            totalNew++;
                        }
                        else
                        {
                            if (!TestUrl(info.url))
                            {

                            }
                        }
                    }

                    curIndex++;
                }
            }
            catch (Exception e)
            {
                logWriter.WriteLine(e.ToString());
            }
            finally
            {
                this.SaveToFile();
                this.logWriter.WriteLine("Total pages : " + (curIndex - 1).ToString());
                var totalTime = (DateTime.Now - start);
                this.logWriter.WriteLine("Total time : " +
                    Math.Round(totalTime.TotalHours) + "h " +
                    Math.Round(totalTime.TotalMinutes) + "m " +
                    Math.Round(totalTime.TotalSeconds % 60) + "s : " +
                    totalTime.TotalSeconds + "ts");
                    
                this.logWriter.WriteLine("Total new : " + totalNew.ToString());
                this.logWriter.WriteLine("Total pages : " + _infoContainers.Count.ToString());
            }

            this.CloseProgram();
        }

        private void ParseTargetPage(InfoContainer container)
        {
            Driver.Navigate().GoToUrl(container.url);
            var nameTmp = Driver.FindElement(By.ClassName("seller-info-value"));
            container.name = nameTmp.Text;

            var phoneTmp = Driver.FindElement(By.ClassName("write-message-btn"));
            //phoneTmp = phoneTmp.FindElement(By.XPath("//div/a"));

            phoneTmp.Click();
            //driverActions.MoveToElement(phoneTmp).Perform();
            //Thread.Sleep(2000);
            //driverActions.ClickAndHold();
            //Thread.Sleep(300);
            //driverActions.Release();
            //phoneTmp.Click();
            Thread.Sleep(2000);

            phoneTmp = Driver.FindElement(By.XPath(".//div[@class='action-phone']/button"));
            phoneTmp.Click();
            Thread.Sleep(2000);

            container.number = Driver.FindElement(By.ClassName("action-phone__result")).Text;
        }

        public InfoContainer ParseTargetPage(string url)
        {
            var result = new InfoContainer();
            result.url = url;

            Driver.Navigate().GoToUrl(url);

            var tmp = Driver.FindElement(By.ClassName("single-item-header"));
            result.title = tmp.Text;
            tmp = Driver.FindElement(By.ClassName("price-value"));
            result.price = tmp.Text;
            tmp = Driver.FindElement(By.ClassName("info-params"));
            var tmp2 = tmp.FindElements(By.ClassName("param"));
            string descStr = string.Empty;
            foreach (var desc in tmp2)
            {
                descStr += desc.Text + "_";
            }
            result.description = descStr;

            tmp = Driver.FindElement(By.ClassName("person-name"));
            result.name = tmp.Text;
            result.userId = GetUserId(tmp.GetAttribute("href"));

            tmp = Driver.FindElement(By.ClassName("amw-test-item-click"));
            tmp.Click();
            Thread.Sleep(2000);

            tmp = Driver.FindElement(By.ClassName("amw-test-item-click"));
            result.number = tmp.Text;

            return result;
        }

        private bool TestUrl(string url)
        {
            Driver.Navigate().GoToUrl(url);
            return Driver.Url == url;
        }

        private string GetUserId(string url)
        {
            string result = string.Empty;
            int index = url.IndexOf("user/") + 4;
            url = url.Remove(0, index);
            index = url.IndexOf("/");
            result = url.Remove(index);

            return result;
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
        public string url;
        public string title;
        public string number;
        public string price;
        public string description;
        public string name;

        public string userId;

        public override string ToString()
        {
            string result = string.Empty;

            result = url + ";" + title + ";" + number + ";" + price + ";" +
                description + ";" + name + ";" + userId ;
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

            while (strings.Count < 7)
            {
                strings.Add("");
            }
            this.url = strings[0];
            this.title = strings[1];
            this.number = strings[2];
            this.price = strings[3];
            this.description = strings[4];
            this.name = strings[5];
            this.userId = strings[6];
        }

        public void UpdateContainer(InfoContainer c)
        {
            this.url = c.url;
            this.title = c.title;
            this.price = c.price;
            this.name = c.name;
            this.number = c.number;
            this.description = c.description;
            this.userId = c.userId;
        }
    }
}
