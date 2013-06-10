using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OkEdv.MusicBrainz;
using System.Diagnostics;
using System.IO;
using TagLib;
using System.Xml;
using System.Xml.XPath;
using LastFmLib;
using LastFmLib.API20;
using System.Security.Cryptography;
using LastFmLib.API20.Types;
using System.Threading;
using System.Reflection;
using System.Configuration;
using System.Drawing;
using System.Net;
using System.Web;
using System.ServiceModel.Web;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public LastFmLib.General.LastFmClient client;
        

        public Form1()
        {
           

            LastFmLib.General.MD5Hash key = new LastFmLib.General.MD5Hash("868b33aa44239a11ebd04e58e7e484ff", true);
            LastFmLib.General.MD5Hash secret = new LastFmLib.General.MD5Hash("832c3c8fbd6c7aab6037e19771535dbc", true);
            var myauth = new LastFmLib.API20.Types.AuthData(key, secret);
            LastFmLib.API20.Settings20.AuthData = myauth;
            //client = LastFmClient.Create(myauth);
            client = new LastFmLib.General.LastFmClient(new LastFmLib.API10.TypeClasses.LastFmUser(myauth));
            
            InitializeComponent();

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string output;
            output = genuidEXE.Run("genpuid.exe", "0736ac2cd889ef77f26f6b5e3fb8a09c G6.mp3", 0);
            System.Windows.Forms.MessageBox.Show(output);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        

        /* Methods for Loading files 
         * Hanno Bean 2011                
         */

        public MusicFiles[] msic;

        private void button1_Click_1(object sender, EventArgs e)
        {
           
            TagLib.File tfile;
            int counter = 0;
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.progressBar1.Value = 0;
                this.progressBar1.Maximum = openFileDialog1.FileNames.Length;
                this.progressBar1.Minimum = 0;
                this.label1.Text = "0";

                msic = new MusicFiles[openFileDialog1.FileNames.Length];
                foreach (string filename in openFileDialog1.FileNames)
                {
                    msic[counter] = new MusicFiles();
                    msic[counter].id = counter;
                    msic[counter].filename = openFileDialog1.FileNames[counter];
                    msic[counter].safefilename = openFileDialog1.SafeFileNames[counter];
                    msic[counter].m_thread = new Thread(new ParameterizedThreadStart(doLoadMe));
                    counter += 1;
                }

                if (msic.Length > 1)
                {
                    msic[0].m_thread.Start(msic[0]);
                    //msic[1].m_thread.Start(msic[1]);
                }
                else
                {
                    msic[0].m_thread.Start(msic[0]);
                }
            }

        }

        public void doLoadThread()
        {

        }

        public void doLoadMe(object num)
        {
            MusicFiles ms = (MusicFiles)num;
            msic[ms.id].busy = true;

            try
            {
                /* thread work */
                TagLib.File tfile = TagLib.File.Create(ms.filename);
                if (tfile.Tag.Title == null) { tfile.Tag.Title = ms.safefilename; }
                if (tfile.Tag.FirstArtist == null) { tfile.Tag.Artists = (new string[] { "Unknown" }); }
                if (tfile.Tag.Album == null) { tfile.Tag.Album = ("No Album"); }
                ListViewItem item1 = new ListViewItem(ms.filename);
                
                item1.SubItems.Add(tfile.Tag.Title);
                item1.SubItems.Add(tfile.Tag.FirstArtist);
                item1.SubItems.Add(tfile.Tag.Album.ToString());
                int theid = this.mainC1.listView1.Items.Count;
                item1.SubItems.Add(theid.ToString());
                this.mainC1.listView1.Invoke(new MethodInvoker(delegate
                    {
                        this.mainC1.listView1.Items.Add(item1);
                        this.progressBar1.Value += 1;
                        double perc = ((this.progressBar1.Value * 100) / this.progressBar1.Maximum);
                        this.label1.Text = perc.ToString() + " %";
                    }));
            }
            catch (Exception e)
            {

            }

            msic[ms.id].busy = false;
            msic[ms.id].status = false;            
            doEndthread(ms.id);
        }

        public void doEndthread(int id)
        {
            try
            {
                int counter = 0;
                while (counter < msic.Length)
                {
                    if ((msic[counter].busy == false) && (msic[counter].status == true) && (msic[counter].m_thread.ThreadState != System.Threading.ThreadState.Running))
                    {
                        msic[counter].m_thread.Start(msic[counter]);
                        break;
                    }
                    counter += 1;
                }
            }
            catch (Exception e)
            {

            }
        }

        public class MusicFiles
        {
            public string filename;
            public string safefilename;
            public int id;
            public bool status;
            public bool busy;
            public Thread m_thread;

            public MusicFiles()
            {
                status = true;
                busy = false;
                id = 0;
                filename = "";
                safefilename = "";
                    
            }
        }
        /*==============================================================*/


        /*
         * Methods for Auto Tagging using LastFM
         * Hanno Bean 2011
         * 
         */

        public LastFMWorker[] lfm;
        public int[] thcounter = new int[2];

        private void button5_Click(object sender, EventArgs e)
        {
            if (mainC1.listView1.SelectedItems.Count > 0)
            {
                int count = mainC1.listView1.SelectedItems.Count;               
                doThread(count);                
            }
            else
            {
                MessageBox.Show("Please choose some songs to tag!");
            }
        }
        public void doThread(int count)
        {
            lfm = new LastFMWorker[count];
            this.progressBar1.Value = 0;
            this.progressBar1.Maximum = count;
            int tel = 0;

            //foreach (ListViewItem item in this.mainC1.listView1.SelectedItems)
           // {
           //     item.Remove();
           // }

            while (tel < count)
            {
                lfm[tel] = new LastFMWorker();
                lfm[tel].filename = mainC1.listView1.SelectedItems[tel].SubItems[0].Text;
                lfm[tel].id = tel;
                lfm[tel].orignial_index = mainC1.listView1.SelectedItems[tel].Index;
                lfm[tel].m_thread = new Thread(new ParameterizedThreadStart(doMe));
                lfm[tel].m_thread.Name = "LastFMThread : " + lfm[tel].id;

                /*string[] str = new string[4];
                str[0] = tel.ToString();
                str[1] = mainC1.listView1.SelectedItems[tel].SubItems[3].Text;
                str[2] = mainC1.listView1.SelectedItems[tel].SubItems[3].Text;
                str[3] = mainC1.listView1.SelectedItems[tel].Index.ToString();
                t[tel] = new Thread(new ParameterizedThreadStart(doMe));
                t[tel].Start(str);*/
                tel += 1;
            }
            if (lfm.Length > 1)
            {
                thcounter[0] = 0;
                thcounter[1] = 1;

                updateTdisplay(0, 1);
                updateTdisplay(1, 1);
                lfm[0].m_thread.Start(lfm[0]);
                lfm[1].m_thread.Start(lfm[1]);
            }
            else
            {
                thcounter[0] = 0;
                updateTdisplay(0, 1);
                lfm[0].m_thread.Start(lfm[0]);
            }

        }

        public void updateTdisplay(int tcounter, int status)
        {
            int currentC = 0;

            if (thcounter[0] == tcounter)
            {
                currentC = 0;
            }
            else
            {
                currentC = 1;
            }

            if (status == 1)
            {
                if (currentC == 0)
                {
                    this.textT1.Invoke(new MethodInvoker(delegate
                    {
                        this.textT1.Text = "Started";
                    }));                    
                }
                else
                {
                    this.textT2.Invoke(new MethodInvoker(delegate
                    {
                        this.textT2.Text = "Started";
                    }));
                }
                
            }
            else if (status == 2)
            {
                if (currentC == 0)
                {
                     
                    this.textT1.Invoke(new MethodInvoker(delegate
                    {
                        this.textT1.Text = "Found Result, Parsing Now";
                    })); 
                }
                else
                {
                    
                    this.textT2.Invoke(new MethodInvoker(delegate
                    {
                        this.textT2.Text = "Found Result, Parsing Now";
                    }));
                }
            }
            else if (status == 3)
            {
                if (currentC == 0)
                {
                    
                    this.textT1.Invoke(new MethodInvoker(delegate
                    {
                        this.textT1.Text = "No Result Found";
                    })); 
                }
                else
                {
                   
                    this.textT2.Invoke(new MethodInvoker(delegate
                    {
                        this.textT2.Text = "No Result Found";
                    }));
                }
            }
            else if (status == 4)
            {
                if (currentC == 0)
                {

                    this.textT1.Invoke(new MethodInvoker(delegate
                    {
                        this.textT1.Text = "Looking up Last.FM fingerprint";
                    }));
                }
                else
                {

                    this.textT2.Invoke(new MethodInvoker(delegate
                    {
                        this.textT2.Text = "Looking up Last.FM fingerprint";
                    }));
                }
            }

        }

        public string HttpGetTrackInfo(string url, string artist, string trackk, string key)
        {
            url = "http://ws.audioscrobbler.com/2.0/?method=track.getInfo";


            string uurl = url + "&api_key=" + key + "&artist=" + HttpUtility.UrlEncode(artist) + "&track=" + HttpUtility.UrlEncode(trackk) + "&autocorrect=1&format=json";
            //868b33aa44239a11ebd04e58e7e484ff=cher=believe
            HttpWebRequest req = WebRequest.Create(uurl)
                                 as HttpWebRequest;
            string result = null;
            using (HttpWebResponse resp = req.GetResponse()
                                          as HttpWebResponse)
            {
                StreamReader reader =
                    new StreamReader(resp.GetResponseStream());
                result = reader.ReadToEnd();
            }
            return result;
        }

        public void doMe(object num)
        {
            LastFMWorker last = (LastFMWorker)num;
            last.busy = true;
            int count = last.id;
            int arcount = last.orignial_index;
            try
            {                
                XmlDocument xdoc;
                XmlNode node;
                double confidence;
                string artist;
                string title;
                string mbid;
                LastFmLib.API20.Types.TrackInformation tr;
                string cd = "";

                ListViewItem item1;
                updateTdisplay(count, 4);
                string xmlsreing = HBPUID.GenUID.LastFM.Run(last.filename, 0).Trim();
                updateTdisplay(count, 2);
                xdoc = new XmlDocument();
                xdoc.LoadXml(xmlsreing);
                node = xdoc.SelectSingleNode("//lfm/tracks/track");
                confidence = 0;
                double.TryParse(xdoc.SelectSingleNode("//lfm/tracks/track/@rank").InnerText, out confidence);
                confidence = confidence * 100;
                artist = node.SelectSingleNode("//track/artist/name").InnerText;
                title = node.SelectSingleNode("//track/name").InnerText;
                mbid = node.SelectSingleNode("//track/mbid").InnerText;
                string largim = null ;
                try
                {
                    largim = xdoc.SelectNodes("//lfm/tracks/track/image").Item(3).InnerXml;
                }
                catch (Exception imagex)
                {

                }

                if (largim != null) {
                    TagLib.File tfile;

                    string localFilename = @"c:\temp.jpg";
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(largim, localFilename);
                    }

                    
                    tfile = TagLib.File.Create(last.filename);
                    TagLib.Picture picture = new TagLib.Picture(localFilename);
                    TagLib.Id3v2.AttachedPictureFrame albumCoverPictFrame = new TagLib.Id3v2.AttachedPictureFrame(picture);
                    albumCoverPictFrame.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                    albumCoverPictFrame.Type = TagLib.PictureType.FrontCover;
                    TagLib.IPicture[] pictFrames = new IPicture[1];
                    pictFrames[0] = (IPicture)albumCoverPictFrame;
                    tfile.Tag.Pictures = pictFrames;

                    if (mbid != null)
                    {
                        tfile.Tag.MusicBrainzTrackId = mbid;
                    }

                    tfile.Save();
                    
                }

                item1 = new ListViewItem(confidence.ToString());
                
                if (mbid.Length > 2)
                {
                    tr = client.Track.GetInfo(new Guid(mbid));
                }
                else
                {
                    tr = client.Track.GetInfo(new LastFmLib.API20.Types.Track(artist, title));
                    //client.Track.
                }



                string tt1 = HttpGetTrackInfo("", artist, title, "868b33aa44239a11ebd04e58e7e484ff");
                dynamic json = JValue.Parse(tt1);
                TagLib.File mp3file;
                mp3file = TagLib.File.Create(last.filename);

                try
                {

                    if (json.track != null)
                    {
                        if (json.track.name != null)
                        {
                            mp3file.Tag.Title = (string)json.track.name;
                        }
                        if (json.track.mbid != null)
                        {
                            mp3file.Tag.MusicBrainzTrackId = (string)json.track.mbid;
                        }
                        if (json.track.url != null)
                        {
                            mp3file.Tag.Lyrics = (string)json.track.url;
                        }

                        if (json.track.album != null)
                        {
                            if (json.track.album.title != null)
                            {
                                mp3file.Tag.Album = (string)json.track.album.title;
                            }
                            if (json.track.album.mbid != null)
                            {
                                mp3file.Tag.MusicBrainzReleaseId = (string)json.track.album.mbid;
                            }
                            if (json.track.album.@attr != null)
                            {
                                if (json.track.album.@attr.position != null)
                                {
                                    mp3file.Tag.Track = (uint)json.track.album.attr.position;
                                }
                            }
                        }
                        if (json.track.artist != null)
                        {
                            if (json.track.artist.name != null)
                            {
                                mp3file.Tag.AlbumArtists[0] = (string)json.track.artist.name;
                            }
                            if (json.track.artist.mbid != null)
                            {
                                mp3file.Tag.MusicBrainzArtistId = (string)json.track.artist.mbid;
                            }
                        }
                        if (json.track.toptags != null)
                        {
                            if (json.track.toptags.tag != null)
                            {
                                if (json.track.toptags.tag[0].name != null)
                                {
                                    mp3file.Tag.Genres[0] = (string)json.track.toptags.tag[0].name;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex11)
                {

                }

                mp3file.Save();
                


                //client.Album.Search(
                XmlDocument xdoc2 = new XmlDocument();
                xdoc2.LoadXml("<xml>" +  tr.innerxml + "</xml>");
                try
                {
                    string albumname;
                    string track;
                    albumname = xdoc2.SelectSingleNode("//album/title").InnerText;
                    track = xdoc2.SelectSingleNode("//album/@position").InnerText;
                    if (albumname != null)
                    {
                        cd = albumname;
                    }
                    else
                    {
                        TagLib.File tfile;
                        tfile = TagLib.File.Create(last.filename);
                        if (tfile.Tag.Album != null)
                        {
                            cd = tfile.Tag.Album;
                        }
                    }

                    if (track != null)
                    {

                    }

                }
                catch (Exception ex)
                {
                    //Album retrieval failed , lets load the old album back shall we
                    TagLib.File tfile;
                    tfile = TagLib.File.Create(last.filename);
                    if (tfile.Tag.Album != null)
                    {
                        cd = tfile.Tag.Album;
                    }
                }

                item1.SubItems.Add(title);
                item1.SubItems.Add(cd);
                item1.SubItems.Add(artist);
                
                
                
                this.mainC1.listView1.Invoke(new MethodInvoker(delegate
                {
                    this.mainC1.listView1.Items[arcount].BackColor = Color.LightGray;
                    this.progressBar1.Value += 1;
                    double perc = ((this.progressBar1.Value * 100) / this.progressBar1.Maximum);
                    this.label1.Text = perc.ToString() + " %";

                    string no = this.mainC1.listView1.Items[arcount].SubItems[4].Text;
                    item1.SubItems.Add(no);

                    item1.SubItems.Add(this.mainC1.listView1.Items[arcount].SubItems[0].Text);

                }));
                AddListBoxItem(item1);
                last.status = false;
                last.busy = false;
                doEndLast(last.id);
            }
            catch (Exception e1)
            {
                TagLib.File mp3file;
                mp3file = TagLib.File.Create(last.filename);
                Regex rgx = new Regex("[^a-zA-Z0-9'&(). -]");
                bool gotart = false;
                bool gottrack = false;
                dynamic json = "";

                try
                {
                string tt1 = HttpGetTrackInfo("", rgx.Replace(mp3file.Tag.AlbumArtists[0].ToString(), ""), rgx.Replace(mp3file.Tag.Title , "") , "868b33aa44239a11ebd04e58e7e484ff");
                 json = JValue.Parse(tt1);
                

                

                    if (json.track != null)
                    {
                        if (json.track.name != null)
                        {
                            gotart = true;
                            mp3file.Tag.Title = (string)json.track.name;
                        }
                        if (json.track.mbid != null)
                        {
                            mp3file.Tag.MusicBrainzTrackId = (string)json.track.mbid;
                        }
                        if (json.track.url != null)
                        {
                            mp3file.Tag.Lyrics = (string)json.track.url;
                        }

                        if (json.track.album != null)
                        {
                            if (json.track.album.title != null)
                            {
                                mp3file.Tag.Album = (string)json.track.album.title;
                            }
                            if (json.track.album.mbid != null)
                            {
                                mp3file.Tag.MusicBrainzReleaseId = (string)json.track.album.mbid;
                            }
                            if (json.track.album.@attr != null)
                            {
                                if (json.track.album.@attr.position != null)
                                {
                                    mp3file.Tag.Track = (uint)json.track.album.attr.position;
                                }
                            }
                        }
                        if (json.track.artist != null)
                        {
                            if (json.track.artist.name != null)
                            {
                                gottrack = true;
                                mp3file.Tag.AlbumArtists[0] = (string)json.track.artist.name;
                            }
                            if (json.track.artist.mbid != null)
                            {
                                mp3file.Tag.MusicBrainzArtistId = (string)json.track.artist.mbid;
                            }
                        }
                        if (json.track.toptags != null)
                        {
                            if (json.track.toptags.tag != null)
                            {
                                if (json.track.toptags.tag[0].name != null)
                                {
                                    mp3file.Tag.Genres[0] = (string)json.track.toptags.tag[0].name;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex11)
                {

                }
                

                if (gottrack && gotart)
                {
                    ListViewItem item1 = new ListViewItem("0");
                    item1.SubItems.Add((string)json.track.name);
                    item1.SubItems.Add(mp3file.Tag.Album);
                    item1.SubItems.Add((string)json.track.artist.name);



                    this.mainC1.listView1.Invoke(new MethodInvoker(delegate
                    {
                        this.mainC1.listView1.Items[arcount].BackColor = Color.LightGray;
                        this.progressBar1.Value += 1;
                        double perc = ((this.progressBar1.Value * 100) / this.progressBar1.Maximum);
                        this.label1.Text = perc.ToString() + " %";

                        string no = this.mainC1.listView1.Items[arcount].SubItems[4].Text;
                        item1.SubItems.Add(no);

                        item1.SubItems.Add(this.mainC1.listView1.Items[arcount].SubItems[0].Text);

                    }));
                    AddListBoxItem(item1);

                }
                else
                {
                    this.mainC1.listView1.Invoke(new MethodInvoker(delegate
                    {
                        this.progressBar1.Value += 1;
                        this.mainC1.listView1.Items[arcount].BackColor = Color.LightPink;
                        double perc = ((this.progressBar1.Value * 100) / this.progressBar1.Maximum);
                        this.label1.Text = perc.ToString() + " %";
                    }));
                }

                try
                {
                    mp3file.Save();
                }
                catch (Exception nex1)
                {

                }

                
                updateTdisplay(count, 3);
                last.status = false;
                last.busy = false;
                doEndLast(last.id);
            }
        }

        

        public class LastFMWorker
        {
            public string filename;
            public int id;
            public Thread m_thread;
            public int orignial_index;
            public bool status;
            public bool busy;

            public LastFMWorker()
            {
                status = true;
                busy = false;
                id = 0;
                filename = "";
                orignial_index = 0;
            }
        }

        public void doEndLast(int id)
        {
            try
            {
                int counter = 0;
                while (counter < lfm.Length)
                {
                    if ((lfm[counter].busy == false) && (lfm[counter].status == true) && (lfm[counter].m_thread.ThreadState != System.Threading.ThreadState.Running))
                    {
                        if (thcounter[0] == id)
                        {
                            thcounter[0] = counter;
                        }
                        else
                        {
                            thcounter[1] = counter;
                        }
                        updateTdisplay(counter, 1);
                        lfm[counter].m_thread.Start(lfm[counter]);
                        break;
                    }
                    counter += 1;
                }
            }
            catch (Exception e)
            {

            }
        }

        /*=================================================================*/


        /*
         * Methods for saving tags
         * Hanno Bean 2011
         */

        private void button3_Click(object sender, EventArgs e)
        {
            int countitems = mainC1.listView2.Items.Count;
            this.progressBar1.Value = 0;
            this.progressBar1.Maximum = countitems;
            doSaveThread(countitems);            
        }
        public void doSaveThread(int count)
        {
            Thread[] t = new Thread[count];
            int tel = 0;
            while (tel < count)
            {
                string[] str = new string[4];
                str[0] = tel.ToString();
                str[1] = mainC1.listView2.Items[tel].SubItems[4].Text;
                str[2] = mainC1.listView2.Items[tel].SubItems[5].Text;
                t[tel] = new Thread(new ParameterizedThreadStart(doSaveMe));
                t[tel].Start(str);
                tel += 1;
            }
        }
        public void doSaveThreadSave(int count)
        {
            Thread[] t = new Thread[count];
            int tel = 0;
            while (tel < count)
            {
                string[] str = new string[4];
                str[0] = tel.ToString();
                str[1] = mainC1.listView2.Items[tel].SubItems[4].Text;
                str[2] = mainC1.listView2.Items[tel].SubItems[5].Text;
                t[tel] = new Thread(new ParameterizedThreadStart(doSaveMeMove));
                t[tel].Start(str);
                tel += 1;
            }
        }
        public void doSaveMe(object num)
        {
            try
            {
                //
                string[] str = new string[4];
                str = (string[])num;
                int count = 0;
                int.TryParse(str[0], out count);
                TagLib.File tfile;
                tfile = TagLib.File.Create(str[2]);
                //GetItemValueFromList(count, 3);

                string text = string.Empty;
                this.mainC1.listView2.Invoke(new MethodInvoker(delegate
                {
                    //tfile.Tag.
                    tfile.Tag.AlbumArtists = (new string[] { mainC1.listView2.Items[count].SubItems[3].Text });
                    tfile.Tag.Performers = (new string[] { mainC1.listView2.Items[count].SubItems[3].Text });
                    tfile.Tag.Artists = (new string[] { mainC1.listView2.Items[count].SubItems[3].Text });
                    tfile.Tag.Composers = (new string[] { mainC1.listView2.Items[count].SubItems[3].Text });
                    tfile.Tag.FirstArtist.Insert(0, mainC1.listView2.Items[count].SubItems[3].Text);
                    tfile.Tag.Title = mainC1.listView2.Items[count].SubItems[1].Text;
                    tfile.Tag.Album = mainC1.listView2.Items[count].SubItems[2].Text;                    
                }));

                tfile.Save();
                this.mainC1.listView2.Invoke(new MethodInvoker(delegate
                {
                    this.progressBar1.Value += 1;
                    double perc = ( (this.progressBar1.Value*100) / this.progressBar1.Maximum);
                    this.label1.Text = perc.ToString() + " %";
                }));
            }
            catch (Exception ex1)
            {

            }
        }

        public void doSaveMeMove(object num)
        {
            string[] str = new string[4];
            str = (string[])num;
            int count = 0;
            int.TryParse(str[0], out count);
            TagLib.File tfile;
            tfile = TagLib.File.Create(str[2]);
            //GetItemValueFromList(count, 3);
            bool saved = false;
            bool moved = false;

            string text = string.Empty;
            try
            {
                //
                //5501015109087
                //42536186986
                this.mainC1.listView2.Invoke(new MethodInvoker(delegate
                {
                    //tfile.Tag.
                    tfile.Tag.AlbumArtists = (new string[] { mainC1.listView2.Items[count].SubItems[3].Text });
                    tfile.Tag.Performers = (new string[] { mainC1.listView2.Items[count].SubItems[3].Text });
                    tfile.Tag.Artists = (new string[] { mainC1.listView2.Items[count].SubItems[3].Text });
                    tfile.Tag.Composers = (new string[] { mainC1.listView2.Items[count].SubItems[3].Text });
                    tfile.Tag.FirstArtist.Insert(0, mainC1.listView2.Items[count].SubItems[3].Text);
                    tfile.Tag.Title = mainC1.listView2.Items[count].SubItems[1].Text;
                    tfile.Tag.Album = mainC1.listView2.Items[count].SubItems[2].Text;
                    tfile.Tag.Comment = "Tagged by my own Custom last.fm tagger! - HB";
                    
                }));

                tfile.Save();
                saved = true;
            }
            catch (Exception ex1)
            {
                this.mainC1.listView2.Invoke(new MethodInvoker(delegate
                {
                    mainC1.listView2.Items[count].SubItems[5].Text = ex1.Message;
                    
                }));
            }
                try
                {
                    string fromp = str[2];
                    //Regex rgx = new Regex("[^a-zA-Z0-9'&().\\ -]");

                    //char[] BAD_CHARS = new char[] { '!', '@', '#', '$', '%', '_' }; //simple example
                    //fromp = RemoveInvalidFilePathCharacters(fromp, "");

                   // fromp = rgx.Replace(fromp, "");
                    string top = Settings1.Default.movefolder + "\\" + RemoveInvalidFilePathCharacters(tfile.Tag.AlbumArtists[0], "") + " - " + RemoveInvalidFilePathCharacters(tfile.Tag.Title, "") +".mp3";
                    System.IO.File.Move(@fromp, @top); // Try to move
                    //Console.WriteLine("Moved"); // Success
                    moved = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex); // Write error
                    this.mainC1.listView2.Invoke(new MethodInvoker(delegate
                    {
                        mainC1.listView2.Items[count].SubItems[5].Text = ex.Message;

                    }));
                }

                if (moved && saved)
                {
                    this.mainC1.listView2.Invoke(new MethodInvoker(delegate
                    {
                        mainC1.listView2.Items[count].BackColor = Color.AliceBlue;
                        this.progressBar1.Value += 1;
                        double perc = ((this.progressBar1.Value * 100) / this.progressBar1.Maximum);
                        this.label1.Text = perc.ToString() + " %";
                    }));
                }
                else
                {
                    this.mainC1.listView2.Invoke(new MethodInvoker(delegate
                    {
                        mainC1.listView2.Items[count].BackColor = Color.MediumVioletRed;
                        this.progressBar1.Value += 1;
                        double perc = ((this.progressBar1.Value * 100) / this.progressBar1.Maximum);
                        this.label1.Text = perc.ToString() + " %";
                    }));
                }
                
            }
            
        

        /*===========================================================================================*/

        public static string RemoveInvalidFilePathCharacters(string filename, string replaceChar)
        {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(filename, replaceChar);
        }

        public class MusicWorker
        {
            public class MusicWorkerThread
            {
                public Boolean status;
                public int id;
                public string filepath;
                public Boolean busy;
                public MusicWorkerThread()
                {

                }
            }

            static MusicWorkerThread[] Mthread;
                        
            public MusicWorker( int numofthreads )
            {
                Mthread = new MusicWorkerThread[numofthreads];                
                
            }

            public static void InitMusicWorker( ListView.SelectedListViewItemCollection items , int keyindex)
            {
                int count = 0;
                int tocount = items.Count;
                while (count < tocount)
                {
                    Mthread[count].id = count;
                    Mthread[count].filepath = items[count].SubItems[keyindex].Text;
                    Mthread[count].status = true;
                    Mthread[count].busy = false;
                }
            }

            public static void Finish(int id) 
            {

            }
        }

        

        public static void OnThreadExit(object sender, EventArgs e)
        {
            //Console.WriteLine("thread is shutting down.");
            //Thread t = (Thread)sender;
        }

        

        

        

        
        private delegate string GetItemValue(int index, int subindex);
        private string GetItemValueFromList(int index, int subindex)
        {
            string ret = "";

            if (mainC1.listView2.InvokeRequired)
            {
                // This is a worker thread so delegate the task.
                //oControl.Invoke(new GetItemValue(oControl), item);
                //GetItemValue d = new GetItemValue(GetItemValueFromList);
                //mainC1.listView2.Invoke(d, new object[] { index, subindex });
                //ret = mainC1.listView2.Items[index].SubItems[subindex].Text;
                this.mainC1.listView2.Invoke(new GetItemValue(this.GetItemValueFromList), index, subindex );
            }
            else
            {
                // This is the UI thread so perform the task.
                //this.mainC1.listView2.Items.Add((ListViewItem)item);
                //mainC1.listView2.Items[count].SubItems[3].Text
                ret = this.mainC1.listView2.Items[index].SubItems[subindex].Text;
            }

            return ret;
        }

        

        public void changesomething(ListViewItem ls)
        {
            this.mainC1.listView2.Items.Add(ls);
        }

        //delegate void SetControlValueCallback( );


        private delegate void AddListViewItemDelegate(object item);

        private void AddListBoxItem(object item)
        {
            if (this.mainC1.listView2.InvokeRequired)
            {
                // This is a worker thread so delegate the task.
                this.mainC1.listView2.Invoke(new AddListViewItemDelegate(this.AddListBoxItem), item);
            }
            else
            {
                // This is the UI thread so perform the task.
                this.mainC1.listView2.Items.Add( (ListViewItem) item);
            }
        }
        

        delegate void SetControlValueCallback(Control oControl, string propName, object propValue);

        private void SetControlPropertyValue(Control oControl, string propName, object propValue)
        {
            if (oControl.InvokeRequired)
            {
                SetControlValueCallback d = new SetControlValueCallback(SetControlPropertyValue);
                oControl.Invoke(d, new object[] { oControl, propName, propValue });
            }
            else
            {
                Type t = oControl.GetType();
                PropertyInfo[] props = t.GetProperties();
                foreach (PropertyInfo p in props)
                {
                    Console.WriteLine(p.Name);
                    if (p.Name.ToUpper() == propName.ToUpper())
                    {
                        p.SetValue(oControl, propValue, null);
                    }
                }
            }
        }

        

        

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this.mainC1.listView1.Items)
            {
                item.Selected = true;
            }
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                this.mainC1.listView1.MultiSelect = true;
                foreach (ListViewItem item in this.mainC1.listView1.Items)
                {
                    item.Selected = true;
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this.mainC1.listView1.SelectedItems)
            {
                item.Remove();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int countitems = mainC1.listView2.Items.Count;
            this.progressBar1.Value = 0;
            this.progressBar1.Maximum = countitems;
            doSaveThreadSave(countitems); 
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                
                string[] files = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.mp3" , SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    TagLib.File tfile;
                    this.progressBar1.Value = 0;
                    this.progressBar1.Maximum = files.Length;
                    this.progressBar1.Minimum = 0;
                    this.label1.Text = "0";
                    int counter = 0;

                    msic = new MusicFiles[files.Length];
                    foreach (string filename in files)
                    {
                        msic[counter] = new MusicFiles();
                        msic[counter].id = counter;
                        msic[counter].filename = filename;
                        msic[counter].safefilename = filename;
                        msic[counter].m_thread = new Thread(new ParameterizedThreadStart(doLoadMe));
                        counter += 1;
                    }

                    if (msic.Length > 1)
                    {
                        msic[0].m_thread.Start(msic[0]);
                        //msic[1].m_thread.Start(msic[1]);
                    }

                    
                }
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Options opt = new Options();
            opt.ShowDialog();
        }
    }

    public class genuidEXE
    {
        internal static string Run(string exeName, string argsLine, int timeoutSeconds)
        {
            StreamReader outputStream = StreamReader.Null;
            string output = "";
            bool success = false;
            try
            {
                Process newProcess = new Process();
                newProcess.StartInfo.FileName = exeName;
                newProcess.StartInfo.Arguments = argsLine;
                newProcess.StartInfo.UseShellExecute = false;
                newProcess.StartInfo.CreateNoWindow = true;
                newProcess.StartInfo.RedirectStandardOutput = true;
                newProcess.Start();
                if (0 == timeoutSeconds)
                {
                    outputStream = newProcess.StandardOutput;
                    output = outputStream.ReadToEnd();
                    newProcess.WaitForExit();
                }
                else
                {
                    success = newProcess.WaitForExit(timeoutSeconds * 1000);

                    if (success)
                    {
                        outputStream = newProcess.StandardOutput;
                        output = outputStream.ReadToEnd();
                    }
                    else
                    {
                        output = "Timed out at " + timeoutSeconds + " seconds waiting for " + exeName + " to exit.";
                    }
                }
            }
            catch (Exception e)
            {
                throw (new Exception("An error occurred running " + exeName + ".", e));
            }
            finally
            {
                outputStream.Close();
            }
            return "\t" + output;
        }

    }

}
