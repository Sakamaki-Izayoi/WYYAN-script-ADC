namespace ElCorki
{
    using LeagueSharp.Common;

    internal class Program
    {
        #region Methods

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Corki.Game_OnGameLoad;
        }

        #endregion
    }
}