using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Extensions.Common;
using Microsoft.Toolkit.HighPerformance.Helpers;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;

namespace GameManagement;

public class StorageInfo
{
    private readonly IPlayniteAPI _playniteAPI;

    public List<GameStorage> Games { get; set; } = new();

    public string TotalSize => $"Total Size: {Games.Sum(x => x.SizeOnDisk).ToFileSizeString()}";

    public StorageInfo(IPlayniteAPI playniteAPI)
    {
        _playniteAPI = playniteAPI;
    }

    public void LoadFromFile(string path)
    {
        if (!File.Exists(path)) return;

        var res = Serialization.FromJsonFile<List<GameStorage>>(path);
        if (res is null || !res.Any()) return;

        Games = res.Select(x =>
        {
            x.PlayniteAPI = _playniteAPI;
            return x;
        }).ToList();
    }

    public void SaveToFile(string path)
    {
        var json = Serialization.ToJson(Games);
        if (json is null) throw new NotImplementedException();

        File.WriteAllText(path, json, Encoding.UTF8);
    }

    private readonly struct CalculateStorageStruct : IRefAction<TempStorage>
    {
        public void Invoke(ref TempStorage item)
        {
            item.GameStorage.SizeOnDisk = GetDirectorySize(item.Game.InstallDirectory);
        }
    }

    private static long GetDirectorySize(string path)
    {
        return Directory
            .EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Sum(file => new FileInfo(file).Length);
    }

    private static bool GameHasInstallDirectory(Game game)
    {
        return !string.IsNullOrWhiteSpace(game.InstallDirectory) && Directory.Exists(game.InstallDirectory);
    }

    public void UpdateStorageInfoForAllNewGames()
    {
        var games = _playniteAPI.Database.Games
            .Where(dbGame => !Games.Any(savedGame => savedGame.GameId.Equals(dbGame.Id)))
            .Where(GameHasInstallDirectory)
            .Select(x => new TempStorage(x, new GameStorage(_playniteAPI)
        {
            GameId = x.Id
        })).ToArray();

        ParallelHelper.ForEach<TempStorage, CalculateStorageStruct>(games);

        Games.AddRange(games.Select(x => x.GameStorage));
    }

    public void AddStorageInfo(Game game)
    {
        if (!GameHasInstallDirectory(game)) return;

        var storage = new GameStorage(_playniteAPI)
        {
            GameId = game.Id,
            SizeOnDisk = GetDirectorySize(game.InstallDirectory)
        };

        Games.Add(storage);
    }

    public void RemoveStorageInfo(Game game)
    {
        Games.RemoveAll(savedGame => savedGame.GameId.Equals(game.Id));
    }
}

public readonly struct TempStorage
{
    public readonly Game Game;
    public readonly GameStorage GameStorage;

    public TempStorage(Game game, GameStorage gameStorage)
    {
        Game = game;
        GameStorage = gameStorage;
    }
}

public class GameStorage
{
    [DontSerialize]
    public IPlayniteAPI? PlayniteAPI { get; set; }

    public GameStorage() { }

    public GameStorage(IPlayniteAPI playniteAPI)
    {
        PlayniteAPI = playniteAPI;
    }

    public Guid GameId { get; set; }

    public long SizeOnDisk { get; set; }

    [DontSerialize] public string FileSizeString => SizeOnDisk.ToFileSizeString();

    [DontSerialize] public string GameName => PlayniteAPI?.Database.Games.First(x => x.Id.Equals(GameId)).Name ?? string.Empty;
}
