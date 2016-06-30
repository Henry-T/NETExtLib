using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Linq;

namespace NETExtLib
{
    public static class Util
    {
        public static List<int> AllIndexOf(this String str, char findChar)
        {
            List<int> ret = new List<int>();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == findChar)
                {
                    ret.Add(i);
                }
            }
            return ret;
        }

        public static String ReplaceLast(this String str, String from, String to)
        {
            int place = str.LastIndexOf(from);

            if (place == -1)
                return String.Copy(str);

            string result = str.Remove(place, from.Length).Insert(place, to);
            return result;
        }

        public static String FullPathWithoutExtension(this FileInfo fileInfo)
        {
            String nameWithoutExt = fileInfo.FileNameWithoutExtension();
            String result = Path.Combine(fileInfo.Directory.FullName, nameWithoutExt);
            // Log.InfoLine("fullname no ext:"+result);
            return result;
        }

        public static String FileNameWithoutExtension(this FileInfo fileInfo)
        {
            return Path.GetFileNameWithoutExtension(fileInfo.Name);
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        public static void CopyDir(String srcDir, String dstDir, List<String> extensions = null)
        {
            DirectoryInfo srcDirInfo = new DirectoryInfo(srcDir);
            if (!srcDirInfo.Exists)
                return;

            if (!Directory.Exists(dstDir))
                Directory.CreateDirectory(dstDir);

            foreach (var fInfo in srcDirInfo.GetFiles())
            {
                String subDstFilePath = Path.Combine(dstDir, fInfo.Name);
                FileInfo fi = new FileInfo(subDstFilePath);
                if (extensions != null && !extensions.Contains(fi.Extension))
                    continue;
                else
                    fInfo.CopyTo(subDstFilePath, true);
            }

            foreach (var subDirInfo in srcDirInfo.GetDirectories())
            {
                String subSrcDirPath = Path.Combine(srcDir, subDirInfo.Name);
                String subDstDirPath = Path.Combine(dstDir, subDirInfo.Name);
                CopyDir(subSrcDirPath, subDstDirPath);
            }
        }

        public static void SafeCopyTo(String srcFile, String tgtFile)
        {
            FileInfo tgtFileInfo = new FileInfo(tgtFile);
            if (!tgtFileInfo.Directory.Exists)
            {
                tgtFileInfo.Directory.Create();
            }
            File.Copy(srcFile, tgtFile, true);
        }

        public static int RoboCopy(String srcDirPath, String dstDirPath, string wildcards = null, bool ignoreEmptyDir = false)
        {
            string file = "robocopy";
            if (wildcards == null) wildcards = "*.*";
            string args = srcDirPath + " " + dstDirPath + " " + wildcards + " " + (ignoreEmptyDir ? " /S " : " /E ") + " /NFL /NDL /NJH /NJS";
            int exitCode = Util.StartProcessToEnd(file, args, Util.WriteProcessDataToStdout, Util.WriteProcessDataToError);
            if (exitCode > 4)
            {
                Log.Error("[RoboCopy Failed] <exit> <src> <dst> " + exitCode + " " + srcDirPath + " " + dstDirPath);
                return exitCode;
            }
            else
                return 0;
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.ToUpperInvariant() == "FILE")
            {
                // relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, '/');
            }

            // Log.InfoLine("MakeRelativePath src: " + fromPath);
            // Log.InfoLine("MakeRelativePath dst: " + toPath);
            // Log.InfoLine("MakeRelativePath ret:" + relativePath);


            return relativePath;
        }

        public static int StartProcessToEnd(String file, String args,
            DataReceivedEventHandler onOutputDataReceived,
            DataReceivedEventHandler onErrorDataReceived,
            String workingDirectory = null)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            if (!String.IsNullOrEmpty(workingDirectory))
                processStartInfo.WorkingDirectory = workingDirectory;
            processStartInfo.FileName = file;
            processStartInfo.Arguments = args;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.CreateNoWindow = true;
            Process process = Process.Start(processStartInfo);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += onOutputDataReceived;
            process.ErrorDataReceived += onErrorDataReceived;
            process.WaitForExit();
            int exitCode = process.ExitCode;
            return exitCode;
        }

        public static void WriteProcessDataToStdout(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null && !String.IsNullOrEmpty(e.Data.ToString()))
                Log.InfoLine(e.Data);
        }

        public static void WriteProcessDataToError(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null && !String.IsNullOrEmpty(e.Data.ToString()))
                Log.Error(e.Data);
        }

        public static void SaveXEleToFileWithoutBOM(XElement xEle, String filePath)
        {
            using (var writer = new XmlTextWriter(filePath, new UTF8Encoding(false)))
            {
                writer.Formatting = Formatting.Indented;
                xEle.Save(writer);
            }
        }

        // http://stackoverflow.com/questions/2245442/c-sharp-split-a-string-by-another-string
        public static String[] SplitByString(String srcStr, String spliter)
        {
            return srcStr.Split(new string[] { spliter }, StringSplitOptions.None);
        }
    }

    public enum ELogLevel
    {
        Verbose = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
    }

    public static class Log
    {
        private static ELogLevel logLevel = ELogLevel.Warn;

        private static bool debug = false;

        public static void SetDebug(bool debug)
        {
            Log.debug = debug;
        }

        public static void SetLevel(ELogLevel logLevel)
        {
            Log.logLevel = logLevel;
        }

        public static void Verbose(String content)
        {
            if (logLevel <= ELogLevel.Verbose)
                Console.Write(content);
        }

        public static void VerboseLine(String content)
        {
            if (logLevel <= ELogLevel.Verbose)
                Console.WriteLine(content);
        }

        public static void Info(String content)
        {
            if (logLevel <= ELogLevel.Info)
                Console.Write(content);
        }

        public static void InfoLine(String content)
        {
            if (logLevel <= ELogLevel.Info)
                Console.WriteLine(content);
        }

        public static void Debug(String content)
        {
            if (debug)
                Console.Write(content);
        }

        public static void DebugLine(String content)
        {
            if (debug)
                Console.WriteLine(content);
        }


        public static void Warn(String content)
        {
            if (logLevel <= ELogLevel.Warn)
                Console.Write(content);
        }

        public static void WarnLine(String content)
        {
            if (logLevel <= ELogLevel.Warn)
                Console.WriteLine(content);
        }

        public static void Error(String content)
        {
            if (logLevel <= ELogLevel.Error)
                Console.Error.Write(content);
        }

        public static void ErrorLine(String content)
        {
            if (logLevel <= ELogLevel.Error)
                Console.Error.WriteLine(content);
        }
    }

    public static class Profiler
    {
        public static bool profileEnabled = false;

        public static void EnableProfile(bool enabled)
        {
            profileEnabled = enabled;
        }
    }
}
