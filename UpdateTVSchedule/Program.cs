using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UpdateTVSchedule.TVPocketObject;

namespace UpdateTVSchedule
{
    class Program
    {
        private static List<TVChannel> listChannels = new List<TVChannel>();
        private static List<TVProgram> listTVPrograms = new List<TVProgram>();
        private static HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
        private static string CurrentDate = DateTime.Now.ToString("dd/MM/yyyy");

        static void Main(string[] args)
        {
            Console.WriteLine("Updating tv schedules");
            if (!LoadListChannelFromJSON(AppDomain.CurrentDomain.BaseDirectory + @"\Data\listres.txt"))
                return;

            if (!GetTVSchedules())
                return;

            UploadToServerAndDatabase();

            Console.WriteLine("Done!");
            //Console.ReadLine();
        }

        private static void UploadToServerAndDatabase()
        {
            try
            {
                Console.WriteLine("Uploading to server and database...");
                string json = JsonConvert.SerializeObject(listTVPrograms);
                string jsonchannel = AppDomain.CurrentDomain.BaseDirectory + @"\JSON\" + DateTime.Now.ToString("ddMMyyyy") + ".json";
                if (File.Exists(jsonchannel))
                {
                    File.Delete(jsonchannel);
                }
                File.WriteAllText(jsonchannel, json, Encoding.UTF8);
                UploadFile(AppDomain.CurrentDomain.BaseDirectory + @"\JSON\" + DateTime.Now.ToString("ddMMyyyy") + ".json", "ftp://u345942156.newbyte@ftp.huyqta.esy.es/tvpocket/" + DateTime.Now.ToString("ddMMyyyy") + ".json", "u345942156.newbyte", "anhhuy");

                string urlAPI = @"http://huyqta.esy.es/index.php/api/programs/ReadJsonToDB/" + DateTime.Now.ToString("ddMMyyyy") + "/format/json";

                // Create a request for the URL. 
                WebRequest request = WebRequest.Create(urlAPI);
                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;
                // Get the response.
                WebResponse response = request.GetResponse();
                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Clean up the streams and the response.
                reader.Close();
                response.Close();

                List<string> listChannelSuccess = listTVPrograms.Select(p => p.refchannel.ToString()).Distinct().ToList();

                string dataChannelSuccess = string.Join(",", listChannelSuccess);
                string dataChannelSuccessPath = AppDomain.CurrentDomain.BaseDirectory + @"\JSON\ChannelSuccess" + DateTime.Now.ToString("ddMMyyyy") + ".txt";
                if (File.Exists(dataChannelSuccessPath))
                {
                    string currentChannelSuccess = File.ReadAllText(dataChannelSuccessPath, Encoding.UTF8);
                    dataChannelSuccess += "," + currentChannelSuccess;
                    File.Delete(dataChannelSuccessPath);
                }
                File.WriteAllText(dataChannelSuccessPath, dataChannelSuccess, Encoding.UTF8);

                string listChannelFail = "";

                foreach (TVChannel channel in listChannels)
                {
                    if (listChannelSuccess.IndexOf(channel.id.ToString()) < 0)
                    {
                        listChannelFail += channel.id.ToString() + ":" + channel.name.ToString() + Environment.NewLine;
                    }
                }

                string dataChannelFail = listChannelFail;
                string dataChannelFailPath = AppDomain.CurrentDomain.BaseDirectory + @"\JSON\ChannelFail" + DateTime.Now.ToString("ddMMyyyy") + ".txt";
                if (File.Exists(dataChannelFailPath))
                {
                    File.Delete(dataChannelFailPath);
                }
                File.WriteAllText(dataChannelFailPath, dataChannelFail, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Methods to upload file to FTP Server
        /// </summary>
        /// <param name="_FileName">local source file name</param>
        /// <param name="_UploadPath">Upload FTP path including Host name</param>
        /// <param name="_FTPUser">FTP login username</param>
        /// <param name="_FTPPass">FTP login password</param>
        private static void UploadFile(string _FileName, string _UploadPath, string _FTPUser, string _FTPPass)
        {
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_UploadPath);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential(_FTPUser, _FTPPass);

            // Copy the contents of the file to the request stream.
            StreamReader sourceStream = new StreamReader(_FileName);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            sourceStream.Close();
            request.ContentLength = fileContents.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Console.WriteLine("Upload File Complete, status {0}", response.StatusDescription);

            response.Close();
        }

        private static bool GetTVSchedules()
        {
            try
            {
                Console.WriteLine("Get TV schedules...");
                string dataChannelSuccessPath = AppDomain.CurrentDomain.BaseDirectory + @"\JSON\ChannelSuccess" + DateTime.Now.ToString("ddMMyyyy") + ".txt";
                List<string> listChannelSuccess = new List<string>();
                if (File.Exists(dataChannelSuccessPath))
                {
                    string dataChannelSuccess = File.ReadAllText(dataChannelSuccessPath, Encoding.UTF8);
                    listChannelSuccess = dataChannelSuccess.Split(',').ToList();
                }

                foreach (TVChannel channel in listChannels)
                {
                    if (listChannelSuccess.IndexOf(channel.id.ToString()) > -1)
                        continue;

                    List<string> urlcrawls = channel.urlcrawl.Split('|').ToList();
                    foreach (string urlcrawl in urlcrawls)
                    {
                        if (urlcrawl.IndexOf("www.mytv.com.vn") > -1)
                        {
                            if (AddTVSchedulesFromMyTV(channel, urlcrawl)) break;
                        }
                        if (urlcrawl.IndexOf("www.htvonline.com.vn") > -1)
                        {
                            if (AddTVSchedulesFromHTVOnline(channel, urlcrawl)) break;
                        }
                        if (urlcrawl.IndexOf("vtv.vn") > -1)
                        {
                            if (AddTVSchedulesFromVTVOnline(channel, urlcrawl)) break;
                        }
                    }
                }
                if (listTVPrograms.Count() > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private static bool AddTVSchedulesFromMyTV(TVChannel channel, string urlcrawl)
        {
            try
            {
                string data = GetMessage(urlcrawl.Replace("%", CurrentDate));

                if (data != null)
                {
                    document.LoadHtml(data);

                    string[] program = document.DocumentNode.Descendants("p").Select(n => n.InnerText).ToArray();
                    string[] time = document.DocumentNode.Descendants("strong").Select(n => n.InnerText).ToArray();
                    if (program.Count() > 0)
                    {
                        for (int i = 0; i < program.Count(); i++)
                        {
                            TVProgram tvProgram = new TVProgram();
                            tvProgram.refchannel = channel.id;
                            tvProgram.dateStart = DateTime.Now.ToString("yyyy-MM-dd").Trim();
                            tvProgram.timeStart = time[i].Trim();
                            tvProgram.duration = 0;
                            tvProgram.posterurl = "";
                            tvProgram.name = program[i].Replace(time[i], "").Trim();
                            listTVPrograms.Add(tvProgram);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool AddTVSchedulesFromHTVOnline(TVChannel channel, string urlcrawl)
        {
            try
            {
                string datetime = DateTime.Now.ToString("dd-MM-yyyy");
                urlcrawl = urlcrawl.Replace("dd-mm-yyyy", datetime);
                string data = GetMessage(urlcrawl);

                if (data != null)
                {
                    document.LoadHtml(data);

                    string[] program = document.DocumentNode.Descendants("p").Select(n => n.InnerText).ToArray();
                    string[] time = document.DocumentNode.Descendants("span").Select(n => n.InnerText).ToArray();
                    if (program.Count() > 0)
                    {
                        for (int i = 0; i < program.Count(); i++)
                        {
                            TVProgram tvProgram = new TVProgram();
                            tvProgram.refchannel = channel.id;
                            tvProgram.dateStart = DateTime.Now.ToString("yyyy-MM-dd").Trim();
                            tvProgram.timeStart = time[i].Trim();
                            tvProgram.duration = 0;
                            tvProgram.posterurl = "";
                            tvProgram.name = program[i].Replace(time[i], "").Trim();
                            listTVPrograms.Add(tvProgram);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool AddTVSchedulesFromVTVOnline(TVChannel channel, string urlcrawl)
        {
            try
            {
                string data = GetMessage(urlcrawl.Replace("%", "0"));

                if (data != null)
                {
                    document.LoadHtml(data);

                    string[] desc = document.DocumentNode.Descendants("b").Select(n => n.InnerText).ToArray();
                    string[] time = document.DocumentNode.Descendants("p").Select(n => n.InnerText).ToArray();
                    string[] program = document.DocumentNode.Descendants("span").Select(n => n.InnerText).ToArray();
                    if (program.Count() > 0)
                    {
                        for (int i = 0; i < program.Count(); i++)
                        {
                            TVProgram tvProgram = new TVProgram();
                            tvProgram.refchannel = channel.id;
                            tvProgram.dateStart = DateTime.Now.ToString("yyyy-MM-dd").Trim();
                            tvProgram.timeStart = time[i].Trim();
                            tvProgram.duration = 0;
                            tvProgram.posterurl = "";
                            string parseprogram = program[i].Trim() != "" ? ": " + program[i].Replace(time[i], "").Trim() : "";
                            tvProgram.name = desc[i].Trim() + parseprogram.Trim();
                            listTVPrograms.Add(tvProgram);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool LoadListChannelFromJSON(string filepath)
        {
            Console.WriteLine("Load list channel from JSON...");
            if (!File.Exists(filepath))
                return false;
            string strChannels = File.ReadAllText(filepath, Encoding.UTF8);
            listChannels = JsonConvert.DeserializeObject<List<TVChannel>>(strChannels);
            if (listChannels.Count < 1)
                return false;

            return true;
        }

        private static string GetMessage(string endPoint)
        {
            WebClient wc = new WebClient();
            if (endPoint.IndexOf("www.htvonline.com.vn") > -1 || endPoint.IndexOf("vtv.vn") > -1)
            {
                wc.Encoding = Encoding.UTF8;
            }

            string res = wc.DownloadString(endPoint);

            if (endPoint.IndexOf("channels/GetAllChannels") > -1)
            {
                return res;
            }
            if (endPoint.IndexOf("vtv.vn") > -1)
            {
                string rett = WebUtility.HtmlDecode(res);
                return rett;
            }
            if (endPoint.IndexOf("www.htvonline.com.vn") > -1)
            {
                if (res.IndexOf("Dữ liệu không có") > -1)
                {
                    res = null;
                }
                else
                {
                    res = res.Replace("<script>\n\t$(document).ready(function($) {\n        $('#div_schedule').mCustomScrollbar();\n    });\t\t \n</script>\n", "");
                }
                return res;
            }
            return (string)JsonConvert.DeserializeObject(res);
        }
    }
}
