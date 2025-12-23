using System;
using System.Threading.Tasks;
using System.Windows;

namespace WassControlSys.Core
{
    public class LocalizationService : ILocalizationService
    {
        public string CurrentLanguage { get; private set; } = "es";

        public async Task SetLanguageAsync(string language)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    string lang = (language ?? "es").ToLowerInvariant();
                    string path = lang.StartsWith("en") ? "Resources/Strings.en.xaml" : (lang.StartsWith("pt") ? "Resources/Strings.pt.xaml" : "Resources/Strings.es.xaml");
                    ResourceDictionary dict;
                    try
                    {
                        dict = new ResourceDictionary { Source = new Uri(path, UriKind.Relative) };
                    }
                    catch
                    {
                        dict = new ResourceDictionary { Source = new Uri("Resources/Strings.es.xaml", UriKind.Relative) };
                    }

                    ResourceDictionary? existing = null;
                    foreach (var md in Application.Current.Resources.MergedDictionaries)
                    {
                        if (md.Source != null && md.Source.OriginalString.Contains("Resources/Strings."))
                        {
                            existing = md;
                            break;
                        }
                    }
                    if (existing != null)
                    {
                        Application.Current.Resources.MergedDictionaries.Remove(existing);
                    }
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                    CurrentLanguage = lang;
                }
                catch { }
            });
        }
    }
}
