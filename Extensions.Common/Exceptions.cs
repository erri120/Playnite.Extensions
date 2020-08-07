using System;

namespace Extensions.Common
{
    public class LoadingGameException : Exception
    {
        public LoadingGameException(string game, Exception e) : base($"Unable to load game {game}!", e) { }
    }
}
