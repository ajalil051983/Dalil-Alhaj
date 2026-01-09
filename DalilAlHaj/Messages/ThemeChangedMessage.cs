namespace DalilAlHaj.Messages
{
    public class ThemeChangedMessage
    {
        public bool IsDarkMode { get; set; }

        public ThemeChangedMessage(bool isDarkMode)
        {
            IsDarkMode = isDarkMode;
        }
    }
}
