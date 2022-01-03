using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Extensions.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;

namespace F95ZoneMetadata;

public class Settings : ISettings
{
    private const string LoginUrl = "https://f95zone.to/login";

    private readonly Plugin? _plugin;
    private readonly IPlayniteAPI? _playniteAPI;

    /// <summary>
    /// User cookie: "xf_user"
    /// </summary>
    public string? CookieUser { get; set; }

    /// <summary>
    /// Two-factor Authentication cookie: "xf_tfa_trust"
    /// </summary>
    public string? CookieTfaTrust { get; set; }

    /// <summary>
    /// Cross-Site Request Forgery cookie: "xf_csrf"
    /// </summary>
    public string? CookieCsrf { get; set; }

    public PlayniteProperty LabelProperty { get; set; } = PlayniteProperty.Features;
    public PlayniteProperty TagProperty { get; set; } = PlayniteProperty.Tags;

    public bool CheckForUpdates { get; set; }

    public int DaysBetweenUpdate { get; set; } = 7;

    [DontSerialize]
    public TimeSpan UpdateDistance => TimeSpan.FromDays(DaysBetweenUpdate);

    public bool UpdateFinishedGames { get; set; } = false;

    public CookieContainer? CreateCookieContainer()
    {
        if (CookieUser is null || CookieCsrf is null) return null;
        var container = new CookieContainer();

        container.Add(new Cookie("xf_user", CookieUser, "/", "f95zone.to")
        {
            Secure = true,
            HttpOnly = true,
            Expires = DateTime.Now + TimeSpan.FromDays(7)
        });

        container.Add(new Cookie("xf_csrf", CookieCsrf, "/", "f95zone.to")
        {
            Secure = true,
            HttpOnly = true,
            Expires = DateTime.Now + TimeSpan.FromDays(7)
        });

        if (CookieTfaTrust is not null)
        {
            container.Add(new Cookie("xf_tfa_trust", CookieTfaTrust, "/", "f95zone.to")
            {
                Secure = true,
                HttpOnly = true,
                Expires = DateTime.Now + TimeSpan.FromDays(7)
            });
        }

        return container;

    }

    public Settings() { }

    public Settings(Plugin plugin, IPlayniteAPI playniteAPI)
    {
        _plugin = plugin;
        _playniteAPI = playniteAPI;

        var savedSettings = plugin.LoadPluginSettings<Settings>();
        if (savedSettings is not null)
        {
            CookieUser = savedSettings.CookieUser;
            CookieTfaTrust = savedSettings.CookieTfaTrust;
            CookieCsrf = savedSettings.CookieCsrf;
            LabelProperty = savedSettings.LabelProperty;
            TagProperty = savedSettings.TagProperty;
            CheckForUpdates = savedSettings.CheckForUpdates;
            DaysBetweenUpdate = savedSettings.DaysBetweenUpdate;
            UpdateFinishedGames = savedSettings.UpdateFinishedGames;
        }
    }

    public void DoLogin()
    {
        if (_playniteAPI is null) throw new InvalidDataException();

        var webView = _playniteAPI.WebViews.CreateView(new WebViewSettings
        {
            UserAgent = "Playnite.Extensions",
            JavaScriptEnabled = true,
            WindowWidth = 900,
            WindowHeight = 700
        });

        webView.Open();
        webView.Navigate(LoginUrl);

        webView.LoadingChanged += WebViewOnLoadingChanged;
    }

    private async void WebViewOnLoadingChanged(object sender, WebViewLoadingChangedEventArgs args)
    {
        if (args.IsLoading) return;
        if (sender is not IWebView web) throw new NotImplementedException();

        var address = web.GetCurrentAddress();
        if (address is null || address.StartsWith(LoginUrl)) return;

        await Task.Run(() =>
        {
            var cookies = web.GetCookies();
            if (cookies is null || !cookies.Any()) return;

            CookieUser = GetCookie(cookies, "xf_user");
            CookieTfaTrust = GetCookie(cookies, "xf_tfa_trust");
            CookieCsrf = GetCookie(cookies, "xf_csrf");

            web.Close();
        });
    }


    private static string? GetCookie(IEnumerable<HttpCookie> cookies, string name)
    {
        var cookie = cookies.FirstOrDefault(x => x.Name is not null && x.Value is not null && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return cookie?.Value;
    }

    private Settings? _previousSettings;

    public void BeginEdit()
    {
        _previousSettings = new Settings
        {
            CookieUser = CookieUser,
            CookieTfaTrust = CookieTfaTrust,
            CookieCsrf = CookieCsrf,
            LabelProperty = LabelProperty,
            TagProperty = TagProperty,
            CheckForUpdates = CheckForUpdates,
            DaysBetweenUpdate = DaysBetweenUpdate,
            UpdateFinishedGames = UpdateFinishedGames
        };
    }

    public void EndEdit()
    {
        _previousSettings = null;
        _plugin?.SavePluginSettings(this);
    }

    public void CancelEdit()
    {
        if (_previousSettings is null) return;

        CookieUser = _previousSettings.CookieUser;
        CookieTfaTrust = _previousSettings.CookieTfaTrust;
        CookieCsrf = _previousSettings.CookieCsrf;
        LabelProperty = _previousSettings.LabelProperty;
        TagProperty = _previousSettings.TagProperty;
        CheckForUpdates = _previousSettings.CheckForUpdates;
        DaysBetweenUpdate = _previousSettings.DaysBetweenUpdate;
        UpdateFinishedGames = _previousSettings.UpdateFinishedGames;
    }

    public bool VerifySettings(out List<string> errors)
    {
        errors = new List<string>();

        if (CookieUser is null)
        {
            errors.Add("The xf_user cookie has to be set!");
        }

        if (CookieCsrf is null)
        {
            errors.Add("The xf_csrf cookie has to be set!");
        }

        if (!Enum.IsDefined(typeof(PlayniteProperty), LabelProperty))
        {
            errors.Add($"Unknown value \"{LabelProperty}\"");
        }

        if (!Enum.IsDefined(typeof(PlayniteProperty), TagProperty))
        {
            errors.Add($"Unknown value \"{TagProperty}\"");
        }

        if (LabelProperty == TagProperty)
        {
            errors.Add($"{nameof(LabelProperty)} == {nameof(TagProperty)}");
        }

        if (DaysBetweenUpdate < 0)
        {
            errors.Add("Update Interval must not be negative!");
        }

        return !errors.Any();
    }
}
