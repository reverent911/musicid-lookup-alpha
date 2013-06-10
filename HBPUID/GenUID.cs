using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace HBPUID
{
    public class GenUID
    {
        public class LastFM
        {
            public static string Run(string argsLine, int timeoutSeconds)
            {
                StreamReader outputStream = StreamReader.Null;
                string output = "";
                bool success = false;
                try
                {
                    Process newProcess = new Process();
                    newProcess.StartInfo.FileName = "LFM\\lastfmfpclient.exe";
                    newProcess.StartInfo.Arguments = '"' + argsLine + '"';
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
                            output = "Timed out at " + timeoutSeconds + " seconds waiting for " + "Last FM Tagger" + " to exit.";
                        }
                    }
                }
                catch (Exception e)
                {
                    throw (new Exception("An error occurred running " + "Last FM Tagger" + ".", e));
                }
                finally
                {
                    outputStream.Close();
                }
                return output;
            }
        }

        public class MusicIP
        {

        }
    }
}
