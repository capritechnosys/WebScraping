using HtmlAgilityPack;
using IronWebScraper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebScrapingConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string mainUrl = "";
            var scraper = new MainPageScraper(mainUrl);
            scraper.Start();
            var listofLinks = scraper.ListLinks;
            scraper.Stop();
            foreach (var href in listofLinks)
            {
                SubPageScraper subScraper = new SubPageScraper(href);
                subScraper.Start();
                var listOfInfo = subScraper.listInfo;
                subScraper.Stop();
                foreach (var info in listOfInfo)
                {
                    SocialScraper social = new SocialScraper(info);
                    social.GetSocialInfoAndUpdateInfo();
                }
            }
        }
    }
    public class MyWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            return request;
        }
    }

    public class SocialScraper
    {
        public Info Info { get; set; }

        public SocialScraper(Info info)
        {
            Info = info;
        }

        public void GetSocialInfoAndUpdateInfo()
        {
            if(Info.Website != null)
            {
                HtmlDocument doc = new HtmlDocument();

                try
                {
                    HtmlWeb hw = new HtmlWeb();
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    doc = hw.Load(Info.Website);
                }
                catch
                {
                    try
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        var data = new MyWebClient().DownloadString(Info.Website);
                        doc.LoadHtml(data);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    
                }
                
                var nodes = doc.DocumentNode.SelectNodes("//a[@href]")?.ToList<HtmlAgilityPack.HtmlNode>();
                if(nodes != null)
                {
                    string facebook = nodes.SelectMany(x => x.Attributes).Where(x => x.Name == "href" && x.Value.Contains("facebook")).Select(x => x.Value).FirstOrDefault();
                    string instagram = nodes.SelectMany(x => x.Attributes).Where(x => x.Name == "href" && x.Value.Contains("instagram")).Select(x => x.Value).FirstOrDefault();
                    string twitter = nodes.SelectMany(x => x.Attributes).Where(x => x.Name == "href" && x.Value.Contains("twitter")).Select(x => x.Value).FirstOrDefault();
                    Info.Facebook = facebook;
                    Info.Twitter = twitter;
                    Info.Instagram = instagram;
                }                
            }            
            using (var context = new InfoDbContext())
            {
                context.Info.Add(Info);
                context.SaveChanges();
            }
        }

    }

    public class MainPageScraper : WebScraper
    {
        public List<string> ListLinks { get; set; }
        private string MainUrl { get; set; }

        public MainPageScraper(string mainUrl)
        {
            MainUrl = mainUrl;
        }
        public MainPageScraper()
        {
            ListLinks = new List<string>();
        }
        public override void Init()
        {
            this.LoggingLevel = WebScraper.LogLevel.All;
            this.Request(MainUrl, Parse);
        }

        public override void Parse(Response response)
        {
            for (int i = 1; i <= response.XPath("/html/body/div[1]/div[3]/div/div").Length; i++)
            {
                for (int j = 1; j <= response.XPath(string.Format("/html/body/div[1]/div[3]/div/div[{0}]/ul/li", i)).Length; j++)
                {
                    var r = response.XPath(string.Format("/html/body/div[1]/div[3]/div/div[{0}]/ul/li[{1}]/a", i, j));
                    var href = r[0].ChildNodes[0].Attributes["href"];
                    ListLinks.Add(href);
                }
            }            
            
        }        
    }

    public class SubPageScraper : WebScraper
    {
        public const string subCommonDomain = "";
        public List<Info> listInfo { get; set; }
        public string subUrl { get; set; }

        public SubPageScraper(string url)
        {
            this.subUrl = url;
            this.listInfo = new List<Info>();
        }
        public override void Init()
        {
            this.LoggingLevel = WebScraper.LogLevel.All;
            string url = string.Format("{0}{1}", subCommonDomain, this.subUrl);
            this.Request(url, Parse);
        }

        public override void Parse(Response response)
        {
            string name = null, website = null, email = null, address = null, phone = null;
            try
            {
                name = response.XPath("/html/body/div[1]/div[3]/div[2]/div[2]/p[1]/b").Select(x => x.TextContentClean).FirstOrDefault();
            }
            catch
            {
            }
            try
            {
                website = response.XPath("/html/body/div[1]/div[3]/div[2]/div[2]/p[2]/a")[0].ChildNodes[0].Attributes["href"];
            }
            catch
            {
            }
            try
            {
                email = response.XPath("/html/body/div[1]/div[3]/div[2]/div[2]/div[1]/p[2]/a").Select(x => x.TextContentClean).FirstOrDefault();
            }
            catch
            {
            }
            try
            {
                address = response.XPath("/html/body/div[1]/div[3]/div[2]/div[2]/div/a")[0].ChildNodes[0].TextContentClean;
            }
            catch
            {
            }

            try
            {
                phone = response.XPath("/html/body/div[1]/div[3]/div[2]/div[2]/div/p")[0].ChildNodes[0].TextContentClean;
            }
            catch
            {
            }

            Info info = new Info()
            {
                Name = name,
                Website = website,
                Email = email,
                Phone = phone,
                Address = address,
            };

            listInfo.Add(info);
        }        
    }
}
