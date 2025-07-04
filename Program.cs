namespace GotifyClient
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            
            // 确保只有一个实例运行
            using (var mutex = new Mutex(true, "GotifyClient", out bool createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("Gotify客户端已在运行中！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.Run(new MainForm());
            }
        }
    }
}
