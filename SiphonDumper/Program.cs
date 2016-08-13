using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SiphonDumper
{
    class Program
    {
        const string Cookie = "";
        const string DumpLocation = "./Dumps/";
        const string BeginLink = "<a href=\"exploits.php?id=";
        const string BeginHTTP = "\">http://";
        const string EndClassTag = "\">";
        const string ExploitLink = "http://siph0n.in/exploits.php?id=";
        const string WidthTag62_2016 = "width=\"62\">2016-";

        const int StartPage = 1;
        const int EndPage = 100;

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Dumping pages from " + StartPage + " - " + EndPage);

            if (!Directory.Exists(DumpLocation))
                Directory.CreateDirectory(DumpLocation);

            Console.WriteLine("Dumping the first 1 pages...");
            for (int i = StartPage; i <= EndPage; i++)
            {
                Console.WriteLine("Dumping page " + i);
                HttpWebRequest webRequest = CreateWebRequest("http://siph0n.in/leaks.php?page=" + i, null);

                string DateTag = "UnknownDate";
                WebResponse response = webRequest.GetResponse();
                using(StreamReader SR = new StreamReader(response.GetResponseStream()))
                {
                    string line = "";

                    while ((line = SR.ReadLine()) != null)
                    {
                        if (line.Contains(BeginLink))
                        {
                            string LinkId = ReadTill(line.Substring(line.IndexOf(BeginLink) + BeginLink.Length), '\"');
                            string Name = line.Substring(line.LastIndexOf(EndClassTag) + EndClassTag.Length).Replace("</a></td>", "");

                            if (Name[Name.Length - 1] == ' ')
                                Name = Name.Substring(0, Name.Length - 1);

                            string EndFileName = Name.Replace("http://", "").Replace(".", "_").Replace("/", "").Replace("\\", "").Replace(":", "_").Replace("?", "_").Replace("|", "_") + ".txt";
                            
                            Console.WriteLine("\tDumping " + Name);

                            bool success = false;

                            string DumpLink = ExploitLink + LinkId;
                            int Tries = 0;

                            while (!success)
                            {
                                try
                                {
                                    Tries++;

                                    if (Tries > 5)
                                    {
                                        Console.WriteLine("[skipped] Failed to fetch " + DumpLink);
                                        break;
                                    }

                                    WebRequest RequestDump = CreateWebRequest(DumpLink, webRequest.RequestUri.OriginalString);
                                    WebResponse DumpResponse = RequestDump.GetResponse();
                                    Stream DumpStream = DumpResponse.GetResponseStream();

                                    string DirDump = DumpLocation + DateTag;

                                    if (!Directory.Exists(DirDump))
                                        Directory.CreateDirectory(DirDump);

                                    string DumpText = "";

                                    using (StreamReader DumpSR = new StreamReader(DumpStream))
                                    {
                                        DumpText = DumpSR.ReadToEnd();
                                        
                                        DumpText = DumpText.Replace("&quot;", "\"");
                                        DumpText = DumpText.Replace("&lt;", "<");
                                        DumpText = DumpText.Replace("&gt;", ">");
                                        DumpText = DumpText.Replace("&amp;", "&");
                                        DumpText = DumpText.Replace("&apos;", "'");
                                        DumpText = DumpText.Replace("&eacute;", "é");
                                        DumpText = DumpText.Replace("&circ;", "^");
                                        DumpText = DumpText.Replace("&tilde;", "~");
                                        DumpText = DumpText.Replace("&ensp;", " ");
                                        DumpText = DumpText.Replace("&emsp;", " ");
                                        DumpText = DumpText.Replace("&thinsp;", " ");
                                        DumpText = DumpText.Replace("&zwnj;", "");
                                        DumpText = DumpText.Replace("&zwj;", "");
                                        DumpText = DumpText.Replace("&lrm;", "");
                                        DumpText = DumpText.Replace("&rlm;", "");
                                        DumpText = DumpText.Replace("&ndash;", "–");
                                        DumpText = DumpText.Replace("&mdash;", "—");
                                        DumpText = DumpText.Replace("&lsquo;", "‘");
                                        DumpText = DumpText.Replace("&rsquo;", "’");
                                        DumpText = DumpText.Replace("&sbquo;", "‚");
                                        DumpText = DumpText.Replace("&ldquo;", "“");
                                        DumpText = DumpText.Replace("&rdquo;", "”");
                                        DumpText = DumpText.Replace("&bdquo;", "„");
                                        DumpText = DumpText.Replace("&permil;", "‰");
                                        DumpText = DumpText.Replace("&lsaquo;", "‹");
                                        DumpText = DumpText.Replace("&rsaquo;", "›");
                                        DumpText = DumpText.Replace("&euro;", "€");
                                    }

                                    //remove the cloudflare email obfuscation
                                    string Decrypted = RemoveCloudflareMailProtection(DumpText);

                                    File.WriteAllText(DirDump + "//" + EndFileName, Decrypted);
                                    success = true;
                                }
                                catch(Exception ex)
                                {
                                    //Console.WriteLine(ex.Message);
                                    Console.WriteLine("Failed opening " + DumpLink);
                                }
                            }

                            DateTag = "UnknownDate";
                        }
                        else if (line.Contains(WidthTag62_2016))
                        {
                            int index = line.IndexOf(WidthTag62_2016) - 5;
                            if(index > 0)
                            {
                                DateTag = ReadTill(line.Substring(index + WidthTag62_2016.Length), '<');
                            }
                        }
                    }

                    
                }
            }
            Console.WriteLine("Done...");
            Console.ReadLine();
        }

        static HttpWebRequest CreateWebRequest(string Uri, string Referer)
        {
            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(Uri);
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:48.0) Gecko/20100101 Firefox/48.0";
            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            webRequest.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            webRequest.Headers.Add("DNT", "1");

            if(Referer != null)
                webRequest.Referer = Referer;

            webRequest.Headers.Add("Cookie", Cookie);

            return webRequest;
        }

        static string ReadTill(string Value, char Till)
        {
            int length = 0;

            for (int i = 0; i < Value.Length; i++)
            {
                if (Value[i] == Till)
                {
                    length = i;
                    break;
                }
            }

            return Value.Substring(0, length);
        }

        private static string DecodeCloudflareMail(string Input)
        {
            int r = Convert.ToInt32(Input.Substring(0, 2), 16);
            string output = "";

            for (int i = 2; i < Input.Length; i += 2)
            {
                int Char = Convert.ToInt32(Input.Substring(i, 2), 16) ^ r;
                output += (char)Char;
            }
            return output;
        }

        private static string RemoveCloudflareMailProtection(string Input)
        {
            using (StreamReader DumpSR = new StreamReader(new MemoryStream(ASCIIEncoding.UTF8.GetBytes(Input))))
            {
                StringBuilder FinalDumpTest = new StringBuilder();

                string DumpLine = "";

                string CloudflareBegin = "<a class=\"__cf_email__";
                string CloudflareEnd = "catch(u){}}()/* ]]> */</script>";

                string CloudflareEmailBegin = "data-cfemail=\"";
                string CloudflareEmailEnd = "\">[";

                while ((DumpLine = DumpSR.ReadLine()) != null)
                {
                    //decrypt/remove multiple line protections in 1 line hence the ghetto while(true)
                    while (true)
                    {
                        int beginIndex = DumpLine.IndexOf(CloudflareBegin);

                        if (beginIndex < 0)
                        {
                            break;
                        }

                        int endIndex = DumpLine.IndexOf(CloudflareEnd, beginIndex);

                        if (endIndex <= 0)
                        {
                            break;
                        }

                        string DataLine = DumpLine.Substring(beginIndex, (endIndex + CloudflareEnd.Length) - beginIndex);
                        int BeginEmailDataIndex = DataLine.IndexOf(CloudflareEmailBegin);

                        if (BeginEmailDataIndex < 0)
                        {
                            break;
                        }

                        int EndEmailDataIndex = DataLine.IndexOf(CloudflareEmailEnd, BeginEmailDataIndex);

                        if (EndEmailDataIndex <= 0)
                        {
                            break;
                        }

                        BeginEmailDataIndex += CloudflareEmailBegin.Length;
                        int len = EndEmailDataIndex - BeginEmailDataIndex;

                        string EmailData = DataLine.Substring(BeginEmailDataIndex, len);
                        string decrypted = DecodeCloudflareMail(EmailData);

                        //replace encrypted data + javascript with decrypted string
                        DumpLine = DumpLine.Replace(DataLine, decrypted);
                    }
                    FinalDumpTest.AppendLine(DumpLine);
                }
                return FinalDumpTest.ToString();
            }
        }
    }
}