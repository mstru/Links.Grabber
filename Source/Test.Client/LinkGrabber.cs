using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Test.Client
{
    /// <summary>
    /// Trieda určená na analýzu webovej stránky
    /// </summary>
    class LinkGrabber
    {
        BetterWebClient client = null;
        Dictionary<string, string> VisitedLinks;
        Settings Settings;

        int PassedLinksCounter = 0;
        int FailedLinksCounter = 0;

        public LinkGrabber(Settings Settings)
        {
            this.Settings = Settings;
            VisitedLinks = new Dictionary<string, string>();
        }

        /// <summary>
        /// Extrakcia URL odkazov
        /// </summary>
        public void Start()
        {
            List<string> linksInWebPage = new List<string>();
            string pathToReportSubLinks = string.Empty;

            if (!(System.IO.Directory.Exists(Settings.ReportDestinationFolder)))
                System.IO.Directory.CreateDirectory(Settings.ReportDestinationFolder);

            UpdateProgressBar(0);
            GrabAndStoreWebPage(Settings.URL, Settings.ReportDestinationFolder, ref linksInWebPage, ref pathToReportSubLinks, ref client, true);

            if (Settings.Depth > 1)
            {
                UpdateProgressBar(55);

                //Rekurzívne sťahovanie odkazov
                RecursivelyDownloadUrl(linksInWebPage, pathToReportSubLinks, Settings.Depth - 1, true);
            }

            UpdateProgressBar(100);
            ShowMessageBox(string.Format("Test finished {0}{0}PASSED: '{1}' FAILED: '{2}'", Environment.NewLine, PassedLinksCounter, FailedLinksCounter));
            UpdateStatusText(string .Format("Test Complete! PASSED: {0} FAILED: {1}", PassedLinksCounter, FailedLinksCounter));
            ReenableGrabButton();
        }

        public void GrabAndStoreWebPage(string url, string path, ref List<string> linksInWebBage, ref string pathToReportSubLinks, ref BetterWebClient client, bool mainPage = false)
        {
            //Čistenie URL
            char[] illegalEnds = new char[] { '/', '#', '?', '&' };
            while (illegalEnds.Contains(url[url.Length - 1]))
            {
                url = url.Substring(0, url.Length - 1);
            }

            //preto lebo navštívené stránky, znovu nenavštevovať
            if (VisitedLinks.ContainsKey(url))
            {
                pathToReportSubLinks = VisitedLinks[url];
                return;
            }

            UpdateProgressText("---------------------------" + Environment.NewLine + "Trying to download: " + url);

            StringBuilder htmlContent = new StringBuilder();
            using (client = new BetterWebClient())
            {
                try
                {
                    client.Credentials = new NetworkCredential(
                        Settings.UserName, 
                        Settings.Password);
                    htmlContent.Append(client.DownloadString(url));
                    UpdateProgressTest(client.StatusCode().ToString(), url, path, "PASSED");
                }
                catch (WebException ex)
                {
                    //999: Non-standard
                    //This non-standard code is returned by some sites (e.g. linkedin) which do not permit scanning.
                    if (mainPage)
                    {
                        ShowMessageBox(ex.Message);
                    }
                    UpdateProgressTest(ex.Message, url, path, "FAILED");
                    return;
                }
            }

            //Stiahne DOM pre stránku
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent.ToString());

            ResolveRelativePathsForLinksInTag("//a", "href", url, ref doc, linksInWebBage);
            //ResolveRelativePathsForLinksInTag("//script", "src", url, ref doc, null);
            //ResolveRelativePathsForLinksInTag("//img", "src", url, ref doc, null);
            //ResolveRelativePathsForLinksInTag("//link", "href", url, ref doc, null);

            string htmlFileName = string.Empty;
            var htmlnode = doc.DocumentNode.Descendants("title").SingleOrDefault();
            if (htmlnode != null)
            {
                htmlFileName = Utilities.RemoveIllegalCharactersFromFileName(htmlnode.InnerText);
            }
            if (htmlFileName.Length < 1)
            {
                int lastIndex = url.LastIndexOf('/');
                htmlFileName = Utilities.RemoveIllegalCharactersFromFileName(url.Substring(lastIndex + 1));
            }
            htmlFileName = htmlFileName.Length > 250 ? htmlFileName.Substring(0, 245) : htmlFileName;

            string pathToSavedoc = path + @"\" + htmlFileName + "_" + ((new Random()).Next(1, 99999)) + "_Grabbed.html";

            doc.Save(pathToSavedoc);

            pathToReportSubLinks = path;

            //Pridanie stránky do navštívených stránok
            VisitedLinks.Add(url, pathToReportSubLinks);

            UpdateProgressText("Success! " + Environment.NewLine + "Saved as : " + path);
            //System.IO.File.WriteAllText(Settings.DestinationFolder + @"\" + htmlFileName, doc.HtmlDocument);
        }

        private void ResolveRelativePathsForLinksInTag(string xPath, string attribute, string url, ref HtmlAgilityPack.HtmlDocument doc, List<string> storeLinks)
        {
            string urlPattern = @"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";

            var node = doc.DocumentNode;
            if (node == null) return;

            var nodes = node.SelectNodes(xPath);
            if (nodes == null) return;

            foreach (var item in nodes)
            {
                if (item.Attributes[attribute] != null && item.Attributes[attribute].Value != null)
                {
                    string link = item.Attributes[attribute].Value;

                    if (!(Utilities.IsUrlValid(link, UriKind.RelativeOrAbsolute)))
                        continue;

                    if (!Utilities.IsAbsoluteUrl(link))
                    {
                        link = Utilities.GetAbsolutePath(url, link);
                        if (Utilities.IsUrlValid(link))
                        {
                            item.Attributes[attribute].Value = link;
                        }
                    }

                    if (item.Attributes[attribute].Value.StartsWith(@"//"))
                    {
                        item.Attributes[attribute].Value = "http:" + item.Attributes[attribute].Value;
                    }

                    //urlPattern pretože sa tu dostávali url typu mailto:$email 
                    if (Regex.IsMatch(link, urlPattern))
                    {
                        if (storeLinks != null)
                        {
                            storeLinks.Add(link);
                        }
                    }
                }
            }
        }

        private void RecursivelyDownloadUrl(List<string> links, string pathToDownload, int depth, bool calledFromMain = false)
        {
            if (depth == 0) return;

            List<string> subLinks = new List<string>();

            for (int i = 0; i < links.Count; i++)
            {
                if (calledFromMain)
                {
                    UpdateProgressText("Downloading URL no " + (i + 1) + "/" + links.Count + " recursively " + depth + " levels deep! " + links[i]);
                    UpdateStatusText("Downloading URL no " + (i + 1) + "/" + links.Count + " recursively " + depth + " levels deep!");
                    PassedLinksCounter++;
                    //UpdateProgressText("Downloading URL no " + (i + 1) + "/" + links.Count + " recursively " + depth + " levels deep!");
                    //UpdateStatusText("Downloading URL no " + (i + 1) + "/" + links.Count + " recursively " + depth + " levels deep!");
                }
                string link = links[i];
                string pathtoDownloadSybLinks = string.Empty;
                GrabAndStoreWebPage(link, pathToDownload, ref subLinks, ref pathtoDownloadSybLinks, ref client);      

                //V prípade keď to je neplatné, tak prišlo k chybe pri volaní odkazu
                if (pathtoDownloadSybLinks.Trim() != string.Empty)
                {
                    RecursivelyDownloadUrl(subLinks, pathtoDownloadSybLinks, depth - 1);
                }

                if (calledFromMain)
                {
                    UpdateProgressBar(((i + 1) / links.Count * 67) + 33);
                }
            }
        }

        private void UpdateStatusBar(string msg)
        {
        }

        private void UpdateProgressBar(int percentage)
        {
            Settings.MainFormInstance.Invoke((MethodInvoker)delegate
            {
                Settings.MainFormInstance.progressBar1.Value = percentage;
            });
        }

        private void ShowMessageBox(string msg)
        {

            Settings.MainFormInstance.Invoke((MethodInvoker)delegate ()
            {
                MessageBox.Show(msg);
            });
        }

        private void UpdateProgressText(string text)
        {
            Settings.MainFormInstance.Invoke((MethodInvoker)delegate
            {
                Settings.MainFormInstance.tbProgres.Text += text + Environment.NewLine;
                Settings.MainFormInstance.tbProgres.SelectionStart = Settings.MainFormInstance.tbProgres.Text.Length;
                Settings.MainFormInstance.tbProgres.ScrollToCaret();
            });
        }

        private void ReenableGrabButton()
        {
            Settings.MainFormInstance.Invoke((MethodInvoker)delegate
            {
                Settings.MainFormInstance.ReEnableStartClearButton();
            });
        }

        private void UpdateStatusText(string msg)
        {
            Settings.MainFormInstance.Invoke((MethodInvoker)delegate
            {
                Settings.MainFormInstance.label3.Text = msg;
            });
        }

        private void UpdateProgressTest(string status, string link, string path, string outcome)
        {    
            //Logovanie na úrovni MainForm
            UpdateProgressText(string.Format("StatusCode '{0}': {1}", status, outcome));

            //Logovanie na úrovni Report.txt
            using (FileStream fs = new FileStream(path + Settings.ReportId, FileMode.Append, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                if (outcome == "FAILED")
                {
                    sw.WriteLine(string.Format("=========={0}StatusCode '{1}': {2} {3}{0}==========", Environment.NewLine, status, outcome, link));
                    FailedLinksCounter++;
                }
                else
                {
                    sw.WriteLine(string.Format("StatusCode '{0}': {1} {2}", status, outcome, link));
                }
            }
        }
    }
}
